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
			Validator walker = new( Diagnostics );
			walker.WalkModule( ctx.Module.Module );
		}

	private sealed class Validator : DeclWalker
	{
		private readonly List<Diagnostic> diagnostics;

		public Validator( List<Diagnostic> diagnostics ) => this.diagnostics = diagnostics;

		protected override void TransformDecl( Decl decl )
		{
			if( decl is Func func )
				ValidateFunc( func, diagnostics );
			else if( decl is AliasDecl alias )
				ValidateAlias( alias, diagnostics );

			base.TransformDecl( decl );
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

	private static void ValidateAlias( AliasDecl alias, List<Diagnostic> diagnostics )
	{
		// Alias templates are type aliases; they cannot rename namespaces.
		if( alias.TplParams.Count > 0 && alias.IsNamespaceAlias ) {
			diagnostics.Add( new Diagnostic(
				alias.srcPos,
				DiagnosticKind.Error,
				string.Format(
					"Namespace alias '{0}' cannot have template parameters",
					alias.name ) ) );
		}
	}
	}
}
