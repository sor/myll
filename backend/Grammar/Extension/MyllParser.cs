using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Myll
{
	public partial class MyllParser : Parser
	{
		public partial class LevStmtContext
		{
			public string Text {
				get {
					int a = Start.StartIndex,
					    b = Stop.StopIndex;
					if( a > b ) (a, b) = (b, a);
					Interval interval  = new Interval( a, b );
					String   text      = Start.InputStream.GetText( interval );
					return text;
				}
			}
		}

		public partial class LevDeclContext
		{
			public string Text {
				get {
					int a = Start.StartIndex,
					    b = Stop.StopIndex;
					if( a > b ) (a, b) = (b, a);
					Interval interval  = new Interval( a, b );
					String   text      = Start.InputStream.GetText( interval );
					return text;
				}
			}
		}
	}
}
