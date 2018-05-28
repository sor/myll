using System.Collections.Generic;

namespace Myll.Core
{
	public class Symbol
	{
		public enum Kind
		{
			Namespace,
			Type,
			Var,
			Func,
		}

		public string       name;
		public Kind         kind;
		public List<string> tpl;
		public List<Symbol> children;
		public List<Symbol> overlays;
		public Decl         impl; // may be null if not loaded
	}
}
