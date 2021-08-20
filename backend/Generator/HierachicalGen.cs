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
						? Format( "#include <{0}>",     i.Substring( 4 ) )
						: Format( "#include \"{0}.h\"", i ) );

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

			Strings ret = new Strings();
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

		public IStrings GenImplGlobal()
		{
			Strings implList = GenImpl();
			if( implList.Count != 0 ) {
				GlobalNamespace globalNS = (GlobalNamespace) hierarchical;
				IStrings        impl     = implList.Prepend( Format( "#include \"{0}.h\"", globalNS.module ) );
				return impl;
			}
			else {
				return null; // do not generate file
			}
		}

		// TODO: convert to IStrings if too slow
		private Strings GenImpl()
		{
			Strings ret = new Strings();
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
			Strings ret = new Strings {
				Format(
					VarFormat[4],
					indent,
					name,
					obj.value != null ? VarFormat[3] + obj.value.Gen() : "" )
			};
			targetDecl.Add( (obj.access, ret) );
		}

		// Those need to be kept in adding order
		public void AddVar( Var obj )
		{
			bool needsTypename = false; // TODO how to determine this

			bool          isInsideStruct = obj.IsInStruct;
			bool          isStatic       = obj.IsStatic;
			string        indentDecl     = IndentDecl;
			string        indentImpl     = IndentImpl;
			string        nameDecl       = obj.name;
			string        nameImpl       = obj.FullyQualifiedName;
			AccessStrings targetDecl     = isStatic ? staticFieldDecl : fieldDecl;
			AccessStrings targetImpl     = isStatic ? staticFieldImpl : fieldImpl;
			Strings retDecl = new Strings {
				//"{0}{1}{2}{3};",
				// 0 indent, 1 typename, 2 type & name, 3 init
				Format(
					VarFormat[0],
					indentDecl,
					isStatic ? VarFormat[1] : "", // TODO add inline if initialized
					needsTypename ? VarFormat[2] : "",
					obj.type.Gen( nameDecl ),
					(obj.init != null && !isStatic)
						? VarFormat[3] + obj.init.Gen()
						: "" )
			};
			targetDecl.Add( (obj.access, retDecl) );

			if( !isStatic && isInsideStruct )
				return;

			// only static fields here
			// TODO prepend "template <typename ...>" if needed
			Strings retImpl = new Strings {
				Format(
					VarFormat[0],
					indentImpl,
					"",
					needsTypename ? VarFormat[2] : "",
					obj.type.Gen( nameImpl ),
					obj.init != null ? VarFormat[3] + obj.init.Gen() : "" )
			};
			targetImpl.Add( (obj.access, retImpl) );
		}

		public void AddFunc( Func obj )
		{
			List<TplParam> tplParams = obj.TplParams;

			bool    hasTpl         = tplParams.Count >= 1;
			bool    isInline       = obj.IsInline;
			bool    isInsideStruct = obj.IsInStruct;
			string  indentDecl     = IndentDecl;
			string  indentImpl     = IndentImpl;
			string  nameDecl       = obj.name;
			string  nameImpl       = obj.FullyQualifiedName;
			Strings targetDecl     = (obj.IsStatic ? staticMethodDecl : methodDecl).Target( obj.access );
			Strings targetImpl     = (obj.IsStatic ? staticMethodImpl : methodImpl).Target( obj.access );
			Strings targetProto    = isInsideStruct ? targetDecl : protoLate.Target( obj.access );

			string paramString = obj.paras
				.Select( p => p.Gen() )
				.Join( ", " );

			string headlineDecl = Format(
				FuncFormat[0],
				indentDecl,
				(obj.IsVirtual ? "virtual " : ""),
				obj.retType.Gen(),
				nameDecl,
				paramString,
				(obj.IsConst ? " const" : "") +
				(obj.IsOverride ? " override" : "") );

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
				tplDecl = null;
				tplImpl = null;
			}

			if( isInline ) {
				if( !isInsideStruct ) {
					if( hasTpl )
						targetProto.Add( tplDecl );
					targetProto.Add( headlineDecl + ";" );
				}

				if( hasTpl )
					targetDecl.Add( tplDecl );
				targetDecl.Add( headlineDecl );
				targetDecl.Add( Format( CurlyOpen, indentDecl ) );
				targetDecl.AddRange( obj.block.GenWithoutCurly( LevelDecl + 1 ) );
				targetDecl.Add( Format( CurlyClose, indentDecl ) );
			}
			else {
				string headlineImpl = Format(
					FuncFormat[0],
					indentImpl,
					"",
					obj.retType.Gen(),
					nameImpl,
					paramString,
					"" );

				if( hasTpl )
					targetProto.Add( tplDecl );
				targetProto.Add( headlineDecl + ";" );

				if( hasTpl )
					targetImpl.Add( tplImpl );
				targetImpl.Add( headlineImpl );
				targetImpl.Add( Format( CurlyOpen, indentImpl ) );
				targetImpl.AddRange( obj.block.GenWithoutCurly( LevelImpl + 1 ) );
				targetImpl.Add( Format( CurlyClose, indentImpl ) );
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
					hierWithTpl.TplParams.Select( o => "typename " + o.name ).Join( ", " ) );

				targetProto.Add( tpl );
				targetDecl.Add( tpl );
			}

			var objNamespace = gen.hierarchical as Namespace;
			var objEnum      = gen.hierarchical as Enumeration;
			var objStruct    = gen.hierarchical as Structural;

			bool isGlobal    = gen.hierarchical is GlobalNamespace;
			bool isNamespace = objNamespace != null;
			bool isEnum      = objEnum      != null;
			bool isStruct    = objStruct    != null;

			string keyword, bases;
			if( isNamespace ) {
				keyword = StructFormat[7];
				bases   = "";
			}
			else if( isEnum ) {
				keyword = StructFormat[6];
				bases = (objEnum.basetype != null)
					? " : " + objEnum.basetype.GenType()
					: "";
			}
			else if( isStruct ) {
				keyword = objStruct.kind switch {
					Structural.Kind.Struct => StructFormat[3],
					Structural.Kind.Class  => StructFormat[4],
					Structural.Kind.Union  => StructFormat[5],
					_                      => null,
				};

				if( keyword == null )
					throw new Exception( Format( "no correct keyword determined: {0}", objStruct ) );

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
			}

			targetDecl.AddRange( gen.GenDecl() );

			if( !isGlobal ) {
				targetDecl.Add( Format( isNamespace ? CurlyClose : CurlyCloseSC, indent ) );
			}

			targetImpl.AddRange( gen.GenImpl() );
		}

		public void AddCtorDtor( ConDestructor obj )
		{
			if( obj.IsStatic )
				throw new ArgumentOutOfRangeException( nameof( obj ), true, "Con/Destructor can not be static" );

			bool    isCtor     = obj.kind == ConDestructor.Kind.Constructor;
			string  indentDecl = IndentDecl;
			string  indentImpl = IndentImpl;
			string  nameDecl   = obj.name;
			string  nameImpl   = obj.FullyQualifiedName;
			Strings targetDecl = (isCtor ? ctorDecl : dtorDecl).Target( obj.access );
			Strings targetImpl = (isCtor ? ctorImpl : dtorImpl).Target( obj.access );

			string paramString = obj.paras
				.Select( para => para.Gen() )
				.Join( ", " );

			string leadingAttr = "";
			if( obj.paras.Count == 1
			 && !obj.IsAttrib( "implicit" ) ) {
				leadingAttr += "explicit ";
			}

			string headlineDecl = Format(
				FuncFormat[1],
				indentDecl,
				leadingAttr,
				nameDecl,
				paramString,
				"" );

			if( obj.IsInline ) {
				targetDecl.Add( headlineDecl );
				targetDecl.Add( Format( CurlyOpen, indentDecl ) );
				targetDecl.AddRange( obj.block.GenWithoutCurly( LevelDecl + 1 ) );
				targetDecl.Add( Format( CurlyClose, indentDecl ) );
			}
			else {
				string headlineImpl = Format(
					FuncFormat[1],
					indentImpl,
					leadingAttr,
					nameImpl,
					paramString,
					"" );

				targetDecl.Add( headlineDecl + ";" );

				targetImpl.Add( headlineImpl );
				targetImpl.Add( Format( CurlyOpen, indentImpl ) );
				targetImpl.AddRange( obj.block.GenWithoutCurly( LevelImpl + 1 ) );
				targetImpl.Add( Format( CurlyClose, indentImpl ) );
			}
		}
	}
}
