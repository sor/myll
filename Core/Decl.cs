using System;
using System.Collections.Generic;

using static System.String;
using static Myll.Generator.StmtFormatting;

namespace Myll.Core
{
	public enum Accessibility
	{
		None,
		Public,
		Protected,
		Private,
	}

	/// <summary>
	/// introduces a name (most of the time)
	/// a Decl is a Stmt, do not question this for now
	/// </summary>
	public class Decl : Stmt
	{
		public string        name;
		public Accessibility accessibility;
		public ScopeLeaf     scope;

		// TODO Symbol?
	}

	// has in-order list of decls, visible from outside
	public class Hierarchical : Decl
	{
		public new Scope scope {
			get => base.scope as Scope;
			set => base.scope = value;
		}

		public readonly List<Decl> children = new List<Decl>();

		// the children add themselves through AddChild or PushScope
		public void AddChild( Decl decl )
		{
			children.Add( decl );
		}
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
			public bool      nullCoal;
		}

		public List<TemplateParam> templateParams;
		public List<Param>         paras;
		public Stmt                block;
		public Typespec            retType;
	}

	// Constructor / Destructor
	public class ConDestructor : Decl
	{
		public enum Kind
		{
			Constructor,
			Destructor,
		}

		public Kind             kind;
		public List<Func.Param> paras;
		public Stmt             block;

		// TODO: initlist
	}

	public class Using : Decl
	{
		public List<TypespecNested> types;
	}

	/*
	var int i { [inline] get; [inline] set; } = 99;
	*/
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

			public Kind      kind;
			public Qualifier qual;
			public Stmt      body; // opt
		}

		public Typespec       type;     // contains Qualifier
		public List<Accessor> accessor; // opt
		public Expr           init;     // opt

		// TODO signature needs to change to provide the different locations to write too
		public override IList<string> Gen( int level )
		{
			// var int[] blah = {1,2,3};
			// int blah[] = {1,2,3};
			bool needsTypename = false; // TODO how to determine this
			return new[] {
				Format(
					VarFormat[0],
					Indent.Repeat( level ),
					needsTypename ? VarFormat[1] : "",
					type.Gen( name ),
					init != null ? VarFormat[2] + init.Gen() : "" )
			};
		}
	}

	public class Enum : Hierarchical
	{
		public class Entry : Decl
		{
			public Expr value;
		}

		public bool flags;
	}

	public class Namespace : Hierarchical
	{
		public bool withBody;

		// TODO: what is needed here?
	}

	public class Structural : Hierarchical
	{
		public enum Kind
		{
			Struct,
			Class,
			Union,
		}

		public Kind                 kind;
		public List<TemplateParam>  tplParams;
		public List<TypespecNested> bases;
		public List<TypespecNested> reqs;

		public Accessibility currentAccessibility;
	}
}
