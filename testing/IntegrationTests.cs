using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Myll.Tests
{
	public sealed class IntegrationTests
	{
		private readonly ITestOutputHelper output;

		private static readonly string RepoRoot = Path.GetFullPath(
			Path.Combine( AppContext.BaseDirectory, "..", "..", "..", ".." ) );

		private static readonly string FrontendDll = Path.Combine( RepoRoot, "frontend", "bin", "Debug", "net10.0", "myll.dll" );

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
				? Path.Combine( RepoRoot, "testing", "generated", $"tmp_{caseName}_{Guid.NewGuid():N}" )
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
			string myllArgs  = $"exec \"{FrontendDll}\" -i \"*.myll\" -o {Quote( generatedDir )} {myllFlags}";

			output.WriteLine( $"Running: dotnet {myllArgs} in {caseDir}" );
			ProcessResult myllResult = ProcessRunner.Run( "dotnet", myllArgs, workingDirectory: caseDir,
				timeout: CaseConfig.MyllTimeout( caseName ) );

			output.WriteLine( myllResult.StdOut );
			if( !string.IsNullOrEmpty( myllResult.StdErr ) )
				output.WriteLine( "STDERR: " + myllResult.StdErr );

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
			string cppArgList = string.Join( " ", cppFiles.Select( Quote ) );

			(string compiler, bool found)[] compilers = {
				("clang++", CompilerExists( "clang++" )),
				("g++", CompilerExists( "g++" )),
				("cl", CompilerExists( "cl" )),
			};

			bool anyFound = false;
			bool anySucceeded = false;
			foreach( var (compiler, found) in compilers )
			{
				if( !found )
				{
					output.WriteLine( $"Skipping {compiler}: not found." );
					continue;
				}

				anyFound = true;

				string args = compiler == "cl"
					? $"/nologo /std:c++20 /EHsc {cppArgList} /Fe:{Quote( binaryPath )}"
					: $"-std=c++20 {cppArgList} -o {Quote( binaryPath )}";
				output.WriteLine( $"Running: {compiler} {args}" );
				ProcessResult compileResult = ProcessRunner.Run( compiler, args, workingDirectory: workingDir,
					timeout: CaseConfig.CompileTimeout( caseName ) );

				output.WriteLine( compileResult.StdOut );
				if( !string.IsNullOrEmpty( compileResult.StdErr ) )
					output.WriteLine( "STDERR: " + compileResult.StdErr );

				if( compileResult.ExitCode == 0 )
				{
					anySucceeded = true;
				}
				else if( !expectFailure )
				{
					Assert.Fail( $"C++ compiler {compiler} failed with exit code {compileResult.ExitCode}." );
				}
			}

			if( !anyFound )
			{
				if( expectFailure )
					Assert.Fail( "No C++ compiler available; cannot verify expected compile failure." );

				output.WriteLine( "No C++ compiler available; skipping binary execution." );
				return;
			}

			if( expectFailure )
			{
				Assert.False( anySucceeded, "Expected C++ compile to fail, but at least one compiler succeeded." );
				return;
			}

			Assert.True( anySucceeded, "No C++ compiler succeeded; cannot run generated binary." );

			// Run the binary
			output.WriteLine( $"Running: {binaryPath}" );
			ProcessResult runResult = ProcessRunner.Run( binaryPath, "", workingDirectory: workingDir,
				environment: new Dictionary<string, string> { ["MYLL_TEST"] = "1" },
				timeout: CaseConfig.RunTimeout( caseName ) );

			output.WriteLine( runResult.StdOut );
			if( !string.IsNullOrEmpty( runResult.StdErr ) )
				output.WriteLine( "STDERR: " + runResult.StdErr );

			Assert.Equal( 0, runResult.ExitCode );
			Assert.False( runResult.TimedOut, "Generated binary timed out" );
		}

		private static bool CompilerExists( string name )
		{
			try
			{
				string args = name == "cl" ? "/nologo /?" : "--version";
				ProcessResult r = ProcessRunner.Run( name, args, timeout: TimeSpan.FromSeconds( 5 ) );
				return r.ExitCode == 0;
			}
			catch
			{
				return false;
			}
		}

		private static string Quote( string path )
			=> $"\"{path}\"";

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
