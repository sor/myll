using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
		public static readonly string Indent      = "\t";
		public static readonly string ThrowFormat = "{0}throw {1};";
		public static readonly string BreakFormat = "{0}break;";

		public static readonly Dictionary<Access, string>
			AccessFormat = new Dictionary<Access, string> {
				{ Access.Public, "{0}public:" },
				{ Access.Protected, "{0}protected:" },
				{ Access.Private, "{0}private:" },
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

		public static readonly string[] VarFormat = {
			"{0}{1}{2}{3};", // 0 indent, 1 typename, 2 type & name, 3 init
			"typename ",
			" = ",
		};

		public static readonly string[] FuncFormat = {
			"{0}{1}{2} {3}({4}){5}", // 0 indent, 1 leading attributes, 2 returntype, 3 name, 4 params, 5 trailing attributes
			"{0}",
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
			public int LevelDecl { get; set; }
			public int LevelImpl { get; set; }

			/*
			public abstract void AddVar( Strings      obj, Access access = Access.None );
			public abstract void AddAccessor( Strings obj, Access access = Access.None );
			public abstract void AddOperator( Strings obj, Access access = Access.None );
			public abstract void AddFunc( Strings     obj, Access access = Access.None );
			*/

			public abstract void AddVar( Var           obj, Access access = Access.None );
			public abstract void AddFunc( Func         obj, Access access = Access.None );
			public abstract void AddStruct( Structural obj, Access access = Access.None );
		}

		// only here for a while until all the dedicated ones work
		public class SimpleGen : DeclGen
		{
			public readonly Strings AllDecl = new Strings();
			public readonly Strings AllImpl = new Strings();

			/*
			public override void AddVar( Strings obj, Access access = Access.None )
			{
				All.AddRange( obj );
			}

			public override void AddAccessor( Strings obj, Access access = Access.None )
			{
				All.AddRange( obj );
			}

			public override void AddOperator( Strings obj, Access access = Access.None )
			{
				All.AddRange( obj );
			}

			public override void AddFunc( Strings obj, Access access = Access.None )
			{
				All.AddRange( obj );
			}
			*/

			public override void AddVar( Var obj, Access access = Access.None )
			{
				string indentDecl = Indent.Repeat( LevelDecl );
				string indentImpl = Indent.Repeat( LevelImpl );

				bool needsTypename = false; // TODO how to determine this
				Strings ret = new Strings {
					Format(
						VarFormat[0],
						indentDecl,
						needsTypename ? VarFormat[1] : "",
						obj.type.Gen( obj.name ),
						obj.init != null ? VarFormat[2] + obj.init.Gen() : "" )
				};

				AllDecl.AddRange( ret );
			}

			public override void AddFunc( Func obj, Access access = Access.None )
			{
				string indentDecl = Indent.Repeat( LevelDecl );
				string indentImpl = Indent.Repeat( LevelImpl );

				Strings ret = new Strings {
					Format(
						FuncFormat[0],
						indentDecl,
						"",
						obj.retType.Gen(),
						obj.name,
						"params",
						"" )
				};

				ret.AddRange( obj.block.Gen( LevelDecl + 1 ) );

				AllDecl.AddRange( ret );
			}

			public override void AddStruct( Structural obj, Access access = Access.None )
			{
				string indentDecl = Indent.Repeat( LevelDecl );
				string indentImpl = Indent.Repeat( LevelImpl );

				StructuralInstanceGen gen = new StructuralInstanceGen(
					obj.kind == Structural.Kind.Class
						? Access.Private
						: Access.Public ) { LevelDecl = LevelDecl + 1 };

				// this happens inside the children, each knows which method to call
				// e.g.: g.AddAccessor( ... );

				obj.children.ForEach( c => c.Gen( gen ) );

				AllDecl.AddRange( gen.Gen() );
			}
		}

		// TODO: not finished at all
		public abstract class StmtGen : DeclGen
		{
			public abstract void AddHere( Strings       obj );
			public abstract void AddInnerScope( Strings obj );
			public abstract void AddOuterScope( Strings obj );

			/*
			public override void AddVar( Strings obj, Access access = Access.None )
			{
				AddInnerScope( obj );
			}

			public override void AddAccessor( Strings obj, Access access = Access.None )
				=> throw new NotSupportedException();

			public override void AddOperator( Strings obj, Access access = Access.None )
				=> throw new NotSupportedException();

			public override void AddFunc( Strings obj, Access access = Access.None )
				=> throw new NotSupportedException();
			*/
		}

		// TODO: can this be used without change for namespaces?
		/**
			StructuralStatic is a subset of StructuralInstance, inheritance for deduplication

			Structure of output:

				(using abbreviations:
					ppp = private, protected, public
					sorted = in the order of the alphabet
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

			/*
			public override void AddVar( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				allDecl.AddRange( obj );
			}

			public override void AddAccessor( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				allDecl.AddRange( obj );
			}

			public override void AddOperator( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				allDecl.AddRange( obj );
			}

			public override void AddFunc( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				// TODO: I guess I really need to pass in the originating object to get prototypes working
				allDecl.AddRange( obj );
			}
			*/

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

		/**
			StructuralStatic is a subset of StructuralInstance, inheritance for deduplication

			Structure of output (using abbreviations: ppp = private, protected, public):

				ppp ordered	fieldList
				ppp grouped	accessors
				ppp grouped	operators
				ppp grouped	methods
		*/
		public class StructuralStaticGen : DeclGen
		{
			// Super inefficient but I don't care for the moment
			protected readonly Strings
				privAccessorDecl = new Strings(),
				protAccessorDecl = new Strings(),
				pubAccessorDecl  = new Strings(),
				privAccessorImpl = new Strings(),
				protAccessorImpl = new Strings(),
				pubAccessorImpl  = new Strings(),
				privOperatorDecl = new Strings(),
				protOperatorDecl = new Strings(),
				pubOperatorDecl  = new Strings(),
				privOperatorImpl = new Strings(),
				protOperatorImpl = new Strings(),
				pubOperatorImpl  = new Strings(),
				privMethodDecl   = new Strings(),
				protMethodDecl   = new Strings(),
				pubMethodDecl    = new Strings(),
				privMethodImpl   = new Strings(),
				protMethodImpl   = new Strings(),
				pubMethodImpl    = new Strings();

			#region Target Switches
			protected Strings TargetAccessorDecl( Access access )
				=> access switch {
					Access.Public    => pubAccessorDecl,
					Access.Protected => protAccessorDecl,
					_                => privAccessorDecl,
				};

			protected Strings TargetAccessorImpl( Access access )
				=> access switch {
					Access.Public    => pubAccessorImpl,
					Access.Protected => protAccessorImpl,
					_                => privAccessorImpl,
				};

			protected Strings TargetOperatorDecl( Access access )
				=> access switch {
					Access.Public    => pubOperatorDecl,
					Access.Protected => protOperatorDecl,
					_                => privOperatorDecl,
				};

			protected Strings TargetOperatorImpl( Access access )
				=> access switch {
					Access.Public    => pubOperatorImpl,
					Access.Protected => protOperatorImpl,
					_                => privOperatorImpl,
				};

			protected Strings TargetMethodDecl( Access access )
				=> access switch {
					Access.Public    => pubMethodDecl,
					Access.Protected => protMethodDecl,
					_                => privMethodDecl,
				};

			protected Strings TargetMethodImpl( Access access )
				=> access switch {
					Access.Public    => pubMethodImpl,
					Access.Protected => protMethodImpl,
					_                => privMethodImpl,
				};
			#endregion

			protected readonly AccessStrings fieldList = new AccessStrings(); // need to stay in order

			protected readonly Access defaultAccess;
			public             Access currentAccess;

			protected AccessStrings genList;
			protected AccessStrings implList;

			public StructuralStaticGen( Access defaultAccess )
			{
				this.defaultAccess = defaultAccess;
				this.currentAccess = defaultAccess;

				genList = new AccessStrings {
					(Access.Private, privAccessorDecl),
					(Access.Protected, protAccessorDecl),
					(Access.Public, pubAccessorDecl),
					(Access.Private, privOperatorDecl),
					(Access.Protected, protOperatorDecl),
					(Access.Public, pubOperatorDecl),
					(Access.Private, privMethodDecl),
					(Access.Protected, protMethodDecl),
					(Access.Public, pubMethodDecl),
				};
				implList = new AccessStrings {
					(Access.Private, privAccessorImpl),
					(Access.Protected, protAccessorImpl),
					(Access.Public, pubAccessorImpl),
					(Access.Private, privOperatorImpl),
					(Access.Protected, protOperatorImpl),
					(Access.Public, pubOperatorImpl),
					(Access.Private, privMethodImpl),
					(Access.Protected, protMethodImpl),
					(Access.Public, pubMethodImpl),
				};
			}

			public virtual Strings Gen()
			{
				Strings ret         = new Strings();
				int     accessLevel = Math.Max( 0, LevelDecl - 1 );
				Access  curAccess   = defaultAccess;

				GenList( ret, ref curAccess, accessLevel, fieldList );
				GenList( ret, ref curAccess, accessLevel, genList );

				return ret;
			}

			public void GenList( Strings ret, ref Access curAccess, int accessLevel, AccessStrings list )
			{
				foreach( (Access access, Strings gen) in list ) {
					if( gen.Count == 0 )
						continue;

					if( access != curAccess )
						ret.Add( Format( AccessFormat[access], accessLevel ) );

					curAccess = access;
					ret.AddRange( gen );
				}
			}

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			private void ThrowOnInvalidAccess( ref Access access )
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

			/*
			// Those need to be added in order
			public override void AddVar( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				fieldList.Add( (access, obj) ); // new code, order preserving
			}

			public override void AddAccessor( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				Strings target = TargetAccessorDecl( access );
				target.AddRange( obj );
			}

			public override void AddOperator( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				Strings target = TargetOperatorDecl( access );
				target.AddRange( obj );
			}

			public override void AddFunc( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				Strings target = TargetMethodDecl( access );
				target.AddRange( obj );
			}
			*/

			// Those need to be added in order
			public override void AddVar( Var obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				string indent = Indent.Repeat( LevelDecl );

				bool needsTypename = false; // TODO how to determine this
				Strings ret = new Strings {
					Format(
						VarFormat[0],
						indent,
						needsTypename ? VarFormat[1] : "",
						obj.type.Gen( obj.name ),
						obj.init != null ? VarFormat[2] + obj.init.Gen() : "" )
				};
				fieldList.Add( (access, ret) );
			}

			public override void AddFunc( Func obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				string indent = Indent.Repeat( LevelDecl );

				Strings decl = TargetMethodDecl( access );
				decl.Add(
					Format(
						FuncFormat[0],
						indent,
						"",
						obj.retType.Gen(),
						obj.name,
						"params",
						"" ) );
				decl.AddRange( obj.block.Gen( LevelDecl + 1 ) );

				Strings impl = TargetMethodImpl( access );
				impl.Add(
					Format(
						FuncFormat[0],
						indent,
						"",
						obj.retType.Gen(),
						obj.name,
						"params",
						"" ) );
				impl.AddRange( obj.block.Gen( LevelDecl + 1 ) );
			}

			public override void AddStruct( Structural obj, Access access = Access.None )
			{
				obj.Gen( this );
				throw new NotImplementedException();
			}
		}

		/**
			StructuralStatic is a subset of StructuralInstance, inheritance for deduplication

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
		public class StructuralInstanceGen : StructuralStaticGen
		{
			public readonly StructuralStaticGen staticStaticGen;

			// Super inefficient but don't care for the moment
			public readonly Strings
				protoGen     = new Strings(), // still needed for toplevel types?
				aliasGen     = new Strings(),
				typesGen     = new Strings(),
				privCtorDecl = new Strings(),
				protCtorDecl = new Strings(),
				pubCtorDecl  = new Strings(),
				privCtorImpl = new Strings(),
				protCtorImpl = new Strings(),
				pubCtorImpl  = new Strings(),
				privDtorDecl = new Strings(), // you can only have one DTor,
				protDtorDecl = new Strings(), // but this makes it simpler to generate the code
				pubDtorDecl  = new Strings(),
				privDtorImpl = new Strings(), // you can only have one DTor,
				protDtorImpl = new Strings(), // but this makes it simpler to generate the code
				pubDtorImpl  = new Strings();

			#region Target Switches
			protected Strings TargetCtorDecl( Access access )
				=> access switch {
					Access.Public    => pubCtorDecl,
					Access.Protected => protCtorDecl,
					_                => privCtorDecl,
				};

			protected Strings TargetCtorImpl( Access access )
				=> access switch {
					Access.Public    => pubCtorImpl,
					Access.Protected => protCtorImpl,
					_                => privCtorImpl,
				};

			protected Strings TargetDtorDecl( Access access )
				=> access switch {
					Access.Public    => pubDtorDecl,
					Access.Protected => protDtorDecl,
					_                => privDtorDecl,
				};

			protected Strings TargetDtorImpl( Access access )
				=> access switch {
					Access.Public    => pubDtorImpl,
					Access.Protected => protDtorImpl,
					_                => privDtorImpl,
				};
			#endregion

			public StructuralInstanceGen( Access defaultAccess )
				: base( defaultAccess )
			{
				staticStaticGen = new StructuralStaticGen( defaultAccess ) { LevelDecl = LevelDecl };
				genList = new AccessStrings {
					(Access.Private, privCtorDecl),
					(Access.Protected, protCtorDecl),
					(Access.Public, pubCtorDecl),
					(Access.Private, privDtorDecl),
					(Access.Protected, protDtorDecl),
					(Access.Public, pubDtorDecl),
					(Access.Private, privAccessorDecl),
					(Access.Protected, protAccessorDecl),
					(Access.Public, pubAccessorDecl),
					(Access.Private, privOperatorDecl),
					(Access.Protected, protOperatorDecl),
					(Access.Public, pubOperatorDecl),
					(Access.Private, privMethodDecl),
					(Access.Protected, protMethodDecl),
					(Access.Public, pubMethodDecl),
				};
				implList = new AccessStrings {
					(Access.Private, privCtorImpl),
					(Access.Protected, protCtorImpl),
					(Access.Public, pubCtorImpl),
					(Access.Private, privDtorImpl),
					(Access.Protected, protDtorImpl),
					(Access.Public, pubDtorImpl),
					(Access.Private, privAccessorImpl),
					(Access.Protected, protAccessorImpl),
					(Access.Public, pubAccessorImpl),
					(Access.Private, privOperatorImpl),
					(Access.Protected, protOperatorImpl),
					(Access.Public, pubOperatorImpl),
					(Access.Private, privMethodImpl),
					(Access.Protected, protMethodImpl),
					(Access.Public, pubMethodImpl),
				};
			}

			public override Strings Gen()
			{
				Strings ret           = new Strings();
				int     accessLevel   = Math.Max( 0, LevelDecl - 1 );
				Access  currentAccess = defaultAccess;

				ret.AddRange( staticStaticGen.Gen() );

				GenList( ret, ref currentAccess, accessLevel, fieldList );
				GenList( ret, ref currentAccess, accessLevel, genList );

				return ret;
			}
		}

		/*
		class StructuralInstanceGenBoth
		{
			private StructuralInstanceGen decl, impl;
		}
		*/
	}
}
