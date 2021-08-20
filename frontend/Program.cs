// HACK to disable all PLINQ have the following line be active
//#define DISABLE_PLINQ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
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

	public sealed class MyllConsoleErrorListener : IAntlrErrorListener<IToken>
	{
		public static readonly MyllConsoleErrorListener Instance = new MyllConsoleErrorListener();

		public void SyntaxError(
			TextWriter           output,
			IRecognizer          recognizer,
			IToken               offendingSymbol,
			int                  line,
			int                  charPositionInLine,
			string               msg,
			RecognitionException e )
		{
			output.WriteLine( "In file {0} line {1}:{2} {3}",
				((MyllParser) recognizer).SourceName,
				line,
				charPositionInLine,
				msg);
		}
	}

	static class Program
	{
		private static DateTime start;
		private static bool     keepGoing = false;

#if DISABLE_PLINQ
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IEnumerable<T> AsParallel<T>( this IEnumerable<T> s ) => s;
		#endif

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IEnumerable<T> AsSequential<T>( this IEnumerable<T> s ) => s;

		private static MyllParser CreateParser( string filename )
		{
			StreamReader      reader      = new( filename );
			AntlrInputStream  inputStream = new( reader ) { name = filename };
			MyllLexer         lexer       = new( inputStream );
			CommonTokenStream tokenStream = new( lexer );
			MyllParser        parser      = new( tokenStream );
			parser.RemoveErrorListeners();
			parser.AddErrorListener( MyllConsoleErrorListener.Instance );
			// This will exit after the first problem
			//parser.ErrorHandler = new BailErrorStrategy();
			return parser;
		}

		private static MyllParser.ProgContext ParseCST( MyllParser parser )
		{
			// if exceptions happen, comment this out
			parser.Interpreter.PredictionMode = PredictionMode.SLL;
			//parser.Interpreter.PredictionMode = PredictionMode.LL_EXACT_AMBIG_DETECTION;

			try {
				MyllParser.ProgContext prog = parser.prog();
				if( !keepGoing && parser.NumberOfSyntaxErrors > 0 ) {
					Console.Error.WriteLine(
						"\nThere were syntactical errors in {0}, aborting execution",
						parser.SourceName );
					Environment.Exit( -99 );
				}
				return prog; // STAGE 1
			}
			// This might never be reached since the error handling abov
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
			FileInfo fi = new( c.Start.InputStream.SourceName );
			return c.module()?.id().GetText()
			    ?? Path.GetFileNameWithoutExtension( fi.Name );
		}

		private static GlobalNamespace CompileModule( ModuleGroup progContext )
		{
			return VisitorExtensions.DeclVis.VisitProgs( progContext );
		}

		private static List<(string, IStrings)> GenerateFiles( GlobalNamespace globalns )
		{
			List<(string, IStrings)> ret = new();

			HierarchicalGen gen = new( globalns, -1, 0 );
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
				/*
				"tests/mixed/stack_big_0.myll",
				"tests/mixed/stack_big_1.myll",
				"tests/mixed/stack_big_2.myll",
				"tests/mixed/stack_big_3.myll",
				"tests/mixed/stack_big_4.myll",
				"tests/mixed/stack_big_5.myll",
				"tests/mixed/stack_big_6.myll",
				"tests/mixed/stack_big_7.myll",
				//*/
				/*"tests/mixed/main.myll",
				"tests/mixed/stack.myll",
				"tests/mixed/enum.myll",
				"tests/mixed/testcase.myll",*/
				"tests/mixed/sheet.myll",
				//"tests/mixed/sheet1.myll",
				//"tests/mixed/sheet2.myll",
				//"tests/mixed/sheet3.myll",
				//"tests/mixed/sheet4.myll",
				//"tests/mixed/plasma.myll",
			};

			Console.WriteLine( "Myll compiler. Version 0.01 (Alpha)\n" );

			int cpus = Environment.ProcessorCount;
			ThreadPool.SetMinThreads( cpus*2, cpus*2 );
			//ThreadPool.SetMaxThreads( cpus*2, 1000 );

			start = DateTime.Now;

			//  ParallelQuery<(string, IStrings)>
			// OR IEnumerable<(string, IStrings)>
			IEnumerable<(string, IStrings)> output
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

			if( !hasFileOutput && !hasConsoleOutput ) {
				Console.WriteLine( "\nNO OUTPUT wanted, just burning CPU time then!" );

				output.Exec();
			}
			else { // Any output
				// if output is needed more than once, then this must be pre-calculated else its gonna Compile multiple times
				if( hasFileOutput && hasConsoleOutput )
					output = output.ToImmutableArray();

				if( hasFileOutput ) {
					Directory.CreateDirectory( "./tests/mixed/generated/" );

					output.ForAll( o => File.WriteAllLines( "./tests/mixed/generated/" + o.Item1, o.Item2 ) );
				}

				if( hasConsoleOutput ) {
					output.ForAll(
						o => {
							Console.WriteLine( "// {0}", o.Item1 );
							Console.WriteLine( o.Item2.Join( "\n" ) );
							Console.WriteLine();
						} );
					/*
					foreach( var o in output ) {
						Console.WriteLine( "// {0}", o.Item1 );
						Console.WriteLine( o.Item2.Join( "\n" ) );
						Console.WriteLine();
					}
					*/
				}
			}

			DateTime end = DateTime.Now;

			Console.WriteLine( "Time elapsed from start to finish: {0:0}ms\n", (end - start).TotalMilliseconds );
			return 0;
		}
	}
}
