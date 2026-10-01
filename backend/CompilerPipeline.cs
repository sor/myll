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
		private const string ReservedIdentifierPrefix = "myll_";

		public static List<ParseResult> ParseFiles(
			IEnumerable<string> inputFiles,
			IReadOnlyCollection<string>? compilerOwnedFiles = null )
		{
			HashSet<string> ownedFiles = compilerOwnedFiles != null
				? new HashSet<string>( compilerOwnedFiles )
				: new HashSet<string>();

			return inputFiles
				.Select( f => CreateParser( f, ownedFiles.Contains( f ) ) )
				.AsParallel()
				.Select( ParseCST )
				.ToList();
		}

		private static ParserResult CreateParser( string filename, bool isCompilerOwned )
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

			return new( parser, diagnostics, isCompilerOwned );
		}

		private static ParseResult ParseCST( ParserResult pd )
		{
			MyllParser       parser      = pd.Parser;
			List<Diagnostic> diagnostics = pd.Diagnostics;

			parser.Interpreter.PredictionMode = PredictionMode.LL_EXACT_AMBIG_DETECTION;

			MyllParser.ProgContext prog;
			try {
				prog = parser.prog();
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
				prog = parser.prog();
			}

			// The reserved prefix protects compiler-injected helper names in
			// generated code, so compiler-shipped files may use it.
			if( !pd.IsCompilerOwned )
				ScanReservedIdentifierPrefix( prog, parser, diagnostics );
			return new( prog, diagnostics ); // STAGE 1
		}

		private static void ScanReservedIdentifierPrefix( MyllParser.ProgContext prog, MyllParser parser, List<Diagnostic> diagnostics )
		{
			CommonTokenStream tokenStream = (CommonTokenStream) parser.TokenStream;
			tokenStream.Fill();

			// imported module names may carry the reserved prefix, they name
			// compiler-shipped modules (e.g. myll_dyn_array from the myll/ dir)
			HashSet<int> importTokenIndexes = new();
			foreach( MyllParser.ImportsContext import in prog.imports() ) {
				if( import.Stop == null )
					continue;
				for( int i = import.Start.TokenIndex; i <= import.Stop.TokenIndex; ++i )
					importTokenIndexes.Add( i );
			}

			foreach( IToken token in tokenStream.GetTokens() ) {
				if( importTokenIndexes.Contains( token.TokenIndex ) )
					continue;
				if( !token.Text.StartsWith( ReservedIdentifierPrefix, StringComparison.Ordinal ) )
					continue;

				diagnostics.Add( new Diagnostic(
					new SrcPos {
						file = token.InputStream?.SourceName ?? parser.SourceName,
						from = new SrcPos.LineCol { line = token.Line, col = token.Column },
						to   = new SrcPos.LineCol { line = token.Line, col = token.Column + token.Text.Length },
					},
					DiagnosticKind.Error,
					String.Format(
						"Identifier prefix '{0}' is reserved for the compiler: '{1}'",
						ReservedIdentifierPrefix,
						token.Text ) ) );
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
			bool                                      isPrototypeFile,
			DialectLoader                             dialectLoader )
		{
			ModuleContext context = new() {
				IsPrototypeFile = isPrototypeFile,
				Dialect         = ResolveDialect( progContext, dialectLoader, out List<Diagnostic> dialectDiagnostics ),
			};
			context.Diagnostics.AddRange( dialectDiagnostics );

			GlobalNamespace module = context.DeclVisitor.VisitProgs( progContext );
			context.Module = module;
			return new( context );
		}

		public static List<CompiledModuleResult> CompileModules(
			IEnumerable<ParseResult> parseResults,
			DialectLoader?           dialectLoader = null )
		{
			DialectLoader loader = dialectLoader ?? new DialectLoader( Array.Empty<string>() );

			return parseResults
				.Select( r => r.Prog )
				.OrderBy( p => p.Start.InputStream.SourceName )	// deterministically order files before grouping so multi-file modules emit stable output
				.GroupBy( ClassifyModule )
				.AsParallel()
				.Select( g => CompileModule(
					g,
					g.All( p => IsPrototypeFile( p.Start.InputStream.SourceName ) ),
					loader ) )
				.ToList();
		}

		private static Dialect ResolveDialect(
			IGrouping<string, MyllParser.ProgContext> progContext,
			DialectLoader                             loader,
			out List<Diagnostic>                      dialectDiagnostics )
		{
			dialectDiagnostics = new List<Diagnostic>();

			string? selectedName = null;
			SrcPos? selectedPos  = null;

			foreach( MyllParser.ProgContext c in progContext ) {
				string name = c.dialectDecl()?.id().GetText() ?? "myll";
				SrcPos  pos = c.dialectDecl()?.ToSrcPos()
				           ?? c.module()?.ToSrcPos()
				           ?? new SrcPos { file = c.Start.InputStream.SourceName };

				if( selectedName == null ) {
					selectedName = name;
					selectedPos  = pos;
					continue;
				}

				if( name != selectedName ) {
					dialectDiagnostics.Add( new Diagnostic(
						pos,
						DiagnosticKind.Error,
						String.Format(
							"Dialect mismatch in module '{0}': this file uses dialect '{1}', but another file already declared '{2}'.",
							progContext.Key,
							name,
							selectedName ) ) );
				}
			}

			Dialect dialect = loader.Load( selectedName ?? "myll", out IReadOnlyList<Diagnostic> loaderDiagnostics );
			dialectDiagnostics.AddRange( loaderDiagnostics );
			return dialect;
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
			string                     outputDirectory  = "",
			SourceMapOptions?          sourceMapOptions = null )
		{
			SourceMapOptions sourceMap = sourceMapOptions ?? new();

			IReadOnlyDictionary<string, IReadOnlyList<RawImport>> moduleRawImports
				= RawImportCollector.Collect( modules );

			IReadOnlySet<string> prototypeModules
				= modules
					.Where( m => m.Context.IsPrototypeFile )
					.Select( m => m.Module.module )
					.ToHashSet();

			foreach( CompiledModuleResult m in modules ) {
				m.Context.SourceMap       = sourceMap;
				m.Context.OutputBasePath  = outputDirectory;
			}

			return modules
				.Where( m => !m.Context.IsPrototypeFile )
				.AsParallel()
				.Select( m => ModuleGenerator.GenerateForModule(
					m.Context, moduleRawImports, prototypeModules, outputDirectory ) )
				.ToList();
		}
	}
}
