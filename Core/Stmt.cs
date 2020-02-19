using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		/// <summary>
		/// This outputs immediate generated code
		/// and inserts necessary declarations through the DeclGen parameter
		/// </summary>
		/// <param name="level">level of indentation</param>
		/// <param name="gen">surrounding context</param>
		/// <returns>immediate generated lines of code</returns>
		public virtual Strings Gen( int level/*, DeclGen gen*/ )
		{
			SimpleGen gen = new SimpleGen { LevelDecl = level };
			Gen( gen );
			return gen.AllDecl;
		}

		public virtual void Gen( DeclGen gen )
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
					? Format( ReturnFormat[0], Indent.Repeat( level ) )
					: Format( ReturnFormat[1], Indent.Repeat( level ), expr.Gen() )
			};
		}
	}

	public class ThrowStmt : Stmt
	{
		public Expr expr;

		public override Strings Gen( int level )
		{
			return new Strings { Format( ThrowFormat, Indent.Repeat( level ), expr.Gen() ) };
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
				Format( BreakFormat, Indent.Repeat( level ) )
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
			string indent = Indent.Repeat( level );

			Strings ret   = new Strings();
			int     index = 0;
			foreach( CondThen ifThen in ifThens ) {
				ret.Add( Format( IfFormat[index], indent, ifThen.condExpr.Gen() ) );
				ret.AddRange( ifThen.thenStmt.Gen( level + 1 ) );
				index = 1;
			}
			if( elseStmt != null ) {
				ret.Add( Format( IfFormat[2], indent ) );
				ret.AddRange( elseStmt.Gen( level + 1 ) );
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
	}

	// +0-1 scope
	public class ForStmt : LoopStmt
	{
		public Stmt initStmt;
		public Expr condExpr;
		public Expr iterExpr;
		public Stmt elseStmt; // opt
	}

	// +0-1 scope
	public class WhileStmt : LoopStmt
	{
		public Expr condExpr;
		public Stmt elseStmt; // opt
	}

	public class DoWhileStmt : LoopStmt
	{
		public Expr condExpr;
	}

	public class TimesStmt : LoopStmt
	{
		public Expr   countExpr;
		public string name; // opt
	}

	public class EachStmt : LoopStmt
	{
		public Expr   fromExpr;
		public Expr   toExpr;
		public string name; // opt
	}

	public class MultiAssignStmt : Stmt
	{
		public List<Expr> exprs;
	}

	public class AggrAssignStmt : Stmt
	{
		public Operand op;
		public Expr    leftExpr;
		public Expr    rightExpr;
	}

	// 1 scope
	public class Block : Stmt
	{
		public List<Stmt> statements;

		public override Strings Gen( int level )
		{
			// Block to Block needs to indent further else it's ok to remain same level
			// The curly braces need to be outdentented one level
			string  indent = Indent.Repeat( level - 1 );
			Strings ret    = new Strings();
			ret.Add( indent + "{" );
			ret.AddRange( statements.SelectMany( s => s.Gen( level + (s is Block ? 1 : 0) ) ) );
			ret.Add( indent + "}" );
			return ret;
		}
	}

	public class ExprStmt : Stmt
	{
		public Expr expr;

		public override Strings Gen( int level )
		{
			string indent = Indent.Repeat( level );
			return new Strings { indent + expr.Gen() };
		}
	}

	public class EmptyStmt : Stmt {}
}
