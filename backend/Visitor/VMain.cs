using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Myll.Core;

using Parser = Myll.MyllParser;

namespace Myll
{
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{
		protected readonly Stack<Scope> ScopeStack;

		public ExtendedVisitor( Stack<Scope> ScopeStack )
		{
			this.ScopeStack = ScopeStack;
		}

		public GlobalNamespace GenerateGlobalScope()
		{
			GlobalNamespace global = new GlobalNamespace {
				name     = "",   // global
				srcPos   = null, // no pos since it exists for multiple files
				withBody = true,
				access   = Access.Public,
				imps     = new HashSet<string>(),
			};
			Scope scope = new Scope {
				parent = null,
				decl   = global,
			};
			ScopeStack.Push( scope );
			return global;
		}

		public void CleanBodylessNamespace()
		{
			// TODO: This needs to be mentioned in the THESIS, unreadable SHIT!
			while( !((Namespace) ScopeStack.Peek().decl).withBody )
				PopScope();
		}

		public void CloseGlobalScope()
		{
			//CleanBodylessNamespace();

			PopScope();

			if( ScopeStack.Count != 0 )
				throw new Exception( "ScopeStack was not empty" );
		}

		public void AddChild( Decl leaf )
		{
			Scope parent = ScopeStack.Peek();
			ScopeLeaf scopeLeaf = new ScopeLeaf {
				parent = parent,
				decl   = leaf,
			};
			parent.AddChild( scopeLeaf );
		}

		public void AddChildren( IEnumerable<Decl> leafs )
		{
			Scope parent = ScopeStack.Peek();
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
			Scope parent = ScopeStack.Peek();
			Scope scope = new Scope {
				parent = parent,
				decl   = hierarchical,
			};
			parent.AddChild( scope );
			ScopeStack.Push( scope );
		}

		// pushing a scope which can't be addressed from the outside
		public void PushScope()
		{
			Scope parent = ScopeStack.Peek();
			Scope scope = new Scope {
				parent = parent,
				decl   = null,
			};
			ScopeStack.Push( scope );
		}

		public void PopScope()
		{
			ScopeStack.Pop();
		}
	}
}
