using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
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
	public sealed class DiscardTransformer : Transformer
	{
		protected override void PostTransform( TransformContext ctx )
		{
			Rewriter walker = new( ctx.ResolutionResult, ctx.Module.Context, Diagnostics );
			walker.WalkModule( ctx.Module.Module );
		}

		private sealed class Rewriter : DeclWalker
		{
			private readonly ResolutionResult? resolutionResult;
			private readonly ModuleContext context;
			private readonly List<Diagnostic> diagnostics;

			// Temporaries introduced by the statement currently being walked; the
			// block hook inserts them before that statement.
			private List<Stmt> preStmts = new();

			public Rewriter( ResolutionResult? resolutionResult, ModuleContext context, List<Diagnostic> diagnostics )
			{
				this.resolutionResult = resolutionResult;
				this.context          = context;
				this.diagnostics      = diagnostics;
			}

			protected override MultiStmt TransformBlock( MultiStmt block )
			{
				List<Stmt> outer = preStmts;

				List<Stmt> stmts = block.stmts;
				int        i     = 0;

				while( i < stmts.Count ) {
					Stmt stmt = stmts[i];
					preStmts = new List<Stmt>();

					Stmt replacement = WalkStmt( stmt );

					if( preStmts.Count > 0 ) {
						stmts.InsertRange( i, preStmts );
						i += preStmts.Count;
					}

					if( replacement != stmt )
						stmts[i] = replacement;

					++i;
				}

				preStmts = outer;
				return block;
			}

			protected override Stmt TransformStmt( Stmt stmt )
			{
				switch( stmt ) {
					case MultiAssign multiAssign:
						return TransformMultiAssign( multiAssign );

					case AggrAssign aggr:
						if( aggr.leftExpr is Discard )
							Error( aggr.leftExpr.srcPos,
								"'_' cannot be used as the target of an aggregate assignment" );
						else
							WalkExpr( aggr.leftExpr );

						if( aggr.rightExpr is Discard )
							Error( aggr.rightExpr.srcPos,
								"'_' cannot be used as a value" );
						else
							WalkExpr( aggr.rightExpr );

						return aggr;

					case ExprStmt exprStmt:
						if( exprStmt.expr is Discard )
							Error( exprStmt.expr.srcPos,
								"'_' cannot be used as a value" );
						else
							WalkExpr( exprStmt.expr );

						return exprStmt;

					case VarStmt varStmt:
						if( varStmt.init != null ) {
							if( varStmt.init is Discard )
								Error( varStmt.init.srcPos,
									"'_' initializer should have been lowered earlier" );
							else
								WalkExpr( varStmt.init );
						}

						return varStmt;

					case ReturnStmt ret:
						if( ret.expr is Discard )
							Error( ret.expr.srcPos,
								"'_' cannot be returned from a function" );
						else if( ret.expr != null )
							WalkExpr( ret.expr );

						return ret;

					case IfStmt ifStmt:
						foreach( IfStmt.CondThen ct in ifStmt.ifThens ) {
							WalkExpr( ct.cond );
							ct.then = WalkNested( ct.then );
						}

						if( ifStmt.els != null )
							ifStmt.els = WalkNested( ifStmt.els );

						return ifStmt;

					case ForStmt forStmt:
						if( forStmt.init != null )
							forStmt.init = WalkStmt( forStmt.init );

						if( forStmt.cond != null )
							WalkExpr( forStmt.cond );

						if( forStmt.iter != null ) {
							int before = preStmts.Count;
							WalkExpr( forStmt.iter );
							if( preStmts.Count > before )
								Error( forStmt.iter.srcPos,
									"discard temporaries are not supported in a for-loop iterator" );
						}

						if( forStmt.body != null )
							forStmt.body = WalkNested( forStmt.body );

						if( forStmt.els != null )
							forStmt.els = WalkNested( forStmt.els );

						return forStmt;

					case RangeForStmt rangeForStmt:
						WalkExpr( rangeForStmt.range );

						if( rangeForStmt.body != null )
							rangeForStmt.body = WalkNested( rangeForStmt.body );

						if( rangeForStmt.els != null )
							rangeForStmt.els = WalkNested( rangeForStmt.els );

						return rangeForStmt;

					case WhileStmt whileStmt:
						WalkExpr( whileStmt.cond );
						whileStmt.body = WalkNested( whileStmt.body );

						if( whileStmt.els != null )
							whileStmt.els = WalkNested( whileStmt.els );

						return whileStmt;

					case DoWhileStmt doWhile:
						WalkExpr( doWhile.cond );
						doWhile.body = WalkNested( doWhile.body );
						return doWhile;

					case TimesStmt times:
						WalkExpr( times.count );
						times.body = WalkNested( times.body );
						return times;

					case SwitchStmt sw:
						WalkExpr( sw.cond );

						foreach( SwitchStmt.CaseBlock cb in sw.cases )
							cb.then = (MultiStmt)WalkNested( cb.then );

						if( sw.els != null )
							sw.els = (MultiStmt)WalkNested( sw.els );

						return sw;

					case TryCatchStmt tryCatch:
						tryCatch.tryBody = WalkNested( tryCatch.tryBody );

						foreach( CatchClause cc in tryCatch.catches )
							cc.body = WalkNested( cc.body );

						return tryCatch;

					default:
						return base.TransformStmt( stmt );
				}
			}

			/// <summary>
			/// Nested bodies always become blocks, with their temporaries wrapped inside
			/// so they run within the branch rather than before it.
			/// </summary>
			private Stmt WalkNested( Stmt stmt )
			{
				if( stmt is MultiStmt )
					return WalkStmt( stmt );

				List<Stmt> outer = preStmts;
				preStmts = new List<Stmt>();
				Stmt processed = WalkStmt( stmt );
				List<Stmt> pre = preStmts;
				preStmts = outer;

				pre.Add( processed );
				return new MultiStmt( pre, true );
			}

			private Stmt TransformMultiAssign( MultiAssign multiAssign )
			{
				// _ = expr  =>  static_cast<void>( expr );
				if( multiAssign.exprs.Count == 2 && multiAssign.exprs[0] is Discard ) {
					Expr right = multiAssign.exprs[1];
					WalkExpr( right );

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
						WalkExpr( expr );
					}
				}

				return multiAssign;
			}

			protected override void TransformExpr( Expr expr )
			{
				switch( expr ) {
					case FuncCallExpr callExpr:
						TransformFuncCallExpr( callExpr );
						break;

					case BinOp binOp:
						WalkExpr( binOp.left );
						WalkExpr( binOp.right );
						break;

					case TernOp ternOp:
						WalkExpr( ternOp.left );
						WalkExpr( ternOp.mid );
						WalkExpr( ternOp.right );
						break;

					case CastExpr cast:
						WalkExpr( cast.expr );
						break;

					case NewExpr newExpr:
						foreach( Arg arg in newExpr.funcCall.args ) {
							if( arg.expr is Discard )
								Error( arg.expr.srcPos,
									"'_' cannot be used as a value" );
							else
								WalkExpr( arg.expr );
						}

						break;

					case Lambda lambda:
						if( lambda.func.body != null )
							WalkBlock( lambda.func.body );

						break;

					case InitListExpr initList:
						foreach( Arg arg in initList.args ) {
							if( arg.expr is Discard )
								Error( arg.expr.srcPos,
									"'_' cannot be used as an initializer-list element" );
							else
								WalkExpr( arg.expr );
						}

						break;

					case UnOp unOp:
						WalkExpr( unOp.expr );
						break;

					case Discard:
						Error( expr.srcPos, "'_' cannot be used as a value" );
						break;

					default:
						break;
				}
			}

			private void TransformFuncCallExpr( FuncCallExpr callExpr )
			{
				Decl? calleeDecl = ResolveCallCallee( callExpr );
				List<Param> parameters = calleeDecl switch {
					Func func      => func.paras,
					Structor stc   => stc.paras,
					_              => new List<Param>(),
				};

				for( int i = 0; i < callExpr.funcCall.args.Count; i++ ) {
					Arg arg = callExpr.funcCall.args[i];
					Expr replacement;

					if( arg.expr is Discard ) {
						Typespec? paramType = i < parameters.Count ? parameters[i].type : null;

						if( paramType == null ) {
							Error( arg.expr.srcPos,
								"Cannot infer parameter type for '_'; overload resolution did not choose a single candidate" );
							replacement = MakeNullptrLiteral( arg.expr.srcPos );
						}
						else {
							replacement = ReplaceDiscardArg( paramType, arg.expr.srcPos );
						}
					}
					else {
						WalkExpr( arg.expr );
						continue;
					}

					arg.expr = replacement;
				}

				// Member access / scoped callees may contain nested discards.
				WalkExpr( callExpr.expr );
			}

			private Decl? ResolveCallCallee( FuncCallExpr callExpr )
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
					IdExpr id when resolutionResult != null && resolutionResult.TryGetResolved( id, out Decl? d )
						=> d,
					ScopedExpr scoped when resolutionResult != null && resolutionResult.TryGetResolved( scoped, out Decl? d )
						=> d,
					BinOp binOp when binOp.right is IdExpr m
						&& resolutionResult != null && resolutionResult.TryGetResolvedMember( m, out Decl? d )
						=> d,
					_ => null,
				};
			}

			private Expr ReplaceDiscardArg( Typespec paramType, SrcPos srcPos )
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
				Typespec tempType = paramType.Clone();
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

			private void Error( SrcPos? srcPos, string message )
			{
				diagnostics.Add( new Diagnostic(
					srcPos ?? new SrcPos { file = "<unknown>" },
					DiagnosticKind.Error,
					message ) );
			}
		}
	}
}