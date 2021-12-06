using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

using static System.String;
using static Myll.Generator.StmtFormatting;

namespace Myll.Core
{
	// TODO: precompute all permutations for performance
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
		public List<Pointer> ptrs; // TODO: these are needed in reverse order, maybe store them reversed

		// Type resolvedType;

		public abstract string GenType();

		public virtual string Gen( string name = "" )
			=> PointerizeName( GenQualifiers() + GenType(), name );

		// r-padded qualifiers TODO: do properly
		[Pure]
		protected string GenQualifiers()
			=> (qual == Qualifier.None)
				? ""
				: qual.ToString().ToLower().Replace( ",", "" ) + " ";

		protected string PointerizeName(string leftOfName, string name = "" )
		{
			name = (" " + name).TrimEnd(); // empty name will result in empty string

			if( ptrs == null || ptrs.IsEmpty() )
				return leftOfName + name;

			bool   wasArray    = false; // needs to be remembered from last iteration
			string rightOfName = "";
			foreach( Pointer ptr in ptrs.AsEnumerable() ) {
				bool needNoParens = ptr.kind.Between( Pointer.Kind.NoNeedForParens_Begin,
				                                      Pointer.Kind.NoNeedForParens_End );
				if( needNoParens ) {
					leftOfName  = leftOfName + rightOfName;
					rightOfName = "";
				}
				else if( wasArray && name != "" ) {
					leftOfName  = leftOfName + "(";
					rightOfName = ")"        + rightOfName;
				}

				wasArray = ptr.kind == Pointer.Kind.RawArray;
				string tpl   = Pointer.template[ptr.kind];
				string index = ptr.expr?.Gen() ?? "";
				if( wasArray ) {
					rightOfName = Format( tpl, rightOfName, index );
				}
				else {
					leftOfName = Format( tpl, leftOfName, index );
				}
			}

			return leftOfName + name + rightOfName;
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

		[Flags, Obsolete( "never used, maybe for dynamically sized integer in the future" )]
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

		public override string GenType()
			=> BasicFormat[kind][size];
	}

	// myll:
	//   var func<int,4>(int a, float b) fun;
	// c++:
	//   named: void (*fun)(int,float)
	//   unnamed: void (*)(int, float)
	// In here the Template are Args and in the Parens are Params,
	// this means the Template part is not visible in the resulting type of the function pointer
	public class TypespecFunc : Typespec
	{
		public List<Param> paras;
		public Typespec    retType; // opt

		public override string GenType()
			=> retType.Gen();

		// bool (*hans)(int)
		public override string Gen( string name = "" )
		{
			string center = PointerizeName( "", name );
			return Format(
				"{0}{1}({2})",
				GenQualifiers() + GenType(),
				center.Brace( center != "" ),
				paras
					.Select( p => p.Gen() )
					.Join( ", " ) );
		}
	}

	// was nestedType
	public class TypespecNested : Typespec
	{
		// TODO remember if a Ctor/Dtor was in the last spot, eg *::ctor
		public List<IdTplArgs> idTpls;

		public override string GenType()
			=> idTpls
				.Select( s => s.Gen() )
				.Join( "::" );
	}

	public class Pointer
	{
		public enum Kind
		{
			NeedParens_Begin,
			RawPtr,
			PtrToAry,
			LVRef, // Ref
			RVRef,
			RawArray,
			NeedParens_End,
			NoNeedForParens_Begin,
			SmartPtr_Begin,
			Unique,
			Shared,
			Weak,
			UniqueArray,
			SharedArray,
			WeakArray,
			SmartPtr_End,
			Array,
			Vector,
			Set,
			OrderedSet,
			MultiSet,
			OrderedMultiSet,
			Map,	// TODO: add below
			OrderedMap,
			MultiMap,
			OrderedMultiMap,
			NoNeedForParens_End,
		}

		public static readonly IReadOnlyDictionary<Kind, string>
			template = new Dictionary<Kind, string> {
				{ Kind.RawPtr,			"{0}*" },
				{ Kind.PtrToAry,		"{0}*" },
				{ Kind.LVRef,			"{0}&" },
				{ Kind.RVRef,			"{0}&&" },
				{ Kind.RawArray,		"{0}[{1}]" },
				{ Kind.Unique,			"std::unique_ptr<{0}>" },
				{ Kind.UniqueArray,		"std::unique_ptr<{0}[]>" },
				{ Kind.Shared,			"std::shared_ptr<{0}>" },
				{ Kind.SharedArray,		"std::shared_ptr<{0}[]>" },
				{ Kind.Weak,			"std::weak_ptr<{0}>" },
				{ Kind.WeakArray,		"std::weak_ptr<{0}[]>" },
				{ Kind.Array,			"std::array<{0},{1}>" },
				{ Kind.Vector,			"std::vector<{0}>" },
				{ Kind.Set,				"std::unordered_set<{0}>" },
				{ Kind.OrderedSet,		"std::set<{0}>" },
				{ Kind.MultiSet,		"std::unordered_multiset<{0}>" },
				{ Kind.OrderedMultiSet, "std::multiset<{0}>" },
			};

		public Qualifier qual;
		public Kind      kind;
		public Expr      expr; // opt: only for Arrays
	}
}
