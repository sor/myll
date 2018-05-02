using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLang.Core
{
	class MyId : MyExpression {
		public string value;

		public MyId( string id ) {
			value = id;
		}

		public override string ToString() {
			//return "ID: " + value;
			return value;
		}
	}

	class BinOpExpr : MyExpression {
		public MyExpression		left, right;
		public string			operation;
		public readonly Assoc	assoc;

		public BinOpExpr( string op, MyExpression l, MyExpression r, Assoc a ) {
			operation = op;
			left = l;
			right = r;
			assoc = a;
		}

		public BinOpExpr( string op, MyExpression l, MyExpression r )
			: this( op, l, r, Assoc.Left ) { }

		IEnumerable<MyExpression> Down() {
			if( assoc == Assoc.Left )
			{
				yield return left;
				yield return right;
			}
			else
			{
				yield return right;
				yield return left;
			}
		}

		public override string ToString() {
#if EXPRESSION_DEBUG
			return string.Format( "{0} BINOP({2}) {1}", left.ToString(), right.ToString(), operation );
#else
			return string.Format( "{0} {2} {1}", left.ToString(), right.ToString(), operation );
#endif
		}
	}

	class ParenExpr : MyExpression {
		public MyExpression expr;

		public ParenExpr( MyExpression expr ) {
			this.expr = expr;
		}

		public override string ToString() {
#if EXPRESSION_DEBUG
			return "PARENS ( " + expr.ToString() + " ) ";
			return string.Format( "{0} BINOP({2}) {1}", left.ToString(), right.ToString(), operation );
#else
			//return "PARENS ( " + expr.ToString() + " ) ";
			return string.Format( "({0})", expr.ToString() );
#endif
		}
	}

	class SubscriptExpr : MyExpression {
		public MyExpression	target, index;

		public SubscriptExpr( MyExpression target, MyExpression index ) {
			System.Diagnostics.Debug.Assert( target != null );
			System.Diagnostics.Debug.Assert( index != null );
			this.target = target;
			this.index = index;
		}

		public override string ToString() {
//			return "SUBSCRIPT " + target.ToString() + "[ " + index.ToString() + " ]";
			return target.ToString() + "[" + index.ToString() + "]";
		}
	}

	// Split in Func || Meth ?
	class MyFuncMethCall : MyExpression {
		public MyExpression		target; // func name
		public MyExpressions	paras;

		public MyFuncMethCall( MyExpression target, MyExpressions paras ) {
			this.target	= target;
			this.paras	= paras;
		}

		public override string ToString() {
			//return "FUNCMETHCALL " + target.ToString() + "( " + paras.ToString() + " )";
			return target.ToString() + "( " + paras.ToString() + " )";
		}
	}

	class PostOpExpr : MyExpression {
		public MyExpression	target;
		public string		op;

		public PostOpExpr( MyExpression target, string op ) {
			this.target = target;
			this.op = op;
		}

		public override string ToString() {
			//return "POSTOP " + target.ToString() + op;
			return string.Format( "{0}{1}", target, op );
		}
	}

	class PreOpExpr : MyExpression {
		public MyExpression	target;
		public string		op;

		public PreOpExpr( MyExpression target, string op ) {
			this.target = target;
			this.op = op;
		}

		public override string ToString() {
			//return "PREOP " + op + target.ToString();
			return string.Format( "{1}{0}", target, op );
		}
	}

	class NewExpr : MyExpression {
		public MyType			type;
		public MyExpressions	paras;

		public NewExpr( MyType type, MyExpressions paras ) {
			this.type = type;
			this.paras = paras;
		}

		public override string ToString() {
			//return "NEW " + type.ToString() + "( " + paras.ToString() + " )";
			return string.Format( "new {0}({1})", type, paras );
		}
	}

	class DeleteExpr : MyExpression {
		public MyExpression	target;
		public bool			is_array;

		public DeleteExpr( MyExpression target, bool is_array ) {
			this.target = target;
			this.is_array = is_array;
		}

		public override string ToString() {
			//return "DELETE " + (is_array ? "[] " : "") + target.ToString();
			return string.Format( "delete{1} {0}", target, (is_array ? "[]" : "") );
		}
	}

	///////////////////////////////////// TODO

	class CastExpr : MyExpression {
		public MyType		cast_type;
		public MyExpression	expr;
	}

	// Nothing here, this is the placeholder Class
	class NopExpr : MyExpression {
		public override string ToString() {
			return "NOP!";
		}
	}
}
