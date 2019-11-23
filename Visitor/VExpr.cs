using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Antlr4.Runtime.Tree;
using Myll.Core;

using static Myll.MyllParser;

namespace Myll
{
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{
		public new List<Func.Arg> VisitArgs( ArgsContext c )
		{
			List<Func.Arg> ret = c?.arg().Select( VisitArg ).ToList()
			                     ?? new List<Func.Arg>();
			return ret;
		}

		public new Func.Call VisitIndexCall( IndexCallContext c )
		{
			Func.Call ret = new Func.Call {
				args     = VisitArgs( c.args() ),
				nullCoal = c.ary.Type == QM_LBRACK,
			};
			return ret;
		}

		public new Func.Call VisitFuncCall( FuncCallContext c )
		{
			Func.Call ret = new Func.Call {
				args     = VisitArgs( c?.args() ),
				nullCoal = c?.ary.Type == QM_LPAREN,
			};
			return ret;
		}

		public new Func.Arg VisitArg( ArgContext c )
		{
			Func.Arg ret = new Func.Arg {
				name = VisitId( c.id() ),
				expr = c.expr().Visit(),
			};
			return ret;
		}
	}

	/**
	 * Only Visit can receive null and will return null, the
	 * other Visit... methods do not support null parameters
	 */
	public class ExprVisitor
		: ExtendedVisitor<Expr>
	{
		public ExprVisitor( Stack<Scope> ScopeStack ) : base( ScopeStack ) {}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override Expr Visit( IParseTree c )
			=> c == null
				? null
				: base.Visit( c );

		public override Expr VisitScopedExpr( ScopedExprContext c )
		{
			ScopedExpr ret = new ScopedExpr {
				op   = Operand.Scoped,
				ids  = c.idTplArgs().Select( VisitIdTplArgs ).ToList(),
				expr = c.expr().Visit(),
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
					left     = left,
					funcCall = VisitFuncCall( c.funcCall() ),
				};
			}
			else if( c.indexCall() != null ) {
				ret = new FuncCallExpr {
					op       = c.indexCall().ary.ToOp(),
					left     = left,
					funcCall = VisitIndexCall( c.indexCall() ),
				};
			}
			else if( c.memAccOP() != null ) {
				IdExpr right = new IdExpr {
					op = Operand.Id,
					id = VisitIdTplArgs( c.idTplArgs() ),
				};
				ret = new BinOp {
					op    = c.memAccOP().v.ToOp(),
					left  = left,
					right = right,
				};
			}
			else
				throw new Exception( "unknown post op" );

			return ret;
		}

		public override Expr VisitPreOpExpr( PreOpExprContext c )
		{
			Expr ret = new UnOp {
				op   = c.preOP().v.ToPreOp(),
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitCastExpr( CastExprContext c )
		{
			Operand op =
				c.QM() != null ? Operand.DynamicCast :
				c.EM() != null ? Operand.AnyCast :
				                 Operand.StaticCast;
			Expr ret = new CastExpr {
				op   = op,
				type = VisitTypespec( c.typespec() ),
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitSizeofExpr( SizeofExprContext c )
		{
			Expr ret = new UnOp {
				op   = Operand.SizeOf,
				expr = c.expr().Visit(),
			};
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

		public override Expr VisitDeleteExpr( DeleteExprContext c )
		{
			Expr ret = new UnOp {
				op   = c.ary != null ? Operand.DeleteAry : Operand.Delete,
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitPreExpr( PreExprContext c )
		{
			IParseTree cc = c.preOpExpr()
			                ?? c.castExpr()
			                ?? c.sizeofExpr()
			                ?? c.newExpr()
			                ?? c.deleteExpr() as IParseTree;

			if( cc == null )
				throw new Exception( "unknown pre op" );

			Expr ret = Visit( cc );

			return ret;
		}

		// TODO: check if this really works
		public override Expr VisitMemPtrExpr( MemPtrExprContext c )
		{
			BinOp ret = new BinOp {
				op    = c.memAccPtrOP().v.ToOp(),
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitPowExpr( PowExprContext c )
		{
			BinOp ret = new BinOp {
				op    = Operand.Pow,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitMultExpr( MultExprContext c )
		{
			BinOp ret = new BinOp {
				op    = c.multOP().v.ToOp(),
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitAddExpr( AddExprContext c )
		{
			BinOp ret = new BinOp {
				op    = c.addOP().v.ToOp(),
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitShiftExpr( ShiftExprContext c )
		{
			BinOp ret = new BinOp {
				op    = c.shiftOP().LSHIFT() != null ? Operand.LeftShift : Operand.RightShift,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitComparisonExpr( ComparisonExprContext c )
		{
			BinOp ret = new BinOp {
				op    = Operand.Comparison,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitRelationExpr( RelationExprContext c )
		{
			BinOp ret = new BinOp {
				op    = c.orderOP().v.ToOp(),
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitEqualityExpr( EqualityExprContext c )
		{
			BinOp ret = new BinOp {
				op    = c.equalOP().v.ToOp(),
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitAndExpr( AndExprContext c )
		{
			BinOp ret = new BinOp {
				op    = Operand.And,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitOrExpr( OrExprContext c )
		{
			BinOp ret = new BinOp {
				op    = Operand.Or,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitNullCoalesceExpr( NullCoalesceExprContext c )
		{
			BinOp ret = new BinOp {
				op    = Operand.NullCoalesce,
				left  = c.expr( 0 ).Visit(),
				right = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitConditionalExpr( ConditionalExprContext c )
		{
			TernOp ret = new TernOp {
				op       = Operand.Conditional,
				ifExpr   = c.expr( 0 ).Visit(),
				thenExpr = c.expr( 1 ).Visit(),
				elseExpr = c.expr( 2 ).Visit(),
			};
			return ret;
		}

		public override Expr VisitParenExpr( ParenExprContext c )
		{
			UnOp ret = new UnOp {
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
				IdentifierTpl id = new IdentifierTpl {
					name         = cc.AUTOINDEX().GetText(),
					templateArgs = new List<TemplateArg>( 0 ),
				};
				ret = new IdExpr {
					op = Operand.WildId,
					id = id,
				};
			}
			else
				throw new Exception( "unknown wildId op" );

			return ret;
		}

		public override Expr VisitLiteralExpr( LiteralExprContext c )
		{
			Literal ret = new Literal {
				op   = Operand.Literal,
				text = c.lit().GetText() // TODO
			};
			return ret;
		}

		public override Expr VisitIdTplExpr( IdTplExprContext c )
		{
			Expr ret = new IdExpr {
				op = Operand.Id,
				id = VisitIdTplArgs( c.idTplArgs() ),
			};
			return ret;
		}
	}
}
