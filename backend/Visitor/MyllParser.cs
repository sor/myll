using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Myll.Core;

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
					Interval interval  = new( a, b );
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
					Interval interval  = new( a, b );
					String   text      = Start.InputStream.GetText( interval );
					return text;
				}
			}
		}

		public interface IRelEqExprContext
		{
			public ExprContext expr( int i );
			public Operand     Op { get; }
		}

		public partial class RelationExprContext : ExprContext, IRelEqExprContext
		{
			public Operand Op => relOP().v.ToOp();
		}

		public partial class EqualityExprContext : ExprContext, IRelEqExprContext
		{
			public Operand Op => equalOP().v.ToOp();
		}
	}
}
