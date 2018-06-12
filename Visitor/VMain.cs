using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Myll.Core;

using Parser = Myll.MyllParser;

namespace Myll
{
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{
		public static Stack<Scope> ScopeStack = new Stack<Scope>();

		public static void AddChild( Decl leaf )
		{
			Scope parent = ScopeStack.Peek();
			ScopeLeaf scopeLeaf = new ScopeLeaf {
				parent = parent,
				decl   = leaf,
			};
			parent.AddChild( scopeLeaf );
		}

		public static void PushScope( Hierarchical hierarchical )
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
		public static void PushScope()
		{
			Scope parent = ScopeStack.Peek();
			Scope scope = new Scope {
				parent = parent,
				decl   = null,
			};
			ScopeStack.Push( scope );
		}

		public static void PopScope()
		{
			ScopeStack.Pop();
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public new string VisitId( Parser.IdContext c )
			=> c.GetText();
	}
}
