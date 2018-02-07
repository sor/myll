using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanSordid.MyLang.Backend
{
	class MyId : MyExpression {
		public string value;

		public MyId( string id ) {
			value = id;
		}

		public override string ToString() {
			return "ID: " + value;
		}
	}

	class MyBinaryOperator : MyExpression {
		public string		operation;
		public MyExpression	left, right;

		public MyBinaryOperator( string op, MyExpression l, MyExpression r ) {
			operation = op;
			left = l;
			right = r;
		}

		public override string ToString() {
			return left.ToString() + " BINOP " + operation + " " + right.ToString();
		}
	}

	class MyParenExpr : MyExpression {
		public MyExpression expr;

		public MyParenExpr( MyExpression expr ) {
			this.expr = expr;
		}

		public override string ToString() {
			return "PARENS ( " + expr.ToString() + " ) ";
		}
	}

	class MySubscript : MyExpression {
		public MyExpression	target, index;

		public MySubscript( MyExpression target, MyExpression index ) {
			System.Diagnostics.Debug.Assert( target != null );
			System.Diagnostics.Debug.Assert( index != null );
			this.target = target;
			this.index = index;
		}

		public override string ToString() {
			return "SUBSCRIPT " + target.ToString() + "[ " + index.ToString() + " ]";
		}
	}

	// Split in Func || Meth ?
	class MyFuncMethCall : MyExpression {
		public MyExpression		target;
		public MyExpressions	paras;

		public MyFuncMethCall( MyExpression target, MyExpressions paras ) {
			this.target	= target;
			this.paras	= paras;
		}

		public override string ToString() {
			return "FUNCMETHCALL " + target.ToString() + "( " + paras.ToString() + " )";
		}
	}

	class MyPostOp : MyExpression {
		public MyExpression	target;
		public string		op;

		public MyPostOp( MyExpression target, string op ) {
			this.target = target;
			this.op = op;
		}

		public override string ToString() {
			return "POSTOP " + target.ToString() + op;
		}
	}

	class MyPreOp : MyExpression {
		public MyExpression	target;
		public string		op;

		public MyPreOp( MyExpression target, string op ) {
			this.target = target;
			this.op = op;
		}

		public override string ToString() {
			return "PREOP " + op + target.ToString();
		}
	}

	class MyNew : MyExpression {
		public MyType			type;
		public MyExpressions	paras;

		public MyNew( MyType type, MyExpressions paras ) {
			this.type = type;
			this.paras = paras;
		}

		public override string ToString() {
			return "NEW " + type.ToString() + "( " + paras.ToString() + " )";
		}
	}

	class MyDelete : MyExpression {
		public MyExpression	target;
		public bool			is_array;

		public MyDelete( MyExpression target, bool is_array ) {
			this.target = target;
			this.is_array = is_array;
		}

		public override string ToString() {
			return "DELETE " + (is_array?"[] ":"") + target.ToString();
		}
	}

	///////////////////////////////////// TODO

	class MyCast : MyExpression {
		public MyType		cast_type;
		public MyExpression	expr;
	}

	// Nothing here, this is the placeholder Class
	class MyNopExpr : MyExpression {
		public override string ToString() {
			return "NOP!";
		}
	}
}
