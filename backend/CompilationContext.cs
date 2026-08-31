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

		public List<Diagnostic> Diagnostics { get; } = new();

		public ExprVisitor ExprVisitor { get; }

		public StmtVisitor StmtVisitor { get; }

		public DeclVisitor DeclVisitor { get; }

		public List<UnresolvedId>             UnresolvedIds             { get; } = new();
		public List<UnresolvedType>           UnresolvedTypes           { get; } = new();
		public List<UnresolvedScoped>         UnresolvedScopeds         { get; } = new();
		public List<UnresolvedUsing>          UnresolvedUsings          { get; } = new();
		public List<UnresolvedAlias>          UnresolvedAliases         { get; } = new();
		public List<UnresolvedMemberAccess>   UnresolvedMemberAccesses  { get; } = new();

		/// <summary>
		/// Callee expressions used in function/constructor calls. The resolver uses this to prefer
		/// constructors or classes over non-callable declarations at call sites.
		/// </summary>
		public HashSet<Expr> FuncCallCallees { get; } = new();

		/// <summary>
		/// Function/constructor call sites together with their callee expression and argument list.
		/// Used by overload resolution to pick the right overload once argument types are known.
		/// </summary>
		public List<UnresolvedCall> UnresolvedCalls { get; } = new();

		/// <summary>
		/// Source positions of every use of the `float` keyword in this module. The resolver uses
		/// this together with <see cref="Dialect.DefaultFloat"/> to emit diagnostics.
		/// </summary>
		public List<SrcPos> FloatKeywordUsages { get; } = new();

		/// <summary>
		/// Local-scoped declarations (parameters, local variables, catch variables, times indices,
		/// lambda parameters) together with the scope they were declared in. Used by the shadowing
		/// checker, since ephemeral block scopes are not part of the persistent hierarchical scope
		/// tree.
		/// </summary>
		public List<(Decl Local, Scope Scope)> LocalDecls { get; } = new();

		private int tempNameCounter;

		/// <summary>
		/// Returns a fresh compiler-generated identifier starting with "myll_tmp_".
		/// Used for anonymous loop indices, discard temporaries, and other synthesized names.
		/// The "myll_tmp_" prefix is reserved for the compiler.
		/// </summary>
		public string NextTempName()
			=> "myll_tmp_" + tempNameCounter++;

		public CompilationContext()
		{
			ExprVisitor = new ExprVisitor( this );
			StmtVisitor = new StmtVisitor( this );
			DeclVisitor = new DeclVisitor( this );
		}
	}
}
