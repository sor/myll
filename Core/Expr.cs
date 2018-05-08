using System;
using System.Collections.Generic;
using System.Text;

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
		
		MemberAccess,
		MemberPtrAccess,
		MemberAccessPtr,
		MemberPtrAccessPtr,
		
		Pow,
		
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
		
		NullCoalesce,
		
		Conditional,
		
		Parens,
	}

	public class Expr
	{
		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var info in GetType().GetProperties())
			{
				var value = info.GetValue(this, null) ?? "(null)";
				sb.Append(info.Name + ": " + value.ToString() + ", ");
			}
			sb.Length = Math.Max(sb.Length - 2, 0);
			return "{"
			       + GetType().Name + " "
			       + sb.ToString()  + "}";
		}
	}

	public class OpExpr : Expr
	{
		public Operand op { get; set; }
	}

	public class UnOp : OpExpr
	{
		public Expr expr { get; set; }
	}

	public class BinOp : OpExpr
	{
		public Expr left { get; set; }
		public Expr right { get; set; }
	}

	public class TernOp : OpExpr
	{
		public Expr ifExpr { get; set; }
		public Expr thenExpr { get; set; }
		public Expr elseExpr { get; set; }
	}

	public class ScopedExpr : Expr
	{
		public List<IdentifierTpl> identifiers;
		public Expr                expr;
	}

	public class Literal : Expr
	{
		public string text { get; set; }
	}
}