using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Myll.Core;

using Array = Myll.Core.Array;
using Enum = Myll.Core.Enum;

using static Myll.MyllParser;

namespace Myll
{
	public partial class MyllVisitor : MyllParserBaseVisitor<object>
	{
		public new Func.Param VisitParam(ParamContext c)
		{
			Func.Param ret = new Func.Param
			{
				name = VisitId(c.id()),
				type = VisitTypeSpec(c.typeSpec())
			};
			return ret;
		}

		public new List<Func.Param> VisitFuncDef(FuncDefContext c)
		{
			List<Func.Param> ret = c.param().Select(VisitParam).ToList();
			return ret;
		}

		public new IdentifierTpl VisitIdTplArgs(IdTplArgsContext c)
		{
			IdentifierTpl ret = new IdentifierTpl
			{
				name         = VisitId(c.id()),
				templateArgs = VisitTplArgs(c.tplArgs())
			};
			return ret;
		}

		public new TemplateArg VisitTplArg(TplArgContext c)
		{
			TemplateArg ret;
			if (c.typeSpec()  != null) ret = new TemplateArg {type = VisitTypeSpec(c.typeSpec())};
			else if (c.id()   != null) ret = new TemplateArg {name = VisitId(c.id())};
			else if (c.expr() != null) ret = new TemplateArg {expr = VisitExpr(c.expr())};
			else throw new Exception("unknown template arg kind");
			return ret;
		}

		public new List<TemplateArg> VisitTplArgs(TplArgsContext c)
		{
			List<TemplateArg> ret = c?.tplArg().Select(VisitTplArg).ToList();
			return ret;
		}

		public Expr VisitExpr(ExprContext c)
		{
			if (c == null)
				return null;
			
			Expr ex = new Expr();
			return ex;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new string VisitId(IdContext c)
		{
			return c.GetText();
		}

		public Enum.Entry VisitEnumEntry(IdExprContext c)
		{
			Enum.Entry ret = new Enum.Entry
			{
				name  = VisitId(c.id()),
				value = VisitExpr(c.expr()),
			};
			return ret;
		}
		
		public override object VisitEnumDecl(EnumDeclContext c)
		{
			Enum ret = new Enum();
			ret.name = VisitId(c.id());
			// add to hierarchy stack
			ret.entries = c.idExpr().Select(VisitEnumEntry).ToList();
			return ret;
		}

		public TemplateParam VisitTplParam(IdContext c)
		{
			TemplateParam tp = new TemplateParam
			{
				name = VisitId(c)
			};
			return tp;
		}
		
		public new List<TemplateParam> VisitTplParams(TplParamsContext c)
		{
			List<TemplateParam> ret = c.id().Select(VisitTplParam).ToList();
			return ret;
		}

		public Stmt VisitStmt(StmtContext c)
		{
			Visit(c);
			// TODO
			return new Stmt();
		}
		
		public new List<Stmt> VisitStmtBlk(StmtBlkContext context)
		{
			List<Stmt> ret = context.stmt().Select(VisitStmt).ToList();
			return ret;
		}

		public override object VisitFunctionDecl(FunctionDeclContext c)
		{
			FuncDeclContext cc = c.funcDecl();
			Func ret = new Func
			{
				name           = VisitId(cc.id()),
				templateParams = VisitTplParams(cc.tplParams()),
				paras          = VisitFuncDef(cc.funcDef()),
				retType        = VisitTypeSpec(cc.typeSpec()),
			};
			if (cc.stmtBlk() != null)
			{
				ret.block = VisitStmtBlk(cc.stmtBlk());
			}
			else if (cc.expr() != null)
			{
				// TODO
				VisitExpr(cc.expr());
				ret.block = new List<Stmt>();
			}
			return ret;
		}

		private void Visit(TerminalNodeImpl node)
		{
			Console.WriteLine(" Visit Symbol={0}", node.Symbol.Text);
		}
		
		public override object VisitProg(ProgContext context)
		{
			Console.WriteLine("HelloVisitor VisitR");
			context
				.children
				.OfType<TerminalNodeImpl>()
				.ToList()
				.ForEach(child => Visit(child));
			return null;
		}
	}
}
