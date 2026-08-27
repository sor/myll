using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	public sealed class NameResolver
	{
		private readonly IReadOnlyDictionary<string, ModuleExports> moduleExports;
		private readonly ResolutionResult result;
		private readonly List<Diagnostic> diagnostics;

		private NameResolver(
			IReadOnlyDictionary<string, ModuleExports> moduleExports,
			ResolutionResult result,
			List<Diagnostic> diagnostics )
		{
			this.moduleExports = moduleExports;
			this.result        = result;
			this.diagnostics   = diagnostics;
		}

		public static (ResolutionResult Result, IReadOnlyList<Diagnostic> Diagnostics) Resolve(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
		{
			var exports = BuildModuleExports( modules );
			var result      = new ResolutionResult();
			var diagnostics = new List<Diagnostic>();
			var resolver    = new NameResolver( exports, result, diagnostics );

			bool progress;
			do {
				progress = false;
				foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
					progress |= resolver.ResolveIds( module, context );
					progress |= resolver.ResolveTypes( module, context );
				}
			} while( progress );

			resolver.ReportUnresolved( modules );

			return (result, diagnostics);
		}

		private static IReadOnlyDictionary<string, ModuleExports> BuildModuleExports(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
		{
			Dictionary<string, ModuleExports> ret = new();
			foreach( (GlobalNamespace module, _) in modules ) {
				var exports = new ModuleExports();
				CollectExports( module.scope, exports );
				ret[module.module] = exports;
			}

			return ret;
		}

		private static void CollectExports( Scope scope, ModuleExports exports )
		{
			foreach( (string name, List<ScopeLeaf> leaves) in scope.children ) {
				foreach( ScopeLeaf leaf in leaves ) {
					Decl? decl = leaf.decl;
					if( decl == null )
						continue;
					if( decl.IsHidden )
						continue;

					exports.Add( name, decl );

					// Recurse into exported namespaces so importers see nested names.
					if( decl is Namespace ns and Hierarchical h )
						CollectExports( h.scope, exports );
				}
			}
		}

		private bool ResolveIds( GlobalNamespace module, CompilationContext context )
		{
			bool progress = false;
			ModuleExports ownExports = moduleExports[module.module];

			foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
				if( result.Ids.ContainsKey( unresolved.Node ) )
					continue;

				string name = unresolved.Node.idTplArgs.id;

				Decl? resolved = LookupInScope( name, unresolved.Scope )
				              ?? LookupInImports( name, module );

				if( resolved != null ) {
					result.Resolve( unresolved.Node, resolved );
					progress = true;
				}
			}

			return progress;
		}

		private bool ResolveTypes( GlobalNamespace module, CompilationContext context )
		{
			bool progress = false;

			foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
				if( result.Types.ContainsKey( unresolved.Node ) )
					continue;

				// Phase 1: single-segment type names only.
				if( unresolved.Node.idTpls.Count != 1 )
					continue;

				string name = unresolved.Node.idTpls[0].id;

				Decl? resolved = LookupInScope( name, unresolved.Scope )
				              ?? LookupInImports( name, module );

				if( resolved != null ) {
					result.Resolve( unresolved.Node, resolved );
					progress = true;
				}
			}

			return progress;
		}

		private Decl? LookupInScope( string name, Scope scope )
		{
			for( Scope? cur = scope; cur != null; cur = cur.parent ) {
				if( cur.children.TryGetValue( name, out List<ScopeLeaf>? leaves ) ) {
					Decl? single = PickSingle( leaves );
					if( single != null )
						return single;
				}
			}

			return null;
		}

		private Decl? LookupInImports( string name, GlobalNamespace module )
		{
			foreach( string importedModule in module.imps ) {
				if( !moduleExports.TryGetValue( importedModule, out ModuleExports? exports ) )
					continue;

				Decl? resolved = exports.Lookup( name );
				if( resolved != null )
					return resolved;
			}

			return null;
		}

		private static Decl? PickSingle( List<ScopeLeaf> leaves )
		{
			List<Decl> visible = leaves
				.Select( l => l.decl )
				.Where( d => d != null && !d.IsHidden )
				.Cast<Decl>()
				.ToList();

			if( visible.Count == 0 )
				return null;

			// If exactly one name exists, use it.
			if( visible.Count == 1 )
				return visible[0];

			// Ambiguous or overload set; can't pick a single decl yet.
			return null;
		}

		private void ReportUnresolved(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
					if( result.Ids.ContainsKey( unresolved.Node ) )
						continue;

					string name = unresolved.Node.idTplArgs.id;
					diagnostics.Add( new Diagnostic(
						unresolved.Node.srcPos,
						DiagnosticKind.Error,
						String.Format( "Unresolved identifier '{0}'", name ) ) );
				}

				foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
					if( result.Types.ContainsKey( unresolved.Node ) )
						continue;

					string name = unresolved.Node.idTpls.Count > 0
						? unresolved.Node.idTpls[0].id
						: "<unknown>";
					diagnostics.Add( new Diagnostic(
						unresolved.Node.srcPos,
						DiagnosticKind.Error,
						String.Format( "Unresolved type '{0}'", name ) ) );
				}
			}
		}

		private sealed class ModuleExports
		{
			private readonly Dictionary<string, List<Decl>> exports = new();

			public void Add( string name, Decl decl )
			{
				if( !exports.TryGetValue( name, out List<Decl>? list ) ) {
					list = new List<Decl>( 1 );
					exports.Add( name, list );
				}

				list.Add( decl );
			}

			public Decl? Lookup( string name )
			{
				if( !exports.TryGetValue( name, out List<Decl>? list ) )
					return null;

				return PickSingleFromExports( list );
			}

			private static Decl? PickSingleFromExports( List<Decl> decls )
			{
				List<Decl> visible = decls.Where( d => !d.IsHidden ).ToList();
				if( visible.Count == 1 )
					return visible[0];

				return null;
			}
		}
	}
}
