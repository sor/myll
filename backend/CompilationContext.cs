using System;
using System.Collections.Generic;
using System.Threading;
using Myll.Core;

namespace Myll
{
	/// <summary>
	/// Owns the visitor instances and scope stack for one module compilation.
	/// This is the first step toward removing the static visitor state in VisitorExtensions.
	/// The ThreadLocal "current context" bridge is temporary and exists only so existing
	/// extension methods such as `c.expr().Visit()` can keep working while the refactor proceeds.
	/// </summary>
	public sealed class CompilationContext
	{
		// HACK: temporary bridge until all call sites pass a context explicitly.
		// See plan/semantic-analysis.md and docs/analysis/01-architecture.md.
		private static readonly ThreadLocal<CompilationContext?> current = new();

		private static readonly CompilationContext defaultContext = new();

		public static CompilationContext Current
			=> current.Value ?? defaultContext;

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

		/// <summary>
		/// Makes this context active on the current thread for the duration of the action.
		/// Replaces the previous active context, if any.
		/// </summary>
		public static void WithActive( CompilationContext context, Action action )
		{
			CompilationContext? previous = current.Value;
			current.Value = context;
			try {
				action();
			}
			finally {
				current.Value = previous;
			}
		}

		/// <summary>
		/// Makes this context active on the current thread for the duration of the action and returns the result.
		/// </summary>
		public static TResult WithActive<TResult>( CompilationContext context, Func<TResult> action )
		{
			CompilationContext? previous = current.Value;
			current.Value = context;
			try {
				return action();
			}
			finally {
				current.Value = previous;
			}
		}
	}
}
