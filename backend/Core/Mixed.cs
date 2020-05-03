using System;
using System.Collections.Generic;
using System.Linq;

namespace Myll.Core
{
	using static String;

	public class SrcPos
	{
		public struct LineCol
		{
			public int line;
			public int col;
		}

		public string  file;
		public LineCol from;
		public LineCol to;

		public override string ToString()
		{
			return Format( "[{0}:{1}:{2}]", file, from.line, from.col );
		}
	}

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

	// func blah(int a) // int is _type_, a is _name_
	public class Param
	{
		public Typespec type;
		public string   name; // opt

		public string Gen()
		{
			return type.Gen( name );
		}
	}

	// blah(n: 1+2) // n is matching _name_ of param, 1+2 is _expr_
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

	public class FuncCall
	{
		public List<Arg> args;
		public bool      indexer;
		public bool      nullCoal;

		public string Gen()
		{
			if( nullCoal )
				throw new NotImplementedException( "null coalescing for function calls needs to be implemented" );

			if( indexer ) {
				// TODO: call a different method that can handle more than one parameter
				if( args.Count != 1 )
					throw new Exception( "indexer call with != 1 arguments" );

				return "["
				     + args.Select( a => a.Gen() )
					       .Join( ", " )
				     + "]";
			}
			else {
				if( args.Count == 0 )
					return "()";

				return "( "
				     + args.Select( a => a.Gen() )
					       .Join( ", " )
				     + " )";
			}
		}
	}

	public class IdTplArgs
	{
		public string       id;
		public List<TplArg> tplArgs; // opt

		public string Gen()
		{
			if( tplArgs.Count == 0 )
				return id;

			return Format(
				"{0}<{1}>",
				id,
				tplArgs
					.Select( t => t.Gen() )
					.Join( ", " ) );
		}
	}

	public class TplArg // literal OR type
	{
		public Typespec typespec;
		public Literal  lit;

		public string Gen()
		{
			return lit?.Gen()
			    ?? typespec?.Gen();
		}
	}

	public class TplParam
	{
		public string name;
	}
}
