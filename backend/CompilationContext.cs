using System.Collections.Generic;
using Myll.Core;
using Myll.Resolver;

namespace Myll
{
	/// <summary>
	/// Owns the visitor instances and scope stack for one module compilation.
	/// This replaces the static visitor state that used to live in VisitorExtensions.
	/// </summary>
	public sealed class CompilationContext
	{
		public Stack<Scope> ScopeStack { get; } = new();

		public bool IsPrototypeFile { get; init; }

		public ExprVisitor ExprVisitor { get; }

		public StmtVisitor StmtVisitor { get; }

		public DeclVisitor DeclVisitor { get; }

		public List<UnresolvedId>             UnresolvedIds             { get; } = new();
		public List<UnresolvedType>           UnresolvedTypes           { get; } = new();
		public List<UnresolvedScoped>         UnresolvedScopeds         { get; } = new();
		public List<UnresolvedUsing>          UnresolvedUsings          { get; } = new();
		public List<UnresolvedMemberAccess>   UnresolvedMemberAccesses  { get; } = new();

		/// <summary>
		/// Callee expressions used in function/constructor calls. The resolver uses this to prefer
		/// constructors or classes over non-callable declarations at call sites.
		/// </summary>
		public HashSet<Expr> FuncCallCallees { get; } = new();

		public CompilationContext()
		{
			ExprVisitor = new ExprVisitor( this );
			StmtVisitor = new StmtVisitor( this );
			DeclVisitor = new DeclVisitor( this );
		}
	}
}
