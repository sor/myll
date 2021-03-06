﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JanSordid.MyLang.CodeGen
{
	public static class Extensions
	{
		//
		// Summary:
		//     Replaces one or more format items in a specified string with the string representation
		//     of a specified object.
		//
		// Parameters:
		//   format:
		//     A composite format string (see Remarks).
		//
		//   arg0:
		//     The object to format.
		//
		// Returns:
		//     A copy of format in which any format items are replaced by the string representation
		//     of arg0.
		//
		// Exceptions:
		//   System.ArgumentNullException:
		//     format is null.
		//
		//   System.FormatException:
		//     The format item in format is invalid.-or- The index of a format item is not
		//     zero.
		public static string Fmt( this string format, object arg0 )
		{
			return string.Format( format, arg0 );
		}
		//
		// Summary:
		//     Replaces the format item in a specified string with the string representation
		//     of a corresponding object in a specified array.
		//
		// Parameters:
		//   format:
		//     A composite format string (see Remarks).
		//
		//   args:
		//     An object array that contains zero or more objects to format.
		//
		// Returns:
		//     A copy of format in which the format items have been replaced by the string
		//     representation of the corresponding objects in args.
		//
		// Exceptions:
		//   System.ArgumentNullException:
		//     format or args is null.
		//
		//   System.FormatException:
		//     format is invalid.-or- The index of a format item is less than zero, or greater
		//     than or equal to the length of the args array.
		public static string Fmt( this string format, params object[] args )
		{
			return string.Format( format, args );
		}
		//
		// Summary:
		//     Replaces the format item in a specified string with the string representation
		//     of a corresponding object in a specified array. A specified parameter supplies
		//     culture-specific formatting information.
		//
		// Parameters:
		//   provider:
		//     An object that supplies culture-specific formatting information.
		//
		//   format:
		//     A composite format string (see Remarks).
		//
		//   args:
		//     An object array that contains zero or more objects to format.
		//
		// Returns:
		//     A copy of format in which the format items have been replaced by the string
		//     representation of the corresponding objects in args.
		//
		// Exceptions:
		//   System.ArgumentNullException:
		//     format or args is null.
		//
		//   System.FormatException:
		//     format is invalid.-or- The index of a format item is less than zero, or greater
		//     than or equal to the length of the args array.
		public static string Fmt( this string format, IFormatProvider provider, params object[] args )
		{
			return string.Format( provider, format, args );
		}
		//
		// Summary:
		//     Replaces the format items in a specified string with the string representation
		//     of two specified objects.
		//
		// Parameters:
		//   format:
		//     A composite format string (see Remarks).
		//
		//   arg0:
		//     The first object to format.
		//
		//   arg1:
		//     The second object to format.
		//
		// Returns:
		//     A copy of format in which format items are replaced by the string representations
		//     of arg0 and arg1.
		//
		// Exceptions:
		//   System.ArgumentNullException:
		//     format is null.
		//
		//   System.FormatException:
		//     format is invalid.-or- The index of a format item is not zero or one.
		public static string Fmt( this string format, object arg0, object arg1 )
		{
			return string.Format( format, arg0, arg1 );
		}
		//
		// Summary:
		//     Replaces the format items in a specified string with the string representation
		//     of three specified objects.
		//
		// Parameters:
		//   format:
		//     A composite format string (see Remarks).
		//
		//   arg0:
		//     The first object to format.
		//
		//   arg1:
		//     The second object to format.
		//
		//   arg2:
		//     The third object to format.
		//
		// Returns:
		//     A copy of format in which the format items have been replaced by the string
		//     representations of arg0, arg1, and arg2.
		//
		// Exceptions:
		//   System.ArgumentNullException:
		//     format is null.
		//
		//   System.FormatException:
		//     format is invalid.-or- The index of a format item is less than zero, or greater
		//     than two.
		public static string Fmt( this string format, object arg0, object arg1, object arg2 )
		{
			return string.Format( format, arg0, arg1, arg2 );
		}
		//
		// Summary:
		//     Concatenates the members of a constructed System.Collections.Generic.IEnumerable<T>
		//     collection of type System.String, using the specified separator between each
		//     member.
		//
		// Parameters:
		//   separator:
		//     The string to use as a separator.
		//
		//   values:
		//     A collection that contains the strings to concatenate.
		//
		// Returns:
		//     A string that consists of the members of values delimited by the separator
		//     string. If values has no members, the method returns System.String.Empty.
		//
		// Exceptions:
		//   System.ArgumentNullException:
		//     values is null.
		public static string Join( this IEnumerable<string> values )
		{
			return string.Join( string.Empty, values );
		}
		//
		// Summary:
		//     Concatenates the members of a constructed System.Collections.Generic.IEnumerable<T>
		//     collection of type System.String, using the specified separator between each
		//     member.
		//
		// Parameters:
		//   separator:
		//     The string to use as a separator.
		//
		//   values:
		//     A collection that contains the strings to concatenate.
		//
		// Returns:
		//     A string that consists of the members of values delimited by the separator
		//     string. If values has no members, the method returns System.String.Empty.
		//
		// Exceptions:
		//   System.ArgumentNullException:
		//     values is null.
		public static string Join( this IEnumerable<string> values, string separator )
		{
			return string.Join( separator, values );
		}
	}
}
