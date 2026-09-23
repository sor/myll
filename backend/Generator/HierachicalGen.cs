using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Generator
{
	using static String;
	using static StmtFormatting;

	using ISourceLines      = IEnumerable<SourceLine>;
	using AccessSourceLines  = List<(Access access, SourceLines gen)>;
	using IAccessSourceLines = IEnumerable<(Access access, SourceLines gen)>;

	[Flags]
	public enum GenerateAt
	{
		Nowhere    = 0,
		Decl       = 1 << 0,
		Impl       = 1 << 1,
		Everywhere = Decl | Impl,
	}

	internal class PPPSourceLines
	{
		public readonly SourceLines
			priv = new(),
			prot = new(),
			pub  = new();

		public SourceLines Target( Access access )
			=> access switch {
				Access.Private   => priv,
				Access.Protected => prot,
				_                => pub,
			};
	}

	internal static class Extensions
	{
		internal static IAccessSourceLines Concat( this IAccessSourceLines self, PPPSourceLines pppSourceLines )
			=> self.Concat(
				new AccessSourceLines {
					(Access.Private,   pppSourceLines.priv),
					(Access.Protected, pppSourceLines.prot),
					(Access.Public,    pppSourceLines.pub),
				} );
	}

	/**
		HierarchicalGen

		Structure of output (using abbreviations: ppp = private, protected, public):

			ppp grouped	proto: early (hierarcs) and late (funcs)
			ppp ordered	types/alias

			static:
				ppp ordered	fieldList
				ppp grouped	accessors
				ppp grouped	operators
				ppp grouped	methods

			ppp ordered	fieldList
			ppp grouped	ctors
			ppp one		dtor
			ppp grouped	accessors
			ppp grouped	operators
			ppp grouped	methods
	*/
	public class HierarchicalGen
	{
		/// Fields are special, they can not be grouped by ppp or sorted
		private readonly AccessSourceLines
			constexprDecl   = new(),
			staticFieldDecl = new(),
			staticFieldImpl = new(),
			fieldDecl       = new(),
			fieldImpl       = new();

		// Super memory inefficient but I don't care for the moment
		private readonly PPPSourceLines
			protoEarly         = new(),
			protoLate          = new(),
			hierarchicalDecl   = new(),
			hierarchicalImpl   = new(),
			staticAccessorDecl = new(),
			staticAccessorImpl = new(),
			staticMethodDecl   = new(),
			staticMethodImpl   = new(),
			ctorDecl           = new(),
			ctorImpl           = new(),
			dtorDecl           = new(), // you can only have one DTor,
			dtorImpl           = new(), // but this makes it simpler to generate the code
			accessorDecl       = new(),
			accessorImpl       = new(),
			operatorDecl       = new(),
			operatorImpl       = new(),
			methodDecl         = new(),
			methodImpl         = new();

		private readonly Access defaultAccess;

		private readonly Hierarchical hierarchical;

		private readonly IReadOnlyDictionary<string, IReadOnlyList<RawImport>>? moduleRawImports;
		private readonly IReadOnlySet<string>?                                  prototypeModules;

		private readonly HashSet<string> definedNamespaces         = new();
		private readonly HashSet<string> namespaceUsingNames       = new();
		private readonly HashSet<string> namespaceAliasTargetNames = new();

		private readonly ModuleContext? context;
		private Dialect Dialect => context?.Dialect ?? Dialect.Default;

		public List<Diagnostic> Diagnostics { get; } = new();

		private bool IsEntryPointForModule( Func obj )
			=> obj.name == "main"
			&& !obj.IsInStruct
			&& context?.IsMainModule == true
			&& (obj.kind == Func.Kind.Function || obj.kind == Func.Kind.Procedure);

		private static bool IsStdSpanType( TypespecNested nested )
		{
			if( nested.idTpls.Count != 2 )
				return false;
			if( nested.idTpls[0].id != "std" )
				return false;
			if( nested.idTpls[1].id != "span" )
				return false;
			return nested.idTpls[1].tplArgs.Count == 1;
		}

		private bool TryGetSpanMainInfo( Func obj, out string argvType, out string argName, out string localType )
		{
			argvType = "";
			argName  = "";
			localType = "";

			if( obj.paras.Count != 1 )
				return false;

			Param param = obj.paras[0];
			if( param.type is not TypespecNested nested )
				return false;
			if( !IsStdSpanType( nested ) )
				return false;

			Typespec element = nested.idTpls[1].tplArgs[0].typespec;
			if( element == null
			 || element.ptrs == null
			 || element.ptrs.Count != 1
			 || element.ptrs[0].kind != Pointer.Kind.PtrToAry )
				return false;

			if( element is not TypespecBasic basic
			 || basic.kind != TypespecBasic.Kind.Char
			 || basic.size != 1 )
				return false;

			bool isConst = (basic.qual & Qualifier.Const) != 0
			            || (element.ptrs[0].qual & Qualifier.Const) != 0;

			argvType  = isConst ? "const char*" : "char*";
			argName   = param.name ?? "args";
			localType = "std::span<" + element.Gen() + ">";
			return true;
		}

		private void PrependDeclSourceMap( SourceLines target, Decl decl )
		{
			if( context?.SourceMap.Decl != true )
				return;

			SrcPos? pos = decl.srcPos;
			if( pos == null )
				return;

			string basePath = !IsNullOrEmpty( context.OutputBasePath )
				? context.OutputBasePath
				: Directory.GetCurrentDirectory();
			string mappedFile = Path.GetRelativePath( basePath, pos.file ).Replace( '\\', '/' );

			target.Add( Format( "#line {0} \"{1}\"", pos.from.line, mappedFile ) );
		}

		private int LevelDecl { get; }
		private int LevelImpl { get; }

		private string IndentDecl   => IndentString.Repeat( LevelDecl );
		private string IndentImpl   => IndentString.Repeat( LevelImpl );
		private string DeIndentDecl => IndentString.Repeat( LevelDecl - 1 );
		private string DeIndentImpl => IndentString.Repeat( LevelImpl - 1 );

		public HierarchicalGen(
			Hierarchical                                           obj,
			int                                                    levelDecl,
			int                                                    levelImpl,
			IReadOnlyDictionary<string, IReadOnlyList<RawImport>>? moduleRawImports = null,
			IReadOnlySet<string>?                                  prototypeModules = null,
			ModuleContext?                                         context = null )
		{
			hierarchical          = obj; // this is 'myself'
			LevelDecl             = levelDecl;
			LevelImpl             = levelImpl;
			this.moduleRawImports = moduleRawImports;
			this.prototypeModules = prototypeModules;
			this.context          = context;

			defaultAccess = obj.defaultAccess;
		}

		public ISourceLines GenDeclGlobal()
		{
			// throw if not global
			GlobalNamespace globalNS = (GlobalNamespace) hierarchical;
			ISet<string> implicitImports = new HashSet<string>( StdBuiltins.ImplicitStdImports );

			SourceLines includes = new();
			HashSet<string> seen = new();

			void AddRawImport( RawImport raw )
			{
				string include = raw.kind == RawImportKind.System
					? Format( "#include <{0}>", raw.path )
					: Format( "#include \"{0}\"", raw.path );

				if( seen.Add( include ) )
					includes.Add( include );
			}

			void AddModuleRawImports( string moduleName )
			{
				if( moduleRawImports == null )
					return;
				if( !moduleRawImports.TryGetValue( moduleName, out IReadOnlyList<RawImport>? raws ) )
					return;
				foreach( RawImport raw in raws )
					AddRawImport( raw );
			}

			void AddGeneratedModuleInclude( string moduleName )
			{
				string include = Format( "#include \"{0}.hpp\"", moduleName );

				if( seen.Add( include ) )
					includes.Add( include );
			}

			void AddModuleIncludes( string moduleName )
			{
				if( prototypeModules != null && prototypeModules.Contains( moduleName ) )
					AddModuleRawImports( moduleName );
				else
					AddGeneratedModuleInclude( moduleName );
			}

			// Implicit standard imports in a fixed, stable order.
			foreach( string imp in StdBuiltins.ImplicitStdImports ) {
				if( imp == globalNS.module )
					continue;

				bool hasModule =
					( moduleRawImports != null && moduleRawImports.ContainsKey( imp ) )
				 || ( prototypeModules != null && prototypeModules.Contains( imp ) );

				if( !hasModule ) {
					Diagnostics.Add( new Diagnostic(
						null,
						DiagnosticKind.Error,
						Format( "Implicit standard module '{0}' is not part of the compilation.", imp ) ) );
					continue;
				}

				AddModuleIncludes( imp );
			}

			// User imports in source order (raw imports and generated includes for imported modules).
			foreach( ImportSpec spec in globalNS.imports ) {
				switch( spec ) {
				case NamedImportSpec named:
					if( named.name == globalNS.module )
						continue;
					if( implicitImports.Contains( named.name ) )
						continue; // already emitted above
					AddModuleIncludes( named.name );
					break;

				case RawImportSpec rawSpec:
					AddRawImport( rawSpec.raw );
					break;
				}
			}

			ISourceLines declList = GenDecl();
			ISourceLines decl = new SourceLines { PragmaOnce }
				.Concat( includes )
				.Concat( declList );

			return decl;
		}

		// TODO: convert to ISourceLines if too slow
		private SourceLines GenDecl()
		{
			Access curAccess    = defaultAccess;
			string accessIndent = DeIndentDecl;

			SourceLines ret = new();

			foreach( string ns in namespaceAliasTargetNames
				.Where( n => definedNamespaces.Contains( n ) )
				.OrderBy( n => n ) ) {
				ret.Add( Format( "{0}namespace {1} {{}}", IndentDecl, ns ) );
			}

			if( Dialect.GlobalUsingNS == GlobalUsingNSMode.Leaky
			 && namespaceUsingNames.Count > 0 ) {
				foreach( string ns in namespaceUsingNames
					.Where( n => definedNamespaces.Contains( n ) )
					.OrderBy( n => n ) ) {
					ret.Add( Format( "{0}namespace {1} {{}}", IndentDecl, ns ) );
				}
			}

			IAccessSourceLines
				listDecl = new AccessSourceLines()
					.Concat( protoEarly )
					.Concat( constexprDecl )
					.Concat( hierarchicalDecl )
					.Concat( staticFieldDecl )
					.Concat( staticAccessorDecl )
					.Concat( staticMethodDecl )
					.Concat( fieldDecl )
					.Concat( protoLate )
					.Concat( ctorDecl )
					.Concat( dtorDecl )
					.Concat( accessorDecl )
					.Concat( operatorDecl )
					.Concat( methodDecl );

			foreach( (Access access, SourceLines gen) in listDecl ) {
				if( gen.Count == 0 )
					continue;

				if( access != curAccess )
					ret.Add( Format( AccessFormat[access], accessIndent ) );

				curAccess = access;

				ret.AddRange( gen );
			}

			return ret;
		}

		public ISourceLines? GenImplGlobal()
		{
			SourceLines implList = GenImpl();
			if( implList.Count != 0 ) {
				GlobalNamespace globalNS = (GlobalNamespace) hierarchical;
				ISourceLines        impl     = implList.Prepend( Format( "#include \"{0}.hpp\"", globalNS.module ) );
				return impl;
			}
			else {
				return null; // do not generate file
			}
		}

		// TODO: convert to ISourceLines if too slow
		private SourceLines GenImpl()
		{
			SourceLines ret = new();
			IAccessSourceLines
				listImpl = new AccessSourceLines()
					.Concat( hierarchicalImpl )
					.Concat( staticFieldImpl )
					.Concat( staticAccessorImpl )
					.Concat( staticMethodImpl )
					.Concat( fieldImpl )
					.Concat( ctorImpl )
					.Concat( dtorImpl )
					.Concat( accessorImpl )
					.Concat( operatorImpl )
					.Concat( methodImpl );

			foreach( (Access access, SourceLines gen) in listImpl ) {
				if( gen.Count == 0 )
					continue;

				ret.AddRange( gen );
			}

			return ret;
		}

		// Those need to be kept in adding order
		public void AddEntry( EnumEntry obj )
		{
			string        indent     = IndentDecl;
			string        name       = obj.name;
			AccessSourceLines targetDecl = obj.IsStatic ? staticFieldDecl : fieldDecl;
			SourceLines ret = new() {
				Format(
					EntryFormat[0],
					indent,
					name,
					obj.value != null ? EntryFormat[1] + obj.value.Gen() : "" )
			};
			PrependDeclSourceMap( ret, obj );
			targetDecl.Add( (obj.access, ret) );
		}

		public void AddUsing( UsingDecl obj )
		{
			string indent = IndentDecl;

			string ret = Format(
				UsingFormat[obj.IsNamespaceUsing ? 1 : 0],
				indent,
				obj.type.Gen() );

			protoEarly.Target( obj.access ).Add( ret );

			if( obj.IsNamespaceUsing )
				namespaceUsingNames.Add( obj.name );
		}

		public void AddAlias( AliasDecl obj )
		{
			string indent = IndentDecl;

			string targetName;
			if( obj.IsNamespaceAlias ) {
				targetName = obj.type.Gen();
				namespaceAliasTargetNames.Add( targetName );
			}
			else {
				targetName = obj.type.Gen();
			}

			string ret = Format(
				AliasFormat[obj.IsNamespaceAlias ? 1 : 0],
				indent,
				obj.name,
				targetName );

			protoEarly.Target( obj.access ).Add( ret );
		}

		// Those need to be kept in adding order
		public void AddVar( VarDecl obj )
		{
			// Dependent nested types emit 'typename ' from TypespecNested.GenType().
			// The VarFormat typename slot is intentionally left empty here to avoid
			// a duplicate prefix.
			bool needsTypename = false;

			bool isInsideStruct   = obj.IsInStruct;
			bool isStatic         = obj.IsStatic;
			bool isHidden         = obj.IsHidden;
			bool isCompileTime    = obj.IsCompileTime;
			bool isExplicitInline = obj.IsInline;
			bool isExplicitExtern = obj.IsExternal;
			bool isConstType      = (obj.type.qual & Qualifier.Const) != 0;
			bool isExtern         = !isInsideStruct
			                     && ( isExplicitExtern
			                       || !( isHidden || isExplicitInline || isCompileTime || isConstType ) );

			bool hadError = false;
			void Error( string message )
			{
				Diagnostics.Add( new Diagnostic( obj.srcPos, DiagnosticKind.Error, message ) );
				hadError = true;
			}

			if( obj.IsNoInit && ( isConstType || isCompileTime ) )
				Error( "[noinit]/[uninit] cannot be used with const or compile-time variables" );

			if( isInsideStruct ) {
				if( isHidden )                       Error( "[hide]/[hidden] is only valid at module/namespace scope." );
				if( isExplicitExtern )               Error( "[extern] is only valid at module/namespace scope." );
				if( isExplicitInline && !isStatic )  Error( "[inline] on a class field requires [static]." );
				if( isCompileTime && !isStatic )     Error( "[ct] on a class field requires [static]." );
			}
			else {
				if( isStatic )                       Error( "[static] is only valid on class fields; use [hide]/[hidden] for module variables." );
				if( isExplicitInline && isHidden )   Error( "[inline] and [hide] are mutually exclusive." );

				if( isExplicitExtern ) {
					if( isExplicitInline )           Error( "[extern] and [inline] are mutually exclusive." );
					if( isHidden )                   Error( "[extern] and [hide]/[hidden] are mutually exclusive." );
					if( isCompileTime )              Error( "[extern] cannot be used with [ct]." );
				}
				else if( isHidden ) {
					// nothing else to set
				}
				else if( isExplicitInline || isCompileTime || isConstType ) {
					// nothing else to set
				}
				else {
					// module-level variables are external by default unless hidden/inline/ct/const
				}
			}

			if( hadError )
				return;

			AccessSourceLines targetDecl = isStatic
				? staticFieldDecl
				: ( isCompileTime && !isInsideStruct )
				? constexprDecl
				: fieldDecl;
			AccessSourceLines targetImpl = isStatic ? staticFieldImpl : fieldImpl;

			bool       needsInline   = isExplicitInline
			                        || ( isInsideStruct && isStatic && isCompileTime );
			GenerateAt emitAt        = GenerateAt.Decl;
			GenerateAt initIn        = GenerateAt.Decl;

			if( isInsideStruct ) {
				if( isStatic && !needsInline ) {
					emitAt = GenerateAt.Everywhere;
					initIn = GenerateAt.Impl;
				}
			}
			else {
				if( isExplicitExtern ) {
					initIn = GenerateAt.Nowhere;
				}
				else if( isHidden ) {
					emitAt = GenerateAt.Impl;
					initIn = GenerateAt.Impl;
				}
				else if( isExplicitInline || isCompileTime || isConstType ) {
					// nothing else to set
				}
				else {
					emitAt = GenerateAt.Everywhere;
					initIn = GenerateAt.Impl;
				}
			}

			string directArgs = obj.isDirectConstruct
				? ( ( obj.init as FuncCallExpr )?.funcCall ) is FuncCall fc
					&& fc.args.Count > 0
					? fc.Gen()
					: VarEmptyInitFormat
				: "";

			string initDecl = ( initIn & GenerateAt.Decl ) == 0 ? ""
			                : obj.isDirectConstruct             ? directArgs
			                : obj.init != null                  ? VarFormat[6] + obj.init.Gen()
			                : obj.IsNoInit || isExtern          ? ""
			                : VarEmptyInitFormat;

			string initImpl = ( initIn & GenerateAt.Impl ) == 0 ? ""
			                : obj.isDirectConstruct             ? directArgs
			                : obj.init != null                  ? VarFormat[6] + obj.init.Gen()
			                : obj.IsNoInit                      ? ""
			                : VarEmptyInitFormat;

			bool hasTpl = obj.TplParams.Count >= 1;
			string tplDeclLine = hasTpl
				? Format(
					"{0}template <{1}>",
					IndentDecl,
					obj.TplParams.Select( p => "typename " + p.name ).Join( ", " ) )
				: string.Empty;
			string tplImplLine = hasTpl
				? Format(
					"{0}template <{1}>",
					IndentImpl,
					obj.TplParams.Select( p => "typename " + p.name ).Join( ", " ) )
				: string.Empty;

			// 0 indent, 1 extern, 2 inline, 3 static, 4 constexpr, 5 typename, 6 type & name, 7 init
			if( (emitAt & GenerateAt.Decl) != 0 ) {
				SourceLines retDecl = new();
				if( hasTpl )
					retDecl.Add( tplDeclLine );
				retDecl.Add( Format(
					VarFormat[0],
					IndentDecl,
					isExtern             ? VarFormat[1] : "",
					needsInline          ? VarFormat[2] : "",
					isStatic || isHidden ? VarFormat[3] : "",
					isCompileTime        ? VarFormat[4] : "",
					needsTypename        ? VarFormat[5] : "",
					obj.type.Gen( obj.name ),
					initDecl ) );
				PrependDeclSourceMap( retDecl, obj );
				targetDecl.Add( (obj.access, retDecl) );
			}

			if( (emitAt & GenerateAt.Impl) != 0 ) {
				SourceLines retImpl = new();
				if( hasTpl )
					retImpl.Add( tplImplLine );
				retImpl.Add( Format(
					VarFormat[0],
					IndentImpl,
					"",
					"",
					isHidden      ? VarFormat[3] : "",
					"",
					needsTypename ? VarFormat[5] : "",
					obj.type.Gen( obj.FullyQualifiedName ),
					initImpl ) );
				PrependDeclSourceMap( retImpl, obj );
				targetImpl.Add( (obj.access, retImpl) );
			}
		}

		public void AddFunc( Func obj )
		{
			List<TplParam> tplParams = obj.TplParams;

			bool    hasTpl         = tplParams.Count >= 1;
			bool    isInlined      = obj.IsInlined;
			bool    isInsideStruct = obj.IsInStruct;
			bool    isStatic       = obj.IsStatic;
			bool    isExternal     = obj.IsExternal;
			string  indentDecl     = IndentDecl;
			string  indentImpl     = IndentImpl;
			string  nameDecl       = obj.name;
			string  nameImpl       = obj.FullyQualifiedName;
			SourceLines targetDecl     = (isStatic ? staticMethodDecl : methodDecl).Target( obj.access );
			SourceLines targetImpl     = (isStatic ? staticMethodImpl : methodImpl).Target( obj.access );
			SourceLines targetProto    = isInsideStruct ? targetDecl : protoLate.Target( obj.access );

			string paramString = obj.paras
				.Select( p => p.Gen() )
				.Join( ", " );

			bool isAbstract = obj.IsAbstract;
			bool isDefault  = obj.IsDefault;
			bool isDisabled = obj.IsDisabled;
			string specialTail = isAbstract ? " = 0"
			                   : isDefault  ? " = default"
			                   : isDisabled ? " = delete"
			                   : "";
			bool isSpecial = specialTail != "";

			if( isDefault && !isInsideStruct ) {
				Diagnostics.Add( new Diagnostic(
					obj.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"[default] is only valid on special member functions; '{0}' is a free function",
						obj.FullyQualifiedName ) ) );
				return;
			}

			string prefix = (obj.IsNodiscard ? "[[nodiscard]] " : "")
		              + (obj.IsCompileTime ? "constexpr " : "")
		              + (isStatic          ? "static "    : "")
		              + (isExternal        ? "extern "    : "")
		              + (obj.IsVirtual || isAbstract ? "virtual "   : "")
		              + (isInlined         ? "inline "    : "");
			string declSuffix = (obj.IsConstMethod ? " const"    : "")
		              + (obj.IsNoexcept  ? " noexcept" : "")
		              + (obj.IsOverride    ? " override" : "");
			string implSuffix = (obj.IsConstMethod ? " const" : "")
		              + (obj.IsNoexcept  ? " noexcept" : "");

			bool isMainEntry = IsEntryPointForModule( obj );
			string argvType = "";
			string argName  = "";
			string localType = "";
			bool isSpanMain  = isMainEntry
			                && TryGetSpanMainInfo( obj, out argvType, out argName, out localType );
			bool emitPrototype = !isMainEntry;

			if( isSpanMain )
				paramString = Format( "int myll_argc, {0} myll_argv[]", argvType );

		if( obj.kind == Func.Kind.Convert ) {
			AddConversionOperator( obj, prefix, declSuffix, hasTpl, isInlined, isExternal, isInsideStruct, targetDecl, targetImpl, targetProto );
			return;
		}

		string headlineDecl = Format(

				FuncFormat[0],
				indentDecl,
				prefix,
				obj.retType.Gen( nameDecl ),
				paramString,
				declSuffix );

			if( isAbstract && obj.IsVirtual )
				Diagnostics.Add( new Diagnostic(
					obj.srcPos,
					DiagnosticKind.Warning,
					String.Format( "[virtual] is redundant on an abstract method '{0}'", obj.FullyQualifiedName ) ) );

			// TODO add the surrounding templates as well for tplImpl
			string tplDecl, tplImpl;
			if( hasTpl ) {
				string tplContent = tplParams
					.Select( o => "typename " + o.name )
					.Join( ", " );

				tplDecl = Format(
					"{0}template <{1}>",
					indentDecl,
					tplContent );

				tplImpl = Format(
					"{0}template <{1}>",
					indentImpl,
					tplContent );
			}
			else {
				tplDecl = string.Empty;
				tplImpl = string.Empty;
			}

			if( isSpecial ) {
				if( obj.body != null ) {
					Diagnostics.Add( new Diagnostic(
						obj.srcPos,
						DiagnosticKind.Error,
						String.Format( "Special function '{0}' (abstract/default/delete) must not have a body", obj.FullyQualifiedName ) ) );
					return;
				}

				if( isAbstract && !isInsideStruct ) {
					Diagnostics.Add( new Diagnostic(
						obj.srcPos,
						DiagnosticKind.Error,
						String.Format( "Abstract method '{0}' must be inside a class or struct", obj.FullyQualifiedName ) ) );
					return;
				}

				if( isSpecial && ( isInlined || isExternal || isStatic ) ) {
					Diagnostics.Add( new Diagnostic(
						obj.srcPos,
						DiagnosticKind.Error,
						String.Format( "Special function '{0}' cannot be combined with [inline], [extern] or [static]", obj.FullyQualifiedName ) ) );
					return;
				}

				if( emitPrototype ) {
					PrependDeclSourceMap( targetProto, obj );
					if( hasTpl )
						targetProto.Add( tplDecl );

					targetProto.Add( headlineDecl + specialTail + ";" );
				}
				return;
			}


			// TODO: move inlines to bottom of header
			if( isInlined || isExternal || hasTpl ) {
				if( !isInsideStruct ) {
					if( emitPrototype ) {
						PrependDeclSourceMap( targetProto, obj );
						if( hasTpl )
							targetProto.Add( tplDecl );
						targetProto.Add( headlineDecl + ";" );
					}
				}
				else if( isExternal ) {
					// External members still need a declaration inside the class definition,
					// and it should get its own source-map directive.
					PrependDeclSourceMap( targetDecl, obj );
					if( hasTpl )
						targetDecl.Add( tplDecl );
					targetDecl.Add( headlineDecl + ";" );
				}

				if( !isExternal ) {
					if( obj.body == null ) {
						Diagnostics.Add( new Diagnostic(
							obj.srcPos,
							DiagnosticKind.Error,
							String.Format( "Non-external function '{0}' must have a body", obj.FullyQualifiedName ) ) );
						return;
					}

				PrependDeclSourceMap( targetDecl, obj );
				if( hasTpl )
					targetDecl.Add( tplDecl );

				MultiStmt bodyToGen = obj.body;
				if( isSpanMain ) {
					Stmt init = new FreetextStmt( Format( "{0} {1}( myll_argv, myll_argc );", localType, argName ) );
					bodyToGen = new MultiStmt( new List<Stmt> { init }.Concat( obj.body.stmts ), true );
				}

				targetDecl.Add( headlineDecl );
				targetDecl.AddRange( bodyToGen.Gen( LevelDecl ) );

				}
			}
			else {
				if( obj.body == null ) {
					Diagnostics.Add( new Diagnostic(
						obj.srcPos,
						DiagnosticKind.Error,
						String.Format( "Non-external function '{0}' must have a body", obj.FullyQualifiedName ) ) );
					return;
				}

				string headlineImpl = Format(
					FuncFormat[0],
					indentImpl,
					"",
					obj.retType.Gen( nameImpl ),
					paramString,
					implSuffix );

				if( emitPrototype ) {
					PrependDeclSourceMap( targetProto, obj );
					if( hasTpl )
						targetProto.Add( tplDecl );

					targetProto.Add( headlineDecl + ";" );
				}

				PrependDeclSourceMap( targetImpl, obj );
				if( hasTpl )
					targetImpl.Add( tplImpl );

				MultiStmt bodyToGen = obj.body;
				if( isSpanMain ) {
					Stmt init = new FreetextStmt( Format( "{0} {1}( myll_argv, myll_argc );", localType, argName ) );
					bodyToGen = new MultiStmt( new List<Stmt> { init }.Concat( obj.body.stmts ), true );
				}

				targetImpl.Add( headlineImpl );
				targetImpl.AddRange( bodyToGen.Gen( LevelImpl ) );
			}
		}

		private void AddConversionOperator(
			Func         obj,
			string       prefix,
			string       declSuffix,
			bool         hasTpl,
			bool         isInlined,
			bool         isExternal,
			bool         isInsideStruct,
			SourceLines  targetDecl,
			SourceLines  targetImpl,
			SourceLines  targetProto )
		{
			string indentDecl = IndentDecl;
			string indentImpl = IndentImpl;
			string targetType = obj.retType.GenType();
			string explicitKw = obj.IsImplicit ? "" : "explicit ";
			string declHeadline = Format(
				"{0}{1}{2}operator {3}(){4}",
				indentDecl,
				prefix,
				explicitKw,
				targetType,
				declSuffix );

			string tplDecl, tplImpl;
			if( hasTpl ) {
				string tplContent = obj.TplParams
					.Select( o => "typename " + o.name )
					.Join( ", " );

				tplDecl = Format( "{0}template <{1}>", indentDecl, tplContent );
				tplImpl = Format( "{0}template <{1}>", indentImpl, tplContent );
			}
			else {
				tplDecl = string.Empty;
				tplImpl = string.Empty;
			}

			string qual = obj.FullyQualifiedName;
			if( !qual.EndsWith( "::" ) )
				qual += "::";
			string implHeadline = Format(
				"{0}{1}operator {2}(){3}",
				indentImpl,
				qual,
				targetType,
				declSuffix );

			if( isInlined || isExternal || hasTpl ) {
				if( !isInsideStruct ) {
					Diagnostics.Add( new Diagnostic(
						obj.srcPos,
						DiagnosticKind.Error,
						"Conversion operators must be defined inside a class or struct" ) );
					return;
				}

				if( isExternal ) {
					PrependDeclSourceMap( targetDecl, obj );
					if( hasTpl )
						targetDecl.Add( tplDecl );
					targetDecl.Add( declHeadline + ";" );
				}
				else {
					if( obj.body == null ) {
						Diagnostics.Add( new Diagnostic(
							obj.srcPos,
							DiagnosticKind.Error,
							"Non-external conversion operator must have a body" ) );
						return;
					}

					PrependDeclSourceMap( targetDecl, obj );
					if( hasTpl )
						targetDecl.Add( tplDecl );

					targetDecl.Add( declHeadline );
					targetDecl.AddRange( obj.body.Gen( LevelDecl ) );
				}
			}
			else {
				if( obj.body == null ) {
					Diagnostics.Add( new Diagnostic(
						obj.srcPos,
						DiagnosticKind.Error,
						"Non-external conversion operator must have a body" ) );
					return;
				}

				PrependDeclSourceMap( targetProto, obj );
				if( hasTpl )
					targetProto.Add( tplDecl );

				targetProto.Add( declHeadline + ";" );

				PrependDeclSourceMap( targetImpl, obj );
				if( hasTpl )
					targetImpl.Add( tplImpl );

				targetImpl.Add( implHeadline );
				targetImpl.AddRange( obj.body.Gen( LevelImpl ) );
			}
		}

		// this adds an hierarchical as child
		public void AddHierarchical( Hierarchical obj )
		{
			if( obj.IsStatic )
				throw new ArgumentOutOfRangeException( nameof( obj ), true, "Hierarchicals can not be static" );

			// this is a sub-gen for the child-hierarchical obj
			HierarchicalGen gen = new( obj, LevelDecl + 1, LevelImpl, context: context );

			// this happens inside the children, each knows which method to call
			// e.g.: g.AddAccessor( ... );

			obj.children.ForEach( c => c.AddToGen( gen ) );

			if( gen.hierarchical is Structural structural )
				AddHiddenBaseMethodUsings( structural, gen );

			Diagnostics.AddRange( gen.Diagnostics );

			string  indent      = gen.DeIndentDecl;
			string  nameDecl    = gen.hierarchical.name;
			SourceLines targetProto = protoEarly.Target( obj.access );
			SourceLines targetDecl  = hierarchicalDecl.Target( obj.access );
			SourceLines targetImpl  = hierarchicalImpl.Target( obj.access );

			bool isGlobal    = gen.hierarchical is GlobalNamespace;
			bool isNamespace = gen.hierarchical is Namespace;
			bool isEnum      = gen.hierarchical is Enumeration;

			if( !isGlobal ) {
				PrependDeclSourceMap( targetProto, obj );
				PrependDeclSourceMap( targetDecl, obj );
			}

			if( gen.hierarchical is ITemplatedDecl hierWithTpl
			 && hierWithTpl.TplParams.Count >= 1 ) {
				string tpl = Format(
					"{0}template <{1}>",
					indent,
					hierWithTpl.TplParams
						.Select( o => "typename " + o.name )
						.Join( ", " ) );

				targetProto.Add( tpl );
				targetDecl.Add( tpl );
			}

			if( isNamespace )
				definedNamespaces.Add( nameDecl );

			string keyword, bases;
			if( isNamespace ) {
				keyword = StructFormat[7];
				bases   = "";
			}
		else if( gen.hierarchical is Enumeration objEnum ) {
			keyword = StructFormat[6];
			bases = (objEnum.baseType != null)
				? Format( StructFormat[1], "", objEnum.baseType.GenType() )
				: "";
		}
		else if( gen.hierarchical is Structural objStruct ) {
			keyword = objStruct.kind switch {
				Structural.Kind.Struct => StructFormat[3],
				Structural.Kind.Class  => StructFormat[4],
				Structural.Kind.Union  => StructFormat[5],
				_                      => throw new Exception( Format( "no correct keyword determined: {0}", objStruct ) ),
			};

			string BasePrefix( BaseType b )
				=> (b.isVirtual ? "virtual " : "")
				+ b.access switch {
					Access.Private   => "private ",
					Access.Protected => "protected ",
					_                => "public ",
				};

			string BaseTypeName( BaseType b )
			{
				if( b.type is TypespecNested nested )
					return nested.GenType( false );

				return b.type.GenType();
			}

			bases = (objStruct.basetypes.Count < 1)
				? ""
				: Format( StructFormat[1], BasePrefix( objStruct.basetypes[0] ), BaseTypeName( objStruct.basetypes[0] ) )
				  + objStruct.basetypes
					.Skip( 1 )
					.Select( t => Format( StructFormat[2], BasePrefix( t ), BaseTypeName( t ) ) )
					.Join( "" );
		}
			else {
				throw new InvalidOperationException( "not an enum and not a struct" );
			}

			// "{0}{1}{2} {3}{4}{5}",
			// 0 indent, 1 keyword, 2 attributes, 3 name, 4 final, 5 bases
			if( !isGlobal ) {
				if( !isNamespace ) {
					targetProto.Add(
						Format(
							StructFormat[0],
							indent,
							keyword,
							"",
							nameDecl,
							"",
							(isEnum ? bases : "") + ";" ) );
				}
				targetDecl.Add(
					Format(
						StructFormat[0],
						indent,
						keyword,
						"",
						nameDecl,
						"",
						bases ) );
				targetDecl.Add( Format( CurlyOpen, indent ) );

			if( gen.hierarchical is Structural objStruct ) {
			if( objStruct.basetypes.Count >= 1
			 && !String.IsNullOrEmpty( Dialect.BaseClassAliasName )
			 && !objStruct.children.Any( c => c.name == Dialect.BaseClassAliasName ) ) {
				targetDecl.Add(
					Format(
						AliasFormat[0],
						gen.IndentDecl,
						Dialect.BaseClassAliasName,
						objStruct.basetypes[0].type.GenType() ) );
			}

			if( !String.IsNullOrEmpty( Dialect.OwnClassAliasName )
			 && !objStruct.children.Any( c => c.name == Dialect.OwnClassAliasName ) ) {
				targetDecl.Add(
					Format(
						AliasFormat[0],
						gen.IndentDecl,
						Dialect.OwnClassAliasName,
						objStruct.name ) );
			}
			}
			}

			targetDecl.AddRange( gen.GenDecl() );

			if( !isGlobal )
				targetDecl.Add( Format( isNamespace ? CurlyClose : CurlyCloseSC, indent ) );

			targetImpl.AddRange( gen.GenImpl() );
		}

		/// <summary>
		/// If a derived class reintroduces a base method name with a different signature,
		/// C++ would hide the base overloads. Emit one or more C++ `using Base::name;`
		/// declarations so that overload resolution sees both the base and derived overloads.
		/// </summary>
		private static void AddHiddenBaseMethodUsings( Structural structural, HierarchicalGen gen )
		{
			List<Func> derivedFuncs = structural.children
				.OfType<Func>()
				.Where( f => !IsUnhidableSpecialMember( f ) )
				.ToList();

			if( derivedFuncs.Count == 0 )
				return;

			HashSet<string> processed = new();

			foreach( Func derived in derivedFuncs ) {
				string name = derived.name;
				if( !processed.Add( name ) )
					continue;

				List<(BaseType Base, Func Method)> baseMethods
					= FindBaseMethods( structural, name, new HashSet<Structural>(), gen );

				if( baseMethods.Count == 0 )
					continue;

				bool autoUnhide = ShouldAutoUnhide( derived, structural, gen.Dialect );

				if( baseMethods.Select( m => m.Method ).Distinct( ReferenceEqualityComparer.Instance ).Count() > 1 ) {
					if( autoUnhide ) {
						gen.Diagnostics.Add( new Diagnostic(
							derived.srcPos,
							DiagnosticKind.Warning,
							String.Format(
								"Method '{0}' is hidden by an overload in '{1}'; auto-unhiding is skipped because the name exists in multiple base classes. Consider using 'using Base::{0};' explicitly.",
								name,
								structural.name ) ) );
					}
					continue;
				}

				if( !autoUnhide )
					continue;

				Func baseMethod = baseMethods[0].Method;
				BaseType owningBase = baseMethods[0].Base;

				if( owningBase.access != Access.Public || baseMethod.access != Access.Public )
					continue;

				List<Func> derivedWithName = derivedFuncs
					.Where( f => f.name == name )
					.ToList();

				if( derivedWithName.Any( d => HasSameSignature( d, baseMethod ) ) )
					continue;

				string baseTypeName = owningBase.type.GenType();
				string usingLine = Format(
					"{0}using {1}::{2};",
					gen.IndentDecl,
					baseTypeName,
					name );

				gen.methodDecl.Target( Access.Public ).Add( usingLine );
			}
		}

		private static bool ShouldAutoUnhide( Func derived, Structural structural, Dialect dialect )
		{
			if( derived.IsUnshadow )
				return true;
			if( derived.IsShadow )
				return false;
			if( structural.IsUnshadow )
				return true;
			if( structural.IsShadow )
				return false;
			return dialect.AutoUnhideBaseMethods;
		}

		private static bool IsUnhidableSpecialMember( Func func )
			=> func.name == "operator=" || func.name.StartsWith( "~" );

		private static bool IsCopyOrMoveConstructor( Structor ctor )
		{
			if( ctor.kind != Structor.Kind.Constructor || ctor.paras.Count != 1 )
				return false;

			if( ctor.scope?.parent?.decl is not Structural parent )
				return false;

			Typespec type = ctor.paras[0].type;
			if( type is not TypespecNested nested )
				return false;

			bool nameMatches = nested.resolvedDecl == parent
				|| nested.idTpls.LastOrDefault()?.id == parent.name;
			if( !nameMatches )
				return false;

			if( nested.ptrs == null || nested.ptrs.Count != 1 )
				return false;

			Pointer ptr = nested.ptrs[0];
			if( ptr.kind != Pointer.Kind.LVRef && ptr.kind != Pointer.Kind.RVRef )
				return false;

			Qualifier qual = ptr.qual == Qualifier.None ? nested.qual : ptr.qual;
			bool isCopy = ptr.kind == Pointer.Kind.LVRef && qual == Qualifier.Const;
			bool isMove = ptr.kind == Pointer.Kind.RVRef && qual == Qualifier.None;
			return isCopy || isMove;
		}

		private static List<(BaseType Base, Func Method)> FindBaseMethods(
			Structural              structural,
			string                  name,
			HashSet<Structural>     visited,
			HierarchicalGen?        gen )
		{
			List<(BaseType Base, Func Method)> ret = new();

			if( !visited.Add( structural ) )
				return ret;

			foreach( BaseType bt in structural.basetypes ) {
				Structural? baseStruct = ResolveBaseStructural( bt, gen );
				if( baseStruct == null )
					continue;

				Func? direct = baseStruct.children
					.OfType<Func>()
					.FirstOrDefault( f => f.name == name && !IsUnhidableSpecialMember( f ) );

				if( direct != null ) {
					ret.Add( (bt, direct) );
				}
				else {
					ret.AddRange( FindBaseMethods( baseStruct, name, visited, gen )
						.Select( m => (bt, m.Method) ) );
				}
			}

			return ret;
		}

		private static Structural? ResolveBaseStructural( BaseType bt, HierarchicalGen? gen )
		{
			if( bt.type is not TypespecNested nested )
				return null;

			if( nested.resolvedDecl is Structural structural )
				return structural;

			Scope? moduleScope = gen?.hierarchical?.scope?.UpToGlobal;
			if( moduleScope == null )
				return null;

			string name = nested.idTpls.Last().id;
			if( !moduleScope.children.TryGetValue( name, out List<ScopeLeaf>? leaves ) )
				return null;

			foreach( ScopeLeaf leaf in leaves ) {
				if( leaf.HasDecl && leaf.decl is Structural s )
					return s;
			}

			return null;
		}

		private static bool HasSameSignature( Func a, Func b )
		{
			if( a.paras.Count != b.paras.Count )
				return false;

			for( int i = 0; i < a.paras.Count; i++ ) {
				if( !ConversionRules.IsExactMatch( a.paras[i].type, b.paras[i].type ) )
					return false;
			}

			return true;
		}

		public void AddForwardDecl( Structural structural )
		{
			string keyword = structural.kind switch {
				Structural.Kind.Struct => "struct",
				Structural.Kind.Class  => "class",
				Structural.Kind.Union  => "union",
				_                      => throw new InvalidOperationException( "unknown structural kind" ),
			};

			SourceLines target = protoEarly.Target( structural.access );
			string  indent = DeIndentDecl;

			PrependDeclSourceMap( target, structural );

			if( structural.TplParams.Count >= 1 ) {
				target.Add( Format(
					"{0}template <{1}>",
					indent,
					structural.TplParams
						.Select( t => "typename " + t.name )
						.Join( ", " ) ) );
			}

			target.Add( Format( "{0}{1} {2};", indent, keyword, structural.name ) );
		}

		public void AddStructor( Structor obj )
		{
			if( obj.IsStatic )
				throw new ArgumentOutOfRangeException( nameof( obj ), true, "Con/Destructor can not be static" );

			bool    isCtor     = obj.kind == Structor.Kind.Constructor;
			bool    isDtor     = obj.kind == Structor.Kind.Destructor;
			bool    isDefault  = obj.IsDefault;
			bool    isDisabled = obj.IsDisabled;
			bool    isInlined  = obj.IsInlined || isDefault || isDisabled;
			string  indentDecl = IndentDecl;
			string  indentImpl = IndentImpl;
			string  nameDecl   = obj.name;
			string  nameImpl   = obj.FullyQualifiedName;
			SourceLines targetDecl = (isCtor ? ctorDecl : dtorDecl).Target( obj.access );
			SourceLines targetImpl = (isCtor ? ctorImpl : dtorImpl).Target( obj.access );

			string paramString = obj.paras
				.Select( para => para.Gen() )
				.Join( ", " );

			string leadingAttrDecl = obj.IsNodiscard ? "[[nodiscard]] " : "";
			if( obj.paras.Count == 1 // TODO: 0, 1 or more parameterizable
					&& !obj.IsImplicit
					&& !IsCopyOrMoveConstructor( obj ) ) {
				leadingAttrDecl += "explicit ";
			}

			if( isDtor && obj.IsVirtual ) {
				leadingAttrDecl += "virtual ";
			}

			string followingDecl =
				(obj.IsNoexcept ? " noexcept" : "")
				+ (isDefault  ? " = default;" :
				   isDisabled ? " = delete;" :
				                "");

			string headlineDecl = Format(
				FuncFormat[1],
				indentDecl,
				leadingAttrDecl,
				nameDecl,
				paramString,
				followingDecl );

			if( isInlined ) {
				PrependDeclSourceMap( targetDecl, obj );
				targetDecl.Add( headlineDecl );
				if( !isDefault && !isDisabled ) {
					if( obj.body == null ) {
						Diagnostics.Add( new Diagnostic(
							obj.srcPos,
							DiagnosticKind.Error,
							String.Format( "Non-default/non-disabled constructor/destructor '{0}' must have a body", obj.FullyQualifiedName ) ) );
						return;
					}

					targetDecl.AddRange( obj.body.Gen( LevelDecl ) );
				}
			}
			else {
				if( obj.body == null ) {
					Diagnostics.Add( new Diagnostic(
						obj.srcPos,
						DiagnosticKind.Error,
						String.Format( "Non-default/non-disabled constructor/destructor '{0}' must have a body", obj.FullyQualifiedName ) ) );
					return;
				}

				string headlineImpl = Format(
					FuncFormat[1],
					indentImpl,
					"", //leadingAttrImpl,
					nameImpl,
					paramString,
					"" );

				PrependDeclSourceMap( targetDecl, obj );
				targetDecl.Add( headlineDecl + ";" );

				PrependDeclSourceMap( targetImpl, obj );
				targetImpl.Add( headlineImpl + (obj.IsNoexcept ? " noexcept" : "") );
				targetImpl.AddRange( obj.body.Gen( LevelImpl ) );
			}
		}
	}
}
