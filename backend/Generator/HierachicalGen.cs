using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

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

		// TODO/HACK: provisional duplicate-name check. Replace with ScopeStack-based name
		//            resolution once that is wired up, so namespaces and nested scopes are respected.
		private readonly HashSet<string> declaredVars = new();

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
			IStrings includes = globalNS.imps
				.Select(
					i => i.StartsWith( "std_" )
						? Format( "#include <{0}>", i.Substring( 4 ) )
						: i.StartsWith( "c_" )
						? Format( "#include <c{0}>", i.Substring( 2 ) )
						: Format( "#include \"{0}.hpp\"", i ) );

			IStrings declList = GenDecl();
			IStrings decl = DefaultIncludes
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
				UsingFormat[obj.name == null ? 1 : 0],
				indent,
				obj.name,
				obj.type.Gen() );

			protoEarly.Target( obj.access ).Add( ret );
		}

		// Those need to be kept in adding order
		public void AddVar( VarDecl obj )
		{
			bool needsTypename = false; // TODO how to determine this

			bool isInsideStruct = obj.IsInStruct;
			bool isStatic       = obj.IsStatic;
			bool isHidden       = obj.IsHidden;
			bool isCompileTime  = obj.IsCompileTime;
			bool isInline       = obj.IsInline;
			bool isExtern       = obj.IsExternal;
			bool isConstType    = (obj.type.qual & Qualifier.Const) != 0;

			// TODO: report source location (file, line, column) for all of these attribute/duplicate errors.
			if( isInsideStruct ) {
				if( isHidden )                   throw new NotSupportedException( "[hide]/[hidden] is only valid at module/namespace scope." );
				if( isExtern )                   throw new NotSupportedException( "[extern] is only valid at module/namespace scope." );
				if( isInline      && !isStatic ) throw new NotSupportedException( "[inline] on a class field requires [static]." );
				if( isCompileTime && !isStatic ) throw new NotSupportedException( "[ct] on a class field requires [static]." );
			} else {
				if( isStatic )              throw new NotSupportedException( "[static] is only valid on class fields; use [hide]/[hidden] for module-level variables." );
				if( isInline && isHidden )  throw new NotSupportedException( "[inline] and [hide] are mutually exclusive." );
				if( isExtern ) {
					if( isInline )          throw new NotSupportedException( "[extern] and [inline] are mutually exclusive." );
					if( isHidden )          throw new NotSupportedException( "[extern] and [hide]/[hidden] are mutually exclusive." );
					if( isCompileTime )     throw new NotSupportedException( "[extern] cannot be used with [ct]." );
					if( isConstType )       throw new NotSupportedException( "[extern] cannot be used with const." );
					if( obj.init != null )  throw new NotSupportedException( "[extern] variables cannot have an initializer." );
				}
			}

			if( !declaredVars.Add( obj.name ) )
				throw new NotSupportedException( String.Format( "Duplicate variable/field declaration: {0}", obj.name ) );

			AccessStrings targetDecl = isStatic ? staticFieldDecl : fieldDecl;
			AccessStrings targetImpl = isStatic ? staticFieldImpl : fieldImpl;

			bool       externKw      = !isInsideStruct && ( isExtern || !( isHidden || isInline || isCompileTime || isConstType ) );
			bool       needsInline   = isInline || (isInsideStruct && isStatic && isCompileTime);
			GenerateAt emitAt        = GenerateAt.Decl;
			GenerateAt initIn        = GenerateAt.Decl;

			if( isInsideStruct ) {
				if( isStatic && !needsInline ) {
					emitAt = GenerateAt.Everywhere;
					initIn = GenerateAt.Impl;
				}
			} else {
				if( isExtern ) {
					initIn   = GenerateAt.Nowhere;
				} else if( isHidden ) {
					emitAt   = GenerateAt.Impl;
					initIn   = GenerateAt.Impl;
				} else if( isInline || isCompileTime || isConstType ) {
					// nothing else to set
				} else {
					emitAt   = GenerateAt.Everywhere;
					initIn   = GenerateAt.Impl;
				}
			}

			// 0 indent, 1 extern, 2 inline, 3 static, 4 constexpr, 5 typename, 6 type & name, 7 init
			if( (emitAt & GenerateAt.Decl) != 0 ) {
				Strings retDecl = new() {
					Format(
						VarFormat[0],
						IndentDecl,
						externKw             ? VarFormat[1] : "",
						needsInline          ? VarFormat[2] : "",
						isStatic || isHidden ? VarFormat[3] : "",
						isCompileTime        ? VarFormat[4] : "",
						needsTypename        ? VarFormat[5] : "",
						obj.type.Gen( obj.name ),
						(initIn & GenerateAt.Decl) != 0 && obj.init != null ? VarFormat[6] + obj.init.Gen() : "" )
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
						(initIn & GenerateAt.Impl) != 0 && obj.init != null ? VarFormat[6] + obj.init.Gen() : "" )
				};
				targetImpl.Add( (obj.access, retImpl) );
			}
		}

		public void AddFunc( Func obj )
		{
			List<TplParam> tplParams = obj.TplParams;

			bool    hasTpl         = tplParams.Count >= 1;
			bool    isInline       = obj.IsInline;
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

			string prefix = (isStatic ? "static " : "")
			              + (isExternal ? "extern " : "")
			              + (obj.IsVirtual ? "virtual " : "")
			              + (isInline ? "inline " : "");
			string suffix = (obj.IsConst ? " const" : "")
			              + (obj.IsOverride ? " override" : "");
			string headlineDecl = Format(
				FuncFormat[0],
				indentDecl,
				prefix,
				obj.retType.Gen( nameDecl ),
				paramString,
				suffix );

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

			// TODO: move inlines to bottom of header
			if( isInline || isExternal ) {
				if( !isInsideStruct ) {
					if( hasTpl )
						targetProto.Add( tplDecl );
					targetProto.Add( headlineDecl + ";" );
				}

				if( !isExternal ) {
					if( obj.body == null )
						throw new InvalidOperationException( String.Format( "Non-external function '{0}' must have a body", obj.FullyQualifiedName ) );

					if( hasTpl )
						targetDecl.Add( tplDecl );

					targetDecl.Add( headlineDecl );
					targetDecl.AddRange( obj.body.Gen( LevelDecl ) );
				}
			}
			else {
				if( obj.body == null )
					throw new InvalidOperationException( String.Format( "Non-external function '{0}' must have a body", obj.FullyQualifiedName ) );

				string headlineImpl = Format(
					FuncFormat[0],
					indentImpl,
					"",
					obj.retType.Gen( nameImpl ),
					paramString,
					(obj.IsConst ? " const" : "") );

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

			string keyword, bases;
			if( isNamespace ) {
				keyword = StructFormat[7];
				bases   = "";
			}
			else if( gen.hierarchical is Enumeration objEnum ) {
				keyword = StructFormat[6];
				bases = (objEnum.baseType != null)
					? " : " + objEnum.baseType.GenType()
					: "";
			}
			else if( gen.hierarchical is Structural objStruct ) {
				keyword = objStruct.kind switch {
					Structural.Kind.Struct => StructFormat[3],
					Structural.Kind.Class  => StructFormat[4],
					Structural.Kind.Union  => StructFormat[5],
					_                      => throw new Exception( Format( "no correct keyword determined: {0}", objStruct ) ),
				};

				bases = (objStruct.basetypes.Count < 1)
					? ""
					: " : public " + objStruct.basetypes
						.Select( t => t.GenType() )
						.Join( ", public " );
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

				// TODO: wrong spot? move towards the inside or create an alias-decl beforehand
				if( gen.hierarchical is Structural objStruct && objStruct.basetypes.Count >= 1 ) {
					targetDecl.Add(
						Format(
							UsingFormat[0],
							gen.IndentDecl,
							"base",
							objStruct.basetypes[0].GenType() ) );
				}
			}

			targetDecl.AddRange( gen.GenDecl() );

			if( !isGlobal )
				targetDecl.Add( Format( isNamespace ? CurlyClose : CurlyCloseSC, indent ) );

			targetImpl.AddRange( gen.GenImpl() );
		}

		public void AddStructor( Structor obj )
		{
			if( obj.IsStatic )
				throw new ArgumentOutOfRangeException( nameof( obj ), true, "Con/Destructor can not be static" );

			bool    isCtor     = obj.kind == Structor.Kind.Constructor;
			bool    isDtor     = obj.kind == Structor.Kind.Destructor;
			bool    isDefault  = obj.IsDefault;
			bool    isDisabled = obj.IsDisabled;
			bool    isInline   = obj.IsInline || isDefault || isDisabled;
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
					&& !obj.IsImplicit ) {
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

			if( isInline ) {
				targetDecl.Add( headlineDecl );
				if( !isDefault && !isDisabled ) {
					if( obj.body == null )
						throw new InvalidOperationException( String.Format( "Non-default/non-disabled constructor/destructor '{0}' must have a body", obj.FullyQualifiedName ) );

					targetDecl.AddRange( obj.body.Gen( LevelDecl ) );
				}
			}
			else {
				if( obj.body == null )
					throw new InvalidOperationException( String.Format( "Non-default/non-disabled constructor/destructor '{0}' must have a body", obj.FullyQualifiedName ) );

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
