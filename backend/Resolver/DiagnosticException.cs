using System;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Exception that carries a user-facing diagnostic. Generation catches this
	/// in places where the code generator is the earliest point that can detect
	/// the error and no diagnostics channel is available deeper in the AST.
	/// </summary>
	public sealed class DiagnosticException : Exception
	{
		public Diagnostic Diagnostic { get; }

		public DiagnosticException( Diagnostic diagnostic )
			: base( diagnostic.Message )
		{
			Diagnostic = diagnostic;
		}
	}
}
