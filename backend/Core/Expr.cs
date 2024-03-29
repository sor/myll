﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Myll.Generator;

namespace Myll.Core
{
	using static String;

	internal static class Precedence
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
		// If there are too many changes in precedence, this pre-check might not make sense anymore
		public static readonly IDictionary<Operand, int>
			OriginalPrecedenceLevel = new Dictionary<Operand, int> {
				{ Operand.BitAnd, 110 },
				{ Operand.BitXor, 120 },
				{ Operand.BitOr, 130 },
			};

		static Precedence()
		{
			int currentLevel = 0;
			foreach( Operand op in Enum.GetValues( typeof( Operand ) ) ) {
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
		NCFuncCall, // new, special null coalescing = NC
		IndexCall,
		NCIndexCall, // new, special null coalescing = NC
		MemberAccess,
		NCMemberAccess, // new, special null coalescing = NC
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
		CopyCast,
		MoveCast,
		ForwardCast,
		StaticCast,
		DynamicCast,
		AddCVCast,
		RemoveCVCast,
		ConstCast,
		BitCast,
		ReinterpretCast,
	//	AnyCast, // const_cast & reinterpret_cast
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

		NullCoalesce, // new, special general null coalescing

		Conditional, // a ? b : c

		Parens,

		Ids_Begin,
		Id,
		SpecialId,
		WildId,    // special
		DiscardId, // special
		Ids_End,

		Literal,
	}

	public abstract class Expr
	{
		public Operand op              { get; set; }
		public int     PrecedenceLevel => Precedence.PrecedenceLevel[op];
		/// Is precedence divergent from the based upon language?
		public bool    IsDivergentPrecedence => Precedence.OriginalPrecedenceLevel.ContainsKey( op );
		public int     OriginalPrecedenceLevel
			=> Precedence.OriginalPrecedenceLevel.TryGetValue( op, out int value )
				? value
				: PrecedenceLevel;

		[Pure]
		public override string ToString()
		{
			StringBuilder sb = GetType().GetProperties().Aggregate(
				new StringBuilder(),
				( builder, info ) => builder.AppendFormat(
					"{0}: {1}, ",
					info.Name,
					info.GetValue( this, null ) ?? "(null)" ) );

			// Remove the excess ", " from the end of the string
			sb.Length = Math.Max( sb.Length - 2, 0 );

			return Format( "{{{0} {1}}}", GetType().Name, sb );
		}

		public abstract string Gen( bool doBrace = false );
	}

	public class Discard : Expr
	{
		public Discard()
		{
			op = Operand.DiscardId;
		}

		public override string Gen( bool doBrace = false )
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
				return expr.Gen().Brace( true );

			bool isPreOp       = op.Between( Operand.PreOps_Begin,  Operand.PreOps_End );
			bool isPostOp      = op.Between( Operand.PostOps_Begin, Operand.PostOps_End );
			bool divPrecedence = IsDivergentPrecedence;
			bool doBraceExpr = (divPrecedence || expr.IsDivergentPrecedence)
			                && OriginalPrecedenceLevel < expr.OriginalPrecedenceLevel;

			string opFormat = op.GetFormat().Brace( doBrace );

			return Format(
				opFormat,
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
			//      eq / binOr / mult
			// c++: 100 / *110* / 50

			bool divPrecedence = IsDivergentPrecedence;
			bool doBraceLeft = (divPrecedence || left.IsDivergentPrecedence)
			                && OriginalPrecedenceLevel < left.OriginalPrecedenceLevel;
			bool doBraceRight = (divPrecedence || right.IsDivergentPrecedence)
			                 && OriginalPrecedenceLevel < right.OriginalPrecedenceLevel;

			string opFormat = op.GetFormat().Brace( doBrace );

			return Format(
				opFormat,
				left.Gen( doBraceLeft ),
				right.Gen( doBraceRight ) );
		}
	}

	/// Ternary Operation - three operands, currently only: if ? then : else
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

			string opFormat = "{0} ? {1} : {2}".Brace( doBrace );

			return Format(
				opFormat,
				left.Gen( doBraceLeft ),
				mid.Gen( doBraceMid ),
				right.Gen( doBraceRight ) );
		}
	}

	public class Lambda : Expr
	{
		public Func func;

		public override string Gen( bool doBrace = false )
		{
			// TODO capture and template
			string paramString = func.paras
				.Select( p => p.Gen() )
				.Join( ", " );

			List<string> body        = func.body.GenWithoutCurly( 1 );
			bool         shortLambda = body.Count <= 1;
			if( shortLambda )
				return Format(
					"[&]({0}) {{ {1} }}",
					paramString,
					body.Join( "\n" ).TrimStart() ).Brace( doBrace );
			else
				return Format(
					"[&]({0}) {{\n{1}\n}}",
					paramString,
					body.Join( "\n" ) ).Brace( doBrace );
		}
	}

	public class ScopedExpr : Expr
	{
		public List<IdTplArgs> idTpls;

		public override string Gen( bool doBrace = false )
		{
			string ret = idTpls
				.Select( s => s.Gen() )
				.Join( "::" );

			return ret.Brace( doBrace );
		}
	}

	/*
	 	The replacement must happen before Gen()
	 	Happens along with the generation of the symbols, so SKIP for later as well

		This Code:
			func tryget(int, int&) {...}
			tryget( c, _ ); // we don't care about 2nd param
		Needs to Gen this:
	 		[[maybe_unused]]
	 		int temp_4711; // non colliding name, up and down the line
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
		public IdTplArgs idTplArgs;

		public override string Gen( bool doBrace = false )
		{
			if( op.In( Operand.WildId, Operand.DiscardId ) )
				throw new Exception( "These should have already been replaced by now" );

			// TODO solve that properly via Operand.SpecialId during creation
			if( idTplArgs.id == "self" && idTplArgs.tplArgs.IsEmpty() )
				return "(*this)";
			else if( idTplArgs.id == "null" && idTplArgs.tplArgs.IsEmpty() )
				return "nullptr";

			return idTplArgs.Gen();
		}
	}

	// Synopsis:
	//  obj.myMethod<int> (arg1, arg2)
	// ^    UnOp.expr    ^  funcCall  ^
	public class FuncCallExpr : UnOp
	{
		public FuncCall funcCall;

		public override string Gen( bool doBrace = false )
		{
			string ret = expr.Gen() + funcCall.Gen();
			return ret.Brace( doBrace );
		}
	}

	public class CastExpr : UnOp
	{
		public Typespec type;

		public override string Gen( bool doBrace = false )
		{
			string format = op switch {
				Operand.CopyCast        => "{1}( {0} )",
				Operand.MoveCast        => "{1}( {0} )",
				Operand.ForwardCast     => "{1}( {0} )",
				Operand.AddCVCast       => "static_cast<{1}<decltype( {0} )>>( {0} )",
				Operand.RemoveCVCast    => "const_cast<{1}<decltype( {0} )>>( {0} )",
				Operand.StaticCast      => "static_cast<{1}>( {0} )",
				Operand.DynamicCast     => "dynamic_cast<{1}>( {0} )",
				Operand.ConstCast       => "const_cast<{1}>( {0} )",
				Operand.BitCast         => "std::bit_cast<{1}>( {0} )",
				Operand.ReinterpretCast => "reinterpret_cast<{1}>( {0} )",
				_                       => null,
			};
			string
				exprText = expr.Gen(),
				typeText = type.Gen();

			if( format == null )
				throw new Exception( Format( "Invalid cast of {0} to {1}", exprText, typeText ) );

			string ret = Format( format, exprText, typeText );
			return ret.Brace( doBrace );
		}
	}

	public class NewExpr : Expr
	{
		public Typespec type;
		public FuncCall funcCall;

		public override string Gen( bool doBrace = false )
		{
			string       ret;
			Pointer      ptr        = type.ptrs.FirstOrDefault(); // needs to be a variable to keep it accessible
			bool         isSmartPtr = ptr?.kind.Between( Pointer.Kind.SmartPtr_Begin, Pointer.Kind.SmartPtr_End ) ?? false;
			if( isSmartPtr ) {
				// remove the pointer that is about to be replaced by custom code
				type.ptrs.RemoveAt( 0 );
				string ptrFmt = ptr.kind switch {
					Pointer.Kind.Unique		 => "std::make_unique<{0}>({1})",
					Pointer.Kind.UniqueArray => "std::make_unique<{0}[]>({1})",
					Pointer.Kind.Shared		 => "std::make_shared<{0}>({1})",
					Pointer.Kind.SharedArray => "std::make_shared<{0}[]>({1})",
					_						 => throw new Exception("weak_ptr can not be new'ed"),
				};
				ret = Format( ptrFmt, type.Gen(), ptr.expr.Gen( false ) );
			}
			else {
				ret = Format( "new {0}{1}", type.Gen(), funcCall.Gen() );
			}
			return ret.Brace( doBrace );
		}
	}

	// TODO
	public class Literal : Expr
	{
		public string text;

		public override string Gen( bool doBrace = false )
		{
			return text; //.Brace( doBrace )
		}
	}
}
