﻿using System;
using System.Collections.Generic;

namespace Myll.Core
{
	public class Decl
	{
		public string name;
		public string srcFile;
		public uint   srcLine;
		public uint   srcCol;

		// TODO Symbol
	}

	public class Enum : Decl // is a scope
	{
		public class Entry : Decl
		{
			public Expr value;
		}

		public List<Entry> entries = new List<Entry>();
		public bool        flags;
	}

	public class Func : Decl
	{
		public class Param
		{
			public Typespec type;
			public string   name;
		}

		// fac(n: 1+2) // n is matching _name_ of param, 1+2 is _expr_
		public class Arg
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

	public class Var : Decl
	{
		public Typespec type;
		public Expr     init; // opt
		public bool     isConst;
	}
}
