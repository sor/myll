using System.Collections.Generic;
using Myll;
using Myll.Core;

namespace Myll.Resolver
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
		MyllParser Parser,
		List<Diagnostic> Diagnostics );

	public sealed record ParseResult(
		MyllParser.ProgContext Prog,
		List<Diagnostic> Diagnostics );

	public sealed record ResolveResult(
		ResolutionResult Result,
		IReadOnlyList<Diagnostic> Diagnostics );

	public sealed record GeneratedFilesResult(
		List<(string Path, IEnumerable<string> Lines)> Files,
		List<Diagnostic> Diagnostics );
}
