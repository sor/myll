using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Myll.Core;
using Myll.Resolver;

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

		public DeclVisitor( ModuleContext context )
			: base( context ) {}

		public string ProbeModule( ProgContext c )
		{
			string module = c.module()?.id().GetText()
			             ?? c.Start.InputStream.SourceName.Replace( ".myll", "" );
			return module;
		}

		/*
		public override Decl? Visit( IParseTree? c )
			=> c == null
				? null
				: base.Visit( c );
		*/

		public override Decl Visit( IParseTree c )
			=>  c == null
				? throw new Exception()
				: base.Visit( c );

		public Decl VisitMulti<T>( T[] c )
			where T : ParserRuleContext
			=> c.Length switch {
				0 => throw new InvalidOperationException( "Empty array for VisitMulti" ), //null,
				1 => Visit( c[0] ),
				_ => c.Select( Visit ).ToMulti()
			};

		private static bool IsAccessAttribute( Attribs? attribs )
		{
			if( attribs == null )
				return false;
			if( attribs.ContainsKey( "access" ) )
				return true;
			return attribs.ContainsKey( "pub" )
			    || attribs.ContainsKey( "priv" )
			    || attribs.ContainsKey( "prot" );
		}

		private void ValidateAccessAttribute( Attribs? attribs )
		{
			if( !IsAccessAttribute( attribs ) )
				return;

			if( scopeStack.Peek().decl is Structural )
				return;

			throw new InvalidOperationException(
				"Access attributes are only valid inside class/struct/union declarations." );
		}

		public GlobalNamespace VisitProgs( IGrouping<string, ProgContext> cs )
		{
			GlobalNamespace global = GenerateGlobalScope( cs.Key );

			try {
				foreach( ProgContext c in cs ) {
					foreach( ImportsContext importCtx in c.imports() ) {
						foreach( ImportNameContext nameCtx in importCtx.importName() ) {
							if( nameCtx.namedImport() is NamedImportContext namedCtx ) {
								global.imports.Add( new NamedImportSpec {
									name   = namedCtx.id().GetText(),
									srcPos = namedCtx.ToSrcPos(),
								} );
							}
							else if( nameCtx.systemImport() is SystemImportContext sysCtx ) {
								string raw  = sysCtx.GetText();
								string path = raw.Substring( 1, raw.Length - 2 );
								global.imports.Add( new RawImportSpec {
									raw = new RawImport {
										kind   = RawImportKind.System,
										path   = path,
										srcPos = sysCtx.ToSrcPos(),
									},
									srcPos = sysCtx.ToSrcPos(),
								} );
							}
							else if( nameCtx.localImport() is LocalImportContext localCtx ) {
								string raw  = localCtx.STRING_LIT().GetText();
								string path = raw.Substring( 1, raw.Length - 2 );
								global.imports.Add( new RawImportSpec {
									raw = new RawImport {
										kind   = RawImportKind.Local,
										path   = path,
										srcPos = localCtx.ToSrcPos(),
									},
									srcPos = localCtx.ToSrcPos(),
								} );
							}
						}
					}

					c.decl().Select( Visit ).Exec();

					CleanBodylessNamespace();
				}
			}
			catch( DiagnosticException ex ) {
				Context.Diagnostics.Add( ex.Diagnostic );
				// Open scopes from the aborted visitation (class bodies, functions)
				// must be drained so closing the global scope stays consistent.
				while( scopeStack.Count > 1 )
					PopScope();
			}

			CloseGlobalScope();

			return global;
		}

		public override Decl VisitDecl( DeclContext c )
		{
			Attribs? attribs = c.attribBlk()?.Visit();
			ReportDuplicateAttribs( c.attribBlk() );

			if( c.COLON() != null ) {
				VisitAttrColon( attribs );
				return null!;
			}

			ValidateAccessAttribute( attribs );

			// Namespace and class/struct attributes must be applied before children are visited,
			// so that the [extern] flag is visible while adding child declarations.
			DefDeclContext? defDecl = c.defDecl();
			if( defDecl?.declNamespace() is DeclNamespaceContext nsCtx && attribs != null ) {
				DefNamespaceContext? defNs = nsCtx.defNamespace();
				if( defNs != null )
					return VisitDefNamespace( defNs, attribs );
			}

			if( defDecl?.declStruct() is DeclStructContext structCtx && attribs != null ) {
				DefStructContext? defStruct = structCtx.defStruct();
				Structural.Kind   kind      = structCtx.kindOfStruct().Visit();
				if( defStruct != null )
					return VisitDefStruct( defStruct, kind, attribs );
			}

			Decl ret = (defDecl != null)
				? Visit( defDecl )
				: VisitMulti( c.decl() );

			if( attribs != null )
				ret.AssignAttribs( attribs );

			switch( ret ) {
				case Func func:
					ValidateForwardDeclaration( func );
					break;
				case MultiDecl multi:
					foreach( Func func in multi.decls.OfType<Func>() )
						ValidateForwardDeclaration( func );
					break;
			}

			return ret;
		}
		private void ReportDuplicateAttribs( AttribBlkContext? c )
		{
			if( c == null )
				return;

			HashSet<string> seen = new( StringComparer.Ordinal );
			foreach( AttribContext a in c.attrib() ) {
				string name = a.attribId().GetText();
				if( seen.Add( name ) )
					continue;

				// [spec=all] and [spec=some] name opposite specialization kinds;
				// combining them names none.
				if( name == "spec" ) {
					Context.Diagnostics.Add( new Diagnostic(
						a.ToSrcPos(),
						DiagnosticKind.Error,
						"Duplicate attribute 'spec'; [spec=all] and [spec=some] cannot be combined" ) );
					continue;
				}

				Context.Diagnostics.Add( new Diagnostic(
					a.ToSrcPos(),
					DiagnosticKind.Warning,
					String.Format( "Duplicate attribute '{0}' ignored; the first one applies.", name ) ) );
			}
		}

		private void ValidateForwardDeclaration( Decl decl )
		{
			if( !decl.IsForwardDeclaration )
				return;

			if( Context.IsPrototypeFile )
				return;

			if( decl.IsExternal )
				return;

			if( scopeStack.Peek().decl is Hierarchical h && h.IsExternal )
				return;

			if( decl is Func func
			 && ( func.IsAbstract || func.IsDefault || func.IsDisabled ) )
				return;

			Context.Diagnostics.Add( new Diagnostic(
				decl.srcPos,
				DiagnosticKind.Error,
				"Forward declarations are only allowed in .decl.myll/.d.myll files, [extern] contexts, or OOP special members." ) );
		}

		public override Decl VisitAttrUsing(	AttrUsingContext	c ) => VisitAttrAnyDecl( c.attribBlk(), c.defUsing(), c.attrUsing(), c.COLON() != null )!;
		public override Decl VisitAttrAlias(	AttrAliasContext	c ) => VisitAttrAnyDecl( c.attribBlk(), c.defAlias(), c.attrAlias(), c.COLON() != null )!;
		public override Decl VisitAttrConvert(	AttrConvertContext	c ) => VisitAttrAnyDecl( c.attribBlk(), c.defConvert(), c.attrConvert(), c.COLON() != null )!;
		public override Decl VisitAttrCtor(		AttrCtorContext		c ) => VisitAttrAnyDecl( c.attribBlk(), c.defCtor(), c.attrCtor(), c.COLON() != null )!;
		public override Decl VisitAttrOp(		AttrOpContext		c ) => VisitAttrAnyDecl( c.attribBlk(), c.defOp(), c.attrOp(), c.COLON() != null )!;

		// no override
		public Decl? VisitAttrAnyDecl<TDefContext, TAttrContext>(
			AttribBlkContext? aAttribBlk,
			TDefContext?      cDef,
			TAttrContext[]    cAttr,
			bool              isColon ) // missed opportunity for an ACDC joke
			where TDefContext : ParserRuleContext
			where TAttrContext : ParserRuleContext
		{
			Attribs? attribs = aAttribBlk?.Visit();
			if( isColon ) {
				VisitAttrColon( attribs );
				return null;
			}

			Decl ret = (cDef != null)
				? Visit( cDef )
				: VisitMulti( cAttr );

			ValidateAccessAttribute( attribs );

			if( attribs != null )
				ret.AssignAttribs( attribs );

			return ret;
		}

		// no override
		public Decl? VisitAttrFunc( AttrFuncContext c, Func.Kind kind )
		{
			Attribs? attribs = c.attribBlk()?.Visit();
			if( c.COLON() != null ) {
				VisitAttrColon( attribs );
				return null;
			}

			Decl ret = (c.defFunc() != null)
				? VisitDefFunc( c.defFunc(), kind )
				: c.attrFunc()
					.Select( ac => VisitAttrFunc( ac, kind ) )
					.OfType<MultiDecl>()
					.ToMulti();

			ValidateAccessAttribute( attribs );

			if( attribs != null )
				ret.AssignAttribs( attribs );

			return ret;
		}

		// no override
		public MultiDecl? VisitAttrVar( AttrVarContext c, VarDecl.Kind kind )
		{
			Attribs? attribs = c.attribBlk()?.Visit();
			if( c.COLON() != null ) {
				VisitAttrColon( attribs );
				return null;
			}

			MultiDecl ret = (c.defVar() != null)
				? VisitDefVar( c.defVar(), kind )
				: c.attrVar()
					.Select( ac => VisitAttrVar( ac, kind ) )
					.OfType<MultiDecl>()
					.ToMulti();

			ValidateAccessAttribute( attribs );

			if( attribs != null )
				ret.AssignAttribs( attribs );

			return ret;
		}

		// no override
		public void VisitAttrColon( Attribs? attribs )
		{
			if( attribs == null )
				throw new InvalidOperationException( "VisitAttrColon without attributes" );

			ValidateAccessAttribute( attribs );

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

			Decl ret = (c.defFunc() != null)
				? VisitDefFunc( c.defFunc(), kind )
				: c.attrFunc()
					.Select( ac => VisitAttrFunc( ac, kind ) )
					.OfType<MultiDecl>()
					.ToMulti();

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
				ret = c.attrVar()
					.Select( ac => VisitAttrVar( ac, kind ) )
					.OfType<MultiDecl>()
					.ToMulti();
			else
				throw new InvalidOperationException( "declVar unknown " );

			return ret;
		}

		public override Namespace VisitDefNamespace( DefNamespaceContext c )
			=> VisitDefNamespace( c, null );

		private Namespace VisitDefNamespace( DefNamespaceContext c, Attribs? earlyAttribs )
		{
			// CnP: recheck this
			Namespace? ret = null;

			CleanBodylessNamespace();

			// TODO: check if Namespace already exists
			// add new namespaces to hierarchy
			bool withBody = (c.SEMI()  == null
			              && c.COLON() == null);
			bool isForwardSemi = c.SEMI() != null;
			foreach( IdContext id in c.id() ) {
				Namespace ns = new() {
					srcPos   = id.ToSrcPos(),
					name     = id.Visit(),
					withBody = withBody,
				};
				if( earlyAttribs != null )
					ns.AssignAttribs( earlyAttribs );
				ns.IsForwardDeclaration = isForwardSemi;
				PushScope( ns );
				ret ??= ns;
			}

			if( withBody ) {
				// visit children and remove hierarchy afterwards
				c.decl().Select( Visit ).Exec();

				// wrong order but irrelevant now
				foreach( IdContext unused in c.id() )
					PopScope();
			}
			else if( isForwardSemi ) {
				// `namespace N;` is a forward declaration only: it does not
				// scope the rest of the file. Use `namespace N:` for that.
				foreach( IdContext unused in c.id() )
					PopScope();
			}
			// else COLON: leave the namespace scope open for following decls

			ValidateForwardDeclaration( ret! );
			return ret!;
		}

		public override Decl VisitAttrNamespace( AttrNamespaceContext c )
		{
			Attribs? attribs = c.attribBlk()?.Visit();
			if( c.COLON() != null ) {
				VisitAttrColon( attribs );
				return null!;
			}

			Decl ret = (c.defNamespace() != null)
				? VisitDefNamespace( c.defNamespace(), attribs )
				: VisitMulti( c.attrNamespace() );

			ValidateAccessAttribute( attribs );

			if( attribs != null && ret != null )
				ret.AssignAttribs( attribs );

			return ret!;
		}

		public override MultiDecl VisitDefUsing( DefUsingContext c )
		{
			SrcPos srcPos = c.ToSrcPos();
			MultiDecl ret = VisitTypespecsNested( c.typespecsNested() )
				.Select(
					t => new UsingDecl() {
						srcPos = srcPos,
						name   = (t is TypespecNested n) ? n.idTpls.Last().id : null!,
						type   = t
					} )
				.ToMulti();

			Scope scope = scopeStack.Peek();
			foreach( UsingDecl usingDecl in ret.decls.OfType<UsingDecl>() ) {
				Context.UnresolvedUsings.Add( new UnresolvedUsing( usingDecl, scope ) );

				// defUsing parses a nested name, not an actual type use; don't let
				// the resolver report it as an unresolved type.
				if( usingDecl.type is TypespecNested nested )
					Context.UnresolvedTypes.RemoveAll( u => u.Node == nested );
			}

			AddChildren( ret.decls );
			return ret;
		}

	public override AliasDecl VisitDefAlias( DefAliasContext c )
	{
		// TODO: multi-decl
		List<TplParam> tplParams = VisitTplParams( c.tplParams() );

		// Alias templates need their own scope so that template parameter
		// placeholders are visible while resolving the aliased type, while the
		// alias name itself is registered in the enclosing scope.
		PushScope();
		Scope aliasScope = scopeStack.Peek();

		AliasDecl ret = new() {
			srcPos     = c.ToSrcPos(),
			name       = c.id().GetText(),
			type       = VisitTypespec( c.typespec() ),
			TplParams  = tplParams,
			TplSpec    = VisitTplSpec( c.tplSpec() ),
			aliasScope = aliasScope,
		};

		List<TypespecNested> nonTypePlaceholders = NormalizeAliasTypeTemplateArgs( ret.type, tplParams );
		foreach( TypespecNested placeholder in nonTypePlaceholders )
			Context.UnresolvedTypes.RemoveAll( u => u.Node == placeholder );

		Context.UnresolvedAliases.Add( new UnresolvedAlias( ret, aliasScope ) );
		if( ret.type is TypespecNested nested )
			Context.UnresolvedTypes.Add( new UnresolvedType( nested, aliasScope ) );

		PopScope();
		AddChild( ret );
		return ret;
	}

	private static List<TypespecNested> NormalizeAliasTypeTemplateArgs( Typespec type, List<TplParam> tplParams )
	{
		List<TypespecNested> placeholders = new();

		HashSet<string> nonTypeNames = tplParams
			.Where( p => p.kind == TplParamKind.NonType )
			.Select( p => p.name )
			.ToHashSet( StringComparer.Ordinal );
		if( nonTypeNames.Count == 0 )
			return placeholders;

		Queue<Typespec> work = new();
		work.Enqueue( type );
		while( work.Count > 0 ) {
			Typespec current = work.Dequeue();
			if( current is not TypespecNested nested )
				continue;

			foreach( IdTplArgs segment in nested.idTpls ) {
				if( segment.tplArgs == null )
					continue;
				for( int i = 0; i < segment.tplArgs.Count; i++ ) {
					TplArg arg = segment.tplArgs[i];
					if( arg.typespec != null )
						work.Enqueue( arg.typespec );

					if( arg.typespec is not TypespecNested placeholder
					 || placeholder.idTpls.Count != 1 )
						continue;
					IdTplArgs placeholderSegment = placeholder.idTpls[0];
					if( placeholderSegment.tplArgs == null || placeholderSegment.tplArgs.Count != 0 )
						continue;
					if( !nonTypeNames.Contains( placeholderSegment.id ) )
						continue;

					placeholders.Add( placeholder );
					segment.tplArgs[i] = new TplArg {
						lit = new IdExpr {
							idTplArgs = new IdTplArgs { id = placeholderSegment.id },
							srcPos    = placeholder.srcPos,
						},
					};
				}
			}
		}

		return placeholders;
	}

		// TODO
		public override Decl VisitDefAspect( DefAspectContext c )
			=> throw new DiagnosticException( new Diagnostic(
				c.ToSrcPos(),
				DiagnosticKind.Error,
				"aspect is not supported; see REASONS.md" ) );

		// TODO
		public override Decl VisitDefConcept( DefConceptContext c )
			=> throw new DiagnosticException( new Diagnostic(
				c.ToSrcPos(),
				DiagnosticKind.Error,
				"concept is not supported; see REASONS.md" ) );

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

			if( c.LCURLY() != null ) {
				// do not reset curAccess because there is no change inside enums
				PushScope( ret );
				{
					if( c.idExprs() != null )
						AddChildren( VisitEnumEntrys( c.idExprs() ) );
				}
				PopScope();
			}
			else {
				ret.IsForwardDeclaration = true;
				PushScope( ret );
				PopScope();
			}

			ValidateForwardDeclaration( ret );
			return ret;
		}

		public Structural VisitDefStruct( DefStructContext c, Structural.Kind kind )
			=> VisitDefStruct( c, kind, null );

		private Structural VisitDefStruct( DefStructContext c, Structural.Kind kind, Attribs? earlyAttribs )
		{
			// CnP: recheck this
			Structural ret = new() {
				srcPos    = c.ToSrcPos(),
				name      = c.typeId().Visit(),
				access    = curAccess,
				kind      = kind,
				TplParams = VisitTplParams( c.tplParams() ),
				TplSpec   = VisitTplSpec( c.tplSpec() ),
				reqs      = VisitTypespecsNested( c.reqs?.typespecNested() ),
			};

			if( earlyAttribs != null )
				ret.AssignAttribs( earlyAttribs );

			if( c.LCURLY() != null ) {
				PushScope( ret );
				{
					// Build base specs after the class scope has been pushed so that
					// the class's own template parameters are visible in dependent
					// base types such as Outer<T>::Base.
					ret.basetypes = BuildBaseSpecs( c.bases );

					// HACK: will be buggy. needs to move to ScopeStack, when ScopeStack works.
					// turns out to be not so buggy after all...
					Access savedAccess = curAccess;
					curAccess = ret.defaultAccess;

					c.decl().Select( Visit ).Exec();

					curAccess = savedAccess;
				}
				PopScope();
			}
			else {
				ret.IsForwardDeclaration = true;
				PushScope( ret );
				{
					// Same as above: base specs may reference the class's own
					// template parameters.
					ret.basetypes = BuildBaseSpecs( c.bases );
				}
				PopScope();
			}

			ValidateForwardDeclaration( ret );
			return ret;
		}

		// no override
		public List<BaseType> BuildBaseSpecs( BaseSpecsContext? c )
			=> c?.baseSpec()
				.Select( BuildBaseSpec )
				.ToList()
			?? new List<BaseType>();

		// no override
		public BaseType BuildBaseSpec( BaseSpecContext c )
		{
			BaseType ret = new() {
				type = VisitTypespecNested( c.typespecNested() ),
			};

			Attribs? attribs = c.attribBlk()?.Visit();
			if( attribs != null ) {
				foreach( KeyValuePair<string, List<string>> kv in attribs ) {
					string name = kv.Key;
					switch( name ) {
						case "pub":
						case "priv":
						case "prot":
							if( ret.access != Access.Public )
								throw new NotSupportedException( "Only one access specifier is allowed per base class." );
							ret.access = name switch {
								"priv" => Access.Private,
								"prot" => Access.Protected,
								_      => Access.Public,
							};
							break;
						case "virtual":
							ret.isVirtual = true;
							break;
						default:
							throw new NotSupportedException( "Unsupported attribute on base class: " + name );
					}
				}
			}

			return ret;
		}

		public override Decl VisitDefConvert( DefConvertContext c )
		{
			Scope parent = scopeStack.Peek();
			if( parent.decl is not Structural structuralParent )
				throw new Exception( "convert declarations are only allowed inside a class or struct" );

			// Determine direction: `convert -> T` is an operator, `convert <- T` is a
			// converting constructor.
			bool isOperator = c.RARROW() != null;

			if( isOperator ) {
				Typespec retType = VisitTypespec( c.typespec() );
				Func ret = BuildConvertOperator(
					c.ToSrcPos(), retType, VisitTplParams( c.tplParams() ), new(), c.funcBody() );
				AddChild( ret );
				c.id()?.Visit();
				return ret;
			}

			// Converting constructor: convert <- T
			Typespec paramType = VisitTypespec( c.typespec() );
			string paramName = c.id()?.Visit() ?? "other";
			Structor retCtor = BuildConversionCtor(
				structuralParent, c.ToSrcPos(), VisitTplParams( c.tplParams() ), paramType, paramName, c.funcBody() );
			AddChild( retCtor );
			return retCtor;
		}

		/// <summary>
		/// Shared conversion constructor for every spelling: `convert <- T`,
		/// `ctor convert <- T`, and `ctor convert T`. The parameter is `T [name]`.
		/// </summary>
		private Structor BuildConversionCtor(
			Structural structuralParent,
			SrcPos srcPos,
			List<TplParam> tplParams,
			Typespec paramType,
			string paramName,
			FuncBodyContext funcBody )
		{
			PushScope();
			// Register inside the constructor's own scope: template parameters
			// are injected there, so a parameter type naming one must resolve in
			// that scope.
			if( paramType is TypespecNested nestedParam )
				Context.UnresolvedTypes.Add( new UnresolvedType( nestedParam, scopeStack.Peek() ) );

			Structor ret = new() {
				srcPos           = srcPos,
				name             = structuralParent.name,
				access           = curAccess,
				kind             = Structor.Kind.Constructor,
				isConversionCtor = true,
				TplParams        = tplParams,
				ctorScope        = scopeStack.Peek(),
				paras = new() {
					new Param {
						name = paramName,
						type = paramType,
					},
				},
				body = funcBody.Visit( Context ),
			};
			AddParamsToScope( ret.paras );
			PopScope();
			return ret;
		}

		/// <summary>
		/// Shared conversion operator for `convert -> T` and `op convert -> T`. The
		/// defOp spelling may carry template parameters and requires.
		/// </summary>
		private Func BuildConvertOperator(
			SrcPos srcPos,
			Typespec retType,
			List<TplParam> tplParams,
			List<TypespecNested> requires,
			FuncBodyContext funcBody )
		{
			PushScope();
			// Register inside the operator's own scope: template parameters are
			// injected there, so a return type naming one must resolve there.
			if( retType is TypespecNested nested )
				Context.UnresolvedTypes.Add( new UnresolvedType( nested, scopeStack.Peek() ) );

			Func ret = new() {
				srcPos    = srcPos,
				name      = string.Empty,
				access    = curAccess,
				kind      = Func.Kind.Convert,
				TplParams = tplParams,
				Requires  = requires,
				paras     = new(),
				body      = funcBody.Visit( Context ),
				retType   = retType,
			};
			ret.funcScope = scopeStack.Peek();
			PopScope();
			return ret;
		}

		public override Structor VisitDefCtor( DefCtorContext c )
		{
			Scope parent = scopeStack.Peek();
			if( parent.decl is not Structural structuralParent )
				throw new Exception( "parent of ctor has no decl or is not a structural" );

			// Converting constructor: `ctor convert <- T` / `ctor convert T`
			if( c.CONVERT() != null ) {
				Typespec paramType = VisitTypespec( c.typespec() );
				string paramName = c.id()?.Visit() ?? "other";
				Structor retConvert = BuildConversionCtor(
					structuralParent, c.ToSrcPos(), VisitTplParams( c.tplParams() ), paramType, paramName, c.funcBody() );
				AddChild( retConvert );
				return retConvert;
			}

			PushScope();
			Structor ret;

			if( c.kindOfPassing() != null ) {
				ret = BuildSpecialConstructor( c, structuralParent );
			}
			else {
				ret = new() {
					srcPos    = c.ToSrcPos(),
					name      = structuralParent.name,
					access    = curAccess,
					kind      = Structor.Kind.Constructor,
					TplParams = VisitTplParams( c.tplParams() ),
					TplSpec   = VisitTplSpec( c.tplSpec() ),
					paras     = c.funcTypeDef() != null
						? VisitFuncTypeDef( c.funcTypeDef() ).ToList()
						: new(),
					// TODO: cc.initList(); // opt
				};
				ret.ctorScope = scopeStack.Peek();
				AddParamsToScope( ret.paras );
			}

			MultiStmt body = c.funcBody().Visit( Context );
			ret.body = IsEmptyFunctionBody( body ) ? null : body;

			PopScope();
			AddChild( ret );
			return ret;
		}

		private Structor BuildSpecialConstructor( DefCtorContext c, Structural structuralParent )
		{
			PassingKind passingKind = c.kindOfPassing().Visit();

			// `ctor copy<T>` and `ctor move<T>` parse as the element-type form
			// (`<typespec>`): copy/move constructors cannot take template
			// parameters — C++ forbids copy/move constructor templates.
			if( c.tplType != null && passingKind != PassingKind.InitList ) {
				Context.Diagnostics.Add( new Diagnostic(
					c.ToSrcPos(),
					DiagnosticKind.Error,
					String.Format(
						"'{0}' constructors cannot be parameterized by a template type; copy/move constructors cannot be templates (C++); declare a plain templated constructor 'ctor<U>( ... )' instead",
						passingKind.ToString().ToLowerInvariant() ) ) );
				return new() {
					srcPos = c.ToSrcPos(),
					name   = structuralParent.name,
					kind   = Structor.Kind.Constructor,
				};
			}

			string className = structuralParent.name;
			string paramName = c.id()?.Visit()
				?? ( passingKind == PassingKind.InitList ? "init" : "other" );

			Param param;
			if( passingKind == PassingKind.InitList ) {
				Typespec elementType;
				if( c.tplType != null ) {
					elementType = VisitTypespec( c.tplType );
				}
				else if( structuralParent.TplParams.Count >= 1 ) {
					elementType = new TypespecNested {
						idTpls = new List<IdTplArgs> {
							new() { id = structuralParent.TplParams[0].name },
						},
					};
				}
				else {
					throw new NotSupportedException(
						"`ctor initlist` requires an explicit element type or a templated class" );
				}

				var paramType = new TypespecNested {
					isInitList = true,
					idTpls     = new List<IdTplArgs> {
						new() { id = "std" },
						new() {
							id      = "initializer_list",
							tplArgs = new List<TplArg> { new() { typespec = elementType } },
						},
					},
					qual = Qualifier.Const,
					ptrs = new List<Pointer> { new() { kind = Pointer.Kind.LVRef } },
				};
				Context.UnresolvedTypes.Add( new UnresolvedType( paramType, scopeStack.Peek() ) );

				param = new() {
					name = paramName,
					type = paramType,
				};
			}
			else {
				if( passingKind != PassingKind.Copy && passingKind != PassingKind.Move )
					throw new NotSupportedException( "only copy, move and initlist constructors are supported" );

				List<IdTplArgs> classIdTpls = new() {
					new() { id = className },
				};

				var qualPtrsTuple = passingKind.ToQualPtrs();
				var paramType = new TypespecNested {
					idTpls = classIdTpls,
					qual   = qualPtrsTuple.qual,
					ptrs   = qualPtrsTuple.ptrs,
				};
				Context.UnresolvedTypes.Add( new UnresolvedType( paramType, scopeStack.Peek() ) );

				param = new() {
					name = paramName,
					type = paramType,
				};
			}

			var ret = new Structor {
				srcPos = c.ToSrcPos(),
				name   = structuralParent.name,
				access = curAccess,
				kind   = Structor.Kind.Constructor,
				paras  = new() { param },
				// TODO: cc.initList(); // opt
			};

			if( passingKind == PassingKind.InitList )
				ret.MergeAttribs( new Attribs { ["implicit"] = new List<string>() } );

			AddParamsToScope( ret.paras );
			return ret;
		}

		public override Structor VisitDefDtor( DefDtorContext c )
		{
			Scope parent = scopeStack.Peek();
			if( parent.decl is not Structural structuralParent )
				throw new Exception( "parent of dtor has no decl or is not a structural" );

			PushScope();
			Structor ret = new() {
				srcPos = c.ToSrcPos(),
				name   = "~" + structuralParent.name,
				access = curAccess,
				kind   = Structor.Kind.Destructor,
				paras  = new(),
				body   = c.funcBody().Visit( Context ),
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
				if( c.kindOfPassing() != null ) {
					string?     id          = c.id()?.Visit();
					PassingKind passingKind = c.kindOfPassing().Visit();
					bool        isCopy      = passingKind == PassingKind.Copy;
					bool        isMove      = passingKind == PassingKind.Move;
					bool        isInitList  = passingKind == PassingKind.InitList;

					if( c.tplType != null && !isInitList )
						throw new NotSupportedException(
							"explicit element type is only supported for `operator initlist =`" );

					if( !isCopy && !isMove && !isInitList )
						throw new NotSupportedException(
							"only copy, move and initlist special assignment ops are supported" );

					if( parent.decl is not Structural structuralParent )
						throw new Exception( "parent of operator= special assignment has no decl or is not a structural" );

					string className = structuralParent.name;
					List<IdTplArgs> classIdTpls = new() {
						new() { id = className },
					};

					Typespec paramType;
					if( isInitList ) {
						Typespec elementType;
						if( c.tplType != null ) {
							elementType = VisitTypespec( c.tplType );
						}
						else if( structuralParent.TplParams.Count >= 1 ) {
							elementType = new TypespecNested {
								idTpls = new List<IdTplArgs> {
									new() { id = structuralParent.TplParams[0].name },
								},
							};
						}
						else {
							throw new NotSupportedException(
								"`operator initlist =` requires an explicit element type or a templated class" );
						}

						paramType = new TypespecNested {
							isInitList = true,
							idTpls     = new List<IdTplArgs> {
								new() { id = "std" },
								new() {
									id      = "initializer_list",
									tplArgs = new List<TplArg> { new() { typespec = elementType } },
								},
							},
							qual = Qualifier.Const,
							ptrs = new List<Pointer> { new() { kind = Pointer.Kind.LVRef } },
						};
						Context.UnresolvedTypes.Add( new UnresolvedType( (TypespecNested)paramType, scopeStack.Peek() ) );
					}
					else {
						var qualPtrsTuple = passingKind.ToQualPtrs();
						paramType = new TypespecNested {
							idTpls = classIdTpls,
							qual   = qualPtrsTuple.qual,
							ptrs   = qualPtrsTuple.ptrs,
						};
						Context.UnresolvedTypes.Add( new UnresolvedType( (TypespecNested)paramType, scopeStack.Peek() ) );
					}

					Param param = new() {
						name = id ?? "other", // TODO: replace "other" with configuration
						type = paramType,
					};
					AddParamsToScope( new() { param } );

					var retType = new TypespecNested() {
						idTpls = classIdTpls,
						ptrs = new() {
							new() { kind = Pointer.Kind.LVRef },
						},
					};
					Context.UnresolvedTypes.Add( new UnresolvedType( retType, scopeStack.Peek() ) );

					MultiStmt opBody = c.funcBody().Visit( Context );
					ret = new() {
						srcPos   = c.ToSrcPos(),
						name     = "operator=",
						access   = curAccess,
						kind     = Func.Kind.Operator,
						body     = IsEmptyFunctionBody( opBody ) ? null : opBody,
						Requires = VisitTypespecsNested( c.typespecsNested() ),
						paras    = new() { param },
						retType  = retType,
					};
					//ret = VisitOpSpecialAssign( c );
				}
				else if( c.CONVERT() != null ) {
					ret = BuildConvertOperator(
						c.ToSrcPos(),
						VisitTypespec( c.typespec() ),
						VisitTplParams( c.tplParams() ),
						VisitTypespecsNested( c.typespecsNested() ),
						c.funcBody() );
				}
				else if( c.opSymbol() != null ) {
					string opName = "operator" + c.opSymbol().GetText();

					if( IsUnsupportedOperator( opName ) ) {
						Context.Diagnostics.Add( new Diagnostic(
							c.ToSrcPos(),
							DiagnosticKind.Error,
							String.Format( "Operator '{0}' is parsed but not yet supported by the C++ generator.", opName ) ) );
					}

					ret = VisitDefCoreFunc(
						c.defCoreFunc(),
						opName,
						Func.Kind.Operator,
						VisitTypespecsNested( c.typespecsNested() ) );

					if( IsComparisonOperator( opName )
					 && ret.retType is TypespecBasic { kind: TypespecBasic.Kind.ImplicitAuto } ) {
						ret.retType = opName == "operator<=>"
							? new TypespecBasic { kind = TypespecBasic.Kind.ExplicitAuto, size = TypespecBasic.SizeUndetermined }
							: new TypespecBasic { kind = TypespecBasic.Kind.Bool, size = 1 };
					}

					AddParamsToScope( ret.paras );
					ret.body = c.funcBody().Visit( Context );
					if( IsEmptyFunctionBody( ret.body ) )
						ret.body = null;
				}
				else {
					throw new NotSupportedException( "Unknown operator definition form." );
				}
			}
			ret.funcScope = scopeStack.Peek();
			PopScope();
			AddChild( ret ); // needs ret.name to be set already
			c.id()?.Visit();
			return ret;
		}

		private static bool IsUnsupportedOperator( string opName )
		{
			return opName switch {
				"operator**"   => true,
				"operator?:"   => true,
				"operator**="  => true,
				"operator?="   => true,
				_              => false,
			};
		}

		private static bool IsComparisonOperator( string opName )
		{
			return opName switch {
				"operator<"    => true,
				"operator>"    => true,
				"operator<="   => true,
				"operator>="   => true,
				"operator=="   => true,
				"operator!="   => true,
				"operator<=>"  => true,
				_              => false,
			};
		}

		// no override
		public Func VisitDefFunc( DefFuncContext c, Func.Kind kind )
		{
			PushScope();

			Scope funcScope = scopeStack.Peek();

			Func ret = VisitDefCoreFunc(
				c.defCoreFunc(),
				c.id().Visit(),
				kind,
				VisitTypespecsNested( c.typespecsNested() ) );

			ret.funcScope = funcScope;

			AddParamsToScope( ret.paras );
			MultiStmt body = c.funcBody().Visit( Context );
			if( IsEmptyFunctionBody( body ) ) {
				ret.IsForwardDeclaration = true;
				ret.body = null;
			}
			else {
				ret.body = body;
			}

			PopScope();
			AddChild( ret );

			return ret;
		}

		private static bool IsEmptyFunctionBody( MultiStmt body )
		{
			// An empty body is only a forward declaration if the function was
			// written with a bare semicolon body (func f();), not with empty
			// braces (func f() {}).
			return body.stmts.Count == 1 && body.stmts[0] is EmptyStmt;
		}

		// no override
		public Func VisitDefCoreFunc(
			DefCoreFuncContext   c,
			string               name,
			Func.Kind            kind,
			List<TypespecNested> requires )
		{
			Func ret = new() {
				srcPos    = c.ToSrcPos(),
				name      = name,
				access    = curAccess,
				kind      = kind,
				TplParams = VisitTplParams( c.tplParams() ),
				TplSpec   = VisitTplSpec( c.tplSpec() ),
				Requires  = requires,
				paras     = VisitFuncTypeDef( c.funcTypeDef() ).ToList(),
			};
			if( c.typespec() != null ) {
				ret.retType = VisitTypespec( c.typespec() );
			}
			else {
				ret.retType = new TypespecBasic {
					kind = TypespecBasic.Kind.ImplicitAuto,
					size = TypespecBasic.SizeUndetermined,
				};
			}
			return ret;
		}

		// no override
		// list of typed and initialized vars
		public MultiDecl VisitDefVar( DefVarContext c, VarDecl.Kind kind )
		{
			Scope  parent = scopeStack.Peek();
			SrcPos srcPos = c.ToSrcPos();
			TypespecContext sharedTypeCtx = c.typedIdAcors().typespec();

			List<Decl> decls = new();
			foreach( IdAccessorContext q in c.typedIdAcors().idAccessors().idAccessor() ) {
				List<TplParam> tplParams = VisitTplParams( q.tplParams() );
				bool isTemplate = tplParams.Count >= 1;
				bool isSpec = q.tplSpec() != null;

				// Variable template parameters and specializations need their own scope:
				// parameters for resolution, unbound parameters for specializations
				// (filled by the SpecTransformer once the primary is known).
				Scope? varTplScope = null;
				if( isTemplate || isSpec ) {
					PushScope();
					varTplScope = scopeStack.Peek();
				}

				Typespec type = VisitTypespec( sharedTypeCtx );
				if( kind.ToQualifier() == Qualifier.Const ) {
					type.qual |= Qualifier.Const;
				}

				List<TplBinding> tplSpec = isSpec
					? VisitTplSpec( q.tplSpec() )
					: new();

				bool isDirectConstruct = q.funcCall() != null;
				Expr? init = isDirectConstruct
					? new FuncCallExpr {
						srcPos     = srcPos,
						expr       = TypespecToExpr( type, srcPos ),
						funcCall   = VisitFuncCall( q.funcCall() ),
					}
					: q.expr()?.Visit( Context );

				// The synthesized constructor call participates in resolution:
				// CTAD deduces the template arguments for bare types. Only the
				// direct-construction form needs this entry: copy-init
				// constructor calls (var Box e = Box( 9 )) are real registered
				// expressions that the IdExpr/scoped CTAD branches pick up.
				if( isDirectConstruct && init is FuncCallExpr directCall )
					Context.UnresolvedCalls.Add( new UnresolvedCall( directCall.expr, directCall.funcCall, scopeStack.Peek(), true ) );

				MarkCtadPending( type, init, isDirectConstruct );

				if( varTplScope != null )
					PopScope();

				VarDecl decl = new() {
					srcPos             = srcPos,
					name               = q.id().GetText(),
					kind               = kind,
					access             = curAccess,
					type               = type,
					init               = init,
					isDirectConstruct  = isDirectConstruct,
					accessor           = q.accessorDef().Visit( Context ),
					TplParams          = tplParams,
					TplSpec            = tplSpec,
					varScope           = varTplScope,
				};
				decls.Add( decl );
			}

			AddChildren( decls );
			return new MultiDecl( decls );
		}

		private List<TplBinding> VisitTplSpec( TplSpecContext? c )
		{
			if( c == null )
				return new();

			return c.tplSpecEntry()
				.Select( e => new TplBinding {
					name = ( e.tplBind() != null ? e.tplBind().id() : e.id() ).Visit(),
					// null type marks an unbound placeholder (the "name everything" form)
					type = e.tplBind() != null && e.tplBind().typespec() != null ? VisitTypespec( e.tplBind().typespec() ) : null,
					lit  = e.tplBind() != null && e.tplBind().expr() != null ? e.tplBind().expr().Visit( Context ) : null,
				} )
				.ToList();
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
