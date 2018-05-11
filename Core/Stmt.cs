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
			foreach (var info in GetType().GetProperties())
			{
				var value = info.GetValue(this, null) ?? "(null)";
				sb.Append(info.Name + ": " + value.ToString() + ", ");
			}
			sb.Length = Math.Max(sb.Length - 2, 0);
			return "{"
			       + GetType().Name + " "
			       + sb.ToString()  + "}";
		}
	}

	public class ReturnStmt : Stmt
	{
		public Expr expr;
	}

	public class ThrowStmt : Stmt
	{
		public Expr expr;
	}

	public class BreakStmt : Stmt
	{}

	public class FallStmt : Stmt
	{}

	public class IfStmt : Stmt
	{
		public Expr ifExpr;
		public Stmt thenStmt;
		public Stmt elseStmt;	// opt
	}

	public class ForStmt : Stmt
	{
		public Stmt initStmt;
		public Expr condExpr;
		public Expr iterExpr;
		public Stmt loopStmt;
		public Stmt elseStmt;	// opt
	}

	public class Block : Stmt
	{
		public List<Stmt> statements;
	}
}
