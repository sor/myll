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
		private readonly Stack<Scope> ScopeStack;

		public ExtendedVisitor(Stack<Scope> ScopeStack)
		{
			this.ScopeStack = ScopeStack;
		}

		public Namespace GenerateGlobalScope(SrcPos srcPos)
		{
			Namespace global = new Namespace {
				name     = "", // global
				srcPos   = srcPos,
				withBody = true,
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
			CleanBodylessNamespace();

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

		// this will become more specialized most likely, don't depend on current behavior
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public new string VisitId( Parser.IdContext c )
			=> c.GetText();
	}
}
