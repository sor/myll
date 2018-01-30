using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanSordid.MyLang
{
	abstract public class MyBase { }
	abstract public class MyStatement : MyBase { }
	abstract public class MyExpression : MyStatement { }

	public class MyStatements : MyBase
	{
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

	class MyReturnStmt : MyStatement
	{
		public MyExpression	expr;

		public override string ToString() { return "RETURN: " + expr;  }
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

		public override string ToString() {
			return "IF: " + expr + " THEN: " + if_statement + " ELSE " + (else_statement == null ? "NOTHING" : else_statement.ToString());
		}
	}

	class MyTimesStmt : MyStatement {
		public MyExpression	expr;
		public MyStatement	statement;

		public MyTimesStmt( MyExpression ex, MyStatement ts )
		{
			expr		= ex;
			statement	= ts;
		}

		public override string ToString() {
			return "TIMES: " + expr + " DO: " + statement;
		}
	}

	class MyBlockStatement : MyStatement
	{
		public MyStatements list = new MyStatements();

		public override string ToString() { return "BLOCK { " + list + " } "; }
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

	// Expressions

	abstract class MyLiteral : MyExpression { }

	class MyStringLiteral : MyLiteral
	{
		public string value;
		
		public MyStringLiteral( string text )
		{
			value = text;
		}

		public override string ToString()
		{
			return "StringLit: " + value;
		}
	}

	class MyCharLiteral : MyLiteral
	{
		public string /* maybe better than char */	value;

		public MyCharLiteral( string text )
		{
			value = text;
		}

		public override string ToString()
		{
			return "CharLit: " + value;
		}
	}

	class MyIntegerLiteral : MyLiteral
	{
		public System.Numerics.BigInteger value;

		public MyIntegerLiteral( System.Numerics.BigInteger number )
		{
			value = number;
		}

		public MyIntegerLiteral( string text )
		{
			value = System.Numerics.BigInteger.Parse( text );
		}

		public override string ToString()
		{
			return "IntLit: " + value;
		}
	}

	class MyFloatingLiteral : MyLiteral
	{
		public double value;

		public MyFloatingLiteral( double number )
		{
			value = number;
		}

		public MyFloatingLiteral( string text )
		{
			value = System.Double.Parse( text );
		}

		public override string ToString()
		{
			return "FloatLit: " + value;
		}
	}

	

	
}
