using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Myll.Core;

using static System.String;

namespace Myll.Generator
{
	using Strings = List<string>;
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

		public abstract class DeclGen
		{
			public abstract void AddVarOrField( Strings   obj, Access access = Access.None );
			public abstract void AddAccessor( Strings     obj, Access access = Access.None );
			public abstract void AddOperator( Strings     obj, Access access = Access.None );
			public abstract void AddFuncOrMethod( Strings obj, Access access = Access.None );
		}

		public class GlobalGen : DeclGen
		{
			public readonly Strings
				includeGen  = new Strings(),
				protoGen    = new Strings(),
				usingGen    = new Strings(),
				aliasGen    = new Strings(),
				typesGen    = new Strings(),
				varGen      = new Strings(),
				accessorGen = new Strings(),
				operatorGen = new Strings(),
				funcGen     = new Strings();

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			private void ThrowOnInvalidAccess( Access access )
			{
				if( access != Access.None )
					throw new ArgumentOutOfRangeException(
						nameof( access ),
						access,
						@"Global Scope has no Accessibility; needed to be None" );
			}

			public override void AddVarOrField( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				varGen.AddRange( obj );
			}

			public override void AddAccessor( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				accessorGen.AddRange( obj );
			}

			public override void AddOperator( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				operatorGen.AddRange( obj );
			}

			public override void AddFuncOrMethod( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( access );
				funcGen.AddRange( obj );
			}
		}

		// StructuralStatic is a subset of StructuralInstance, inheritance for deduplication
		public class StructuralStaticGen : DeclGen
		{
			protected readonly Access defaultAccess;
			public             Access currentAccess;

			public readonly Strings
				privAccessorGen = new Strings(),
				protAccessorGen = new Strings(),
				pubAccessorGen  = new Strings(),
				privOperatorGen = new Strings(),
				protOperatorGen = new Strings(),
				pubOperatorGen  = new Strings(),
				privMethodGen   = new Strings(),
				protMethodGen   = new Strings(),
				pubMethodGen    = new Strings();

			protected          AccessStrings genList;
			protected readonly AccessStrings fieldList = new AccessStrings();

			public StructuralStaticGen( Access defaultAccess )
			{
				this.defaultAccess = defaultAccess;
				this.currentAccess = defaultAccess;

				genList = new AccessStrings {
					(Access.Private, privAccessorGen),
					(Access.Protected, protAccessorGen),
					(Access.Public, pubAccessorGen),
					(Access.Private, privOperatorGen),
					(Access.Protected, protOperatorGen),
					(Access.Public, pubOperatorGen),
					(Access.Private, privMethodGen),
					(Access.Protected, protMethodGen),
					(Access.Public, pubMethodGen),
				};
			}

			public virtual Strings Gen( int level )
			{
				Strings ret         = new Strings();
				int     accessLevel = Math.Max( 0, level - 1 );
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

			// Those need to be added in order
			public override void AddVarOrField( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				fieldList.Add( (access, obj) ); // new code, order preserving
			}

			public override void AddAccessor( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				Strings target = access switch {
					Access.Public    => pubAccessorGen,
					Access.Protected => protAccessorGen,
					_                => privAccessorGen,
				};
				target.AddRange( obj );
			}

			public override void AddOperator( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				Strings target = access switch {
					Access.Public    => pubOperatorGen,
					Access.Protected => protOperatorGen,
					_                => privOperatorGen,
				};
				target.AddRange( obj );
			}

			public override void AddFuncOrMethod( Strings obj, Access access = Access.None )
			{
				ThrowOnInvalidAccess( ref access );
				Strings target = access switch {
					Access.Public    => pubMethodGen,
					Access.Protected => protMethodGen,
					_                => privMethodGen,
				};
				target.AddRange( obj );
			}
		}

		// StructuralStatic is a subset of StructuralInstance, inheritance for deduplication
		public class StructuralInstanceGen : StructuralStaticGen
		{
			public readonly StructuralStaticGen staticStaticGen;

			public readonly Strings
				protoGen    = new Strings(),
				aliasGen    = new Strings(),
				typesGen    = new Strings(),
				privCtorGen = new Strings(),
				protCtorGen = new Strings(),
				pubCtorGen  = new Strings(),
				privDtorGen = new Strings(), // you can only have one DTor,
				protDtorGen = new Strings(), // but this makes it simpler to generate the code
				pubDtorGen  = new Strings();

			public StructuralInstanceGen( Access defaultAccess )
				: base( defaultAccess )
			{
				staticStaticGen = new StructuralStaticGen( defaultAccess );
				genList = new AccessStrings {
					(Access.Private, privCtorGen),
					(Access.Protected, protCtorGen),
					(Access.Public, pubCtorGen),
					(Access.Private, privDtorGen),
					(Access.Protected, protDtorGen),
					(Access.Public, pubDtorGen),
					(Access.Private, privAccessorGen),
					(Access.Protected, protAccessorGen),
					(Access.Public, pubAccessorGen),
					(Access.Private, privOperatorGen),
					(Access.Protected, protOperatorGen),
					(Access.Public, pubOperatorGen),
					(Access.Private, privMethodGen),
					(Access.Protected, protMethodGen),
					(Access.Public, pubMethodGen),
				};
			}

			public override Strings Gen( int level )
			{
				Strings ret           = new Strings();
				int     accessLevel   = Math.Max( 0, level - 1 );
				Access  currentAccess = defaultAccess;

				ret.AddRange( staticStaticGen.Gen( level ) );

				GenList( ret, ref currentAccess, accessLevel, fieldList );
				GenList( ret, ref currentAccess, accessLevel, genList );

				foreach( (Access access, Strings gen) in genList ) {
					if( gen.Count == 0 )
						continue;

					if( access != currentAccess )
						ret.Add( Format( AccessFormat[access], accessLevel ) );

					currentAccess = access;
					ret.AddRange( gen );
				}
				return ret;
			}
		}
	}
}
