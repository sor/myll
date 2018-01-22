using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Tree;

namespace myll
{
	public class MyVisitor : MyParserBaseVisitor<object>
	{
		public override object VisitProg(MyParser.ProgContext context)
		{
			Console.WriteLine("HelloVisitor VisitR");
			context
				.children
				.OfType<TerminalNodeImpl>()
				.ToList()
				.ForEach(child => Visit(child));
			return null;
		}

		private void Visit(TerminalNodeImpl node)
		{
			Console.WriteLine(" Visit Symbol={0}", node.Symbol.Text);
		}
	}
}
