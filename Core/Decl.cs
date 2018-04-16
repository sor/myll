using System;
using System.Collections.Generic;

namespace Myll.Core
{
	public class Decl
	{
		public MyIdDef Name;
		public string  SrcFile;
		public uint    SrcLine;

		// Symbol
	}

	public class EnumDecl : Decl // is a scope
	{
		public class Entry : Decl
		{
			public MyExpr Value;
		}

		public List<Entry> Entries = new List<Entry>();
		public bool        Flags;
	}

	public class FuncDecl : Decl
	{
		public class Param : Decl
		{
			// Typespec type
		}

		public List<Param> paras;
		// Typespec ret_type
		// List<Stmt> block
	}
}