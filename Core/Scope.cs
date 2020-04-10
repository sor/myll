using System.Collections.Generic;

namespace Myll.Core
{
	public class ScopeLeaf
	{
		public Scope parent; // only global scope has this set to null
		public Decl  decl;

		public bool HasDecl => decl != null;
	}

	// has fast indexable decls
	// IF hierarchical
	// THEN visible from outside
	// ELSE NOT visible from outside
	public class Scope : ScopeLeaf
	{
		// opt
		public new Hierarchical decl {
			get => base.decl as Hierarchical;
			set => base.decl = value;
		}

		public readonly Dictionary<string, List<ScopeLeaf>>
			children = new Dictionary<string, List<ScopeLeaf>>();

		// unresolved???
		public List<Scope> importedScopes; // (base) Class(es) and (using) Namespaces

		public void AddChild( ScopeLeaf scope )
		{
			Decl child = scope.decl;
			decl?.AddChild( child );
			child.scope = scope;
			AddToDict( scope, child );
		}

		public void AddChild( Scope scope )
		{
			Hierarchical child = scope.decl;
			decl?.AddChild( child );
			child.scope = scope;
			AddToDict( scope, child );
		}

		private void AddToDict( ScopeLeaf scopeLeaf, Decl child )
		{
			List<ScopeLeaf> list;
			if( !children.TryGetValue( child.name, out list ) ) {
				list = new List<ScopeLeaf>( 1 );
				children.Add( child.name, list );
			}
			list.Add( scopeLeaf );
		}
	}
}
