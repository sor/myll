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
			string generatedDir = UseTempOutputDirectory()
				? Path.Combine( RepoRoot, "testing", "generated", String.Format( "tmp_{0}_{1:N}", caseName, Guid.NewGuid() ) )
				: Path.Combine( RepoRoot, "testing", "generated", caseName );

			bool cleanupGeneratedDir = UseTempOutputDirectory();
			TestDepth depth = CaseConfig.GetDepth( caseName );

			try
			{
				Directory.CreateDirectory( generatedDir );

				string[] myllFiles = Directory.GetFiles( caseDir, "*.myll" );
				Assert.NotEmpty( myllFiles );

				bool useMyllCr = CaseConfig.UseMyllCompileRun( caseName ) && depth == TestDepth.Run;
				string myllFlags = useMyllCr ? "-Ccr" : "-C";
				string myllArgs = String.Format( "exec \"{0}\" -i \"*.myll\" -o {1} {2}", FrontendDll,
					Quote( generatedDir ), myllFlags );

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
