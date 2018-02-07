using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Myll.Core;

namespace Myll
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

		public MyExpr VisitExpr(MyParser.ExprContext context)
		{
			MyExpr ex = new MyExpr(context.GetText());
			return ex;
		}

		public override object VisitEnumDecl(MyParser.EnumDeclContext context)
		{
			MyEnum o = new MyEnum();
			o.Name = new MyIdDef(context.id().GetText());
			o.Entries = new List<MyEnumEntry>();
			// add to hierarchy stack
			foreach (MyParser.IdExprContext e in context.idExpr())
			{
				MyEnumEntry ent = new MyEnumEntry();
				ent.Key = new MyIdDef(e.id().GetText());
				MyParser.ExprContext assign = e.expr();
				if (!assign.IsEmpty)
					ent.Value = VisitExpr(assign);
				o.Entries.Add(ent);
			}
			
			return base.VisitEnumDecl(context);
		}

		private void Visit(TerminalNodeImpl node)
		{
			Console.WriteLine(" Visit Symbol={0}", node.Symbol.Text);
		}
	}
}
