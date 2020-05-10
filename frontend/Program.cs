using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Antlr4.Runtime;
using Myll.Core;
using Myll.Generator;

namespace Myll
{
	using Strings     = List<string>;
	using IStrings    = IEnumerable<string>;
	using ModuleGroup = IGrouping<string, MyllParser.ProgContext>;

	// HACK comment this out disable all PLINQ, see Program AsParallel too
	using static ParallelEnumerable;

	static class Program
	{
		private static DateTime start;

		// HACK to disable all PLINQ: enable this and comment out "using static ParallelEnumerable"
		//[MethodImpl( MethodImplOptions.AggressiveInlining )]
		//public static IEnumerable<T> AsParallel<T>( this IEnumerable<T> s ) => s;

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IEnumerable<T> AsSequential<T>( this IEnumerable<T> s ) => s;

		private static MyllParser CreateMyllParser( string filename )
		{
			StreamReader      reader            = new StreamReader( filename );
			AntlrInputStream  inputStream       = new AntlrInputStream( reader ) { name = filename };
			MyllLexer         lexer             = new MyllLexer( inputStream );
			CommonTokenStream commonTokenStream = new CommonTokenStream( lexer );
			MyllParser        parser            = new MyllParser( commonTokenStream );
			return parser;
		}

		private static string ClassifyModule( MyllParser.ProgContext c )
		{
			FileInfo fi = new FileInfo( c.Start.InputStream.SourceName );
			return c.module()?.id().GetText()
			    ?? Path.GetFileNameWithoutExtension( fi.Name );
		}

		private static MyllParser.ProgContext ParseAST( MyllParser c )
		{
			return c.prog();
		}

		private static GlobalNamespace CompileModule( ModuleGroup progContext )
		{
			return VisitorExtensions.DeclVis.VisitProgs( progContext );
		}

		private static List<(string, IStrings)> GenerateFiles( GlobalNamespace globalns )
		{
			List<(string, IStrings)> ret = new List<(string, IStrings)>();

			HierarchicalGen gen = new HierarchicalGen( globalns, -1, 0 );
			// do NOT call gen.AddNamespace( ret ).
			// Instead AddToGen() is there to call the correct virtual method on the gen
			// TODO why is it adding itself as a child?
			globalns.AddToGen( gen );

			IStrings decl = gen.GenDeclGlobal();
			IStrings impl = gen.GenImplGlobal();
			if( decl != null ) ret.Add( (string.Format( "{0}.h",   globalns.module ), decl) );
			if( impl != null ) ret.Add( (string.Format( "{0}.cpp", globalns.module ), impl) );

			return ret;
		}

		public static int Main( string[] args )
		{
			bool hasConsoleOutput = args.Contains( "--stdout" );
			bool hasFileOutput    = args.Contains( "--fileout" );

			Strings files = new Strings {
			/*	"tests/mixed/stack_big.myll",
				"tests/mixed/stack_big2.myll",
				"tests/mixed/stack_big3.myll",
				"tests/mixed/stack_big4.myll",
				"tests/mixed/stack_big5.myll",
				"tests/mixed/stack_big6.myll",
				"tests/mixed/stack_big7.myll",
				"tests/mixed/stack_big8.myll",//*/
				"tests/mixed/main.myll",
				"tests/mixed/stack.myll",
				"tests/mixed/enum.myll",
				"tests/mixed/testcase.myll",
			};

			Console.WriteLine( "Myll compiler. Version 0.01\n" );

			int cpus = Environment.ProcessorCount;
			ThreadPool.SetMinThreads( cpus*2, cpus*2 );
			//ThreadPool.SetMaxThreads( cpus*2, 1000 );

			start = DateTime.Now;

			//  ParallelQuery<(string, IStrings)>
			// OR IEnumerable<(string, IStrings)>
			var output
					= files
						.Select( CreateMyllParser )
						.AsParallel()
						.Select( ParseAST )
						.AsSequential()
						.GroupBy( ClassifyModule )
						.ToImmutableArray()
						// TODO .AsParallel() causes errors, as CompileModule changes globals
						.Select( CompileModule )
						.AsParallel()
						.SelectMany( GenerateFiles )
						//.ToImmutableArray()
						;

			Console.WriteLine( "Time elapsed after last ToArray call {0:0}ms\n", (DateTime.Now - start).TotalMilliseconds );

			if( hasFileOutput ) {
				Directory.CreateDirectory( "./tests/mixed/generated/" );

				output.ForAll( o => File.WriteAllLines( "./tests/mixed/generated/" + o.Item1, o.Item2 ) );
			}

			if( hasConsoleOutput ) {
				foreach( var o in output ) {
					Console.WriteLine( "// {0}", o.Item1 );
					Console.WriteLine( o.Item2.Join( "\n" ) );
					Console.WriteLine();
				}
				foreach( var o in output ) {
					Console.WriteLine( "// {0}", o.Item1 );
					Console.WriteLine( o.Item2.Join( "\n" ) );
					Console.WriteLine();
				}
			}

			if( !hasConsoleOutput && !hasFileOutput ) {
				Console.WriteLine( "\nNO OUTPUT wanted, just burning CPU time then!" );

				output.Exec();
			}


			DateTime end = DateTime.Now;

			Console.WriteLine( "Time elapsed from start to finish: {0:0}ms\n", (end - start).TotalMilliseconds );
			return 0;
		}
	}
}
