using System.Collections.Generic;
using Myll.Core;

namespace Myll
{
	/// <summary>
	/// Owns the visitor instances and scope stack for one module compilation.
	/// This replaces the static visitor state that used to live in VisitorExtensions.
	/// </summary>
	public sealed class CompilationContext
	{
		public Stack<Scope> ScopeStack { get; } = new();

		public ExprVisitor ExprVisitor { get; }

		public StmtVisitor StmtVisitor { get; }

		public DeclVisitor DeclVisitor { get; }

		public CompilationContext()
		{
			ExprVisitor = new ExprVisitor( this );
			StmtVisitor = new StmtVisitor( this );
			DeclVisitor = new DeclVisitor( this );
		}
	}
}
