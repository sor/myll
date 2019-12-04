using System;
using System.Collections.Generic;
using System.Linq;
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
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}

	static class Extensions
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
	}
}
