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
}
