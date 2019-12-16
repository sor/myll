﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Myll
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			Application.Run( new Form1() );
		}
	}

	// TODO: move to dedicated file
	static class GeneralExtensions
	{
		public static bool In<T>( this T val, params T[] values )
			where T : struct
		{
			return values.Contains( val );
		}

		public static bool Between<T>( this T value, T min, T max )
			where T : IComparable<T>
		{
			return min.CompareTo( value ) <= 0
			    && value.CompareTo( max ) <= 0;
		}

		public static string Repeat( this string value, int count )
		{
			return count == 0 || string.IsNullOrEmpty( value )
				? string.Empty
				: new StringBuilder( value.Length * count )
					.Insert( 0, value, count )
					.ToString();
		}

		public static string Join( this ICollection<string> values, string delimiter )
		{
			// TODO: this might be the bottleneck in the end, bench when finished
			// there is a specialized string.Join with string[]
			return string.Join( delimiter, values );
		}
	}
}
