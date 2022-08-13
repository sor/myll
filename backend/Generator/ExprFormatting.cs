using System.Collections.Generic;
using Myll.Core;

namespace Myll.Generator
{
	public static class ExprFormatting
	{
		private static readonly IDictionary<Operand, string>
			ToFormat = new Dictionary<Operand, string> {
				{ Operand.PostIncr, "{0}++" },
				{ Operand.PostDecr, "{0}--" },
				{ Operand.FuncCall, "{0}( {1} )" },    // 0 = FuncCallExpr.left, 1 = FuncCallExpr.funcCall
				{ Operand.IndexCall, "{0}[{1}]" },     // 0 = FuncCallExpr.left, 1 = FuncCallExpr.funcCall
				{ Operand.MemberAccess, "{0}.{1}" },
				{ Operand.MemberPtrAccess, "{0}->{1}" },
				{ Operand.MemberAccessPtr, "TODO MAP .*" },     // TODO
				{ Operand.MemberPtrAccessPtr, "TODO MPAP ->*" }, // TODO
				{ Operand.PreIncr, "++{0}" },
				{ Operand.PreDecr, "--{0}" },
				{ Operand.PrePlus, "+{0}" },
				{ Operand.PreMinus, "-{0}" },
				{ Operand.Negation, "!{0}" },
				{ Operand.Complement, "~{0}" },
				{ Operand.Dereference, "*{0}" },
				{ Operand.AddressOf, "&{0}" },
				{ Operand.Pow, "pow( {0}, {1} )" },
				{ Operand.Multiply, "{0} * {1}" },
				{ Operand.EuclideanDivide, "{0} / {1}" },
				{ Operand.Modulo, "{0} % {1}" },
				{ Operand.Dot, "dot( {0}, {1} )" },
				{ Operand.Cross, "cross( {0}, {1} )" },
				{ Operand.Divide, "(double){0} / (double){1}" }, // TODO: this for integral, div() for others
				{ Operand.Add, "{0} + {1}" },
				{ Operand.Subtract, "{0} - {1}" },
				{ Operand.BitAnd, "{0} & {1}" },
				{ Operand.BitXor, "{0} ^ {1}" },
				{ Operand.BitOr, "{0} | {1}" },
				{ Operand.LeftShift, "{0} << {1}"},
				{ Operand.RightShift,"{0} >> {1}"},
				{ Operand.Comparison, "cmp( {0}, {1} )" },
				{ Operand.LessThan, "{0} < {1}" },
				{ Operand.LessEqual, "{0} <= {1}" },
				{ Operand.GreaterThan, "{0} > {1}" },
				{ Operand.GreaterEqual, "{0} >= {1}" },
				{ Operand.Equal, "{0} == {1}" },
				{ Operand.NotEqual, "{0} != {1}" },
				{ Operand.And, "{0} && {1}" },
				{ Operand.Or, "{0} || {1}" },

				{ Operand.Delete,		"delete {0}" },
				{ Operand.DeleteAry,	"delete[] {0}" },
			};

		private static readonly IDictionary<Operand, string>
			ToAssignFormat = new Dictionary<Operand, string> {
				{ Operand.Pow,				"{0} = pow( {0}, {1} );" },
				{ Operand.Multiply,			"{0} *= {1};" },
				{ Operand.EuclideanDivide,	"{0} /= {1};" },
				{ Operand.Modulo,			"{0} %= {1};" },
				{ Operand.Dot,				"{0} = dot( {0}, {1} );" },
				{ Operand.Cross,			"{0} = cross( {0}, {1} );" },
				{ Operand.Divide,			"{0} = (double){0} / (double){1};" }, // TODO: this for integral, div() for others
				{ Operand.Add,				"{0} += {1};" },
				{ Operand.Subtract,			"{0} -= {1};" },
				{ Operand.BitAnd,			"{0} &= {1};" },
				{ Operand.BitXor,			"{0} ^= {1};" },
				{ Operand.BitOr,			"{0} |= {1};" },
				{ Operand.LeftShift,		"{0} <<= {1};" },
				{ Operand.RightShift,		"{0} >>= {1};" },
			};

		public static string GetFormat( this Operand op )
		{
			return ToFormat[op];
		}

		public static string GetAssignFormat( this Operand op )
		{
			return ToAssignFormat[op];
		}
	}
}
