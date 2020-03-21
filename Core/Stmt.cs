using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Myll.Generator;

using static System.String;
using static Myll.Generator.StmtFormatting;

namespace Myll.Core
{
	using Strings = List<string>;

	public class Stmt
	{
		public SrcPos srcPos;

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach( var info in GetType().GetProperties() ) {
				object value = info.GetValue( this, null )
				            ?? "(null)";
				sb.Append( info.Name + ": " + value.ToString() + ", " );
			}

			sb.Length = Math.Max( sb.Length - 2, 0 );
			return "{"
			     + GetType().Name + " "
			     + sb.ToString()  + "}";
		}

		// only override in Block
		public virtual Strings GenWithoutCurly( int level )
		{
			return Gen( level );
		}

		/// <summary>
		/// This outputs immediate generated code
		/// and inserts necessary declarations through the DeclGen parameter
		/// </summary>
		/// <param name="level">level of indentation</param>
		/// <param name="gen">surrounding context</param>
		/// <returns>immediate generated lines of code</returns>
		public virtual Strings Gen( int level/*, DeclGen gen*/ )
		{
			// TODO: fix levels
			SimpleGen gen = new SimpleGen(level,level);
			AddToGen( gen );
			return gen.GenDecl();
		}

		public virtual void AddToGen( DeclGen gen )
		{
			// TODO: will become abstract once its overriden everywhere
			throw new NotImplementedException( Format( "plx override Decl Gen @ {0}", GetType().Name) );
		}
	}

	public class UsingStmt : Stmt
	{
		public List<TypespecNested> types;
	}

	public class ReturnStmt : Stmt
	{
		public Expr expr; // opt

		public override Strings Gen( int level )
		{
			return new Strings {
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
			return new Strings {
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

			string  indent = IndentString.Repeat( level );
			Strings ret    = new Strings();
			ret.Add( Format( LoopFormat[0], indent, initStmt.Gen( 0 ), condExpr.Gen(), iterExpr.Gen() ) );
			ret.Add( Format( CurlyOpen,     indent ) );
			ret.AddRange( bodyStmt.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( CurlyCloseSC, indent ) );
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
			ret.Add( Format( CurlyCloseSC, indent ) );
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
		public Expr   countExpr;
		public string name; // opt

		public static int    random_number = 1000;                          // TODO: solve that better
		public static string RandomName => "myll_times_" + ++random_number; // TODO: solve that better

		public override Strings Gen( int level )
		{
			string  varname = name ?? RandomName;
			string  indent  = IndentString.Repeat( level );
			Strings ret     = new Strings();
			ret.Add( Format( LoopFormat[0], indent, "int " + varname + " = 0", varname + " < " + countExpr.Gen(), "++" + varname ) );
			ret.Add( Format( CurlyOpen, indent ) );
			ret.AddRange( bodyStmt.GenWithoutCurly( level + 1 ) );
			ret.Add( Format( CurlyCloseSC, indent ) );
			return ret;
		}
	}

	// HACK: really waste this syntax on a from-to-loop?
	// better idea is to use this to call methods on each entry in a collection
	// e.g.
	//   Vec<Entity> ve;
	//   for( Entity& e : ve )
	//     e.update();
	// could be
	//   ve..update();
	public class EachStmt : LoopStmt
	{
		public Expr   fromExpr;
		public Expr   toExpr;
		public string name; // opt
	}

	public class MultiAssignStmt : Stmt
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

	public class AggrAssignStmt : Stmt
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
			string  indent = IndentString.Repeat( level - 1 );
			Strings ret    = new Strings();
			ret.Add( Format( CurlyOpen, indent ) );
			ret.AddRange( stmts.SelectMany( s => s.Gen( level + (s is Block ? 1 : 0) ) ) );
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
			return new Strings { indent + expr.Gen() };
		}
	}

	public class EmptyStmt : Stmt {}
}
