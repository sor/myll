using System.Collections.Generic;
using Myll.Core;

namespace Myll.Resolver
{
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

		public IReadOnlyDictionary<IdExpr, Decl>          Ids      => ids;
		public IReadOnlyDictionary<TypespecNested, Decl>  Types    => types;
		public IReadOnlyDictionary<ScopedExpr, Decl>      Scopeds  => scopeds;
		public IReadOnlyDictionary<IdExpr, Decl>          Members  => members;

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

		public bool IsResolvedOrInvalid( IdExpr id )
			=> ids.ContainsKey( id ) || invalidIds.ContainsKey( id );

		public bool IsResolvedOrInvalid( ScopedExpr scoped )
			=> scopeds.ContainsKey( scoped ) || invalidScopeds.ContainsKey( scoped );

		public bool IsResolvedOrInvalid( Expr expr )
			=> expr switch {
				IdExpr     id     => IsResolvedOrInvalid( id ),
				ScopedExpr scoped => IsResolvedOrInvalid( scoped ),
				_                 => false,
			};

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
