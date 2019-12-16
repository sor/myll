using System.Collections.Generic;

namespace Myll.Core
{
	// basically any kind of declaration
	// things which introduce a name
	public class Symbol
	{
		public enum Kind
		{
			Namespace,
			Type,        // int, u32, bool, ...
			Structural,  // struct, class, union
			Enumeration, // enum
			Variable,    // var,  const
			Function,    // func, op
			InstanceVar, // class::var,  class::const
			Method,      // class::func, class::op
		}

		public const int UNRESOLVED = -2;
		public const int RESOLVING  = -1;
		public const int RESOLVED   = 0;

		public int unresolvedCount = UNRESOLVED;

		public bool IsUnResolved => unresolvedCount == UNRESOLVED;
		public bool IsResolving  => unresolvedCount == RESOLVING;
		public bool IsResolved   => unresolvedCount == RESOLVED;

		public Kind         kind;
		public string       name;
		public List<string> tpl;  // only the names, TODO: what about the requires?
		public Decl         impl; // may be decl, hierarchical or null if not loaded

		public List<Symbol> overlays; // using's and parent class'es
		public Symbol       parent;

		public PairList<string, Symbol>  funcParameter = new PairList<string, Symbol>();
		public MultiDict<string, Symbol> children      = new MultiDict<string, Symbol>();
	}

	public class PairList<TKey, TValue> : List<KeyValuePair<TKey, TValue>> {}

	public class MultiDict<TKey, TValue> : Dictionary<TKey, List<TValue>>
	{
		public void Add( TKey key, TValue value )
		{
			List<TValue> list;

			if( !base.TryGetValue( key, out list ) ) {
				list = new List<TValue>( 1 );
				base.Add( key, list );
			}
			list.Add( value );
		}
	}
}
