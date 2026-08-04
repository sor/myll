using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Myll
{
	public sealed record CppCompilerInvocation( string Compiler, string Arguments );

	public static class CppCompiler
	{
		public static CppCompilerInvocation CreateInvocation(
			IReadOnlyCollection<string> cppFiles,
			string? flags = null,
			string? outputPath = null )
		{
			if( cppFiles == null || cppFiles.Count == 0 )
				throw new ArgumentException( "At least one C++ source file is required.", nameof( cppFiles ) );

			string? envCompiler = Environment.GetEnvironmentVariable( "MYLL_CXX" );
			if( !string.IsNullOrWhiteSpace( envCompiler ) ) {
				if( !Exists( envCompiler ) )
					throw new InvalidOperationException(
						string.Format( "C++ compiler '{0}' specified by MYLL_CXX was not found.", envCompiler ) );

				return new CppCompilerInvocation(
					envCompiler,
					BuildArguments( envCompiler, cppFiles, flags, outputPath ) );
			}

			string[] compilers = RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
				? new[] { "cl", "clang++", "g++" }
				: new[] { "clang++", "g++", "cl" };

			foreach( string compiler in compilers ) {
				if( Exists( compiler ) )
					return new CppCompilerInvocation(
						compiler,
						BuildArguments( compiler, cppFiles, flags, outputPath ) );
			}

			throw new InvalidOperationException(
				"No C++ compiler found. Tried: " + string.Join( ", ", compilers ) );
		}

		public static bool IsCl( string name )
		{
			return Path.GetFileNameWithoutExtension( name )
				.Equals( "cl", StringComparison.OrdinalIgnoreCase );
		}

		public static bool Exists( string name )
		{
			try {
				string args = IsCl( name ) ? "/nologo /?" : "--version";
				Process process = new();
				process.StartInfo = new() {
					FileName               = name,
					Arguments              = args,
					RedirectStandardOutput = true,
					RedirectStandardError  = true,
					UseShellExecute        = false,
				};
				process.Start();
				process.WaitForExit();
				return process.ExitCode == 0;
			}
			catch {
				return false;
			}
		}

		private static string BuildArguments(
			string compiler,
			IReadOnlyCollection<string> cppFiles,
			string? flags,
			string? outputPath )
		{
			string quotedFiles = string.Join( " ", cppFiles.Select( Quote ) );

			if( IsCl( compiler ) ) {
				var parts = new List<string> { "/nologo", "/std:c++20", "/EHsc" };
				if( !string.IsNullOrWhiteSpace( flags ) )
					parts.Add( flags );
				parts.Add( quotedFiles );
				if( !string.IsNullOrWhiteSpace( outputPath ) )
					parts.Add( "/Fe:" + Quote( outputPath ) );
				return string.Join( " ", parts );
			}
			else {
				var parts = new List<string> { "-std=c++20" };
				if( !string.IsNullOrWhiteSpace( flags ) )
					parts.Add( flags );
				parts.Add( quotedFiles );
				if( !string.IsNullOrWhiteSpace( outputPath ) )
					parts.AddRange( new[] { "-o", Quote( outputPath ) } );
				return string.Join( " ", parts );
			}
		}

		private static string Quote( string path )
			=> string.Format( "\"{0}\"", path );
	}
}
