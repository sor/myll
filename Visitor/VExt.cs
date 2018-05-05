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
using Parser = Myll.MyllParser;

//using static Myll.MyllParser;

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
				{Parser.DBL_PLUS,	Operand.PostIncr},
				{Parser.DBL_MINUS,	Operand.PostDecr},
				{Parser.DOT,		Operand.MemberAccess},
				{Parser.DOT_STAR,	Operand.MemberAccessPtr},
				{Parser.RARROW,		Operand.MemberPtrAccess},
				{Parser.ARROW_STAR,	Operand.MemberPtrAccessPtr},
			//	{Parser.DBL_STAR,	Operand.Pow},
				{Parser.STAR,		Operand.Multiply},
				{Parser.SLASH,		Operand.Divide},
				{Parser.MOD,		Operand.Modulo},
				{Parser.PLUS,		Operand.Addition},
				{Parser.MINUS,		Operand.Subtraction},
				{Parser.AMP,		Operand.BitAnd},
				{Parser.HAT,		Operand.BitXor},
				{Parser.COMPARE,	Operand.Comparison},
				{Parser.LT,			Operand.LessThan},
				{Parser.LTEQ,		Operand.LessEqual},
				{Parser.GT,			Operand.GreaterThan},
				{Parser.GTEQ,		Operand.GreaterEqual},
				{Parser.EQ,			Operand.Equal},
				{Parser.NEQ,		Operand.NotEqual},
				{Parser.DBL_AMP,	Operand.And},
				{Parser.DBL_PIPE,	Operand.Or},
			};

		private static readonly Dictionary<int, Operand>
			PreToOperand = new Dictionary<int, Operand>
			{
				{Parser.DBL_PLUS,	Operand.PreIncr},
				{Parser.DBL_MINUS,	Operand.PreDecr},
				{Parser.PLUS,		Operand.PrePlus},
				{Parser.MINUS,		Operand.PreMinus},
				{Parser.EXCL,		Operand.Negation},
				{Parser.TILDE,		Operand.Complement},
				{Parser.STAR,		Operand.Dereference},
				{Parser.AMP,		Operand.AddressOf},
			};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Operand ToOp(this IToken tok)
			=> ToOperand[tok.Type];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Operand PreToOp(this IToken tok)
			=> ToOperand[tok.Type];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Expr Visit(this Parser.ExprContext c)
		{
			if (c == null)
				return null;

			return exprVis.Visit(c);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Stmt Visit(this Parser.StmtContext c)
		{
			if (c == null)
				return null;

			return stmtVis.Visit(c);
		}
	}
}