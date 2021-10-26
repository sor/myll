using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Myll.Core
{
	public static class Extensions
	{
		public static bool In<T>( this T val, params T[] values )
			//where T : struct
		{
			return values.Contains( val );
		}

		// public static bool In( this string val, params string[] values )
		// {
		// 	return values.Contains( val );
		// }

		public static bool Between<T>( this T value, T min, T max )
			where T : IComparable //<T>
		{
			return min.CompareTo( value )    <= 0
				   && value.CompareTo( max ) <= 0;
		}

		// handles negative counts gracefully, returning an empty string
		public static string Repeat( this string value, int count )
		{
			return count <= 0 || string.IsNullOrEmpty( value )
				? string.Empty
				: new StringBuilder( value.Length * count )
					.Insert( 0, value, count )
					.ToString();
		}

		// why was this ICollection instead of IEnumerable?
		public static string Join( this IEnumerable<string> values, string delimiter )
		{
			// TODO: this might be the bottleneck in the end, bench when finished
			// there is a specialized string.Join with string[]
			return string.Join( delimiter, values );
		}

		[Pure]
		public static bool IsEmpty<T>( [NotNull] this IEnumerable<T> values ) => !values.Any();
	}
}
