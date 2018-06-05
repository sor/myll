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
	public class StmtVisitor
		: ExtendedVisitor<Stmt>
	{
		public override Stmt Visit( IParseTree c )
			=> c == null
				? null
				: base.Visit( c );

		public override Stmt VisitFuncBody( FuncBodyContext c )
		{
			Stmt ret;
			if( c.levStmt() != null ) {
				ret = c.levStmt().Visit();
			}
			else if( c.expr() != null ) {
				ret = new ReturnStmt {
					srcPos = c.ToSrcPos(),
					expr   = c.expr().Visit(),
				};
			}
			else throw new Exception( "unknown function decl body" );
			return ret;
		}

		public override Stmt VisitUsing( UsingContext c )
		{
			Stmt ret = new UsingStmt {
				srcPos = c.ToSrcPos(),
				types  = VisitTypespecsNested( c.typespecsNested() ),
			};
			return ret;
		}

		public override Stmt VisitVariableDecl( VariableDeclContext c )
		{
			Stmt ret = new VarsStmt {
				vars = c.typedIdAcors()
					.Select( VisitTypedIdAcors )
					.SelectMany( q => q )
					.ToList(),
			};
			// TODO save the constness
			return ret;
		}

		public override Stmt VisitEmptyStmt( EmptyStmtContext c )
		{
			return new EmptyStmt {
				srcPos = c.ToSrcPos(),
			};
		}

		public override Stmt VisitReturnStmt( ReturnStmtContext c )
		{
			Stmt ret = new ReturnStmt {
				srcPos = c.ToSrcPos(),
				expr   = c.expr().Visit(),
			};
			return ret;
		}

		public override Stmt VisitThrowStmt( ThrowStmtContext c )
		{
			Stmt ret = new ThrowStmt {
				srcPos = c.ToSrcPos(),
				expr   = c.expr().Visit(),
			};
			return ret;
		}

		public override Stmt VisitBreakStmt( BreakStmtContext c )
		{
			Stmt ret = new BreakStmt {
				srcPos = c.ToSrcPos(),
				depth  = int.Parse( c.INTEGER_LIT().ToString() ),
			};
			return ret;
		}

		public override Stmt VisitIfStmt( IfStmtContext c )
		{
			Stmt ret = new IfStmt {
				srcPos   = c.ToSrcPos(),
				condExpr = c.expr().Visit(),
				thenStmt = c.levStmt( 0 ).Visit(),
				elseStmt = c.levStmt( 1 ).Visit(),
			};
			return ret;
		}

		public new CaseStmt VisitCaseStmt( CaseStmtContext c )
		{
			CaseStmt ret = new CaseStmt {
				srcPos    = c.ToSrcPos(),
				caseExprs = c.expr().Select( q => q.Visit() ).ToList(),
				bodyStmts = c.levStmt().Select( Visit ).ToList(),
				autoBreak = c.FALL() != null,
			};
			return ret;
		}

		public override Stmt VisitSwitchStmt( SwitchStmtContext c )
		{
			Stmt ret = new SwitchStmt {
				srcPos    = c.ToSrcPos(),
				condExpr  = c.expr().Visit(),
				caseStmts = c.caseStmt().Select( VisitCaseStmt ).ToList(),
				elseStmts = c.levStmt().Select( Visit ).ToList(),
			};
			return ret;
		}

		public override Stmt VisitLoopStmt( LoopStmtContext c )
		{
			LoopStmt ret = new LoopStmt {
				srcPos   = c.ToSrcPos(),
				bodyStmt = c.levStmt().Visit(),
			};
			return ret;
		}

		public override Stmt VisitForStmt( ForStmtContext c )
		{
			LoopStmt ret = new ForStmt {
				srcPos   = c.ToSrcPos(),
				initStmt = Visit( c.levStmtDef() ),
				condExpr = c.expr( 0 ).Visit(),
				iterExpr = c.expr( 1 ).Visit(),
				bodyStmt = c.levStmt( 0 ).Visit(),
				elseStmt = c.levStmt( 1 ).Visit(),
			};
			return ret;
		}

		public override Stmt VisitWhileStmt( WhileStmtContext c )
		{
			LoopStmt ret = new WhileStmt {
				srcPos   = c.ToSrcPos(),
				condExpr = c.expr().Visit(),
				bodyStmt = c.levStmt( 0 ).Visit(),
				elseStmt = c.levStmt( 1 ).Visit(),
			};
			return ret;
		}

		public override Stmt VisitDoWhileStmt( DoWhileStmtContext c )
		{
			LoopStmt ret = new DoWhileStmt {
				srcPos   = c.ToSrcPos(),
				condExpr = c.expr().Visit(),
				bodyStmt = c.levStmt().Visit(),
			};
			return ret;
		}

		public override Stmt VisitTimesStmt( TimesStmtContext c )
		{
			LoopStmt ret = new TimesStmt {
				srcPos    = c.ToSrcPos(),
				countExpr = c.expr().Visit(),
				name      = c.id().GetText(), // TODO: check for null
				bodyStmt  = c.levStmt().Visit(),
			};
			// TODO: add name to current scope
			return ret;
		}

		public override Stmt VisitEachStmt( EachStmtContext c )
		{
			LoopStmt ret = new EachStmt {
				srcPos   = c.ToSrcPos(),
				fromExpr = c.expr( 0 ).Visit(),
				toExpr   = c.expr( 1 ).Visit(),
				name     = c.id().GetText(), // TODO: check for null
				bodyStmt = c.levStmt().Visit(),
			};
			// TODO: add name to current scope
			return ret;
		}

		public override Stmt VisitAggrAssignStmt( AggrAssignStmtContext c )
		{
			AssignStmt ret = new AggrAssignStmt {
				srcPos    = c.ToSrcPos(),
				op        = c.aggrAssignOP().v.ToAssignOp(),
				leftExpr  = c.expr( 0 ).Visit(),
				rightExpr = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Stmt VisitMultiAssignStmt( MultiAssignStmtContext c )
		{
			AssignStmt ret = new MultiAssignStmt {
				srcPos = c.ToSrcPos(),
				op     = Operand.Equal,
				exprs  = c.expr().Select( q => q.Visit() ).ToList(),
			};
			return ret;
		}

		public override Stmt VisitBlockStmt( BlockStmtContext c )
		{
			Block ret = new Block {
				srcPos = c.ToSrcPos(),
				statements = c.levStmt()?.Select( Visit ).ToList()
				             ?? new List<Stmt>()
			};
			return ret;
		}

		public override Stmt VisitExpressionStmt( ExpressionStmtContext c )
		{
			Stmt ret = new ExprStmt {
				srcPos = c.ToSrcPos(),
				expr   = c.expr().Visit(),
			};
			return ret;
		}
	}
}
