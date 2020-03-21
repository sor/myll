using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Myll.Generator;

using static System.String;

namespace Myll.Core
{
	static class Precedence
	{
		// Precedence Table in MYLL, will be filled with everything from Operand
		// The original C++ levels are times 10 here to enable insertion of new in-between levels
		public static readonly IDictionary<Operand, int>
			PrecedenceLevel = new Dictionary<Operand, int> {
				{ Operand.Scoped, 10 },
				{ Operand.PostOps_Begin, 20 },
				{ Operand.PreOps_Begin, 30 },
				{ Operand.MemberAccessPtr_Begin, 40 },
				{ Operand.Pow, 45 }, // new mid level
				{ Operand.MultOps_Begin, 50 },
				{ Operand.AddOps_Begin, 60 },
				{ Operand.ShiftOps_Begin, 70 },
				{ Operand.Comparison, 80 },
				{ Operand.OrderOps_Begin, 90 },
				{ Operand.EqualOps_Begin, 100 },
				{ Operand.And, 140 },
				{ Operand.Or, 150 },
				{ Operand.NullCoalesce, 155 }, // new mid level
				{ Operand.Conditional, 160 },
			};

		// Only the deviating levels for moved operators
		// If there is too much difference in precedence, this check might not make sense anymore
		public static readonly IDictionary<Operand, int>
			OriginalPrecedenceLevel = new Dictionary<Operand, int> {
				{ Operand.BitAnd, 110 },
				{ Operand.BitXor, 120 },
				{ Operand.BitOr, 130 },
			};

		static Precedence()
		{
			int currentLevel = 0;
			foreach( Operand op in System.Enum.GetValues( typeof( Operand ) ) ) {
				if( PrecedenceLevel.TryGetValue( op, out int level ) ) {
					currentLevel = level;
				}
				else {
					PrecedenceLevel.Add( op, currentLevel );
				}
			}
		}
	}

	public enum Operand
	{
		Scoped,

		PostOps_Begin,
		PostIncr,
		PostDecr,
		FuncCall,
		NCFuncCall, // new, special
		IndexCall,
		NCIndexCall, // new, special
		MemberAccess,
		NCMemberAccess, // new, special
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
		MoveCast,
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
		NCMemberAccessPtr, // new, special
		MemberPtrAccessPtr,
		MemberAccessPtr_End,

		Pow,

		MultOps_Begin,
		Multiply,
		EuclideanDivide, // for floating points it is not euclidean
		Modulo,
		BitAnd, // moved, special
		Dot,    // new, special
		Cross,  // new, special
		Divide, // new, special
		MultOps_End,

		AddOps_Begin,
		Add,
		Subtract,
		BitOr,  // moved, special
		BitXor, // moved, special
		AddOps_End,

		ShiftOps_Begin,
		LeftShift,
		RightShift,
		ShiftOps_End,

		Comparison, // special, spaceship

		OrderOps_Begin,	// TODO: Relational Ops
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

		NullCoalesce, // new, special

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
		public Operand op              { get; set; }
		public int     PrecedenceLevel => Precedence.PrecedenceLevel[op];
		/// Is precedence divergent from the based upon language?
		public bool    IsDivergentPrecedence => Precedence.OriginalPrecedenceLevel.ContainsKey( op );
		public int     OriginalPrecedenceLevel
			=> Precedence.OriginalPrecedenceLevel.TryGetValue( op, out int value )
				? value
				: PrecedenceLevel;

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

		public virtual string Gen( bool doBrace = false )
		{
			throw new NotImplementedException();
		}
	}

	/// Unary Operation - one operand
	public class UnOp : Expr
	{
		public Expr expr { get; set; }

		public override string Gen( bool doBrace = false )
		{
			if( op == Operand.Parens )
				return string.Format( "({0})", expr.Gen() );

			bool isPreOp       = Operand.PreOps_Begin  <= op && op <= Operand.PreOps_End;
			bool isPostOp      = Operand.PostOps_Begin <= op && op <= Operand.PostOps_End;
			bool divPrecedence = IsDivergentPrecedence;
			bool doBraceExpr = (divPrecedence || expr.IsDivergentPrecedence)
			                && OriginalPrecedenceLevel < expr.OriginalPrecedenceLevel;

			// TODO: pre or post or what?
			return string.Format(
				doBrace
					? "({1} <{0}>)"
					: "{1} <{0}>",
				op.ToString(),
				expr.Gen( doBraceExpr ) );
		}
	}

	/// Binary Operation - two operands
	public class BinOp : Expr
	{
		public Expr left  { get; set; }
		public Expr right { get; set; }

		public override string Gen( bool doBrace = false )
		{
			// only look downward
			// myll: a * b | c == d
			// c++: (a * b | c) == d
			// myll: 100 / 60 / 50
			//      eq / binor / mult
			// c++: 100 / *110* / 50

			bool divPrecedence = IsDivergentPrecedence;
			bool doBraceLeft = (divPrecedence || left.IsDivergentPrecedence)
			                && OriginalPrecedenceLevel < left.OriginalPrecedenceLevel;
			bool doBraceRight = (divPrecedence || right.IsDivergentPrecedence)
			                 && OriginalPrecedenceLevel < right.OriginalPrecedenceLevel;

			string opFormat = doBrace
				? "(" + op.GetFormat() + ")"
				: op.GetFormat();

			return string.Format(
				opFormat,
				left.Gen( doBraceLeft ),
				right.Gen( doBraceRight ) );
		}
	}

	/// Ternary Operation - three operands, right now only: if ? then : else
	public class TernOp : Expr
	{
		public Expr left  { get; set; }
		public Expr mid   { get; set; }
		public Expr right { get; set; }

		public override string Gen( bool doBrace = false )
		{
			bool divPrecedence = IsDivergentPrecedence;
			bool doBraceLeft = (divPrecedence || left.IsDivergentPrecedence)
			                && OriginalPrecedenceLevel < left.OriginalPrecedenceLevel;
			bool doBraceMid = (divPrecedence || mid.IsDivergentPrecedence)
			               && OriginalPrecedenceLevel < mid.OriginalPrecedenceLevel;
			bool doBraceRight = (divPrecedence || right.IsDivergentPrecedence)
			                 && OriginalPrecedenceLevel < right.OriginalPrecedenceLevel;

			return string.Format(
				doBrace
					? "({0} ? {1} : {2})"
					: "{0} ? {1} : {2}",
				left.Gen( doBraceLeft ),
				mid.Gen( doBraceMid ),
				right.Gen( doBraceRight ) );
		}
	}

	public class ScopedExpr : Expr
	{
		public List<IdTpl> idTpls;
		public Expr        expr;

		public override string Gen( bool doBrace = false )
		{
			string ret = idTpls
				.Select( s => s.Gen() )
				.Append( expr.Gen() )
				.Join( "::" );
			return doBrace
				? "(" + ret + ")"
				: ret;
		}
	}

	/*
	 	The replacement must happen before Gen()
	 	Happens along with the generation of the symbols, so SKIP for later as well

		This Code:
			// tryget is func(int, int&) {...}
			tryget( c, _ ); // we don't care about 2nd param
		Needs to Gen this:
	 		[[maybe_unused]] int temp_4711; // non colliding name, up and down the line
	 		tryget( c, temp_4711 );

	 	Assignment case:
	 		std::ignore = func_with_nodiscard();

		Pointer:
			func ptr(T*) called as ptr( _ ) will transform to ptr( nullptr )
			or maybe not, this is only for out-parameters???

		Problems:
			Overloaded Functions, which to call? cast the _ like: call( (int)_ )
			This should fail: var int a = _;
	*/
	public class IdExpr : Expr
	{
		public IdTpl idTpl;

		public override string Gen( bool doBrace = false )
		{
			if( op.In( Operand.WildId, Operand.DiscardId ) )
				throw new InvalidOperationException( "These should have already been replaced by now" );

			return idTpl.Gen();
			//return string.Format( "{0}", id.Gen() );
		}
	}

	public class FuncCallExpr : Expr
	{
		public Expr      left; // name and tpl args in here
		public Func.Call funcCall;

		public override string Gen( bool doBrace = false )
		{
			string ret = left.Gen() + funcCall.Gen();
			return doBrace
				? "(" + ret + ")"
				: ret;
		}
	}

	public class CastExpr : UnOp
	{
		public Typespec type;

		public override string Gen( bool doBrace = false )
		{
			string format = op switch {
				Operand.StaticCast  => "std::static_cast<{1}>( {0} )",
				Operand.DynamicCast => "std::dynamic_cast<{1}>( {0} )",
				Operand.AnyCast     => "std::reinterpret_cast<{1}>( {0} )", // TODO: identify const_cast
				Operand.MoveCast    => "std::{1}( {0} )",                   // TODO: this can handle std::forward as well
			};
			string ret = Format( format, expr.Gen(), type.Gen() );
			return doBrace
				? "(" + ret + ")"
				: ret;
		}
	}

	public class NewExpr : Expr
	{
		public Typespec  type;
		public Func.Call funcCall;

		public override string Gen( bool doBrace = false )
		{
			string ret = "new " + type.Gen() + funcCall.Gen();
			return doBrace
				? "(" + ret + ")"
				: ret;
		}
	}

	public class Literal : Expr
	{
		// TODO
		public string text { get; set; }

		public override string Gen( bool doBrace = false )
		{
			return /*doBrace ? "(" + text + ")" :*/ text;
		}
	}
}
