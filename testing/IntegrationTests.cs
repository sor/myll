using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
			string generatedDir = UseTempOutputDirectory()
				? Path.Combine( RepoRoot, "testing", "generated", String.Format( "tmp_{0}_{1:N}", caseName, Guid.NewGuid() ) )
				: Path.Combine( RepoRoot, "testing", "generated", caseName );

			bool cleanupGeneratedDir = UseTempOutputDirectory();
			try
			{
				Directory.CreateDirectory( generatedDir );

				string[] myllFiles = Directory.GetFiles( caseDir, "*.myll" );
				Assert.NotEmpty( myllFiles );

				// The frontend treats each '-i' argument as a search pattern relative to the current working directory.
				// Run the compiler from inside the case directory with a simple "*.myll" pattern.
				string myllFlags = CaseConfig.UseMyllCompileRun( caseName ) ? "-Ccr" : "-C";
				string myllArgs = String.Format( "exec \"{0}\" -i \"*.myll\" -o {1} {2}", FrontendDll,
					Quote( generatedDir ), myllFlags );

				output.WriteLine( "Running: dotnet " + myllArgs + " in " + caseDir );
				ProcessResult myllResult = ProcessRunner.Run( "dotnet", myllArgs, workingDirectory: caseDir,
					timeout: CaseConfig.MyllTimeout( caseName ) );

				output.WriteLine( myllResult.StdOut );
				if( !string.IsNullOrEmpty( myllResult.StdErr ) )
					output.WriteLine( "STDERR: " + myllResult.StdErr );

				bool expectRunFailure = CaseConfig.ExpectRunFailure( caseName );
				if( expectRunFailure && CaseConfig.UseMyllCompileRun( caseName ) )
				{
					output.WriteLine( "Case is configured to expect a run failure via Myll's internal -cr path." );
					Assert.True(
						myllResult.ExitCode != 0 && !myllResult.TimedOut,
						string.Format( "Expected the generated binary to fail, but Myll returned exit code {0}{1}.",
							myllResult.ExitCode, myllResult.TimedOut ? " after timing out" : "" ) );
					return;
				}

				Assert.Equal( 0, myllResult.ExitCode );
				Assert.False( myllResult.TimedOut, "Myll compiler timed out" );

				bool expectCppCompileFailure = CaseConfig.ExpectCppCompileFailure( caseName );

				// 2. Golden file comparison
				if( !expectCppCompileFailure )
				{
					string goldenDir = Path.Combine( RepoRoot, "testing", "golden", caseName );
					bool goldenMatch = GoldenFileComparer.Compare( generatedDir, goldenDir, out string diffReport );
					if( !goldenMatch )
					{
						output.WriteLine( "Golden file mismatch:\n" + diffReport );
						Assert.True( goldenMatch, diffReport );
					}
				}
				else
				{
					output.WriteLine( "Skipping golden comparison; case is configured to expect C++ compile failure." );
				}

			// 3. Optional C++ compile + run
			if( CaseConfig.UseMyllCompileRun( caseName ) )
			{
				output.WriteLine( "Skipping harness C++ compile/run; Myll's internal -cr path was used." );
				return;
			}

			if( CaseConfig.IsGenerateOnly( caseName ) )
			{
				output.WriteLine( "Generate-only case; skipping C++ compile/run." );
				return;
			}

			string[] cppFiles = Directory.GetFiles( generatedDir, "*.cpp" );
			if( cppFiles.Length == 0 )
				return;

			CompileAndRunCpp( cppFiles, generatedDir, caseName, expectCppCompileFailure );
			}
			finally
			{
				if( cleanupGeneratedDir )
					TryDeleteDirectory( generatedDir );
			}
		}

		private void CompileAndRunCpp( string[] cppFiles, string workingDir, string caseName, bool expectFailure )
		{
			string executable = RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
				? "test.exe"
				: "test.out";

			string binaryPath = Path.Combine( workingDir, executable );
			CppCompilerInvocation invocation = CppCompiler.CreateInvocation( cppFiles, outputPath: binaryPath );

			output.WriteLine( string.Format( "Running: {0} {1}", invocation.Compiler, invocation.Arguments ) );
			ProcessResult compileResult = ProcessRunner.Run(
				invocation.Compiler, invocation.Arguments, workingDirectory: workingDir,
				timeout: CaseConfig.CompileTimeout( caseName ) );

			output.WriteLine( compileResult.StdOut );
			if( !string.IsNullOrEmpty( compileResult.StdErr ) )
				output.WriteLine( "STDERR: " + compileResult.StdErr );

			if( expectFailure )
			{
				Assert.False( compileResult.ExitCode == 0,
					string.Format( "Expected C++ compile to fail, but {0} succeeded.", invocation.Compiler ) );
				return;
			}

			Assert.Equal( 0, compileResult.ExitCode );

			// Run the binary
			output.WriteLine( "Running: " + binaryPath );
			ProcessResult runResult = ProcessRunner.Run( binaryPath, "", workingDirectory: workingDir,
				environment: new Dictionary<string, string> { ["MYLL_TEST"] = "1" },
				timeout: CaseConfig.RunTimeout( caseName ) );

			output.WriteLine( runResult.StdOut );
			if( !string.IsNullOrEmpty( runResult.StdErr ) )
				output.WriteLine( "STDERR: " + runResult.StdErr );

			Assert.Equal( 0, runResult.ExitCode );
			Assert.False( runResult.TimedOut, "Generated binary timed out" );
		}

		private static string Quote( string path )
			=> String.Format( "\"{0}\"", path );

		private static bool UseTempOutputDirectory()
		{
			string? value = Environment.GetEnvironmentVariable( "MYLL_TEST_TEMP" );
			return !string.IsNullOrEmpty( value );
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
