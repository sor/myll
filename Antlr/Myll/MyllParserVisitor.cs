//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from D:/documents/code/csharp/myll/Antlr\MyllParser.g4 by ANTLR 4.7

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
	/// Visit a parse tree produced by the <c>Tier1n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier1n([NotNull] MyllParser.Tier1nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier2n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier2n([NotNull] MyllParser.Tier2nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier3n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier3n([NotNull] MyllParser.Tier3nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier4n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier4n([NotNull] MyllParser.Tier4nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier4_5n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier4_5n([NotNull] MyllParser.Tier4_5nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier5n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier5n([NotNull] MyllParser.Tier5nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier6n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier6n([NotNull] MyllParser.Tier6nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier7n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier7n([NotNull] MyllParser.Tier7nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier8n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier8n([NotNull] MyllParser.Tier8nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier9n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier9n([NotNull] MyllParser.Tier9nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier10n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier10n([NotNull] MyllParser.Tier10nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier11n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier11n([NotNull] MyllParser.Tier11nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier12n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier12n([NotNull] MyllParser.Tier12nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier13n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier13n([NotNull] MyllParser.Tier13nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier14n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier14n([NotNull] MyllParser.Tier14nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier15n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier15n([NotNull] MyllParser.Tier15nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier16n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier16n([NotNull] MyllParser.Tier16nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ParenExprn</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParenExprn([NotNull] MyllParser.ParenExprnContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier50n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier50n([NotNull] MyllParser.Tier50nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier51n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier51n([NotNull] MyllParser.Tier51nContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier52n</c>
	/// labeled alternative in <see cref="MyllParser.exprNew"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier52n([NotNull] MyllParser.Tier52nContext context);
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
	/// Visit a parse tree produced by the <c>Tier4</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier4([NotNull] MyllParser.Tier4Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier5</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier5([NotNull] MyllParser.Tier5Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier6</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier6([NotNull] MyllParser.Tier6Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier7</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier7([NotNull] MyllParser.Tier7Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier8</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier8([NotNull] MyllParser.Tier8Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier9</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier9([NotNull] MyllParser.Tier9Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier15</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier15([NotNull] MyllParser.Tier15Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier16</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier16([NotNull] MyllParser.Tier16Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier13</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier13([NotNull] MyllParser.Tier13Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier14</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier14([NotNull] MyllParser.Tier14Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier11</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier11([NotNull] MyllParser.Tier11Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier12</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier12([NotNull] MyllParser.Tier12Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier10</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier10([NotNull] MyllParser.Tier10Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier51</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier51([NotNull] MyllParser.Tier51Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier52</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier52([NotNull] MyllParser.Tier52Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier50</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier50([NotNull] MyllParser.Tier50Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier4_5</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier4_5([NotNull] MyllParser.Tier4_5Context context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ParenExpr</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParenExpr([NotNull] MyllParser.ParenExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>Tier1</c>
	/// labeled alternative in <see cref="MyllParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTier1([NotNull] MyllParser.Tier1Context context);
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
	/// Visit a parse tree produced by the <c>BreakStmt</c>
	/// labeled alternative in <see cref="MyllParser.stmt"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBreakStmt([NotNull] MyllParser.BreakStmtContext context);
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
