using System;
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
		Divide, // normal / division
		Modulo,
		BitAnd, // moved, special
		Dot,    // new, special
		Cross,  // new, special
		FractionalDivide, // new, special
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

	public abstract class Expr : Node
	{
		public Operand op              { get; set; }
		public int     PrecedenceLevel => Precedence.PrecedenceLevel[op];
		/// Is precedence divergent from the based upon language?
		public bool    IsDivergentPrecedence => Precedence.OriginalPrecedenceLevel.ContainsKey( op );
		public int     OriginalPrecedenceLevel
			=> Precedence.OriginalPrecedenceLevel.TryGetValue( op, out int value )
				? value
				: PrecedenceLevel;

		/// <summary>
		/// The static type of this expression, computed by the type-resolution pass after
		/// name resolution has converged. Null if the type could not be determined.
		/// </summary>
		public Typespec? Type { get; set; }

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
		public Expr expr { get; set; } = null!;

		public override string Gen( bool doBrace = false )
		{
			if( op == Operand.Parens )
				return expr.Gen().Brace( true );

			bool isPreOp       = op.Between( Operand.PreOps_Begin,  Operand.PreOps_End );
			bool isPostOp      = op.Between( Operand.PostOps_Begin, Operand.PostOps_End );
			bool divPrecedence = IsDivergentPrecedence;
			bool doBraceExpr = (divPrecedence || expr.IsDivergentPrecedence)
			                && OriginalPrecedenceLevel < expr.OriginalPrecedenceLevel;

			if( op == Operand.Complement
			 && expr.Type is TypespecBasic { kind: TypespecBasic.Kind.Bitwise } ) {
				return Format(
					"static_cast<{0}>( ~{1} )",
					expr.Type.GenType(),
					expr.Gen( doBraceExpr ) ).Brace( doBrace );
			}

			string opFormat = op.GetFormat().Brace( doBrace );

			return Format(
				opFormat,
				expr.Gen( doBraceExpr ) );
		}
	}

	/// Binary Operation - two operands
	public class BinOp : Expr
	{
		public Expr left  { get; set; } = null!;
		public Expr right { get; set; } = null!;

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

			bool isBitOp = TryGetBitFamilyType( out string? bitType );
			string opFormat = ( isBitOp ? GetBitOperatorFormat( op ) : null ) ?? op.GetFormat();
			opFormat = opFormat.Brace( doBrace );

			string leftStr  = left.Gen( doBraceLeft );
			string rightStr = right.Gen( doBraceRight );

			// Bit-family operators generate bitwise C++. Untyped integer literals must
			// be cast to the bit type; std::byte in particular does not accept naked ints.
			if( isBitOp ) {
				bool isShift = op is Operand.LeftShift or Operand.RightShift;

				// The shift amount stays a normal integer; std::byte shifts accept an
				// integer count.
				if( !isShift && left.Type is TypespecBasic { kind: TypespecBasic.Kind.UntypedInteger } )
					leftStr = BitFamilyCast( bitType!, leftStr );
				if( !isShift && right.Type is TypespecBasic { kind: TypespecBasic.Kind.UntypedInteger } )
					rightStr = BitFamilyCast( bitType!, rightStr );

				// Subtraction and implication use a negated operand. In C++ the built-in
				// unary ~ promotes small unsigned types to int, so we cast back to the bit
				// type to preserve the intended width.
				switch( op ) {
					case Operand.Subtract:
						rightStr = BitFamilyCast( bitType!, Format( "~{0}", rightStr ) );
						break;

					case Operand.Divide:
						leftStr = BitFamilyCast( bitType!, Format( "~{0}", leftStr ) );
						break;
				}
			}

			return Format(
				opFormat,
				leftStr,
				rightStr );
		}

		private static string? GetBitOperatorFormat( Operand op )
		{
			return op switch {
				Operand.Add              => "{0} | {1}",
				Operand.Multiply         => "{0} & {1}",
				Operand.Subtract         => "{0} & {1}",
				Operand.Divide           => "{0} | {1}",
				Operand.BitAnd           => "{0} & {1}",
				Operand.BitOr            => "{0} | {1}",
				Operand.BitXor           => "{0} ^ {1}",
				_                        => null,
			};
		}

		/// <summary>
		/// Casts a value to the bit-family type. For <c>std::byte</c> this uses C++17
		/// brace-init (<c>std::byte{ n }</c>); otherwise it falls back to
		/// <c>static_cast&lt;T&gt;( n )</c>.
		/// </summary>
		private static string BitFamilyCast( string bitType, string expr )
		{
			if( bitType == "std::byte" )
				return Format( "std::byte{{{0}}}", expr );

			return Format( "static_cast<{0}>( {1} )", bitType, expr );
		}

		private bool TryGetBitFamilyType( out string? bitType )
		{
			bitType = null;

			if( IsBitFamilyOperand( left ) ) {
				bitType = left.Type!.GenType();
				return true;
			}

			if( IsBitFamilyOperand( right ) ) {
				bitType = right.Type!.GenType();
				return true;
			}

			return false;
		}

		private static bool IsBitFamilyOperand( Expr e )
			=> e.Type is TypespecBasic { kind: TypespecBasic.Kind.Bitwise or TypespecBasic.Kind.Byte }
			 && ( e.Type.ptrs == null || e.Type.ptrs.Count == 0 );
	}

	/// Ternary Operation - three operands, currently only: if ? then : else
	public class TernOp : Expr
	{
		public Expr left  { get; set; } = null!;
		public Expr mid   { get; set; } = null!;
		public Expr right { get; set; } = null!;

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
		public Func func = null!;

		public override string Gen( bool doBrace = false )
		{
			// TODO capture and template
			string paramString = func.paras
				.Select( p => p.Gen() )
				.Join( ", " );

			if( func.body == null )
				throw new InvalidOperationException( String.Format( "Lambda at {0} must have a body", func.srcPos?.ToString() ?? "<unknown location>" ) );

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
		public List<IdTplArgs> idTpls = null!;

		// Set by NameResolver.Apply() once names have been resolved.
		public Decl? resolvedDecl;

		public override string Gen( bool doBrace = false )
		{
			if( resolvedDecl != null
			 && resolvedDecl.name != "<builtin>"
			 && idTpls.TrueForAll( it => it.tplArgs.Count == 0 ) )
				return resolvedDecl.FullyQualifiedName.Brace( doBrace );

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
		public IdTplArgs idTplArgs = null!;

		// Set by NameResolver.Apply() once names have been resolved.
		public Decl? resolvedDecl;

		public override string Gen( bool doBrace = false )
		{
			if( op.In( Operand.WildId, Operand.DiscardId ) )
				throw new Exception( "These should have already been replaced by now" );

			// TODO solve that properly via Operand.SpecialId during creation
			if( idTplArgs.id == "self" && idTplArgs.tplArgs.IsEmpty() )
				return "(*this)";
			else if( idTplArgs.id == "null" && idTplArgs.tplArgs.IsEmpty() )
				return "nullptr";

			// Prefer the resolved declaration. Locals and instance members stay
			// unqualified; everything else gets a qualified name so we do not depend
			// on C++ unqualified lookup or using-directives.
			if( resolvedDecl != null && resolvedDecl.name != "<builtin>" ) {
				string name = EmitReferenceName( resolvedDecl );

				if( idTplArgs.tplArgs.Count > 0 ) {
					name += "<" + idTplArgs.tplArgs
						.Select( t => t.Gen() )
						.Join( ", " ) + ">";
				}

				return name.Brace( doBrace );
			}

			return idTplArgs.Gen().Brace( doBrace );
		}

		private static string EmitReferenceName( Decl decl )
		{
			// Locals and instance members are emitted raw: C++ unqualified lookup
			// finds them in their own scope/class.
			if( decl.IsLocal )
				return decl.name;

			if( decl.IsInStruct && !decl.IsStatic )
				return decl.name;

			return decl.scope != null
				? decl.ReferenceName
				: decl.name;
		}
	}

	// Synopsis:
	//  obj.myMethod<int> (arg1, arg2)
	// ^    UnOp.expr    ^  funcCall  ^
	public class FuncCallExpr : UnOp
	{
		public FuncCall funcCall = null!;

		public override string Gen( bool doBrace = false )
		{
			string ret = expr.Gen() + funcCall.Gen();
			return ret.Brace( doBrace );
		}
	}

	public class CastExpr : UnOp
	{
		public Typespec type = null!;

		public override string Gen( bool doBrace = false )
		{
			// Casting a float literal to another float size can be emitted as the
			// appropriately suffixed literal instead of `static_cast<f64>( 9.15f )`.
			if( type is TypespecBasic basic
			 && basic.kind == TypespecBasic.Kind.Float
			 && ( type.ptrs == null || type.ptrs.Count == 0 )
			 && expr is Literal lit
			 && Literal.IsFloatLiteral( lit.text ) ) {
				string suffix = basic.size switch {
					4  => "f",
					8  => "",
					16 => "L",
					_  => "f",
				};

				// f16 has no standard C++ literal suffix, so keep the cast for that case.
				if( basic.size != 2 )
					return ( lit.text + suffix ).Brace( doBrace );
			}

			// C++17 relaxed enum-class initialization: std::byte{ n } is the idiomatic
			// way to convert a numeric value to std::byte.
			if( type is TypespecBasic { kind: TypespecBasic.Kind.Byte }
			 && ( type.ptrs == null || type.ptrs.Count == 0 ) ) {
				return Format( "std::byte{{{0}}}", expr.Gen() ).Brace( doBrace );
			}

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
				_                       => throw new Exception( Format( "Invalid cast of {0} to {1}", expr?.Gen() ?? "", type.Gen() ) ),
			};
			string
				exprText = expr.Gen(),
				typeText = type.Gen();

			string ret = Format( format, exprText, typeText );
			return ret.Brace( doBrace );
		}
	}

	public class NewExpr : Expr
	{
		public Typespec type = null!;
		public FuncCall funcCall = null!;

		public override string Gen( bool doBrace = false )
		{
			string      ret;
			Pointer?    ptr = type.ptrs?.LastOrDefault(); // needs to be a variable to keep it accessible
			List<Pointer> savedPtrs = type.ptrs ?? new();

			if( ptr?.kind == Pointer.Kind.RawPtr ) {
				// `new` already returns a raw pointer, so the outermost raw pointer
				// is consumed by the allocation rather than being part of the allocated type.
				type.ptrs = savedPtrs.Take( savedPtrs.Count - 1 ).ToList();
				string innerType = type.Gen();
				type.ptrs = savedPtrs;

				ret = Format( "new {0}{1}", innerType, funcCall.Gen() );
			}
			else if( ptr != null && ptr.kind.Between( Pointer.Kind.SmartPtr_Begin, Pointer.Kind.SmartPtr_End ) ) {
				string ptrFmt = ptr.kind switch {
					Pointer.Kind.Unique      => "std::make_unique<{0}>({1})",
					Pointer.Kind.UniqueArray => "std::make_unique<{0}[]>({1})",
					Pointer.Kind.Shared      => "std::make_shared<{0}>({1})",
					Pointer.Kind.SharedArray => "std::make_shared<{0}[]>({1})",
					_                        => throw new Exception( "weak_ptr can not be new'ed" ),
				};

				// remove the outermost smart pointer from the type without mutating the AST
				type.ptrs = savedPtrs.Take( savedPtrs.Count - 1 ).ToList();
				string innerType = type.Gen();
				type.ptrs = savedPtrs;

				string args = ptr.kind switch {
					Pointer.Kind.Unique      => funcCall.args.Select( a => a.Gen() ).Join( ", " ),
					Pointer.Kind.Shared      => funcCall.args.Select( a => a.Gen() ).Join( ", " ),
					Pointer.Kind.UniqueArray => ptr.expr?.Gen() ?? "",
					Pointer.Kind.SharedArray => ptr.expr?.Gen() ?? "",
					_                        => throw new Exception( "weak_ptr can not be new'ed" ),
				};

				ret = Format( ptrFmt, innerType, args );
			}
			else {
				// bare types and raw arrays are used literally after `new`
				ret = Format( "new {0}{1}", type.Gen(), funcCall.Gen() );
			}
			return ret.Brace( doBrace );
		}
	}

	// TODO
	public class Literal : Expr
	{
		public string text = null!;

		public override string Gen( bool doBrace = false )
		{
			// Myll float literals are untyped and default to f32. Append 'f' in the
			// generated C++ so that `1.0` becomes a float literal, not a double.
			if( IsFloatLiteral( text ) && !text.EndsWith( "f" ) && !text.EndsWith( "F" ) )
				return text + "f";

			return text; //.Brace( doBrace )
		}

		internal static bool IsFloatLiteral( string t )
		{
			if( string.IsNullOrEmpty( t ) )
				return false;

			if( t == "null" || t == "true" || t == "false" )
				return false;

			if( t[0] == '"' || t[0] == '\'' )
				return false;

			if( !char.IsDigit( t[0] ) && t[0] != '.' )
				return false;

			if( t.Contains( '.' ) )
				return true;

			bool isHex = t.StartsWith( "0x", StringComparison.OrdinalIgnoreCase );
			return !isHex && ( t.Contains( 'e' ) || t.Contains( 'E' ) );
		}
	}
}
