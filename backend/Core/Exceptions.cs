using System;
using Antlr4.Runtime;

namespace Myll
{
	public class UnreachableException : Exception
	{
		public UnreachableException( IToken token )
			: base( String.Format(
				"reached unreachable code at line {0}, column {1}: token '{2}' ({3})",
				token.Line,
				token.Column,
				token.Text,
				token.Type ) ) {}
	}
}
