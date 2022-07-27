using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Myll.Core;

namespace Myll
{
	using static MyllParser;

	using Attribs = Dictionary<string, List<string>>;

	public class DeclVisitor
		: ExtendedVisitor<Decl>
	{
		// HACK: will be buggy. needs to move to ScopeStack, when ScopeStack works.
		private Access curAccess = Access.Public;

		public DeclVisitor( Stack<Scope> scopeStack )
			: base( scopeStack ) {}

		public string ProbeModule( ProgContext c )
		{
			string module = c.module()?.id().GetText()
			             ?? c.Start.InputStream.SourceName.Replace( ".myll", "" );
			return module;
		}

		public override Decl? Visit( IParseTree? c )
			=> c == null
				? null
				: base.Visit( c );

		public Decl? VisitMulti<T>( T[] c )
			where T : ParserRuleContext
			=> c.Length switch {
				0 => throw new InvalidDataException( "Empty array for VisitMulti" ), //null,
				1 => Visit( c[0] ),
				_ => new MultiDecl( c.Select( Visit ) )
			};

		public GlobalNamespace VisitProgs( IGrouping<string, ProgContext> cs )
		{
			GlobalNamespace global = GenerateGlobalScope( cs.Key );

			foreach( ProgContext c in cs ) {
				global.imps.UnionWith(
					c.imports()
						.SelectMany( i => i.id() )
						.Select( i => i.GetText() )
						.ToList() );

				c.decl().Select( Visit ).Exec();

				CleanBodylessNamespace();
			}

			CloseGlobalScope();

			return global;
		}

		public override Decl VisitAttrDecl( 	AttrDeclContext		c ) => VisitAttrAnyDecl( c.attribBlk(), c.defDecl(), c.attrDecl() );
		public override Decl VisitAttrUsing( 	AttrUsingContext	c ) => VisitAttrAnyDecl( c.attribBlk(), c.defUsing(), c.attrUsing() );
		public override Decl VisitAttrAlias( 	AttrAliasContext	c ) => VisitAttrAnyDecl( c.attribBlk(), c.defAlias(), c.attrAlias() );
		public override Decl VisitAttrConvert( 	AttrConvertContext	c ) => VisitAttrAnyDecl( c.attribBlk(), c.defConvert(), c.attrConvert() );
		public override Decl VisitAttrCtor( 	AttrCtorContext		c ) => VisitAttrAnyDecl( c.attribBlk(), c.defCtor(), c.attrCtor() );
		public override Decl VisitAttrOp( 		AttrOpContext		c ) => VisitAttrAnyDecl( c.attribBlk(), c.defOp(), c.attrOp() );

		// no override
		public Decl VisitAttrAnyDecl<TDefContext, TAttrContext>(
			AttribBlkContext? blkc,
			TDefContext?      dc,
			TAttrContext[]    ac ) // missed opportunity for an ACDC joke
			where TDefContext : ParserRuleContext
			where TAttrContext : ParserRuleContext
		{
			Decl     ret;
			Attribs? attribs = blkc?.Visit();
			if( dc != null ) {
				ret = Visit( dc );
			}
			else if( ac.Any() ) {
				ret = VisitMulti( ac );
			}
			else {
				VisitAttrColon( attribs );
				return null;
			}

			if( attribs != null )
				ret.AssignAttribs( attribs );

			return ret;
		}

		// no override
		public Decl VisitAttrFunc( AttrFuncContext c, Func.Kind kind )
		{
			Decl    ret;
			Attribs attribs = c.attribBlk()?.Visit();
			if( c.defFunc() != null ) {
				ret = VisitDefFunc( c.defFunc(), kind );
			}
			else if( c.attrFunc() != null ) {
				ret = new MultiDecl( c.attrFunc().Select( ac => VisitAttrFunc( ac, kind ) ) );
			}
			else {
				VisitAttrColon( attribs );
				return null;
			}

			if( attribs != null )
				ret?.AssignAttribs( attribs );

			return ret;
		}

		// no override
		public MultiDecl VisitAttrVar( AttrVarContext c, VarDecl.Kind kind )
		{
			MultiDecl ret;
			Attribs   attribs = c.attribBlk()?.Visit();
			if( c.defVar() != null ) {
				ret = VisitDefVar( c.defVar(), kind );
			}
			else if( c.attrVar() != null ) {
				ret = new( c.attrVar().Select( ac => VisitAttrVar( ac, kind ) ) );
			}
			else {
				VisitAttrColon( attribs );
				return null;
			}

			if( attribs != null )
				ret?.AssignAttribs( attribs );

			return ret;
		}

		// no override
		public void VisitAttrColon( Attribs attribs )
		{
			// HACK: will be buggy like VisitAccessMod needs to move to ScopeStack, when ScopeStack works.
			// HACK: only works for pub, prot & priv now, with optional "access=" prefix
			string access = attribs.ContainsKey( "access" )
				? attribs["access"].First()
				: attribs.First().Key;

			curAccess = access switch {
				"pub"  => Access.Public,
				"prot" => Access.Protected,
				"priv" => Access.Private,
				_      => throw new NotSupportedException( "Got unsupported attribute in AttribState: " + access ),
			};
		}

		// The other Decl should not need implementations here, only Struct, Func, and Var

		public override Decl VisitDeclStruct( DeclStructContext c )
		{
			Structural.Kind kind = c.kindOfStruct().Visit();
			return VisitDefStruct( c.defStruct(), kind );
		}

		public override Decl VisitDeclFunc( DeclFuncContext c )
		{
			Func.Kind kind = c.kindOfFunc().Visit();

			Decl ret;
			if( c.defFunc() != null )
				ret = VisitDefFunc( c.defFunc(), kind );
			else if( c.attrFunc() != null )
				ret = new MultiDecl( c.attrFunc().Select( ac => VisitAttrFunc( ac, kind ) ) );
			else
				throw new InvalidOperationException( "declFunc unknown" );

			return ret;
		}

		public override MultiDecl VisitDeclVar( DeclVarContext c )
		{
			// TODO: huge overlap with VisitAttrVar

			VarDecl.Kind kind = c.kindOfVar().Visit();

			MultiDecl ret;
			if( c.defVar() != null )
				ret = VisitDefVar( c.defVar(), kind );
			else if( c.attrVar() != null )
				ret = new( c.attrVar().Select( ac => VisitAttrVar( ac, kind ) ) );
			else
				throw new InvalidOperationException( "declVar unknown " );

			return ret;
		}

		public override Namespace VisitDefNamespace( DefNamespaceContext c )
		{
			// CnP: recheck this
			Namespace ret = null;

			CleanBodylessNamespace();

			// TODO: check if Namespace already exists
			// add new namespaces to hierarchy
			bool withBody = (c.SEMI()  == null
			              && c.COLON() == null);
			foreach( IdContext id in c.id() ) {
				Namespace ns = new() {
					srcPos   = id.ToSrcPos(),
					name     = id.Visit(),
					withBody = withBody,
				};
				PushScope( ns );
				if( ret == null )
					ret = ns;
			}

			// only visit children and remove hierarchy with then
			if( withBody ) {
				c.decl().Select( Visit ).Exec();

				// wrong order but irrelevant now
				foreach( IdContext unused in c.id() )
					PopScope();
			}

			return ret;
		}

		public override MultiDecl VisitDefUsing( DefUsingContext c )
		{
			SrcPos pos = c.ToSrcPos();
			MultiDecl ret = new(
				VisitTypespecsNested( c.typespecsNested() ).Select(
					t => new UsingDecl() {
						srcPos = pos,
						type   = t
					} ) );
			AddChildren( ret.decls );
			return ret;
		}

		public override UsingDecl VisitDefAlias( DefAliasContext c )
		{
			// TODO: tplParams, multi-decl
			List<TplParam> useMe = VisitTplParams( c.tplParams() );

			// TODO: This should not be a UsingDecl
			UsingDecl ret = new() {
				srcPos = c.ToSrcPos(),
				name   = c.id().GetText(),
				type   = VisitTypespec( c.typespec() ),
			};
			AddChild( ret );
			return ret;
		}

		// TODO
		public override Decl VisitDefAspect( DefAspectContext c ) => base.VisitDefAspect( c );

		// TODO
		public override Decl VisitDefConcept( DefConceptContext c ) => base.VisitDefConcept( c );

		public override Enumeration VisitDefEnum( DefEnumContext c )
		{
			// CnP: recheck this
			// TODO: enum inheritance from non-basic types
			Enumeration ret = new() {
				srcPos   = c.ToSrcPos(),
				name     = c.id().Visit(),
				access   = curAccess,
				baseType = (c.bases != null) ? VisitTypespecBasic( c.bases ) : null,
			};
			// do not reset curAccess because there is no change inside enums
			PushScope( ret );
			{
				AddChildren( VisitEnumEntrys( c.idExprs() ) );
			}
			PopScope();

			return ret;
		}

		public Structural VisitDefStruct( DefStructContext c, Structural.Kind kind )
		{
			// CnP: recheck this
			Structural ret = new() {
				srcPos    = c.ToSrcPos(),
				name      = c.id().Visit(),
				access    = curAccess,
				kind      = kind,
				TplParams = VisitTplParams( c.tplParams() ),
				basetypes = VisitTypespecsNested( c.bases?.typespecNested() ),
				reqs      = VisitTypespecsNested( c.reqs?.typespecNested() ),
			};

			PushScope( ret );
			{
				// HACK: will be buggy. needs to move to ScopeStack, when ScopeStack works.
				// turns out to be not so buggy after all...
				Access savedAccess = curAccess;
				curAccess = ret.defaultAccess;

				c.decl().Select( Visit ).Exec();

				curAccess = savedAccess;
			}
			PopScope();

			return ret;
		}

		// TODO
		public override Decl VisitDefConvert( DefConvertContext c ) => base.VisitDefConvert( c );

		public override Structor VisitDefCtor( DefCtorContext c )
		{
			Scope parent = scopeStack.Peek();
			if( !parent.HasDecl || !(parent.decl is Structural) )
				throw new Exception( "parent of ctor has no decl or is not a structural" );

			PushScope();
			Structor ret = new() {
				srcPos = c.ToSrcPos(),
				name   = parent.decl.name, //.FullyQualifiedName;,
				access = curAccess,
				kind   = Structor.Kind.Constructor,
				paras  = VisitFuncTypeDef( c.funcTypeDef() ).ToList(),
				body   = c.funcBody().Visit(),
				// TODO: cc.initList(); // opt
			};
			PopScope();
			AddChild( ret );
			return ret;
		}

		public override Structor VisitDefDtor( DefDtorContext c )
		{
			Scope parent = scopeStack.Peek();
			if( !parent.HasDecl || !(parent.decl is Structural) )
				throw new Exception( "parent of dtor has no decl or is not a structural" );

			PushScope();
			Structor ret = new() {
				srcPos = c.ToSrcPos(),
				name   = "~" + parent.decl.name,
				access = curAccess,
				kind   = Structor.Kind.Destructor,
				paras  = new(),
				body   = c.funcBody().Visit(),
			};
			PopScope();
			AddChild( ret );
			return ret;
		}

		public override Func VisitDefOp( DefOpContext c )
		{
			Scope parent = scopeStack.Peek();
			Func ret;
			PushScope();
			{
				if( c.ASSIGN() != null ) {
					string?     id          = c.id().Visit();
					PassingKind passingKind = c.kindOfPassing().Visit();
					bool        isCopy      = passingKind == PassingKind.Copy;
					bool        isMove      = passingKind == PassingKind.Move;

					if( !isCopy && !isMove )
						throw new NotSupportedException( "only copy and move special assignment ops are supported" );

					if( !parent.HasDecl || !(parent.decl is Structural) )
						throw new Exception( "parent of operator= copy or move has no decl or is not a structural" );

					string className = parent.decl.name; //.FullyQualifiedName;
					List<IdTplArgs> classIdTpls = new() {
						new() {
							id      = className,
							tplArgs = new(),
						},
					};

					var qualPtrsTuple = passingKind.ToQualPtrs();
					Param param = new() {
						name = id ?? "other", // TODO: replace "other" with configuration
						type = new TypespecNested {
							idTpls = classIdTpls,
							qual   = qualPtrsTuple.qual,
							ptrs   = qualPtrsTuple.ptrs,
						},
					};

					ret = new() {
						srcPos   = c.ToSrcPos(),
						name     = "operator=",
						access   = curAccess,
						body     = c.funcBody().Visit(),
						Requires = VisitTypespecsNested( c.typespecsNested() ),
						paras    = new() { param },
						retType = new TypespecNested() {
							idTpls = classIdTpls,
							ptrs = new() {
								new() { kind = Pointer.Kind.LVRef },
							},
						},
					};
					//ret = VisitOpSpecialAssign( c );
				}
				else if( c.CONVERT() != null ) {
					ret = new() {
						srcPos    = c.ToSrcPos(),
						name      = string.Empty,
						access    = curAccess,
						kind      = Func.Kind.Convert,
						TplParams = VisitTplParams( c.tplParams() ),
						Requires  = VisitTypespecsNested( c.typespecsNested() ),
						paras     = new(),
						body      = c.funcBody().Visit(),
						retType   = VisitTypespec( c.typespec() ),
					};
				}
				else {
					string stringOp  = c.STRING_LIT().GetText();
					string cleanedOp = "operator" + stringOp.Substring( 1, stringOp.Length - 2 );
					ret = VisitDefCoreFunc(
						c.defCoreFunc(),
						cleanedOp,
						Func.Kind.Operator,
						VisitTypespecsNested( c.typespecsNested() ),
						c.funcBody().Visit() );
				}
			}
			PopScope();
			AddChild( ret ); // needs ret.name to be set already
			c.id().Visit();
			return ret;
		}

		// no override
		public Func VisitDefFunc( DefFuncContext c, Func.Kind kind )
		{
			PushScope();

			Func ret = VisitDefCoreFunc(
				c.defCoreFunc(),
				c.id().Visit(),
				kind,
				VisitTypespecsNested( c.typespecsNested() ),
				c.funcBody().Visit() );

			PopScope();
			AddChild( ret );

			// what was that for?
			//bool wasOK = NotifyObservers( ret );
			// validator
			//ret.HasAttrib( "blah" );

			return ret;
		}

		// no override
		public Func VisitDefCoreFunc(
			DefCoreFuncContext   c,
			string               name,
			Func.Kind            kind,
			List<TypespecNested> requires,
			Block?               body )
		{
			Func ret = new() {
				srcPos    = c.ToSrcPos(),
				name      = name,
				access    = curAccess,
				kind      = kind,
				TplParams = VisitTplParams( c.tplParams() ),
				Requires  = requires,
				paras     = VisitFuncTypeDef( c.funcTypeDef() ).ToList(),
				body      = body,
			};
			ret.retType = c.typespec() != null ? VisitTypespec( c.typespec() ) :
				ret.IsReturningSomething ?
					new TypespecBasic {
						kind = TypespecBasic.Kind.Auto,
						size = TypespecBasic.SizeUndetermined,
					} :
					new TypespecBasic {
						kind = TypespecBasic.Kind.Void,
						size = TypespecBasic.SizeInvalid,
					};
			return ret;
		}

		// no override
		// list of typed and initialized vars
		public MultiDecl VisitDefVar( DefVarContext c, VarDecl.Kind kind )
		{
			Scope scope = scopeStack.Peek();
			// determine if only scope or container
			Typespec type = VisitTypespec( c.typedIdAcors().typespec() );
			if( kind.ToQualifier() == Qualifier.Const ) {
				type.qual |= Qualifier.Const;
			}
			List<Decl> decls = c.typedIdAcors()
				.idAccessors()
				.idAccessor()
				.Select(
					q => new VarDecl {
						srcPos   = c.ToSrcPos(),
						name     = q.id().GetText(),
						kind     = kind,
						access   = curAccess,
						type     = type,
						init     = q.expr()?.Visit(),
						accessor = q.accessorDef().Visit(),
						// TODO: Accessors, is this still valid?
					} as Decl )
				.ToList();
			AddChildren( decls );
			MultiDecl ret = new( decls );
			return ret;
		}

		#region Disallowed Visitors (throwing InvalidOperationException)

		public override Decl VisitAttrFunc( AttrFuncContext c )
			=> throw new InvalidOperationException(
				"This method may never be called, always use the two parameter overload" );

		public override Decl VisitAttrVar( AttrVarContext c )
			=> throw new InvalidOperationException(
				"This method may never be called, always use the two parameter overload" );

		public override Decl VisitDefStruct( DefStructContext c )
			=> throw new InvalidOperationException(
				"This method may never be called, always use the two parameter overload" );

		public override Decl VisitDefFunc( DefFuncContext c )
			=> throw new InvalidOperationException(
				"This method may never be called, always use the two parameter overload" );

		public override Decl VisitDefVar( DefVarContext c )
			=> throw new InvalidOperationException(
				"This method may never be called, always use the two parameter overload" );

		public override Decl VisitDefCoreFunc( DefCoreFuncContext c )
			=> throw new InvalidOperationException(
				"This method may never be called, always use the two parameter overload" );

		#endregion
	}
}
