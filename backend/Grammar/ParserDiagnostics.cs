using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Myll.Core;

namespace Myll
{
	public sealed class LexerDiagnosticListener : IAntlrErrorListener<int>
	{
		private readonly string file;
		private readonly List<Diagnostic> diagnostics;

		public LexerDiagnosticListener( string file, List<Diagnostic> diagnostics )
		{
			this.file        = file;
			this.diagnostics = diagnostics;
		}

		public void SyntaxError(
			TextWriter           output,
			IRecognizer          recognizer,
			int                  offendingSymbol,
			int                  line,
			int                  charPositionInLine,
			string               msg,
			RecognitionException e )
		{
			diagnostics.Add( new Diagnostic(
				new SrcPos {
					file = file,
					from = new SrcPos.LineCol { line = line, col = charPositionInLine },
					to   = new SrcPos.LineCol { line = line, col = charPositionInLine + 1 },
				},
				DiagnosticKind.Error,
				msg ) );
		}
	}

	public sealed class ParserDiagnosticListener : IAntlrErrorListener<IToken>
	{
		private readonly List<Diagnostic> diagnostics;

		public ParserDiagnosticListener( List<Diagnostic> diagnostics )
		{
			this.diagnostics = diagnostics;
		}

		public void SyntaxError(
			TextWriter           output,
			IRecognizer          recognizer,
			IToken               offendingSymbol,
			int                  line,
			int                  charPositionInLine,
			string               msg,
			RecognitionException e )
		{
			string file      = offendingSymbol?.TokenSource?.SourceName
			                ?? ((MyllParser)recognizer).SourceName;
			int    startCol  = offendingSymbol?.Column ?? charPositionInLine;
			int    length    = offendingSymbol?.Text?.Length ?? 1;

			diagnostics.Add( new Diagnostic(
				new SrcPos {
					file = file,
					from = new SrcPos.LineCol { line = line, col = startCol },
					to   = new SrcPos.LineCol { line = line, col = startCol + length },
				},
				DiagnosticKind.Error,
				msg ) );
		}
	}
}
