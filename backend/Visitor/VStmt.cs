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

		public StmtVisitor( CompilationContext context ) : base( context ) {}

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
			Stmt ret;
			if( c.defStmt() != null ) {
				ret = Visit( c.defStmt() );
			}
			else {
				PushScope();
				ret = c.stmt()
					.Select( Visit )
					.ToBlock();
				PopScope();
			}

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
					expr   = c.expr().Visit( Context ),
				}.ToBlock();
			}
			else {
				throw new Exception( "Unknown Func body" );
			}

			return ret;
		}

		public override Stmt VisitAttrUsing(	AttrUsingContext	c ) => VisitAttrAnyStmt( c.attribBlk(), c.defUsing(), c.attrUsing() )!;
		public override Stmt VisitAttrAlias(	AttrAliasContext	c ) => VisitAttrAnyStmt( c.attribBlk(), c.defAlias(), c.attrAlias() )!;

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

		public override AliasStmt VisitDefAlias( DefAliasContext c )
		{
			// TODO: tplParams, multi-decl
			List<TplParam> useMe = VisitTplParams( c.tplParams() );

			// Local aliases are emitted as C++ type aliases.
			// Namespace aliases at function scope are not supported yet.
			AliasStmt ret = new() {
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
					q => {
						Expr? init              = q.funcCall() != null
							? new FuncCallExpr {
								srcPos     = srcPos,
								expr       = TypespecToExpr( type, srcPos ),
								funcCall   = VisitFuncCall( q.funcCall() ),
							}
							: q.expr()?.Visit( Context );
						bool  isDirectConstruct = q.funcCall() != null;
						Expr? finalInit         = TransformInit( init, kind );
						isDirectConstruct      &= finalInit != null;
						VarStmt stmt = new() {
							srcPos             = srcPos,
							name               = q.id().GetText(),
							kind               = kind,
							type               = type,
							init               = finalInit,
							isDirectConstruct  = isDirectConstruct,
						};
						if( init is Discard && finalInit == null )
							stmt.AssignAttribs( new Attribs { ["noinit"] = new List<string>() } );
						return stmt as Stmt;
					} )
				.ToList();

			foreach( VarStmt stmt in stmts.OfType<VarStmt>() ) {
				AddScopeOnly( new VarDecl {
					name   = stmt.name,
					type   = stmt.type,
					kind   = stmt.kind,
					access = Access.Public,
				} );
			}

			MultiStmt ret = stmts.ToMulti();
			return ret;
		}

		// no init
		private static Expr? TransformInit( Expr? init, VarDecl.Kind kind )
		{
			if( init == null )
				return null;

			if( init is not Discard )
				return init;

			// var/let with init "_" means "uninitialized".
			if( kind is VarDecl.Kind.Var or VarDecl.Kind.Let )
				return null;

			// const/field with init "_" is not supported.
			throw new NotSupportedException(
				"'_' initializer is not allowed for " + kind.ToString().ToLowerInvariant() + " declarations" );
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
				expr = c.expr()?.Visit( Context ),
			};
			return ret;
		}

		public override ThrowStmt VisitStmtThrow( StmtThrowContext c )
		{
			ThrowStmt ret = new() {
				expr = c.expr().Visit( Context ),
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

		public override ContinueStmt VisitStmtContinue( StmtContinueContext c )
		{
			ContinueStmt ret = new() {
				depth = c.INTEGER_LIT()?.ToInt() ?? 1,
			};
			return ret;
		}

		public override Stmt VisitStmtContinue2( StmtContinue2Context c )
			=> throw new NotImplementedException(
				"continue case/default/else is not implemented yet" );

		public override Stmt VisitStmtDefer( StmtDeferContext c )
			=> throw new NotImplementedException( "defer is not implemented yet" );

		public override Stmt VisitStmtReturnIf( StmtReturnIfContext c )
		{
			ExprContext[] exprs = c.expr();

			// `do return <expr>? if( <cond> );` — the first expr is optional, the second is
			// the condition. If only one expr is present, it is the condition.
			Expr? returnExpr = exprs.Length > 1 ? exprs[0].Visit( Context ) : null;
			Expr  condition  = exprs.Length > 1 ? exprs[1].Visit( Context ) : exprs[0].Visit( Context );

			return new IfStmt {
				srcPos   = c.ToSrcPos(),
				ifThens  = new() {
					new IfStmt.CondThen(
						condition,
						new ReturnStmt {
							srcPos = returnExpr?.srcPos ?? c.ToSrcPos(),
							expr   = returnExpr,
						} ),
				},
			};
		}

		public override TryCatchStmt VisitStmtTryCatch( StmtTryCatchContext c )
		{
			TryCatchStmt ret = new() {
				tryBody = Visit( c.stmt() ),
			};

			foreach( CatchClauseContext cc in c.catchClause() ) {
				PushScope();

				CatchClause clause = new();
				if( cc.funcTypeDef() != null ) {
					List<Param> paras = VisitFuncTypeDef( cc.funcTypeDef() ).ToList();
					clause.param = paras.Count switch {
						0 => null,
						1 => paras[0],
						_ => throw new NotImplementedException(
							"catch clause must have exactly one parameter or none" ),
					};

					if( clause.param != null ) {
						AddScopeOnly( new VarDecl {
							name   = clause.param.name ?? "",
							type   = clause.param.type,
							access = Access.Public,
							kind   = VarDecl.Kind.Var,
						} );
					}
				}

				clause.body = Visit( cc.stmt() );
				PopScope();
				ret.catches.Add( clause );
			}

			return ret;
		}

		// no override
		[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		public new IfStmt.CondThen VisitCondThen( CondThenContext c )
			=> new(
				c.expr().Visit( Context ),
				c.stmt().Visit( Context ) );

		public override IfStmt VisitStmtIf( StmtIfContext c )
		{
			IfStmt ret = new() {
				ifThens = c.condThen().Select( VisitCondThen ).ToList(),
				els     = c.stmt()?.Visit( Context ),
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
			// The current behavior is ImplicitBreak (see Dialect.SwitchFallthroughMode).
			// TODO: honor Dialect.SwitchFallthrough instead of always inserting a break.
			//       In Explicit mode every non-empty case must end with break, return, or fallthrough.
			//       In ImplicitFallthrough mode do not insert a break at all.
			if( hasNoStmt ) {
				// OK, consecutive case stmt, silent fallthrough
			}
			else if( isFall ) {
				body.stmts.Add( new FreetextStmt( "[[fallthrough]];" ) );
			}
			else if( body.stmts.Last() is not BreakStmt and not ReturnStmt ) {
				body.stmts.Add( new BreakStmt() );
			}

			SwitchStmt.CaseBlock ret = new(
				c.expr().Select( q => q.Visit( Context ) ).ToList(),
				body );
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
				cond  = c.cond.Visit( Context ),
				cases = c.caseBlock().Select( VisitCaseBlock ).ToList(),
				els   = VisitDefaultBlock( c.defaultBlock() ),
			};
			return ret;
		}

		public override LoopStmt VisitStmtLoop( StmtLoopContext c )
		{
			LoopStmt ret = new() {
				body = c.body.Visit( Context ),
			};
			return ret;
		}

		public override ForStmt VisitStmtFor( StmtForContext c )
		{
			ForStmt ret = new() {
				init = c.init.Visit( Context ),
				cond = c.cond?.Visit( Context ),
				iter = c.iter?.Visit( Context ),
				body = VisitBlockify( c.body ),
				els  = c.els?.Visit( Context ),
			};
			return ret;
		}

		public override WhileStmt VisitStmtWhile( StmtWhileContext c )
		{
			WhileStmt ret = new() {
				cond = c.cond.Visit( Context ),
				body = VisitBlockify( c.body ),
				els  = c.els?.Visit( Context ),
			};
			return ret;
		}

		public override DoWhileStmt VisitStmtDoWhile( StmtDoWhileContext c )
		{
			DoWhileStmt ret = new() {
				cond = c.cond.Visit( Context ),
				body = VisitBlockify( c.body ),
			};
			return ret;
		}

		public override TimesStmt VisitStmtTimes( StmtTimesContext c )
		{
			PushScope();

			TimesStmt ret = new() {
				count = c.count.Visit( Context ),
				name  = c.name?.Visit(),
			};

			// Anonymous loop variable: treat "_" or omitted name as hidden.
			if( String.IsNullOrEmpty( ret.name ) || ret.name == "_" )
				ret.name = Context.NextTempName();

			AddScopeOnly( new VarDecl {
				name   = ret.name,
				type   = new TypespecBasic {
					kind = TypespecBasic.Kind.Integer,
					size = TypespecBasic.SizeUndetermined,
				},
				access = Access.Public,
				kind   = VarDecl.Kind.Var,
			} );

			ret.body = VisitBlockify( c.body );

			// fill ret.offset
			ITerminalNode intLit = c.INTEGER_LIT();
			if( intLit != null ) {
				_ = long.TryParse( intLit.GetText(), out ret.offset );
				if( c.MINUS() != null )
					ret.offset *= -1;
			}

			PopScope();
			return ret;
		}

		public override AggrAssign VisitStmtAggregate( StmtAggregateContext c )
		{
			AggrAssign ret = new() {
				op        = c.aggrAssignOP().v.ToOp(),
				leftExpr  = c.expr( 0 ).Visit( Context ),
				rightExpr = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		public override MultiAssign VisitStmtAssign( StmtAssignContext c )
		{
			MultiAssign ret = new() {
				exprs = c.expr().Select( q => q.Visit( Context ) ).ToList(),
			};
			return ret;
		}

		public override ExprStmt VisitStmtExpr( StmtExprContext c )
		{
			ExprStmt ret = new() {
				expr = c.expr().Visit( Context ),
			};
			return ret;
		}
	}
}
