using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Myll.Core;
using Myll.Generator;
using Myll.Resolver;

namespace Myll
{
	public static class CompilerPipeline
	{
		public static List<ParseResult> ParseFiles( IEnumerable<string> inputFiles )
		{
			return inputFiles
				.Select( CreateParser )
				.AsParallel()
				.Select( ParseCST )
				.ToList();
		}

		private static ParserResult CreateParser( string filename )
		{
			string            text        = File.ReadAllText( filename );
			AntlrInputStream  inputStream = new( text ) { name = filename };
			MyllLexer         lexer       = new( inputStream );
			List<Diagnostic>  diagnostics = new();

			lexer.RemoveErrorListeners();
			lexer.AddErrorListener( new LexerDiagnosticListener( filename, diagnostics ) );

			CommonTokenStream tokenStream = new( lexer );
			MyllParser        parser      = new( tokenStream );

			parser.RemoveErrorListeners();
			parser.AddErrorListener( new ParserDiagnosticListener( diagnostics ) );

			return new( parser, diagnostics );
		}

		private static ParseResult ParseCST( ParserResult pd )
		{
			MyllParser       parser      = pd.Parser;
			List<Diagnostic> diagnostics = pd.Diagnostics;

			parser.Interpreter.PredictionMode = PredictionMode.LL_EXACT_AMBIG_DETECTION;

			try {
				MyllParser.ProgContext prog = parser.prog();
				return new( prog, diagnostics ); // STAGE 1
			}
			// This might never be reached since the error handling above
			catch( Exception ex ) { // STAGE 2
				Console.Error.WriteLine(
					"First Stage failed of {0} with exception {1}",
					parser.RuleContext.Start.InputStream.SourceName,
					ex );
				((CommonTokenStream) parser.TokenStream).Reset(); // rewind input stream
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.LL;
				return new( parser.prog(), diagnostics );
			}
		}

		public static string ClassifyModule( MyllParser.ProgContext c )
		{
			FileInfo fi = new( c.Start.InputStream.SourceName );
			return c.module()?.id().GetText()
			    ?? Path.GetFileNameWithoutExtension( fi.Name );
		}

		public static CompiledModuleResult CompileModule(
			IGrouping<string, MyllParser.ProgContext> progContext,
			bool                                      isPrototypeFile )
		{
			ModuleContext context = new() {
				IsPrototypeFile = isPrototypeFile,
				Dialect         = Dialect.Default,
			};

			GlobalNamespace module = context.DeclVisitor.VisitProgs( progContext );
			context.Module = module;
			return new( context );
		}

		public static List<CompiledModuleResult> CompileModules( IEnumerable<ParseResult> parseResults )
		{
			return parseResults
				.Select( r => r.Prog )
				.OrderBy( p => p.Start.InputStream.SourceName )	// deterministically order files before grouping so multi-file modules emit stable output
				.GroupBy( ClassifyModule )
				.AsParallel()
				.Select( g => CompileModule(
					g,
					g.All( p => IsPrototypeFile( p.Start.InputStream.SourceName ) ) ) )
				.ToList();
		}

		public static bool IsPrototypeFile( string path )
			=> path.EndsWith( ".d.myll", StringComparison.OrdinalIgnoreCase )
			|| path.EndsWith( ".decl.myll", StringComparison.OrdinalIgnoreCase )
			|| path.EndsWith( ".extern.myll", StringComparison.OrdinalIgnoreCase );

		public static List<Diagnostic> ResolveModules( List<CompiledModuleResult> modules )
		{
			ResolveResult result = NameResolver.Resolve( modules );
			result.Result.Apply();
			return result.Diagnostics.ToList();
		}

		public static List<GeneratedModuleFilesResult> GenerateFiles(
			List<CompiledModuleResult> modules,
			string                     outputDirectory = "",
			bool                       emitSourceMap   = false )
		{
			IReadOnlyDictionary<string, IReadOnlyList<RawImport>> moduleRawImports
				= RawImportCollector.Collect( modules );

			IReadOnlySet<string> prototypeModules
				= modules
					.Where( m => m.Context.IsPrototypeFile )
					.Select( m => m.Module.module )
					.ToHashSet();

			foreach( CompiledModuleResult m in modules )
				m.Context.EmitSourceMap = emitSourceMap;

			return modules
				.Where( m => !m.Context.IsPrototypeFile )
				.AsParallel()
				.Select( m => ModuleGenerator.GenerateForModule(
					m.Context, moduleRawImports, prototypeModules, outputDirectory ) )
				.ToList();
		}
	}
}
