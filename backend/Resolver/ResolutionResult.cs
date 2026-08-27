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
		private readonly Dictionary<IdExpr, Decl>          ids   = new();
		private readonly Dictionary<TypespecNested, Decl>  types = new();

		public IReadOnlyDictionary<IdExpr, Decl>          Ids   => ids;
		public IReadOnlyDictionary<TypespecNested, Decl>  Types => types;

		public void Resolve( IdExpr id, Decl decl )
			=> ids[id] = decl;

		public void Resolve( TypespecNested type, Decl decl )
			=> types[type] = decl;

		public bool TryGetResolved( IdExpr id, out Decl? decl )
			=> ids.TryGetValue( id, out decl );

		public bool TryGetResolved( TypespecNested type, out Decl? decl )
			=> types.TryGetValue( type, out decl );
	}
}
