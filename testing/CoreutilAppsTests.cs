using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Myll.Tests
{
	/// <summary>
	/// Quick smoke test for the coreutil sample apps.
	/// The default harness only generates C++ for all utilities; the actual C++
	/// compilation is left to a pre-submit / CI step (see AGENTS.md).
	/// </summary>
	public sealed class CoreutilAppsTests
	{
		private static readonly string RepoRoot = Path.GetFullPath(
			Path.Combine( AppContext.BaseDirectory, "..", "..", "..", ".." ) );

		private static readonly string FrontendDll = GetFrontendDll();

		private readonly ITestOutputHelper output;

		public CoreutilAppsTests( ITestOutputHelper output )
		{
			this.output = output;
		}

		[Fact]
		public void GenerateCoreutilApps()
		{
			string coreutilDir  = Path.Combine( RepoRoot, "frontend", "coreutil" );
			string generatedDir = Path.Combine( RepoRoot, "testing", "generated",
				String.Format( "coreutil_apps_tmp_{0:N}", Guid.NewGuid() ) );

			Directory.CreateDirectory( generatedDir );

			try {
				string args = String.Format(
					"exec \"{0}\" -i \"*.myll\" -o {1} -C",
					FrontendDll,
					Quote( generatedDir ) );

				output.WriteLine( "Running: dotnet " + args + " in " + coreutilDir );
				ProcessResult result = ProcessRunner.Run(
					"dotnet", args,
					workingDirectory: coreutilDir,
					timeout: TimeSpan.FromSeconds( 60 ) );

				output.WriteLine( result.StdOut );
				if( !string.IsNullOrEmpty( result.StdErr ) )
					output.WriteLine( "STDERR: " + result.StdErr );

				Assert.Equal( 0, result.ExitCode );
				Assert.False( result.TimedOut, "Myll compiler timed out" );
				Assert.True(
					Directory.GetFiles( generatedDir, "*.cpp" ).Length > 0,
					"No .cpp files were generated" );
			}
			finally {
				TryDeleteDirectory( generatedDir );
			}
		}

		private static string GetFrontendDll()
		{
			string testingBin = AppContext.BaseDirectory.TrimEnd( Path.DirectorySeparatorChar );
			string frameworkDir = Path.GetFileName( testingBin )!;
			string configDir    = Path.GetFileName( Path.GetDirectoryName( testingBin ) )!;

			return Path.Combine( RepoRoot, "frontend", "bin", configDir, frameworkDir, "myll.dll" );
		}

		private static string Quote( string value )
		{
			return value.Contains( ' ' ) ? "\"" + value + "\"" : value;
		}

		private static void TryDeleteDirectory( string path )
		{
			try {
				Directory.Delete( path, recursive: true );
			}
			catch {
				// Best-effort cleanup.
			}
		}
	}
}
