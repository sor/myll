using System;
using System.Collections.Generic;
using Myll.Core;

namespace Myll.Transform
{
	/// <summary>
	/// Validates attributes that are only meaningful on special member functions.
	/// Runs after default attributes are applied so that dialect-level defaults do
	/// not accidentally silence user-written attribute misuse.
	/// </summary>
	public sealed class SpecialMemberAttributeValidator : Transformer
	{
		protected override void PostTransform( TransformContext ctx )
		{
			ValidateDecl( ctx.Module.Module, Diagnostics );
		}

		private static void ValidateDecl( Decl decl, List<Diagnostic> diagnostics )
		{
			if( decl is Func func )
				ValidateFunc( func, diagnostics );

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					ValidateDecl( child, diagnostics );
			}
		}

		private static void ValidateFunc( Func func, List<Diagnostic> diagnostics )
		{
			// [default] is only meaningful on constructors, destructors, and operator
			// special members. Free functions (including namespace-level ones) cannot
			// be defaulted.
			if( func.IsDefault && !func.IsInStruct ) {
				diagnostics.Add( new Diagnostic(
					func.srcPos,
					DiagnosticKind.Error,
					string.Format(
						"[default] is only valid on special member functions; '{0}' is a free function",
						func.FullyQualifiedName ) ) );
			}
		}
	}
}
