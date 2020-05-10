using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using Myll.Core;

namespace Myll
{
	using static MyllParser;

	using Attribs = Dictionary<string, List<string>>;

	public class DeclVisitor
		: ExtendedVisitor<Decl>
	{
		public DeclVisitor( Stack<Scope> scopeStack )
			: base( scopeStack ) {}

		public string ProbeModule( ProgContext c )
		{
			string module = c.module()?.id().GetText()
			             ?? c.Start.InputStream.SourceName.Replace( ".myll", "" );
			return module;
		}

		public override Decl Visit( IParseTree c )
			=> c == null
				? null
				: base.Visit( c );

		public override Decl VisitEnumDecl( EnumDeclContext c )
		{
			Enumeration ret = new Enumeration {
				srcPos   = c.ToSrcPos(),
				name     = c.id().Visit(),
				access   = curAccess,
				basetype = (c.bases != null) ? VisitTypespecBasic( c.bases ) : null,
			};
			// do not reset curAccess because there is no change inside enums
			PushScope( ret );
			AddChildren( VisitEnumEntrys( c.idExprs() ) );
			PopScope();

			return ret;
		}

		public override Decl VisitFuncDef( FuncDefContext c )
		{
			// ist das hier richtig?
			PushScope();
			Func ret = new Func {
				srcPos    = c.ToSrcPos(),
				name      = c.id().Visit(),
				access    = curAccess,
				TplParams = VisitTplParams( c.tplParams() ),
				paras     = VisitFuncTypeDef( c.funcTypeDef() ).ToList(),
				block     = c.funcBody().Visit(),
			};
			ret.retType = c.typespec() != null ? VisitTypespec( c.typespec() ) :
				ret.IsReturningSomething ?
					new TypespecBasic {
						kind = TypespecBasic.Kind.Auto,
						size = TypespecBasic.SizeUndetermined,
						ptrs = new List<Pointer>(),
					} :
					new TypespecBasic {
						kind = TypespecBasic.Kind.Void,
						size = TypespecBasic.SizeInvalid,
						ptrs = new List<Pointer>(),
					};
			PopScope();
			AddChild( ret );
			return ret;
		}

		public new List<Decl> VisitFunctionDecl( FunctionDeclContext c )
			=> c.funcDef().Select( VisitFuncDef ).ToList();

		public override Decl VisitOpDef( OpDefContext c )
		{
			// TODO solve that better
			string stringOp  = c.STRING_LIT().GetText();
			string cleanedOp = "operator" + stringOp.Substring( 1, stringOp.Length - 2 );
			PushScope();
			Func ret = new Func {
				srcPos    = c.ToSrcPos(),
				name      = cleanedOp,
				access    = curAccess,
				TplParams = VisitTplParams( c.tplParams() ),
				paras     = VisitFuncTypeDef( c.funcTypeDef() ).ToList(),
				block     = c.funcBody().Visit(),
			};
			ret.retType = c.typespec() != null ? VisitTypespec( c.typespec() ) :
				ret.IsReturningSomething ?
					new TypespecBasic {
						kind = TypespecBasic.Kind.Auto,
						size = TypespecBasic.SizeUndetermined,
						ptrs = new List<Pointer>(),
					} :
					new TypespecBasic {
						kind = TypespecBasic.Kind.Void,
						size = TypespecBasic.SizeInvalid,
						ptrs = new List<Pointer>(),
					};
			PopScope();
			AddChild( ret );
			return ret;
		}

		public new List<Decl> VisitOpDecl( OpDeclContext c )
			=> c.opDef().Select( VisitOpDef ).ToList();

		public override Decl VisitAttribDeclBlock( AttribDeclBlockContext c )
		{
			List<Decl> decls = c.levDecl()
				.Select( o => o?.Visit() )
				.ToList();

			// always has attribs
			Attribs attribs = c.attribBlk().Visit();
			decls.ForEach( o => o.AssignAttribs( attribs ) );

			// HACK: can only return one here, is this really a problem? Workaround could be a Decl that contains a Decl[]
			return decls[0];
		}

		public override Decl VisitAttribDecl( AttribDeclContext c )
		{
			Decl ret =
				(c.inAnyStmt() != null) ? Visit( c.inAnyStmt() ) :
				(c.inDecl()    != null) ? Visit( c.inDecl() ) :
				                          throw new ArgumentOutOfRangeException(
					                          nameof( c ), c, "neither inAnyStmt nor inDecl" );

			// PPP is null
			if( ret != null ) {
				Attribs attribs = c.attribBlk()?.Visit();
				if( attribs != null )
					ret.AssignAttribs( attribs );
			}

			return ret;
		}

		public GlobalNamespace VisitProgs( IGrouping<string, ProgContext> cs )
		{
			GlobalNamespace global = GenerateGlobalScope( cs.Key );

			foreach( ProgContext c in cs ) {
				global.imps.UnionWith(
					c.imports()
						.SelectMany( i => i.id() )
						.Select( i => i.GetText() )
						.ToList() );

				c.levDecl().Select( Visit ).Exec();

				CleanBodylessNamespace();
			}

			CloseGlobalScope();

			return global;
		}

		public override Decl VisitNamespace( NamespaceContext c )
		{
			Namespace ret = null;

			CleanBodylessNamespace();

			// TODO: check if Namespace already exists
			// add new namespaces to hierarchy
			bool withBody = (c.levDecl().Length >= 1);
			foreach( IdContext id in c.id() ) {
				Namespace ns = new Namespace {
					srcPos   = id.ToSrcPos(),
					name     = id.Visit(),
					withBody = withBody,
				};
				PushScope( ns );
				if( ret == null )
					ret = ns;
			}

			// only visit children and remove hierarchy with body
			if( withBody ) {
				c.levDecl().Select( Visit ).Exec();

				// wrong order but irrelevant now
				foreach( IdContext unused in c.id() )
					PopScope();
			}

			return ret;
		}

		public override Decl VisitStructDecl( StructDeclContext c )
		{
			Structural ret = new Structural {
				srcPos    = c.ToSrcPos(),
				name      = c.id().Visit(),
				access    = curAccess,
				kind      = c.v.ToStructuralKind(),
				TplParams = VisitTplParams( c.tplParams() ),
				basetypes = VisitTypespecsNested( c.bases?.typespecNested() ),
				reqs      = VisitTypespecsNested( c.reqs?.typespecNested() ),
			};
			PushScope( ret );

			// HACK: will be buggy. needs to move to ScopeStack, when ScopeStack works.
			Access savedAccess = curAccess;
			curAccess = ret.defaultAccess;

			c.levDecl().Select( Visit ).Exec();

			curAccess = savedAccess;

			PopScope();

			return ret;
		}

		public override Decl VisitCtorDecl( CtorDeclContext c )
		{
			Scope parent = scopeStack.Peek();
			if( !parent.HasDecl || !(parent.decl is Structural) )
				throw new Exception( "parent of c/dtor has no decl or is not a structural" );

			string name = parent.decl.name; //.FullyQualifiedName;

			PushScope();
			ConDestructor ret = new ConDestructor {
				srcPos = c.ToSrcPos(),
				name   = name,
				access = curAccess,
				kind   = ConDestructor.Kind.Constructor,
				paras  = VisitFuncTypeDef( c.funcTypeDef() ).ToList(),
				block  = c.levStmt().Visit(),
				// TODO: cc.initList(); // opt
			};
			PopScope();
			AddChild( ret );
			return ret;
		}

		public override Decl VisitDtorDecl( DtorDeclContext c )
		{
			Scope parent = scopeStack.Peek();
			if( !parent.HasDecl || !(parent.decl is Structural) )
				throw new Exception( "parent of c/dtor has no decl or is not a structural" );

			string name = "~" + parent.decl.name;

			PushScope();
			ConDestructor ret = new ConDestructor {
				srcPos = c.ToSrcPos(),
				name   = name,
				access = curAccess,
				kind   = ConDestructor.Kind.Destructor,
				paras  = new List<Param>(),
				block  = c.levStmt().Visit(),
				// TODO: cc.initList(); // opt
			};
			PopScope();
			AddChild( ret );
			return ret;
		}

		// list of typed and initialized vars
		public List<Var> VisitVars( TypedIdAcorsContext c )
		{
			Scope scope = scopeStack.Peek();
			// determine if only scope or container
			Typespec type = VisitTypespec( c.typespec() );
			List<Var> ret = c.idAccessors()
				.idAccessor()
				.Select(
					q => new Var {
						srcPos   = c.ToSrcPos(),
						name     = q.id().GetText(),
						access   = curAccess,
						type     = type,
						init     = q.expr().Visit(),
						accessor = q.accessorDef().Visit(),
						// TODO: Accessors, is this still valid?
					} )
				.ToList();
			AddChildren( ret );
			return ret;
		}

		public override Decl VisitVariableDecl( VariableDeclContext c )
		{
			Decl ret = new VarsDecl {
				vars = c.typedIdAcors()
					.Select( VisitVars )
					.SelectMany( q => q )
					.ToList(),
			};
			// TODO save the constness
			return ret;
		}

		// HACK: will be buggy. needs to move to ScopeStack, when ScopeStack works.
		private Access curAccess = Access.Public;
		public override Decl VisitAccessMod( AccessModContext c )
		{
			curAccess = c.Visit();
			return null;
		}
	}
}
