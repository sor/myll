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
	public partial class Visitor : MyllParserBaseVisitor<object>
	{
		public Enum.Entry VisitEnumEntry(IdExprContext c)
		{
			Enum.Entry ret = new Enum.Entry
			{
				name  = VisitId(c.id()),
				value = c.expr().Visit(),
			};
			return ret;
		}

		public override object VisitEnumDecl(EnumDeclContext c)
		{
			// add to hierarchy stack
			Enum ret = new Enum
			{
				name    = VisitId(c.id()),
				entries = c.idExpr().Select(VisitEnumEntry).ToList(),
			};
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
			FuncDeclContext cc = c.funcDecl();
			Func ret = new Func
			{
				name           = VisitId(cc.id()),
				templateParams = VisitTplParams(cc.tplParams()),
				paras          = VisitFuncDef(cc.funcDef()),
				retType        = VisitTypeSpec(cc.typeSpec()),
			};

			if (cc.stmt() != null)
			{
				ret.block = cc.stmt().Visit();
			}
			else if (cc.expr() != null)
			{
				cc.expr().Visit();
				// TODO
				ret.block = new Block();
			}
			return ret;
		}
	}
}
