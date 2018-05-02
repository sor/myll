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
		
		public override object VisitFunctionDecl(FunctionDeclContext c)
		{
			FuncDeclContext cc  = c.funcDecl();
			Func            ret = new Func
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
				VisitExpr(cc.expr());
				// TODO
				ret.block = new List<Stmt>();
			}
			return ret;
		}
	}
}
