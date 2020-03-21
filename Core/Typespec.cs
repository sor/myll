using System;
using System.Collections.Generic;
using System.Linq;
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
		public List<Pointer> ptrs;

		// Type resolvedType;

		public abstract string GenType();

		// public virtual string Gen( string name = "" )
		// {
		// 	// TODO maybe nameless Gen() needs a different way
		// 	return "Named Typespec " + name;
		// }
		public virtual string Gen( string name = "" )
		{
			// TODO: solve the pointer/array formatting
			return BaseGen(
				GenType()
			  + (name == ""
					? ""
					: " " + name) );
		}

		protected string BaseGen( string value )
		{
			return qual == Qualifier.None
				? value
				: qual.ToString().ToLower().Replace( ",", "" ) + " " + value; // TODO: do properly
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
			  + (string.IsNullOrEmpty( name )
					? ""
					: " " + name) );
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
		public List<Func.Param> paras;
		public Typespec         retType; // opt

		public override string GenType()
		{
			throw new NotImplementedException();
		}
	}

	// func blah(int a) // int is _type_, a is _name_
	/*public class Param
	{
		public string   name; // opt
		public Typespec type;
	}*/

	// was nestedType
	public class TypespecNested : Typespec
	{
		public List<IdTpl> idTpls;

		public override string GenType()
		{
			return idTpls
				.Select( s => s.Gen() )
				.Join( "::" );
		}
	}

	// idTplArgs
	public class IdTpl
	{
		public string            id;
		public List<TemplateArg> tplArgs; // opt

		public string Gen()
		{
			if( tplArgs.Count == 0 )
				return id;

			return string.Format(
				"{0}<{1}>",
				id,
				tplArgs
					.Select( t => t.Gen() )
					.Join( ", " ) );
		}
	}

	// tplArg
	public class TemplateArg // name OR literal OR type
	{
		public string   id; // id passed down through template
		public Typespec typespec;
		public Expr     expr; // must be a constexpr

		public string Gen()
		{
			return id
			    ?? typespec?.Gen()
			    ?? expr?.Gen();
		}
	}

	public class TplParam
	{
		public string name;
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
			Array, // Future...
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

		string Gen( string inner )
		{
			if( kind < Kind.NoNeedForBracketing_Begin ) {
				// TODO bracketing is fixed for the moment, but ugly
				if( kind == Kind.RawArray )
					return String.Format( "({0})[{1}]", inner, expr?.Gen() ?? "" );

				string a = kind switch {
					Kind.RawPtr   => "*{0}",
					Kind.PtrToAry => "*{0}",
					Kind.LVRef    => "&{0}",
					Kind.RVRef    => "&&{0}",
					_             => "{0}"
				};
				return "";
			}
			else {
				return "";
			}
		}

		public Qualifier qual;
		public Kind      kind;
		public Expr      expr; // opt: only for Arrays
	}
}
