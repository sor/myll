using System.Collections.Generic;
using System.Linq.Expressions;

namespace Myll.Core
{
	public class SrcPos
	{
		public struct LineCol
		{
			public int line;
			public int col;
		}

		public string  file;
		public LineCol from;
		public LineCol to;
	}

	public class IdDef
	{
		public string name;
	}

	public class IdRef
	{
		public string name;
	}
}
