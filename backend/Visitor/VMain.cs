using System;
using System.Collections.Generic;
using Myll.Core;

using Parser = Myll.MyllParser;

namespace Myll
{
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{
		protected readonly Stack<Scope> scopeStack;

		public ExtendedVisitor( Stack<Scope> scopeStack )
		{
			this.scopeStack = scopeStack;
		}

		public GlobalNamespace GenerateGlobalScope( string module )
		{
			GlobalNamespace global = new GlobalNamespace {
				name     = "",   // global
				srcPos   = null, // no pos since it exists for multiple files
				withBody = true,
				imps     = new HashSet<string>(),
				module   = module,
			};
			Scope scope = new Scope {
				parent = null,
				decl   = global,
			};
			scopeStack.Push( scope );
			return global;
		}

		public void CleanBodylessNamespace()
		{
			// TODO: This needs to be mentioned in the THESIS, unreadable SHIT!
			while( !((Namespace) scopeStack.Peek().decl).withBody )
				PopScope();
		}

		public void CloseGlobalScope()
		{
			PopScope();

			if( scopeStack.Count != 0 )
				throw new Exception( "ScopeStack was not empty" );
		}

		public void AddChild( Decl leaf )
		{
			Scope parent = scopeStack.Peek();
			ScopeLeaf scopeLeaf = new ScopeLeaf {
				parent = parent,
				decl   = leaf,
			};
			parent.AddChild( scopeLeaf );
		}

		public void AddChildren( IEnumerable<Decl> leafs )
		{
			Scope parent = scopeStack.Peek();
			foreach( Decl leaf in leafs ) {
				ScopeLeaf scopeLeaf = new ScopeLeaf {
					parent = parent,
					decl   = leaf,
				};
				parent.AddChild( scopeLeaf );
			}
		}

		public void PushScope( Hierarchical hierarchical )
		{
			Scope parent = scopeStack.Peek();
			Scope scope = new Scope {
				parent = parent,
				decl   = hierarchical,
			};
			parent.AddChild( scope );
			scopeStack.Push( scope );
		}

		// pushing a scope which can't be addressed from the outside
		public void PushScope()
		{
			Scope parent = scopeStack.Peek();
			Scope scope = new Scope {
				parent = parent,
				decl   = null,
			};
			scopeStack.Push( scope );
		}

		public void PopScope()
		{
			scopeStack.Pop();
		}
	}
}
