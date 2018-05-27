using System;
using System.Collections.Generic;
using System.Text;

namespace Myll.Core
{
	public class Stmt
	{
		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach( var info in GetType().GetProperties() ) {
				var value = info.GetValue( this, null ) ?? "(null)";
				sb.Append( info.Name + ": " + value.ToString() + ", " );
			}
			sb.Length = Math.Max( sb.Length - 2, 0 );
			return "{"
			       + GetType().Name + " "
			       + sb.ToString()  + "}";
		}
	}

	public class UsingStmt : Stmt
	{
		public List<TypespecNested> types;
	}

	public class VarsStmt : Stmt
	{
		public List<Var> vars;
	}

	public class ReturnStmt : Stmt
	{
		public Expr expr; // opt
	}

	public class ThrowStmt : Stmt
	{
		public Expr expr;
	}

	public class BreakStmt : Stmt
	{
		public int depth;
	}

	public class IfStmt : Stmt
	{
		public Expr condExpr;
		public Stmt thenStmt;
		public Stmt elseStmt; // opt
	}

	public class CaseStmt // not a Stmt?
	{
		public List<Expr> caseExprs;
		public List<Stmt> bodyStmts;
		public bool       autoBreak;
	}

	public class SwitchStmt : Stmt
	{
		public Expr           condExpr;
		public List<CaseStmt> caseStmts;
		public List<Stmt>     elseStmts; // opt
	}

	public class LoopStmt : Stmt
	{
		public Stmt bodyStmt;
	}

	public class ForStmt : LoopStmt
	{
		public Stmt initStmt;
		public Expr condExpr;
		public Expr iterExpr;
		public Stmt elseStmt; // opt
	}

	public class WhileStmt : LoopStmt
	{
		public Expr condExpr;
		public Stmt elseStmt; // opt
	}

	public class DoWhileStmt : LoopStmt
	{
		public Expr condExpr;
	}

	public class TimesStmt : LoopStmt
	{
		public Expr   countExpr;
		public string name; // opt
	}

	public class EachStmt : LoopStmt
	{
		public Expr   fromExpr;
		public Expr   toExpr;
		public string name; // opt
	}

	public class AssignStmt : Stmt
	{
		public Operand op;
	}

	public class MultiAssignStmt : AssignStmt
	{
		public List<Expr> exprs;
	}

	public class AggrAssignStmt : AssignStmt
	{
		public Expr leftExpr;
		public Expr rightExpr;
	}

	public class Block : Stmt
	{
		public List<Stmt> statements;
	}

	public class ExprStmt : Stmt
	{
		public Expr expr;
	}

	public class EmptyStmt : Stmt
	{}
}
