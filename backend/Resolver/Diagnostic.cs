using System.Collections.Generic;
using Myll.Core;
using Myll.Resolver;

namespace Myll
{
	public enum DiagnosticKind
	{
		Error,
		Warning,
		Note,
	}

	public sealed record Diagnostic(
		SrcPos? Location,
		DiagnosticKind Kind,
		string Message );

	public sealed record ParserResult(
		MyllParser      Parser,
		List<Diagnostic> Diagnostics,
		bool            IsCompilerOwned );

	public sealed record ParseResult(
		MyllParser.ProgContext Prog,
		List<Diagnostic> Diagnostics );

	public sealed record ResolveResult(
		ResolutionResult Result,
		IReadOnlyList<Diagnostic> Diagnostics );

	public sealed record GeneratedModuleFilesResult(
		List<(string Path, IEnumerable<SourceLine> Lines)> Files,
		List<Diagnostic> Diagnostics );
}
