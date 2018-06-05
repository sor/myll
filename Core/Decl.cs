using System;
using System.Collections.Generic;

namespace Myll.Core
{
	/*
	 scope ganz losgeloest von stmt und decl maybe?
	 */

	public enum Accessability
	{
		None,
		Public,
		Protected,
		Private,
	}

	// introduces a name (most of the time)
	public class Decl : Stmt
	{
		public string        name;
		public Accessability accessability;

		// TODO Symbol?
	}

	// has in-order list of decls, visible from outside
	public class Container : Decl
	{
		public List<Decl> children = new List<Decl>();
		public Scope      scope    = new Scope();

		public void AddChild( Decl decl )
		{
			children.Add( decl );
			scope.AddChild( decl );
		}
	}

	// has fast indexable decls, NOT directly visible from outside
	public class Scope
	{
		public Dictionary<string, List<Decl>>
			children = new Dictionary<string, List<Decl>>();

		// TODO: List<Using>, here or in Container

		public void AddChild( Decl decl )
		{
			List<Decl> list;
			if( !children.TryGetValue( decl.name, out list ) ) {
				list = new List<Decl>( 1 );
				children.Add( decl.name, list );
			}
			list.Add( decl );
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
		}

		public Scope               scope;
		public List<TemplateParam> templateParams;
		public List<Param>         paras;
		public Stmt                block;
		public Typespec            retType;
	}

	// Constructor / Destructor
	public class Structor : Container
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

	public class Enum : Container
	{
		public class Entry : Decl
		{
			public Expr value;
		}

		// the enum entries are stored here and not in DeclContainer::children
		public List<Entry> entries = new List<Entry>();
		public bool        flags;
	}

	public class Namespace : Container
	{
		public bool withBody;

		// TODO: what is needed here?
	}

	public class Structural : Container
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

		public Accessability currentAccessability;
	}
}
