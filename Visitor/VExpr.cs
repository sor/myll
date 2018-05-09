using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Myll.Core;

using Array = Myll.Core.Array;
using Enum = Myll.Core.Enum;

using static Myll.MyllParser;

namespace Myll
{
	public class ExprVisitor : MyllParserBaseVisitor<Expr>
	{
		public override Expr Visit(IParseTree c)
		{
			if (c == null)
				return null;
			
			return base.Visit(c);
		}

		public override Expr VisitScopedExpr(ScopedExprContext c)
		{
			ScopedExpr ret = new ScopedExpr {
				ids  = c.idTplArgs().Select(AllVis.VisitIdTplArgs).ToList(),
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public new FuncCallExpr.Arg VisitArg(ArgContext c)
		{
			FuncCallExpr.Arg ret = new FuncCallExpr.Arg {
				name = c.id().GetText(),
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitPostExpr(PostExprContext c)
		{
			Expr ret;
			Expr left = c.expr().Visit();
			if (c.postOP() != null)
				ret = new UnOp {
					expr = left,
					op   = c.postOP().v.ToOp(),
				};
			else if (c.funcCall() != null) {
				ret = new FuncCallExpr {
					op   = c.funcCall().ary.ToOp(),
					left = left,
					args = c.funcCall().arg().Select(VisitArg).ToList(),
				};
			}
			else if (c.indexCall() != null) {
				ret = new FuncCallExpr {
					op   = c.indexCall().ary.ToOp(),
					left = left,
					args = c.indexCall().arg().Select(VisitArg).ToList(),
				};
			}
			else if (c.memAccOP() != null) {
				IdTplExpr right = new IdTplExpr {
					id = AllVis.VisitIdTplArgs(c.idTplArgs()),
				};
				ret = new BinOp {
					op    = c.memAccOP().v.ToOp(),
					left  = left,
					right = right,
				};
			}
			else
				throw new Exception("unknown pre op");

			return ret;
		}

		public override Expr VisitPreOpExpr(PreOpExprContext c)
		{
			Expr ret = new UnOp {
				op   = c.preOP().v.PreToOp(),
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitCastExpr(CastExprContext c)
		{
			Expr ret = new CastExpr {
				op   = Operand.StaticCast,
				type = AllVis.VisitTypeSpec(c.typeSpec()),
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitSizeofExpr(SizeofExprContext c)
		{
			Expr ret = new UnOp {
				op   = Operand.SizeOf,
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitNewExpr(NewExprContext c)
		{
			Expr ret = new NewExpr {
				op   = Operand.New,
				type = AllVis.VisitTypeSpec(c.typeSpec()),
				args = c.funcCall().arg().Select(VisitArg).ToList(),
			};
			return ret;
		}

		public override Expr VisitDeleteExpr(DeleteExprContext c)
		{
			Expr ret = new UnOp {
				op   = c.ary != null ? Operand.DeleteAry : Operand.Delete,
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitPreExpr(PreExprContext c)
		{
			//Visit(a??b??c)
			Expr ret;
			if (c.preOpExpr() != null)
				ret = VisitPreOpExpr(c.preOpExpr());
			else if (c.castExpr() != null)
				ret = VisitCastExpr(c.castExpr());
			else if (c.sizeofExpr() != null)
				ret = VisitSizeofExpr(c.sizeofExpr());
			else if (c.newExpr() != null)
				ret = VisitNewExpr(c.newExpr());
			else if (c.deleteExpr() != null)
				ret = VisitDeleteExpr(c.deleteExpr());
			else
				throw new Exception("unknown pre op");
			
			return ret;
		}

		public override Expr VisitMemPtrExpr(MemPtrExprContext c)
		{
			BinOp ret = new BinOp {
				op    = c.memAccPtrOP().v.ToOp(),
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitPowExpr(PowExprContext c)
		{
			BinOp ret = new BinOp {
				op    = Operand.Pow,
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitMultExpr(MultExprContext c)
		{
			BinOp ret = new BinOp {
				op    = c.multOP().v.ToOp(),
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitAddExpr(AddExprContext c)
		{
			BinOp ret = new BinOp {
				op    = c.addOP().v.ToOp(),
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitShiftExpr(ShiftExprContext c)
		{
			BinOp ret = new BinOp {
				op    = Operand.LeftShift,	// TODO
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitComparisonExpr(ComparisonExprContext c)
		{
			BinOp ret = new BinOp {
				op    = Operand.Comparison,
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitRelationExpr(RelationExprContext c)
		{
			BinOp ret = new BinOp {
				op    = c.orderOP().v.ToOp(),
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitEqualityExpr(EqualityExprContext c)
		{
			BinOp ret = new BinOp {
				op    = c.equalOP().v.ToOp(),
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitAndExpr(AndExprContext c)
		{
			BinOp ret = new BinOp {
				op    = Operand.And,
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitOrExpr(OrExprContext c)
		{
			BinOp ret = new BinOp {
				op    = Operand.Or,
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitNullCoalesceExpr(NullCoalesceExprContext c)
		{
			BinOp ret = new BinOp {
				op    = Operand.NullCoalesce,
				left  = c.expr(0).Visit(),
				right = c.expr(1).Visit(),
			};
			return ret;
		}

		public override Expr VisitConditionalExpr(ConditionalExprContext c)
		{
			TernOp ret = new TernOp {
				op       = Operand.Conditional,
				ifExpr   = c.expr(0).Visit(),
				thenExpr = c.expr(1).Visit(),
				elseExpr = c.expr(2).Visit(),
			};
			return ret;
		}

		public override Expr VisitParenExpr(ParenExprContext c)
		{
			UnOp ret = new UnOp {
				op   = Operand.Parens,
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Expr VisitLiteralExpr(LiteralExprContext c)
		{
			Literal ret = new Literal
			{
				text = c.GetText() // TODO
			};
			return ret;
		}
	}
}