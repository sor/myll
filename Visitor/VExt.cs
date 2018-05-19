using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;

using Myll.Core;

using Parser = Myll.MyllParser;

namespace Myll
{
	public static class VisitorExtensions
	{
		private static readonly ExprVisitor ExprVis = new ExprVisitor();
		private static readonly StmtVisitor StmtVis = new StmtVisitor();
		public static readonly  Visitor     AllVis  = new Visitor();

		private static readonly Dictionary<int, Operand>
			ToOperand = new Dictionary<int, Operand>
			{
				{Parser.DBL_PLUS,	Operand.PostIncr},
				{Parser.DBL_MINUS,	Operand.PostDecr},
				{Parser.LPAREN,		Operand.FuncCall},
				{Parser.QM_LPAREN,	Operand.NCFuncCall},
				{Parser.LBRACK,		Operand.IndexCall},
				{Parser.QM_LBRACK,	Operand.NCIndexCall},
				{Parser.POINT,		Operand.MemberAccess},
				{Parser.QM_POINT,	Operand.NCMemberAccess},
				{Parser.RARROW,		Operand.MemberPtrAccess},
				{Parser.POINT_STAR,	Operand.MemberAccessPtr},
				{Parser.QM_POINT_STAR,Operand.NCMemberAccessPtr},
				{Parser.ARROW_STAR,	Operand.MemberPtrAccessPtr},
			//	{Parser.DBL_STAR,	Operand.Pow},
				{Parser.STAR,		Operand.Multiply},
				{Parser.SLASH,		Operand.EuclideanDivide},
				{Parser.MOD,		Operand.Modulo},
				{Parser.DOT,		Operand.Dot},
				{Parser.CROSS,		Operand.Cross},
				{Parser.DIV,		Operand.Divide},
				{Parser.PLUS,		Operand.Add},
				{Parser.MINUS,		Operand.Subtract},
				{Parser.AMP,		Operand.BitAnd},
				{Parser.PIPE,		Operand.BitOr},
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
			ToPreOperand = new Dictionary<int, Operand> {
				{Parser.DBL_PLUS,	Operand.PreIncr},
				{Parser.DBL_MINUS,	Operand.PreDecr},
				{Parser.PLUS,		Operand.PrePlus},
				{Parser.MINUS,		Operand.PreMinus},
				{Parser.EM,			Operand.Negation},
				{Parser.TILDE,		Operand.Complement},
				{Parser.STAR,		Operand.Dereference},
				{Parser.AMP,		Operand.AddressOf},
			};

		private static readonly Dictionary<int, Operand>
			ToAssignOperand = new Dictionary<int, Operand>
			{
				{Parser.ASSIGN,		Operand.Equal},
				{Parser.AS_POW,		Operand.Pow},
				{Parser.AS_MUL,		Operand.Multiply},
				{Parser.AS_DIV,		Operand.EuclideanDivide},
				{Parser.AS_MOD,		Operand.Modulo},
				{Parser.DOT,		Operand.Dot},
				{Parser.CROSS,		Operand.Cross},
			//	{Parser.AS_DIV,		Operand.Divide},
				{Parser.AS_ADD,		Operand.Add},
				{Parser.AS_SUB,		Operand.Subtract},
				{Parser.AS_LSH,		Operand.LeftShift},
				{Parser.AS_RSH,		Operand.RightShift},
				{Parser.AS_AND,		Operand.BitAnd},
				{Parser.AS_OR,		Operand.BitOr},
				{Parser.AS_XOR,		Operand.BitXor},
			};

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Operand ToOp( this IToken tok )
			=> ToOperand[tok.Type];

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Operand ToPreOp( this IToken tok )
			=> ToPreOperand[tok.Type];

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Operand ToAssignOp( this IToken tok )
			=> ToAssignOperand[tok.Type];

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Expr Visit( this Parser.ExprContext c )
			=> c == null
				? null
				: ExprVis.Visit( c );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Stmt Visit( this Parser.LevStmtContext c )
			=> c == null
				? null
				: StmtVis.Visit( c );
	}
}
