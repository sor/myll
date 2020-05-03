using System.Collections.Generic;

namespace Myll.Core
{
	public class ScopeLeaf
	{
		public Scope parent; // only global scope has this set to null
		public Decl  decl;

		public bool HasDecl => decl != null;
	}

	// has fast index-able decls
	// IF hierarchical
	// THEN visible from outside
	// ELSE NOT visible from outside
	public class Scope : ScopeLeaf
	{
		public Scope UpToGlobal    => parent?.UpToGlobal ?? this;
		public Scope UpToNamespace => decl is Namespace ? this : parent.UpToNamespace;

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
			if( !children.TryGetValue( child.name, out List<ScopeLeaf> list ) ) {
				list = new List<ScopeLeaf>( 1 );
				children.Add( child.name, list );
			}
			list.Add( scopeLeaf );
		}
	}
}
