﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.String;

using static Myll.Generator.StmtFormatting;

namespace Myll.Core
{
	using Strings = List<string>;

	public enum Access
	{
		None,
		Private,
		Protected,
		Public,
		Irrelevant,
	}

	/// <summary>
	/// introduces a name (most of the time)
	/// a Decl is a Stmt, do not question this for now
	/// </summary>
	public class Decl : Stmt
	{
		public string    name;
		public Access    access;
		public ScopeLeaf scope;
		public bool      IsStatic => false; // TODO
		public bool      IsInline => false; // TODO

		// TODO Symbol?

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach( var info in GetType().GetProperties() ) {
				object value = info.GetValue( this, null )
				            ?? "(null)";
				sb.Append( info.Name + ": " + value.ToString() + ", " );
			}

			sb.Length = Math.Max( sb.Length - 2, 0 );
			return "{"
			     + GetType().Name + " "
			     + name           + " "
			     + sb.ToString()  + "}";
		}

		public string FullyQualifiedName {
			get {
				string ret = name;
				for( Scope cur = scope.parent; cur?.parent != null; cur = cur.parent )
					ret = (cur.decl?.name ?? "unknown_fix_me") + "::" + ret;

				return ret;
			}
		}
	}

	// has in-order list of decls, visible from outside
	public class Hierarchical : Decl
	{
		public readonly List<Decl> children = new List<Decl>();

		public new Scope scope {
			get => base.scope as Scope;
			set => base.scope = value;
		}

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

			public string Gen()
			{
				return type.Gen( name );
			}
		}

		// fac(n: 1+2) // n is matching _name_ of param, 1+2 is _expr_
		public class Arg // Decl
		{
			public string name; // opt
			public Expr   expr;

			public string Gen()
			{
				if( !IsNullOrEmpty( name ) )
					throw new NotImplementedException( "named function arguments needs to be implemented" );

				return expr.Gen();
			}
		}

		public class Call
		{
			public List<Arg> args;
			public bool      nullCoal;

			public string Gen()
			{
				if( nullCoal )
					throw new NotImplementedException( "null coalescing for function calls needs to be implemented" );

				if( args.Count == 0 )
					return "()";

				return "( " + args.Select( a => a.Gen() ).Join( ", " ) + " )";
			}
		}

		public List<TplParam> tplParams;
		public List<Param>    paras;
		public Stmt           block;
		public Typespec       retType;

		public override void AddToGen( DeclGen gen )
		{
			gen.AddFunc( this, access );
		}
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

	/**
	<remarks>
		This needs to know if it needs to output "typename" in front of the type.
		This needs to have been created by a var or field decl,
			or from <see cref="Operand.WildId"/> and <see cref="Operand.DiscardId"/> in an earlier step
	</remarks>
	<example>
		var int i { [inline] get; [inline] set; } = 99;
	</example>
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
		public List<Accessor> accessor; // opt, structural or global
		public Expr           init;     // opt

		public override void AddToGen( DeclGen gen )
		{
			gen.AddVar( this, access );
		}
	}

	// TODO: rename class, its not a stmt anymore
	public class VarsStmt : Decl
	{
		public List<Var> vars;

		public override void AddToGen( DeclGen gen )
		{
			vars.ForEach( v => v.AddToGen( gen ) );
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
		public override void AddToGen( DeclGen gen )
		{
			gen.AddNamespace( this );
		}
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
		public List<TplParam>       tplParams;
		public List<TypespecNested> bases;
		public List<TypespecNested> reqs;

		public Access currentAccess;

		public override void AddToGen( DeclGen gen )
		{
			gen.AddStruct( this, access );
		}
	}
}
