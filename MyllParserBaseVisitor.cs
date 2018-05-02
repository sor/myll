namespace Myll
{
	using Antlr4.Runtime.Tree;

	public partial class MyllParserBaseVisitor<Result> : AbstractParseTreeVisitor<Result>, IMyllParserVisitor<Result>
	{
		protected MyllVisitor     vis;
		protected MyllExprVisitor exprVis;
		protected MyllStmtVisitor stmtVis;

		public void TellVisitors(MyllVisitor vis, MyllExprVisitor exprVis, MyllStmtVisitor stmtVis)
		{
			this.vis = vis;
			this.exprVis = exprVis;
			this.stmtVis = stmtVis;
		}
	}
}