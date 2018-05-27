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
	public class Symbol
	{
		public enum Kind
		{
			Namespace,
			Type,
			Var,
			Func,
		}

		public string       name;
		public Kind         kind;
		public List<string> tpl;
		public List<Symbol> children;
		public List<Symbol> overlays;
		public Decl         impl; // may be null if not loaded
	}

	public partial class Visitor
		: MyllParserBaseVisitor<object>
	{
		public Stack<Decl> hierarchyStack;

		public Enum.Entry VisitEnumEntry( IdExprContext c )
		{
			Enum.Entry ret = new Enum.Entry {
				name  = VisitId( c.id() ),
				value = c.expr().Visit(),
			};
			return ret;
		}

		public List<Enum.Entry> VisitEnumEntrys( IdExprsContext c )
			=> c.idExpr().Select( VisitEnumEntry ).ToList();

		#region NEW
		public new Func.Param VisitParam( ParamContext c )
		{
			Func.Param ret = new Func.Param {
				name = VisitId( c.id() ),
				type = VisitTypeSpec( c.typeSpec() )
			};
			return ret;
		}

		public new List<Func.Param> VisitFuncTypeDef( FuncTypeDefContext c )
			=> c.param().Select( VisitParam ).ToList();

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
		#endregion

		#region OVERRIDES
		public override object VisitEnumDecl( EnumDeclContext c )
		{
			// TODO add to hierarchy stack

			Enum ret = new Enum {
				name    = VisitId( c.id() ),
				entries = VisitEnumEntrys( c.idExprs() )
			};
			return ret;
		}

		public override object VisitFuncDef( FuncDefContext c )
		{
			Func ret = new Func {
				name           = VisitId( c.id() ),
				templateParams = VisitTplParams( c.tplParams() ),
				paras          = VisitFuncTypeDef( c.funcTypeDef() ),
				retType        = VisitTypeSpec( c.typeSpec() ),
				block          = c.funcBody().Visit(),
			};

			return ret;
		}

		public override object VisitClassDecl( ClassDeclContext c )
		{
			string name = c.id().GetText();
			List<TemplateParam> tplParams = VisitTplParams( c.tplParams() );
			switch( c.v.Type ) {
				case CLASS: break;
				case STRUCT: break;
				case UNION: break;
			}
			return base.VisitClassDecl( c );
		}

		public override object VisitFunctionDecl( FunctionDeclContext c )
		{
			return c.funcDef().Select( VisitFuncDef ).ToList();
		}
		#endregion
	}
}
