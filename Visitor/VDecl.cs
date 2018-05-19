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
				entries = c.idExprs()
					.idExpr()
					.Select( VisitEnumEntry )
					.ToList(),
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

		public override object VisitFuncDef( FuncDefContext c )
		{
			Func ret = new Func {
				name           = VisitId( c.id() ),
				templateParams = VisitTplParams( c.tplParams() ),
				paras          = VisitFuncTypeDef( c.funcTypeDef() ),
				retType        = VisitTypeSpec( c.typeSpec() ),
			};

			if( c.levStmt() != null ) {
				ret.block = c.levStmt().Visit();
			}
			else if( c.expr() != null ) {
				ret.block = new ReturnStmt {
					expr = c.expr().Visit(),
				};
			}
			else throw new Exception( "unknown function decl body" );
			return ret;
		}

		public override object VisitFunctionDecl( FunctionDeclContext c )
		{
			return c.funcDef().Select( VisitFuncDef ).ToList();
		}

		// list of typed and initialized vars
		public new List<Var> VisitTypedIdExprs( TypedIdExprsContext c )
		{
			Typespec type = VisitTypeSpec( c.typeSpec() );
			List<Var> ret = c.idExprs()
				.idExpr()
				.Select( q => new Var {
					name = q.id().GetText(),
					type = type,
					init = q.expr().Visit(),
				} )
				.ToList();
			return ret;
		}
	}
}
