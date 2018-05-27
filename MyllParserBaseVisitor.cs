namespace Myll
{
	using Antlr4.Runtime.Tree;

	public partial class MyllParserBaseVisitor<Result>
		: AbstractParseTreeVisitor<Result>, IMyllParserVisitor<Result>
	{
		// TODO: all 'new'ed methods could be in here and then available in Decl, Stmt, Expr
		protected Visitor AllVis => VisitorExtensions.AllVis;
	}
}
