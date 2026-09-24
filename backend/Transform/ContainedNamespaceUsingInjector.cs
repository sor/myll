using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Transform
{
	/// <summary>
	/// Prepares function bodies for "contained" namespace usings.
	///
	/// Class/struct/union-scope <c>using NS;</c> is always contained, because C++ does
	/// not allow <c>using namespace</c> inside a class definition. Module-scope
	/// <c>using NS;</c> is contained when the dialect's
	/// <see cref="Dialect.GlobalUsingNS"/> is <c>Contained</c>.
	///
	/// For every function/constructor/destructor body, this transform collects the
	/// namespace names introduced by contained usings in its enclosing scopes and
	/// stores them on the function node. The generator then emits
	/// <c>using namespace NS;</c> at the start of each affected body.
	///
	/// Namespace-scope <c>using NS;</c> is left untouched: it is emitted in the header
	/// and is valid C++.
	/// </summary>
	[Obsolete( "Kept for potential future use; currently Myll emits fully qualified names, so per-function `using namespace` injection is redundant." )]
	public sealed class ContainedNamespaceUsingInjector : Transformer
	{
		protected override void PostTransform( TransformContext ctx )
		{
			GlobalNamespace module  = ctx.Module.Module;
			ModuleContext   context = ctx.Module.Context;
			InjectIntoDecl( module, context );
		}

		private static void InjectIntoDecl( Decl decl, ModuleContext context )
		{
			if( decl is IBodyOwner bodyOwner )
				InjectIntoFuncLike( bodyOwner.InjectedNamespaceUsings, (Scope?)decl.scope?.parent, context );

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					InjectIntoDecl( child, context );
			}
		}

		private static void InjectIntoFuncLike(
			List<string>  target,
			Scope?        scope,
			ModuleContext context )
		{
			HashSet<string> seen = new();

			for( Scope? cur = scope; cur != null; cur = cur.parent ) {
				if( !context.ContainedNamespaceUsingsByScope.TryGetValue( cur, out List<string>? list ) )
					continue;

				foreach( string ns in list ) {
					if( seen.Add( ns ) )
						target.Add( ns );
				}
			}
		}
	}
}
