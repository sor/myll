using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Lowers Myll discard expressions (<c>_</c>) into valid C++ after name and overload
	/// resolution has determined the expected types.
	///
	/// Supported uses:
	/// <list type="bullet">
	///   <item><c>_ = expr;</c> becomes <c>static_cast&lt;void&gt;( expr );</c></item>
	///   <item><c>f(_)</c> for a raw pointer parameter becomes <c>static_cast&lt;T*&gt;( nullptr )</c></item>
	///   <item><c>f(_)</c> for a value or reference parameter introduces a hidden local</item>
	/// </list>
	/// All other uses of <c>_</c> produce a diagnostic error.
	/// </summary>
	public sealed class DiscardTransformer : ITransformer
	{
		private readonly ResolutionResult result;
		private readonly List<Diagnostic> diagnostics;

		public DiscardTransformer( ResolutionResult result, List<Diagnostic> diagnostics )
		{
			this.result      = result;
			this.diagnostics = diagnostics;
		}

		public void Transform(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules,
			List<Diagnostic> diagnostics )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				TransformDecl( module, context );
			}
		}

		private void TransformDecl( Decl decl, CompilationContext context )
		{
			switch( decl ) {
				case Func func:
					if( func.body != null )
						TransformBlock( func.body, context );
					break;

				case Structor stc:
					if( stc.body != null )
						TransformBlock( stc.body, context );
					break;

				case Hierarchical h:
					foreach( Decl child in h.children )
						TransformDecl( child, context );
					break;
			}
		}

		private void TransformBlock( MultiStmt block, CompilationContext context )
		{
			List<Stmt> stmts = block.stmts;
			int        i     = 0;

			while( i < stmts.Count ) {
				Stmt       stmt     = stmts[i];
				List<Stmt> preStmts = new();

				Stmt replacement = TransformTopLevelStmt( stmt, preStmts, context );

				if( preStmts.Count > 0 ) {
					stmts.InsertRange( i, preStmts );
					i += preStmts.Count;
				}

				if( replacement != stmt )
					stmts[i] = replacement;

				i++;
			}
		}

		private Stmt TransformTopLevelStmt( Stmt stmt, List<Stmt> preStmts, CompilationContext context )
		{
			switch( stmt ) {
				case MultiStmt multi:
					TransformBlock( multi, context );
					return multi;

				case MultiAssign multiAssign:
					return TransformMultiAssign( multiAssign, preStmts, context );

				case AggrAssign aggr:
					if( aggr.leftExpr is Discard )
						Error( aggr.leftExpr.srcPos,
							"'_' cannot be used as the target of an aggregate assignment" );
					else
						TransformExpr( aggr.leftExpr, preStmts, context );

					if( aggr.rightExpr is Discard )
						Error( aggr.rightExpr.srcPos,
							"'_' cannot be used as a value" );
					else
						TransformExpr( aggr.rightExpr, preStmts, context );

					return aggr;

				case ExprStmt exprStmt:
					if( exprStmt.expr is Discard ) {
						Error( exprStmt.expr.srcPos,
							"'_' cannot be used as a value" );
					}
					else {
						TransformExpr( exprStmt.expr, preStmts, context );
					}

					return exprStmt;

				case VarStmt varStmt:
					if( varStmt.init != null ) {
						if( varStmt.init is Discard )
							Error( varStmt.init.srcPos,
								"'_' initializer should have been lowered earlier" );
						else
							TransformExpr( varStmt.init, preStmts, context );
					}

					return varStmt;

				case ReturnStmt ret:
					if( ret.expr is Discard ) {
						Error( ret.expr.srcPos,
							"'_' cannot be returned from a function" );
					}
					else if( ret.expr != null ) {
						TransformExpr( ret.expr, preStmts, context );
					}

					return ret;

				case IfStmt ifStmt:
					foreach( IfStmt.CondThen ct in ifStmt.ifThens ) {
						TransformExpr( ct.cond, preStmts, context );
						ct.then = ProcessNestedBlockStmt( ct.then, context );
					}

					if( ifStmt.els != null )
						ifStmt.els = ProcessNestedBlockStmt( ifStmt.els, context );

					return ifStmt;

				case ForStmt forStmt:
					if( forStmt.init != null ) {
						var initPreStmts = new List<Stmt>();
						forStmt.init = TransformTopLevelStmt( forStmt.init, initPreStmts, context );
						preStmts.AddRange( initPreStmts );
					}

					if( forStmt.cond != null )
						TransformExpr( forStmt.cond, preStmts, context );

					if( forStmt.iter != null ) {
						var iterPreStmts = new List<Stmt>();
						TransformExpr( forStmt.iter, iterPreStmts, context );
						if( iterPreStmts.Count > 0 )
							Error( forStmt.iter.srcPos,
								"discard temporaries are not supported in a for-loop iterator" );
					}

					if( forStmt.body != null )
						forStmt.body = ProcessNestedBlockStmt( forStmt.body, context );

					if( forStmt.els != null )
						forStmt.els = ProcessNestedBlockStmt( forStmt.els, context );

					return forStmt;

				case WhileStmt whileStmt:
					TransformExpr( whileStmt.cond, preStmts, context );
					whileStmt.body = ProcessNestedBlockStmt( whileStmt.body, context );

					if( whileStmt.els != null )
						whileStmt.els = ProcessNestedBlockStmt( whileStmt.els, context );

					return whileStmt;

				case DoWhileStmt doWhile:
					TransformExpr( doWhile.cond, preStmts, context );
					doWhile.body = ProcessNestedBlockStmt( doWhile.body, context );
					return doWhile;

				case TimesStmt times:
					TransformExpr( times.count, preStmts, context );
					times.body = ProcessNestedBlockStmt( times.body, context );
					return times;

				case SwitchStmt sw:
					TransformExpr( sw.cond, preStmts, context );

					foreach( SwitchStmt.CaseBlock cb in sw.cases )
						cb.then = ProcessNestedBlockStmt( cb.then, context );

					if( sw.els != null )
						sw.els = ProcessNestedBlockStmt( sw.els, context );

					return sw;

				case TryCatchStmt tryCatch:
					tryCatch.tryBody = ProcessNestedBlockStmt( tryCatch.tryBody, context );

					foreach( CatchClause cc in tryCatch.catches ) {
						if( cc.body != null )
							cc.body = ProcessNestedBlockStmt( cc.body, context );
					}

					return tryCatch;

				default:
					return stmt;
			}
		}

		private MultiStmt ProcessNestedBlockStmt( Stmt stmt, CompilationContext context )
		{
			if( stmt is MultiStmt multi ) {
				TransformBlock( multi, context );
				return multi;
			}

			var preStmts   = new List<Stmt>();
			Stmt processed = TransformTopLevelStmt( stmt, preStmts, context );

			var block = new List<Stmt>();
			block.AddRange( preStmts );
			block.Add( processed );
			return new MultiStmt( block, true );
		}

		private Stmt TransformMultiAssign(
			MultiAssign multiAssign, List<Stmt> preStmts, CompilationContext context )
		{
			// _ = expr  =>  static_cast<void>( expr );
			if( multiAssign.exprs.Count == 2 && multiAssign.exprs[0] is Discard ) {
				Expr right = multiAssign.exprs[1];
				TransformExpr( right, preStmts, context );

				return new ExprStmt {
					srcPos = multiAssign.srcPos,
					expr   = MakeVoidCast( right ),
				};
			}

			for( int i = 0; i < multiAssign.exprs.Count; i++ ) {
				Expr expr = multiAssign.exprs[i];

				if( expr is Discard ) {
					Error( expr.srcPos,
						"'_' may only appear as the left-hand side of a simple assignment" );
				}
				else {
					TransformExpr( expr, preStmts, context );
				}
			}

			return multiAssign;
		}

		private void TransformExpr( Expr expr, List<Stmt> preStmts, CompilationContext context )
		{
			switch( expr ) {
				case FuncCallExpr callExpr:
					TransformFuncCallExpr( callExpr, preStmts, context );
					break;

				case BinOp binOp:
					TransformExpr( binOp.left, preStmts, context );
					TransformExpr( binOp.right, preStmts, context );
					break;

				case TernOp ternOp:
					TransformExpr( ternOp.left, preStmts, context );
					TransformExpr( ternOp.mid, preStmts, context );
					TransformExpr( ternOp.right, preStmts, context );
					break;

				case CastExpr cast:
					TransformExpr( cast.expr, preStmts, context );
					break;

				case NewExpr newExpr:
					foreach( Arg arg in newExpr.funcCall.args ) {
						if( arg.expr is Discard )
							Error( arg.expr.srcPos,
								"'_' cannot be used as a value" );
						else
							TransformExpr( arg.expr, preStmts, context );
					}

					break;

				case Lambda lambda:
					if( lambda.func.body != null )
						TransformBlock( lambda.func.body, context );

					break;

				case UnOp unOp:
					TransformExpr( unOp.expr, preStmts, context );
					break;

				case Discard:
					Error( expr.srcPos, "'_' cannot be used as a value" );
					break;

				default:
					break;
			}
		}

		private void TransformFuncCallExpr(
			FuncCallExpr callExpr, List<Stmt> preStmts, CompilationContext context )
		{
			Decl? calleeDecl = ResolveCallCallee( callExpr, context );
			List<Param> parameters = calleeDecl switch {
				Func func      => func.paras,
				Structor stc   => stc.paras,
				_              => new List<Param>(),
			};

			for( int i = 0; i < callExpr.funcCall.args.Count; i++ ) {
				Arg  arg = callExpr.funcCall.args[i];
				Expr replacement;

				if( arg.expr is Discard ) {
					Typespec? paramType = i < parameters.Count ? parameters[i].type : null;

					if( paramType == null ) {
						Error( arg.expr.srcPos,
							"Cannot infer parameter type for '_'; overload resolution did not choose a single candidate" );
						replacement = MakeNullptrLiteral( arg.expr.srcPos );
					}
					else {
						replacement = ReplaceDiscardArg( paramType, arg.expr.srcPos, preStmts, context );
					}
				}
				else {
					TransformExpr( arg.expr, preStmts, context );
					continue;
				}

				arg.expr = replacement;
			}

			// Member access / scoped callees may contain nested discards.
			TransformExpr( callExpr.expr, preStmts, context );
		}

		private Decl? ResolveCallCallee( FuncCallExpr callExpr, CompilationContext context )
		{
			UnresolvedCall? unresolved = context.UnresolvedCalls
				.FirstOrDefault( uc => uc.Call == callExpr.funcCall );

			if( unresolved != null )
				return ResolveCalleeDecl( unresolved.Callee );

			return null;
		}

		private Decl? ResolveCalleeDecl( Expr callee )
		{
			return callee switch {
				IdExpr id when result.TryGetResolved( id, out Decl? d )     => d,
				ScopedExpr scoped when result.TryGetResolved( scoped, out Decl? d ) => d,
				BinOp binOp when binOp.right is IdExpr m
				             && result.TryGetResolvedMember( m, out Decl? d )       => d,
				_                                                           => null,
			};
		}

		private Expr ReplaceDiscardArg(
			Typespec paramType, SrcPos srcPos, List<Stmt> preStmts, CompilationContext context )
		{
			List<Pointer>? ptrs = paramType.ptrs;

			if( ptrs is { Count: > 0 } ) {
				Pointer.Kind kind = ptrs[ptrs.Count - 1].kind;

				if( kind == Pointer.Kind.RawPtr )
					return MakeNullptrCast( paramType, srcPos );

				if( kind.Between( Pointer.Kind.SmartPtr_Begin, Pointer.Kind.SmartPtr_End ) ) {
					Error( srcPos,
						"'_' cannot be used for smart-pointer parameters" );

					return MakeNullptrCast( paramType, srcPos );
				}
			}

			// Value or reference parameter: introduce a hidden local.
			// Drop any const/reference qualifiers; the temp must be default-initializable.
			Typespec tempType = CloneTypespecBase( paramType );
			tempType.qual = Qualifier.None;
			if( tempType.ptrs is { Count: > 0 } )
				tempType.ptrs.RemoveAt( tempType.ptrs.Count - 1 );

			string tempName = context.NextTempName();

			preStmts.Add( new VarStmt {
				srcPos = srcPos,
				name   = tempName,
				kind   = VarDecl.Kind.Var,
				type   = tempType,
			} );

			return new IdExpr {
				srcPos    = srcPos,
				op        = Operand.Id,
				idTplArgs = new IdTplArgs { id = tempName },
			};
		}

		private static Expr MakeNullptrCast( Typespec targetType, SrcPos srcPos )
		{
			return new CastExpr {
				op     = Operand.StaticCast,
				type   = targetType,
				expr   = MakeNullptrLiteral( srcPos ),
				Type   = targetType,
				srcPos = srcPos,
			};
		}

		private static Expr MakeVoidCast( Expr expr )
		{
			Typespec voidType = new TypespecBasic {
				kind = TypespecBasic.Kind.Void,
				size = TypespecBasic.SizeInvalid,
			};

			return new CastExpr {
				op     = Operand.StaticCast,
				type   = voidType,
				expr   = expr,
				Type   = voidType,
				srcPos = expr.srcPos,
			};
		}

		private static Literal MakeNullptrLiteral( SrcPos srcPos )
		{
			return new Literal {
				op     = Operand.Literal,
				text   = "nullptr",
				srcPos = srcPos,
				Type   = new TypespecBasic {
					kind = TypespecBasic.Kind.ExplicitAuto,
					size = TypespecBasic.SizeUndetermined,
				},
			};
		}

		private static Typespec CloneTypespecBase( Typespec source )
		{
			Typespec ret = source switch {
				TypespecBasic b  => new TypespecBasic {
					kind           = b.kind,
					size           = b.size,
					align          = b.align,
					isDefaultSized = b.isDefaultSized,
				},
				TypespecNested n => new TypespecNested {
					resolvedDecl = n.resolvedDecl,
					idTpls = n.idTpls
						.Select( it => new IdTplArgs { id = it.id, tplArgs = it.tplArgs } )
						.ToList(),
				},
				TypespecFunc f   => new TypespecFunc {
					paras   = f.paras,
					retType = f.retType,
				},
				_                => throw new InvalidOperationException(
					"unknown Typespec variant" ),
			};

			ret.srcPos = source.srcPos;
			ret.qual   = source.qual;
			return ret;
		}

		private void Error( SrcPos? srcPos, string message )
		{
			diagnostics.Add( new Diagnostic(
				srcPos ?? new SrcPos { file = "<unknown>" },
				DiagnosticKind.Error,
				message ) );
		}
	}
}
