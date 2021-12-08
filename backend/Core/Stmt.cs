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

	using Strings = List<string>;
	using Attribs = Dictionary<string, List<string>>;

	public abstract class Stmt
	{
		public  SrcPos  srcPos;
		private Attribs attribs { get; set; }

		public bool IsStatic => HasAttrib( "static" );

		public bool HasAttrib( string key )
			=> attribs != null
			&& attribs.ContainsKey( key );

		public bool IsAttrib( string key, string value )
			=> attribs != null
			&& attribs.TryGetValue( key, out Strings values )
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

		// Only override in Block, is the same as Gen() everywhere else
		public virtual Strings GenWithoutCurly( int level )
		{
			return Gen( level );
		}

		// Outputs immediate generated lines of code
		// level is the level of indentation
		public abstract Strings Gen( int level );
	}

	public class VarStmt : Stmt
	{
		public string   name;
		public Typespec type; // contains Qualifier
		public Expr     init; // opt

		public override Strings Gen( int level )
		{
			string indent        = IndentString.Repeat( level );
			bool   needsTypename = false; // TODO how to determine this
			return new Strings {
				Format(
					VarFormat[0],
					indent,
					IsStatic ? VarFormat[1] : "",
					needsTypename ? VarFormat[2] : "",
					type.Gen( name ),
					init != null ? VarFormat[3] + init.Gen() : "" )
			};
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
			string indent = IndentString.Repeat( level );
			string ret = Format(
				UsingFormat[name == null ? 1 : 0],
				indent,
				name,
				type.Gen() );

			return new Strings { ret };
		}
	}

	public class ReturnStmt : Stmt
	{
		public Expr? expr;
		public bool  HasValue => expr != null;

		public override Strings Gen( int level )
			=> new() {
				HasValue
					? Format( ReturnFormat[1], IndentString.Repeat( level ), expr.Gen() )
					: Format( ReturnFormat[0], IndentString.Repeat( level ) )
			};
	}

	public class ThrowStmt : Stmt
	{
		public Expr expr;

		public override Strings Gen( int level )
			=> new() {
				Format( ThrowFormat, IndentString.Repeat( level ), expr.Gen() )
			};
	}

	public class BreakStmt : Stmt
	{
		public int depth = 1; // C++ default is 1, break one level

		public override Strings Gen( int level )
		{
			if( depth != 1 )
				throw new NotImplementedException(
					"no depth except 1 supported directly, analyze step must take care of this!" );

			return new() {
				Format( BreakFormat, IndentString.Repeat( level ) )
			};
		}
	}

	public class MultiAssign : Stmt
	{
		public List<Expr> exprs;

		public override Strings Gen( int level )
		{
			string indent = IndentString.Repeat( level );
			return new Strings {
				indent + exprs.Select( e => e.Gen() ).Join( " = " ) + ";"
			};
		}
	}

	public class AggrAssign : Stmt
	{
		public Operand op;
		public Expr    leftExpr;
		public Expr    rightExpr;

		public override Strings Gen( int level )
			=> new Strings { Format( op.GetAssignFormat(), leftExpr.Gen(), rightExpr.Gen() ) }
				.Indent( level )
				.ToList();
	}

	public class ExprStmt : Stmt
	{
		public Expr expr;

		public override Strings Gen( int level )
			=> new Strings { Format( "{0};", expr.Gen() ) }
				.Indent( level )
				.ToList();
	}

	public class EmptyStmt : Stmt
	{
		public override Strings Gen( int level )
		{
			throw new NotImplementedException( "Would this just work as a semicolon?" );
		}
	}

	// This class should be phased out in the far future, but for now it's just too useful
	public class FreetextStmt : Stmt
	{
		public Strings lines;

		public FreetextStmt( string text )
			=> lines = new Strings { text };

		public override Strings Gen( int level )
			=> lines
				.Indent( level )
				.ToList();
	}

	/// =!= Stmt which contain other Stmt themselves =!=

	// TODO move or remove?
	public struct CondThen
	{
		public Expr condExpr;
		public Stmt thenStmt;
	}

	// 1-2-n scopes
	public class IfStmt : Stmt
	{
		public List<CondThen> ifThens;
		public Stmt?          elseStmt;

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( CondThen ifThen in ifThens )
					foreach( Stmt subStmt in ifThen.thenStmt.EnumerateDF )
						yield return subStmt;

				if( elseStmt != null )
					foreach( Stmt subStmt in elseStmt.EnumerateDF )
						yield return subStmt;

				yield return this;
			}
		}

		public override Strings Gen( int level )
		{
			Strings ret    = new();
			string  indent = IndentString.Repeat( level );
			int     index  = 0; // 0 or 1, index of format string
			foreach( CondThen ifThen in ifThens ) {
				ret.Add( Format( IfFormat[index], indent, ifThen.condExpr.Gen() ) );
				ret.Add( Format( CurlyOpen,       indent ) );
				ret.AddRange( ifThen.thenStmt.GenWithoutCurly( level + 1 ) );
				ret.Add( Format( CurlyClose, indent ) );
				index = 1;
			}
			if( elseStmt != null ) {
				ret.Add( Format( IfFormat[2], indent ) );
				ret.Add( Format( CurlyOpen,   indent ) );
				ret.AddRange( elseStmt.GenWithoutCurly( level + 1 ) );
				ret.Add( Format( CurlyClose, indent ) );
			}
			return ret;
		}
	}

	public struct CaseStmt
	{
		public List<Expr> caseExprs; // can have multiple ORed conditions
		public MultiStmt  bodyStmt;
	}

	public class SwitchStmt : Stmt
	{
		public Expr           condExpr;
		public List<CaseStmt> caseStmts;
		public Stmt?          elseStmt;

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( CaseStmt caseStmt in caseStmts )
					foreach( Stmt subStmt in caseStmt.bodyStmt.EnumerateDF )
						yield return subStmt;

				if( elseStmt != null )
					foreach( Stmt subStmt in elseStmt.EnumerateDF )
						yield return subStmt;

				yield return this;
			}
		}

		public override Strings Gen( int level )
		{
			Strings ret      = new();
			string  indent   = IndentString.Repeat( level );
			string  inindent = IndentString.Repeat( level + 1 );
			ret.Add( Format( "{0}switch({1})", indent, condExpr.Gen()) );
			ret.Add( indent + "{" );
			foreach( CaseStmt caseStmt in caseStmts ) {
				foreach( Expr expr in caseStmt.caseExprs ) {
					ret.Add( Format( "{0}case {1}:", inindent, expr.Gen() ) );
				}
				ret.AddRange( caseStmt.bodyStmt.Gen( level + 2 ) );
			}
			if( elseStmt != null ) {
				ret.Add( inindent + "default:" );
				ret.AddRange( elseStmt.Gen( level + 2 ) );
			}
			ret.Add( indent + "}" );
			return ret;
		}
	}

	// 1 scope
	public class LoopStmt : Stmt
	{
		public Stmt bodyStmt;

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( Stmt subStmt in bodyStmt.EnumerateDF )
					yield return subStmt;

				yield return this;
			}
		}

		public override Strings Gen( int level )
		{
			Strings ret    = new();
			string  indent = IndentString.Repeat( level );
			ret.Add( Format( LoopFormat[1], indent, "true" ) );
			ret.Add( Format( CurlyOpen,     indent ) );
			ret.AddRange( bodyStmt.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( CurlyCloseSC, indent ) );
			return ret;
		}
	}

	// +0-1 scope
	public class ForStmt : LoopStmt
	{
		public Stmt  initStmt;
		public Expr  condExpr;
		public Expr  iterExpr;
		public Stmt? elseStmt;

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( Stmt subStmt in initStmt.EnumerateDF )
					yield return subStmt;

				if( elseStmt != null )
					foreach( Stmt subStmt in elseStmt.EnumerateDF )
						yield return subStmt;

				foreach( Stmt baseStmt in base.EnumerateDF )
					yield return baseStmt;
			}
		}

		public override Strings Gen( int level )
		{
			if( elseStmt != null )
				throw new NotImplementedException( "implement else for for-loop" );

			Strings inits = initStmt.Gen( 0 );
			if( inits.Count > 1 )
				throw new NotImplementedException( "for statement does not support more than one initializer yet" );

			Strings ret    = new();
			string  indent = IndentString.Repeat( level );
			ret.Add( Format( LoopFormat[0], indent, inits.Count == 0 ? ";" : inits.First(), condExpr.Gen(), iterExpr.Gen() ) );
			ret.Add( Format( CurlyOpen,     indent ) );
			ret.AddRange( bodyStmt.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( CurlyClose, indent ) );
			return ret;
		}
	}

	// +0-1 scope
	public class WhileStmt : LoopStmt
	{
		public Expr  condExpr;
		public Stmt? elseStmt;

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				if( elseStmt != null )
					foreach( Stmt subStmt in elseStmt.EnumerateDF )
						yield return subStmt;

				foreach( Stmt baseStmt in base.EnumerateDF )
					yield return baseStmt;
			}
		}

		public override Strings Gen( int level )
		{
			if( elseStmt != null )
				throw new NotImplementedException( "implement else for while-loop" );

			Strings ret    = new();
			string  indent = IndentString.Repeat( level );
			ret.Add( Format( LoopFormat[1], indent, condExpr.Gen() ) );
			ret.Add( Format( CurlyOpen,     indent ) );
			ret.AddRange( bodyStmt.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( CurlyClose, indent ) );
			return ret;
		}
	}

	public class DoWhileStmt : LoopStmt
	{
		public Expr condExpr;

		public override Strings Gen( int level )
		{
			Strings ret    = new();
			string  indent = IndentString.Repeat( level );
			ret.Add( Format( LoopFormat[2], indent ) );
			ret.Add( Format( CurlyOpen,     indent ) );
			ret.AddRange( bodyStmt.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( CurlyClose,    indent ) );
			ret.Add( Format( LoopFormat[3], indent, condExpr.Gen() ) );
			return ret;
		}
	}

	public class TimesStmt : LoopStmt
	{
		// HACK: solve that better
		public static int    randomNumber = 1000;
		public static string randomName => "myll_times_" + ++randomNumber;

		public Expr   countExpr;
		public string name; // opt

		public override Strings Gen( int level )
		{
			Strings ret     = new();
			string  varName = name ?? randomName;
			string  indent  = IndentString.Repeat( level );
			ret.Add(
				Format(
					LoopFormat[0],
					indent,
					"int " + varName + " = 0;",
					varName + " < "           + countExpr.Gen(),
					"++"                      + varName ) );
			ret.Add( Format( CurlyOpen, indent ) );
			ret.AddRange( bodyStmt.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( CurlyClose, indent ) );
			return ret;
		}
	}

	public class MultiStmt : Stmt
	{
		public List<Stmt> stmts = new();

		// TODO merge this with Block?
		// public bool       IsBlock { get; init; }

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( Stmt stmt in stmts )
					foreach( Stmt subStmt in stmt.EnumerateDF )
						yield return subStmt;

				yield return this;
			}
		}

		public override void AssignAttribs( Attribs inAttribs )
		{
			stmts.ForEach( v => v.AssignAttribs( inAttribs ) );
		}

		public override Strings Gen( int level )
		{
			Strings ret = stmts
				.SelectMany( obj => obj.Gen( level ) )
				.ToList();
			return ret;
		}
	}

	// 1 scope
	public class Block : Stmt
	{
		public List<Stmt> stmts;

		[Pure]
		public override IEnumerable<Stmt> EnumerateDF {
			get {
				foreach( Stmt stmt in stmts )
					foreach( Stmt subStmt in stmt.EnumerateDF )
						yield return subStmt;

				yield return this;
			}
		}

		// only overriden here
		public override Strings GenWithoutCurly( int level )
		{
			return stmts
				.SelectMany( s => s.Gen( level ) )
				.ToList();
		}

		public override Strings Gen( int level )
		{
			// Block to Block needs to indent further else it's ok to remain same level
			// The curly braces need to be outdentented one level
			Strings ret    = new();
			string  indent = IndentString.Repeat( level /*- 1*/ );
			ret.Add( Format( CurlyOpen, indent ) );
			ret.AddRange( stmts.SelectMany( s => s.Gen( level + 1 /*(s is Block ? 1 : 0)*/ ) ) );
			ret.Add( Format( CurlyClose, indent ) );
			return ret;
		}
	}
}
