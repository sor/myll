using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Antlr4.Runtime;
using Myll.Core;
using Myll.Generator;

namespace Myll
{
	using Strings = List<string>;

	static class Program
	{
		private static readonly Strings DefaultIncludes = new Strings {
			"#pragma once",
			"#include <memory>",    // smart pointer
			"#include <utility>",   // move, pair, swap
			"#include <cmath>",     // math
		//	"#include <iostream>",  // in and output
		//	"#include <vector>",    // dynamically sized array
		//	"#include <algorithm>", // algorithms
		};

		static MyllParser.ProgContext GetProgContext( string filename )
		{
			StreamReader      reader            = new StreamReader( filename );
			AntlrInputStream  inputStream       = new AntlrInputStream( reader ) { name = filename };
			MyllLexer         lexer             = new MyllLexer( inputStream );
			CommonTokenStream commonTokenStream = new CommonTokenStream( lexer );
			MyllParser        parser            = new MyllParser( commonTokenStream );
			return parser.prog();
		}

		static IEnumerable<IGrouping<string, MyllParser.ProgContext>> ClassifyModules( List<string> filenames )
		{
			// prepare all ANTLR stages and then determine the modules
			IEnumerable<IGrouping<string, MyllParser.ProgContext>> ret
				= filenames
					.Select( GetProgContext )
					.GroupBy( c => VisitorExtensions.DeclVis.ProbeModule( c ) );

			return ret;
		}

		static List<(string, Strings)> Compile(
			IEnumerable<IGrouping<string, MyllParser.ProgContext>> moduleGroups )
		{
			List<(string, Strings)> ret = new List<(string, Strings)>();
			// grouped by modules, generating decl and impl
			foreach( IGrouping<string, MyllParser.ProgContext> progContext in moduleGroups ) {
				GlobalNamespace globalns = VisitorExtensions.DeclVis.VisitProgs( progContext );

				StmtFormatting.SimpleGen gen = new StmtFormatting.SimpleGen( -1, 0 );
				// do NOT call gen.AddNamespace( ret ).
				// Instead AddToGen() is there to call the correct virtual method on the gen
				globalns.AddToGen( gen );

				Strings includes = globalns.imps
					.Select(
						i => i.StartsWith( "std_" )
							? string.Format( "#include <{0}>",   i.Substring( 4 ) )
							: string.Format( "#include \"{0}.h\"", i ) )
					.ToList();

				Strings
					decl = DefaultIncludes
						.Concat( includes )
						.Concat( gen.GenDecl() )
						.ToList(),
					impl = gen
						.GenImpl()
						.Prepend( string.Format( "#include \"{0}.h\"", progContext.Key ) )
						.ToList();

				ret.Add( (string.Format( "{0}.h",   progContext.Key ), decl) );
				ret.Add( (string.Format( "{0}.cpp", progContext.Key ), impl) );
			}
			return ret;
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			//StreamReader reader = new StreamReader(  );
			//TextReader tr = new TextReader();
			//string testcase = File.ReadAllText( "testcase.myll" );
			var moduleGroups = ClassifyModules(
				new List<string> {
					"main.myll",
					"stack.myll",
					//"testcase.myll",
					//"testcase2.myll",
				} );

			List<(string, Strings)> output = Compile( moduleGroups );

			output.ForEach(
				o => {
					//var fs = File.Create( "output_" + o.Item1 );
			// TODO
			File.WriteAllLines( /*"output_" +*/ o.Item1, o.Item2 );
					Console.WriteLine( "// {0}", o.Item1 );
					Console.WriteLine( o.Item2.Join( "\n" ) );
				} );

			// TODO: multifile, either merge Namespaces for same module or merge in Generator
		}
	}

	// TODO: move to dedicated file
	static class GeneralExtensions
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

		// why was this ICollection instead of IEnumerable?
		public static string Join( this IEnumerable<string> values, string delimiter )
		{
			// TODO: this might be the bottleneck in the end, bench when finished
			// there is a specialized string.Join with string[]
			return string.Join( delimiter, values );
		}
	}
}
