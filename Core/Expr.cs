using System;
using System.Collections.Generic;
using System.Text;

namespace Myll.Core
{
	public enum Operand
	{
		PostOps_Begin,
		PostIncr,
		PostDecr,
		FuncCall,
		NCFuncCall,
		IndexCall,
		NCIndexCall,
		MemberAccess,
		NCMemberAccess,
		MemberPtrAccess,
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
		
		StaticCast,
		DynamicCast,
		AnyCast,
		SizeOf,
		New,
		Delete,
		DeleteAry,
		PreOps_End,
		
		AssignOps_Begin,
		Assign,
		// TODO: move to statements
		AssignOps_End,
		
		MemberAccessPtr,
		NCMemberAccessPtr,
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
		public List<IdentifierTpl> ids;
		public Expr                expr;
	}

	public class IdTplExpr : Expr
	{
		public IdentifierTpl id;
	}
	
	public class PostExpr : Expr
	{
		public Expr left { get; set; }
	}

	public class FuncCallExpr : OpExpr
	{
		// fac(n: 1+2) // n is matching _name_ of param, 1+2 is _expr_
		public class Arg
		{
			public string name; // opt
			public Expr   expr;
		}

		public Expr      left; // name
		public List<Arg> args;
		//public List<TemplateArg> templateArgs; // part of expr
	}

	public class CastExpr : OpExpr
	{
		public Typespec type;
		public Expr     expr;
	}

	public class NewExpr : OpExpr
	{
		public Typespec               type;
		public List<FuncCallExpr.Arg> args;
	}

	public class Literal : Expr
	{
		public string text { get; set; }
	}
}