using System;
using System.Numerics;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Literal-only constant folding for template arguments, shared by the
	/// visitor (use-site template arguments) and the pre-resolution
	/// transformer (specialization bindings). Unary minus plus the binary
	/// integer operators; division by zero and out-of-range shifts stay
	/// unfolded for the C++ compiler to reject. Mirrors the
	/// untyped-integer folding rules of the resolver.
	/// </summary>
	public static class TemplateArgFolding
	{
		public static bool TryFoldLiteral( Expr expr, out BigInteger value )
		{
			value = BigInteger.Zero;

			switch( expr ) {
				case Literal lit:
					return ConversionRules.TryParseInteger( lit.text, out value );

				case UnOp { op: Operand.PreMinus, expr: Literal inner }:
					return ConversionRules.TryParseInteger( inner.text, out value )
					 && ( value = -value ) == value;

				case BinOp binOp:
					if( !TryFoldLiteral( binOp.left, out BigInteger left )
					 || !TryFoldLiteral( binOp.right, out BigInteger right ) )
						return false;
					return TryFoldBinary( binOp.op, left, right, out value );

				default:
					return false;
			}
		}

		private static bool TryFoldBinary( Operand op, BigInteger left, BigInteger right, out BigInteger result )
		{
			result = BigInteger.Zero;

			switch( op ) {
				case Operand.Add:
				case Operand.Multiply:
				case Operand.Subtract:
				case Operand.Divide:
				case Operand.Modulo:
				case Operand.BitAnd:
				case Operand.BitOr:
				case Operand.BitXor:
				case Operand.LeftShift:
				case Operand.RightShift:
					return TryFoldInteger( op, left, right, out result );

				default:
					return false;
			}
		}

		private static bool TryFoldInteger( Operand op, BigInteger left, BigInteger right, out BigInteger result )
		{
			result = BigInteger.Zero;

			switch( op ) {
				case Operand.Add:
					result = left + right;
					return true;

				case Operand.Subtract:
					result = left - right;
					return true;

				case Operand.Multiply:
					result = left * right;
					return true;

				case Operand.Divide:
					// Both C++ and BigInteger integer division truncate toward zero;
					// division by zero is left to the C++ compiler.
					if( right.IsZero )
						return false;
					result = left / right;
					return true;

				case Operand.Modulo:
					if( right.IsZero )
						return false;
					result = left % right;
					return true;

				case Operand.BitAnd:
					result = left & right;
					return true;

				case Operand.BitOr:
					result = left | right;
					return true;

				case Operand.BitXor:
					result = left ^ right;
					return true;

				case Operand.LeftShift:
					if( right.Sign < 0 || right > int.MaxValue )
						return false;
					result = left << (int) right;
					return true;

				case Operand.RightShift:
					if( right.Sign < 0 || right > int.MaxValue )
						return false;
					result = left >> (int) right;
					return true;

				default:
					return false;
			}
		}
	}
}