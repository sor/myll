using System;
using System.Collections.Generic;

namespace Myll.Core
{
	// basically any kind of declaration
	// things which introduce a name
	[Obsolete( "not used ATM, properly check before using" )]
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

		public const int Unresolved = -2;
		public const int Resolving  = -1;
		public const int Resolved   = 0;

		public int unresolvedCount = Unresolved;

		public bool IsUnResolved => unresolvedCount == Unresolved;
		public bool IsResolving  => unresolvedCount == Resolving;
		public bool IsResolved   => unresolvedCount == Resolved;

		public Kind         kind;
		public string       name;
		public List<string> tpl;  // only the names, TODO: what about the requires?
		public Decl         impl; // may be decl, hierarchical or null if not loaded

		public List<Symbol> overlays; // using's and parent class'es
		public Symbol       parent;

		public PairList<string, Symbol>  funcParameter = new();
		public MultiDict<string, Symbol> children      = new();
	}

	[Obsolete( "not used ATM, properly check before using" )]
	public class PairList<TKey, TValue> : List<KeyValuePair<TKey, TValue>> {}

	[Obsolete( "not used ATM, properly check before using" )]
	public class MultiDict<TKey, TValue> : Dictionary<TKey, List<TValue>>
	{
		public void Add( TKey key, TValue value )
		{
			List<TValue> list;

			if( !TryGetValue( key, out list ) ) {
				list = new List<TValue>( 1 );
				base.Add( key, list );
			}
			list.Add( value );
		}
	}
}
