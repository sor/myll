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
	using Attribs = Dictionary<string, List<string>>;

	public static class VisitorExtensions
	{
		private static readonly Stack<Scope> ScopeStack = new Stack<Scope>();
		private static readonly ExprVisitor  ExprVis    = new ExprVisitor( ScopeStack );
		private static readonly StmtVisitor  StmtVis    = new StmtVisitor( ScopeStack );
		public static readonly  DeclVisitor  DeclVis    = new DeclVisitor( ScopeStack );

		private static readonly Dictionary<int, Operand>
			ToOperand = new Dictionary<int, Operand> {
				{ Parser.DBL_PLUS, Operand.PostIncr },
				{ Parser.DBL_MINUS, Operand.PostDecr },
				{ Parser.LPAREN, Operand.FuncCall },
				{ Parser.QM_LPAREN, Operand.NCFuncCall },
				{ Parser.LBRACK, Operand.IndexCall },
				{ Parser.QM_LBRACK, Operand.NCIndexCall },
				{ Parser.POINT, Operand.MemberAccess },
				{ Parser.QM_POINT, Operand.NCMemberAccess },
				{ Parser.RARROW, Operand.MemberPtrAccess },
				{ Parser.POINT_STAR, Operand.MemberAccessPtr },
				{ Parser.QM_POINT_STAR, Operand.NCMemberAccessPtr },
				{ Parser.ARROW_STAR, Operand.MemberPtrAccessPtr },
				//	{Parser.DBL_STAR,	Operand.Pow},
				{ Parser.STAR, Operand.Multiply },
				{ Parser.SLASH, Operand.EuclideanDivide },
				{ Parser.MOD, Operand.Modulo },
				{ Parser.DOT, Operand.Dot },
				{ Parser.CROSS, Operand.Cross },
				{ Parser.DIV, Operand.Divide },
				{ Parser.PLUS, Operand.Add },
				{ Parser.MINUS, Operand.Subtract },
				{ Parser.AMP, Operand.BitAnd },
				{ Parser.HAT, Operand.BitXor },
				{ Parser.PIPE, Operand.BitOr },
				{ Parser.COMPARE, Operand.Comparison },
				{ Parser.LT, Operand.LessThan },
				{ Parser.LTEQ, Operand.LessEqual },
				{ Parser.GT, Operand.GreaterThan },
				{ Parser.GTEQ, Operand.GreaterEqual },
				{ Parser.EQ, Operand.Equal },
				{ Parser.NEQ, Operand.NotEqual },
				{ Parser.DBL_AMP, Operand.And },
				{ Parser.DBL_PIPE, Operand.Or },
			};

		private static readonly Dictionary<int, Operand>
			ToPreOperand = new Dictionary<int, Operand> {
				{ Parser.DBL_PLUS, Operand.PreIncr },
				{ Parser.DBL_MINUS, Operand.PreDecr },
				{ Parser.PLUS, Operand.PrePlus },
				{ Parser.MINUS, Operand.PreMinus },
				{ Parser.EM, Operand.Negation },
				{ Parser.TILDE, Operand.Complement },
				{ Parser.STAR, Operand.Dereference },
				{ Parser.AMP, Operand.AddressOf },
			};

		private static readonly Dictionary<int, Operand>
			ToAssignOperand = new Dictionary<int, Operand> {
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

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static void Exec<TSource>( this IEnumerable<TSource> s )
		{
			if( s == null ) return;
			using( IEnumerator<TSource> e = s.GetEnumerator() )
				while( e.MoveNext() ) {}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static int ToInt( this ITerminalNode o, int @default = 0 )
			=> o == null
				? @default
				: int.Parse( o.GetText() );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Operand ToOp( this IToken tok )
			=> ToOperand[tok.Type];

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Operand ToPreOp( this IToken tok )
			=> ToPreOperand[tok.Type];

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Operand ToAssignOp( this IToken tok )
			=> ToAssignOperand[tok.Type];

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static SrcPos ToSrcPos( this ParserRuleContext c )
			=> new SrcPos {
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
			=> c == null
				? null
				: ExprVis.Visit( c );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Literal Visit( this Parser.LitContext c )
			=> ExprVis.VisitLit( c );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Stmt Visit( this Parser.LevStmtContext c )
			=> c == null
				? null
				: StmtVis.Visit( c );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Stmt Visit( this Parser.FuncBodyContext c )
			=> c == null
				? null
				: StmtVis.VisitFuncBody( c );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Decl Visit( this Parser.LevDeclContext c )
			=> c == null
				? null
				: DeclVis.Visit( c );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Qualifier Visit( this Parser.QualContext[] c )
			=> c.Aggregate( Qualifier.None, ( a, q ) => a | q.Visit() );


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Var.Accessor Visit( this Parser.AccessorDefContext c )
			=> new Var.Accessor {
				body = c.funcBody().Visit(),
				qual = c.qual().Visit(),
				kind = c.v.ToAccessorKind(),
			};

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static List<Var.Accessor> Visit( this Parser.AccessorDefContext[] c )
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
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Var.Accessor.Kind ToAccessorKind( this IToken tok )
			=> tok.Type switch {
				Parser.GET    => Var.Accessor.Kind.Get,
				Parser.REFGET => Var.Accessor.Kind.RefGet,
				Parser.SET    => Var.Accessor.Kind.Set
			};

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Structural.Kind ToStructuralKind( this IToken tok )
			=> tok.Type switch {
				Parser.STRUCT => Structural.Kind.Struct,
				Parser.CLASS  => Structural.Kind.Class,
				Parser.UNION  => Structural.Kind.Union,
			};

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Qualifier Visit( this Parser.QualContext c )
			=> c.v.Type switch {
				MyllParser.CONST    => Qualifier.Const,
				MyllParser.MUTABLE  => Qualifier.Mutable,
				MyllParser.VOLATILE => Qualifier.Volatile,
				MyllParser.STABLE   => Qualifier.Stable,
			};

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Access Visit( this Parser.AccessModContext c )
			=> c.v.Type switch {
				MyllParser.PRIV => Access.Private,
				MyllParser.PROT => Access.Protected,
				MyllParser.PUB  => Access.Public,
			};
#pragma warning restore 8509
	}
}
