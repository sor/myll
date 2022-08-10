#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			ret.srcPos = c.ToSrcPos(); // TODO: change towards a method that applies it to all children
			return ret;
		}

		public Stmt VisitMulti<T>( T[] c )
			where T : ParserRuleContext
			=> c.Length switch {
				0 => throw new InvalidOperationException( "Empty array for VisitMulti" ), //null,
				1 => Visit( c[0] ),
				_ => c.Select( Visit ).ToMulti(),
			};

		public MultiStmt VisitBlockify( StmtContext c )
		{
			Stmt stmt = VisitStmt( c );
			MultiStmt ret = stmt as MultiStmt
			             ?? new List<Stmt> { stmt }.ToBlock();
			Debug.Assert( ret.isScope );
			return ret;
		}

		public override Stmt VisitStmt( StmtContext c )
		{
			Stmt ret = (c.defStmt() != null)
				? Visit( c.defStmt() )
				: c.stmt()
					.Select( Visit )
					.ToBlock();

			Attribs? attribs = c.attribBlk()?.Visit();
			if( attribs != null )
				ret.AssignAttribs( attribs );

			return ret;
		}

		public override MultiStmt VisitFuncBody( FuncBodyContext c )
		{
			// Scope already open?
			MultiStmt    ret;
			StmtContext? lev = c.stmt();
			if( lev != null ) {
				ret = VisitBlockify( lev );
			}
			else if( c.expr() != null ) { // Phatarrow
				ret = new ReturnStmt {    // TODO: return makes no sense for c/dtor
					srcPos = c.ToSrcPos(),
					expr   = c.expr().Visit(),
				}.ToBlock();
			}
			else {
				throw new Exception( "Unknown Func body" );
			}

			return ret;
		}

		public override Stmt? VisitAttrUsing(	AttrUsingContext	c ) => VisitAttrAnyStmt( c.attribBlk(), c.defUsing(), c.attrUsing() );
		public override Stmt? VisitAttrAlias(	AttrAliasContext	c ) => VisitAttrAnyStmt( c.attribBlk(), c.defAlias(), c.attrAlias() );

		// no override
		public Stmt? VisitAttrAnyStmt<TDefContext, TAttrContext>(
			AttribBlkContext? aAttribBlk,
			TDefContext?      cDef,
			TAttrContext[]    cAttr )
			where TDefContext : ParserRuleContext
			where TAttrContext : ParserRuleContext
		{
			Stmt ret = (cDef != null)
				? Visit( cDef )
				: VisitMulti( cAttr );

			Attribs? attribs = aAttribBlk?.Visit();
			if( attribs != null )
				ret.AssignAttribs( attribs );

			return ret;
		}

		// no override
		public MultiStmt VisitAttrVar( AttrVarContext c, VarDecl.Kind kind )
		{
			MultiStmt ret = c.defVar() != null
				? VisitDefVar( c.defVar(), kind )
				: c.attrVar()
					.Select( ac => VisitAttrVar( ac, kind ) )
					.OfType<MultiStmt>()
					.ToMulti();

			Attribs? attribs = c.attribBlk()?.Visit();
			if( attribs != null )
				ret.AssignAttribs( attribs );

			return ret;
		}

		public override MultiStmt VisitDefUsing( DefUsingContext c )
		{
			SrcPos srcPos = c.ToSrcPos();
			MultiStmt ret = VisitTypespecsNested( c.typespecsNested() )
				.Select(
					typespec => new UsingStmt() {
						srcPos = srcPos,
						type   = typespec
					} )
				.ToMulti();
			// TODO: local scope
			//AddChildren( ret.decls );
			return ret;
		}

		public override UsingStmt VisitDefAlias( DefAliasContext c )
		{
			// TODO: tplParams, multi-decl
			List<TplParam> useMe = VisitTplParams( c.tplParams() );

			// TODO: This should not be a UsingStmt
			UsingStmt ret = new() {
				srcPos = c.ToSrcPos(),
				name   = c.id().GetText(),
				type   = VisitTypespec( c.typespec() ),
			};
			// TODO: local scope
			//AddChild( ret );
			return ret;
		}

		// no override
		// list of typed and initialized vars
		public MultiStmt VisitDefVar( DefVarContext c, VarDecl.Kind kind )
		{
			Scope  scope  = scopeStack.Peek();
			SrcPos srcPos = c.ToSrcPos();
			Typespec type = VisitTypespec( c.typedIdAcors().typespec() );
			if( kind.ToQualifier() == Qualifier.Const ) {
				type.qual |= Qualifier.Const;
			}
			List<Stmt> stmts = c.typedIdAcors()
				.idAccessors()
				.idAccessor()
				.Select(
					q => new VarStmt {
						srcPos   = srcPos,
						name     = q.id().GetText(),
						kind     = kind,
						type     = type,
						init     = q.expr()?.Visit(),
					} as Stmt )
				.ToList();
			// TODO local scope
			//AddChildren( stmts );
			MultiStmt ret = stmts.ToMulti();
			return ret;
		}

		public override MultiStmt VisitDeclVar( DeclVarContext c )
		{
			VarDecl.Kind kind = c.kindOfVar().Visit();

			MultiStmt ret;
			if( c.defVar() != null )
				ret = VisitDefVar( c.defVar(), kind );
			else if( c.attrVar() != null )
				ret = c.attrVar()
					.Select( ac => VisitAttrVar( ac, kind ) )
					.OfType<MultiStmt>()
					.ToMulti();
			else
				throw new InvalidOperationException( "no other case than defVar and attrVar" );

			return ret;
		}

		public override EmptyStmt VisitStmtEmpty( StmtEmptyContext c )
			=> new();

		public override ReturnStmt VisitStmtReturn( StmtReturnContext c )
		{
			ReturnStmt ret = new() {
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override ThrowStmt VisitStmtThrow( StmtThrowContext c )
		{
			ThrowStmt ret = new() {
				expr = c.expr().Visit(),
			};
			return ret;
		}

		public override BreakStmt VisitStmtBreak( StmtBreakContext c )
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

		public override IfStmt VisitStmtIf( StmtIfContext c )
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
			bool isScope = c.LCURLY() != null;

			MultiStmt body = new( c.stmt().Select( Visit ), isScope );
			body.srcPos = c.ToSrcPos();

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
		private new MultiStmt? VisitDefaultBlock( DefaultBlockContext? c )
		{
			if( c == null )
				return null;

			bool isScope = c.LCURLY() != null;

			MultiStmt ret = new( c.stmt().Select( Visit ), isScope );
			ret.srcPos = c.ToSrcPos();
			return ret;
		}

		public override SwitchStmt VisitStmtSwitch( StmtSwitchContext c )
		{
			SwitchStmt ret = new() {
				cond  = c.cond.Visit(),
				cases = c.caseBlock().Select( VisitCaseBlock ).ToList(),
				els   = VisitDefaultBlock( c.defaultBlock() ),
			};
			return ret;
		}

		public override LoopStmt VisitStmtLoop( StmtLoopContext c )
		{
			LoopStmt ret = new() {
				body = c.body.Visit(),
			};
			return ret;
		}

		public override ForStmt VisitStmtFor( StmtForContext c )
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

		public override WhileStmt VisitStmtWhile( StmtWhileContext c )
		{
			WhileStmt ret = new() {
				cond = c.cond.Visit(),
				body = c.body.Visit(),
				els  = c.els?.Visit(),
			};
			return ret;
		}

		public override DoWhileStmt VisitStmtDoWhile( StmtDoWhileContext c )
		{
			DoWhileStmt ret = new() {
				cond = c.cond.Visit(),
				body = VisitBlockify( c.body ),
			};
			return ret;
		}

		public override TimesStmt VisitStmtTimes( StmtTimesContext c )
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

		public override AggrAssign VisitStmtAggregate( StmtAggregateContext c )
		{
			AggrAssign ret = new() {
				op        = c.aggrAssignOP().v.ToOp(),
				leftExpr  = c.expr( 0 ).Visit(),
				rightExpr = c.expr( 1 ).Visit(),
			};
			return ret;
		}

		public override MultiAssign VisitStmtAssign( StmtAssignContext c )
		{
			MultiAssign ret = new() {
				exprs = c.expr().Select( q => q.Visit() ).ToList(),
			};
			return ret;
		}

		public override ExprStmt VisitStmtExpr( StmtExprContext c )
		{
			ExprStmt ret = new() {
				expr = c.expr().Visit(),
			};
			return ret;
		}
	}
}
