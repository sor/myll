namespace Myll.Core
{
	public class Stmt
	{
		
	}

	public class IfStmt : Stmt
	{
		public Expr ifExpr;
		public Stmt thenBlock;
		public Stmt elseBlock;
	}
}