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
	public static class VisitorExtensions
	{
		private	static readonly ExprVisitor exprVis = new ExprVisitor();
		private	static readonly StmtVisitor stmtVis = new StmtVisitor();
		public	static readonly Visitor     allVis  = new Visitor();

		private static readonly Dictionary<int, Operand>
			ToOperand = new Dictionary<int, Operand>
			{
				{MyllParser.DBL_PLUS,	Operand.PostIncr},
				{MyllParser.DBL_MINUS,	Operand.PostDecr},
				{MyllParser.DOT,		Operand.Pow},
				{MyllParser.DOT_STAR,	Operand.Pow},
				{MyllParser.RARROW,		Operand.Pow},
				{MyllParser.ARROW_STAR,	Operand.Pow},
			//	{MyllParser.DBL_STAR,	Operand.Pow},
				{MyllParser.STAR,	Operand.Multiply},
				{MyllParser.SLASH,	Operand.Divide},
				{MyllParser.MOD,	Operand.Modulo},
				{MyllParser.PLUS,	Operand.Addition},
				{MyllParser.MINUS,	Operand.Subtraction},
				{MyllParser.AMP,	Operand.BitAnd},
				{MyllParser.HAT,	Operand.BitXor},
				{MyllParser.COMPARE,Operand.Comparison},
				{MyllParser.LT,		Operand.LessThan},
				{MyllParser.LTEQ,	Operand.LessEqual},
				{MyllParser.GT,		Operand.GreaterThan},
				{MyllParser.GTEQ,	Operand.GreaterEqual},
				{MyllParser.EQ,		Operand.Equal},
				{MyllParser.NEQ,	Operand.NotEqual},
				{MyllParser.DBL_AMP,	Operand.And},
				{MyllParser.DBL_PIPE,	Operand.Or},
			};

		private static readonly Dictionary<int, Operand>
			PreToOperand = new Dictionary<int, Operand>
			{
				{MyllParser.DBL_PLUS,	Operand.PreIncr},
				{MyllParser.DBL_MINUS,	Operand.PreDecr},
				{MyllParser.PLUS,	Operand.PrePlus},
				{MyllParser.MINUS,	Operand.PreMinus},
				{MyllParser.EXCL,	Operand.Negation},
				{MyllParser.TILDE,	Operand.Complement},
				{MyllParser.STAR,	Operand.Dereference},
				{MyllParser.AMP,	Operand.AddressOf},
			};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Operand ToOp(this IToken tok)
			=> ToOperand[tok.Type];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Operand PreToOp(this IToken tok)
			=> ToOperand[tok.Type];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Expr Visit(this ExprContext c)
		{
			if (c == null)
				return null;

			return exprVis.Visit(c);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Stmt Visit(this StmtContext c)
		{
			if (c == null)
				return null;

			return stmtVis.Visit(c);
		}
	}

	public partial class Visitor : MyllParserBaseVisitor<object>
	{
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
			else if (c.expr() != null) ret = new TemplateArg {expr = c.expr().Visit()};
			else throw new Exception("unknown template arg kind");
			return ret;
		}

		public new List<TemplateArg> VisitTplArgs(TplArgsContext c)
		{
			List<TemplateArg> ret = c?.tplArg().Select(VisitTplArg).ToList();
			return ret;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new string VisitId(IdContext c)
		{
			return c.GetText();
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
