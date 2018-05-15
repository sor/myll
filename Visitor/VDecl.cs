using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Myll.Core;

using Array = Myll.Core.Array;
using Enum = Myll.Core.Enum;

using static Myll.MyllParser;

namespace Myll
{
	public partial class Visitor : MyllParserBaseVisitor<object>
	{
		public Enum.Entry VisitEnumEntry( IdExprContext c )
		{
			Enum.Entry ret = new Enum.Entry {
				name  = VisitId( c.id() ),
				value = c.expr().Visit(),
			};
			return ret;
		}

		public override object VisitEnumDecl( EnumDeclContext c )
		{
			// TODO add to hierarchy stack
			Enum ret = new Enum {
				name    = VisitId( c.id() ),
				entries = c.idExpr().Select( VisitEnumEntry ).ToList(),
			};
			return ret;
		}

		public new Func.Param VisitParam( ParamContext c )
		{
			Func.Param ret = new Func.Param {
				name = VisitId( c.id() ),
				type = VisitTypeSpec( c.typeSpec() )
			};
			return ret;
		}

		public new List<Func.Param> VisitFuncTypeDef( FuncTypeDefContext c )
		{
			List<Func.Param> ret = c.param().Select( VisitParam ).ToList();
			return ret;
		}

		public override object VisitFunctionDecl( FunctionDeclContext c )
		{
			// TODO rework cc's
			FuncDefContext cc = c.funcDef();
			Func ret = new Func {
				name           = VisitId( cc.id() ),
				templateParams = VisitTplParams( cc.tplParams() ),
				paras          = VisitFuncTypeDef( cc.funcTypeDef() ),
				retType        = VisitTypeSpec( cc.typeSpec() ),
			};

			if( cc.stmt() != null ) {
				ret.block = cc.stmt().Visit();
			}
			else if( cc.expr() != null ) {
				ret.block = new ReturnStmt {
					expr = cc.expr().Visit(),
				};
			}
			else throw new Exception( "unknown function decl body" );
			return ret;
		}

		// list of typed and initialized vars
		public new List<Var> VisitTypedIdExprs( TypedIdExprsContext c )
		{
			Typespec type = VisitTypeSpec( c.typeSpec() );
			List<Var> ret = c.idExpr().Select( q => new Var {
				name = q.id().GetText(),
				type = type,
				init = q.expr().Visit(),
			} ).ToList();
			return ret;
		}
	}
}
