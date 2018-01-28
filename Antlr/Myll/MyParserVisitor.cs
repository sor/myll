//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from D:/documents/code/csharp/myll/Antlr\MyParser.g4 by ANTLR 4.7

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
/// by <see cref="MyParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.7")]
[System.CLSCompliant(false)]
public interface IMyParserVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.postOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPostOP([NotNull] MyParser.PostOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.preOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreOP([NotNull] MyParser.PreOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.assignOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssignOP([NotNull] MyParser.AssignOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.powOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPowOP([NotNull] MyParser.PowOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.multOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMultOP([NotNull] MyParser.MultOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.addOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAddOP([NotNull] MyParser.AddOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.shiftOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitShiftOP([NotNull] MyParser.ShiftOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.bitAndOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBitAndOP([NotNull] MyParser.BitAndOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.bitXorOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBitXorOP([NotNull] MyParser.BitXorOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.bitOrOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBitOrOP([NotNull] MyParser.BitOrOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.andOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAndOP([NotNull] MyParser.AndOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.orOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOrOP([NotNull] MyParser.OrOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.memOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMemOP([NotNull] MyParser.MemOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.memPtrOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMemPtrOP([NotNull] MyParser.MemPtrOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.orderOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOrderOP([NotNull] MyParser.OrderOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.equalOP"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEqualOP([NotNull] MyParser.EqualOPContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.comment"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitComment([NotNull] MyParser.CommentContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.id"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitId([NotNull] MyParser.IdContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.anyId"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAnyId([NotNull] MyParser.AnyIdContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.idOrLit"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdOrLit([NotNull] MyParser.IdOrLitContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.charType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCharType([NotNull] MyParser.CharTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.floatingType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFloatingType([NotNull] MyParser.FloatingTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.binaryType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBinaryType([NotNull] MyParser.BinaryTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.integerType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIntegerType([NotNull] MyParser.IntegerTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.basicType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBasicType([NotNull] MyParser.BasicTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.typeQual"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypeQual([NotNull] MyParser.TypeQualContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.typeQuals"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypeQuals([NotNull] MyParser.TypeQualsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.typePtr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypePtr([NotNull] MyParser.TypePtrContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.nestedType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNestedType([NotNull] MyParser.NestedTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.funcType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncType([NotNull] MyParser.FuncTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.qualType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQualType([NotNull] MyParser.QualTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.qualTypeOrLit"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQualTypeOrLit([NotNull] MyParser.QualTypeOrLitContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.tplParams"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTplParams([NotNull] MyParser.TplParamsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.idTplType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdTplType([NotNull] MyParser.IdTplTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.preOpExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreOpExpr([NotNull] MyParser.PreOpExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.castExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCastExpr([NotNull] MyParser.CastExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.sizeofExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSizeofExpr([NotNull] MyParser.SizeofExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.newExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNewExpr([NotNull] MyParser.NewExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.deleteExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeleteExpr([NotNull] MyParser.DeleteExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.namedExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNamedExpr([NotNull] MyParser.NamedExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.namedExprs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNamedExprs([NotNull] MyParser.NamedExprsContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier2</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier2([NotNull] MyParser.Tier2Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier3</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier3([NotNull] MyParser.Tier3Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier4</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier4([NotNull] MyParser.Tier4Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier5</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier5([NotNull] MyParser.Tier5Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier6</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier6([NotNull] MyParser.Tier6Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier7</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier7([NotNull] MyParser.Tier7Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier8</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier8([NotNull] MyParser.Tier8Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier9</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier9([NotNull] MyParser.Tier9Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier15</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier15([NotNull] MyParser.Tier15Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier16</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier16([NotNull] MyParser.Tier16Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier13</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier13([NotNull] MyParser.Tier13Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier14</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier14([NotNull] MyParser.Tier14Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier200</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier200([NotNull] MyParser.Tier200Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier11</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier11([NotNull] MyParser.Tier11Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier12</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier12([NotNull] MyParser.Tier12Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier104</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier104([NotNull] MyParser.Tier104Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier10</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier10([NotNull] MyParser.Tier10Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier100</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier100([NotNull] MyParser.Tier100Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier4_5</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier4_5([NotNull] MyParser.Tier4_5Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ParenExpr</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParenExpr([NotNull] MyParser.ParenExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier1</c>
	/// labeled alternative in <see cref="MyParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier1([NotNull] MyParser.Tier1Context context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.exprs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExprs([NotNull] MyParser.ExprsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.tt_exp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTt_exp([NotNull] MyParser.Tt_expContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.idExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdExpr([NotNull] MyParser.IdExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.typedIdExprs"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypedIdExprs([NotNull] MyParser.TypedIdExprsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.attrib"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttrib([NotNull] MyParser.AttribContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.attribBlk"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttribBlk([NotNull] MyParser.AttribBlkContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Using</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUsing([NotNull] MyParser.UsingContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>VariableDecl</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVariableDecl([NotNull] MyParser.VariableDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>IfStmt</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIfStmt([NotNull] MyParser.IfStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ForStmt</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForStmt([NotNull] MyParser.ForStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>TimesStmt</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTimesStmt([NotNull] MyParser.TimesStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>EachStmt</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEachStmt([NotNull] MyParser.EachStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ReturnStmt</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitReturnStmt([NotNull] MyParser.ReturnStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>BreakStmt</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBreakStmt([NotNull] MyParser.BreakStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>BlockStmt</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBlockStmt([NotNull] MyParser.BlockStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionStmt</c>
	/// labeled alternative in <see cref="MyParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionStmt([NotNull] MyParser.ExpressionStmtContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.stmtBlk"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStmtBlk([NotNull] MyParser.StmtBlkContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AccessMod</c>
	/// labeled alternative in <see cref="MyParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAccessMod([NotNull] MyParser.AccessModContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ClassCtorDecl</c>
	/// labeled alternative in <see cref="MyParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassCtorDecl([NotNull] MyParser.ClassCtorDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Alias</c>
	/// labeled alternative in <see cref="MyParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAlias([NotNull] MyParser.AliasContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StaticDecl</c>
	/// labeled alternative in <see cref="MyParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStaticDecl([NotNull] MyParser.StaticDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ClassExtendedDecl</c>
	/// labeled alternative in <see cref="MyParser.classDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassExtendedDecl([NotNull] MyParser.ClassExtendedDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.classExtDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassExtDef([NotNull] MyParser.ClassExtDefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.argList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArgList([NotNull] MyParser.ArgListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.initList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInitList([NotNull] MyParser.InitListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.param"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParam([NotNull] MyParser.ParamContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.params"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParams([NotNull] MyParser.ParamsContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>CtorDef</c>
	/// labeled alternative in <see cref="MyParser.ctorDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCtorDef([NotNull] MyParser.CtorDefContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>FuncMeth</c>
	/// labeled alternative in <see cref="MyParser.funcDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFuncMeth([NotNull] MyParser.FuncMethContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>OperatorDecl</c>
	/// labeled alternative in <see cref="MyParser.opDecl"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOperatorDecl([NotNull] MyParser.OperatorDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Attributes</c>
	/// labeled alternative in <see cref="MyParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAttributes([NotNull] MyParser.AttributesContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Namespace</c>
	/// labeled alternative in <see cref="MyParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNamespace([NotNull] MyParser.NamespaceContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ClassDecl</c>
	/// labeled alternative in <see cref="MyParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassDecl([NotNull] MyParser.ClassDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StructDecl</c>
	/// labeled alternative in <see cref="MyParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStructDecl([NotNull] MyParser.StructDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>UnionDecl</c>
	/// labeled alternative in <see cref="MyParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUnionDecl([NotNull] MyParser.UnionDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>EnumDecl</c>
	/// labeled alternative in <see cref="MyParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumDecl([NotNull] MyParser.EnumDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>FunctionDecl</c>
	/// labeled alternative in <see cref="MyParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionDecl([NotNull] MyParser.FunctionDeclContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>rest</c>
	/// labeled alternative in <see cref="MyParser.topLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRest([NotNull] MyParser.RestContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="MyParser.prog"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitProg([NotNull] MyParser.ProgContext context);
}
} // namespace Myll
