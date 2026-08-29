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
					ValidateExpression( call.Call.args[i].expr );

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
				IdExpr     id     when result.TryGetResolved( id, out Decl? d )     => d,
				ScopedExpr scoped when result.TryGetResolved( scoped, out Decl? d ) => d,
				BinOp      binOp  when binOp.right is IdExpr m
				             && result.TryGetResolvedMember( m, out Decl? d )       => d,
				_                                                                   => null,
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
					ValidateVarAttributes( vd );
					if( vd.init != null ) {
						if( vd.type is TypespecBasic { kind: TypespecBasic.Kind.Auto } )
							vd.type = InferAutoType( typeResolver.Resolve( vd.init ) ) ?? vd.type;

						CheckAssignment( vd.type, vd.init, vd.init.srcPos );
					}
					break;

				case Hierarchical h:
					ValidateUniqueVarNames( h );
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

				case VarStmt vs:
					if( vs.init != null ) {
						if( vs.type is TypespecBasic { kind: TypespecBasic.Kind.Auto } )
							vs.type = InferAutoType( typeResolver.Resolve( vs.init ) ) ?? vs.type;

						CheckAssignment( vs.type, vs.init, vs.init.srcPos );
					}
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
					ValidateExpression( exprStmt.expr );

					if( exprStmt.expr is BinOp { op: Operand.Equal } assign )
						CheckAssignment( assign.left, assign.right, assign.right.srcPos );

					break;
				}

				case IfStmt ifStmt:
					foreach( IfStmt.CondThen ifThen in ifStmt.ifThens ) {
						ValidateCondition( ifThen.cond, ifThen.cond.srcPos, "if" );
						ValidateStmt( ifThen.then, currentFunction );
					}
					if( ifStmt.els != null )
						ValidateStmt( ifStmt.els, currentFunction );
					break;

				case ForStmt forStmt:
					if( forStmt.init != null )
						ValidateStmt( forStmt.init, currentFunction );
					if( forStmt.cond != null )
						ValidateCondition( forStmt.cond, forStmt.cond.srcPos, "for" );
					if( forStmt.body != null )
						ValidateStmt( forStmt.body, currentFunction );
					if( forStmt.els != null )
						ValidateStmt( forStmt.els, currentFunction );
					break;

				case WhileStmt whileStmt:
					ValidateCondition( whileStmt.cond, whileStmt.cond.srcPos, "while" );
					ValidateStmt( whileStmt.body, currentFunction );
					if( whileStmt.els != null )
						ValidateStmt( whileStmt.els, currentFunction );
					break;

				case DoWhileStmt doWhile:
					ValidateStmt( doWhile.body, currentFunction );
					ValidateCondition( doWhile.cond, doWhile.cond.srcPos, "do-while" );
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

			ValidateExpression( ret.expr );

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

			ValidateExpression( right );

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
			ValidateExpression( right );

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

		private void ValidateCondition( Expr cond, SrcPos srcPos, string context )
		{
			CheckBooleanOperand( cond, context );
		}

		private void CheckBooleanOperand( Expr expr, string context )
		{
			switch( expr ) {
				case BinOp binOp when binOp.op is Operand.And or Operand.Or: {
					CheckBooleanOperand( binOp.left, context );
					CheckBooleanOperand( binOp.right, context );
					break;
				}

				case UnOp unOp when unOp.op == Operand.Negation: {
					CheckBooleanOperand( unOp.expr, context );
					break;
				}

				default: {
					Typespec? type = typeResolver.Resolve( expr );
					if( type != null && !IsBoolType( type ) ) {
						diagnostics.Add( new Diagnostic(
							expr.srcPos,
							DiagnosticKind.Error,
							String.Format(
								"Condition of '{0}' must be bool, found '{1}'",
								context,
								FormatType( type ) ) ) );
					}

					break;
				}
			}
		}

		private static bool IsBoolType( Typespec type )
		{
			if( type is not TypespecBasic basic )
				return false;

			return basic.kind == TypespecBasic.Kind.Bool && ( type.ptrs == null || type.ptrs.Count == 0 );
		}

		private void ValidateExpression( Expr expr )
		{
			// Logical/conditional operands and a subset of built-in scalar operators are
			// checked here. Anything involving classes, references, or user-defined
			// operator overloads is left for C++ to resolve.
			switch( expr ) {
				case TernOp tern: {
					ValidateCondition( tern.left, tern.left.srcPos, "?:" );
					break;
				}

				case BinOp binOp when binOp.op is Operand.And or Operand.Or: {
					CheckBooleanOperand( binOp.left, binOp.op.ToString().ToLowerInvariant() );
					CheckBooleanOperand( binOp.right, binOp.op.ToString().ToLowerInvariant() );
					break;
				}

				case UnOp unOp when unOp.op == Operand.Negation: {
					CheckBooleanOperand( unOp.expr, "!" );
					break;
				}

				case BinOp binOp when IsArithmeticOperator( binOp.op ): {
					ValidateArithmeticOperator( binOp );
					break;
				}

				case BinOp binOp when IsBitwiseOperator( binOp.op ): {
					ValidateBitwiseOperator( binOp );
					break;
				}

				case BinOp binOp when IsShiftOperator( binOp.op ): {
					ValidateShiftOperator( binOp );
					break;
				}

				case BinOp binOp when IsComparisonOperator( binOp.op ): {
					ValidateComparisonOperator( binOp );
					break;
				}

				case UnOp unOp when IsArithmeticUnaryOperator( unOp.op ): {
					ValidateArithmeticUnaryOperator( unOp );
					break;
				}

				case UnOp unOp when unOp.op == Operand.Complement: {
					ValidateComplementOperator( unOp );
					break;
				}

				case NewExpr newExpr: {
					if( Dialect.StrictNew
					 && ( newExpr.type.ptrs == null || newExpr.type.ptrs.Count == 0 ) )
						AddError( newExpr, "StrictNew is enabled: bare `new T` is not allowed; use an explicit pointer type such as `new T*` or `new T*!`." );

					break;
				}
			}
		}

		private static bool IsArithmeticOperator( Operand op )
			=> op is Operand.Add or Operand.Subtract or Operand.Multiply
			 || op is Operand.Divide or Operand.EuclideanDivide or Operand.Modulo;

		private static bool IsBitwiseOperator( Operand op )
			=> op is Operand.BitAnd or Operand.BitOr or Operand.BitXor;

		private static bool IsShiftOperator( Operand op )
			=> op is Operand.LeftShift or Operand.RightShift;

		private static bool IsComparisonOperator( Operand op )
			=> op is Operand.Equal or Operand.NotEqual or Operand.LessThan
			 || op is Operand.LessEqual or Operand.GreaterThan or Operand.GreaterEqual
			 || op is Operand.Comparison;

		private static bool IsArithmeticUnaryOperator( Operand op )
			=> op is Operand.PrePlus or Operand.PreMinus
			 || op is Operand.PreIncr or Operand.PreDecr
			 || op is Operand.PostIncr or Operand.PostDecr;

		private void ValidateArithmeticOperator( BinOp binOp )
		{
			Typespec? leftType  = typeResolver.Resolve( binOp.left );
			Typespec? rightType = typeResolver.Resolve( binOp.right );

			if( leftType == null || rightType == null )
				return;

			// Leave class-typed operands and their operator overloads to C++.
			if( leftType is not TypespecBasic || rightType is not TypespecBasic )
				return;

			if( binOp.op is Operand.Add or Operand.Subtract
			 && ( OperatorRules.HasPointer( leftType ) || OperatorRules.HasPointer( rightType ) ) ) {
				AddError( binOp, "Pointer arithmetic is disabled; use indexing instead." );
				return;
			}

			if( binOp.op == Operand.Modulo ) {
				if( !OperatorRules.IsScalarInteger( leftType ) || !OperatorRules.IsScalarInteger( rightType ) )
					AddError( binOp, String.Format( "Operator '{0}' requires integer operands, found '{1}' and '{2}'", OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );

				return;
			}

			if( !OperatorRules.IsScalarNumber( leftType ) || !OperatorRules.IsScalarNumber( rightType ) )
				AddError( binOp, String.Format( "Operator '{0}' cannot be applied to types '{1}' and '{2}'", OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );
		}

		private void ValidateBitwiseOperator( BinOp binOp )
		{
			Typespec? leftType  = typeResolver.Resolve( binOp.left );
			Typespec? rightType = typeResolver.Resolve( binOp.right );

			if( leftType == null || rightType == null )
				return;

			if( leftType is not TypespecBasic || rightType is not TypespecBasic )
				return;

			if( !OperatorRules.IsScalarInteger( leftType ) || !OperatorRules.IsScalarInteger( rightType ) )
				AddError( binOp, String.Format( "Operator '{0}' requires integer operands, found '{1}' and '{2}'", OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );
		}

		private void ValidateShiftOperator( BinOp binOp )
		{
			Typespec? leftType  = typeResolver.Resolve( binOp.left );
			Typespec? rightType = typeResolver.Resolve( binOp.right );

			if( leftType == null || rightType == null )
				return;

			// Class-typed left operands are using << for stream insertion or a user-defined overload.
			if( leftType is not TypespecBasic )
				return;

			if( !OperatorRules.IsScalarInteger( leftType ) )
				AddError( binOp.left, String.Format( "Left operand of '{0}' must be an integer type, found '{1}'", OperatorName( binOp.op ), FormatType( leftType ) ) );

			if( rightType is TypespecBasic && !OperatorRules.IsScalarInteger( rightType ) )
				AddError( binOp.right, String.Format( "Right operand of '{0}' must be an integer type, found '{1}'", OperatorName( binOp.op ), FormatType( rightType ) ) );
		}

		private void ValidateComparisonOperator( BinOp binOp )
		{
			Typespec? leftType  = typeResolver.Resolve( binOp.left );
			Typespec? rightType = typeResolver.Resolve( binOp.right );

			if( leftType == null || rightType == null )
				return;

			if( leftType is not TypespecBasic || rightType is not TypespecBasic )
				return;

			if( !OperatorRules.IsScalarComparable( leftType ) || !OperatorRules.IsScalarComparable( rightType ) ) {
				AddError( binOp, String.Format( "Operator '{0}' cannot compare types '{1}' and '{2}'", OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );
				return;
			}

			// Mixed signed/unsigned comparisons are allowed by C++ but produce warnings
			// in some configurations; for now we leave them alone.
		}

		private void ValidateArithmeticUnaryOperator( UnOp unOp )
		{
			Typespec? operandType = typeResolver.Resolve( unOp.expr );
			if( operandType == null )
				return;

			if( operandType is not TypespecBasic )
				return;

			if( !OperatorRules.IsScalarNumber( operandType ) )
				AddError( unOp, String.Format( "Operator '{0}' cannot be applied to type '{1}'", OperatorName( unOp.op ), FormatType( operandType ) ) );
		}

		private void ValidateComplementOperator( UnOp unOp )
		{
			Typespec? operandType = typeResolver.Resolve( unOp.expr );
			if( operandType == null )
				return;

			if( operandType is not TypespecBasic )
				return;

			if( !OperatorRules.IsScalarInteger( operandType ) )
				AddError( unOp, String.Format( "Operator '~' cannot be applied to type '{0}'", FormatType( operandType ) ) );
		}

		private static string OperatorName( Operand op )
		{
			return op switch {
				Operand.Add             => "+",
				Operand.Subtract        => "-",
				Operand.Multiply        => "*",
				Operand.Divide          => "\u00f7",
				Operand.EuclideanDivide => "/",
				Operand.Modulo          => "%",
				Operand.BitAnd          => "&",
				Operand.BitOr           => "|",
				Operand.BitXor          => "^",
				Operand.LeftShift       => "<<",
				Operand.RightShift      => ">>",
				Operand.Equal           => "==",
				Operand.NotEqual        => "!=",
				Operand.LessThan        => "<",
				Operand.LessEqual       => "<=",
				Operand.GreaterThan     => ">",
				Operand.GreaterEqual    => ">=",
				Operand.Comparison      => "<=>",
				Operand.PrePlus         => "+",
				Operand.PreMinus        => "-",
				Operand.PreIncr         => "++",
				Operand.PreDecr         => "--",
				Operand.PostIncr        => "++",
				Operand.PostDecr        => "--",
				_                       => op.ToString(),
			};
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

		private void ValidateVarAttributes( VarDecl vd )
		{
			bool isInsideStruct = vd.IsInStruct;
			bool isStatic       = vd.IsStatic;
			bool isHidden       = vd.IsHidden;
			bool isCompileTime  = vd.IsCompileTime;
			bool isInline       = vd.IsInline;
			bool isExtern       = vd.IsExternal;
			bool isConstType    = (vd.type.qual & Qualifier.Const) != 0;

			if( isInsideStruct ) {
				if( isHidden )                                               AddError( vd, "[hide]/[hidden] is only valid at module/namespace scope." );
				if( isExtern )                                               AddError( vd, "[extern] is only valid at module/namespace scope." );
				if( isInline && !isStatic )                                  AddError( vd, "[inline] on a class field requires [static]." );
				if( isCompileTime && !isStatic )                             AddError( vd, "[ct] on a class field requires [static]." );
			} else {
				if( isStatic )                                               AddError( vd, "[static] is valid only on class fields; use [hide] for module variables." );
				if( isInline && isHidden )                                   AddError( vd, "[inline] and [hide] are mutually exclusive." );
				if( isExtern && isInline )                                   AddError( vd, "[extern] and [inline] are mutually exclusive." );
				if( isExtern && isHidden )                                   AddError( vd, "[extern] and [hide]/[hidden] are mutually exclusive." );
				if( isExtern && isCompileTime )                              AddError( vd, "[extern] cannot be used with [ct]." );
				if( isExtern && isConstType )                                AddError( vd, "[extern] cannot be used with const." );
				if( isExtern && vd.init != null )                            AddError( vd, "[extern] variables cannot have an initializer." );
			}
		}

		private void ValidateUniqueVarNames( Hierarchical h )
		{
			HashSet<string> seen = new();

			foreach( Decl child in h.children ) {
				if( child is not VarDecl vd )
					continue;

				if( !seen.Add( vd.name ) )
					AddError( vd, String.Format( "Duplicate variable/field declaration: {0}", vd.name ) );
			}
		}

		private void AddError( Decl decl, string message )
			=> AddError( decl.srcPos, message );

		private void AddError( Expr expr, string message )
			=> AddError( expr.srcPos, message );

		private void AddError( SrcPos srcPos, string message )
		{
			diagnostics.Add( new Diagnostic( srcPos, DiagnosticKind.Error, message ) );
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
