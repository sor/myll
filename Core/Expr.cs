using System;
using System.Collections.Generic;
using System.Text;

namespace Myll.Core
{
	public enum Operand
	{
		Scoped,

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
		
		Id,
		WildId,
		DiscardId,
		
		Literal,
	}

	public class Expr
	{
		public Operand op { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach( var info in GetType().GetProperties() ) {
				var value = info.GetValue( this, null ) ?? "(null)";
				sb.Append( info.Name + ": " + value.ToString() + ", " );
			}
			sb.Length = Math.Max( sb.Length - 2, 0 );
			return "{"
			       + GetType().Name + " "
			       + sb.ToString()  + "}";
		}
	}

	public class UnOp : Expr
	{
		public Expr expr { get; set; }
	}

	public class BinOp : Expr
	{
		public Expr left { get; set; }
		public Expr right { get; set; }
	}

	public class TernOp : Expr
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

	public class IdExpr : Expr
	{
		public IdentifierTpl id;
	}

	public class FuncCallExpr : Expr
	{
		public Expr      left; // name and tpl args in here
		public Func.Call funcCall;
	}

	public class CastExpr : UnOp
	{
		public Typespec type;
	}

	public class NewExpr : Expr
	{
		public Typespec  type;
		public Func.Call funcCall;
	}

	public class Literal : Expr
	{
		// TODO
		public string text { get; set; }
	}
}