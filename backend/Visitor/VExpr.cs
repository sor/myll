using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Antlr4.Runtime.Tree;
using Myll.Core;

using static Myll.MyllParser;

namespace Myll
{
	/**
	 * Only Visit can receive null and will return null, the
	 * other Visit... methods do not support null parameters
	 */
	public class ExprVisitor
		: ExtendedVisitor<Expr>
	{
		public ExprVisitor( Stack<Scope> scopeStack ) : base( scopeStack ) {}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override Expr Visit( IParseTree c )
			=> c == null
				? null
				: base.Visit( c );

		public override Expr VisitScopedExpr( ScopedExprContext c )
		{
			ScopedExpr ret = new() {
				op     = Operand.Scoped,
				idTpls = c.idTplArgs().Select( VisitIdTplArgs ).ToList(),
			//	expr   = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitPostExpr( PostExprContext c )
		{
			Expr ret;
			Expr left = c.expr().Visit();
			if( c.postOP() != null ) {
				ret = new UnOp {
					expr = left,
					op   = c.postOP().v.ToOp(),
				};
			}
			else if( c.funcCall() != null ) {
				ret = new FuncCallExpr {
					op       = c.funcCall().ary.ToOp(),
					expr     = left,
					funcCall = VisitFuncCall( c.funcCall() ),
				};
			}
			else if( c.indexCall() != null ) {
				ret = new FuncCallExpr {
					op       = c.indexCall().ary.ToOp(),
					expr     = left,
					funcCall = VisitIndexCall( c.indexCall() ),
				};
			}
			else if( c.memAccOP() != null ) {
				IdExpr right = new() {
					op        = Operand.Id,
					idTplArgs = VisitIdTplArgs( c.idTplArgs() ),
				};
				ret = new BinOp {
					op    = c.memAccOP().v.ToOp(),
					left  = left,
					right = right,
				};
			}
			else {
				throw new Exception( "unknown post op" );
			}

			return ret;
		}

		public override Expr VisitNewExpr( NewExprContext c )
		{
			Expr ret = new NewExpr {
				op       = Operand.New,
				type     = VisitTypespec( c.typespec() ),
				funcCall = VisitFuncCall( c.funcCall() ),
			};
			return ret;
		}

		public override Expr VisitPreExpr( PreExprContext c )
		{
			Expr ret;
			Expr expr = c.expr().Visit();

			if( c.typespec() != null
			 || c.MOVE()     != null ) {
				Operand op =
					c.QM()   != null ? Operand.DynamicCast :
					c.EM()   != null ? Operand.AnyCast :
					c.MOVE() != null ? Operand.MoveCast :
					                   Operand.StaticCast;

				Typespec t = (op != Operand.MoveCast)
					? VisitTypespec( c.typespec() )
					: new TypespecNested {
						srcPos = c.ToSrcPos(),
						idTpls = new List<IdTplArgs> {
							new() {
								id      = "move", // TODO: support std::forward as well
								tplArgs = new List<TplArg>(),
							}
						}
					};
				ret = new CastExpr {
					op   = op,
					type = t,
					expr = expr,
				};
			}
			else if( c.preOP() != null ) {
				ret = new UnOp {
					op   = c.preOP().v.ToPreOp(),
					expr = expr,
				};
			}
			else if( c.SIZEOF() != null ) {
				ret = new UnOp {
					op   = Operand.SizeOf,
					expr = expr,
				};
			}
			else if( c.DELETE() != null ) {
				ret = new UnOp {
					op   = c.ary != null ? Operand.DeleteAry : Operand.Delete,
					expr = expr,
				};
			}
			else {
				throw new InvalidOperationException( "unknown pre-op " + c );
			}
			return ret;
		}

		// TODO: check if this really works
		public override Expr VisitMemPtrExpr( MemPtrExprContext c )
		{
			BinOp ret = new() {
				op    = c.memAccPtrOP().v.ToOp(),
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitPowExpr( PowExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.Pow,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitMultExpr( MultExprContext c )
		{
			BinOp ret = new() {
				op    = c.multOP().v.ToOp(),
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitAddExpr( AddExprContext c )
		{
			BinOp ret = new() {
				op    = c.addOP().v.ToOp(),
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitShiftExpr( ShiftExprContext c )
		{
			BinOp ret = new() {
				op    = c.shiftOP().LSHIFT() != null ? Operand.LeftShift : Operand.RightShift,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitComparisonExpr( ComparisonExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.Comparison,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		private class FlattenRelational
		{
			private readonly List<Expr>    exprs = new( 4 );
			private readonly List<Operand> ops   = new( 4 );

			public FlattenRelational(IRelEqExprContext c) => Descent( c );
			private void Descent( IRelEqExprContext c )
			{
				{
					ExprContext l = c.expr( 0 );
					if( l is IRelEqExprContext lre ) Descent( lre );
					else exprs.Add( l.Visit() );
				}
				ops.Add( c.Op );
				{
					ExprContext r = c.expr( 1 );
					if( r is IRelEqExprContext rre ) Descent( rre );
					else exprs.Add( r.Visit() );
				}
			}

			public BinOp VisitWithAnd()
			{
				BinOp left = new() {
					op    = ops[0],
					left  = exprs[0],
					right = exprs[1],
				};

				for( int i = 1; i < ops.Count; ++i ) {
					BinOp right = new() {
						op    = ops[i],
						left  = exprs[i],
						right = exprs[i +1],
					};
					left = new() {
						op    = Operand.And,
						left  = left,
						right = right,
					};
				}

				return left;
			}
		}

		public override Expr VisitRelationExpr( RelationExprContext c )
		{
			FlattenRelational flat = new( c );
			Expr              ret  = flat.VisitWithAnd();
			return ret;
		}

		public override Expr VisitEqualityExpr( EqualityExprContext c )
		{
			FlattenRelational flat = new( c );
			Expr              ret  = flat.VisitWithAnd();
			return ret;
		}

		public override Expr VisitAndExpr( AndExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.And,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitOrExpr( OrExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.Or,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitNullCoalesceExpr( NullCoalesceExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.NullCoalesce,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitConditionalExpr( ConditionalExprContext c )
		{
			TernOp ret = new() {
				op    = Operand.Conditional,
				left  = c.expr( 0 ).Visit(),
				mid   = c.expr( 1 ).Visit(),
				right = c.expr( 2 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitParenExpr( ParenExprContext c )
		{
			UnOp ret = new() {
				op   = Operand.Parens,
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitWildIdExpr( WildIdExprContext c )
		{
			Expr          ret;
			WildIdContext cc = c.wildId();
			if( cc.USCORE() != null ) {
				ret = new Expr {
					op = Operand.DiscardId,
				};
			}
			else if( cc.AUTOINDEX() != null ) {
				IdTplArgs idTplArgs = new() {
					id      = cc.AUTOINDEX().GetText(),
					tplArgs = new List<TplArg>( 0 ),
				};
				ret = new IdExpr {
					op        = Operand.WildId,
					idTplArgs = idTplArgs,
				};
			}
			else {
				throw new Exception( "unknown wildId op" );
			}

			return ret;
		}

		public new Literal VisitLit( LitContext c )
		{
			Literal ret = new() {
				op   = Operand.Literal,
				text = c.GetText() // TODO
			};
			return ret;
		}

		// TODO remove this or the other above?
		public override Expr VisitLiteralExpr( LiteralExprContext c )
		{
			Literal ret = new() {
				op   = Operand.Literal,
				text = c.lit().GetText() // TODO
			};
			return ret;
		}

		public override Expr VisitIdTplExpr( IdTplExprContext c )
		{
			Expr ret = new IdExpr {
				op        = Operand.Id,
				idTplArgs = VisitIdTplArgs( c.idTplArgs() ),
			};
			return ret;
		}
	}
}
