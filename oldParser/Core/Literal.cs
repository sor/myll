using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLang.Core
{
	abstract public class MyLiteral : MyExpression { }

	class MyStringLiteral : MyLiteral {
		public string value;

		public MyStringLiteral( string text ) {
			value = text;
		}

		public override string ToString() {
			return "StringLit: " + value;
		}
	}

	class MyCharLiteral : MyLiteral {
		public string /* maybe better than char */	value;

		public MyCharLiteral( string text ) {
			value = text;
		}

		public override string ToString() {
			return "CharLit: " + value;
		}
	}

	public class MyIntegerLiteral : MyLiteral {
		public System.Numerics.BigInteger value;

		public MyIntegerLiteral( System.Numerics.BigInteger number ) {
			value = number;
		}

		public MyIntegerLiteral( string text ) {
			value = System.Numerics.BigInteger.Parse( text );
		}

		public override string ToString() {
//			return "IntLit: " + value;
			return value.ToString();
		}
	}

	class MyFloatingLiteral : MyLiteral {
		public double value;

		public MyFloatingLiteral( double number ) {
			value = number;
		}

		public MyFloatingLiteral( string text ) {
			value = System.Double.Parse( text );
		}

		public override string ToString() {
			return "FloatLit: " + value;
		}
	}
}
