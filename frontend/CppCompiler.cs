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

			string compiler = FindCompiler();
			return new CppCompilerInvocation(
				compiler,
				BuildArguments( compiler, cppFiles, flags, outputPath ) );
		}

		public static CppCompilerInvocation CreateCompileInvocation(
			string  sourceFile,
			string? flags = null,
			string? outputObject = null )
		{
			if( string.IsNullOrWhiteSpace( sourceFile ) )
				throw new ArgumentException( "A C++ source file is required.", nameof( sourceFile ) );

			string compiler = FindCompiler();
			return new CppCompilerInvocation(
				compiler,
				BuildCompileArguments( compiler, sourceFile, flags, outputObject ) );
		}

		public static CppCompilerInvocation CreateLinkInvocation(
			IReadOnlyCollection<string> objectFiles,
			string                      outputPath,
			string?                     flags = null )
		{
			if( objectFiles == null || objectFiles.Count == 0 )
				throw new ArgumentException( "At least one object file is required.", nameof( objectFiles ) );

			if( string.IsNullOrWhiteSpace( outputPath ) )
				throw new ArgumentException( "An output path is required.", nameof( outputPath ) );

			string compiler = FindCompiler();
			return new CppCompilerInvocation(
				compiler,
				BuildLinkArguments( compiler, objectFiles, flags, outputPath ) );
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

		private static string FindCompiler()
		{
			string? envCompiler = Environment.GetEnvironmentVariable( "MYLL_CXX" );
			if( !string.IsNullOrWhiteSpace( envCompiler ) ) {
				if( !Exists( envCompiler ) )
					throw new InvalidOperationException(
						string.Format( "C++ compiler '{0}' specified by MYLL_CXX was not found.", envCompiler ) );

				return envCompiler;
			}

			string[] compilers = RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
				? new[] { "cl", "clang++", "g++" }
				: new[] { "clang++", "g++", "cl" };

			foreach( string compiler in compilers ) {
				if( Exists( compiler ) )
					return compiler;
			}

			throw new InvalidOperationException(
				"No C++ compiler found. Tried: " + string.Join( ", ", compilers ) );
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

		private static string BuildCompileArguments(
			string  compiler,
			string  sourceFile,
			string? flags,
			string? outputObject )
		{
			if( IsCl( compiler ) ) {
				var parts = new List<string> { "/nologo", "/std:c++20", "/EHsc", "/c" };
				if( !string.IsNullOrWhiteSpace( flags ) )
					parts.Add( flags );
				parts.Add( Quote( sourceFile ) );
				if( !string.IsNullOrWhiteSpace( outputObject ) )
					parts.Add( "/Fo:" + Quote( outputObject ) );
				return string.Join( " ", parts );
			}
			else {
				var parts = new List<string> { "-std=c++20", "-c" };
				if( !string.IsNullOrWhiteSpace( flags ) )
					parts.Add( flags );
				parts.Add( Quote( sourceFile ) );
				if( !string.IsNullOrWhiteSpace( outputObject ) )
					parts.AddRange( new[] { "-o", Quote( outputObject ) } );
				return string.Join( " ", parts );
			}
		}

		private static string BuildLinkArguments(
			string                      compiler,
			IReadOnlyCollection<string> objectFiles,
			string?                     flags,
			string                      outputPath )
		{
			string quotedFiles = string.Join( " ", objectFiles.Select( Quote ) );

			if( IsCl( compiler ) ) {
				var parts = new List<string> { "/nologo" };
				if( !string.IsNullOrWhiteSpace( flags ) )
					parts.Add( flags );
				parts.Add( quotedFiles );
				parts.Add( "/Fe:" + Quote( outputPath ) );
				return string.Join( " ", parts );
			}
			else {
				var parts = new List<string>();
				if( !string.IsNullOrWhiteSpace( flags ) )
					parts.Add( flags );
				parts.Add( quotedFiles );
				parts.AddRange( new[] { "-o", Quote( outputPath ) } );
				return string.Join( " ", parts );
			}
		}

		private static string Quote( string path )
			=> string.Format( "\"{0}\"", path );
	}
}
