using Myll.Core;

namespace Myll.Resolver
{
	public sealed record UnresolvedId( IdExpr Node, Scope Scope );

	public sealed record UnresolvedType( TypespecNested Node, Scope Scope );

	public sealed record UnresolvedScoped( ScopedExpr Node, Scope Scope );

	public sealed record UnresolvedUsing( UsingDecl Node, Scope Scope );

	public sealed record UnresolvedUsingStmt( UsingStmt Node, Scope Scope );

	public sealed record UnresolvedAlias( AliasDecl Node, Scope Scope );

	public sealed record UnresolvedMemberAccess( BinOp Node, Scope Scope );

	// Synthesized constructor calls (direct construction) carry an unregistered
	// callee whose template arguments belong to the class, not to any function:
	// they route to CTAD when bare, and skip validation when explicit.
	public sealed record UnresolvedCall( Expr Callee, FuncCall Call, Scope Scope, bool IsSynthesizedCtorCall = false );
}
