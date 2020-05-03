using System;
using System.Collections.Generic;
using System.Linq;

using static System.String;
using static Myll.Generator.StmtFormatting;

namespace Myll.Core
{
	// TODO precompile all permutation strings for performance
	[Flags]
	public enum Qualifier
	{
		None     = 0,
		Const    = 1 << 0,
		Mutable  = 1 << 1,
		Volatile = 1 << 2,
		Stable   = 1 << 3,
	}

	/// <summary>
	/// Formatting with the name needs to be done in here,
	/// in C++ the name is mixed up in the type
	/// </summary>
	public abstract class Typespec
	{
		public SrcPos        srcPos;
		public Qualifier     qual;
		public List<Pointer> ptrs;	// TODO: these are needed in reverse order, maybe store them reversed

		// Type resolvedType;

		public abstract string GenType();

		public virtual string Gen( string name = "" )
		{
			// TODO: solve the pointer/array formatting
			return BaseGen(
				GenType()
			  + (IsNullOrEmpty( name )
					? PointerizeName()
					: " " + PointerizeName( name )) );
		}

		public string PointerizeName( string name = "" )
		{
			bool workOnName = true;
			foreach( Pointer ptr in ptrs?.AsEnumerable().Reverse() ) {
				// when this is false once, it will remain false
				workOnName &= ptr.kind < Pointer.Kind.NoNeedForBracketing_Begin;
				if( workOnName ) {
					// TODO bracketing works for the moment, but ugly since it always brackets subscript operators
					if( ptr.kind == Pointer.Kind.RawArray ) {
						if( name == "" ) {
							name = Format( "[{0}]", ptr.expr?.Gen() ?? "" );
						}
						else {
							name = Format( "({0})[{1}]", name, ptr.expr?.Gen() ?? "" );
						}
					}
					else {
						string fmt = ptr.kind switch {
							Pointer.Kind.RawPtr   => "*{0}",
							Pointer.Kind.PtrToAry => "*{0}",
							Pointer.Kind.LVRef    => "&{0}",
							Pointer.Kind.RVRef    => "&&{0}",
							_                     => "{0}"
						};
						name = Format( fmt, name );
					}
				}
				else {
					throw new NotImplementedException( "smartpointers and containers as pointers not working yet" );
					// TODO: get smartpointers and containers working as well
				}
			}
			return name;
		}

		protected string BaseGen( string value )
		{
			// TODO: do properly
			return qual == Qualifier.None
				? value
				: qual.ToString().ToLower().Replace( ",", "" ) + " " + value;
		}
	}

	public class TypespecBasic : Typespec
	{
		public enum Kind
		{
			Auto,
			Void,
			Bool,
			Char,
			String,
			Float,
			Binary,
			Integer,
			Unsigned,
		}

		// deprecated, never used, maybe for dynamically sized integer in the future
		[Flags]
		public enum Modifier
		{
			None     = 0,
			Signed   = 1 << 0,
			Unsigned = 1 << 1,
			Size     = 1 << 2,
			Unsized  = 1 << 3,
		}

		public const int  SizeUndetermined = -1;
		public const int  SizeInvalid      = -2;
		public       int  size;  // in bytes, -1 not yet determined, -2 invalid
		public       int  align; // in bytes
		public       Kind kind;

		public override string Gen( string name = "" )
		{
			return BaseGen(
				GenType()
			  + (IsNullOrEmpty( name )
					? PointerizeName()
					: " " + PointerizeName( name )) );
		}


		public override string GenType()
		{
			return BasicFormat[kind][size];
		}
	}

	// myll:
	//   var func<int,4>(int a, float b) fun;
	// c++:
	//   named: void (*fun)(int,float)
	//   unnamed: void (*)(int, float)
	// In here the Template are Args and in the Parens are Params
	public class TypespecFunc : Typespec
	{
		public List<Param> paras;
		public Typespec    retType; // opt

		// bool (*hans)(int)
		public override string Gen( string name = "" )
		{
			// decide if one * is there by default
			return BaseGen(
				Format(
					"{0}({1})({2})",
					GenType(),
					PointerizeName( name ),
					paras
						.Select( p => p.Gen() )
						.Join( ", " ) ) );
		}

		public override string GenType()
		{
			return retType.Gen();
		}
	}

	// was nestedType
	public class TypespecNested : Typespec
	{
		public List<IdTplArgs> idTpls;

		public override string GenType()
		{
			return idTpls
				.Select( s => s.Gen() )
				.Join( "::" );
		}
	}

	public class Pointer
	{
		public enum Kind
		{
			RawPtr,
			PtrToAry,
			LVRef, // Ref
			RVRef,
			RawArray,
			NoNeedForBracketing_Begin, // Sentinel
			Unique,
			Shared,
			Weak,
			Array,
			Vector,
			Set,
			OrderedSet,
			MultiSet,
			OrderedMultiSet,
		}

		private readonly IDictionary<Kind, string>
			template = new Dictionary<Kind, string> {
				{ Kind.RawPtr,		"{0} *{1}" },
				{ Kind.PtrToAry,	"{0} *{1}" },
				{ Kind.LVRef,		"{0} &{1}" },
				{ Kind.RVRef,		"{0} &&{1}" },
				{ Kind.RawArray,	"{0} ({1})[{2}]" }, // TODO: named types must be embedded
				{ Kind.Unique,		"std::unique_ptr<{0}> {1}" },
				{ Kind.Shared,		"std::shared_ptr<{0}> {1}" },
				{ Kind.Weak,		"std::weak_ptr<{0}> {1}" },
				{ Kind.Array,		"std::array<{0},{2}> {1}" },
				{ Kind.Vector,		"std::vector<{0}> {1}" },
				{ Kind.Set,			"std::unordered_set<{0}> {1}" },
				{ Kind.OrderedSet,	"std::set<{0}> {1}" },
				{ Kind.MultiSet,	"std::unordered_multiset<{0}> {1}" },
				{ Kind.OrderedMultiSet, "std::multiset<{0}> {1}" },
			};

		public Qualifier qual;
		public Kind      kind;
		public Expr      expr; // opt: only for Arrays
	}
}
