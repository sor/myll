using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Myll.Generator;

namespace Myll.Core
{
	using static String;
	using static StmtFormatting;

	using Strings  = List<string>;
	using IStrings = IEnumerable<string>;
	using Attribs  = Dictionary<string, List<string>>;

	public abstract class Stmt
	{
		// TODO: If there is no srcPos on myself, redirect to parent
		public  SrcPos  srcPos;
		private Attribs attribs { get; set; }

		public bool IsStatic => HasAttrib( "static" );

		public bool HasAttrib( string attrib )
			=> attribs != null
			&& attribs.ContainsKey( attrib );

		public bool IsAttrib( string attrib, string value )
			=> attribs != null
			&& attribs.TryGetValue( attrib, out Strings values )
			&& values.Contains( value );

		// Enumerate (depth first) through all contained Stmt and itself
		// Only overloaded in Stmt which contain more Stmt itself
		// Filter results with e.g. EnumerateDF.OfType<ReturnStmt>()
		[Pure]
		public virtual IEnumerable<Stmt> EnumerateDF {
			get { yield return this; }
		}

		public virtual void AssignAttribs( Attribs inAttribs )
		{
			// TODO: support multiple attribs, in tandem with ScopeStack
			attribs = inAttribs;

			AttribsAssigned();
		}

		// This is the analyze-for-dummies until analyze works
		protected virtual void AttribsAssigned() {}

		[Pure]
		public override string ToString()
		{
			StringBuilder sb = GetType().GetProperties().Aggregate(
				new StringBuilder(),
				( builder, info ) => builder.AppendFormat(
					"{0}: {1}, ",
					info.Name,
					info.GetValue( this, null ) ?? "(null)" ) );

			// Remove the excess ", " from the end of the string
			sb.Length = Math.Max( sb.Length - 2, 0 );

			return Format( "{{{0} {1}}}", GetType().Name, sb );
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
		public string       name;
		public Typespec     type; // contains Qualifier
		public Expr?        init;

		public override Strings Gen( int level )
		{
			bool   needsTypename = false; // TODO how to determine this
			string initStr       = init != null ? VarFormat[3] + init.Gen() : "";

			string ret = Format(
				VarFormat[0],
				"",
				IsStatic ? VarFormat[1] : "",
				needsTypename ? VarFormat[2] : "",
				type.Gen( name ),
				initStr );
			return ret.IndentAll( level );
		}
	}

	public class UsingStmt : Stmt
	{
		public Typespec type;
		public string   name;

		// in locations where C++ does not support "using namespace" this must not be printed
		// but instead the unqualified types need to be changed to qualified ones
		public override Strings Gen( int level )
		{
			string ret = Format(
				UsingFormat[name == null ? 1 : 0],
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
		public Expr expr;

		public override Strings Gen( int level )
			=> Format( "throw {0};", expr.Gen() ).IndentAll( level );
	}

	public class BreakStmt : Stmt
	{
		public int depth = 1; // C++ default is 1, break one level

		public override Strings Gen( int level )
		{
			if( depth != 1 )
				throw new NotImplementedException(
					"no depth except 1 supported directly, analyze step must take care of this!" );

			return Format( "break;" ).IndentAll( level );
		}
	}

	public class MultiAssign : Stmt
	{
		public List<Expr> exprs;

		public override Strings Gen( int level )
			=> (exprs
				.Select( e => e.Gen() )
				.Join( " = " ) + ";").IndentAll( level );
	}

	public class AggrAssign : Stmt
	{
		public Operand op;
		public Expr    leftExpr;
		public Expr    rightExpr;

		public override Strings Gen( int level )
			=> Format(
				op.GetAssignFormat(),
				leftExpr.Gen(),
				rightExpr.Gen() ).IndentAll( level );
	}

	public class ExprStmt : Stmt
	{
		public Expr expr;

		public override Strings Gen( int level )
			=> Format( "{0};", expr.Gen() ).IndentAll( level );
	}

	public class EmptyStmt : Stmt
	{
		public override Strings GenWithoutCurly( int level )
			=> new() { ";" };
			//			=> throw new NotImplementedException( "Would this just work as a semicolon?" );

		public override Strings Gen( int level )
			=> throw new NotImplementedException( "Would this just work as a semicolon?" );
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
		public struct CondThen
		{
			public Expr cond;
			public Stmt then;
		}

		public List<CondThen> ifThens;
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
		public struct CaseBlock
		{
			public List<Expr> compare; // can have multiple ORed conditions
			public MultiStmt  then;    // isScope = true
		}

		public Expr            cond;
		public List<CaseBlock> cases;
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
		public Stmt body;

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
		public Stmt  init;
		public Expr? cond;
		public Expr? iter;
		public Stmt? els; // TODO: not implemented yet

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				if( init != null )
					foreach( Stmt subStmt in init.EnumerateDF )
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
			if( els != null )
				throw new NotImplementedException( "Else for for-loop not implemented yet" );

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
		public Stmt  body;
		public Expr  cond;
		public Stmt? els;

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				if( els != null )
					foreach( Stmt subStmt in els.EnumerateDF )
						yield return subStmt;

				foreach( Stmt baseStmt in base.EnumerateDF )
					yield return baseStmt;
			}
		}

		public override Strings Gen( int level )
		{
			if( els != null )
				throw new NotImplementedException( "implement else for while-loop" );

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
		public Stmt body;
		public Expr cond;

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
		// HACK: solve that better
		public static int    randomNumber = 1;
		public static string randomName => "myll_times_" + ++randomNumber;

		public Stmt    body;
		public Expr    count;
		public string? name;
		public long    offset = 0;

		public override Strings Gen( int level )
		{
			Strings ret     = new();
			string  indent  = IndentString.Repeat( level );
			string  varName = name ?? randomName;
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
			=> stmts
				.Where( s => s is not EmptyStmt )
				.SelectMany( s => s.Gen( level ) )
				.ToList();

		public override Strings Gen( int level )
		{
			// Block to Block needs to indent further else it's ok to remain same level
			// The curly braces need to be outdentented one level
			Strings ret = stmts
				.Where( s => s is not EmptyStmt )
				.SelectMany( s => s.Gen( isScope ? level + 1 : level ) )
				.Curly( level, isScope )
				.ToList();
			return ret;
		}
	}
}
