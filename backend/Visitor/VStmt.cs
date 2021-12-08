using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Antlr4.Runtime.Tree;
using Myll.Core;

namespace Myll
{
	using static MyllParser;

	using Attribs = Dictionary<string, List<string>>;

	/**
	 * Only Visit can receive null and will return null, the
	 * other Visit... methods do not support null parameters
	 */
	public class StmtVisitor
		: ExtendedVisitor<Stmt>
	{
		public StmtVisitor( Stack<Scope> scopeStack ) : base( scopeStack ) {}

		public override Stmt Visit( IParseTree c )
			=> c == null
				? null
				: base.Visit( c );

		public override Stmt VisitAttribStmt( AttribStmtContext c )
		{
			Stmt ret = Visit( c.inStmt() );

			Attribs attribs = c.attribBlk()?.Visit();
			if( attribs != null )
				ret.AssignAttribs( attribs );

			return ret;
		}

		public override Stmt VisitFuncBody( FuncBodyContext c )
		{
			// Scope already open?
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

		public override Stmt VisitUsingStmt( UsingStmtContext c )
		{
			MultiStmt ret = new();
			foreach( TypespecNestedContext tc in c.typespecsNested().typespecNested() ) {
				UsingStmt usingStmt = new() {
					srcPos = c.ToSrcPos(),
					type   = VisitTypespecNested( tc ), // TODO: still Nested?
				};
				ret.stmts.Add( usingStmt );
			}
			return ret;
		}

		public override Stmt VisitAliasStmt( AliasStmtContext c )
		{
			UsingStmt ret = new() {
				srcPos = c.ToSrcPos(),
				name   = c.id().GetText(),
				type   = VisitTypespec( c.typespec() ),
			};
			return ret;
		}

		// list of typed and initialized vars
		public List<Stmt> VisitStmtVars( TypedIdAcorsContext c )
		{
			//Scope scope = ScopeStack.Peek();
			// determine if only scope or container
			Typespec type = VisitTypespec( c.typespec() );
			List<Stmt> ret = c
				.idAccessors()
				.idAccessor()
				.Select(
					q => new VarStmt {
						srcPos   = c.ToSrcPos(),
						name     = q.id().GetText(),
						type     = type,
						init     = q.expr().Visit(),
						// no accessors as Stmt
					} as Stmt )
				.ToList();
			// TODO ??? AddChildren( ret );
			return ret;
		}

		public override Stmt VisitVariableStmt( VariableStmtContext c )
		{
			MultiStmt ret = new() {
				stmts = c
					.typedIdAcors()
					.SelectMany( VisitStmtVars )
					.ToList(),
			};
			if( c.v.ToQualifier() == Qualifier.Const ) {
				ret.stmts.ForEach( decl => ((VarStmt) decl).type.qual |= Qualifier.Const );
			}
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
			ReturnStmt ret = new() {
				srcPos = c.ToSrcPos(),
				expr   = c.expr().Visit(),
			};
			return ret;
		}

		public override Stmt VisitThrowStmt( ThrowStmtContext c )
		{
			ThrowStmt ret = new() {
				srcPos = c.ToSrcPos(),
				expr   = c.expr().Visit(),
			};
			return ret;
		}

		public override Stmt VisitBreakStmt( BreakStmtContext c )
		{
			BreakStmt ret = new() {
				srcPos = c.ToSrcPos(),
				depth  = c.INTEGER_LIT().ToInt( 1 ),
			};
			return ret;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public new CondThen VisitCondThen( CondThenContext c )
			=> new() {
				condExpr = c.expr().Visit(),
				thenStmt = c.levStmt().Visit(),
			};

		public override Stmt VisitIfStmt( IfStmtContext c )
		{
			IfStmt ret = new() {
				srcPos   = c.ToSrcPos(),
				ifThens  = c.condThen().Select( VisitCondThen ).ToList(),
				elseStmt = c.levStmt().Visit(),
			};

			return ret;
		}

		public new CaseStmt VisitCaseStmt( CaseStmtContext c )
		{
			MultiStmt multiStmt = new() {
				srcPos = c.ToSrcPos(),
				stmts  = c.levStmt().Select( Visit ).ToList(),
			};

			bool isFall      = c.FALL()       != null;
			bool isPhatArrow = c.PHATRARROW() != null; // "=>" always breaks
			// TODO: check if either break or fall is set, throw if necessary
			// breaks will be inside the multiStmt.stmts
			// need info from the switch in here if there is a default case
			if( !isFall || isPhatArrow )
				multiStmt.stmts.Add( new BreakStmt() );
			else
				multiStmt.stmts.Add( new FreetextStmt( "[[fallthrough]];" ) );

			CaseStmt ret = new() {
				caseExprs = c.expr().Select( q => q.Visit() ).ToList(),
				bodyStmt  = multiStmt,
			};
			return ret;
		}

		public new Stmt VisitDefaultStmt( DefaultStmtContext c )
		{
			if( c == null )
				return null;

			MultiStmt ret = new() {
				srcPos = c.ToSrcPos(),
				stmts  = c.levStmt().Select( Visit ).ToList(),
			};
			return ret;
		}

		public override Stmt VisitSwitchStmt( SwitchStmtContext c )
		{
			SwitchStmt ret = new() {
				srcPos    = c.ToSrcPos(),
				condExpr  = c.expr().Visit(),
				caseStmts = c.caseStmt().Select( VisitCaseStmt ).ToList(),
				elseStmt  = VisitDefaultStmt( c.defaultStmt() ),
			};
			return ret;
		}

		public override Stmt VisitLoopStmt( LoopStmtContext c )
		{
			LoopStmt ret = new() {
				srcPos   = c.ToSrcPos(),
				bodyStmt = c.levStmt().Visit(),
			};
			return ret;
		}

		public override Stmt VisitForStmt( ForStmtContext c )
		{
			ForStmt ret = new() {
				srcPos   = c.ToSrcPos(),
				initStmt = c.levStmt(0).Visit(),
				condExpr = c.expr( 0 ).Visit(),
				iterExpr = c.expr( 1 ).Visit(),
				bodyStmt = c.levStmt( 1 ).Visit(),
				elseStmt = c.levStmt( 2 ).Visit(),
			};
			return ret;
		}

		public override Stmt VisitWhileStmt( WhileStmtContext c )
		{
			CondThen condThen = VisitCondThen( c.condThen() );
			WhileStmt ret = new() {
				srcPos   = c.ToSrcPos(),
				condExpr = condThen.condExpr,
				bodyStmt = condThen.thenStmt,
				elseStmt = c.levStmt().Visit(),
			};
			return ret;
		}

		public override Stmt VisitDoWhileStmt( DoWhileStmtContext c )
		{
			DoWhileStmt ret = new() {
				srcPos   = c.ToSrcPos(),
				condExpr = c.expr().Visit(),
				bodyStmt = c.levStmt().Visit(),
			};
			return ret;
		}

		public override Stmt VisitTimesStmt( TimesStmtContext c )
		{
			TimesStmt ret = new() {
				srcPos    = c.ToSrcPos(),
				countExpr = c.expr().Visit(),
				name      = c.id().Visit(), // TODO: check for null
				bodyStmt  = c.levStmt().Visit(),
			};
			// TODO: add name to current scope
			return ret;
		}

		public override Stmt VisitAggrAssignStmt( AggrAssignStmtContext c )
		{
			AggrAssign ret = new() {
				srcPos    = c.ToSrcPos(),
				op        = c.aggrAssignOP().v.ToAssignOp(),
				leftExpr  = c.expr( 0 ).Visit(),
				rightExpr = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Stmt VisitMultiAssignStmt( MultiAssignStmtContext c )
		{
			MultiAssign ret = new() {
				srcPos = c.ToSrcPos(),
				exprs  = c.expr().Select( q => q.Visit() ).ToList(),
			};
			return ret;
		}

		public override Stmt VisitBlockStmt( BlockStmtContext c )
		{
			Block ret = new() {
				srcPos = c.ToSrcPos(),
				stmts  = c.levStmt()?.Select( Visit ).ToList()
				      ?? new List<Stmt>()
			};
			return ret;
		}

		public override Stmt VisitExpressionStmt( ExpressionStmtContext c )
		{
			ExprStmt ret = new() {
				srcPos = c.ToSrcPos(),
				expr   = c.expr().Visit(),
			};
			return ret;
		}
	}
}
