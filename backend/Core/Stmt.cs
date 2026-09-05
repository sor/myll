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

	using Strings  = List<string>;
	using IStrings = IEnumerable<string>;
	using Attribs  = Dictionary<string, List<string>>;

	public abstract class Stmt : AttributedNode
	{
		// Enumerate (depth first) through all contained Stmt and itself
		// Only overloaded in Stmt which contain more Stmt itself
		// Filter results with e.g. EnumerateDF.OfType<ReturnStmt>()
		[Pure]
		public virtual IEnumerable<Stmt> EnumerateDF {
			get { yield return this; }
		}

		// Only override in Block and EmptyStmt, is the same as Gen() everywhere else
		public virtual Strings GenWithoutCurly( int level )
			=> Gen( level );

		// Outputs immediate generated lines of code
		// level is the level of indentation
		public abstract Strings Gen( int level );
	}

	public class VarStmt : Stmt
	{
		public VarDecl.Kind kind;
		public string       name = null!;
		public Typespec     type = null!; // contains Qualifier
		public Expr?        init;
		public bool         isDirectConstruct;
		public bool         IsAutoReturn { get; set; }

		public override Strings Gen( int level )
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

			string typeAndName = type.Gen( name );

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
			return ret.IndentAll( level );
		}
	}

	public class UsingStmt : Stmt
	{
		public Typespec type = null!;
		public string?  name;

		// in locations where C++ does not support "using namespace" this must not be printed
		// but instead the unqualified types need to be changed to qualified ones
		public override Strings Gen( int level )
		{
			string ret = Format(
				UsingFormat[name == null ? 1 : 0],
				"",
				type.Gen() );
			return ret.IndentAll( level );
		}
	}

	public class AliasStmt : Stmt
	{
		public Typespec type = null!;
		public string   name = null!;

		public override Strings Gen( int level )
		{
			string ret = Format(
				AliasFormat[0],
				"",
				name,
				type.Gen() );
			return ret.IndentAll( level );
		}
	}

	public class ReturnStmt : Stmt
	{
		public Expr? expr;
		public bool  HasValue => expr != null;

		public override Strings Gen( int level )
			=> ((expr != null)
					? Format( "return {0};", expr.Gen() )
					: Format( "return;" ))
				.IndentAll( level );
	}

	public class ThrowStmt : Stmt
	{
		public Expr expr = null!;

		public override Strings Gen( int level )
			=> Format( "throw {0};", expr.Gen() ).IndentAll( level );
	}

	public class BreakStmt : Stmt
	{
		public int depth = 1; // C++ default is 1, break one level

		public override Strings Gen( int level )
			=> Format( "break;" ).IndentAll( level );
	}

	public class ContinueStmt : Stmt
	{
		public int depth = 1; // C++ default is 1, continue the innermost loop

		public override Strings Gen( int level )
			=> Format( "continue;" ).IndentAll( level );
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

		public override Strings Gen( int level )
		{
			Strings ret    = new();
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

		public override Strings Gen( int level )
			=> (exprs
				.Select( e => e.Gen() )
				.Join( " = " ) + ";").IndentAll( level );
	}

	public class AggrAssign : Stmt
	{
		public Operand op;
		public Expr    leftExpr = null!;
		public Expr    rightExpr = null!;

		public override Strings Gen( int level )
			=> Format(
				op.GetAssignFormat(),
				leftExpr.Gen(),
				rightExpr.Gen() ).IndentAll( level );
	}

	public class ExprStmt : Stmt
	{
		public Expr expr = null!;

		public override Strings Gen( int level )
			=> Format( "{0};", expr.Gen() ).IndentAll( level );
	}

	public class EmptyStmt : Stmt
	{
		public override Strings GenWithoutCurly( int level )
			=> ";".IndentAll( level );

		public override Strings Gen( int level )
			=> ";".IndentAll( level );
	}

	// This class should be phased out in the far future, but for now it's just too useful
	public class FreetextStmt : Stmt
	{
		public Strings lines;

		public FreetextStmt( string text )
			=> lines = new Strings { text };

		public override Strings Gen( int level )
			=> lines.Indent( level ).ToList();
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

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( CondThen ifThen in ifThens )
					foreach( Stmt subStmt in ifThen.then.EnumerateDF )
						yield return subStmt;

				if( els != null )
					foreach( Stmt subStmt in els.EnumerateDF )
						yield return subStmt;

				yield return this;
			}
		}

		public override Strings Gen( int level )
		{
			Strings ret     = new();
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

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( CaseBlock caseStmt in cases )
					foreach( Stmt subStmt in caseStmt.then.EnumerateDF )
						yield return subStmt;

				if( els != null )
					foreach( Stmt subStmt in els.EnumerateDF )
						yield return subStmt;

				yield return this;
			}
		}

		public override Strings Gen( int level )
		{
			Strings ret      = new();
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

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( Stmt subStmt in body.EnumerateDF )
					yield return subStmt;

				yield return this;
			}
		}

		public override Strings Gen( int level )
		{
			Strings ret    = new();
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

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				if( init != null )
					foreach( Stmt subStmt in init.EnumerateDF )
						yield return subStmt;

				if( body != null )
					foreach( Stmt subStmt in body.EnumerateDF )
						yield return subStmt;

				if( els != null )
					foreach( Stmt subStmt in els.EnumerateDF )
						yield return subStmt;

				foreach( Stmt baseStmt in base.EnumerateDF )
					yield return baseStmt;
			}
		}

		public override Strings Gen( int level )
		{
			if( init is MultiStmt )
				throw new NotImplementedException( "A MultiStmt can not be used in for-loop as init" );

			Strings inits = init.GenWithoutCurly( 0 );
			if( inits.Count > 1 )
				throw new NotImplementedException( "for statement does not support more than one initializer yet" );

			Strings ret     = new();
			string  indent  = IndentString.Repeat( level );
			string  initStr = inits.First();
			string  condStr = cond?.Gen() ?? "";
			string  iterStr = iter?.Gen() ?? "";
			ret.Add( Format( "{0}for( {1} {2}; {3} ) {{", indent, initStr, condStr, iterStr ) );
			ret.AddRange( body?.GenWithoutCurly( level + 1 ) ?? Enumerable.Empty<string>() );
			ret.Add( Format( "{0}}}", indent ) );
			return ret;
		}
	}

	// +0-1 scope
	public class WhileStmt : Stmt
	{
		public Stmt  body = null!;
		public Expr  cond = null!;
		public Stmt? els;

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				if( body != null )
					foreach( Stmt subStmt in body.EnumerateDF )
						yield return subStmt;

				if( els != null )
					foreach( Stmt subStmt in els.EnumerateDF )
						yield return subStmt;

				foreach( Stmt baseStmt in base.EnumerateDF )
					yield return baseStmt;
			}
		}

		public override Strings Gen( int level )
		{
			Strings ret    = new();
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

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				if( body != null )
					foreach( Stmt subStmt in body.EnumerateDF )
						yield return subStmt;

				foreach( Stmt baseStmt in base.EnumerateDF )
					yield return baseStmt;
			}
		}

		public override Strings Gen( int level )
		{
			Strings ret    = new();
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

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				if( body != null )
					foreach( Stmt subStmt in body.EnumerateDF )
						yield return subStmt;

				foreach( Stmt baseStmt in base.EnumerateDF )
					yield return baseStmt;
			}
		}

		public override Strings Gen( int level )
		{
			Strings ret     = new();
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

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( Stmt stmt in stmts )
				foreach( Stmt subStmt in stmt.EnumerateDF )
					yield return subStmt;

				yield return this;
			}
		}

		public MultiStmt( IEnumerable<Stmt>? stmts, bool isScope )
		{
			this.isScope = isScope;
			this.stmts   = stmts?.ToList() ?? new(); // TODO: if stmts contains MultiStmt then unwrap them
		}

		public override void AssignAttribs( Attribs inAttribs )
			=> stmts.ForEach( v => v.AssignAttribs( inAttribs ) );

		public override Strings GenWithoutCurly( int level )
			=> NonEmptyStmts()
				.SelectMany( s => s.Gen( level ) )
				.ToList();

		public override Strings Gen( int level )
		{
			// Block to Block needs to indent further else it's ok to remain same level
			// The curly braces need to be outdentented one level
			Strings ret = NonEmptyStmts()
				.SelectMany( s => s.Gen( isScope ? level + 1 : level ) )
				.Curly( level, isScope )
				.ToList();
			return ret;
		}
	}
}
