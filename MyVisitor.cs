using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public new Typespec VisitTypeSpec(MyParser.TypeSpecContext c)
		{
			Typespec ret = null;
			if (c.basicType()		!= null) ret = new TypespecBase();
			else if (c.funcType()	!= null) ret = VisitFuncType(c.funcType());
			else if (c.nestedType()	!= null) ret = VisitNestedType(c.nestedType());
			else Debug.Assert(false);
			return ret;
		}

		public new Typespec VisitBasicType(MyParser.BasicTypeContext c)
		{
			TypespecBase ret = new TypespecBase
				{ };
			return ret;
		}

		public new Typespec VisitFuncType(MyParser.FuncTypeContext c)
		{
			TypespecFunc ret = new TypespecFunc
			{
				templateArgs = VisitTplArgs(c.tplArgs()),
				paras        = VisitFuncDef(c.funcDef()),
				retType      = VisitTypeSpec(c.typeSpec()),
			};
			return ret;
		}

		public new List<Param> VisitFuncDef(MyParser.FuncDefContext c)
		{
			List<Param> ret = c.param().Select(q =>
			{
				Param p = new Param
				{
					name = q.id().GetText(),
					type = VisitTypeSpec(q.typeSpec())
				};
				return p;
			}).ToList();
			return ret;
		}

		public new Typespec VisitNestedType(MyParser.NestedTypeContext c)
		{
			TypespecNested ret = new TypespecNested
			{
				identifiers = c.idTplArgs().Select(VisitIdTplArgs).ToList()
			};
			return ret;
		}

		public new IdentifierTpl VisitIdTplArgs(MyParser.IdTplArgsContext c)
		{
			IdentifierTpl ret = new IdentifierTpl
			{
				name        = c.id().GetText(),
				templateArg = VisitTplArgs(c.tplArgs())
			};
			return ret;
		}

		public new List<TemplateArg> VisitTplArgs(MyParser.TplArgsContext c)
		{
			List<TemplateArg> ret = c?.tplArg().Select(q =>
			{
				TemplateArg ta = null;		
				if (q.typeSpec()         != null) ta = new TemplateArg {type    = VisitTypeSpec(q.typeSpec())};
				else if (q.INTEGER_LIT() != null) ta = new TemplateArg {literal = q.INTEGER_LIT().ToString()};
				else Debug.Assert(false);
				return ta;
			}).ToList();
			return ret;
		}

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
