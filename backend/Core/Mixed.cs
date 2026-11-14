using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Resolver;

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

		public string  file = null!;
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
		public Stmt?     body; // opt
	}

	// func blah(  int  n     )
	//           \ type name /
	public class Param
	{
		public Typespec type = null!;
		public string?  name;

		// The parameter's type names a pack template parameter (`<T...>`):
		// the parameter is the expansion.
		public bool IsPack
			=> type is TypespecNested { resolvedDecl: TplParamDecl { isPack: true } };

		public string Gen()
			=> IsPack
				? Format( "{0}... {1}", type.Gen(), name ?? "" )
				: type.Gen( name ?? "" );

		public string GenDiagnostic()
			=> IsPack
				? Format( "{0}... {1}", type.GenDiagnostic(), name ?? "" )
				: type.GenDiagnostic( name ?? "" );
	}

	// blah(  n:   1+2   )
	//      \ name expr /
	public class Arg // Decl
	{
		public SrcPos?  srcPos;
		public string?  name;
		public Expr     expr = null!;

		public bool IsPack => expr.Type is TypespecNested { resolvedDecl: TplParamDecl { isPack: true } };

		public string Gen()
		{
			if( !IsNullOrEmpty( name ) )
				throw new DiagnosticException( new Diagnostic(
					srcPos ?? new SrcPos { file = "<unknown>" },
					DiagnosticKind.Error,
					"named function arguments are not implemented yet" ) );

			// A pack-typed argument expands: `args` emits `args...`. The
			// (forward) cast emits its pack template argument, so the
			// expansion suffix applies there alike.
			return expr.Type is TypespecNested { resolvedDecl: TplParamDecl { isPack: true } }
				? expr.Gen() + "..."
				: expr.Gen();
		}
	}

	//     \/-nullCoal
	// blah?( "moep", n: 1+2 )     moep[ 1  ]
	//     \      args       / indexer \args/
	public class FuncCall
	{
		public SrcPos?    srcPos;
		public List<Arg>  args = new();
		public bool       indexer;
		public bool       nullCoal;

		public string Gen()
		{
			if( nullCoal ) {
				throw new DiagnosticException( new Diagnostic(
					srcPos ?? new SrcPos { file = "<unknown>" },
					DiagnosticKind.Error,
					"null coalescing on calls and indexing is not implemented yet" ) );
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
		public string       id      = null!;
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
		public Typespec? typespec;
		public Expr?     lit;

		// A deduced or explicitly bound pack: the trailing arguments of a
		// pack parameter, comma-separated on emission.
		public List<TplArg>? packArgs;

		public bool IsTypeArgument  => typespec != null || packArgs != null;
		public bool IsValueArgument => lit      != null;

		public string Gen()
			=> packArgs != null
				? packArgs.Select( a => a.Gen() ).Join( ", " )
			: lit?.Gen()
			?? typespec?.Gen()
			?? throw new InvalidOperationException( "TplArg has neither literal nor typespec" );

		public string GenDiagnostic()
			=> packArgs != null
				? packArgs.Select( a => a.GenDiagnostic() ).Join( ", " )
			: lit?.Gen()
			?? typespec?.GenDiagnostic()
			?? throw new InvalidOperationException( "TplArg has neither literal nor typespec" );
	}

	public enum TplParamKind
	{
		Type,
		NonType,
		Unknown,
	}

	public class TplParam
	{
		public string       name = null!;
		public TplParamKind kind = TplParamKind.Type;
		public Typespec?    type;

		// A pack parameter (`<T...>`); must be the last of the list. The
		// `...` is only ever written here: parameter lists, call arguments
		// and the forward cast derive the expansion from this marker.
		public bool isPack;

		// The parameter's default (bare `T=int`, `int N=5`): fills at use
		// sites that omit the parameter, and CTAD after deduction. Defaults
		// must be trailing (once defaulted, every following parameter too).
		public TplArg? defaultArg;
	}

	/// <summary>
	/// One binding of a template specialization: `T = int` in `pi<T=int>`.
	/// In the "name everything" form an entry can also be an unbound placeholder
	/// (`T` in `q<T, U=int>`), marked by a null type. Names follow the primary's
	/// parameter declaration order.
	/// </summary>
	public sealed class TplBinding
	{
		public string    name = null!;
		public Typespec? type; // null: unbound placeholder
		public Expr?     lit;  // non-type parameter binding: N = 5
		public Typespec? declaredType; // the non-type spelling: int N = 5
	}
}
