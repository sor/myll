using Myll.Core;

namespace Myll.Resolver
{
	public sealed record UnresolvedId( IdExpr Node, Scope Scope );

	public sealed record UnresolvedType( TypespecNested Node, Scope Scope );

	public sealed record UnresolvedScoped( ScopedExpr Node, Scope Scope );

	public sealed record UnresolvedUsing( UsingDecl Node, Scope Scope );
}
