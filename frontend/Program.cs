using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Myll.Core;
using Myll.Resolver;

namespace Myll
{
	using Strings  = List<string>;

	static partial class Program
	{
		private const  string   Version = "0.01 (Alpha)";

		private static readonly string Executable = RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
			? "a.exe"
			: "a.out";

		private static bool UseColorForDiagnostics()
		{
			string? colorEnv = Environment.GetEnvironmentVariable( "MYLL_COLOR" );
			return colorEnv == "1" ? true
			     : colorEnv == "0" ? false
			     : !Console.IsErrorRedirected
			       && string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "NO_COLOR" ) );
		}

		private static IEnumerable<string> CollectExternFiles( Options opt )
		{
			string? repoRoot = TryGetRepoRoot();
			if( repoRoot != null ) {
				string stdDir = Path.Combine( repoRoot, "std" );
				foreach( string file in EnumeratePrototypeFiles( stdDir ) )
					yield return file;

				string libDir = Path.Combine( repoRoot, "lib" );
				foreach( string file in EnumeratePrototypeFiles( libDir ) )
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

		private static IEnumerable<string> GetDialectSearchDirectories()
		{
			string? repoRoot = TryGetRepoRoot();
			if( repoRoot == null )
				yield break;

			string dialectDir = Path.Combine( repoRoot, "dialect" );
			if( Directory.Exists( dialectDir ) )
				yield return dialectDir;
		}

		private static bool IsArtifact( string path )
		{
			string name = Path.GetFileName( path );
			return name.Equals( Executable, StringComparison.Ordinal )
			    || name.EndsWith( ".o", StringComparison.Ordinal )
			    || name.EndsWith( ".obj", StringComparison.Ordinal )
			    || name.EndsWith( ".a", StringComparison.Ordinal )
			    || name.EndsWith( ".lib", StringComparison.OrdinalIgnoreCase );
		}

		private static void PrintDiagnosticsAndBail(
			Options opt,
			IEnumerable<Diagnostic> diagnostics )
		{
			List<Diagnostic> list = diagnostics.ToList();
			if( list.Count == 0 )
				return;

			Console.Error.Write( DiagnosticFormatter.Format( list, UseColorForDiagnostics() ) );

			if( !opt.IsKeepGoing )
				Environment.Exit( -99 );
		}

		private static void PrintDiagnosticsAndBailOnErrors(
			Options opt,
			IEnumerable<Diagnostic> diagnostics )
		{
			List<Diagnostic> list = diagnostics.ToList();
			if( list.Count == 0 )
				return;

			Console.Error.Write( DiagnosticFormatter.Format( list, UseColorForDiagnostics() ) );

			if( !opt.IsKeepGoing && list.Any( d => d.Kind == DiagnosticKind.Error ) )
				Environment.Exit( -99 );
		}

		private static void PrintDiagnosticsAndBailOnErrors<T>(
			Options opt,
			IEnumerable<T> items,
			Func<T, IEnumerable<Diagnostic>> selector )
			=> PrintDiagnosticsAndBailOnErrors( opt, items.SelectMany( selector ) );

		private static IEnumerable<string> CollectMyllLibraryFiles(
			IEnumerable<string> importedModules,
			ISet<string>        compilerOwnedFiles )
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
				if( wanted.Contains( moduleName ) ) {
					compilerOwnedFiles.Add( file );
					yield return file;
				}
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
					if( String.IsNullOrEmpty( trimmed ) )
						continue;
					if( trimmed[0] == '<' || trimmed[0] == '"' )
						continue;
					yield return trimmed;
				}
			}
		}

		private static List<Diagnostic> SetMainModule( Options opt, List<CompiledModuleResult> modules )
		{
			List<Diagnostic> diagnostics = new();

			List<string> mainModules = modules
				.Where( m => m.Module.children.OfType<Func>().Any( f => f.name == "main" ) )
				.Select( m => m.Module.module )
				.Distinct()
				.ToList();

			string? selectedMain = null;

			if( !String.IsNullOrEmpty( opt.MainModule ) ) {
				if( !mainModules.Contains( opt.MainModule ) ) {
					diagnostics.Add( new Diagnostic(
						null,
						DiagnosticKind.Error,
						String.Format(
							"Module '{0}' specified by -M/--main does not contain a function named 'main'",
							opt.MainModule ) ) );
					return diagnostics;
				}

				selectedMain = opt.MainModule;
			}
			else if( mainModules.Count == 1 ) {
				selectedMain = mainModules[0];
			}
			// If several modules define main, do not auto-select any. This lets the
			// backend emit each main as a normal function while the user can still use
			// -M/--main to choose the entry point for an executable build.

			if( selectedMain != null ) {
				foreach( CompiledModuleResult m in modules ) {
					if( m.Module.module == selectedMain )
						m.Context.IsMainModule = true;
				}
			}

			return diagnostics;
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

			Options opt = ParseCommandLine( args );
			opt.OutPath = Path.GetFullPath( opt.OutPath );

			DateTime start = DateTime.Now;

			int cpus = Environment.ProcessorCount;
			ThreadPool.SetMinThreads( cpus*2, cpus*2 );

			List<string> inputFiles = opt.InFiles.ToList();
			inputFiles.AddRange( CollectExternFiles( opt ) );

			HashSet<string> importedModules = new(
				inputFiles.SelectMany( ExtractImportedModules ) );
			// myll/ is the language's own standard library, so it is exempt
			// from the reserved 'myll_' identifier prefix.
			HashSet<string> compilerOwnedFiles = new();
			inputFiles.AddRange( CollectMyllLibraryFiles( importedModules, compilerOwnedFiles ) );
			inputFiles = inputFiles.Distinct().ToList();

			List<ParseResult> parseResults = CompilerPipeline.ParseFiles( inputFiles, compilerOwnedFiles );
			PrintDiagnosticsAndBail( opt, parseResults.SelectMany( r => r.Diagnostics ) );

			DialectLoader dialectLoader = new( GetDialectSearchDirectories() );
			List<CompiledModuleResult> modules = CompilerPipeline.CompileModules( parseResults, dialectLoader );
			PrintDiagnosticsAndBail( opt, modules.SelectMany( m => m.Context.Diagnostics ) );

			List<Diagnostic> resolutionDiagnostics = CompilerPipeline.ResolveModules( modules );
			PrintDiagnosticsAndBailOnErrors( opt, resolutionDiagnostics );

			List<Diagnostic> mainDiagnostics = SetMainModule( opt, modules );
			PrintDiagnosticsAndBailOnErrors( opt, mainDiagnostics );

			SourceMapOptions sourceMapOptions = new(
				Decl: opt.SourceMap || opt.SourceMapDecl,
				Stmt: opt.SourceMap || opt.SourceMapStmt );

			List<GeneratedModuleFilesResult> generationResults
				= CompilerPipeline.GenerateFiles( modules, opt.OutPath, sourceMapOptions );

			IEnumerable<(string, IEnumerable<SourceLine>)> output = generationResults.SelectMany( r => r.Files );

			PrintDiagnosticsAndBailOnErrors( opt, generationResults, r => r.Diagnostics );

			Console.WriteLine( "Time elapsed after last ToArray call {0:0}ms\n", (DateTime.Now - start).TotalMilliseconds );

			if( opt.IsClear && Directory.Exists( opt.OutPath ) ) {
				foreach( string file in Directory.EnumerateFiles( opt.OutPath ) ) {
					if( IsArtifact( file ) )
						File.Delete( file );
				}
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
					output.ForAll( o => Console.WriteLine( "// {0}\n{1}\n", o.Item1, string.Join( "\n", o.Item2.Select( l => l.Text ) ) ) );
				}

				// NOT else if
				if( opt.IsFileOut ) {
					Directory.CreateDirectory( opt.OutPath );

					output.ForAll( o => {
						string target = Path.GetFullPath( Path.Combine( opt.OutPath, o.Item1 ) );
						File.WriteAllLines( target, o.Item2.Select( l => l.Text ) );
					} );
				}
			}

			DateTime end = DateTime.Now;
			Console.WriteLine( "Time elapsed from start to finish: {0:0}ms\n", (end - start).TotalMilliseconds );

			// compile the generated C++ code into a binary
			if( opt.IsFileOut && opt.IsCompile ) {

				if( !CppCompiler.TryFindCompiler( out _, out Diagnostic? compilerDiagnostic ) ) {
					Console.Error.Write( DiagnosticFormatter.Format( compilerDiagnostic!, UseColorForDiagnostics() ) );
					return -1;
				}

				// Compile exactly what this run generated. Stale outputs from removed
				// modules stay on disk (an IDE holding them open keeps its session;
				// Myll overwrites its own files in place) but are never compiled, so
				// removed code cannot be silently resurrected into the binary.
				// Hand-written helper .cpp files are compiled by the test harness,
				// which knows the files it copies; the CLI only compiles its own output.
				string[] cppFiles = output
					.Where( o => o.Item1.EndsWith( ".cpp", StringComparison.OrdinalIgnoreCase ) )
					.Select( o => Path.Combine( opt.OutPath, o.Item1 ) )
					.ToArray();
				if( cppFiles.Length == 0 )
					return 0; // nothing generated to compile

				string envFlags = Environment.GetEnvironmentVariable( "MYLL_CXXFLAGS" )
				               ?? Environment.GetEnvironmentVariable( "CXXFLAGS" )
				               ?? "";
				string cxxFlags = (opt.IsDebug ? "-g " : "")
				                + (opt.OptimizationLevel > 0 ? "-O" + opt.OptimizationLevel + " " : "")
				                + envFlags;

				// NOTE: We intentionally do not pass an absolute output path here.
				// CppCompiler quotes arguments for shell usage, but this process uses
				// ProcessStartInfo.Arguments directly. Instead we set the compiler's
				// working directory to the output directory and let it emit a.out there.
				CppCompilerInvocation invocation = CppCompiler.CreateInvocation( cppFiles, cxxFlags );

				using Process process = new();
				process.StartInfo = new() {
					WindowStyle         = ProcessWindowStyle.Hidden,
					FileName            = invocation.Compiler,
					Arguments           = invocation.Arguments,
					WorkingDirectory    = opt.OutPath,
				};
				process.Start();
				process.WaitForExit();

				// execute the generated binary
				if( process.ExitCode == 0 && opt.IsRun ) {
					Process process2 = new();
					process2.StartInfo = new() {
						FileName         = Path.Combine( opt.OutPath, Executable ),
						Arguments        = "",
						WorkingDirectory = opt.OutPath,
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
