using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Myll.Generator;

namespace Myll.Core
{
	public static class Extensions
	{
		public static bool In<T>( this T val, params T[] values )
			//where T : struct
			=> values.Contains( val );

		// public static bool In( this string val, params string[] values )
		// {
		// 	return values.Contains( val );
		// }

		public static bool Between<T>( this T value, T min, T max )
			where T : IComparable //<T>
			=> min.CompareTo( value ) <= 0
			&& value.CompareTo( max ) <= 0;

		// handles negative counts gracefully, returning an empty string
		public static string Repeat( this string value, int count )
			=> count <= 0 || string.IsNullOrEmpty( value )
				? string.Empty
				: new StringBuilder( value.Length * count )
					.Insert( 0, value, count )
					.ToString();

		public static string Brace( this string value, bool doBrace )
			=> doBrace
				? string.Format( "({0})", value )
				: value;

		public static string Curly( this string value, bool doCurly )
			=> doCurly
				? string.Format( "{{{0}}}", value )
				: value;

		// does not change indentation of passed in IEnumerable
		public static IEnumerable<string> Curly( this IEnumerable<string> values, string indent, bool doCurly = true )
			=> doCurly
				? values
					.Prepend( string.Format( "{0}{{", indent ) )
					.Append( string.Format( "{0}}}",  indent ) )
				: values;

		// does not change indentation of passed in IEnumerable
		public static IEnumerable<string> Curly( this IEnumerable<string> values, int level, bool doCurly = true )
			=> doCurly
				? values.Curly( StmtFormatting.IndentString.Repeat( level ), true )
				: values;

		//public static IEnumerable<Stmt> FilterEmpty( this IEnumerable<Stmt> values )
		//	=> values.Where( s => s is not EmptyStmt );


		// why was this ICollection instead of IEnumerable?
		public static string Join( this IEnumerable<string> values, string delimiter )
			// TODO: this might be the bottleneck in the end, bench when finished
			// there is a specialized string.Join with string[]
			=> string.Join( delimiter, values );

		public static IEnumerable<string> Indent( this IEnumerable<string> values, string indent )
			=> values.Select( l => string.Format( "{0}{1}", indent, l ) );

		public static IEnumerable<string> Indent( this IEnumerable<string> values, int level )
			=> values.Indent( StmtFormatting.IndentString.Repeat( level ) );

		public static List<string> IndentAll( this string value, int level )
		{
			string indent = StmtFormatting.IndentString.Repeat( level );
			if( !value.Contains( "\n" ) )
				return new List<string> { indent + value };

			string[] split = value.Split( '\n' );
			return split.Indent( indent ).ToList();
		}

		public static List<string> Indent2nd( this string value, int level )
		{
			string indent = StmtFormatting.IndentString.Repeat( level );
			if( !value.Contains( "\n" ) )
				return new List<string> { indent + value };

			string[] split = value.Split( '\n' );
			return split.Skip( 1 ).Indent( level ).Prepend( split.First() ).ToList();
		}

		[Pure]
		public static bool IsEmpty<T>( [NotNull] this IEnumerable<T> values )
			=> !values.Any();
	}
}
