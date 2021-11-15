using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Myll.Generator;

namespace Myll.Core
{
	using static String;
	using static StmtFormatting;

	using Strings = List<string>;
	using Attribs = Dictionary<string, List<string>>;

	public class Stmt
	{
		public  SrcPos  srcPos;
		private Attribs attribs { get; set; }

		public bool IsStatic => IsAttrib( "static" );

		public bool IsAttrib( string key )
			=> attribs != null
			&& attribs.ContainsKey( key );

		public bool IsAttrib( string key, string value )
			=> attribs != null
			&& attribs.TryGetValue( key, out Strings values )
			&& values.Contains( value );

		public virtual void AssignAttribs( Attribs inAttribs )
		{
			attribs = inAttribs;

			AttribsAssigned();
		}

		// this is the analyze-for-dummies until analyze works
		protected virtual void AttribsAssigned() {}

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach( var info in GetType().GetProperties() ) {
				object value = info.GetValue( this, null )
				            ?? "(null)";
				sb.Append( info.Name + ": " + value + ", " );
			}

			sb.Length = Math.Max( sb.Length - 2, 0 );
			return "{"
			     + GetType().Name + " "
			     + sb             + "}";
		}

		// only override in Block
		public virtual Strings GenWithoutCurly( int level )
		{
			return Gen( level );
		}

		// Shouldn't this be abstract?
		/// <summary>
		/// This outputs immediate generated code
		/// and inserts necessary declarations through the DeclGen parameter
		/// </summary>
		/// <param name="level">level of indentation</param>
		/// <returns>immediate generated lines of code</returns>
		public virtual Strings Gen( int level )
		{
			throw new NotImplementedException(
				Format(
					"plx implement in missing class: {0}",
					GetType().Name ) );
		}
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

	public class MultiStmt : Stmt
	{
		public List<Stmt> stmts;

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

	public class UsingStmt : Stmt
	{
		public List<TypespecNested> types;

		// in locations where C++ does not support "using namespace" this must not be printed
		// but instead the unqualified types need to be changed to qualified ones
		public override Strings Gen( int level )
		{
			string indent = IndentString.Repeat( level );
			return types
				.Select( o => Format( "{0}using namespace {1};", indent, o.Gen() ) )
				.ToList();
		}
	}

	public class ReturnStmt : Stmt
	{
		public Expr expr; // opt

		public override Strings Gen( int level )
		{
			return new() {
				expr == null
					? Format( ReturnFormat[0], IndentString.Repeat( level ) )
					: Format( ReturnFormat[1], IndentString.Repeat( level ), expr.Gen() )
			};
		}
	}

	public class ThrowStmt : Stmt
	{
		public Expr expr;

		public override Strings Gen( int level )
		{
			return new() {
				Format( ThrowFormat, IndentString.Repeat( level ), expr.Gen() )
			};
		}
	}

	public class BreakStmt : Stmt
	{
		public int depth; // C++ default is 1, break one level

		public override Strings Gen( int level )
		{
			if( depth != 1 )
				throw new NotImplementedException(
					"no depth except 1 supported directly, analyze step must take care of this!" );

			return new Strings {
				Format( BreakFormat, IndentString.Repeat( level ) )
			};
		}
	}

	// TODO move or remove?
	public struct CondThen
	{
		public Expr condExpr;
		public Stmt thenStmt;
	}

	// 1-2 scopes
	public class IfStmt : Stmt
	{
		public List<CondThen> ifThens;
		public Stmt           elseStmt; // opt

		public override Strings Gen( int level )
		{
			string  indent = IndentString.Repeat( level );
			Strings ret    = new Strings();
			int     index  = 0;
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

	public class CaseStmt : Stmt
	{
		public List<Expr> caseExprs;
		public List<Stmt> bodyStmts;
		public bool       autoBreak;
	}

	public class SwitchStmt : Stmt
	{
		public Expr           condExpr;
		public List<CaseStmt> caseStmts;
		public List<Stmt>     elseStmts; // opt
	}

	// 1 scope
	public class LoopStmt : Stmt
	{
		public Stmt bodyStmt;

		public override Strings Gen( int level )
		{
			string  indent = IndentString.Repeat( level );
			Strings ret    = new Strings();
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
		public Stmt initStmt;
		public Expr condExpr;
		public Expr iterExpr;
		public Stmt elseStmt; // opt

		public override Strings Gen( int level )
		{
			if( elseStmt != null )
				throw new NotImplementedException( "implement else for for-loop" );

			Strings inits = initStmt.Gen( 0 );
			if( inits.Count > 1 )
				throw new NotImplementedException( "for statement does not support more than one initializer yet" );

			string  indent = IndentString.Repeat( level );
			Strings ret    = new Strings();
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
		public Expr condExpr;
		public Stmt elseStmt; // opt

		public override Strings Gen( int level )
		{
			if( elseStmt != null )
				throw new NotImplementedException( "implement else for while-loop" );

			string  indent = IndentString.Repeat( level );
			Strings ret    = new Strings();
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
			string  indent = IndentString.Repeat( level );
			Strings ret    = new Strings();
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
			string  varName = name ?? randomName;
			string  indent  = IndentString.Repeat( level );
			Strings ret     = new Strings();
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
		{
			string indent = IndentString.Repeat( level );
			return new Strings {
				indent + Format( op.GetAssignFormat(), leftExpr.Gen(), rightExpr.Gen() ) + ";"
			};
		}
	}

	// 1 scope
	public class Block : Stmt
	{
		public List<Stmt> stmts;

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
			string  indent = IndentString.Repeat( level /*- 1*/ );
			Strings ret    = new Strings();
			ret.Add( Format( CurlyOpen, indent ) );
			ret.AddRange( stmts.SelectMany( s => s.Gen( level + 1 /*(s is Block ? 1 : 0)*/ ) ) );
			ret.Add( Format( CurlyClose, indent ) );
			return ret;
		}
	}

	public class ExprStmt : Stmt
	{
		public Expr expr;

		public override Strings Gen( int level )
		{
			string indent = IndentString.Repeat( level );
			return new Strings { Format( "{0}{1};", indent, expr.Gen() ) };
		}
	}

	public class EmptyStmt : Stmt {}
}
