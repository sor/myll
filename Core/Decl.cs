using System;
using System.Collections.Generic;

namespace Myll.Core
{
	// introduces a name (most of the time)
	public class Decl
	{
		public string name;
		public string srcFile;
		public uint   srcLine;
		public uint   srcCol;

		// TODO Symbol
	}

	public class Func : Decl
	{
		public class Param
		{
			public Typespec type;
			public string   name;
		}

		// fac(n: 1+2) // n is matching _name_ of param, 1+2 is _expr_
		public class Arg // Decl
		{
			public string name; // opt
			public Expr   expr;
		}

		public class Call
		{
			public List<Arg> args;
		}

		public List<TemplateParam> templateParams;
		public List<Param>         paras;
		public Stmt                block;
		public Typespec            retType;
	}

	public class Using : Decl
	{
		public List<TypespecNested> types;
	}

	public class Var : Decl
	{
		public class Accessor
		{
			public enum Kind
			{
				Get,
				RefGet,
				Set,
			}

			public Kind kind;
			public Stmt body;
			public bool isConst;

			// TODO: maybe Qualifier instead of isConst?
		}

		public Typespec       type;
		public List<Accessor> accessor; // opt
		public Expr           init;     // opt
		public bool           isConst;

		// TODO: maybe Qualifier instead of isConst?
	}

	// introduces a scope and has child decls
	public class DeclContainer : Decl
	{
		public List<Decl>               children;
		public Dictionary<string, Decl> namedChildren;
	}

	public class Enum : DeclContainer
	{
		public class Entry : Decl
		{
			public Expr value;
		}

		// the enum entries are stored here and not in DeclContainer::children
		public List<Entry> entries = new List<Entry>();
		public bool        flags;
	}

	public class Namespace : DeclContainer
	{
		// TODO: this must already be expanded to multiple nested namespace objects
		public List<string> names;
	}

	public class Class : DeclContainer
	{
	}
}
