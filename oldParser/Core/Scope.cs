using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLang.Core
{
	/**
	 * <example>
	 * p [
	 *   if
	 *     i [ j ]
	 *   else
	 *     e [ ]
	 * ]
	 * 
	 * p.children = [ i, e ];
	 * i.parent = p;
	 * e.parent = p;
	 * NamedVar j;
	 * i.vars = [ j ];
	 * </example>
	 **/
	class MyScope
	{
		MyScope			parent	 = null; // null if root scope
		ISet<MyScope>	children = new HashSet<MyScope>();
		MyFields		vars	 = new MyFields();	// TODO: a different type must be inserted here

		public void Add( MyScope child ) {
			child.parent = this;
			children.Add( child );
		}

		public MyField Find( string var_name ) {
			MyField x = vars.list.Find( q => q.name == var_name );
			if( x != null )
				return x;

			if( parent != null )
			{
				x = parent.Find( var_name );
				if( x != null )
					return x;
			}

			// find in class / namespace

			return null;
		}
	}
}