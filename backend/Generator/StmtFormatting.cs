using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Myll.Core;

using static System.String;

namespace Myll.Generator
{
	using Strings = List<string>;
	using UniqueStrings = HashSet<string>;
	using IntToString = Dictionary<int, string>;
	using AccessStrings = List<(Access access, List<string> gen)>;

	public static class StmtFormatting
	{
		//public static readonly string IndentString = "\t";
		public static readonly string IndentString = "    ";
		public static readonly string ThrowFormat  = "{0}throw {1};";
		public static readonly string BreakFormat  = "{0}break;";
		public static readonly string CurlyOpen    = "{0}{{";
		public static readonly string CurlyClose   = "{0}}}";
		public static readonly string CurlyCloseSC = "{0}}};";

		internal static AccessStrings AddPPPString( this AccessStrings self, StructuralGen.PPPStrings pppStrings )
		{
			self.AddRange(
				new AccessStrings {
					(Access.Private,   pppStrings.priv),
					(Access.Protected, pppStrings.prot),
					(Access.Public,    pppStrings.pub),
				} );

			return self;
		}

		public static readonly Dictionary<Access, string>
			AccessFormat = new Dictionary<Access, string> {
				{ Access.Private,   "{0}  private:" },
				{ Access.Protected, "{0}  protected:" },
				{ Access.Public,    "{0}  public:" },
			};

		public static readonly string[] ReturnFormat = {
			"{0}return;",
			"{0}return {1};",
		};

		public static readonly string[] IfFormat = {
			"{0}if( {1} )",
			"{0}else if( {1} )",
			"{0}else",
		};

		public static readonly string[] LoopFormat = {
			"{0}for( {1} {2}; {3} )", // first semicolon is part of the stmt
			"{0}while( {1} )",
			"{0}do",
			"{0}while( {1} );",
		};

		public static readonly string[] VarFormat = {
			"{0}{1}{2}{3}{4};", // 0 indent, 1 static , 2 typename, 3 type & name, 4 init
			"static ",
			"typename ",
			" = ",
		};

		public static readonly string[] FuncFormat = {
			"{0}{1}{2} {3}({4}){5}", // function:  0 indent, 1 leading attributes, 2 return type, 3 name, 4 params, 5 trailing attributes
			"{0}{1}{2}({3}){4}",     // ctor/dtor: 0 indent, 1 leading attributes, 2 name, 3 params, 4 trailing attributes
			"{0}",
		};

		public static readonly string[] StructFormat = {
			"{0}{1}{2} {3}{4}{5}", // 0 indent, 1 keyword, 2 attributes, 3 name, 4 final, 5 bases or semicolon
			" : {0}{1}",           // first base; 0 virtual and/or ppp, 1 name
			", {0}{1}",            // other bases; 0 virtual and/or ppp, 1 name
			"struct",
			"class",
			"union",
		};

		public static readonly IReadOnlyDictionary<TypespecBasic.Kind, IntToString>
			BasicFormat = new Dictionary<TypespecBasic.Kind, IntToString> {
				{
					TypespecBasic.Kind.Auto, new IntToString {
						{ TypespecBasic.SizeUndetermined, "auto" }
					}
				}, {
					TypespecBasic.Kind.Void, new IntToString {
						{ TypespecBasic.SizeInvalid, "void" }
					}
				}, {
					TypespecBasic.Kind.Bool, new IntToString {
						{ 1, "bool" }
					}
				}, {
					TypespecBasic.Kind.Char, new IntToString {
						{ 1, "char" }, // TODO char8_t
						{ 4, "char32_t" },
					}
				}, {
					TypespecBasic.Kind.String, new IntToString {
						{ TypespecBasic.SizeUndetermined, "std::string" }
					}
				}, {
					TypespecBasic.Kind.Float, new IntToString {
						{ 2, "half" },
						{ 4, "float" },
						{ 8, "double" },
						{ 16, "long double" },
					}
				}, {
					TypespecBasic.Kind.Binary, new IntToString {
						{ 1, "std::byte" },
						{ 2, "std::uint16_t" },
						{ 4, "std::uint32_t" },
						{ 8, "std::uint64_t" },
					}
				}, {
					TypespecBasic.Kind.Integer, new IntToString {
						{ 1, "std::int8_t" },
						{ 2, "std::int16_t" },
						{ 4, "int" },
						{ 8, "std::int64_t" },
					}
				}, {
					TypespecBasic.Kind.Unsigned, new IntToString {
						{ 1, "std::uint8_t" },
						{ 2, "std::uint16_t" },
						{ 4, "unsigned int" },
						{ 8, "std::uint64_t" },
					}
				},
				/* TODO {
					TypespecBasic.Kind.Size, new IntToString {
						{ 1, "std::intptr_t" },
						{ 2, "std::uintptr_t" },
					}
				},*/
			};

		// not only for Decls, for Stmts who need more interaction with surrounding as well
		public abstract class DeclGen
		{
			internal int LevelDecl { get; set; }
			internal int LevelImpl { get; set; }

			internal string IndentDecl   => IndentString.Repeat( LevelDecl );
			internal string IndentImpl   => IndentString.Repeat( LevelImpl );
			internal string DeIndentDecl => IndentString.Repeat( LevelDecl - 1 );
			internal string DeIndentImpl => IndentString.Repeat( LevelImpl - 1 );

			public DeclGen( int LevelDecl, int LevelImpl )
			{
				this.LevelDecl = LevelDecl;
				this.LevelImpl = LevelImpl;
			}

			public abstract Strings GenDecl();
			public abstract Strings GenImpl();

			public abstract void AddNamespace( Namespace obj, Access access = Access.None );
			public abstract void AddVar( Var             obj, Access access = Access.None );
			public abstract void AddFunc( Func           obj, Access access = Access.None );
			public abstract void AddStruct( Structural   obj, Access access = Access.None );

			public virtual void AddCtorDtor( ConDestructor obj, Access access = Access.None )
			{
				throw new NotSupportedException();
			}
		}

		// only here for a while, until all the dedicated ones work
		public class SimpleGen : DeclGen
		{
			private readonly Strings AllDecl = new Strings();
			private readonly Strings AllImpl = new Strings();

			public SimpleGen( int LevelDecl, int LevelImpl )
				: base( LevelDecl, LevelImpl ) {}

			public override Strings GenDecl() => AllDecl;
			public override Strings GenImpl() => AllImpl;

			public override void AddNamespace( Namespace obj, Access access = Access.None )
			{
				if( obj.IsStatic )
					throw new ArgumentOutOfRangeException( nameof( obj ), true, "Namespaces can not be static" );

				SimpleGen gen = new SimpleGen( LevelDecl + 1, LevelImpl );

				// this happens inside the children, each knows which method to call
				// e.g.: g.AddAccessor( ... );

				obj.children.ForEach( c => c.AddToGen( gen ) );

				string indent = IndentDecl;

				// -1 is global
				if( LevelDecl > -1 ) {
					AllDecl.Add( Format( "{0}namespace {1}", indent, obj.name ) );
					AllDecl.Add( Format( CurlyOpen, indent ) );
				}
				AllDecl.AddRange( gen.GenDecl() );
				if( LevelDecl > -1 ) {
					AllDecl.Add( Format( CurlyClose, indent ) );
				}

				AllImpl.AddRange( gen.GenImpl() );

				/*StructuralGen.GenStructStruct
					structsDecl = new StructuralGen.GenStructStruct(),
					structsImpl = new StructuralGen.GenStructStruct();

				structsDecl.AddStructDecl( gen, access );
				structsImpl.AddStructImpl( gen, access );

				// TODO: do this later for prototypes to work properly
				Access curAccess = Access.None;
				AllDecl.AddRange( structsDecl.GenDecl( ref curAccess ) );
				AllImpl.AddRange( structsImpl.GenImpl() );
				*/
			}

			public override void AddVar( Var obj, Access access = Access.None )
			{
				string indentDecl = IndentDecl;
				string indentImpl = IndentImpl;

				string name          = obj.name;
				bool   needsTypename = false; // TODO how to determine this
				Strings ret = new Strings {
					Format(
						VarFormat[0],
						indentDecl,
						obj.IsStatic ? VarFormat[1] : "",
						needsTypename ? VarFormat[2] : "",
						obj.type.Gen( name ),
						obj.init != null ? VarFormat[3] + obj.init.Gen() : "" )
				};
				AllDecl.AddRange( ret );
			}

			public override void AddFunc( Func obj, Access access = Access.None )
			{
				string indentDecl = IndentDecl;
				string indentImpl = IndentImpl;

				string paramString = obj.paras
					.Select( para => para.Gen() )
					.Join( ", " );

				Strings ret = new Strings {
					Format(
						FuncFormat[0],
						indentDecl,
						"",
						obj.retType.Gen(),
						obj.name,
						paramString,
						";" )
				};
				AllDecl.AddRange( ret );

				Strings impl = new Strings {
					Format(
						FuncFormat[0],
						indentImpl,
						"",
						obj.retType.Gen(),
						obj.FullyQualifiedName,
						paramString,
						"" )
				};
				impl.AddRange( obj.block.Gen( LevelImpl + 1 ) );
				AllImpl.AddRange( impl );
			}

			public override void AddStruct( Structural obj, Access access = Access.None )
			{
				// HACK: since reusing the class implementation, everything needs to be forced to public for now
				access = Access.Public;

				if( obj.IsStatic )
					throw new ArgumentOutOfRangeException( nameof( obj ), true, "Structurals can not be static" );

				StructuralGen gen = new StructuralGen( obj, LevelDecl + 1, LevelImpl );

				// this happens inside the children, each knows which method to call
				// e.g.: g.AddAccessor( ... );

				obj.children.ForEach( c => c.AddToGen( gen ) );

				StructuralGen.GenStructStruct
					structs = new StructuralGen.GenStructStruct();

				structs.parent = gen;

				// they are all public
				structs.AddStruct( gen, access );

				// TODO: do this later for prototypes to work properly
				string accessIndent = DeIndentDecl;
				Access curAccess = Access.Public;
				AllDecl.AddRange( structs.GenDecl( ref curAccess, accessIndent ) );
				AllImpl.AddRange( structs.GenImpl() );

				//AllDecl.AddRange( gen.GenDecl() );
				//AllImpl.AddRange( gen.GenImpl() );
			}
		}

		// TODO: not finished at all
		/*public abstract class StmtGen : DeclGen
		{
			public abstract void AddHere( Strings       obj );
			public abstract void AddInnerScope( Strings obj );
			public abstract void AddOuterScope( Strings obj );

		}*/

		// TODO: can this be used without change for namespaces?
		/**
			GlobalGen

			Structure of output:

				(using abbreviations:
					ppp = private, protected, public
					sorted = in the order of the alphabet/dictionary
					ordered = in the order they have been added
					grouped = grouped by a category, then in the order they have been added
				)

				sorted includes
				ordered proto
				ordered	types/alias
				ordered	variables
				ordered	accessors
				ordered	operators
				ordered	functions
		*/

		/*
		public class GlobalGen : DeclGen
		{
			public readonly UniqueStrings
				includeDecl = new UniqueStrings(); // needs to be determined in analyze step

			public readonly Strings
				typeProto = new Strings(), // maybe needs to be determined in analyze step
				restProto = new Strings(), // maybe needs to be determined in analyze step
				allDecl   = new Strings(), // alias and using in here
				allImpl   = new Strings();

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			private void ThrowOnInvalidAccess( Access access )
			{
				if( access != Access.None )
					throw new ArgumentOutOfRangeException(
						nameof( access ),
						access,
						@"Global Scope has no Accessibility; needed to be None" );
			}

			public override void AddVar( Var obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				throw new NotImplementedException();
			}

			public override void AddFunc( Func obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				throw new NotImplementedException();
			}

			public override void AddStruct( Structural obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				throw new NotImplementedException();
			}
		}
		*/

		/**
			StructuralGen

			Structure of output (using abbreviations: ppp = private, protected, public):

				ppp grouped	proto
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
		public class StructuralGen : DeclGen
		{
			public class PPPStrings
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

			internal abstract class GenStructBase
			{
				protected AccessStrings genListDecl;
				protected AccessStrings genListImpl;
				internal  DeclGen       parent;

				// These GenDecl/GenImpl are the alternative to two derived classes just for these methods
				public Strings GenDecl( ref Access curAccess, string accessIndent )
				{
					Strings ret = new Strings();
					GenList( ret, genListDecl, accessIndent, ref curAccess );
					return ret;
				}

				public Strings GenImpl()
				{
					Strings ret = new Strings();
					GenList( ret, genListImpl );
					return ret;
				}

				// For Decl
				protected void GenList( Strings ret, AccessStrings list, string accessIndent, ref Access curAccess )
				{
					foreach( (Access access, Strings gen) in list ) {
						if( gen.Count == 0 )
							continue;

						if( access != Access.Irrelevant ) {
							if( access != curAccess )
								ret.Add( Format( AccessFormat[access], accessIndent ) );

							curAccess = access;
						}
						ret.AddRange( gen );
					}
				}

				// For Impl
				protected void GenList( Strings ret, AccessStrings list )
				{
					foreach( (Access access, Strings gen) in list ) {
						if( gen.Count == 0 )
							continue;

						ret.AddRange( gen );
					}
				}

				protected void ThrowOnInvalidAccess( ref Access access )
				{
					// TODO: reactivate this
					//if( access == Access.None )
					//	access = currentAccess;

					// Could be outside of the Enums valid values
					if( !access.In( Access.Private, Access.Protected, Access.Public ) )
						throw new ArgumentOutOfRangeException(
							nameof( access ),
							access,
							@"Encountered invalid Accessibility; needed to be Private, Protected or Public" );
				}
			}

			/// Generates a Struct inside a Struct
			internal class GenStructStruct : GenStructBase
			{
				// Super memory inefficient but I don't care for the moment
				protected readonly PPPStrings
					protoDecl = new PPPStrings(),
					protoImpl = new PPPStrings(),
					structDecl = new PPPStrings(),
					structImpl = new PPPStrings();

				public GenStructStruct()
				{
					genListDecl = new AccessStrings( 6 )
						.AddPPPString( protoDecl )
						.AddPPPString( structDecl );

					genListImpl = new AccessStrings( 6 )
						.AddPPPString( protoImpl )
						.AddPPPString( structImpl );
				}

				public void AddStruct( StructuralGen gen, Access access = Access.None )
				{
					string  indentDecl  = gen.DeIndentDecl;
					string  nameDecl    = gen.obj.name;
					Strings targetProto = protoDecl.Target( access );
					Strings targetDecl  = structDecl.Target( access );
					Strings targetImpl  = structImpl.Target( access );

					List<TplParam> tplParams = gen.obj.TplParams;
					if( tplParams.Count >= 1 ) {
						string tpl = Format(
							"{0}template <{1}>",
							indentDecl,
							tplParams.Select( o => "typename " + o.name ).Join( ", " ) );

						targetProto.Add( tpl );
						targetDecl.Add( tpl );
					}
					// "{0}{1}{2} {3}{4}{5}",
					// 0 indent, 1 keyword, 2 attributes, 3 name, 4 final, 5 bases
					targetProto.Add(
						Format(
							StructFormat[0],
							indentDecl,
							StructFormat[4], // TODO: is currently hardcoded to class
							"",
							nameDecl,
							"",
							";" ) );

					targetDecl.Add(
						Format(
							StructFormat[0],
							indentDecl,
							StructFormat[4], // TODO: is currently hardcoded to class
							"",
							nameDecl,
							"",
							"" /*StructFormat[2]*/ ) );
					targetDecl.Add( Format( CurlyOpen, indentDecl ) );
					targetDecl.AddRange( gen.GenDecl() );
					targetDecl.Add( Format( CurlyCloseSC, indentDecl ) );

					targetImpl.AddRange( gen.GenImpl() );
				}
			}

			/// Fields are special, they can not be grouped by ppp or sorted
			private class GenStructField : GenStructBase
			{
				public GenStructField()
				{
					genListDecl = new AccessStrings();
					genListImpl = new AccessStrings();
				}

				public void AddVar( Var obj, Access access = Access.None )
				{
					bool needsTypename = false; // TODO how to determine this

					string indentDecl = parent.IndentDecl;
					string indentImpl = parent.IndentImpl;
					string nameDecl   = obj.name;
					string nameImpl   = obj.FullyQualifiedName;

					//"{0}{1}{2}{3};", // 0 indent, 1 typename, 2 type & name, 3 init
					Strings retDecl = new Strings {
						Format(
							VarFormat[0],
							indentDecl,
							obj.IsStatic ? VarFormat[1] : "",
							needsTypename ? VarFormat[2] : "",
							obj.type.Gen( nameDecl ),
							obj.init != null ? VarFormat[3] + obj.init.Gen() : "" )
					};
					genListDecl.Add( (access, retDecl) );

					if( !obj.IsStatic )
						return;

					Strings retImpl = new Strings {
						Format(
							VarFormat[0],
							indentImpl,
							obj.IsStatic ? VarFormat[1] : "",
							needsTypename ? VarFormat[2] : "",
							obj.type.Gen( nameImpl ),
							obj.init != null ? VarFormat[3] + obj.init.Gen() : "" )
					};
					genListImpl.Add( (access, retImpl) );
				}
			}

			private class GenStructFunc : GenStructBase
			{
				// Super memory inefficient but I don't care for the moment
				protected readonly PPPStrings
					accessorDecl = new PPPStrings(),
					accessorImpl = new PPPStrings(),
					operatorDecl = new PPPStrings(),
					operatorImpl = new PPPStrings(),
					methodDecl   = new PPPStrings(),
					methodImpl   = new PPPStrings();

				public GenStructFunc()
				{
					genListDecl = new AccessStrings {
						(Access.Private,   accessorDecl.priv),
						(Access.Protected, accessorDecl.prot),
						(Access.Public,    accessorDecl.pub),
						(Access.Private,   operatorDecl.priv),
						(Access.Protected, operatorDecl.prot),
						(Access.Public,    operatorDecl.pub),
						(Access.Private,   methodDecl.priv),
						(Access.Protected, methodDecl.prot),
						(Access.Public,    methodDecl.pub),
					};
					genListImpl = new AccessStrings {
						(Access.Private,   accessorImpl.priv),
						(Access.Protected, accessorImpl.prot),
						(Access.Public,    accessorImpl.pub),
						(Access.Private,   operatorImpl.priv),
						(Access.Protected, operatorImpl.prot),
						(Access.Public,    operatorImpl.pub),
						(Access.Private,   methodImpl.priv),
						(Access.Protected, methodImpl.prot),
						(Access.Public,    methodImpl.pub),
					};
				}

				public void AddFunc( Func obj, Access access = Access.None )
				{
					string  indentDecl = parent.IndentDecl;
					string  indentImpl = parent.IndentImpl;
					string  nameDecl   = obj.name;
					string  nameImpl   = obj.FullyQualifiedName;
					Strings targetDecl = methodDecl.Target( access );
					Strings targetImpl = methodImpl.Target( access );

					List<TplParam> tplParams = obj.TplParams;
					if( tplParams.Count >= 1 ) {
						string tpl = Format(
							"{0}template <{1}>",
							indentDecl,
							tplParams.Select( o => "typename " + o.name ).Join( ", " ) );

						//targetProto.Add( tpl );
						targetDecl.Add( tpl );
					}

					string paramString = obj.paras
						.Select( para => para.Gen() )
						.Join( ", " );

					if( obj.IsInline ) {
						targetDecl.Add(
							Format(
								FuncFormat[0],
								indentDecl,
								"",
								obj.retType.Gen(),
								nameDecl,
								paramString,
								"" ) );
						targetDecl.Add( Format( CurlyOpen, indentDecl ) );
						targetDecl.AddRange( obj.block.GenWithoutCurly( parent.LevelDecl + 1 ) );
						targetDecl.Add( Format( CurlyClose, indentDecl ) );
					}
					else {
						targetDecl.Add(
							Format(
								FuncFormat[0],
								indentDecl,
								"",
								obj.retType.Gen(),
								nameDecl,
								paramString,
								";" ) );

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
						targetImpl.AddRange( obj.block.GenWithoutCurly( parent.LevelImpl + 1 ) );
						targetImpl.Add( Format( CurlyClose, indentImpl ) );
					}
				}
			}

			private class GenStructCtorDtor : GenStructBase
			{
				// TODO move somewhere else
				public readonly Strings
					protoGen = new Strings(), // still needed for toplevel types?
					aliasGen = new Strings(),
					typesGen = new Strings();

				// Super memory inefficient but don't care for the moment
				public readonly PPPStrings
					ctorsDecl = new PPPStrings(),
					ctorsImpl = new PPPStrings(),
					dtorsDecl = new PPPStrings(), // you can only have one DTor,
					dtorsImpl = new PPPStrings(); // but this makes it simpler to generate the code

				public GenStructCtorDtor()
				{
					genListDecl = new AccessStrings( 6 )
						.AddPPPString( ctorsDecl )
						.AddPPPString( dtorsDecl );

					genListImpl = new AccessStrings( 6 )
						.AddPPPString( ctorsImpl )
						.AddPPPString( dtorsImpl );
				}

				public void AddCtorDtor( ConDestructor obj, Access access = Access.None )
				{
					bool    isCtor     = obj.kind == ConDestructor.Kind.Constructor;
					string  indentDecl = parent.IndentDecl;
					string  indentImpl = parent.IndentImpl;
					string  nameDecl   = obj.name;
					string  nameImpl   = obj.FullyQualifiedName;
					Strings targetDecl = (isCtor ? ctorsDecl : dtorsDecl).Target( access );
					Strings targetImpl = (isCtor ? ctorsImpl : dtorsImpl).Target( access );

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
						targetDecl.AddRange( obj.block.GenWithoutCurly( parent.LevelDecl + 1 ) );
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
						targetImpl.AddRange( obj.block.GenWithoutCurly( parent.LevelImpl + 1 ) );
						targetImpl.Add( Format( CurlyClose, indentDecl ) );
					}
				}
			}

			private readonly GenStructStruct
				structs = new GenStructStruct();

			private readonly GenStructField
				staticFields = new GenStructField(),
				fields       = new GenStructField();

			private readonly GenStructFunc
				staticFuncs = new GenStructFunc(),
				funcs       = new GenStructFunc();

			private readonly GenStructCtorDtor
				ctorDtor = new GenStructCtorDtor();

			private readonly List<GenStructBase> allGens;

			protected readonly Access defaultAccess;
			protected          Access currentAccess;

			protected Structural obj;

			public StructuralGen( Structural obj, int LevelDecl, int LevelImpl )
				: base( LevelDecl, LevelImpl )
			{
				this.obj = obj;

				defaultAccess = (obj.kind == Structural.Kind.Class)
					? Access.Private
					: Access.Public;
				currentAccess = defaultAccess;

				allGens = new List<GenStructBase> {
					structs,
					staticFields,
					staticFuncs,
					fields,
					ctorDtor,
					funcs,
				};

				allGens.ForEach( g => g.parent = this );
			}

			protected void ThrowOnInvalidAccess( ref Access access )
			{
				if( access == Access.None )
					access = currentAccess;

				// Could be outside of the Enums valid values
				if( !access.In( Access.Public, Access.Protected, Access.Private ) )
					throw new ArgumentOutOfRangeException(
						nameof( access ),
						access,
						@"Encountered invalid Accessibility; needed to be Public, Protected or Private" );
			}

			public override Strings GenDecl()
			{
				Strings ret          = new Strings();
				Access  curAccess    = defaultAccess;
				string  accessIndent = DeIndentDecl;

				allGens.ForEach( o => ret.AddRange( o.GenDecl( ref curAccess, accessIndent ) ) );

				return ret;
			}

			public override Strings GenImpl()
			{
				Strings ret = new Strings();

				allGens.ForEach( o => ret.AddRange( o.GenImpl() ) );

				return ret;
			}

			public override void AddNamespace( Namespace obj, Access access = Access.None )
			{
				throw new NotImplementedException();
			}

			// Those need to be added and kept in order
			public override void AddVar( Var obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );

				if( obj.IsStatic ) {
					staticFields.AddVar( obj, access );
				}
				else {
					fields.AddVar( obj, access );
				}
			}

			public override void AddFunc( Func obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );

				if( obj.IsStatic ) {
					staticFuncs.AddFunc( obj, access );
				}
				else {
					funcs.AddFunc( obj, access );
				}
			}

			public override void AddStruct( Structural obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				if( obj.IsStatic )
					throw new ArgumentOutOfRangeException( nameof( obj ), true, "Structurals can not be static" );

				StructuralGen gen = new StructuralGen( obj, LevelDecl + 1, LevelImpl );

				// this happens inside the children, each knows which method to call
				// e.g.: g.AddAccessor( ... );

				obj.children.ForEach( c => c.AddToGen( gen ) );

				structs.AddStruct( gen, access );
			}

			public override void AddCtorDtor( ConDestructor obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				if( obj.IsStatic )
					throw new ArgumentOutOfRangeException( nameof( obj ), true, "Con/Destructor can not be static" );

				ctorDtor.AddCtorDtor( obj, access );
			}
		}
	}
}
