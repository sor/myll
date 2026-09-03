// HACK to disable all PLINQ have the following line be active
//#define DISABLE_PLINQ

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Myll.Core;
using Myll.Generator;
using Myll.Resolver;

namespace Myll
{
	using Strings     = List<string>;
	using IStrings    = IEnumerable<string>;
	using ModuleGroup = IGrouping<string, MyllParser.ProgContext>;

	#if !DISABLE_PLINQ
	using static ParallelEnumerable;
#endif

	public sealed class LexerDiagnosticListener : IAntlrErrorListener<int>
	{
		private readonly string file;
		private readonly List<Diagnostic> diagnostics;

		public LexerDiagnosticListener( string file, List<Diagnostic> diagnostics )
		{
			this.file        = file;
			this.diagnostics = diagnostics;
		}

		public void SyntaxError(
			TextWriter           output,
			IRecognizer          recognizer,
			int                  offendingSymbol,
			int                  line,
			int                  charPositionInLine,
			string               msg,
			RecognitionException e )
		{
			diagnostics.Add( new Diagnostic(
				new SrcPos {
					file = file,
					from = new SrcPos.LineCol { line = line, col = charPositionInLine },
					to   = new SrcPos.LineCol { line = line, col = charPositionInLine + 1 },
				},
				DiagnosticKind.Error,
				msg ) );
		}
	}

	public sealed class ParserDiagnosticListener : IAntlrErrorListener<IToken>
	{
		private readonly List<Diagnostic> diagnostics;

		public ParserDiagnosticListener( List<Diagnostic> diagnostics )
		{
			this.diagnostics = diagnostics;
		}

		public void SyntaxError(
			TextWriter           output,
			IRecognizer          recognizer,
			IToken               offendingSymbol,
			int                  line,
			int                  charPositionInLine,
			string               msg,
			RecognitionException e )
		{
			string file      = offendingSymbol?.TokenSource?.SourceName
			                ?? ((MyllParser)recognizer).SourceName;
			int    startCol  = offendingSymbol?.Column ?? charPositionInLine;
			int    length    = offendingSymbol?.Text?.Length ?? 1;

			diagnostics.Add( new Diagnostic(
				new SrcPos {
					file = file,
					from = new SrcPos.LineCol { line = line, col = startCol },
					to   = new SrcPos.LineCol { line = line, col = startCol + length },
				},
				DiagnosticKind.Error,
				msg ) );
		}
	}

	static partial class Program
	{
		private const  string   Version = "0.01 (Alpha)";
		private static DateTime start;
		private static Options  opt = new(); // PERF: new() necessary to let it be not-null

		private static readonly string Executable = RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
			? "a.exe"
			: "a.out";

#if DISABLE_PLINQ
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IEnumerable<T> AsParallel<T>( this IEnumerable<T> s ) => s;
#endif

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IEnumerable<T> AsSequential<T>( this IEnumerable<T> s ) => s;

		private static (MyllParser Parser, List<Diagnostic> Diagnostics) CreateParser( string filename )
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
			// This will exit after the first problem
			//parser.ErrorHandler = new BailErrorStrategy();
			//Console.WriteLine( "Time elapsed after CreateParser   {0:0}ms", (DateTime.Now - start).TotalMilliseconds );

			return (parser, diagnostics);
		}

		private static (MyllParser.ProgContext Prog, List<Diagnostic> Diagnostics) ParseCST(
			(MyllParser Parser, List<Diagnostic> Diagnostics) pd )
		{
			MyllParser       parser      = pd.Parser;
			List<Diagnostic> diagnostics = pd.Diagnostics;

			// if exceptions happen, comment this out
			//parser.Interpreter.PredictionMode = PredictionMode.SLL;
			parser.Interpreter.PredictionMode = PredictionMode.LL_EXACT_AMBIG_DETECTION;

			try {
				MyllParser.ProgContext prog = parser.prog();
				//Console.WriteLine( "Time elapsed after ParseCST       {0:0}ms", (DateTime.Now - start).TotalMilliseconds );

				return (prog, diagnostics); // STAGE 1
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
				return (parser.prog(), diagnostics);
			}
		}

		private static bool UseColorForDiagnostics()
		{
			string? colorEnv = Environment.GetEnvironmentVariable( "MYLL_COLOR" );
			return colorEnv == "1" ? true
			     : colorEnv == "0" ? false
			     : !Console.IsErrorRedirected
			       && string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "NO_COLOR" ) );
		}

		private static string ClassifyModule( MyllParser.ProgContext c )
		{
			FileInfo fi = new( c.Start.InputStream.SourceName );
			string ret = c.module()?.id().GetText()
			          ?? Path.GetFileNameWithoutExtension( fi.Name );
			//Console.WriteLine( "Time elapsed after ClassifyModule {0:0}ms", (DateTime.Now - start).TotalMilliseconds );
			return ret;
		}

		private static (GlobalNamespace Module, CompilationContext Context) CompileModule(
			ModuleGroup progContext,
			bool        isPrototypeFile )
		{
			CompilationContext context = new() {
				IsPrototypeFile = isPrototypeFile,
			};
			//Console.WriteLine( "Time elapsed after CompileModule  {0:0}ms", (DateTime.Now - start).TotalMilliseconds );
			GlobalNamespace module = context.DeclVisitor.VisitProgs( progContext );
			return (module, context);
		}

		private static IEnumerable<string> CollectExternFiles( Options opt )
		{
			string? repoRoot = TryGetRepoRoot();
			if( repoRoot != null ) {
				string stdDir = Path.Combine( repoRoot, "std" );
				foreach( string file in EnumeratePrototypeFiles( stdDir ) )
					yield return file;
			}

			IEnumerable<string> candidates;
			if( opt.ExternDirs.Any() ) {
				candidates = opt.ExternDirs;
			}
			else {
				candidates = opt.InFiles
					.Select( Path.GetDirectoryName )
					.Where( d => !String.IsNullOrEmpty( d ) )
					.Cast<string>()
					.Distinct();
			}

			foreach( string dir in candidates ) {
				string externDir = opt.ExternDirs.Any() ? dir : Path.Combine( dir, "extern" );
				foreach( string file in EnumeratePrototypeFiles( externDir ) )
					yield return file;
			}
		}

		private static string? TryGetRepoRoot()
		{
			DirectoryInfo? di = new DirectoryInfo( AppContext.BaseDirectory ).Parent?.Parent?.Parent?.Parent;
			return di?.FullName;
		}

		private static IEnumerable<string> EnumeratePrototypeFiles( string dir )
		{
			if( !Directory.Exists( dir ) )
				yield break;

			foreach( string file in Directory.EnumerateFiles( dir, "*.d.myll" ) )
				yield return file;
			foreach( string file in Directory.EnumerateFiles( dir, "*.decl.myll" ) )
				yield return file;
			foreach( string file in Directory.EnumerateFiles( dir, "*.extern.myll" ) )
				yield return file;
		}

		private static bool IsPrototypeFile( string path )
			=> path.EndsWith( ".d.myll", StringComparison.OrdinalIgnoreCase )
			|| path.EndsWith( ".decl.myll", StringComparison.OrdinalIgnoreCase )
			|| path.EndsWith( ".extern.myll", StringComparison.OrdinalIgnoreCase );

		private static IEnumerable<string> CollectMyllLibraryFiles( IEnumerable<string> importedModules )
		{
			string? repoRoot = TryGetRepoRoot();
			if( repoRoot == null )
				yield break;

			string myllDir = Path.Combine( repoRoot, "myll" );
			if( !Directory.Exists( myllDir ) )
				yield break;

			HashSet<string> wanted = new( importedModules );
			foreach( string file in Directory.EnumerateFiles( myllDir, "*.myll" ) ) {
				string moduleName = Path.GetFileNameWithoutExtension( file );
				if( wanted.Contains( moduleName ) )
					yield return file;
			}
		}

		private static IEnumerable<string> ExtractImportedModules( string path )
		{
			if( !File.Exists( path ) )
				yield break;

			foreach( string rawLine in File.ReadLines( path ) ) {
				// strip line comments so imports inside comments are not picked up
				int comment = rawLine.IndexOf( "//", StringComparison.Ordinal );
				string line = comment < 0 ? rawLine : rawLine.Substring( 0, comment );

				Match match = Regex.Match( line, @"^\s*import\s+([^;]+);" );
				if( !match.Success )
					continue;

				foreach( string name in match.Groups[1].Value.Split( ',' ) ) {
					string trimmed = name.Trim();
					if( !String.IsNullOrEmpty( trimmed ) )
						yield return trimmed;
				}
			}
		}

		private static (List<(string, IStrings)> Files, List<Diagnostic> Diagnostics) GenerateFiles(
			GlobalNamespace global_ns )
		{
			List<(string, IStrings)> ret = new();

			HierarchicalGen gen = new( global_ns, -1, 0 );
			// do NOT call gen.AddNamespace( ret ).
			// Instead AddToGen() is there to call the correct virtual method on the gen
			// TODO why is it adding itself as a child?
			try {
				global_ns.AddToGen( gen );

				IStrings decl = gen.GenDeclGlobal();
				IStrings? impl = gen.GenImplGlobal();
				if( decl != null ) ret.Add( (string.Format( "{0}.hpp", global_ns.module ), decl) );
				if( impl != null ) ret.Add( (string.Format( "{0}.cpp", global_ns.module ), impl) );
			}
			catch( DiagnosticException ex ) {
				gen.Diagnostics.Add( ex.Diagnostic );
			}

			//Console.WriteLine( "Time elapsed after GenerateFiles  {0:0}ms", (DateTime.Now - start).TotalMilliseconds );
			return (ret, gen.Diagnostics);
		}

		public static int Main( string[] args )
		{
			Console.WriteLine( "Myll compiler. Version {0}\n", Version );

			if( args.Length == 0 ) {
				Console.WriteLine( "Usage: myll -i <input.myll>... -o <output> [options]" );
				Console.WriteLine( "       myll --test" );
				Console.WriteLine( "Use --help for the full option list." );
				return 1;
			}

			opt = ParseCommandLine( args );

			start = DateTime.Now;

			int cpus = Environment.ProcessorCount;
			ThreadPool.SetMinThreads( cpus*2, cpus*2 );
			//ThreadPool.SetMaxThreads( cpus*2, 1000 );

		//  ParallelQuery<(string, IStrings)>
		// OR IEnumerable<(string, IStrings)>
		List<string> inputFiles = opt.InFiles.ToList();
		inputFiles.AddRange( CollectExternFiles( opt ) );

		HashSet<string> importedModules = new(
			inputFiles.SelectMany( ExtractImportedModules ) );
		inputFiles.AddRange( CollectMyllLibraryFiles( importedModules ) );
		inputFiles = inputFiles.Distinct().ToList();

		List<(MyllParser.ProgContext Prog, List<Diagnostic> Diagnostics)> parseResults
				= inputFiles
					.Select( CreateParser )
					.AsParallel()
					.Select( ParseCST )
					.AsSequential()
					.ToList();

			List<Diagnostic> syntaxDiagnostics = parseResults
				.SelectMany( r => r.Diagnostics )
				.ToList();

			if( syntaxDiagnostics.Count > 0 ) {
				Console.Error.Write( DiagnosticFormatter.Format( syntaxDiagnostics, UseColorForDiagnostics() ) );

				if( !opt.IsKeepGoing )
					Environment.Exit( -99 );
			}

			List<(GlobalNamespace Module, CompilationContext Context)> modules
				= parseResults
					.Select( r => r.Prog )
					.GroupBy( ClassifyModule )
					.ToImmutableArray()
					// TODO .AsParallel() causes errors, as CompileModule changes globals
					.Select( g => CompileModule(
						g,
						g.All( p => IsPrototypeFile( p.Start.InputStream.SourceName ) ) ) )
					.ToList();

			List<Diagnostic> visitorDiagnostics = modules
				.SelectMany( m => m.Context.Diagnostics )
				.ToList();

			if( visitorDiagnostics.Count > 0 ) {
				Console.Error.Write( DiagnosticFormatter.Format( visitorDiagnostics, UseColorForDiagnostics() ) );

				if( !opt.IsKeepGoing )
					Environment.Exit( -99 );
			}

		var autoReturnDiagnostics = new List<Diagnostic>();
		new AutoReturnTransformer().Transform( modules, autoReturnDiagnostics );
		if( autoReturnDiagnostics.Count > 0 ) {
			Console.Error.Write(
				DiagnosticFormatter.Format( autoReturnDiagnostics, UseColorForDiagnostics() ) );
		}

		new TemplateParamTransformer().Transform( modules, new List<Diagnostic>() );
		new ChainTransformer().Transform( modules, autoReturnDiagnostics );

		if( opt.IsResolve ) {
			var (result, diagnostics) = NameResolver.Resolve( modules );
			if( diagnostics.Count > 0 ) {
				Console.Error.Write( DiagnosticFormatter.Format( diagnostics, UseColorForDiagnostics() ) );

				if( !opt.IsKeepGoing )
					Environment.Exit( -99 );
			}

			result.Apply();
		}

		new ElseOnLoopTransformer().Transform( modules, new List<Diagnostic>() );
		new BreakContinueTransformer().Transform( modules, new List<Diagnostic>() );

		var aliasShadowing = new ConfiguredAliasShadowingTransformer();
		aliasShadowing.Transform( modules );
		if( aliasShadowing.Diagnostics.Count > 0 ) {
			Console.Error.Write(
				DiagnosticFormatter.Format( aliasShadowing.Diagnostics, UseColorForDiagnostics() ) );
		}

			List<(List<(string, IStrings)> Files, List<Diagnostic> Diagnostics)> generationResults
				= modules
					.Where( m => !m.Context.IsPrototypeFile )
					.AsParallel()
					.Select( m => GenerateFiles( m.Module ) )
					.ToList();

			IEnumerable<(string, IStrings)> output = generationResults.SelectMany( r => r.Files );

			List<Diagnostic> generationDiagnostics = generationResults
				.SelectMany( r => r.Diagnostics )
				.ToList();

			if( generationDiagnostics.Count > 0 ) {
				Console.Error.Write( DiagnosticFormatter.Format( generationDiagnostics, UseColorForDiagnostics() ) );

				if( !opt.IsKeepGoing
				 && generationDiagnostics.Any( d => d.Kind == DiagnosticKind.Error ) )
					Environment.Exit( -99 );
			}

			Console.WriteLine( "Time elapsed after last ToArray call {0:0}ms\n", (DateTime.Now - start).TotalMilliseconds );

			if( opt.IsClear && Directory.Exists( opt.OutPath ) ) {
				bool OldFileFilter( string s ) => s.EndsWith( Path.DirectorySeparatorChar + Executable )
				                               || s.EndsWith( ".cpp" )
				                               || s.EndsWith( ".hpp" );

				Directory
					.EnumerateFiles( opt.OutPath )
					.Where( OldFileFilter )
					.ForAll( File.Delete );
			}

			if( !opt.IsFileOut && !opt.IsStdOut ) {
				Console.WriteLine( "\nNO OUTPUT wanted, just burning CPU time while calculating the output!\n" );

				output.Exec();
			}
			else {
				if( opt.IsFileOut && opt.IsStdOut ) {
					// if more than one output is requested,
					// then this must be pre-executed,
					// else its gonna Compile multiple times
					output = output.ToImmutableArray();
				}

				if( opt.IsStdOut ) {
					output.ForAll( o => Console.WriteLine( "// {0}\n{1}\n", o.Item1, o.Item2.Join( "\n" ) ) );
				}

				// NOT else if
				if( opt.IsFileOut ) {
					Directory.CreateDirectory( opt.OutPath );

					output.ForAll( o => File.WriteAllLines( Path.Combine( opt.OutPath, o.Item1 ), o.Item2 ) );
				}
			}

			DateTime end = DateTime.Now;
			Console.WriteLine( "Time elapsed from start to finish: {0:0}ms\n", (end - start).TotalMilliseconds );

			// compile the generated C++ code into a binary
			if( opt.IsFileOut && opt.IsCompile ) {

				Directory.SetCurrentDirectory( opt.OutPath );

				if( !CppCompiler.TryFindCompiler( out _, out Diagnostic? compilerDiagnostic ) ) {
					Console.Error.Write( DiagnosticFormatter.Format( compilerDiagnostic!, UseColorForDiagnostics() ) );
					return -1;
				}

				string[] cppFiles = Directory.GetFiles( ".", "*.cpp" );
				string envFlags = Environment.GetEnvironmentVariable( "MYLL_CXXFLAGS" )
				               ?? Environment.GetEnvironmentVariable( "CXXFLAGS" )
				               ?? "";
				string cxxFlags = (opt.IsDebug ? "-g " : "")
				                + (opt.OptimizationLevel > 0 ? "-O " + opt.OptimizationLevel + " " : "")
				                + envFlags;

				CppCompilerInvocation invocation = CppCompiler.CreateInvocation( cppFiles, cxxFlags );

				using Process process = new();
				process.StartInfo = new() {
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName    = invocation.Compiler,
					Arguments   = invocation.Arguments,
				};
				process.Start();
				process.WaitForExit();

				// execute the generated binary
				if( process.ExitCode == 0 && opt.IsRun ) {
					Process process2 = new();
					process2.StartInfo = new() {
						//WindowStyle      = System.Diagnostics.ProcessWindowStyle.Hidden,
						FileName  = Executable, //"cmd.exe",
						Arguments = "",         //"/C touch Hans"
					};
					process2.Start();
					process2.WaitForExit();

					Console.WriteLine("Executable finished with exit code {0} 0x{0:X8}", process2.ExitCode);

					return process2.ExitCode;
				}
				else if( process.ExitCode != 0 ) {
					return process.ExitCode;
				}
			}

			return 0;
		}

	}
}
