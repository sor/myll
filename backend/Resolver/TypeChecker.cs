using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Validates type-correctness after name and overload resolution. Emits diagnostics for
	/// mismatched assignments, initializers, returns, and function arguments.
	/// </summary>
	public sealed class TypeChecker
	{
		private readonly ResolutionResult result;
		private readonly TypeResolver typeResolver;
		private readonly List<Diagnostic> diagnostics;

		public TypeChecker( ResolutionResult result, List<Diagnostic> diagnostics )
		{
			this.result        = result;
			this.typeResolver  = new TypeResolver( result );
			this.diagnostics   = diagnostics;
		}

		public void Validate(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				ValidateCalls( context );
				ValidateDecl( module );
			}
		}

		private void ValidateCalls( CompilationContext context )
		{
			foreach( UnresolvedCall call in context.UnresolvedCalls ) {
				Decl? calleeDecl = ResolveCalleeDecl( call.Callee );
				if( calleeDecl == null )
					continue;

				List<Param> paras = calleeDecl switch {
					Func func      => func.paras,
					Structor stc   => stc.paras,
					_              => new List<Param>(),
				};

				if( paras.Count != call.Call.args.Count )
					continue; // arity mismatch is already reported by overload resolution

				for( int i = 0; i < paras.Count; i++ ) {
					Typespec? argType = typeResolver.Resolve( call.Call.args[i].expr );
					if( argType == null )
						continue; // cannot determine argument type yet

					if( !ConversionRules.IsImplicitlyConvertible( argType, paras[i].type ) ) {
						diagnostics.Add( new Diagnostic(
							call.Call.args[i].expr.srcPos,
							DiagnosticKind.Error,
							String.Format(
								"Cannot convert argument type '{0}' to parameter type '{1}'",
								FormatType( argType ),
								FormatType( paras[i].type ) ) ) );
					}
				}
			}
		}

		private Decl? ResolveCalleeDecl( Expr callee )
		{
			return callee switch {
				IdExpr id when result.TryGetResolved( id, out Decl? d )        => d,
				ScopedExpr scoped when result.TryGetResolved( scoped, out Decl? d ) => d,
				BinOp binOp when binOp.right is IdExpr m
				             && result.TryGetResolvedMember( m, out Decl? d )   => d,
				_                                                            => null,
			};
		}

		private void ValidateDecl( Decl decl )
		{
			switch( decl ) {
				case Func func:
					if( func.body != null )
						ValidateStmt( func.body, func );
					break;

				case Structor stc:
					if( stc.body != null )
						ValidateStmt( stc.body, null );
					break;

				case VarDecl vd:
					if( vd.init != null ) {
						if( vd.type is TypespecBasic { kind: TypespecBasic.Kind.Auto } )
							vd.type = InferAutoType( typeResolver.Resolve( vd.init ) ) ?? vd.type;

						CheckAssignment( vd.type, vd.init, vd.init.srcPos );
					}
					break;

				case Hierarchical h:
					foreach( Decl child in h.children )
						ValidateDecl( child );
					break;
			}
		}

		private void ValidateStmt( Stmt stmt, Func? currentFunction )
		{
			switch( stmt ) {
				case MultiStmt multi:
					foreach( Stmt s in multi.stmts )
						ValidateStmt( s, currentFunction );
					break;

				case VarDecl vd:
					ValidateDecl( vd );
					break;

				case ReturnStmt ret:
					ValidateReturn( ret, currentFunction );
					break;

				case MultiAssign multiAssign:
					ValidateMultiAssign( multiAssign );
					break;

				case AggrAssign aggrAssign:
					CheckAssignment( aggrAssign.leftExpr, aggrAssign.rightExpr, aggrAssign.rightExpr.srcPos );
					break;

				case ExprStmt exprStmt: {
					if( exprStmt.expr is BinOp { op: Operand.Equal } assign )
						CheckAssignment( assign.left, assign.right, assign.right.srcPos );
					break;
				}

				case IfStmt ifStmt:
					foreach( IfStmt.CondThen ifThen in ifStmt.ifThens )
						ValidateStmt( ifThen.then, currentFunction );
					if( ifStmt.els != null )
						ValidateStmt( ifStmt.els, currentFunction );
					break;

				case ForStmt forStmt:
					if( forStmt.init != null )
						ValidateStmt( forStmt.init, currentFunction );
					if( forStmt.body != null )
						ValidateStmt( forStmt.body, currentFunction );
					if( forStmt.els != null )
						ValidateStmt( forStmt.els, currentFunction );
					break;

				case WhileStmt whileStmt:
					ValidateStmt( whileStmt.body, currentFunction );
					if( whileStmt.els != null )
						ValidateStmt( whileStmt.els, currentFunction );
					break;

				case DoWhileStmt doWhile:
					ValidateStmt( doWhile.body, currentFunction );
					break;

				case TimesStmt times:
					ValidateStmt( times.body, currentFunction );
					break;

				case SwitchStmt sw:
					foreach( SwitchStmt.CaseBlock c in sw.cases )
						ValidateStmt( c.then, currentFunction );
					if( sw.els != null )
						ValidateStmt( sw.els, currentFunction );
					break;

				case TryCatchStmt tryCatch:
					// try and catch bodies are currently generic Stmt? Check actual properties if available.
					break;
			}
		}

		private void ValidateReturn( ReturnStmt ret, Func? currentFunction )
		{
			if( currentFunction == null ) {
				if( ret.expr != null ) {
					diagnostics.Add( new Diagnostic(
						ret.expr.srcPos,
						DiagnosticKind.Error,
						"Cannot return a value from a constructor/destructor" ) );
				}

				return;
			}

			Typespec expected = currentFunction.retType;
			bool isVoid = expected is TypespecBasic { kind: TypespecBasic.Kind.Void };

			if( ret.expr == null ) {
				if( !isVoid ) {
					diagnostics.Add( new Diagnostic(
						ret.srcPos,
						DiagnosticKind.Error,
						String.Format( "Function '{0}' must return a value of type '{1}'",
							currentFunction.name, FormatType( expected ) ) ) );
				}

				return;
			}

			if( isVoid ) {
				diagnostics.Add( new Diagnostic(
					ret.expr.srcPos,
					DiagnosticKind.Error,
					String.Format( "Function '{0}' returns void and may not return a value",
						currentFunction.name ) ) );
				return;
			}

			Typespec? actual = typeResolver.Resolve( ret.expr );
			if( actual == null )
				return;

			if( !ConversionRules.IsImplicitlyConvertible( actual, expected ) ) {
				diagnostics.Add( new Diagnostic(
					ret.expr.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"Cannot return type '{0}' from function '{1}' expecting '{2}'",
						FormatType( actual ),
						currentFunction.name,
						FormatType( expected ) ) ) );
			}
		}

		private void ValidateMultiAssign( MultiAssign multiAssign )
		{
			// a = b = c => check b -> a, c -> b
			for( int i = 0; i + 1 < multiAssign.exprs.Count; i++ ) {
				Expr left  = multiAssign.exprs[i];
				Expr right = multiAssign.exprs[i + 1];
				CheckAssignment( left, right, right.srcPos );
			}
		}

		private void CheckAssignment( Expr left, Expr right, SrcPos srcPos )
		{
			Typespec? leftType  = typeResolver.Resolve( left );
			Typespec? rightType = typeResolver.Resolve( right );

			if( leftType == null || rightType == null )
				return;

			if( !ConversionRules.IsImplicitlyConvertible( rightType, leftType ) ) {
				diagnostics.Add( new Diagnostic(
					srcPos,
					DiagnosticKind.Error,
					String.Format(
						"Cannot convert '{0}' to '{1}'",
						FormatType( rightType ),
						FormatType( leftType ) ) ) );
			}
		}

		private void CheckAssignment( Typespec leftType, Expr right, SrcPos srcPos )
		{
			Typespec? rightType = typeResolver.Resolve( right );
			if( rightType == null )
				return;

			if( !ConversionRules.IsImplicitlyConvertible( rightType, leftType ) ) {
				diagnostics.Add( new Diagnostic(
					srcPos,
					DiagnosticKind.Error,
					String.Format(
						"Cannot convert '{0}' to '{1}'",
						FormatType( rightType ),
						FormatType( leftType ) ) ) );
			}
		}

		private static Typespec? InferAutoType( Typespec? initType )
		{
			if( initType == null )
				return null;

			return initType switch {
				TypespecBasic basic when basic.kind == TypespecBasic.Kind.UntypedInteger
					=> new TypespecBasic { kind = TypespecBasic.Kind.Integer, size = 4 },
				TypespecBasic basic when basic.kind == TypespecBasic.Kind.UntypedFloat
					=> new TypespecBasic { kind = TypespecBasic.Kind.Float, size = Dialect.DefaultFloatSize() },
				_ => initType,
			};
		}

		private static string FormatType( Typespec type )
		{
			return type switch {
				TypespecBasic basic when basic.kind == TypespecBasic.Kind.UntypedInteger
				                       => "untyped integer",
				TypespecBasic basic when basic.kind == TypespecBasic.Kind.UntypedFloat
				                       => "untyped float",
				_                      => type.GenType(),
			};
		}
	}
}
