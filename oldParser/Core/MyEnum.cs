using System;
using System.Collections.Generic;
using System.Linq;

namespace MyLang.Core
{
	class MyEnum : MyHierarchic
	{
		Library.Structural lib;
		public override Library.Hierarchic LibHier { get { return lib; } }

		public List<KeyValuePair<string, MyExpression>> entries
		 = new List<KeyValuePair<string, MyExpression>>();

		private bool	AutoIndexed		= true;
		private bool	ManualIndexed	= true;
		private int		LongestKey		= 0;

		public MyEnum( string name, MyHierarchic parent )
			: base( name, parent ) { }

		// value is string since you can pass more than just numbers
		public void Add( string k, MyExpression v )
		{
			entries.Add( new KeyValuePair<string, MyExpression>( k, v ) );
			LongestKey = Math.Max( LongestKey, k.Length );
			AutoIndexed = false;
		}

		public void Add( string k )
		{
			entries.Add( new KeyValuePair<string, MyExpression>( k, null ) );
			LongestKey = Math.Max( LongestKey, k.Length );
			ManualIndexed = false;
		}
	}
}
