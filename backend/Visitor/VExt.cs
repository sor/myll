using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Myll.Core;

using Parser = Myll.MyllParser;

namespace Myll
{
	using static MyllParser;

	using Attribs = Dictionary<string, List<string>>;

	public partial class MyllParserBaseVisitor<Result>
		: AbstractParseTreeVisitor<Result>, IMyllParserVisitor<Result>
	{
		// TODO: all 'new'ed methods could be in here and then available in Decl, Stmt, Expr
		// sometimes the problem is that the methods are already in here, therefore ExtendedVisitor is necessary
		//protected Visitor AllVis => VisitorExtensions.AllVis;
	}

	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{
		protected new IEnumerable<Arg> VisitArgs( ArgsContext cs )
			=> cs?.arg().Select(
				   c => new Arg {
					   name = c.id().Visit(),
					   expr = c.expr().Visit(),
				   } )
			?? Enumerable.Empty<Arg>();

		protected new FuncCall VisitIndexCall( IndexCallContext c )
			=> new() {
				args     = VisitArgs( c.args() ).ToList(),
				nullCoal = c.ary.Type == QM_LBRACK,
				indexer  = true,
			};

		protected new FuncCall VisitFuncCall( FuncCallContext c )
			=> new() {
				args     = VisitArgs( c?.args() ).ToList(),
				nullCoal = c?.ary.Type == QM_LPAREN,
				indexer  = false,
			};

		protected new IEnumerable<Param> VisitFuncTypeDef( FuncTypeDefContext cs )
			=> cs.param().Select(
				c => new Param {
					name = c.id().Visit(),
					type = VisitTypespec( c.typespec() )
				} );

		protected IEnumerable<EnumEntry> VisitEnumEntrys( IdExprsContext cs )
			=> cs.idExpr().Select(
				c => new EnumEntry {
					srcPos = c.ToSrcPos(),
					name   = c.id().Visit(),
					value  = c.expr()?.Visit(),
				} );
	}

	public static class VisitorExtensions
	{
		// HACK these must become non-static
		private static readonly Stack<Scope> ScopeStack = new();
		private static readonly ExprVisitor  ExprVis    = new( ScopeStack );
		private static readonly StmtVisitor  StmtVis    = new( ScopeStack );
		public static readonly  DeclVisitor  DeclVis    = new( ScopeStack );

		private static readonly Dictionary<int, Operand>
			ToPreOperand = new() {
				{ Parser.DBL_PLUS,	Operand.PreIncr },
				{ Parser.DBL_MINUS,	Operand.PreDecr },
				{ Parser.PLUS,		Operand.PrePlus },
				{ Parser.MINUS,		Operand.PreMinus },
				{ Parser.EM,		Operand.Negation },
				{ Parser.TILDE,		Operand.Complement },
				{ Parser.STAR,		Operand.Dereference },
				{ Parser.AMP,		Operand.AddressOf },
			};

		private static readonly Dictionary<int, Operand>
			ToOperand = new() {
				{ Parser.DBL_PLUS,	Operand.PostIncr },
				{ Parser.DBL_MINUS,	Operand.PostDecr },
				{ Parser.LPAREN,	Operand.FuncCall },
				{ Parser.QM_LPAREN,	Operand.NCFuncCall },
				{ Parser.LBRACK,	Operand.IndexCall },
				{ Parser.QM_LBRACK,	Operand.NCIndexCall },
				{ Parser.POINT,		Operand.MemberAccess },
				{ Parser.QM_POINT,	Operand.NCMemberAccess },
				{ Parser.RARROW,	Operand.MemberPtrAccess },
				{ Parser.POINT_STAR, Operand.MemberAccessPtr },
				{ Parser.QM_POINT_STAR, Operand.NCMemberAccessPtr },
				{ Parser.ARROW_STAR, Operand.MemberPtrAccessPtr },
				//	{Parser.DBL_STAR,	Operand.Pow},
				{ Parser.STAR,		Operand.Multiply },
				{ Parser.SLASH,		Operand.EuclideanDivide },
				{ Parser.MOD,		Operand.Modulo },
				{ Parser.DOT,		Operand.Dot },
				{ Parser.CROSS,		Operand.Cross },
				{ Parser.DIV,		Operand.Divide },
				{ Parser.PLUS,		Operand.Add },
				{ Parser.MINUS,		Operand.Subtract },
				{ Parser.AMP,		Operand.BitAnd },
				{ Parser.HAT,		Operand.BitXor },
				{ Parser.PIPE,		Operand.BitOr },
				{ Parser.COMPARE,	Operand.Comparison },
				{ Parser.LT,		Operand.LessThan },
				{ Parser.LTEQ,		Operand.LessEqual },
				{ Parser.GT,		Operand.GreaterThan },
				{ Parser.GTEQ,		Operand.GreaterEqual },
				{ Parser.EQ,		Operand.Equal },
				{ Parser.NEQ,		Operand.NotEqual },
				{ Parser.DBL_AMP,	Operand.And },
				{ Parser.DBL_PIPE,	Operand.Or },
				{ Parser.ASSIGN,	Operand.Equal },
				{ Parser.AS_POW,	Operand.Pow },
				{ Parser.AS_MUL,	Operand.Multiply },
				{ Parser.AS_SLASH,	Operand.EuclideanDivide },
				{ Parser.AS_MOD,	Operand.Modulo },
				{ Parser.AS_DOT,	Operand.Dot },
				{ Parser.AS_CROSS,	Operand.Cross },
				{ Parser.AS_DIV,	Operand.Divide },
				{ Parser.AS_ADD,	Operand.Add },
				{ Parser.AS_SUB,	Operand.Subtract },
				{ Parser.AS_LSH,	Operand.LeftShift },
				{ Parser.AS_RSH,	Operand.RightShift },
				{ Parser.AS_AND,	Operand.BitAnd },
				{ Parser.AS_OR,		Operand.BitOr },
				{ Parser.AS_XOR,	Operand.BitXor },
			};

		[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		public static void Exec<T>( this IEnumerable<T> s )
		{
			if( s == null ) return;
			using( IEnumerator<T> e = s.GetEnumerator() )
				while( e.MoveNext() ) {}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		public static void ForAll<T>( this IEnumerable<T> s, Action<T> action )
		{
			foreach( T item in s )
				action( item );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		public static int ToInt( this ITerminalNode o )
			=> int.Parse( o.GetText() );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Operand ToPreOp( this IToken tok )
			=> ToPreOperand[tok.Type];

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Operand ToOp( this IToken tok )
			=> ToOperand[tok.Type];

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static SrcPos ToSrcPos( this ParserRuleContext c )
			=> new() {
				file = c.Start.InputStream.SourceName,
				from = {
					line = c.Start.Line,
					col  = c.Start.Column,
				},
				to = {
					line = c.Stop.Line,
					col  = c.Stop.Column,
				},
			};

		// this will become more specialized most likely, don't depend on current behavior
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static string Visit( this Parser.IdContext c )
			=> c?.GetText();

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static List<string> Visit( this Parser.IdContext[] c )
			=> c.Select( i => i.Visit() ).ToList();

		// this will become more specialized most likely, don't depend on current behavior
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static string Visit( this Parser.IdOrLitContext c )
			=> c?.GetText();

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static List<string> Visit( this Parser.IdOrLitContext[] c )
			=> c.Select( i => i.Visit() ).ToList();

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Expr Visit( this Parser.ExprContext c )
		{
			if( c == null )
				throw new ArgumentNullException();

			return c == null
				? null
				: ExprVis.Visit( c );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Literal Visit( this Parser.LitContext c )
			=> ExprVis.VisitLit( c );

		// TODO those null tolerant methods need to be removed
		[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		public static Stmt Visit( this Parser.LevStmtContext c )
			=> c == null
				? null
				: StmtVis.Visit( c );

		// TODO those null tolerant methods need to be removed
		[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		public static Block Visit( this Parser.FuncBodyContext c )
			=> c == null
				? null
				: StmtVis.VisitFuncBody( c );

		//[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		//public static Block VisitBlockify( this ParserRuleContext c )
		//	=> StmtVis.VisitBlockify( c );

		// TODO those null tolerant methods need to be removed
		[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
		public static Decl Visit( this Parser.LevDeclContext c )
			=> c == null
				? null
				: DeclVis.Visit( c );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Qualifier Visit( this Parser.QualContext[] c )
			=> c.Aggregate( Qualifier.None, ( a, q ) => a | q.Visit() );


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Accessor Visit( this Parser.AccessorDefContext c )
			=> new() {
				body = c.funcBody().Visit(),
				qual = c.qual().Visit(),
				kind = c.v.ToAccessorKind(),
			};

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static List<Accessor> Visit( this Parser.AccessorDefContext[] c )
			=> c.Select( Visit ).ToList();

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Attribs Visit( this Parser.AttribBlkContext c )
		{
			Attribs attribs = c.attrib()
				.ToDictionary(
					a => a.attribId().GetText(),
					a => a.idOrLit().Visit() );

			return attribs;
		}

#pragma warning disable 8509

		// To____Kind

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Accessor.Kind ToAccessorKind( this IToken tok )
			=> tok.Type switch {
				Parser.GET    => Accessor.Kind.Get,
				Parser.REFGET => Accessor.Kind.RefGet,
				Parser.SET    => Accessor.Kind.Set,
				_             => throw new ArgumentOutOfRangeException( "ToAccessorKind out of range " + tok )
			};

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Structural.Kind ToStructuralKind( this IToken tok )
			=> tok.Type switch {
				Parser.STRUCT => Structural.Kind.Struct,
				Parser.CLASS  => Structural.Kind.Class,
				Parser.UNION  => Structural.Kind.Union,
				_             => throw new ArgumentOutOfRangeException( "ToStructuralKind out of range " + tok )
			};

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Qualifier ToQualifier( this IToken tok )
			=> tok.Type switch {
				Parser.VAR   => Qualifier.None,
				Parser.FIELD => Qualifier.None,
				Parser.CONST => Qualifier.Const,
				Parser.LET   => Qualifier.Const,
				_            => throw new ArgumentOutOfRangeException( "ToQualifier out of range " + tok )
			};

		// Visit

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Qualifier Visit( this Parser.QualContext c )
			=> c.v.Type switch {
				MyllParser.CONST    => Qualifier.Const,
				MyllParser.MUTABLE  => Qualifier.Mutable,
				MyllParser.VOLATILE => Qualifier.Volatile,
				MyllParser.STABLE   => Qualifier.Stable,
				_                   => throw new ArgumentOutOfRangeException( "Visit Qualifier out of range " + c )
			};

#pragma warning restore 8509
	}
}
