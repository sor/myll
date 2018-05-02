using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MyLang.Core;

namespace MyLang.Core
{
	public class MyStatements : IBase {
		public List<MyStatement> list;
		public MyStatements()				{ list = new List<MyStatement>();			}
		public MyStatements( int capacity )	{ list = new List<MyStatement>( capacity );	}
		public void Add( MyStatement s )	{ list.Add( s ); }
		public override string ToString()	{ return string.Join( "\n", list.Select( q => q.ToString() ) ); }
	}

/* statement: 'return'	expr SEMI											# ReturnStmt
			| 'break'	SEMI												# BreakStmt
			| 'if'		LPAREN expr RPAREN statement ( 'else' statement )?	# IfStmt
			| 'var'		field_expr SEMI										# VariableDecl
			| expr SEMI														# ExpressionStmt
			| LCURLY statement* RCURLY										# BlockStmt
			;*/

	class MyReturnStmt : MyStatement {
		public MyExpression	expr;

		public override string ToString() { return "return " + expr + ";";  }
	}

	class MyBreakStmt : MyStatement {}

	class MyIfStmt : MyStatement {
		public MyExpression	expr;
		public MyStatement	if_statement;
		public MyStatement	else_statement;

		public MyIfStmt( MyExpression ex, MyStatement ifs, MyStatement elses ) {
			expr			= ex;
			if_statement	= ifs;
			else_statement	= elses;
		}

		public override IEnumerable<string> ToStringList( string I = "  " ) {
			var ret = new List<string>();
			ret.Add(		string.Format( I + "if( {0} )",	expr )	);
			ret.AddRange(	if_statement.ToStringList( I ).Select( q => I + q )	);
			if( else_statement != null )
			{
				ret.Add(		string.Format( I + "else" )								);
				ret.AddRange(	else_statement.ToStringList( I ).Select( q => I + q )	);
			}
			return ret;
			//return "IF: " + expr + " THEN: " + if_statement + " ELSE " + (else_statement == null ? "NOTHING" : else_statement.ToString());
		}
	}

	class AssignmentStmt : MyStatement {
		public MyExpressions exprs = new MyExpressions();
		public List<string> ops = new List<string>();
	}

	class MyTimesStmt : MyStatement {
		public MyExpression	expr;
		public MyStatement	statement;
		public MyId			identifier;

		public MyTimesStmt( MyExpression ex, MyStatement ts, MyId id ) {
			expr		= ex;
			statement	= ts;
			identifier	= id;
		}

		public override string ToString() {
			return "TIMES: " + expr +
				" ID: " + (identifier == null ? "automatic" : identifier.ToString()) +
				" DO: " + statement;
		}

		public override IEnumerable<string> ToStringList( string I = "  " ) {
			var ret = new List<string>();
			string var_name;
			if( identifier != null )
				var_name = identifier.value;
			else
				var_name = "i"; // TODO: get unique name for loop variable

			ret.Add( I + "for( int " + var_name + " = 0; " + var_name + " < " + expr + "; ++" + var_name + " )" );
			ret.AddRange( statement.ToStringList( I ).Select( q => I + q ) );
			return ret;
		}
	}

	class MyBlockStatement : MyStatement {
		public MyStatements list = new MyStatements();

		public override string ToString() { return "BLOCK { " + list + " } "; }
		public override IEnumerable<string> ToStringList( string I = "  " ) {
			var ret = new List<string>();
			ret.Add( "{" );
			ret.AddRange( list.list.SelectMany( q => q.ToStringList( I ) ) );
			ret.Add( "}" );
			return ret;
		}
	}

/*  expr: LPAREN 's' anyType RPAREN expr	# StaticCastExpr
		| expr	op=DBL_STAR			expr	# Pow
		|		SQRT				expr	# Sqrt
		| expr	op=(STAR|SLASH)		expr	# MulDiv
		| expr	op=(PLUS|MINUS)		expr	# AddSub
		| expr	op=LSHIFT			expr	# LShift
		| expr	('>''>' )			expr	# RShift
		| idOrLit							# IdOrLitExpr
		| '(' expr ')'						# ParensExpr
		| 'nop'								# NopExpr
		;*/

	class MyExpressionStatement : MyStatement
	{
		public MyExpression expr;

		public MyExpressionStatement( MyExpression ex )
		{
			expr = ex;
		}

		public override string ToString()
		{
			return expr.ToString() + ";";
		}
	}
}
