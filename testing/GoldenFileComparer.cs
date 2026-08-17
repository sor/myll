using System;
using System.IO;
using System.Linq;

namespace Myll.Tests
{
	public static class GoldenFileComparer
	{
		public static bool Compare( string generatedDir, string goldenDir, out string diffReport )
		{
			diffReport = "";

			if( !Directory.Exists( goldenDir ) )
				return true; // No golden directory means skip comparison

			var goldenFiles = Directory.GetFiles( goldenDir, "*", SearchOption.AllDirectories )
			                           .Select( f => Path.GetRelativePath( goldenDir, f ) )
			                           .OrderBy( f => f )
			                           .ToList();

			var generatedFiles = Directory.Exists( generatedDir )
				? Directory.GetFiles( generatedDir, "*", SearchOption.AllDirectories )
				           .Select( f => Path.GetRelativePath( generatedDir, f ) )
				           .Where( f => !IsBuildArtifact( f ) )
				           .OrderBy( f => f )
				           .ToList()
				: new();

			bool match = true;

			foreach( var gf in goldenFiles )
			{
				var genPath = Path.Combine( generatedDir, gf );
				if( !File.Exists( genPath ) )
				{
					diffReport += "Missing generated file: " + gf + "\n";
					match = false;
					continue;
				}

				var goldenBytes = File.ReadAllBytes( Path.Combine( goldenDir, gf ) );
				var genBytes    = File.ReadAllBytes( genPath );

				if( !goldenBytes.SequenceEqual( genBytes ) )
				{
					diffReport += "Mismatch: " + gf + "\n";
					match = false;
				}
			}

			foreach( var gf in generatedFiles )
			{
				if( !goldenFiles.Contains( gf ) )
				{
					diffReport += "Unexpected generated file not in golden: " + gf + "\n";
					match = false;
				}
			}

			return match;
		}

		private static bool IsBuildArtifact( string relativePath )
		{
			string ext  = Path.GetExtension( relativePath ).ToLowerInvariant();
			string name = Path.GetFileNameWithoutExtension( relativePath ).ToLowerInvariant();
			if( ext is ".exe" or ".out" )
				return true;

			if( string.IsNullOrEmpty( ext ) && ( name == "a" || name == "test" ) )
				return true;

			return false;
		}
	}
}
