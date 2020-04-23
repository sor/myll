using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Generator
{
	using static String;
	using static StmtFormatting;

	using Strings        = List<string>;
	using AccessStrings  = List<(Access access, List<string> gen)>;
	using IAccessStrings = IEnumerable<(Access access, List<string> gen)>;

	internal class PPPStrings
	{
		public readonly Strings
			priv = new Strings(),
			prot = new Strings(),
			pub  = new Strings();

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
			staticFieldDecl = new AccessStrings(),
			staticFieldImpl = new AccessStrings(),
			fieldDecl       = new AccessStrings();

		// Super memory inefficient but I don't care for the moment
		private readonly PPPStrings
			protoEarly         = new PPPStrings(),
			protoLate          = new PPPStrings(),
			hierarchicalDecl   = new PPPStrings(),
			hierarchicalImpl   = new PPPStrings(),
			staticAccessorDecl = new PPPStrings(),
			staticAccessorImpl = new PPPStrings(),
			staticOperatorDecl = new PPPStrings(),
			staticOperatorImpl = new PPPStrings(),
			staticMethodDecl   = new PPPStrings(),
			staticMethodImpl   = new PPPStrings(),
			ctorDecl           = new PPPStrings(),
			ctorImpl           = new PPPStrings(),
			dtorDecl           = new PPPStrings(), // you can only have one DTor,
			dtorImpl           = new PPPStrings(), // but this makes it simpler to generate the code
			accessorDecl       = new PPPStrings(),
			accessorImpl       = new PPPStrings(),
			operatorDecl       = new PPPStrings(),
			operatorImpl       = new PPPStrings(),
			methodDecl         = new PPPStrings(),
			methodImpl         = new PPPStrings();

		private readonly Access defaultAccess;

		private Hierarchical obj;

		private int LevelDecl { get; set; }
		private int LevelImpl { get; set; }

		private string IndentDecl   => IndentString.Repeat( LevelDecl );
		private string IndentImpl   => IndentString.Repeat( LevelImpl );
		private string DeIndentDecl => IndentString.Repeat( LevelDecl - 1 );
		private string DeIndentImpl => IndentString.Repeat( LevelImpl - 1 );

		public HierarchicalGen( Hierarchical obj, int LevelDecl, int LevelImpl )
		{
			this.obj       = obj;
			this.LevelDecl = LevelDecl;
			this.LevelImpl = LevelImpl;

			defaultAccess = obj.defaultAccess;
		}

		private void ThrowOnInvalidAccess( ref Access access )
		{
			if( access == Access.None ) {
				throw new Exception( "should not happen anymore?!" );
				//was access = currentAccess; before
				//access = defaultAccess;
			}

			// Could be outside of the Enums valid values
			if( !access.In( Access.Public, Access.Protected, Access.Private ) )
				throw new ArgumentOutOfRangeException(
					nameof( access ),
					access,
					@"Encountered invalid Accessibility; needed to be Public, Protected or Private" );
		}

		// TODO: convert to IStrings if too slow
		public Strings GenDecl()
		{
			Access curAccess    = defaultAccess;
			string accessIndent = DeIndentDecl;

			Strings ret = new Strings();
			IAccessStrings
				listDecl = new AccessStrings()
					.Concat( protoEarly )
					.Concat( protoLate )
					.Concat( hierarchicalDecl )
					.Concat( staticFieldDecl )
					.Concat( staticAccessorDecl )
					.Concat( staticOperatorDecl )
					.Concat( staticMethodDecl )
					.Concat( fieldDecl )
					.Concat( ctorDecl )
					.Concat( dtorDecl )
					.Concat( accessorDecl )
					.Concat( operatorDecl )
					.Concat( methodDecl );

			foreach( (Access access, Strings gen) in listDecl ) {
				if( gen.Count == 0 )
					continue;

				if( access != Access.Irrelevant ) {
					if( access != curAccess )
						ret.Add( Format( AccessFormat[access], accessIndent ) );

					curAccess = access;
				}
				ret.AddRange( gen );
			}

			return ret;
		}

		// TODO: convert to IStrings if too slow
		public Strings GenImpl()
		{
			Strings ret = new Strings();
			IAccessStrings
				listImpl = new AccessStrings()
					.Concat( hierarchicalImpl )
					.Concat( staticFieldImpl )
					.Concat( staticAccessorImpl )
					.Concat( staticOperatorImpl )
					.Concat( staticMethodImpl )
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
		public void AddEntry( EnumEntry obj, Access access = Access.None )
		{
			ThrowOnInvalidAccess( ref access );

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
			targetDecl.Add( (access, ret) );
		}

		// Those need to be kept in adding order
		public void AddVar( Var obj, Access access = Access.None )
		{
			ThrowOnInvalidAccess( ref access );

			bool isStatic      = obj.IsStatic;
			bool needsTypename = false; // TODO how to determine this

			string        indentDecl = IndentDecl;
			string        nameDecl   = obj.name;
			AccessStrings targetDecl = obj.IsStatic ? staticFieldDecl : fieldDecl;
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
			targetDecl.Add( (access, retDecl) );

			if( !isStatic )
				return;

			// only static fields here
			// TODO prepend "template <typename ...>" if needed
			string        indentImpl = IndentImpl;
			string        nameImpl   = obj.FullyQualifiedName;
			AccessStrings targetImpl = staticFieldImpl;
			Strings retImpl = new Strings {
				Format(
					VarFormat[0],
					indentImpl,
					"",
					needsTypename ? VarFormat[2] : "",
					obj.type.Gen( nameImpl ),
					obj.init != null ? VarFormat[3] + obj.init.Gen() : "" )
			};
			targetImpl.Add( (access, retImpl) );
		}

		public void AddFunc( Func obj, Access access = Access.None )
		{
			ThrowOnInvalidAccess( ref access );

			string  indentDecl  = IndentDecl;
			string  indentImpl  = IndentImpl;
			string  nameDecl    = obj.name;
			string  nameImpl    = obj.FullyQualifiedName;
			Strings targetProto = protoLate.Target( access );
			Strings targetDecl  = (obj.IsStatic ? staticMethodDecl : methodDecl).Target( access );
			Strings targetImpl  = (obj.IsStatic ? staticMethodImpl : methodImpl).Target( access );

			List<TplParam> tplParams = obj.TplParams;
			if( tplParams.Count >= 1 ) {
				string tpl = Format(
					"{0}template <{1}>",
					indentDecl,
					tplParams.Select( o => "typename " + o.name ).Join( ", " ) );

				targetProto.Add( tpl );
				targetDecl.Add( tpl );
			}

			string paramString = obj.paras
				.Select( para => para.Gen() )
				.Join( ", " );

			string headlineDecl = Format(
				FuncFormat[0],
				indentDecl,
				"",
				obj.retType.Gen(),
				nameDecl,
				paramString,
				"" );

			targetProto.Add( headlineDecl + ";" );
			if( obj.IsInline ) {
				targetDecl.Add( headlineDecl );
				targetDecl.Add( Format( CurlyOpen, indentDecl ) );
				targetDecl.AddRange( obj.block.GenWithoutCurly( LevelDecl + 1 ) );
				targetDecl.Add( Format( CurlyClose, indentDecl ) );
			}
			else {
				targetImpl.Add(
					Format(
						FuncFormat[0],
						indentImpl,
						"",
						obj.retType.Gen(),
						nameImpl,
						paramString,
						"" ) );
				targetImpl.Add( Format( CurlyOpen, indentImpl ) );
				targetImpl.AddRange( obj.block.GenWithoutCurly( LevelImpl + 1 ) );
				targetImpl.Add( Format( CurlyClose, indentImpl ) );
			}
		}

		public void AddHierarchical( Hierarchical obj, Access access = Access.None )
		{
			ThrowOnInvalidAccess( ref access );
			if( obj.IsStatic )
				throw new ArgumentOutOfRangeException( nameof( obj ), true, "Structurals/Enums can not be static" );

			HierarchicalGen gen = new HierarchicalGen( obj, LevelDecl + 1, LevelImpl );

			// this happens inside the children, each knows which method to call
			// e.g.: g.AddAccessor( ... );

			obj.children.ForEach( c => c.AddToGen( gen ) );

			string  indent      = gen.DeIndentDecl;
			string  nameDecl    = gen.obj.name;
			Strings targetProto = protoEarly.Target( access );
			Strings targetDecl  = hierarchicalDecl.Target( access );
			Strings targetImpl  = hierarchicalImpl.Target( access );

			if( gen.obj is ITplParams hierWithTpl
			 && hierWithTpl.TplParams.Count >= 1 ) {
				string tpl = Format(
					"{0}template <{1}>",
					indent,
					hierWithTpl.TplParams.Select( o => "typename " + o.name ).Join( ", " ) );

				targetProto.Add( tpl );
				targetDecl.Add( tpl );
			}

			var objNamespace = gen.obj as Namespace;
			var objEnum      = gen.obj as Enumeration;
			var objStruct    = gen.obj as Structural;

			bool isGlobal    = gen.obj is GlobalNamespace;
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
				};
				bases = (objStruct.basetypes.Count < 1)
					? ""
					: " : " + objStruct.basetypes
						.Select( t => t.GenType() )
						.Join( ", " );
			}
			else {
				throw new InvalidOperationException( "not an enum and not a struct" );
			}

			// "{0}{1}{2} {3}{4}{5}",
			// 0 indent, 1 keyword, 2 attributes, 3 name, 4 final, 5 bases
			if( !isNamespace ) {
				targetProto.Add(
					Format(
						StructFormat[0],
						indent,
						keyword,
						"",
						nameDecl,
						"",
						(objEnum != null ? bases : "") + ";" ) );
			}

			if( !isGlobal ) {
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
				targetDecl.Add( Format( CurlyCloseSC, indent ) );
			}

			targetImpl.AddRange( gen.GenImpl() );
		}

		public void AddCtorDtor( ConDestructor obj, Access access = Access.None )
		{
			ThrowOnInvalidAccess( ref access );
			if( obj.IsStatic )
				throw new ArgumentOutOfRangeException( nameof( obj ), true, "Con/Destructor can not be static" );

			bool    isCtor     = obj.kind == ConDestructor.Kind.Constructor;
			string  indentDecl = IndentDecl;
			string  indentImpl = IndentImpl;
			string  nameDecl   = obj.name;
			string  nameImpl   = obj.FullyQualifiedName;
			Strings targetDecl = (isCtor ? ctorDecl : dtorDecl).Target( access );
			Strings targetImpl = (isCtor ? ctorImpl : dtorImpl).Target( access );

			string paramString = obj.paras
				.Select( para => para.Gen() )
				.Join( ", " );

			string leadingAttr = "";
			if( obj.paras.Count == 1
			 && (!obj.attribs?.ContainsKey( "implicit" ) ?? true) ) {
				leadingAttr += "explicit ";
			}
			if( obj.IsInline ) {
				targetDecl.Add(
					Format(
						FuncFormat[1],
						indentDecl,
						leadingAttr,
						nameDecl,
						paramString,
						"" ) );
				targetDecl.Add( Format( CurlyOpen, indentDecl ) );
				targetDecl.AddRange( obj.block.GenWithoutCurly( LevelDecl + 1 ) );
				targetDecl.Add( Format( CurlyClose, indentDecl ) );
			}
			else {
				targetDecl.Add(
					Format(
						FuncFormat[1],
						indentDecl,
						leadingAttr,
						nameDecl,
						paramString,
						";" ) );

				targetImpl.Add(
					Format(
						FuncFormat[1],
						indentImpl,
						leadingAttr,
						nameImpl,
						paramString,
						"" ) );
				targetImpl.Add( Format( CurlyOpen, indentDecl ) );
				targetImpl.AddRange( obj.block.GenWithoutCurly( LevelImpl + 1 ) );
				targetImpl.Add( Format( CurlyClose, indentDecl ) );
			}
		}
	}
}
