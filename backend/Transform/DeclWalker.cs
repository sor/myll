using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Transform
{
	/// <summary>
	/// Shared AST traversal for transforms that need every body of every declaration,
	/// including lambda bodies reachable from expressions.
	///
	/// The default implementations recurse generically. Overrides that take over a
	/// statement kind must route nested statements through <see cref="WalkStmt"/>
	/// themselves, which keeps every node visited exactly once.
	/// </summary>
	public abstract class DeclWalker
	{
		public void WalkModule( GlobalNamespace module ) => TransformDecl( module );

		protected virtual void TransformDecl( Decl decl )
		{
			switch( decl ) {
				case Func func:
					WalkBody( func );
					break;
				case Structor stc:
					WalkBody( stc );
					break;
				case Hierarchical h:
					// Snapshot: transforms may synthesize declarations into the
					// iterated list (e.g. enum operators).
					foreach( Decl child in h.children.ToList() )
						TransformDecl( child );
					break;
			}
		}

		/// <summary>
		/// Walks a function body; lambda bodies are reached through this hook too, so
		/// overrides that reset per-body state also isolate lambdas.
		/// </summary>
		protected virtual void WalkBody( Func func )
		{
			Func?     prevOwner = CurrentOwner;
			bool      prevInMember = InMemberContext;
			CurrentOwner    = func;
			InMemberContext = func.kind == Func.Kind.Method && !func.IsStatic;
			if( func.body != null )
				func.body = WalkBlock( func.body );
			CurrentOwner    = prevOwner;
			InMemberContext = prevInMember;
		}

		protected virtual void WalkBody( Structor stc )
		{
			Func?     prevOwner = CurrentOwner;
			bool      prevInMember = InMemberContext;
			CurrentOwner    = null;
			InMemberContext = true;
			if( stc.body != null )
				stc.body = WalkBlock( stc.body );
			CurrentOwner    = prevOwner;
			InMemberContext = prevInMember;
		}

		/// <summary>
		/// The innermost function or lambda whose body is currently being walked.
		/// </summary>
		protected Func? CurrentOwner { get; private set; }

		/// <summary>
		/// True while walking the body of a non-static member function, constructor/
		/// destructor, or method lambda; i.e. where <c>this</c> exists.
		/// </summary>
		protected bool InMemberContext { get; private set; }

		protected Stmt WalkStmt( Stmt stmt )
			=> stmt is MultiStmt block
				? WalkBlock( block )
				: TransformStmt( stmt );

		protected MultiStmt WalkBlock( MultiStmt block ) => TransformBlock( block );

		/// <summary>
		/// Block hook: called for every statement block. The default walks each
		/// statement in order.
		/// </summary>
		protected virtual MultiStmt TransformBlock( MultiStmt block )
		{
			block.stmts = block.stmts.Select( WalkStmt ).ToList();
			return block;
		}

		/// <summary>
		/// Statement hook: called for every non-block statement. The default recurses
		/// into child statements and child expressions.
		/// </summary>
		protected virtual Stmt TransformStmt( Stmt stmt )
		{
			stmt.ReplaceChildStmts( WalkStmt );
			stmt.VisitChildExprs( WalkExpr );
			return stmt;
		}

		/// <summary>
		/// Expression hook: called for every expression reached from a statement. The
		/// default walks lambda bodies and recurses into sub-expressions.
		/// </summary>
		protected virtual void TransformExpr( Expr expr )
		{
			if( expr is Lambda lambda ) {
				WalkBody( lambda.func );
				return;
			}
			expr.VisitSubExprs( WalkExpr );
		}

		protected void WalkExpr( Expr expr ) => TransformExpr( expr );
	}
}
