using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Myll;
using Xunit;
using Xunit.Abstractions;

namespace Myll.Tests
{
	public sealed class IntegrationTests
	{
		private readonly ITestOutputHelper output;

		private static readonly string RepoRoot = Path.GetFullPath(
			Path.Combine( AppContext.BaseDirectory, "..", "..", "..", ".." ) );

		private static readonly string FrontendDll = GetFrontendDll();

		private static string ObjectExtension
			=> RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ? ".obj" : ".o";

		private static string ExecutableName
			=> RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ? "test.exe" : "test.out";

		private static string GetFrontendDll()
		{
			string testingBin = AppContext.BaseDirectory.TrimEnd( Path.DirectorySeparatorChar );
			string frameworkDir = Path.GetFileName( testingBin );
			string configDir    = Path.GetFileName( Path.GetDirectoryName( testingBin ) )!;

			return Path.Combine( RepoRoot, "frontend", "bin", configDir, frameworkDir, "myll.dll" );
		}

		public IntegrationTests( ITestOutputHelper output )
		{
			this.output = output;
		}

		public static IEnumerable<object[]> GetTestCases()
		{
			string casesDir = Path.Combine( RepoRoot, "testing", "cases" );
			if( !Directory.Exists( casesDir ) )
				yield break;

			foreach( string caseDir in Directory.GetDirectories( casesDir ).OrderBy( d => d ) )
			{
				string name = Path.GetFileName( caseDir )!;
				yield return new object[] { name };
			}
		}

		[Theory]
		[MemberData( nameof( GetTestCases ) )]
		public void RunCase( string caseName )
		{
			string caseDir      = Path.Combine( RepoRoot, "testing", "cases", caseName );
			bool cleanupGeneratedDir = UseTempOutputDirectory() && !CaseConfig.UseFixedOutput( caseName );
			string generatedDir = cleanupGeneratedDir
				? Path.Combine( RepoRoot, "testing", "generated", String.Format( "tmp_{0}_{1:N}", caseName, Guid.NewGuid() ) )
				: Path.Combine( RepoRoot, "testing", "generated", caseName );
			TestDepth depth = CaseConfig.GetDepth( caseName );

			try
			{
				Directory.CreateDirectory( generatedDir );

				string[] myllFiles = Directory.GetFiles( caseDir, "*.myll" )
					.OrderBy( f => Path.GetFileName( f ) )
					.ToArray();
				Assert.NotEmpty( myllFiles );

				bool useMyllCr = CaseConfig.UseMyllCompileRun( caseName ) && depth == TestDepth.Run;
				var myllFlags = new List<string> { useMyllCr ? "-Ccr" : "-C" };

				string extraFlags = CaseConfig.MyllExtraFlags( caseName );
				if( !string.IsNullOrWhiteSpace( extraFlags ) )
					myllFlags.Add( extraFlags );

				string myllArgs = String.Format( "exec \"{0}\" -i \"*.myll\" -o {1} {2}", FrontendDll,
					Quote( generatedDir ), string.Join( " ", myllFlags ) );

				output.WriteLine( "Running: dotnet " + myllArgs + " in " + caseDir );
				ProcessResult myllResult = ProcessRunner.Run( "dotnet", myllArgs, workingDirectory: caseDir,
					timeout: CaseConfig.MyllTimeout( caseName ) );

				output.WriteLine( myllResult.StdOut );
				if( !string.IsNullOrEmpty( myllResult.StdErr ) )
					output.WriteLine( "STDERR: " + myllResult.StdErr );

				if( depth == TestDepth.GenerateFailing )
				{
					Assert.True(
						myllResult.ExitCode != 0 && !myllResult.TimedOut,
						string.Format( "Expected Myll generation to fail, but it returned exit code {0}{1}.",
							myllResult.ExitCode, myllResult.TimedOut ? " after timing out" : "" ) );
					AssertFailingDiagnostics( caseDir, myllResult );
					return;
				}

				Assert.Equal( 0, myllResult.ExitCode );
				Assert.False( myllResult.TimedOut, "Myll compiler timed out" );

				// Golden file comparison
				string goldenDir = Path.Combine( RepoRoot, "testing", "golden", caseName );
				bool goldenMatch = GoldenFileComparer.Compare( generatedDir, goldenDir, out string diffReport );
				if( !goldenMatch )
				{
					output.WriteLine( "Golden file mismatch:\n" + diffReport );
					Assert.True( goldenMatch, diffReport );
				}

				if( useMyllCr )
				{
					output.WriteLine( "Myll's internal -cr path was used; checking final exit code." );
					AssertRunSuccess( myllResult, "Myll -cr" );
					return;
				}

				CopySupportFiles( caseDir, generatedDir );

				string[] cppFiles = Directory.GetFiles( generatedDir, "*.cpp" );
				if( cppFiles.Length == 0 )
					return;

				RunBuildPhases( cppFiles, generatedDir, caseName, depth );
			}
			finally
			{
				CleanupBuildArtifacts( generatedDir );
				if( cleanupGeneratedDir )
					TryDeleteDirectory( generatedDir );
			}
		}

		private readonly struct FailMarker
		{
			public readonly string FileName;
			public readonly int    Line;
			public readonly int?   ExpectedCount;
			public readonly string? Substring;

			public FailMarker( string fileName, int line, int? expectedCount, string? substring )
			{
				FileName      = fileName;
				Line          = line;
				ExpectedCount = expectedCount;
				Substring     = substring;
			}
		}

		private static readonly Regex FailMarkerRegex = new(
			@"//\s*FAIL(?:\s+(?<count>\d+))?(?:\s*:\s*(?<text>.*))?\s*$",
			RegexOptions.Compiled | RegexOptions.IgnoreCase );

		private static readonly Regex DiagnosticRegex = new(
			@"\[(?<path>[^\]\s]+\.(?:myll|decl\.myll)):(?<line>\d+):(?<col>\d+)\]\s+(?<kind>error|warning):\s*(?<message>.*)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase );

		private void AssertFailingDiagnostics( string caseDir, ProcessResult result )
		{
			List<FailMarker> markers = new();
			foreach( string file in Directory.GetFiles( caseDir, "*.myll" ) ) {
				string fileName = Path.GetFileName( file );
				string[] lines  = File.ReadAllLines( file );
				for( int i = 0; i < lines.Length; i++ ) {
					Match m = FailMarkerRegex.Match( lines[i] );
					if( !m.Success )
						continue;

					int? count = null;
					if( m.Groups["count"].Success )
						count = int.Parse( m.Groups["count"].Value );

					string? substring = m.Groups["text"].Success
						? m.Groups["text"].Value.Trim()
						: null;

					markers.Add( new FailMarker( fileName, i + 1, count, substring ) );
				}
			}

			// If the case has no markers, fall back to the legacy behaviour (exit code only).
			if( markers.Count == 0 )
				return;

			string combinedOutput = ( result.StdOut ?? "" ) + Environment.NewLine + ( result.StdErr ?? "" );
			List<(string FileName, int Line, string Message)> diagnostics = DiagnosticRegex
				.Matches( combinedOutput )
				.Select( m => (
					FileName: Path.GetFileName( m.Groups["path"].Value ),
					Line: int.Parse( m.Groups["line"].Value ),
					Message: m.Groups["message"].Value.Trim() ) )
				.ToList();

			foreach( FailMarker marker in markers ) {
				List<string> matches = diagnostics
					.Where( d => d.FileName == marker.FileName
					          && d.Line == marker.Line
					          && ( marker.Substring == null
					            || d.Message.Contains( marker.Substring, StringComparison.OrdinalIgnoreCase ) ) )
					.Select( d => d.Message )
					.ToList();

				if( matches.Count == 0 ) {
					Assert.Fail( string.Format(
						"Expected a diagnostic on {0}:{1} (marked with // FAIL{2}), but none was reported.",
						marker.FileName,
						marker.Line,
						marker.Substring != null ? ": " + marker.Substring : "" ) );
				}

				if( marker.ExpectedCount.HasValue && matches.Count != marker.ExpectedCount.Value ) {
					Assert.Fail( string.Format(
						"Expected {0} diagnostic(s) on {1}:{2} (marked with // FAIL {3}), but found {4}.",
						marker.ExpectedCount.Value,
						marker.FileName,
						marker.Line,
						marker.ExpectedCount.Value,
						matches.Count ) );
				}
			}
		}

		private void RunBuildPhases( string[] cppFiles, string workingDir, string caseName, TestDepth depth )
		{
			var objectFiles = new List<string>();
			bool anyCompileFailed = false;

			foreach( string cppFile in cppFiles )
			{
				string objectFile = Path.Combine( workingDir,
					Path.GetFileNameWithoutExtension( cppFile ) + ObjectExtension );
				objectFiles.Add( objectFile );

				CppCompilerInvocation invocation = CppCompiler.CreateCompileInvocation(
					cppFile, outputObject: objectFile );

				output.WriteLine( string.Format( "Compiling: {0} {1}", invocation.Compiler, invocation.Arguments ) );
				ProcessResult result = ProcessRunner.Run(
					invocation.Compiler, invocation.Arguments, workingDirectory: workingDir,
					timeout: CaseConfig.CompileTimeout( caseName ) );

				output.WriteLine( result.StdOut );
				if( !string.IsNullOrEmpty( result.StdErr ) )
					output.WriteLine( "STDERR: " + result.StdErr );

				if( result.ExitCode != 0 || result.TimedOut )
				{
					anyCompileFailed = true;
					output.WriteLine( string.Format( "Compilation of {0} failed.", Path.GetFileName( cppFile ) ) );
				}
			}

			if( depth == TestDepth.Generate )
			{
				Assert.True( anyCompileFailed,
					"generate case: C++ compilation was expected to fail, but it succeeded. "
					+ "Consider moving this case to a deeper category." );
				return;
			}

			Assert.False( anyCompileFailed,
				"Compilation failed, but the case is configured for a deeper depth than generate." );

			if( depth == TestDepth.Compile )
			{
				string binaryPath = Path.Combine( workingDir, ExecutableName );
				ProcessResult linkResult = LinkObjects( objectFiles, binaryPath, workingDir, caseName );
				Assert.True(
					linkResult.ExitCode != 0 || linkResult.TimedOut,
					"compile case: link was expected to fail, but it succeeded. "
					+ "Consider moving this case to the 'link' category." );
				return;
			}

			// Link must succeed for Link and Run depths.
			string runBinaryPath = Path.Combine( workingDir, ExecutableName );
			ProcessResult finalLinkResult = LinkObjects( objectFiles, runBinaryPath, workingDir, caseName );
			Assert.Equal( 0, finalLinkResult.ExitCode );
			Assert.False( finalLinkResult.TimedOut, "Link timed out" );

			if( depth == TestDepth.Link )
			{
				ProcessResult runResult = RunBinary( runBinaryPath, workingDir, caseName );
				Assert.True(
					runResult.ExitCode != 0 || runResult.TimedOut,
					"link case: run was expected to fail, but it succeeded. "
					+ "Consider moving this case to the 'run' category." );
				return;
			}

			ProcessResult finalRunResult = RunBinary( runBinaryPath, workingDir, caseName );
			AssertRunSuccess( finalRunResult, "generated binary" );
		}

		private ProcessResult LinkObjects(
			IReadOnlyCollection<string> objectFiles,
			string                      binaryPath,
			string                      workingDir,
			string                      caseName )
		{
			CppCompilerInvocation linkInvocation = CppCompiler.CreateLinkInvocation(
				objectFiles, binaryPath );

			output.WriteLine( string.Format( "Linking: {0} {1}", linkInvocation.Compiler, linkInvocation.Arguments ) );
			ProcessResult linkResult = ProcessRunner.Run(
				linkInvocation.Compiler, linkInvocation.Arguments, workingDirectory: workingDir,
				timeout: CaseConfig.CompileTimeout( caseName ) );

			output.WriteLine( linkResult.StdOut );
			if( !string.IsNullOrEmpty( linkResult.StdErr ) )
				output.WriteLine( "STDERR: " + linkResult.StdErr );

			return linkResult;
		}

		private ProcessResult RunBinary( string binaryPath, string workingDir, string caseName )
		{
			output.WriteLine( "Running: " + binaryPath );
			return ProcessRunner.Run( binaryPath, "", workingDirectory: workingDir,
				environment: new Dictionary<string, string> { ["MYLL_TEST"] = "1" },
				timeout: CaseConfig.RunTimeout( caseName ) );
		}

		private void AssertRunSuccess( ProcessResult result, string source )
		{
			Assert.Equal( 0, result.ExitCode );
			Assert.False( result.TimedOut, source + " timed out" );
		}

		private static string Quote( string path )
			=> String.Format( "\"{0}\"", path );

		private static bool UseTempOutputDirectory()
		{
			string? value = Environment.GetEnvironmentVariable( "MYLL_TEST_TEMP" );
			return !string.IsNullOrEmpty( value );
		}

		private static void CleanupBuildArtifacts( string generatedDir )
		{
			if( !Directory.Exists( generatedDir ) )
				return;

			try
			{
				foreach( string file in Directory.GetFiles( generatedDir ) )
				{
					string ext = Path.GetExtension( file ).ToLowerInvariant();
					string name = Path.GetFileName( file );
					if( ext == ".o" || ext == ".obj" || name == ExecutableName )
						File.Delete( file );
				}
			}
			catch
			{
				// Best-effort cleanup only.
			}
		}

		private static void CopySupportFiles( string caseDir, string generatedDir )
		{
			if( !Directory.Exists( caseDir ) || !Directory.Exists( generatedDir ) )
				return;

			foreach( string file in Directory.GetFiles( caseDir ) ) {
				string extension = Path.GetExtension( file ).ToLowerInvariant();
				if( extension is not (".hpp" or ".cpp" or ".h") )
					continue;

				string fileName = Path.GetFileName( file );
				string dest     = Path.Combine( generatedDir, fileName );

				if( File.Exists( dest ) )
					continue; // do not overwrite generated sources

				File.Copy( file, dest );
			}
		}

		private static void TryDeleteDirectory( string path )
		{
			try
			{
				if( Directory.Exists( path ) )
					Directory.Delete( path, true );
			}
			catch
			{
				// Best-effort cleanup only.
			}
		}
	}
}
