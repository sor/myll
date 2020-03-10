using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Antlr4.Runtime;

namespace Myll
{
	static class Program
	{
		public static string Output { get; set; }
		public static string OutputImpl { get; set; }

		static (string,string) Compile( string code )
		{
			AntlrInputStream  inputStream       = new AntlrInputStream( code );
			MyllLexer         lexer             = new MyllLexer( inputStream );
			CommonTokenStream commonTokenStream = new CommonTokenStream( lexer );
			MyllParser        parser            = new MyllParser( commonTokenStream );
			//VisitorExtensions.AllVis.Visit( parser.prog() );
			VisitorExtensions.DeclVis.Visit( parser.prog() );
			//parser.levStmt().Visit();

			// HACK
			return (Output, OutputImpl);
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			string testcase = File.ReadAllText( "testcase.myll" );
			var outputs = Compile( testcase );

			Console.WriteLine("// decl");
			Console.WriteLine( outputs.Item1 );
			Console.WriteLine("// impl");
			Console.WriteLine( outputs.Item2 );
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

		// handles negative counts gracefully, returning an empty string
		public static string Repeat( this string value, int count )
		{
			return count <= 0 || string.IsNullOrEmpty( value )
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
