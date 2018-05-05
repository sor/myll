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

	public class IfStmt : Stmt
	{
		public Expr ifExpr;
		public Stmt thenBlock;
		public Stmt elseBlock;
	}

	public class FallStmt : Stmt
	{
		// This is the opposite of break in a case of a switch
	}

	public class Block : Stmt
	{
		public List<Stmt> statements;
	}
}