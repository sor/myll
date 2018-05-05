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
	public class StmtVisitor : MyllParserBaseVisitor<Stmt>
	{
		public override Stmt Visit(IParseTree c)
		{
			if (c == null)
				return null;

			return base.Visit(c);
		}

		public override Stmt VisitIfStmt(IfStmtContext c)
		{
			IfStmt ret = new IfStmt
			{
				ifExpr    = c.expr().Visit(),
				thenBlock = c.stmt(0).Visit(),
				elseBlock = c.stmt(1).Visit(),
			};
			return ret;
		}

		public override Stmt VisitFallStmt(FallStmtContext c)
		{
			return new FallStmt();
		}

		public override Stmt VisitStmtBlk(StmtBlkContext c)
		{
			Block ret = new Block
			{
				//a?.b ?? c;
				//(a ? a.b : nullptr) ? (a ? a.b : nullptr) : c; 
				statements = c?.stmt().Select(Visit).ToList()
				             ?? new List<Stmt>()
			};
			return ret;
		}
	}
}