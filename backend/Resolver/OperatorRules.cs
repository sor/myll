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
			         or TypespecBasic.Kind.Char
			         or TypespecBasic.Kind.UntypedInteger;

		public static bool IsScalarFloat( Typespec? t )
			=> t is TypespecBasic b && HasNoPointer( t )
			 && ( b.kind == TypespecBasic.Kind.Float
			   || b.kind == TypespecBasic.Kind.UntypedFloat );

		public static bool IsScalarNumber( Typespec? t )
			=> IsScalarInteger( t ) || IsScalarFloat( t );

		public static bool IsScalar( Typespec? t )
			=> IsScalarNumber( t );

		public static bool IsBuiltinScalar( Typespec? t )
			=> t is TypespecBasic && HasNoPointer( t );

		public static bool HasPointer( Typespec? t )
			=> t?.ptrs is { Count: > 0 };
	}
}
