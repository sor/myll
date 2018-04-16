using System;
using System.Collections.Generic;

namespace Myll.Core
{
	[Flags]
	public enum Qualifier
	{
		None     = 0,
		Const    = 1 << 0,
		Mutable  = 1 << 1,
		Volatile = 1 << 2,
		Stable   = 1 << 3,
	}

	public class Typespec // QualType
	{
		public string        srcFile;
		public uint          srcLine;
		public Qualifier     qual;
		public List<Pointer> ptrs;

		// Type resolvedType;
	}

	public class TypespecBase : Typespec
	{
		public enum Kind
		{
			Auto,
			Void,
			Bool,
			Char,
			Float,
			Binary,
			Integer
		}

		[Flags]
		public enum Modifier
		{
			None     = 0,
			Signed   = 1 << 0,
			Unsigned = 1 << 1,
			Size     = 1 << 2,
		}

		public int  size;  // in bytes
		public int  align; // in bytes
		public Kind kind;
	}

	// var func<int,4>(int a, float b) fun; // Here in the Template are Args and in the Parens are Params
	public class TypespecFunc : Typespec
	{
		public List<TemplateArg> templateArgs; // opt
		public List<Param>       paras;
		public Typespec          ret;
	}

	// func(int a) // int is type, a is name
	public class Param
	{
		public string   name; // opt
		public Typespec type;
	}

	// fac(n: 1+2) // n is matching name of param, 1+2 is expr
	public class Arg
	{
		public string   name; // opt
		//public Expr expr;
	}

	public class TypespecNested : Typespec
	{
		public List<TemplatedIdentifier> templatedIdentifiers;
	}

	public class TemplatedIdentifier
	{
		public string            name;
		public List<TemplateArg> templateArg; // opt
	}

	public class TemplateArg // literal or type
	{
		public string   literal;
		public Typespec type;
	}

	public class Pointer
	{
		public enum Kind
		{
			Raw,
			LVRef, // Ref
			RVRef,
			RawArray,
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

		private readonly Dictionary<Kind, string> template = new Dictionary<Kind, string>
		{
			{Kind.Raw, "{0} * {1}"},
			{Kind.LVRef, "{0} & {1}"},
			{Kind.RVRef, "{0} && {1}"},
			{Kind.RawArray, "{0}[{2}] {1}"}, // TODO: named types must be embedded
			{Kind.Unique, "std::unique_ptr<{0}> {1}"},
			{Kind.Shared, "std::shared_ptr<{0}> {1}"},
			{Kind.Weak, "std::weak_ptr<{0}> {1}"},
			{Kind.Array, "std::array<{0},{2}> {1}"},
			{Kind.Vector, "std::vector<{0}> {1}"},
			{Kind.Set, "std::unordered_set<{0}> {1}"},
			{Kind.OrderedSet, "std::set<{0}> {1}"},
			{Kind.MultiSet, "std::unordered_multiset<{0}> {1}"},
			{Kind.OrderedMultiSet, "std::multiset<{0}> {1}"},
		};

		public Qualifier qual;
		public Kind      kind;
	}

	public class Array : Pointer
	{
		//public Expr expr;
	}
}
