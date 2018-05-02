//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from /home/sor/myll/Antlr/MyllParser.g4 by ANTLR 4.7

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace Myll {
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="MyllParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.7")]
[System.CLSCompliant(false)]
public interface IMyllParserVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.comment"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitComment([NotNull] MyllParser.CommentContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.postOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPostOP([NotNull] MyllParser.PostOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.preOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreOP([NotNull] MyllParser.PreOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.assignOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssignOP([NotNull] MyllParser.AssignOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.powOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPowOP([NotNull] MyllParser.PowOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.multOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMultOP([NotNull] MyllParser.MultOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.multOPn"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMultOPn([NotNull] MyllParser.MultOPnContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.addOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAddOP([NotNull] MyllParser.AddOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.addOPn"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAddOPn([NotNull] MyllParser.AddOPnContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.shiftOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitShiftOP([NotNull] MyllParser.ShiftOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.bitAndOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBitAndOP([NotNull] MyllParser.BitAndOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.bitXorOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBitXorOP([NotNull] MyllParser.BitXorOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.bitOrOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBitOrOP([NotNull] MyllParser.BitOrOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.andOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAndOP([NotNull] MyllParser.AndOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.orOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOrOP([NotNull] MyllParser.OrOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.memOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMemOP([NotNull] MyllParser.MemOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.memPtrOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMemPtrOP([NotNull] MyllParser.MemPtrOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.cmpOp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCmpOp([NotNull] MyllParser.CmpOpContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.orderOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOrderOP([NotNull] MyllParser.OrderOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.equalOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEqualOP([NotNull] MyllParser.EqualOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.lit"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLit([NotNull] MyllParser.LitContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.wildId"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitWildId([NotNull] MyllParser.WildIdContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.id"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitId([NotNull] MyllParser.IdContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.idOrLit"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdOrLit([NotNull] MyllParser.IdOrLitContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.specialType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSpecialType([NotNull] MyllParser.SpecialTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.charType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCharType([NotNull] MyllParser.CharTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.floatingType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFloatingType([NotNull] MyllParser.FloatingTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.binaryType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBinaryType([NotNull] MyllParser.BinaryTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.signedIntType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSignedIntType([NotNull] MyllParser.SignedIntTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.unsignIntType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUnsignIntType([NotNull] MyllParser.UnsignIntTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.basicType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBasicType([NotNull] MyllParser.BasicTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typeQual"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypeQual([NotNull] MyllParser.TypeQualContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typeQuals"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypeQuals([NotNull] MyllParser.TypeQualsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typePtr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypePtr([NotNull] MyllParser.TypePtrContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.idTplArgs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdTplArgs([NotNull] MyllParser.IdTplArgsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.nestedType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNestedType([NotNull] MyllParser.NestedTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.funcType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncType([NotNull] MyllParser.FuncTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typeSpec"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypeSpec([NotNull] MyllParser.TypeSpecContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.arg"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArg([NotNull] MyllParser.ArgContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.funcCall"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncCall([NotNull] MyllParser.FuncCallContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.indexCall"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIndexCall([NotNull] MyllParser.IndexCallContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.param"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParam([NotNull] MyllParser.ParamContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.funcDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncDef([NotNull] MyllParser.FuncDefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.tplArg"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTplArg([NotNull] MyllParser.TplArgContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.tplArgs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTplArgs([NotNull] MyllParser.TplArgsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.tplParams"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTplParams([NotNull] MyllParser.TplParamsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.preOpExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreOpExpr([NotNull] MyllParser.PreOpExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.castExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCastExpr([NotNull] MyllParser.CastExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.sizeofExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSizeofExpr([NotNull] MyllParser.SizeofExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.newExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewExpr([NotNull] MyllParser.NewExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.deleteExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeleteExpr([NotNull] MyllParser.DeleteExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AndExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAndExpr([NotNull] MyllParser.AndExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier2</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier2([NotNull] MyllParser.Tier2Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier3</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier3([NotNull] MyllParser.Tier3Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ComparisonExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitComparisonExpr([NotNull] MyllParser.ComparisonExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>PowExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPowExpr([NotNull] MyllParser.PowExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>MultExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMultExpr([NotNull] MyllParser.MultExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AddExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAddExpr([NotNull] MyllParser.AddExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>RelationExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRelationExpr([NotNull] MyllParser.RelationExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ConditionalExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitConditionalExpr([NotNull] MyllParser.ConditionalExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>MemPtrExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMemPtrExpr([NotNull] MyllParser.MemPtrExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ScopeExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitScopeExpr([NotNull] MyllParser.ScopeExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>OrExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOrExpr([NotNull] MyllParser.OrExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>WildIdExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitWildIdExpr([NotNull] MyllParser.WildIdExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>EqualityExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEqualityExpr([NotNull] MyllParser.EqualityExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>LiteralExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLiteralExpr([NotNull] MyllParser.LiteralExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ParenExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParenExpr([NotNull] MyllParser.ParenExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>IdTplExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdTplExpr([NotNull] MyllParser.IdTplExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ShiftExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitShiftExpr([NotNull] MyllParser.ShiftExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.idExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdExpr([NotNull] MyllParser.IdExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typedIdExprs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypedIdExprs([NotNull] MyllParser.TypedIdExprsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.attrib"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttrib([NotNull] MyllParser.AttribContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.attribBlk"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttribBlk([NotNull] MyllParser.AttribBlkContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Using</c>
	/// labeled alternative in <see cref="MyllParser.stmtDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUsing([NotNull] MyllParser.UsingContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>VariableDecl</c>
	/// labeled alternative in <see cref="MyllParser.stmtDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVariableDecl([NotNull] MyllParser.VariableDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StmtDecl</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmtDecl([NotNull] MyllParser.StmtDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ReturnStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitReturnStmt([NotNull] MyllParser.ReturnStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ThrowStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitThrowStmt([NotNull] MyllParser.ThrowStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>BreakStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBreakStmt([NotNull] MyllParser.BreakStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>FallStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFallStmt([NotNull] MyllParser.FallStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>IfStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIfStmt([NotNull] MyllParser.IfStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ForStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForStmt([NotNull] MyllParser.ForStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>TimesStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTimesStmt([NotNull] MyllParser.TimesStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>EachStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEachStmt([NotNull] MyllParser.EachStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AssignmentStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssignmentStmt([NotNull] MyllParser.AssignmentStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>BlockStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBlockStmt([NotNull] MyllParser.BlockStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionStmt([NotNull] MyllParser.ExpressionStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.stmtBlk"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmtBlk([NotNull] MyllParser.StmtBlkContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AccessMod</c>
	/// labeled alternative in <see cref="MyllParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAccessMod([NotNull] MyllParser.AccessModContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ClassCtorDecl</c>
	/// labeled alternative in <see cref="MyllParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassCtorDecl([NotNull] MyllParser.ClassCtorDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Alias</c>
	/// labeled alternative in <see cref="MyllParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAlias([NotNull] MyllParser.AliasContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StaticDecl</c>
	/// labeled alternative in <see cref="MyllParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStaticDecl([NotNull] MyllParser.StaticDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ClassExtendedDecl</c>
	/// labeled alternative in <see cref="MyllParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassExtendedDecl([NotNull] MyllParser.ClassExtendedDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.classExtDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassExtDef([NotNull] MyllParser.ClassExtDefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.initList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInitList([NotNull] MyllParser.InitListContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>CtorDef</c>
	/// labeled alternative in <see cref="MyllParser.ctorDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCtorDef([NotNull] MyllParser.CtorDefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.funcDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncDecl([NotNull] MyllParser.FuncDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.opDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOpDecl([NotNull] MyllParser.OpDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Attributes</c>
	/// labeled alternative in <see cref="MyllParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttributes([NotNull] MyllParser.AttributesContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Namespace</c>
	/// labeled alternative in <see cref="MyllParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNamespace([NotNull] MyllParser.NamespaceContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ClassDecl</c>
	/// labeled alternative in <see cref="MyllParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassDecl([NotNull] MyllParser.ClassDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StructDecl</c>
	/// labeled alternative in <see cref="MyllParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStructDecl([NotNull] MyllParser.StructDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>UnionDecl</c>
	/// labeled alternative in <see cref="MyllParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUnionDecl([NotNull] MyllParser.UnionDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>EnumDecl</c>
	/// labeled alternative in <see cref="MyllParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumDecl([NotNull] MyllParser.EnumDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>FunctionDecl</c>
	/// labeled alternative in <see cref="MyllParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionDecl([NotNull] MyllParser.FunctionDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>restStmt</c>
	/// labeled alternative in <see cref="MyllParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRestStmt([NotNull] MyllParser.RestStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.prog"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitProg([NotNull] MyllParser.ProgContext context);
}
} // namespace Myll
