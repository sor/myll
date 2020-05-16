// HACK to disable all PLINQ have the following line be active
//#define DISABLE_PLINQ

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Myll.Core;
using Myll.Generator;

namespace Myll
{
	using Strings     = List<string>;
	using IStrings    = IEnumerable<string>;
	using ModuleGroup = IGrouping<string, MyllParser.ProgContext>;

	#if !DISABLE_PLINQ
	using static ParallelEnumerable;
	#endif

	static class Program
	{
		private static DateTime start;

		#if DISABLE_PLINQ
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IEnumerable<T> AsParallel<T>( this IEnumerable<T> s ) => s;
		#endif

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IEnumerable<T> AsSequential<T>( this IEnumerable<T> s ) => s;

		private static MyllParser CreateParser( string filename )
		{
			StreamReader      reader      = new StreamReader( filename );
			AntlrInputStream  inputStream = new AntlrInputStream( reader ) { name = filename };
			MyllLexer         lexer       = new MyllLexer( inputStream );
			CommonTokenStream tokenStream = new CommonTokenStream( lexer );
			MyllParser        parser      = new MyllParser( tokenStream );
			return parser;
		}

		private static MyllParser.ProgContext ParseCST( MyllParser parser )
		{
			// if exceptions happen, comment this out
			parser.Interpreter.PredictionMode = PredictionMode.SLL;

			try {
				return parser.prog(); // STAGE 1
			}
			catch( Exception ex ) { // STAGE 2
				Console.Error.WriteLine(
					"First Stage failed of {0} with exception {1}",
					parser.RuleContext.Start.InputStream.SourceName,
					ex );
				((CommonTokenStream) parser.TokenStream).Reset(); // rewind input stream
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.LL;
				return parser.prog();
			}
		}

		private static string ClassifyModule( MyllParser.ProgContext c )
		{
			FileInfo fi = new FileInfo( c.Start.InputStream.SourceName );
			return c.module()?.id().GetText()
			    ?? Path.GetFileNameWithoutExtension( fi.Name );
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
			bool hasConsoleOutput = false&&args.Contains( "--stdout" );
			bool hasFileOutput    = args.Contains( "--fileout" );

			Strings files = new Strings {
			/*	"tests/mixed/stack_big.myll",
				"tests/mixed/stack_big_2.myll",
				"tests/mixed/stack_big_3.myll",
				"tests/mixed/stack_big_4.myll",
				"tests/mixed/stack_big_5.myll",
				"tests/mixed/stack_big_6.myll",
				"tests/mixed/stack_big_7.myll",
				"tests/mixed/stack_big_8.myll",//*/
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
						.Select( CreateParser )
						.AsParallel()
						.Select( ParseCST )
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
