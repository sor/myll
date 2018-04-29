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
		// TODO
		AssignOps_End,
		MultOps_Begin,
		Multiply,
		Divide,
		Modulo,
		MultOps_End,
		AddOps_Begin,
		Addition,
		Subtraction,
		AddOps_End,
	}

	public class Expr
	{
		
	}

	public class UnOp : Expr
	{
		public Expr    expr;
		public Operand op;
	}

	public class BinOp : Expr
	{
		public Expr    left;
		public Expr    right;
		public Operand op;
	}
}