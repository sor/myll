using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Myll.Core;

namespace Myll.Generator
{
	public static class SourceMapGenerator
	{
		/// <summary>
		/// Inserts C++ <c>#line</c> directives into a generated file so that C++
		/// compiler diagnostics map back to the original Myll source locations.
		/// </summary>
		/// <param name="lines">The generated lines produced by the generator.</param>
		/// <param name="outputBasePath">Directory containing the generated file; used to make source paths relative.</param>
		public static IEnumerable<SourceLine> Apply(
			IEnumerable<SourceLine> lines,
			string               outputBasePath )
		{
			// currentFile/currentLine describe the mapping active *right now* for the next
			// line that will be emitted by the C++ compiler.
			string? currentFile           = null;
			int     currentLine           = 0;
			int     linesSinceLastDirective = 0;

			foreach( SourceLine line in lines ) {
				SrcPos? pos = line.SrcPos;
				if( pos != null ) {
					bool fileChanged = currentFile != pos.file;
					int  expectedLine = currentLine + linesSinceLastDirective;

					if( fileChanged || expectedLine != pos.from.line ) {
						currentFile              = pos.file;
						currentLine              = pos.from.line;
						linesSinceLastDirective  = 0;

						string mappedFile = MakeRelativePath( outputBasePath, pos.file );
						string directive  = fileChanged
							? String.Format( "#line {0} \"{1}\"", pos.from.line, mappedFile )
							: String.Format( "#line {0}", pos.from.line );

						yield return new SourceLine( directive );
					}
				}

				yield return line;
				++linesSinceLastDirective;
			}
		}

		/// <summary>
		/// Removes redundant file names from generated <c>#line</c> directives.
		/// A <c>#line</c> only needs to repeat the file name when the file changes
		/// from the previous directive.
		/// </summary>
		public static IEnumerable<SourceLine> NormalizeFileDirectives(
			IEnumerable<SourceLine> lines )
		{
			string? lastFile = null;

			foreach( SourceLine line in lines ) {
				string text = line.Text;
				Match m = DirectiveRegex.Match( text );

				if( m.Success ) {
					int     lineNumber = int.Parse( m.Groups[1].Value );
					Group   fileGroup  = m.Groups[2];

					if( fileGroup.Success ) {
						string file = fileGroup.Value;
						if( lastFile == file ) {
							yield return new SourceLine(
								String.Format( "#line {0}", lineNumber ) );
						}
						else {
							lastFile = file;
							yield return line;
						}
					}
					else {
						yield return line;
					}
				}
				else {
					yield return line;
				}
			}
		}

		private static readonly Regex DirectiveRegex
			= new(
				@"^#line\s+(\d+)(?:\s+""([^""]*)"")?\s*$",
				RegexOptions.Compiled | RegexOptions.CultureInvariant );

		private static string MakeRelativePath( string fromDirectory, string toFile )
		{
			string path = Path.GetRelativePath( fromDirectory, toFile );
			return path.Replace( '\\', '/' );
		}
	}
}
