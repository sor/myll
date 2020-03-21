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
		public static readonly string IndentString = "    "; // \t
		public static readonly string ThrowFormat  = "{0}throw {1};";
		public static readonly string BreakFormat  = "{0}break;";
		public static readonly string CurlyOpen    = "{0}{{";
		public static readonly string CurlyClose   = "{0}}}";
		public static readonly string CurlyCloseSC = "{0}}};";

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
			"{0}for( {1}; {2}; {3} )",
			"{0}while( {1} )",
			"{0}do",
			"{0}while( {1} );",
		};

		public static readonly string[] VarFormat = {
			"{0}{1}{2}{3};", // 0 indent, 1 typename, 2 type & name, 3 init
			"typename ",
			" = ",
		};

		public static readonly string[] FuncFormat = {
			"{0}{1}{2} {3}({4}){5}", // 0 indent, 1 leading attributes, 2 return type, 3 name, 4 params, 5 trailing attributes
			"{0}",
		};

		public static readonly string[] StructFormat = {
			"{0}{1}{2} {3}{4}{5}", // 0 indent, 1 keyword, 2 attributes, 3 name, 4 final, 5 bases or semicolon
			"{0}{{",               // 0 indent
			"{0}}};",              // 0 indent
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

				string indent = DeIndentDecl;

				// -1 is global
				if( LevelDecl > -1 ) {
					AllDecl.Add( Format( "{0}namespace {1}", indent, obj.name ) );
					AllDecl.Add( Format( "{0}{{", indent ) );
				}
				AllDecl.AddRange( gen.GenDecl() );
				if( LevelDecl > -1 ) {
					AllDecl.Add( Format( "{0}}}", indent ) );
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
						needsTypename ? VarFormat[1] : "",
						obj.type.Gen( name ),
						obj.init != null ? VarFormat[2] + obj.init.Gen() : "" )
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
				if( obj.IsStatic )
					throw new ArgumentOutOfRangeException( nameof( obj ), true, "Structurals can not be static" );

				StructuralGen gen = new StructuralGen( obj, LevelDecl + 1, LevelImpl );

				// this happens inside the children, each knows which method to call
				// e.g.: g.AddAccessor( ... );

				obj.children.ForEach( c => c.AddToGen( gen ) );

				StructuralGen.GenStructStruct
					structsDecl = new StructuralGen.GenStructStruct(),
					structsImpl = new StructuralGen.GenStructStruct();

				structsDecl.parent = gen;

				structsDecl.AddStructDecl( gen, access );
				structsImpl.AddStructImpl( gen, access );

				// TODO: do this later for prototypes to work properly
				string accessIndent = DeIndentDecl;
				Access curAccess = Access.None;
				AllDecl.AddRange( structsDecl.GenDecl( ref curAccess, accessIndent ) );
				AllImpl.AddRange( structsImpl.GenImpl() );

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
			internal abstract class GenStructBase
			{
				protected AccessStrings genList;
				internal  DeclGen       parent;

				// These GenDecl/GenImpl are the alternative to two derived classes just for these methods
				public Strings GenDecl( ref Access curAccess, string accessIndent )
				{
					Strings ret = new Strings();
					GenList( ret, genList, accessIndent, ref curAccess );
					return ret;
				}

				public Strings GenImpl()
				{
					Strings ret = new Strings();
					GenList( ret, genList );
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
					if( !access.In( Access.Public, Access.Protected, Access.Private ) )
						throw new ArgumentOutOfRangeException(
							nameof( access ),
							access,
							@"Encountered invalid Accessibility; needed to be Public, Protected or Private" );
				}
			}

			/// Generates a Struct inside a Struct
			internal class GenStructStruct : GenStructBase
			{
				// Super memory inefficient but I don't care for the moment
				protected readonly Strings
					privProto  = new Strings(),
					protProto  = new Strings(),
					pubProto   = new Strings(),
					privStruct = new Strings(),
					protStruct = new Strings(),
					pubStruct  = new Strings();

				#region Target Switches

				protected Strings TargetProto( Access access )
					=> access switch {
						Access.Private   => privProto,
						Access.Protected => protProto,
						_                => pubProto,
					};

				protected Strings TargetStruct( Access access )
					=> access switch {
						Access.Private   => privStruct,
						Access.Protected => protStruct,
						_                => pubStruct,
					};

				#endregion

				public GenStructStruct()
				{
					genList = new AccessStrings {
						(Access.Private,    privProto),
						(Access.Protected,  protProto),
						(Access.Public,     pubProto),
						(Access.Irrelevant, new Strings { "" }),
						(Access.Private,    privStruct),
						(Access.Protected,  protStruct),
						(Access.Public,     pubStruct),
					};
				}

				public void AddStructDecl( StructuralGen gen, Access access = Access.None )
				{
					Strings targetProto  = TargetProto( access );
					Strings targetStruct = TargetStruct( access );
					string  indent       = gen.DeIndentDecl;
					string  name         = gen.obj.name;

					// "{0}{1}{2} {3}{4}{5}",
					// 0 indent, 1 keyword, 2 attributes, 3 name, 4 final, 5 bases
					targetProto.Add(
						Format(
							StructFormat[0],
							indent,
							StructFormat[6], // TODO: is currently hardcoded to class
							"",
							name,
							"",
							";" ) );

					targetStruct.Add(
						Format(
							StructFormat[0],
							indent,
							StructFormat[6], // TODO: is currently hardcoded to class
							"",
							name,
							"",
							"" /*StructFormat[2]*/ ) );
					targetStruct.Add( Format( StructFormat[1], indent ) ); // "{"
					targetStruct.AddRange( gen.GenDecl() );
					targetStruct.Add( Format( StructFormat[2], indent ) ); // "};"
				}

				public void AddStructImpl( StructuralGen gen, Access access = Access.None )
				{
					Strings target = TargetStruct( access );

					target.AddRange( gen.GenImpl() );
				}
			}

			/// Fields are special, they can not be grouped by ppp or sorted
			private class GenStructField : GenStructBase
			{
				public GenStructField()
				{
					genList = new AccessStrings();
				}

				public void AddVarDecl( Var obj, Access access = Access.None )
				{
					string indent        = parent.IndentDecl;
					bool   needsTypename = false; // TODO how to determine this
					string name          = obj.name;
					//"{0}{1}{2}{3};", // 0 indent, 1 typename, 2 type & name, 3 init
					Strings ret = new Strings {
						Format(
							VarFormat[0],
							indent,
							needsTypename ? VarFormat[1] : "",
							obj.type.Gen( name ),
							obj.init != null ? VarFormat[2] + obj.init.Gen() : "" )
					};
					genList.Add( (access, ret) );
				}

				public void AddVarImpl( Var obj, Access access = Access.None )
				{
					if( !obj.IsStatic )
						return;

					string indent        = parent.IndentImpl;
					bool   needsTypename = false; // TODO how to determine this
					string name          = obj.FullyQualifiedName;
					Strings ret = new Strings {
						Format(
							VarFormat[0],
							indent,
							needsTypename ? VarFormat[1] : "",
							obj.type.Gen( name ),
							obj.init != null ? VarFormat[2] + obj.init.Gen() : "" )
					};
					genList.Add( (access, ret) );
				}
			}

			private class GenStructFunc : GenStructBase
			{
				// Super memory inefficient but I don't care for the moment
				protected readonly Strings
					privAccessor = new Strings(),
					protAccessor = new Strings(),
					pubAccessor  = new Strings(),
					privOperator = new Strings(),
					protOperator = new Strings(),
					pubOperator  = new Strings(),
					privMethod   = new Strings(),
					protMethod   = new Strings(),
					pubMethod    = new Strings();

				#region Target Switches

				protected Strings TargetAccessor( Access access )
					=> access switch {
						Access.Private   => privAccessor,
						Access.Protected => protAccessor,
						_                => pubAccessor,
					};

				protected Strings TargetOperator( Access access )
					=> access switch {
						Access.Private   => privOperator,
						Access.Protected => protOperator,
						_                => pubOperator,
					};

				protected Strings TargetMethod( Access access )
					=> access switch {
						Access.Private   => privMethod,
						Access.Protected => protMethod,
						_                => pubMethod,
					};

				#endregion

				public GenStructFunc()
				{
					genList = new AccessStrings {
						(Access.Private, privAccessor),
						(Access.Protected, protAccessor),
						(Access.Public, pubAccessor),
					//	(Access.Irrelevant, new Strings { "" }),
						(Access.Private, privOperator),
						(Access.Protected, protOperator),
						(Access.Public, pubOperator),
					//	(Access.Irrelevant, new Strings { "" }),
						(Access.Private, privMethod),
						(Access.Protected, protMethod),
						(Access.Public, pubMethod),
					};
				}

				// Alternative to two derived classes just for these methods
				public void AddFuncDecl( Func obj, Access access = Access.None )
				{
					Strings target = TargetMethod( access );
					string  indent = parent.IndentDecl;
					string  name   = obj.name;
					target.Add(
						Format(
							FuncFormat[0],
							indent,
							"",
							obj.retType.Gen(),
							name,
							"params",
							";" ) );
				}

				// Alternative to two derived classes just for these methods
				public void AddFuncImpl( Func obj, Access access = Access.None )
				{
					Strings target = TargetMethod( access );
					string  indent = parent.IndentImpl;
					string  name   = obj.FullyQualifiedName;
					target.Add(
						Format(
							FuncFormat[0],
							indent,
							"",
							obj.retType.Gen(),
							name,
							"params",
							"" ) );
					target.AddRange( obj.block.Gen( parent.LevelImpl + 1 ) );
				}
			}

			private class GenStructCtorDtor : GenStructBase
			{
				// Super memory inefficient but don't care for the moment
				public readonly Strings
					protoGen = new Strings(), // still needed for toplevel types?
					aliasGen = new Strings(),
					typesGen = new Strings(),
					privCtor = new Strings(),
					protCtor = new Strings(),
					pubCtor  = new Strings(),
					privDtor = new Strings(), // you can only have one DTor,
					protDtor = new Strings(), // but this makes it simpler to generate the code
					pubDtor  = new Strings();

				#region Target Switches

				protected Strings TargetCtor( Access access )
					=> access switch {
						Access.Private   => privCtor,
						Access.Protected => protCtor,
						_                => pubCtor,
					};

				protected Strings TargetDtor( Access access )
					=> access switch {
						Access.Private   => privDtor,
						Access.Protected => protDtor,
						_                => pubDtor,
					};

				#endregion

				public GenStructCtorDtor()
				{
					genList = new AccessStrings {
						(Access.Private, privCtor),
						(Access.Protected, protCtor),
						(Access.Public, pubCtor),
						(Access.Private, privDtor),
						(Access.Protected, protDtor),
						(Access.Public, pubDtor),
					};
				}

				public void AddCtorDtor( ConDestructor obj, Access access = Access.None )
				{
					Strings target = (obj.kind == ConDestructor.Kind.Constructor)
						? TargetCtor( access )
						: TargetDtor( access );

					// TODO
					//target.AddRange( null );
				}
			}

			private readonly GenStructStruct
				structsDecl = new GenStructStruct(),
				structsImpl = new GenStructStruct();

			private readonly GenStructField
				staticFieldsDecl = new GenStructField(),
				staticFieldsImpl = new GenStructField(),
				fieldsDecl       = new GenStructField(),
				fieldsImpl       = new GenStructField();

			private readonly GenStructFunc
				staticFuncsDecl = new GenStructFunc(),
				staticFuncsImpl = new GenStructFunc(),
				funcsDecl       = new GenStructFunc(),
				funcsImpl       = new GenStructFunc();

			private readonly GenStructCtorDtor
				ctorDtorDecl = new GenStructCtorDtor(),
				ctorDtorImpl = new GenStructCtorDtor();

			private readonly List<GenStructBase> allGensDecl;
			private readonly List<GenStructBase> allGensImpl;

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

				allGensDecl = new List<GenStructBase> {
					structsDecl,
					staticFieldsDecl,
					staticFuncsDecl,
					fieldsDecl,
					ctorDtorDecl,
					funcsDecl,
				};

				allGensImpl = new List<GenStructBase> {
					structsImpl,
					staticFieldsImpl,
					staticFuncsImpl,
					fieldsImpl,
					ctorDtorImpl,
					funcsImpl,
				};

				allGensDecl.ForEach( g => g.parent = this );
				allGensImpl.ForEach( g => g.parent = this );
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

				allGensDecl.ForEach( o => ret.AddRange( o.GenDecl( ref curAccess, accessIndent ) ) );

				return ret;
			}

			public override Strings GenImpl()
			{
				Strings ret = new Strings();

				allGensImpl.ForEach( o => ret.AddRange( o.GenImpl() ) );

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
					staticFieldsDecl.AddVarDecl( obj, access );
					staticFieldsImpl.AddVarImpl( obj, access );
				}
				else {
					fieldsDecl.AddVarDecl( obj, access );
					fieldsImpl.AddVarImpl( obj, access );
				}
			}

			public override void AddFunc( Func obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );

				if( obj.IsStatic ) {
					staticFuncsDecl.AddFuncDecl( obj, access );
					staticFuncsImpl.AddFuncImpl( obj, access );
				}
				else {
					funcsDecl.AddFuncDecl( obj, access );
					funcsImpl.AddFuncImpl( obj, access );
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

				structsDecl.AddStructDecl( gen, access );
				structsImpl.AddStructImpl( gen, access );
			}

			// TODO
			public override void AddCtorDtor( ConDestructor obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				if( obj.IsStatic )
					throw new ArgumentOutOfRangeException( nameof( obj ), true, "Con/Destructor can not be static" );

				ctorDtorDecl.AddCtorDtor( obj, access );
				ctorDtorImpl.AddCtorDtor( obj, access );
			}
		}
	}
}
