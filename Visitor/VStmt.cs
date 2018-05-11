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
	/**
	 * Only Visit can receive null and will return null, the
	 * other Visit... methods do not support null parameters
	 */
	public class StmtVisitor : MyllParserBaseVisitor<Stmt>
	{
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override Stmt Visit( IParseTree c )
			=> c == null
				? null
				: base.Visit( c );

		public override Stmt VisitIfStmt( IfStmtContext c )
		{
			Stmt ret = new IfStmt {
				ifExpr   = c.expr().Visit(),
				thenStmt = c.stmt( 0 ).Visit(),
				elseStmt = c.stmt( 1 ).Visit(),
			};
			return ret;
		}

		public override Stmt VisitFallStmt( FallStmtContext c )
		{
			return new FallStmt();
		}

		public override Stmt VisitBlockStmt( BlockStmtContext c )
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
