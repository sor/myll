using Antlr4.Runtime.Tree;

namespace Myll
{
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{}

	public partial class MyllParserBaseVisitor<Result>
		: AbstractParseTreeVisitor<Result>, IMyllParserVisitor<Result>
	{
		// TODO: all 'new'ed methods could be in here and then available in Decl, Stmt, Expr
		// sometimes the problem is that the methods are already in here, therefore ExtendedVisitor is necessary
		//protected Visitor AllVis => VisitorExtensions.AllVis;
	}
}
