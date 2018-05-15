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



		public override Stmt VisitReturnStmt( ReturnStmtContext c )
		{
			Stmt ret = new ReturnStmt {
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Stmt VisitThrowStmt( ThrowStmtContext c )
		{
			Stmt ret = new ThrowStmt {
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Stmt VisitBreakStmt( BreakStmtContext c )
		{
			Stmt ret = new BreakStmt {
				depth = int.Parse( c.INTEGER_LIT().ToString() ),
			};
			return ret;
		}

		public override Stmt VisitIfStmt( IfStmtContext c )
		{
			Stmt ret = new IfStmt {
				condExpr = c.expr().Visit(),
				thenStmt = c.stmt( 0 ).Visit(),
				elseStmt = c.stmt( 1 ).Visit(),
			};
			return ret;
		}

		public new CaseStmt VisitCaseStmt( CaseStmtContext c )
		{
			CaseStmt ret = new CaseStmt {
				caseExprs = c.expr().Select( q => q.Visit() ).ToList(),
				bodyStmts = c.stmt().Select( Visit ).ToList(),
				autoBreak = c.FALL() != null,
			};
			return ret;
		}

		public override Stmt VisitSwitchStmt( SwitchStmtContext c )
		{
			Stmt ret = new SwitchStmt {
				condExpr  = c.expr().Visit(),
				caseStmts = c.caseStmt().Select( VisitCaseStmt ).ToList(),
				elseStmts = c.stmt().Select( Visit ).ToList(),
			};
			return ret;
		}

		public override Stmt VisitLoopStmt( LoopStmtContext c )
		{
			LoopStmt ret = new LoopStmt {
				bodyStmt = c.stmt().Visit(),
			};
			return ret;
		}

		public override Stmt VisitForStmt( ForStmtContext c )
		{
			LoopStmt ret = new ForStmt {
				initStmt = Visit( c.stmtDef() ),
				condExpr = c.expr( 0 ).Visit(),
				iterExpr = c.expr( 1 ).Visit(),
				bodyStmt = c.stmt( 0 ).Visit(),
				elseStmt = c.stmt( 1 ).Visit(),
			};
			return ret;
		}

		public override Stmt VisitWhileStmt( WhileStmtContext c )
		{
			LoopStmt ret = new WhileStmt {
				condExpr = c.expr().Visit(),
				bodyStmt = c.stmt( 0 ).Visit(),
				elseStmt = c.stmt( 1 ).Visit(),
			};
			return ret;
		}

		public override Stmt VisitDoWhileStmt( DoWhileStmtContext c )
		{
			LoopStmt ret = new DoWhileStmt {
				condExpr = c.expr().Visit(),
				bodyStmt = c.stmt().Visit(),
			};
			return ret;
		}

		public override Stmt VisitTimesStmt( TimesStmtContext c )
		{
			LoopStmt ret = new TimesStmt {
				countExpr = c.expr().Visit(),
				name      = c.id().GetText(),
				bodyStmt  = c.stmt().Visit(),
			};
			return ret;
		}

		public override Stmt VisitEachStmt( EachStmtContext c )
		{
			LoopStmt ret = new EachStmt {
				fromExpr = c.expr( 0 ).Visit(),
				toExpr   = c.expr( 1 ).Visit(),
				name     = c.id().GetText(),
				bodyStmt = c.stmt().Visit(),
			};
			return ret;
		}

		public override Stmt VisitAssignmentStmt( AssignmentStmtContext c )
		{
			AssignStmt ret;
			if( c.aggrAssignOP() != null ) {
				ret = new AggrAssignStmt {
					op        = c.aggrAssignOP().v.ToAssignOp(),
					leftExpr  = c.expr( 0 ).Visit(),
					rightExpr = c.expr( 1 ).Visit(),
				};
			}
			else if( c.assignOP() != null ) {
				ret = new MultiAssignStmt {
					op    = Operand.Equal,
					exprs = c.expr().Select( q => q.Visit() ).ToList(),
				};
			}
			else throw new Exception( "unknown assign op kind" );
			return ret;
		}

		public override Stmt VisitBlockStmt( BlockStmtContext c )
		{
			Block ret = new Block {
				statements = c.stmt()?.Select( Visit ).ToList()
				             ?? new List<Stmt>()
			};
			return ret;
		}

		public override Stmt VisitExpressionStmt( ExpressionStmtContext c )
		{
			Stmt ret = new ExprStmt {
				expr = c.expr().Visit(),
			};
			return ret;
		}
	}
}
