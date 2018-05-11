namespace Myll
{
	using Antlr4.Runtime.Tree;

	public partial class MyllParserBaseVisitor<Result> : AbstractParseTreeVisitor<Result>, IMyllParserVisitor<Result>
	{
		protected Visitor AllVis => VisitorExtensions.AllVis;
	}
}