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
			=> Format( "[{0}:{1}:{2}]", file, from.line, from.col );
	}

	// all following classes do not have a SrcPos, only the Expr have

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

	// func blah(  int  n     )
	//           \ type name /
	public class Param
	{
		public Typespec type;
		public string?  name;

		public string Gen()
			=> type.Gen( name );
	}

	// blah(  n:   1+2   )
	//      \ name expr /
	public class Arg // Decl
	{
		public string? name;
		public Expr    expr;

		public string Gen()
		{
			if( !IsNullOrEmpty( name ) )
				throw new NotImplementedException( "named function arguments needs to be implemented" );

			return expr.Gen();
		}
	}

	//     \/-nullCoal
	// blah?( "moep", n: 1+2 )     moep[ 1  ]
	//     \      args       / indexer \args/
	public class FuncCall
	{
		public List<Arg> args;
		public bool      indexer;
		public bool      nullCoal;

		public string Gen()
		{
			if( nullCoal ) {
				throw new NotImplementedException( "null coalescing for function calls needs to be implemented" );
			}
			else if( indexer ) {
				// TODO: call a different method that can handle more than one parameter
				if( args.Count != 1 )
					throw new Exception( "indexer call with != 1 arguments" );

				return Format(
					"[{0}]",
					args
						.Select( a => a.Gen() )
						.Join( ", " ) );
			}
			else if( args.Count == 0 ) {
				return "()";
			}
			else {
				return Format(
					"( {0} )",
					args
						.Select( a => a.Gen() )
						.Join( ", " ) );
			}
		}
	}

	public class IdTplArgs
	{
		public string       id;
		public List<TplArg> tplArgs = new();

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
			=> lit?.Gen()
			?? typespec?.Gen();
	}

	public class TplParam
	{
		public string name;
	}
}
