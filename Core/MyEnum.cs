using System.Collections.Generic;

namespace Myll.Core
{
	public class MyEnumEntry
	{
		public MyIdDef Key;
		public MyExpr  Value;
	}

	public class MyEnum	// is a scope
	{
		public MyIdDef Name;
		public List<MyEnumEntry> Entries = new List<MyEnumEntry>();
		
		public bool Flags;
	}	
}