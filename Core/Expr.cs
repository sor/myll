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
		NCFuncCall, // special
		IndexCall,
		NCIndexCall, // special
		MemberAccess,
		NCMemberAccess, // special
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

		Cast_Begin,
		StaticCast,
		DynamicCast,
		AnyCast, // const_cast & reinterpret_cast
		Cast_End,

		SizeOf,
		New,
		Delete,
		DeleteAry,
		PreOps_End,

		MemberAccessPtr_Begin,
		MemberAccessPtr,
		NCMemberAccessPtr, // special
		MemberPtrAccessPtr,
		MemberAccessPtr_End,

		Pow,

		MultOps_Begin,
		Multiply,
		EuclideanDivide,
		Modulo,
		BitAnd, // special
		Dot,    // special
		Cross,  // special
		Divide, // special
		MultOps_End,

		AddOps_Begin,
		Add,
		Subtract,
		BitOr,  // special
		BitXor, // special
		AddOps_End,

		ShiftOps_Begin,
		LeftShift,
		RightShift,
		ShiftOps_End,

		Comparison, // special, spaceship

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

		NullCoalesce, // special

		Conditional, // a ? b : c

		Parens,

		Ids_Begin,
		Id,
		WildId,    // special
		DiscardId, // special
		Ids_End,

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

	// Unary Operation - one operand
	public class UnOp : Expr
	{
		public Expr expr { get; set; }
	}

	// Binary Operation - two operands
	public class BinOp : Expr
	{
		public Expr left  { get; set; }
		public Expr right { get; set; }
	}

	// Ternary Operation - three operands, right now only: if ? then : else
	public class TernOp : Expr
	{
		public Expr ifExpr   { get; set; }
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
