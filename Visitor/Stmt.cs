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
	public partial class MyllVisitor : MyllParserBaseVisitor<object>
	{
		public Stmt VisitStmt(StmtContext c)
		{
			Visit(c);
			// TODO
			return new Stmt();
		}
		
		public new List<Stmt> VisitStmtBlk(StmtBlkContext c)
		{
			if (c == null)
				return null;
			
			List<Stmt> ret = c.stmt().Select(VisitStmt).ToList();
			return ret;
		}
	}
}