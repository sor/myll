using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Myll.Generator;
using Myll.Resolver;

namespace Myll.Core
{
	using static String;
	using static StmtFormatting;

	using Attribs = Dictionary<string, List<string>>;

	public abstract class Stmt : AttributedNode
	{

		// Only override in Block and EmptyStmt, is the same as Gen() everywhere else
		public virtual SourceLines GenWithoutCurly( int level )
			=> Gen( level );

		// Outputs immediate generated lines of code
		// level is the level of indentation
		public abstract SourceLines Gen( int level );

		/// <summary>
		/// Stamps every emitted line that has no source position with this statement's
		/// <see cref="srcPos"/>. Do not use for normal statement generation: braces and
		/// other helper lines are intentionally left unmapped because they never produce
		/// useful compiler diagnostics on their own. Use this helper only when you
		/// explicitly want every line produced by a generator to share one source
		/// location.
		/// </summary>
		[Obsolete( "Only use when every produced line must share this statement's srcPos; braces should stay unmapped." )]
		protected SourceLines WithSourceMap( SourceLines lines )
		{
			if( srcPos == null )
				return lines;
			foreach( SourceLine line in lines )
				if( line.SrcPos == null )
					line.SrcPos = srcPos;
			return lines;
		}

		/// <summary>
		/// Replaces every child statement in place with the result of <paramref name="replace"/>.
		/// MultiStmt blocks are excluded: they are walked via dedicated block hooks.
		/// </summary>
		public virtual void ReplaceChildStmts( Func<Stmt, Stmt> replace ) { }

		/// <summary>
		/// Visits every expression directly owned by this statement.
		/// </summary>
		public virtual void VisitChildExprs( Action<Expr> visit ) { }
	}

	public class VarStmt : Stmt
	{
		public VarDecl.Kind kind;
		public string       name = null!;
		public Typespec     type = null!; // contains Qualifier
		public Expr?        init;
		public bool         isDirectConstruct;
		public VarDecl?     decl;
		public bool         IsAutoReturn { get; set; }

		/// <summary>
		/// True when the declared type was `auto` (explicit or omitted). The resolver
		/// still infers and validates the concrete type, but emission keeps `auto`
		/// wherever C++ deduces the same type.
		/// </summary>
		public bool IsAutoDeclared { get; set; }

		public override void VisitChildExprs( Action<Expr> visit )
		{
			if( init != null )
				visit( init );
		}

		public override SourceLines Gen( int level )
		{
			// Dependent nested types emit 'typename ' from TypespecNested.GenType().
			// The VarFormat typename slot is intentionally left empty here to avoid
			// a duplicate prefix.
			bool   needsTypename = false;
			bool   isConstType   = (type.qual & Qualifier.Const) != 0;

			if( IsNoInit && isConstType )
				throw new DiagnosticException( new Diagnostic(
					srcPos,
					DiagnosticKind.Error,
					"[noinit]/[uninit] cannot be used with const variables" ) );

			FuncCall? directCall = ( init as FuncCallExpr )?.funcCall;
			string initStr = isDirectConstruct
				? directCall != null && directCall.args.Count > 0
					? directCall.Gen()
					: VarEmptyInitFormat
				: init != null
					? VarFormat[6] + init.Gen()
					: IsNoInit
						? ""
						: VarEmptyInitFormat;

			// C++ `auto` would deduce a raw pointer for `new` while Myll's type is a
			// smart pointer, so those keep the materialized concrete type. Everything
			// else resolves the same way C++ would deduce it.
			// The concrete emission stays available: a future [resolve] attribute can
			// opt back in per declaration.
			bool emitAuto = IsAutoDeclared && init is not NewExpr;

			string typeAndName = emitAuto
				? VarAutoDeclFormat + name
				: type.Gen( name );
			//string typeAndName = type.Gen( name );

			string ret = Format(
				VarFormat[0],
				"",
				"",                           // extern
				"",                           // inline
				IsStatic      ? VarFormat[3] : "",
				IsCompileTime ? VarFormat[4] : "",
				needsTypename ? VarFormat[5] : "",
				typeAndName,
				initStr );
			return ret.IndentAll( level, srcPos );
		}
	}

	public class UsingStmt : Stmt
	{
		public Typespec type = null!;
		public string?  name;

		// Set by the resolver when this using statement targets a namespace.
		public bool IsNamespaceUsing { get; set; }

		// in locations where C++ does not support "using namespace" this must not be printed
		// but instead the unqualified types need to be changed to qualified ones
		public override SourceLines Gen( int level )
		{
			string ret = Format(
				UsingFormat[IsNamespaceUsing ? 1 : 0],
				"",
				type.Gen() );
			return ret.IndentAll( level, srcPos );
		}
	}

	public class AliasStmt : Stmt
	{
		public Typespec       type     = null!;
		public string         name     = null!;
		public List<TplParam> TplParams { get; init; } = new();

		public override SourceLines Gen( int level )
		{
			string ret = Format(
				AliasFormat[0],
				"",
				name,
				type.Gen() );
			return ret.IndentAll( level, srcPos );
		}
	}

	public class ReturnStmt : Stmt
	{
		public Expr? expr;
		public bool  HasValue => expr != null;

		public override void VisitChildExprs( Action<Expr> visit )
		{
			if( expr != null )
				visit( expr );
		}

		public override SourceLines Gen( int level )
			=> ((expr != null)
					? Format( "return {0};", expr.Gen() )
					: Format( "return;" ))
				.IndentAll( level, srcPos );
	}

	public class ThrowStmt : Stmt
	{
		public Expr? expr;

		public override void VisitChildExprs( Action<Expr> visit )
		{
			if( expr != null )
				visit( expr );
		}

		public override SourceLines Gen( int level )
			=> expr != null
				? Format( "throw {0};", expr.Gen() ).IndentAll( level, srcPos )
				: "throw;".IndentAll( level, srcPos );
	}

	public class BreakStmt : Stmt
	{
		public int depth = 1; // C++ default is 1, break one level

		public override SourceLines Gen( int level )
			=> Format( "break;" ).IndentAll( level, srcPos );
	}

	public class ContinueStmt : Stmt
	{
		public int depth = 1; // C++ default is 1, continue the innermost loop

		public override SourceLines Gen( int level )
			=> Format( "continue;" ).IndentAll( level, srcPos );
	}

	public class CatchClause
	{
		public Param? param;
		public Stmt   body = null!;
	}

	public class TryCatchStmt : Stmt
	{
		public Stmt              tryBody = null!;
		public List<CatchClause> catches = new();

		public override void ReplaceChildStmts( Func<Stmt, Stmt> replace )
		{
			tryBody = replace( tryBody );
			foreach( CatchClause cc in catches )
				cc.body = replace( cc.body );
		}

		public override SourceLines Gen( int level )
		{
			SourceLines ret    = new();
			string  indent = IndentString.Repeat( level );

			ret.Add( Format( TryCatchFormat[0], indent ) );
			ret.Add( Format( CurlyOpen, indent ) );
			ret.AddRange( tryBody.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( CurlyClose, indent ) );

			foreach( CatchClause cc in catches ) {
				if( cc.param != null )
					ret.Add( Format( TryCatchFormat[1], indent, cc.param.Gen() ) );
				else
					ret.Add( Format( TryCatchFormat[2], indent ) );

				ret.Add( Format( CurlyOpen, indent ) );
				ret.AddRange( cc.body.GenWithoutCurly( level + 1 ) );
				ret.Add( Format( CurlyClose, indent ) );
			}

			return ret;
		}
	}

	public class MultiAssign : Stmt
	{
		public List<Expr> exprs = new();

		public override void VisitChildExprs( Action<Expr> visit )
		{
			foreach( Expr e in exprs )
				visit( e );
		}

		public override SourceLines Gen( int level )
			=> (exprs
				.Select( e => e.Gen() )
				.Join( " = " ) + ";").IndentAll( level, srcPos );
	}

	public class AggrAssign : Stmt
	{
		public Operand op;
		public Expr    leftExpr = null!;
		public Expr    rightExpr = null!;

		public override void VisitChildExprs( Action<Expr> visit )
		{
			visit( leftExpr );
			visit( rightExpr );
		}

		public override SourceLines Gen( int level )
			=> Format(
				op.GetAssignFormat(),
				leftExpr.Gen(),
				rightExpr.Gen() ).IndentAll( level, srcPos );
	}

	public class ExprStmt : Stmt
	{
		public Expr expr = null!;

		public override void VisitChildExprs( Action<Expr> visit )
			=> visit( expr );

		public override SourceLines Gen( int level )
			=> Format( "{0};", expr.Gen() ).IndentAll( level, srcPos );
	}

	public class EmptyStmt : Stmt
	{
		public override SourceLines GenWithoutCurly( int level )
			=> ";".IndentAll( level, srcPos );

		public override SourceLines Gen( int level )
			=> ";".IndentAll( level, srcPos );
	}

	// This class should be phased out in the far future, but for now it's just too useful
	public class FreetextStmt : Stmt
	{
		public SourceLines lines;

		public FreetextStmt( string text )
			=> lines = new SourceLines { text };

		public override SourceLines Gen( int level )
			=> new SourceLines( lines.Indent( level ) );
	}

	/// =!= Stmt which contain other Stmt themselves =!=

	// 1-2-n scopes
	public class IfStmt : Stmt
	{
		public class CondThen
		{
			public Expr cond;
			public Stmt then;

			public CondThen( Expr cond, Stmt then )
			{
				this.cond = cond;
				this.then = then;
			}
		}

		public List<CondThen> ifThens = new();
		public Stmt?          els;

		public override void ReplaceChildStmts( Func<Stmt, Stmt> replace )
		{
			foreach( CondThen ct in ifThens )
				ct.then = replace( ct.then );
			els = els != null ? replace( els ) : null;
		}

		public override void VisitChildExprs( Action<Expr> visit )
		{
			foreach( CondThen ct in ifThens )
				visit( ct.cond );
		}

		public override SourceLines Gen( int level )
		{
			SourceLines ret     = new();
			string  indent  = IndentString.Repeat( level );
			bool    isFirst = true;
			foreach( CondThen ifThen in ifThens ) {
				string fmt = isFirst
					? "{0}if( {1} ) {{"
					: "{0}}} else if( {1} ) {{";
				ret.Add( Format( fmt, indent, ifThen.cond.Gen() ) );
				ret.AddRange( ifThen.then.GenWithoutCurly( level + 1 ) );
				isFirst = false;
			}
			if( els != null ) {
				ret.Add( Format( "{0}}} else {{", indent ) );
				ret.AddRange( els.GenWithoutCurly( level + 1 ) );
			}
			ret.Add( Format( "{0}}}", indent ) );
			return ret;
		}
	}

	public class SwitchStmt : Stmt
	{
		public class CaseBlock
		{
			public List<Expr> compare; // can have multiple ORed conditions
			public MultiStmt  then;    // isScope = true

			public CaseBlock( List<Expr> compare, MultiStmt then )
			{
				this.compare = compare;
				this.then    = then;
			}
		}

		public Expr            cond = null!;
		public List<CaseBlock> cases = new();
		public MultiStmt?      els; // isScope = true

		public override void ReplaceChildStmts( Func<Stmt, Stmt> replace )
		{
			foreach( CaseBlock cb in cases )
				cb.then = (MultiStmt)replace( cb.then );
			els = els != null ? (MultiStmt)replace( els ) : null;
		}

		public override void VisitChildExprs( Action<Expr> visit )
		{
			visit( cond );
			foreach( CaseBlock cb in cases )
				foreach( Expr e in cb.compare )
					visit( e );
		}

		public override SourceLines Gen( int level )
		{
			SourceLines ret      = new();
			string  indent   = IndentString.Repeat( level );
			string  inindent = IndentString.Repeat( level + 1 );
			ret.Add( Format( "{0}switch({1}) {{", indent, cond.Gen() ) );
			foreach( CaseBlock caseStmt in cases ) {
				foreach( Expr expr in caseStmt.compare )
					ret.Add( Format( "{0}case {1}:", inindent, expr.Gen() ) );

				ret.AddRange( caseStmt.then.GenWithoutCurly( level + 2 ).Curly( inindent, caseStmt.then.isScope ) );
			}
			if( els != null ) {
				ret.Add( Format( "{0}default:", inindent ) );
				ret.AddRange( els.GenWithoutCurly( level + 2 ).Curly( inindent, els.isScope ) );
			}
			ret.Add( Format( "{0}}}", indent ) );
			return ret;
		}
	}

	// 1 scope
	public class LoopStmt : Stmt
	{
		public Stmt body = null!;

		public override void ReplaceChildStmts( Func<Stmt, Stmt> replace )
			=> body = replace( body );

		public override SourceLines Gen( int level )
		{
			SourceLines ret    = new();
			string  indent = IndentString.Repeat( level );
			ret.Add( Format( "{0}while( true ) {{", indent ) );
			ret.AddRange( body.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( "{0}}}", indent ) );
			return ret;
		}
	}

	// +0-1 scope
	public class ForStmt : Stmt
	{
		public Stmt? body;
		public Stmt  init = null!;
		public Expr? cond;
		public Expr? iter;
		public Stmt? els; // TODO: not implemented yet

		public override void ReplaceChildStmts( Func<Stmt, Stmt> replace )
		{
			init = replace( init );
			body = body != null ? replace( body ) : null;
			els  = els  != null ? replace( els )  : null;
		}

		public override void VisitChildExprs( Action<Expr> visit )
		{
			if( cond != null )
				visit( cond );
			if( iter != null )
				visit( iter );
		}

		public override SourceLines Gen( int level )
		{
			if( init is MultiStmt )
				throw new DiagnosticException( new Diagnostic(
					srcPos,
					DiagnosticKind.Error,
					"multiple initializers in a for header are not implemented yet" ) );

			SourceLines inits = init.GenWithoutCurly( 0 );
			if( inits.Count > 1 )
				throw new DiagnosticException( new Diagnostic(
					srcPos,
					DiagnosticKind.Error,
					"multi-statement for initializers are not implemented yet" ) );

			SourceLines ret     = new();
			string  indent  = IndentString.Repeat( level );
			string  initStr = inits.First().Text;
			string  condStr = cond?.Gen() ?? "";
			string  iterStr = iter?.Gen() ?? "";
			ret.Add( Format( "{0}for( {1} {2}; {3} ) {{", indent, initStr, condStr, iterStr ) );
			ret.AddRange( body?.GenWithoutCurly( level + 1 ) ?? new SourceLines() );
			ret.Add( Format( "{0}}}", indent ) );
			return ret;
		}
	}

	// +0-1 scope
	public class RangeForStmt : Stmt
	{
		public VarStmt varDecl = null!;
		public Expr    range   = null!;
		public Stmt?   body;
		public Stmt?   els;

		public override void ReplaceChildStmts( Func<Stmt, Stmt> replace )
		{
			varDecl = (VarStmt)replace( varDecl );
			body = body != null ? replace( body ) : null;
			els  = els  != null ? replace( els )  : null;
		}

		public override void VisitChildExprs( Action<Expr> visit )
			=> visit( range );

		public override SourceLines Gen( int level )
		{
			SourceLines ret    = new();
			string  indent = IndentString.Repeat( level );
			string  decl   = varDecl.IsAutoDeclared
				? VarAutoDeclFormat + varDecl.name
				: varDecl.type.Gen( varDecl.name );
			ret.Add( Format( RangeForFormat[0], indent, decl, range.Gen() ) );
			ret.AddRange( body?.GenWithoutCurly( level + 1 ) ?? new SourceLines() );
			ret.Add( Format( CurlyClose, indent ) );
			return ret;
		}
	}

	// +0-1 scope
	public class WhileStmt : Stmt
	{
		public Stmt  body = null!;
		public Expr  cond = null!;
		public Stmt? els;

		public override void ReplaceChildStmts( Func<Stmt, Stmt> replace )
		{
			body = replace( body );
			els  = els != null ? replace( els ) : null;
		}

		public override void VisitChildExprs( Action<Expr> visit )
			=> visit( cond );

		public override SourceLines Gen( int level )
		{
			SourceLines ret    = new();
			string  indent = IndentString.Repeat( level );
			ret.Add( Format( "{0}while( {1} ) {{", indent, cond.Gen() ) );
			ret.AddRange( body.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( CurlyClose, indent ) );
			return ret;
		}
	}

	public class DoWhileStmt : Stmt
	{
		public Stmt body = null!;
		public Expr cond = null!;

		public override void ReplaceChildStmts( Func<Stmt, Stmt> replace )
			=> body = replace( body );

		public override void VisitChildExprs( Action<Expr> visit )
			=> visit( cond );

		public override SourceLines Gen( int level )
		{
			SourceLines ret    = new();
			string  indent = IndentString.Repeat( level );
			ret.Add( Format( "{0}do {{", indent ) );
			ret.AddRange( body.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( "{0}}} while( {1} );", indent, cond.Gen() ) );
			return ret;
		}
	}

	public class TimesStmt : Stmt
	{
		public Stmt    body = null!;
		public Expr    count = null!;
		public string? name;
		public long    offset = 0;

		public override void ReplaceChildStmts( Func<Stmt, Stmt> replace )
			=> body = replace( body );

		public override void VisitChildExprs( Action<Expr> visit )
			=> visit( count );

		public override SourceLines Gen( int level )
		{
			SourceLines ret     = new();
			string  indent  = IndentString.Repeat( level );
			string  varName = name ?? "myll_tmp_missing";
			ret.Add(
				Format(
					"{0}for( int {1} = {2}; {1} < {3}+{2}; ++{1} ) {{",
					indent,
					varName,
					offset,
					count.Gen( true ) ) );
			ret.AddRange( body.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( "{0}}}", indent ) );
			return ret;
		}
	}

	// 1 scope
	public class MultiStmt : Stmt
	{
		public bool       isScope { get; init; } = false;
		public List<Stmt> stmts = new();

		public bool isEmpty => stmts.IsEmpty();

		[Pure]
		private IEnumerable<Stmt> NonEmptyStmts()
			=> stmts.Count == 1 && stmts[0] is EmptyStmt
				? stmts
				: stmts.Where( s => s is not EmptyStmt );


		public MultiStmt( IEnumerable<Stmt>? stmts, bool isScope )
		{
			this.isScope = isScope;
			this.stmts   = stmts?.ToList() ?? new(); // TODO: if stmts contains MultiStmt then unwrap them
		}

		public override void AssignAttribs( Attribs inAttribs )
			=> stmts.ForEach( v => v.AssignAttribs( inAttribs ) );

		public override SourceLines GenWithoutCurly( int level )
			=> new SourceLines( NonEmptyStmts()
				.SelectMany( s => s.Gen( level ) ) );

		public override SourceLines Gen( int level )
		{
			// Block to Block needs to indent further else it's ok to remain same level
			// The curly braces need to be outdentented one level
			SourceLines ret = new SourceLines( NonEmptyStmts()
				.SelectMany( s => s.Gen( isScope ? level + 1 : level ) )
				.Curly( level, isScope ) );
			return ret;
		}
	}
}
