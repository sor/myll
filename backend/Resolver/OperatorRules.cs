using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Helpers for deciding which built-in scalar types may participate in which operators.
	/// This is intentionally conservative: it only validates operators whose operands are
	/// plain built-in scalar kinds (i/u/f, bool, char). Anything involving classes, references,
	/// or user-defined operator overloads is left for C++ to resolve.
	/// </summary>
	public static class OperatorRules
	{
		private static bool HasNoPointer( Typespec? t )
			=> t?.ptrs == null || t.ptrs.Count == 0;

		public static bool IsScalarInteger( Typespec? t )
			=> t is TypespecBasic b && HasNoPointer( t )
			 && b.kind is TypespecBasic.Kind.Integer
			         or TypespecBasic.Kind.Unsigned
			         or TypespecBasic.Kind.Bool
			         or TypespecBasic.Kind.UntypedInteger;

		public static bool IsScalarBit( Typespec? t )
			=> t is TypespecBasic b && HasNoPointer( t )
			 && b.kind == TypespecBasic.Kind.Bitwise;

		public static bool IsScalarByte( Typespec? t )
			=> t is TypespecBasic b && HasNoPointer( t )
			 && b.kind == TypespecBasic.Kind.Byte;

		public static bool IsScalarBitFamily( Typespec? t )
			=> IsScalarBit( t ) || IsScalarByte( t );

		public static bool IsScalarFloat( Typespec? t )
			=> t is TypespecBasic b && HasNoPointer( t )
			 && ( b.kind == TypespecBasic.Kind.Float
			   || b.kind == TypespecBasic.Kind.UntypedFloat );

		public static bool IsScalarNumber( Typespec? t )
			=> IsScalarInteger( t ) || IsScalarFloat( t );

		// May participate in comparisons and equality. Char is only comparable with char
		// (enforced in TypeChecker.ValidateComparisonOperator) and never arithmetic.
		public static bool IsScalarComparable( Typespec? t )
			=> t is TypespecBasic b && HasNoPointer( t )
			 && b.kind is TypespecBasic.Kind.Integer
			         or TypespecBasic.Kind.Unsigned
			         or TypespecBasic.Kind.Float
			         or TypespecBasic.Kind.Bool
			         or TypespecBasic.Kind.Char
			         or TypespecBasic.Kind.UntypedInteger
			         or TypespecBasic.Kind.UntypedFloat;

		public static bool IsScalar( Typespec? t )
			=> IsScalarNumber( t );

		public static bool HasPointer( Typespec? t )
			=> t?.ptrs is { Count: > 0 };

		public static bool IsPointerToArray( Typespec? t )
			=> t?.ptrs is { Count: > 0 } p
			 && p[^1].kind == Pointer.Kind.PtrToAry;

		/// <summary>
		/// Operator classification for the built-in operator sets, one canonical
		/// place so the resolver and the type checker cannot drift apart.
		/// </summary>
		public static bool IsMemberAccess( Operand op )
			=> op is Operand.MemberAccess or Operand.NCMemberAccess
			    or Operand.MemberPtrAccess or Operand.MemberAccessPtr
			    or Operand.NCMemberAccessPtr or Operand.MemberPtrAccessPtr;

		public static bool IsArithmetic( Operand op )
			=> op is Operand.Add or Operand.Subtract or Operand.Multiply
			    or Operand.Divide or Operand.FractionalDivide or Operand.Modulo;

		// Bit-flavored arithmetic and bitwise/shift operators, for operand family checks.
		// Bit types are bags of bits with set semantics: `+` union, `-` difference,
		// `*` intersection, `/` implication/subset (tentative), `%` equivalence.
		public static bool IsBitOperator( Operand op )
			=> op is Operand.Add or Operand.Subtract or Operand.Multiply
			    or Operand.Divide or Operand.Modulo
			    or Operand.BitAnd or Operand.BitOr or Operand.BitXor
			    or Operand.LeftShift or Operand.RightShift;

		public static bool IsBitwise( Operand op )
			=> op is Operand.BitAnd or Operand.BitOr or Operand.BitXor;

		public static bool IsShift( Operand op )
			=> op is Operand.LeftShift or Operand.RightShift;

		public static bool IsComparison( Operand op )
			=> op is Operand.Equal or Operand.NotEqual or Operand.LessThan
			    or Operand.LessEqual or Operand.GreaterThan or Operand.GreaterEqual
			    or Operand.Comparison;

		public static bool IsLogical( Operand op )
			=> op is Operand.And or Operand.Or;

		/// <summary>
		/// A typed bit/byte operand may not mix families; untyped operands bind to either.
		/// </summary>
		public static bool SameBitFamily( Typespec? left, Typespec? right )
		{
			bool leftBit  = IsScalarBit( left );
			bool rightBit = IsScalarBit( right );
			bool leftByte = IsScalarByte( left );
			bool rightByte = IsScalarByte( right );

			if( ( leftBit && rightByte ) || ( leftByte && rightBit ) )
				return false;

			return leftBit || rightBit || leftByte || rightByte;
		}
	}
}
