using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
{
	/// <summary>
	/// Reorders nested declarations so a class/struct definition always follows its
	/// same-module bases, keeping everything else in source order. C++ inheritance
	/// needs a complete base type, so forward declarations are not enough to keep
	/// the order-independence invariant.
	///
	/// The sort is stable and minimal: only declarations whose child subtree really
	/// contains a base are moved, cross-module bases are left to the includes, and
	/// inheritance cycles are reported instead of being reordered.
	/// </summary>
	public sealed class StructuralOrderTransformer : Transformer
	{
		protected override void PostTransform( TransformContext ctx )
		{
			GlobalNamespace root = ctx.Module.Module;

			Dictionary<Hierarchical, Hierarchical> parents = new();
			HashSet<Structural> definedStructurals = new();
			CollectHierarchies( root, parents, definedStructurals );

			// per lowest common ancestor: pairs of children (before, after),
			// declaring that `after` must be emitted after `before`
			Dictionary<Hierarchical, List<(Decl before, Decl after)>> childEdges = new();

			foreach( Structural derived in definedStructurals ) {
				foreach( BaseType baseType in derived.basetypes ) {
					Structural? baseStructural = ResolveBaseStructural( baseType, ctx.ResolutionResult );
					if( baseStructural == null || !definedStructurals.Contains( baseStructural ) )
						continue;

					if( baseStructural == derived ) {
						Diagnostics.Add( new Diagnostic(
							derived.srcPos,
							DiagnosticKind.Error,
							String.Format( "Class '{0}' inherits from itself", derived.name ) ) );
					}
					else {
						AddChildEdge( derived, baseStructural, parents, childEdges );
					}
				}
			}

			foreach( (Hierarchical parent, List<(Decl before, Decl after)> edges) in childEdges )
				ApplyStableTopologicalOrder( parent, edges );
		}

		private static void CollectHierarchies(
			Hierarchical node,
			Dictionary<Hierarchical, Hierarchical> parents,
			HashSet<Structural> definedStructurals )
		{
			foreach( Decl child in node.children ) {
				if( child is not Hierarchical hier )
					continue;

				parents[hier] = node;
				if( hier is Structural structural && !structural.IsForwardDeclaration && !structural.IsExternal )
					definedStructurals.Add( structural );

				CollectHierarchies( hier, parents, definedStructurals );
			}
		}

		private static Structural? ResolveBaseStructural( BaseType baseType, ResolutionResult? resolution )
		{
			if( baseType.type is not TypespecNested nested )
				return null;

			Decl? decl = nested.resolvedDecl;
			if( decl == null && resolution != null && resolution.TryGetResolved( nested, out Decl? resolved ) )
				decl = resolved;

			return decl as Structural;
		}

		private void AddChildEdge(
			Structural derived,
			Structural baseStructural,
			Dictionary<Hierarchical, Hierarchical> parents,
			Dictionary<Hierarchical, List<(Decl before, Decl after)>> childEdges )
		{
			// both chains end at the module root, so the common part is never empty
			List<Hierarchical> derivedChain = BuildChain( derived, parents );
			List<Hierarchical> baseChain    = BuildChain( baseStructural, parents );

			int di = derivedChain.Count - 1;
			int bi = baseChain.Count - 1;
			while( di >= 0 && bi >= 0 && derivedChain[di] == baseChain[bi] ) {
				--di;
				--bi;
			}

			if( di < 0 ) {
				Diagnostics.Add( new Diagnostic(
					derived.srcPos,
					DiagnosticKind.Error,
					String.Format( "Class '{0}' cannot inherit from class '{1}' nested inside it",
						derived.name, baseStructural.name ) ) );
				return;
			}
			if( bi < 0 ) {
				Diagnostics.Add( new Diagnostic(
					derived.srcPos,
					DiagnosticKind.Error,
					String.Format( "Class '{0}' cannot inherit from its enclosing class '{1}'",
						derived.name, baseStructural.name ) ) );
				return;
			}

			Hierarchical lca = derivedChain[di + 1];
			if( !childEdges.TryGetValue( lca, out List<(Decl before, Decl after)>? edges ) ) {
				edges = new();
				childEdges.Add( lca, edges );
			}
			edges.Add( (baseChain[bi], derivedChain[di]) );
		}

		private static List<Hierarchical> BuildChain(
			Hierarchical start,
			Dictionary<Hierarchical, Hierarchical> parents )
		{
			List<Hierarchical> chain = new() { start };
			while( parents.TryGetValue( chain[^1], out Hierarchical? parent ) )
				chain.Add( parent );
			return chain;
		}

		private void ApplyStableTopologicalOrder( Hierarchical parent, List<(Decl before, Decl after)> edges )
		{
			Dictionary<Decl, List<Decl>> dependents = new();
			Dictionary<Decl, int> indegree = new();
			foreach( (Decl before, Decl after) in edges ) {
				if( !dependents.TryGetValue( before, out List<Decl>? afters ) ) {
					afters = new();
					dependents.Add( before, afters );
				}
				if( afters.Contains( after ) )
					continue;
				afters.Add( after );
				indegree[after] = indegree.TryGetValue( after, out int deg ) ? deg + 1 : 1;
			}

			List<Decl> children = parent.children;
			HashSet<Decl> remaining = new( children );
			List<Decl> ordered = new( children.Count );

			while( remaining.Count > 0 ) {
				Decl? next = null;
				foreach( Decl child in children ) {
					if( !remaining.Contains( child ) )
						continue;
					if( indegree.TryGetValue( child, out int deg ) && deg > 0 )
						continue;
					next = child;
					break;
				}

				if( next == null ) {
					ReportCycle( children, remaining );
					return;     // keep source order, diagnostics abort the build anyway
				}

				remaining.Remove( next );
				ordered.Add( next );
				if( dependents.TryGetValue( next, out List<Decl>? successors ) ) {
					foreach( Decl successor in successors )
						--indegree[successor];
				}
			}

			if( !ordered.SequenceEqual( children ) ) {
				children.Clear();
				children.AddRange( ordered );
			}
		}

		private void ReportCycle( List<Decl> children, HashSet<Decl> remaining )
		{
			Structural? stuck = children
				.Where( remaining.Contains )
				.OfType<Structural>()
				.FirstOrDefault();

			Diagnostics.Add( new Diagnostic(
				stuck?.srcPos,
				DiagnosticKind.Error,
				String.Format( "Circular inheritance involving class '{0}'",
					stuck?.name ?? "<unknown>" ) ) );
		}
	}
}