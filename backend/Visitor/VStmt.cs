using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;
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

		[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		public Stmt Visit( ParserRuleContext c )
		{
			if( c == null )
				throw new ArgumentNullException();

			Stmt ret = base.Visit( c );
			ret.srcPos = c.ToSrcPos();
			return ret;
		}

		public Block? VisitBlockify( StmtContext c )
		{
			//if( c == null )
			//	return null;

			Block? ret;

			DefStmtContext? sc = c.defStmt();
			if( sc != null ) {
				if( sc is BlockStmtContext cb ) {
					ret = VisitBlockStmt( cb );
				}
				else if( sc is EmptyStmtContext ) {
					ret = null;
				}
				else {
					ret = new Block {
						srcPos  = c.ToSrcPos(),
						isScope = true,
						stmts   = new List<Stmt>(),
					};

					if( c.defStmt() is not EmptyStmtContext )
						ret.stmts.Add( Visit( c ) );
				}
			}
			else {
				throw new InvalidOperationException( "Stmt Blockify unhandled case" );
				//c.stmt()
				// BUG: OR c.stmt
			}
			return ret;
		}

		// TODO: to be removed
		public override Block VisitBlockStmt( BlockStmtContext c )
		{
			Block ret = new() {
				isScope = true,
				stmts   = c.stmt()?.Select( Visit ).ToList() ?? new List<Stmt>()
			};
			return ret;
		}

		public override Stmt VisitStmt( StmtContext c )
		{
			Stmt ret;
			if( c.defStmt() != null ) {
				ret = Visit( c.defStmt() );
			}
			else if( c.stmt().Any() ) {
				//ret = VisitMulti( c.stmt() );
				// HACK: not checked
				ret = new Block {
					srcPos  = c.ToSrcPos(),
					isScope = true,
					stmts   = c.stmt().Select( Visit ).ToList(),
				};
			}
			else {
				throw new InvalidOperationException( "Stmt unhandled case" );
			}

			Attribs? attribs = c.attribBlk()?.Visit();
			if( attribs != null )
				ret.AssignAttribs( attribs );

			return ret;
		}

		public override Block VisitFuncBody( FuncBodyContext c )
		{
			// Scope already open?
			Block        ret;
			StmtContext? lev = c.stmt();
			if( lev != null ) {
				ret = VisitBlockify( lev );
			}
			else if( c.expr() != null ) { // Phatarrow
				ret = new Block {
					isScope = true,
					stmts = new() {
						new ReturnStmt {	// TODO: return makes no sense for c/dtor
							srcPos = c.ToSrcPos(),
							expr   = c.expr().Visit(),
						}
					}
				};
			}
			else {
				throw new Exception( "Unknown Func body" );
			}

			return ret;
		}

	//	public override Block VisitStmtUsing( StmtUsingContext c )
		public override Block VisitUsingStmt( UsingStmtContext c )
		{
			Block ret = new() {
				isScope = false,
				stmts   = new(),
			};
			foreach( TypespecNestedContext tc in c.typespecsNested().typespecNested() ) {
				UsingStmt usingStmt = new() {
					srcPos = c.ToSrcPos(),
					type   = VisitTypespecNested( tc ), // TODO: still Nested?
				};
				ret.stmts.Add( usingStmt );
			}
			return ret;
		}

	//	public override UsingStmt VisitStmtAlias( StmtAliasContext c )
		public override UsingStmt VisitAliasStmt( AliasStmtContext c )
		{
			UsingStmt ret = new() {
				srcPos = c.ToSrcPos(),
				name   = c.id().GetText(),
				type   = VisitTypespec( c.typespec() ),
			};
			return ret;
		}

		// no override
		// list of typed and initialized vars
		private List<Stmt> VisitStmtVars( TypedIdAcorsContext c )
		{
			//Scope scope = ScopeStack.Peek();
			// determine if only scope or container
			Typespec type = VisitTypespec( c.typespec() );
			List<Stmt> ret = c
				.idAccessors()
				.idAccessor()
				.Select(
					q => new VarStmt {
						srcPos = c.ToSrcPos(), // needs to stay in here
						name   = q.id().GetText(),
						type   = type,
						init   = q.expr()?.Visit(),
						// no accessors as Stmt
					} as Stmt )
				.ToList();
			// TODO ??? AddChildren( ret );
			return ret;
		}

	//	public override Block VisitStmtVariable( StmtVariableContext c )
		public override Block VisitVariableStmt( VariableStmtContext c )
		{
			Block ret = new() {
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

	//	public override EmptyStmt VisitStmtEmpty( StmtEmptyContext c )
		public override EmptyStmt VisitEmptyStmt( EmptyStmtContext c )
			=> new();

	//	public override ReturnStmt VisitStmtReturn( StmtReturnContext c )
		public override ReturnStmt VisitReturnStmt( ReturnStmtContext c )
		{
			ReturnStmt ret = new() {
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override ThrowStmt VisitStmtThrow( StmtThrowContext c ) => (ThrowStmt)base.VisitStmtThrow( c );
		public override ThrowStmt VisitThrowStmt( ThrowStmtContext c )
		{
			ThrowStmt ret = new() {
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override Stmt VisitStmtBreak( StmtBreakContext c ) => base.VisitStmtBreak( c );
		public override BreakStmt VisitBreakStmt( BreakStmtContext c )
		{
			BreakStmt ret = new() {
				depth = c.INTEGER_LIT()?.ToInt() ?? 1,
			};
			return ret;
		}

		// no override
		[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		public new IfStmt.CondThen VisitCondThen( CondThenContext c )
			=> new() {
				cond = c.expr().Visit(),
				then = c.stmt().Visit(),
			};

		public override IfStmt VisitStmtIf( StmtIfContext c ) => (IfStmt)base.VisitStmtIf( c );
		public override IfStmt VisitIfStmt( IfStmtContext c )
		{
			IfStmt ret = new() {
				ifThens = c.condThen().Select( VisitCondThen ).ToList(),
				els     = c.stmt().Visit(),
			};

			return ret;
		}

		// no override
		private new SwitchStmt.CaseBlock VisitCaseBlock( CaseBlockContext c )
		{
			Block body = new() {
				srcPos  = c.ToSrcPos(), // needs to stay in here
				isScope = c.LCURLY() != null,
				stmts   = c.stmt().Select( Visit ).ToList(),
			};

			bool hasNoStmt = body.stmts.IsEmpty();
			bool isFall    = c.FALL() != null;
			// TODO: check if either break or fall is set, throw if necessary
			// breaks will be inside the body.stmts
			// need info from the switch in here if there is a default case
			if( hasNoStmt ) {
				// OK, consecutive case stmt, silent fallthrough
			}
			else if( isFall ) {
				body.stmts.Add( new FreetextStmt( "[[fallthrough]];" ) );
			}
			else if( body.stmts.Last() is not BreakStmt and not ReturnStmt ) {
				body.stmts.Add( new BreakStmt() );
			}

			SwitchStmt.CaseBlock ret = new() {
				compare = c.expr().Select( q => q.Visit() ).ToList(),
				then    = body,
			};
			return ret;
		}

		// no override
		private new Block? VisitDefaultBlock( DefaultBlockContext? c )
		{
			if( c == null )
				return null;

			Block ret = new() {
				srcPos  = c.ToSrcPos(), // needs to stay in here
				isScope = c.LCURLY() != null,
				stmts   = c.stmt().Select( Visit ).ToList(),
			};
			return ret;
		}

		public override Stmt VisitStmtSwitch( StmtSwitchContext c ) => base.VisitStmtSwitch( c );
		public override SwitchStmt VisitSwitchStmt( SwitchStmtContext c )
		{
			SwitchStmt ret = new() {
				cond  = c.cond.Visit(),
				cases = c.caseBlock().Select( VisitCaseBlock ).ToList(),
				els   = VisitDefaultBlock( c.defaultBlock() ),
			};
			return ret;
		}

		public override Stmt VisitStmtLoop( StmtLoopContext c ) => base.VisitStmtLoop( c );
		public override LoopStmt VisitLoopStmt( LoopStmtContext c )
		{
			LoopStmt ret = new() {
				body = c.body.Visit(),
			};
			return ret;
		}

		public override Stmt VisitStmtFor( StmtForContext c ) => base.VisitStmtFor( c );
		public override ForStmt VisitForStmt( ForStmtContext c )
		{
			ForStmt ret = new() {
				init = c.init.Visit(),
				cond = c.cond?.Visit(),
				iter = c.iter?.Visit(),
				body = VisitBlockify( c.body ),
				els  = c.els?.Visit(),
			};
			return ret;
		}

		public override Stmt VisitStmtWhile( StmtWhileContext c ) => base.VisitStmtWhile( c );
		public override WhileStmt VisitWhileStmt( WhileStmtContext c )
		{
			WhileStmt ret = new() {
				cond = c.cond.Visit(),
				body = c.body.Visit(),
				els  = c.els?.Visit(),
			};
			return ret;
		}

		public override Stmt VisitStmtDoWhile( StmtDoWhileContext c ) => base.VisitStmtDoWhile( c );
		public override DoWhileStmt VisitDoWhileStmt( DoWhileStmtContext c )
		{
			DoWhileStmt ret = new() {
				cond = c.cond.Visit(),
				body = VisitBlockify( c.body ),
			};
			return ret;
		}

		public override Stmt VisitStmtTimes( StmtTimesContext c ) => base.VisitStmtTimes( c );
		public override TimesStmt VisitTimesStmt( TimesStmtContext c )
		{
			TimesStmt ret = new() {
				count = c.count.Visit(),
				name  = c.name?.Visit(),
				body  = VisitBlockify( c.body ),
			};
			// TODO: add "name" to current scope

			// fill ret.offset
			ITerminalNode intLit = c.INTEGER_LIT();
			if( intLit != null ) {
				_ = long.TryParse( intLit.GetText(), out ret.offset );
				if( c.MINUS() != null )
					ret.offset *= -1;
			}
			return ret;
		}

		public override Stmt VisitStmtAggregate( StmtAggregateContext c ) => base.VisitStmtAggregate( c );
		public override AggrAssign VisitAggrAssignStmt( AggrAssignStmtContext c )
		{
			AggrAssign ret = new() {
				op        = c.aggrAssignOP().v.ToOp(),
				leftExpr  = c.expr( 0 ).Visit(),
				rightExpr = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override Stmt VisitStmtAssign( StmtAssignContext c ) => base.VisitStmtAssign( c );
		public override MultiAssign VisitMultiAssignStmt( MultiAssignStmtContext c )
		{
			MultiAssign ret = new() {
				exprs = c.expr().Select( q => q.Visit() ).ToList(),
			};
			return ret;
		}

		public override Stmt VisitStmtExpr( StmtExprContext c ) => base.VisitStmtExpr( c );
		public override ExprStmt VisitExpressionStmt( ExpressionStmtContext c )
		{
			ExprStmt ret = new() {
				expr = c.expr().Visit(),
			};
			return ret;
		}
	}
}
