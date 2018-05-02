using System.Reflection.Emit;
using Antlr4.Runtime.Atn;

namespace Myll.Core
{
	/*
	mul: * / % << >> &
	add: + - | ...
	cmp: == <= >= ...
	and: &&
	or: ||
	tern: ?:
	*/
	
	public enum Operand
	{
		PostOps_Begin,
		PostIncr,
		PostDecr,
		PostOps_End,
		
		PreOps_Begin,
		PreIncr,
		PreDecr,
		PrePlus,
		PreMinus,
		Negation,
		Complement,
		Dereference,
		AddressOf,
		PreOps_End,
		
		AssignOps_Begin,
		Assign,
		// TODO: move to statements
		AssignOps_End,
		
		MultOps_Begin,
		Multiply,
		Divide,
		Modulo,
		BitAnd,
		MultOps_End,
		
		AddOps_Begin,
		Addition,
		Subtraction,
		BitOr,
		BitXor,
		AddOps_End,
		
		ShiftOps_Begin,
		LeftShift,
		RightShift,
		ShiftOps_End,
		
		Comparison,
		
		OrderOps_Begin,
		LessThan,
		LessEqual,
		GreaterThan,
		GreaterEqual,
		OrderOps_End,
		
		EqualOps_Begin,
		Equal,
		NotEqual,
		EqualOps_End,

		BooleanOps_Begin,
		And,
		Or,
		BooleanOps_End,
		
		Ternary,
	}

	public class Expr
	{
		
	}

	public class OpExpr : Expr
	{
		public Operand op;
	}

	public class UnOp : OpExpr
	{
		public Expr expr;
	}

	public class BinOp : OpExpr
	{
		public Expr left;
		public Expr right;
	}

	public class TernOp : OpExpr
	{
		public Expr ifExpr;
		public Expr thenExpr;
		public Expr elseExpr;
	}
}