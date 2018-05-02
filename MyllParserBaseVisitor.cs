namespace Myll
{
	using Antlr4.Runtime.Tree;

	public partial class MyllParserBaseVisitor<Result> : AbstractParseTreeVisitor<Result>, IMyllParserVisitor<Result>
	{
		protected Visitor     vis;
		protected ExprVisitor exprVis;
		protected StmtVisitor stmtVis;

		public void TellVisitors(Visitor vis, ExprVisitor exprVis, StmtVisitor stmtVis)
		{
			this.vis = vis;
			this.exprVis = exprVis;
			this.stmtVis = stmtVis;
		}
	}
}