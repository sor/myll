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
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{
		public new Func.Param VisitParam( ParamContext c )
		{
			Func.Param ret = new Func.Param {
				name = VisitId( c.id() ),
				type = VisitTypespec( c.typespec() )
			};
			return ret;
		}

		public new List<Func.Param> VisitFuncTypeDef( FuncTypeDefContext c )
			=> c.param().Select( VisitParam ).ToList();

		public new Var.Accessor VisitAccessorDef( AccessorDefContext c )
		{
			Var.Accessor ret = new Var.Accessor {
				body    = c.funcBody().Visit(),
				isConst = false, // TODO
				kind    = c.v.ToAccessorKind(),
			};
			return ret;
		}

		public List<Var.Accessor> VisitAccessorsDef( AccessorDefContext[] c )
			=> c.Select( VisitAccessorDef ).ToList();

// list of typed and initialized vars
		public new List<Var> VisitTypedIdAcors( TypedIdAcorsContext c )
		{
			Typespec type = VisitTypespec( c.typespec() );
			List<Var> ret = c.idAccessors()
				.idAccessor()
				.Select( q => new Var {
					name = q.id().GetText(),
					type = type,
					init = q.expr().Visit(),
					accessor = VisitAccessorsDef(q.accessorDef()),
					// TODO: Accessors
				} )
				.ToList();
			return ret;
		}
	}

	public class DeclVisitor
		: ExtendedVisitor<Decl>
	{
		public override Decl Visit( IParseTree c )
			=> c == null
				? null
				: base.Visit( c );

		public Stack<Decl> hierarchyStack;

		public Enum.Entry VisitEnumEntry( IdExprContext c )
		{
			Console.WriteLine( "HelloVisitor enumentry" );

			Enum.Entry ret = new Enum.Entry {
				name  = VisitId( c.id() ),
				value = c.expr().Visit(),
			};
			return ret;
		}

		public List<Enum.Entry> VisitEnumEntrys( IdExprsContext c )
			=> c.idExpr().Select( VisitEnumEntry ).ToList();

		public override Decl VisitEnumDecl( EnumDeclContext c )
		{
			// TODO add to hierarchy stack
			Console.WriteLine( "HelloVisitor enum" );

			Decl ret = new Enum {
				name    = VisitId( c.id() ),
				entries = VisitEnumEntrys( c.idExprs() )
			};
			return ret;
		}

		public override Decl VisitFuncDef( FuncDefContext c )
		{
			Func ret = new Func {
				name           = VisitId( c.id() ),
				templateParams = VisitTplParams( c.tplParams() ),
				paras          = VisitFuncTypeDef( c.funcTypeDef() ),
				retType        = VisitTypespec( c.typespec() ),
				block          = c.funcBody().Visit(),
			};
			return ret;
		}

		public new List<Decl> VisitFunctionDecl( FunctionDeclContext c )
		{
			return c.funcDef().Select( VisitFuncDef ).ToList();
		}

		public override Decl VisitStructDecl( StructDeclContext c )
		{
			string               name      = VisitId( c.id() );
			List<TemplateParam>  tplParams = VisitTplParams( c.tplParams() );
			List<TypespecNested> bases     = VisitTypespecsNested( c.bases );
			List<TypespecNested> reqs      = VisitTypespecsNested( c.reqs );
			switch( c.v.Type ) {
				case STRUCT: break;
				case CLASS:  break;
				case UNION:  break;
			}
			return base.VisitStructDecl( c );
		}
	}
}
