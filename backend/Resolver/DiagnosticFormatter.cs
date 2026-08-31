using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Myll.Core;

namespace Myll.Resolver
{
	public static class DiagnosticFormatter
	{
		public const string ErrorColor   = "\x1b[31m";
		public const string WarningColor = "\x1b[33m";
		public const string NoteColor    = "\x1b[36m";
		public const string ResetColor   = "\x1b[0m";
		private const int TabWidth = 4;

		public static string Format( Diagnostic diagnostic, bool useColor = false )
		{
			StringBuilder sb = new();
			AppendDiagnostic( sb, diagnostic, useColor, new FileCache() );
			return sb.ToString();
		}

		public static string Format( IEnumerable<Diagnostic> diagnostics, bool useColor = false )
		{
			StringBuilder sb = new();
			FileCache cache = new();

			foreach( Diagnostic diagnostic in diagnostics )
				AppendDiagnostic( sb, diagnostic, useColor, cache );

			return sb.ToString();
		}

		private static void AppendDiagnostic(
			StringBuilder sb,
			Diagnostic diagnostic,
			bool useColor,
			FileCache cache )
		{
			AppendHeader( sb, diagnostic, useColor );
			if( diagnostic.Location != null )
				AppendSourceContext( sb, diagnostic.Location, cache, diagnostic.Kind, useColor );
		}

		private static void AppendHeader( StringBuilder sb, Diagnostic diagnostic, bool useColor )
		{
			string kindText = diagnostic.Kind.ToString().ToLowerInvariant();
			string kindPart = useColor
				? ColorFor( diagnostic.Kind ) + kindText + ResetColor
				: kindText;

			if( diagnostic.Location == null ) {
				sb.AppendLine( System.String.Format( "{0}: {1}", kindPart, diagnostic.Message ) );
				return;
			}

			sb.AppendLine(
				System.String.Format(
					"[{0}:{1}:{2}] {3}: {4}",
					diagnostic.Location.file,
					diagnostic.Location.from.line,
					diagnostic.Location.from.col + 1,
					kindPart,
					diagnostic.Message ) );
		}

		private static void AppendSourceContext(
			StringBuilder sb,
			SrcPos srcPos,
			FileCache cache,
			DiagnosticKind kind,
			bool useColor )
		{
			if( !File.Exists( srcPos.file ) )
				return;

			string[] lines = cache.GetLines( srcPos.file );

			if( srcPos.from.line <= 0 || srcPos.from.line > lines.Length )
				return;

			int firstLine = Math.Max( srcPos.from.line - 1, 1 );
			int lastLine  = Math.Min( Math.Max( srcPos.to.line, srcPos.from.line ) + 1, lines.Length );

			for( int lineNo = firstLine; lineNo <= lastLine; lineNo++ ) {
				string rawLine    = lines[lineNo - 1];
				string line       = ExpandTabs( rawLine );
				string linePrefix = System.String.Format( "{0,4} | ", lineNo );

				sb.Append( linePrefix );
				sb.AppendLine( line );

				if( lineNo < srcPos.from.line || lineNo > srcPos.to.line )
					continue;

				int startCol = lineNo == srcPos.from.line ? srcPos.from.col : 0;
				int endCol   = lineNo == srcPos.to.line   ? srcPos.to.col : rawLine.Length;

				if( endCol <= startCol )
					endCol = startCol + 1;

				endCol = Math.Min( endCol, rawLine.Length );

				if( startCol < rawLine.Length ) {
					int visualStart = ToVisualColumn( rawLine, startCol );
					int visualEnd   = ToVisualColumn( rawLine, endCol );
					int length      = visualEnd - visualStart;

					sb.Append( new string( ' ', linePrefix.Length + visualStart ) );

					if( useColor )
						sb.Append( ColorFor( kind ) );

					sb.Append( new string( '^', Math.Max( length, 1 ) ) );

					if( useColor )
						sb.Append( ResetColor );

					sb.AppendLine();
				}
			}
		}

		private static string ExpandTabs( string line )
		{
			StringBuilder sb = new();
			foreach( char c in line ) {
				if( c == '\t' )
					sb.Append( new string( ' ', TabWidth - ( sb.Length % TabWidth ) ) );
				else
					sb.Append( c );
			}
			return sb.ToString();
		}

		private static int ToVisualColumn( string line, int column )
		{
			int visual = 0;
			for( int i = 0; i < column && i < line.Length; i++ ) {
				if( line[i] == '\t' )
					visual += TabWidth - ( visual % TabWidth );
				else
					visual++;
			}
			return visual;
		}

		private static string ColorFor( DiagnosticKind kind )
			=> kind switch {
				DiagnosticKind.Error   => ErrorColor,
				DiagnosticKind.Warning => WarningColor,
				DiagnosticKind.Note    => NoteColor,
				_                      => "",
			};

		private sealed class FileCache
		{
			private readonly Dictionary<string, string[]> cache = new();

			public string[] GetLines( string path )
			{
				if( !cache.TryGetValue( path, out string[]? lines ) ) {
					lines = File.ReadAllLines( path );
					cache.Add( path, lines );
				}

				return lines;
			}
		}
	}
}
