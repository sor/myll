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

		public override Expr VisitAddExpr(AddExprContext c)
		{
			BinOp ret = new BinOp
			{
				op    = c.addOP().v.ToOp(),
				left  = Visit(c.expr(0)),
				right = Visit(c.expr(1)),
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