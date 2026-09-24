using System.Collections.Generic;
using Myll.Core;

namespace Myll.Resolver
{
	public enum InvalidCallKind
	{
		Ambiguous,
		NoMatchingOverload,
		TemplateArityMismatchSingle,
		TemplateArityMismatchMultiple,
		TemplateKindMismatchSingle,
		TemplateKindMismatchMultiple,
	}

	public sealed record InvalidCallInfo(
		InvalidCallKind Kind,
		string          Name,
		Decl?           AttemptedDecl,
		int             TemplateArgCount );

	/// <summary>
	/// Holds the outcome of a resolution pass without modifying AST nodes.
	/// Maps unresolved references to their resolved declarations.
	/// </summary>
	public sealed class ResolutionResult
	{
		private readonly Dictionary<IdExpr, Decl>          ids           = new();
		private readonly Dictionary<TypespecNested, Decl>  types         = new();
		private readonly Dictionary<ScopedExpr, Decl>      scopeds       = new();
		private readonly Dictionary<IdExpr, Decl>          members       = new();

		// Nodes whose resolution was attempted but rejected (e.g. wrong template arity).
		// These are not applied to the AST, but must be skipped on later resolver passes
		// and in the unresolved-reporting phase so the original diagnostic is not drowned
		// out by a generic "unresolved identifier" message.
		private readonly Dictionary<IdExpr, Decl?>         invalidIds      = new();
		private readonly Dictionary<ScopedExpr, Decl?>     invalidScopeds  = new();
		private readonly Dictionary<TypespecNested, Decl?> invalidTypes    = new();
		private readonly Dictionary<Expr, InvalidCallInfo> invalidCalls    = new();

		public IReadOnlyDictionary<IdExpr, Decl>          Ids      => ids;
		public IReadOnlyDictionary<TypespecNested, Decl>  Types    => types;
		public IReadOnlyDictionary<ScopedExpr, Decl>      Scopeds  => scopeds;
		public IReadOnlyDictionary<IdExpr, Decl>          Members  => members;

		public IReadOnlyDictionary<TypespecNested, Decl?> InvalidTypes => invalidTypes;
		public IReadOnlyDictionary<Expr, InvalidCallInfo> InvalidCalls => invalidCalls;

		public void Resolve( IdExpr id, Decl decl )
			=> ids[id] = decl;

		public void Resolve( TypespecNested type, Decl decl )
			=> types[type] = decl;

		public void Resolve( ScopedExpr scoped, Decl decl )
			=> scopeds[scoped] = decl;

		public void ResolveMember( IdExpr member, Decl decl )
			=> members[member] = decl;

		public void MarkInvalid( IdExpr id, Decl? attemptedDecl = null )
			=> invalidIds[id] = attemptedDecl;

		public void MarkInvalid( ScopedExpr scoped, Decl? attemptedDecl = null )
			=> invalidScopeds[scoped] = attemptedDecl;

		public bool IsInvalid( IdExpr id )
			=> invalidIds.ContainsKey( id );

		public bool IsInvalid( ScopedExpr scoped )
			=> invalidScopeds.ContainsKey( scoped );

		public bool IsInvalid( TypespecNested type )
			=> invalidTypes.ContainsKey( type );

		public bool IsInvalidCall( Expr callee )
			=> invalidCalls.ContainsKey( callee );

		public bool IsInvalid( Expr expr )
			=> expr switch {
				IdExpr     id     => invalidIds.ContainsKey( id ) || invalidCalls.ContainsKey( id ),
				ScopedExpr scoped => invalidScopeds.ContainsKey( scoped ) || invalidCalls.ContainsKey( scoped ),
				_                 => invalidCalls.ContainsKey( expr ),
			};

		public bool IsResolvedOrInvalid( IdExpr id )
			=> ids.ContainsKey( id )
			|| invalidIds.ContainsKey( id )
			|| invalidCalls.ContainsKey( id );

		public bool IsResolvedOrInvalid( ScopedExpr scoped )
			=> scopeds.ContainsKey( scoped )
			|| invalidScopeds.ContainsKey( scoped )
			|| invalidCalls.ContainsKey( scoped );

		public bool IsResolvedOrInvalid( TypespecNested type )
			=> types.ContainsKey( type ) || invalidTypes.ContainsKey( type );

		public bool IsResolvedOrInvalid( Expr expr )
			=> expr switch {
				IdExpr     id     => IsResolvedOrInvalid( id ),
				ScopedExpr scoped => IsResolvedOrInvalid( scoped ),
				_                 => IsInvalidCall( expr ),
			};

		public void MarkInvalid( TypespecNested type, Decl? attemptedDecl = null )
			=> invalidTypes[type] = attemptedDecl;

		public void MarkInvalidCall( Expr callee, InvalidCallInfo info )
			=> invalidCalls[callee] = info;

		public bool TryGetResolved( IdExpr id, out Decl? decl )
			=> ids.TryGetValue( id, out decl );

		public bool TryGetResolved( TypespecNested type, out Decl? decl )
			=> types.TryGetValue( type, out decl );

		public bool TryGetResolved( ScopedExpr scoped, out Decl? decl )
			=> scopeds.TryGetValue( scoped, out decl );

		public bool TryGetResolvedMember( IdExpr member, out Decl? decl )
			=> members.TryGetValue( member, out decl );

		/// <summary>
		/// Copies the resolved declarations onto the AST nodes so that the
		/// C++ generator can emit fully-qualified names without threading the
		/// resolution map through every Gen() call.
		/// </summary>
		public void Apply()
		{
			foreach( var kvp in ids )      kvp.Key.resolvedDecl      = kvp.Value;
			foreach( var kvp in types )    kvp.Key.resolvedDecl      = kvp.Value;
			foreach( var kvp in scopeds )  kvp.Key.resolvedDecl      = kvp.Value;
			foreach( var kvp in members )  kvp.Key.resolvedDecl      = kvp.Value;
		}
	}
}
