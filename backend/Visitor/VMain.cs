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

		// Temporary bridge; see CompilationContext for details.
		protected readonly CompilationContext? context;

		public ExtendedVisitor( Stack<Scope> scopeStack )
		{
			this.scopeStack = scopeStack;
		}

		public ExtendedVisitor( CompilationContext context )
			: this( context.ScopeStack )
		{
			this.context = context;
		}

		protected CompilationContext Context
			=> context ?? throw new InvalidOperationException(
				"No CompilationContext is available for this visitor." );

		public GlobalNamespace GenerateGlobalScope( string module )
		{
			GlobalNamespace global = new() {
				name     = "", // global, no source position because it spans multiple files
				withBody = true,
				imps     = new HashSet<string>(),
				module   = module,
			};
			Scope scope = new() {
				decl = global,
			};
			global.scope = scope;
			scopeStack.Push( scope );
			return global;
		}

		public void CleanBodylessNamespace()
		{
			// TODO: This needs to be mentioned in the THESIS, unreadable SHIT!
			while( !((Namespace) scopeStack.Peek().decl!).withBody )
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
			if( parent.decl is Namespace ns && ns.IsExternal )
				leaf.IsExternNamespace = true;

			ScopeLeaf scopeLeaf = new() {
				parent = parent,
				decl   = leaf,
			};
			parent.AddChild( scopeLeaf );
		}

		public void AddChildren( IEnumerable<Decl> leafs )
		{
			Scope parent = scopeStack.Peek();
			bool inheritExternal = parent.decl is Namespace ns && ns.IsExternal;
			foreach( Decl leaf in leafs ) {
				if( inheritExternal )
					leaf.IsExternNamespace = true;

				ScopeLeaf scopeLeaf = new() {
					parent = parent,
					decl   = leaf,
				};
				parent.AddChild( scopeLeaf );
			}
		}

		/// <summary>
		/// Adds a declaration to the current scope for resolution purposes without
		/// also adding it to the enclosing hierarchical declaration's child list.
		/// Used for parameters, local variables, and other symbols that should not
		/// be emitted as top-level or field declarations.
		/// </summary>
		public void AddScopeOnly( Decl leaf )
		{
			Scope parent = scopeStack.Peek();
			if( parent.decl is Namespace ns && ns.IsExternal )
				leaf.IsExternNamespace = true;

			ScopeLeaf scopeLeaf = new() {
				parent = parent,
				decl   = leaf,
			};
			leaf.scope = parent;
			if( !parent.children.TryGetValue( leaf.name, out List<ScopeLeaf>? list ) ) {
				list = new List<ScopeLeaf>( 1 );
				parent.children.Add( leaf.name, list );
			}
			list.Add( scopeLeaf );
		}

		protected void AddParamsToScope( List<Param> paras )
		{
			foreach( Param p in paras ) {
				AddScopeOnly( new VarDecl {
					name   = p.name ?? "",
					type   = p.type,
					access = Access.Public,
					kind   = VarDecl.Kind.Var,
				} );
			}
		}

		public void PushScope( Hierarchical hierarchical )
		{
			Scope parent = scopeStack.Peek();
			if( parent.decl is Namespace ns && ns.IsExternal )
				hierarchical.IsExternNamespace = true;

			Scope scope = new() {
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
			Scope scope = new() {
				parent = parent,
			};
			scopeStack.Push( scope );
		}

		public void PopScope()
		{
			scopeStack.Pop();
		}
	}
}
