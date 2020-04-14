//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.8
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from /home/sor/myll/backend/Grammar/MyllParser.g4 by ANTLR 4.8

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
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.8")]
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
	/// Visit a parse tree produced by <see cref="MyllParser.addOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAddOP([NotNull] MyllParser.AddOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.shiftOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitShiftOP([NotNull] MyllParser.ShiftOPContext context);
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
	/// Visit a parse tree produced by <see cref="MyllParser.nulCoalOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNulCoalOP([NotNull] MyllParser.NulCoalOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.memAccOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMemAccOP([NotNull] MyllParser.MemAccOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.memAccPtrOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMemAccPtrOP([NotNull] MyllParser.MemAccPtrOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.assignOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssignOP([NotNull] MyllParser.AssignOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.aggrAssignOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAggrAssignOP([NotNull] MyllParser.AggrAssignOPContext context);
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
	/// Visit a parse tree produced by <see cref="MyllParser.qual"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQual([NotNull] MyllParser.QualContext context);
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
	/// Visit a parse tree produced by <see cref="MyllParser.typespec"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypespec([NotNull] MyllParser.TypespecContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typespecBasic"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypespecBasic([NotNull] MyllParser.TypespecBasicContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typespecFunc"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypespecFunc([NotNull] MyllParser.TypespecFuncContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typespecNested"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypespecNested([NotNull] MyllParser.TypespecNestedContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typespecsNested"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypespecsNested([NotNull] MyllParser.TypespecsNestedContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.arg"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArg([NotNull] MyllParser.ArgContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.args"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArgs([NotNull] MyllParser.ArgsContext context);
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
	/// Visit a parse tree produced by <see cref="MyllParser.funcTypeDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncTypeDef([NotNull] MyllParser.FuncTypeDefContext context);
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
	/// Visit a parse tree produced by <see cref="MyllParser.threeWay"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitThreeWay([NotNull] MyllParser.ThreeWayContext context);
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
	/// Visit a parse tree produced by the <c>PostExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPostExpr([NotNull] MyllParser.PostExprContext context);
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
	/// Visit a parse tree produced by the <c>NullCoalesceExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNullCoalesceExpr([NotNull] MyllParser.NullCoalesceExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>OrExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOrExpr([NotNull] MyllParser.OrExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ThreeWayConditionalExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitThreeWayConditionalExpr([NotNull] MyllParser.ThreeWayConditionalExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>PreExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreExpr([NotNull] MyllParser.PreExprContext context);
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
	/// Visit a parse tree produced by the <c>ThrowExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitThrowExpr([NotNull] MyllParser.ThrowExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>LiteralExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLiteralExpr([NotNull] MyllParser.LiteralExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ScopedExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitScopedExpr([NotNull] MyllParser.ScopedExprContext context);
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
	/// Visit a parse tree produced by <see cref="MyllParser.idAccessor"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdAccessor([NotNull] MyllParser.IdAccessorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.idExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdExpr([NotNull] MyllParser.IdExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.idAccessors"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdAccessors([NotNull] MyllParser.IdAccessorsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.idExprs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdExprs([NotNull] MyllParser.IdExprsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.typedIdAcors"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypedIdAcors([NotNull] MyllParser.TypedIdAcorsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.attribId"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttribId([NotNull] MyllParser.AttribIdContext context);
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
	/// Visit a parse tree produced by <see cref="MyllParser.caseStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCaseStmt([NotNull] MyllParser.CaseStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.initList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInitList([NotNull] MyllParser.InitListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.funcBody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncBody([NotNull] MyllParser.FuncBodyContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.accessorDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAccessorDef([NotNull] MyllParser.AccessorDefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.funcDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncDef([NotNull] MyllParser.FuncDefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.opDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOpDef([NotNull] MyllParser.OpDefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.condThen"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCondThen([NotNull] MyllParser.CondThenContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Namespace</c>
	/// labeled alternative in <see cref="MyllParser.inDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNamespace([NotNull] MyllParser.NamespaceContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StructDecl</c>
	/// labeled alternative in <see cref="MyllParser.inDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStructDecl([NotNull] MyllParser.StructDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ConceptDecl</c>
	/// labeled alternative in <see cref="MyllParser.inDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitConceptDecl([NotNull] MyllParser.ConceptDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>EnumDecl</c>
	/// labeled alternative in <see cref="MyllParser.inDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumDecl([NotNull] MyllParser.EnumDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>FunctionDecl</c>
	/// labeled alternative in <see cref="MyllParser.inDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionDecl([NotNull] MyllParser.FunctionDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>OpDecl</c>
	/// labeled alternative in <see cref="MyllParser.inDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOpDecl([NotNull] MyllParser.OpDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AccessMod</c>
	/// labeled alternative in <see cref="MyllParser.inDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAccessMod([NotNull] MyllParser.AccessModContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>CtorDecl</c>
	/// labeled alternative in <see cref="MyllParser.inDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCtorDecl([NotNull] MyllParser.CtorDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>DtorDecl</c>
	/// labeled alternative in <see cref="MyllParser.inDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDtorDecl([NotNull] MyllParser.DtorDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Using</c>
	/// labeled alternative in <see cref="MyllParser.inAnyStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUsing([NotNull] MyllParser.UsingContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AliasDecl</c>
	/// labeled alternative in <see cref="MyllParser.inAnyStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAliasDecl([NotNull] MyllParser.AliasDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>VariableDecl</c>
	/// labeled alternative in <see cref="MyllParser.inAnyStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVariableDecl([NotNull] MyllParser.VariableDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>EmptyStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEmptyStmt([NotNull] MyllParser.EmptyStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>BlockStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBlockStmt([NotNull] MyllParser.BlockStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ReturnStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitReturnStmt([NotNull] MyllParser.ReturnStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ThrowStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitThrowStmt([NotNull] MyllParser.ThrowStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>BreakStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBreakStmt([NotNull] MyllParser.BreakStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>IfStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIfStmt([NotNull] MyllParser.IfStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SwitchStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSwitchStmt([NotNull] MyllParser.SwitchStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>LoopStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLoopStmt([NotNull] MyllParser.LoopStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ForStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForStmt([NotNull] MyllParser.ForStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>WhileStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitWhileStmt([NotNull] MyllParser.WhileStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>DoWhileStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDoWhileStmt([NotNull] MyllParser.DoWhileStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>TimesStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTimesStmt([NotNull] MyllParser.TimesStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>EachStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEachStmt([NotNull] MyllParser.EachStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>MultiAssignStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMultiAssignStmt([NotNull] MyllParser.MultiAssignStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AggrAssignStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAggrAssignStmt([NotNull] MyllParser.AggrAssignStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionStmt</c>
	/// labeled alternative in <see cref="MyllParser.inStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionStmt([NotNull] MyllParser.ExpressionStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AttribDeclBlock</c>
	/// labeled alternative in <see cref="MyllParser.levDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttribDeclBlock([NotNull] MyllParser.AttribDeclBlockContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AttribDecl</c>
	/// labeled alternative in <see cref="MyllParser.levDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttribDecl([NotNull] MyllParser.AttribDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AttribStmt</c>
	/// labeled alternative in <see cref="MyllParser.levStmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttribStmt([NotNull] MyllParser.AttribStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.module"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitModule([NotNull] MyllParser.ModuleContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.imports"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitImports([NotNull] MyllParser.ImportsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyllParser.prog"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitProg([NotNull] MyllParser.ProgContext context);
}
} // namespace Myll
