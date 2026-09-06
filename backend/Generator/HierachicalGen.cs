using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Generator
{
	using static String;
	using static StmtFormatting;

	using Strings        = List<string>;
	using IStrings       = IEnumerable<string>;
	using AccessStrings  = List<(Access access, List<string> gen)>;
	using IAccessStrings = IEnumerable<(Access access, List<string> gen)>;

	[Flags]
	public enum GenerateAt
	{
		Nowhere    = 0,
		Decl       = 1 << 0,
		Impl       = 1 << 1,
		Everywhere = Decl | Impl,
	}

	internal class PPPStrings
	{
		public readonly Strings
			priv = new(),
			prot = new(),
			pub  = new();

		public Strings Target( Access access )
			=> access switch {
				Access.Private   => priv,
				Access.Protected => prot,
				_                => pub,
			};
	}

	internal static class Extensions
	{
		internal static IAccessStrings Concat( this IAccessStrings self, PPPStrings pppStrings )
			=> self.Concat(
				new AccessStrings {
					(Access.Private,   pppStrings.priv),
					(Access.Protected, pppStrings.prot),
					(Access.Public,    pppStrings.pub),
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
		private readonly AccessStrings
			staticFieldDecl = new(),
			staticFieldImpl = new(),
			fieldDecl       = new(),
			fieldImpl       = new();

		// Super memory inefficient but I don't care for the moment
		private readonly PPPStrings
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

		private readonly HashSet<string> definedNamespaces = new();
		private readonly HashSet<string> namespaceUsingNames = new();

		public List<Diagnostic> Diagnostics { get; } = new();

		private int LevelDecl { get; }
		private int LevelImpl { get; }

		private string IndentDecl   => IndentString.Repeat( LevelDecl );
		private string IndentImpl   => IndentString.Repeat( LevelImpl );
		private string DeIndentDecl => IndentString.Repeat( LevelDecl - 1 );
		private string DeIndentImpl => IndentString.Repeat( LevelImpl - 1 );

		public HierarchicalGen( Hierarchical obj, int levelDecl, int levelImpl )
		{
			hierarchical = obj; // this is 'myself'
			LevelDecl    = levelDecl;
			LevelImpl    = levelImpl;

			defaultAccess = obj.defaultAccess;
		}

		public IStrings GenDeclGlobal()
		{
			// throw if not global
			GlobalNamespace globalNS = (GlobalNamespace) hierarchical;
			ISet<string> implicitImports = new HashSet<string>( StdBuiltins.ImplicitStdImports );

			Strings includes = new();
			HashSet<string> seen = new();

			string MapImport( string imp )
				=> imp.StartsWith( "std_" )
					? Format( "#include <{0}>", imp.Substring( 4 ) )
					: imp.StartsWith( "c_" )
					? Format( "#include <c{0}>", imp.Substring( 2 ) )
					: imp.Contains( "." )
					? Format( "#include \"{0}\"", imp )
					: Format( "#include \"{0}.hpp\"", imp );

			// Implicit standard imports in a fixed, stable order.
			foreach( string imp in StdBuiltins.ImplicitStdImports ) {
				if( imp == globalNS.module )
					continue;
				if( !globalNS.imps.Contains( imp ) )
					continue;
				string include = MapImport( imp );
				if( seen.Add( include ) )
					includes.Add( include );
			}

			// Remaining imports in deterministic order.
			foreach( string imp in globalNS.imps
				.Where( i => i != globalNS.module && !implicitImports.Contains( i ) )
				.OrderBy( i => i, StringComparer.Ordinal ) ) {
				string include = MapImport( imp );
				if( seen.Add( include ) )
					includes.Add( include );
			}

			IStrings declList = GenDecl();
			IStrings decl = new Strings { PragmaOnce }
				.Concat( includes )
				.Concat( declList );

			return decl;
		}

		// TODO: convert to IStrings if too slow
		private Strings GenDecl()
		{
			Access curAccess    = defaultAccess;
			string accessIndent = DeIndentDecl;

			Strings ret = new();

			if( Dialect.GlobalUsingNS == GlobalUsingNSMode.Leaky
			 && namespaceUsingNames.Count > 0 ) {
				foreach( string ns in namespaceUsingNames
					.Where( n => definedNamespaces.Contains( n ) )
					.OrderBy( n => n ) ) {
					ret.Add( Format( "{0}namespace {1} {{}}", IndentDecl, ns ) );
				}
			}

			IAccessStrings
				listDecl = new AccessStrings()
					.Concat( protoEarly )
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

			foreach( (Access access, Strings gen) in listDecl ) {
				if( gen.Count == 0 )
					continue;

				if( access != curAccess )
					ret.Add( Format( AccessFormat[access], accessIndent ) );

				curAccess = access;

				ret.AddRange( gen );
			}

			return ret;
		}

		public IStrings? GenImplGlobal()
		{
			Strings implList = GenImpl();
			if( implList.Count != 0 ) {
				GlobalNamespace globalNS = (GlobalNamespace) hierarchical;
				IStrings        impl     = implList.Prepend( Format( "#include \"{0}.hpp\"", globalNS.module ) );
				return impl;
			}
			else {
				return null; // do not generate file
			}
		}

		// TODO: convert to IStrings if too slow
		private Strings GenImpl()
		{
			Strings ret = new();
			IAccessStrings
				listImpl = new AccessStrings()
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

			foreach( (Access access, Strings gen) in listImpl ) {
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
			AccessStrings targetDecl = obj.IsStatic ? staticFieldDecl : fieldDecl;
			Strings ret = new() {
				Format(
					EntryFormat[0],
					indent,
					name,
					obj.value != null ? EntryFormat[1] + obj.value.Gen() : "" )
			};
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

			string ret = Format(
				AliasFormat[obj.IsNamespaceAlias ? 1 : 0],
				indent,
				obj.name,
				obj.type.Gen() );

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

			AccessStrings targetDecl = isStatic ? staticFieldDecl : fieldDecl;
			AccessStrings targetImpl = isStatic ? staticFieldImpl : fieldImpl;

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

			// 0 indent, 1 extern, 2 inline, 3 static, 4 constexpr, 5 typename, 6 type & name, 7 init
			if( (emitAt & GenerateAt.Decl) != 0 ) {
				Strings retDecl = new() {
					Format(
						VarFormat[0],
						IndentDecl,
						isExtern             ? VarFormat[1] : "",
						needsInline          ? VarFormat[2] : "",
						isStatic || isHidden ? VarFormat[3] : "",
						isCompileTime        ? VarFormat[4] : "",
						needsTypename        ? VarFormat[5] : "",
						obj.type.Gen( obj.name ),
						initDecl )
				};
				targetDecl.Add( (obj.access, retDecl) );
			}

			if( (emitAt & GenerateAt.Impl) != 0 ) {
				Strings retImpl = new() {
					Format(
						VarFormat[0],
						IndentImpl,
						"",
						"",
						isHidden      ? VarFormat[3] : "",
						"",
						needsTypename ? VarFormat[5] : "",
						obj.type.Gen( obj.FullyQualifiedName ),
						initImpl )
				};
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
			Strings targetDecl     = (isStatic ? staticMethodDecl : methodDecl).Target( obj.access );
			Strings targetImpl     = (isStatic ? staticMethodImpl : methodImpl).Target( obj.access );
			Strings targetProto    = isInsideStruct ? targetDecl : protoLate.Target( obj.access );

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

			string prefix = (obj.IsCompileTime ? "constexpr " : "")
		              + (isStatic          ? "static "    : "")
		              + (isExternal        ? "extern "    : "")
		              + (obj.IsVirtual || isAbstract ? "virtual "   : "")
		              + (isInlined         ? "inline "    : "");
			string suffix = (obj.IsPure     ? " const"    : "")
		              + (obj.IsOverride ? " override" : "");
			string headlineDecl = Format(
				FuncFormat[0],
				indentDecl,
				prefix,
				obj.retType.Gen( nameDecl ),
				paramString,
				suffix );

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

			// TODO: [default] on free functions is invalid C++ unless it is a special member;
			// we currently emit it and let the C++ compiler complain. Add a Myll-side error later.
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

				if( hasTpl )
					targetProto.Add( tplDecl );

				targetProto.Add( headlineDecl + specialTail + ";" );
				return;
			}

			// TODO: move inlines to bottom of header
			if( isInlined || isExternal || hasTpl ) {
				if( !isInsideStruct ) {
					if( hasTpl )
						targetProto.Add( tplDecl );
					targetProto.Add( headlineDecl + ";" );
				}

				if( !isExternal ) {
					if( obj.body == null ) {
						Diagnostics.Add( new Diagnostic(
							obj.srcPos,
							DiagnosticKind.Error,
							String.Format( "Non-external function '{0}' must have a body", obj.FullyQualifiedName ) ) );
						return;
					}

					if( hasTpl )
						targetDecl.Add( tplDecl );

					targetDecl.Add( headlineDecl );
					targetDecl.AddRange( obj.body.Gen( LevelDecl ) );
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
					(obj.IsPure ? " const" : "") );

				if( hasTpl )
					targetProto.Add( tplDecl );

				targetProto.Add( headlineDecl + ";" );

				if( hasTpl )
					targetImpl.Add( tplImpl );

				targetImpl.Add( headlineImpl );
				targetImpl.AddRange( obj.body.Gen( LevelImpl ) );
			}
		}

		// this adds an hierarchical as child
		public void AddHierarchical( Hierarchical obj )
		{
			if( obj.IsStatic )
				throw new ArgumentOutOfRangeException( nameof( obj ), true, "Hierarchicals can not be static" );

			// this is a sub-gen for the child-hierarchical obj
			HierarchicalGen gen = new( obj, LevelDecl + 1, LevelImpl );

			// this happens inside the children, each knows which method to call
			// e.g.: g.AddAccessor( ... );

			obj.children.ForEach( c => c.AddToGen( gen ) );

			if( gen.hierarchical is Structural structural )
				AddHiddenBaseMethodUsings( structural, gen );

			Diagnostics.AddRange( gen.Diagnostics );

			string  indent      = gen.DeIndentDecl;
			string  nameDecl    = gen.hierarchical.name;
			Strings targetProto = protoEarly.Target( obj.access );
			Strings targetDecl  = hierarchicalDecl.Target( obj.access );
			Strings targetImpl  = hierarchicalImpl.Target( obj.access );

			if( gen.hierarchical is ITplParams hierWithTpl
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

			bool isGlobal    = gen.hierarchical is GlobalNamespace;
			bool isNamespace = gen.hierarchical is Namespace;
			bool isEnum      = gen.hierarchical is Enumeration;

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

				bool autoUnhide = ShouldAutoUnhide( derived, structural );

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

		private static bool ShouldAutoUnhide( Func derived, Structural structural )
		{
			if( derived.IsUnshadow )
				return true;
			if( derived.IsShadow )
				return false;
			if( structural.IsUnshadow )
				return true;
			if( structural.IsShadow )
				return false;
			return Dialect.AutoUnhideBaseMethods;
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

			Strings target = protoEarly.Target( structural.access );
			string  indent = DeIndentDecl;

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
			Strings targetDecl = (isCtor ? ctorDecl : dtorDecl).Target( obj.access );
			Strings targetImpl = (isCtor ? ctorImpl : dtorImpl).Target( obj.access );

			string paramString = obj.paras
				.Select( para => para.Gen() )
				.Join( ", " );

			string leadingAttrDecl = "";
			if( obj.paras.Count == 1 // TODO: 0, 1 or more parameterizable
					&& !obj.IsImplicit
					&& !IsCopyOrMoveConstructor( obj ) ) {
				leadingAttrDecl += "explicit ";
			}

			if( isDtor && obj.IsVirtual ) {
				leadingAttrDecl += "virtual ";
			}

			string followingDecl =
				isDefault  ? " = default;" :
				isDisabled ? " = delete;" :
							 "";

			string headlineDecl = Format(
				FuncFormat[1],
				indentDecl,
				leadingAttrDecl,
				nameDecl,
				paramString,
				followingDecl );

			if( isInlined ) {
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

				targetDecl.Add( headlineDecl + ";" );

				targetImpl.Add( headlineImpl );
				targetImpl.AddRange( obj.body.Gen( LevelImpl ) );
			}
		}
	}
}
