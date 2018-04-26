﻿using System;
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

	public class Expr
	{
		// TODO
	}

	public class Stmt
	{
		// TODO
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
			String,
			Float,
			Binary,
			Integer,
			Unsigned,
		}

		[Flags]
		public enum Modifier
		{
			None     = 0,
			Signed   = 1 << 0,
			Unsigned = 1 << 1,
			Size     = 1 << 2,
			Unsized  = 1 << 3,
		}

		public int  size;  // in bytes, -1 not yet determined, -2 invalid
		public int  align; // in bytes
		public Kind kind;
	}

	// var func<int,4>(int a, float b) fun; // Here in the Template are Args and in the Parens are Params
	public class TypespecFunc : Typespec
	{
		public List<TemplateArg> templateArgs; // opt
		public List<Func.Param>  paras;
		public Typespec          retType; // opt
	}

	// func blah(int a) // int is _type_, a is _name_
	/*public class Param
	{
		public string   name; // opt
		public Typespec type;
	}*/

	// fac(n: 1+2) // n is matching _name_ of param, 1+2 is _expr_
	public class Arg
	{
		public string   name; // opt
		//public Expr expr;
	}

	// nestedType
	public class TypespecNested : Typespec
	{
		public List<IdentifierTpl> identifiers;
	}

	// idTplArgs
	public class IdentifierTpl
	{
		public string            name;
		public List<TemplateArg> templateArgs; // opt
	}

	// typeSpecOrLit
	public class TemplateArg // name, literal or type
	{
		public string   name; // id passed down through template
		public Expr     expr; // must be a constexpr
		public Typespec type;
	}

	public class TemplateParam
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
			Unique,
			Shared,
			Weak,
			RawArray,
			Array, // Future...
			Vector,
			Set,
			OrderedSet,
			MultiSet,
			OrderedMultiSet,
		}

		private readonly Dictionary<Kind, string> template = new Dictionary<Kind, string>
		{
			{Kind.RawPtr, "{0} * {1}"},
			{Kind.LVRef, "{0} & {1}"},
			{Kind.RVRef, "{0} && {1}"},
			{Kind.Unique, "std::unique_ptr<{0}> {1}"},
			{Kind.Shared, "std::shared_ptr<{0}> {1}"},
			{Kind.Weak, "std::weak_ptr<{0}> {1}"},
			{Kind.RawArray, "{0}[{2}] {1}"}, // TODO: named types must be embedded
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
		public Expr expr;
	}
}