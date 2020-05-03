using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Myll.Core;
using Myll.Generator;

namespace Myll
{
	using Strings     = List<string>;
	using IStrings    = IEnumerable<string>;
	using ModuleGroup = IGrouping<string, MyllParser.ProgContext>;

	static class Program
	{
		private static readonly Strings DefaultIncludes = new Strings {
			"#pragma once",
			"#include <memory>",      // smart pointer (expensive)
			"#include <utility>",     // move, pair, swap
			"#include <cmath>",       // math
			"#include <type_traits>", // underlying_type
		//	"#include <iostream>",  // in and output
		//	"#include <vector>",    // dynamically sized array
		//	"#include <map>",    	// hash map (expensive)
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

		static IEnumerable<ModuleGroup> ClassifyModules( List<string> filenames )
		{
			// prepare all ANTLR stages and then determine the modules
			IEnumerable<ModuleGroup>
				ret = filenames
					.Select( GetProgContext )
					.GroupBy(
						c => {
							FileInfo fi = new FileInfo( c.Start.InputStream.SourceName );
							string module = c.module()?.id().GetText()
							             ?? Path.GetFileNameWithoutExtension( fi.Name );
							return module;
						} );

			return ret;
		}

		static List<(string, IStrings)> Compile( IEnumerable<ModuleGroup> moduleGroups )
		{
			List<(string, IStrings)> ret = new List<(string, IStrings)>();
			// grouped by modules, generating decl and impl
			foreach( ModuleGroup progContext in moduleGroups ) {
				GlobalNamespace globalns = VisitorExtensions.DeclVis.VisitProgs( progContext );

				HierarchicalGen gen = new HierarchicalGen( globalns, -1, 0 );
				// do NOT call gen.AddNamespace( ret ).
				// Instead AddToGen() is there to call the correct virtual method on the gen
				globalns.AddToGen( gen );

				IStrings includes = globalns.imps
					.Select(
						i => i.StartsWith( "std_" )
							? string.Format( "#include <{0}>",     i.Substring( 4 ) )
							: string.Format( "#include \"{0}.h\"", i ) );

				Strings declList = gen.GenDecl();
				IStrings decl = DefaultIncludes
					.Concat( includes )
					.Concat( declList );

				ret.Add( (string.Format( "{0}.h", progContext.Key ), decl) );

				Strings implList = gen.GenImpl();
				if( implList.Count != 0 ) {
					IStrings impl = implList.Prepend( string.Format( "#include \"{0}.h\"", progContext.Key ) );
					ret.Add( (string.Format( "{0}.cpp",                                    progContext.Key ), impl) );
				}
			}
			return ret;
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static int Main( string[] args )
		{
			DateTime start = DateTime.Now;

			bool hasConsoleOutput = args.Contains( "--stdout" );
			bool hasFileOutput    = args.Contains( "--fileout" );
			//StreamReader reader = new StreamReader(  );
			//TextReader tr = new TextReader();
			//string testcase = File.ReadAllText( "testcase.myll" );

			//DirectoryInfo di = new DirectoryInfo( Directory.GetCurrentDirectory() );
			//di = di.Parent.Parent.Parent.GetDirectories("tests").First();
			//var tests_subdirs = di.EnumerateDirectories();
			//Console.WriteLine( "// directory {0}", tests_subdirs.Select( d => d.FullName ).Join( ", " ) );

			Strings files = new Strings {
				"tests/mixed/main.myll",
				"tests/mixed/stack.myll",
				"tests/mixed/enum.myll",
				"tests/mixed/testcase.myll",
			};

			IEnumerable<ModuleGroup> moduleGroups = ClassifyModules( files );

			//  Filename, Content
			List<(string, IStrings)> output = Compile( moduleGroups );

			if( hasFileOutput )
				Directory.CreateDirectory( "./tests/mixed/generated/" );

			output.ForEach(
				o => {
					//var fs = File.Create( "output_" + o.Item1 );

					if( hasFileOutput )
						File.WriteAllLines( "./tests/mixed/generated/" + o.Item1, o.Item2 );

					if( hasConsoleOutput ) {
						Console.WriteLine( "\n// {0}", o.Item1 );
						Console.WriteLine( o.Item2.Join( "\n" ) );
					}
				} );

			// TODO: multifile, merge Namespaces for same module or merge in Generator

			DateTime end = DateTime.Now;

			Console.WriteLine( "Time elapsed in main() {0}ms", (end - start).TotalMilliseconds );
			return 0;
		}
	}
}
