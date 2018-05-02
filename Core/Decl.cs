using System;
using System.Collections.Generic;

namespace Myll.Core
{
	public class Decl
	{
		public string name;
		public string srcFile;
		public uint   srcLine;

		// Symbol
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
		public class Param : Decl
		{
			public Typespec type;
		}

		public List<TemplateParam> templateParams;
		public List<Param>         paras;
		public Stmt                block;
		public Typespec            retType;
	}
}