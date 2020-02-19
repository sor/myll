using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;

using Myll.Core;
using Myll.Generator;
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
			throw new NotImplementedException( "refactored away on 20-11-2019" );
		}

		// list of typed and initialized vars
		public List<Var> VisitVars( TypedIdAcorsContext c )
		{
			Scope scope = ScopeStack.Peek();
			// determine if only scope or container
			Typespec type = VisitTypespec( c.typespec() );
			List<Var> ret = c.idAccessors()
				.idAccessor()
				.Select(
					q => new Var {
						name     = q.id().GetText(),
						type     = type,
						init     = q.expr().Visit(),
						accessor = q.accessorDef().Visit(),
						// TODO: Accessors
					} )
				.ToList();
			return ret;
		}

		public Enum.Entry VisitEnumEntry( IdExprContext c )
		{
			Enum.Entry ret = new Enum.Entry {
				name  = VisitId( c.id() ),
				value = c.expr().Visit(),
			};
			AddChild( ret );
			return ret;
		}

		public List<Enum.Entry> VisitEnumEntrys( IdExprsContext c )
			=> c.idExpr().Select( VisitEnumEntry ).ToList();
	}

	public class DeclVisitor
		: ExtendedVisitor<Decl>
	{
		public DeclVisitor( Stack<Scope> ScopeStack ) : base( ScopeStack ) {}

		public override Decl Visit( IParseTree c )
			=> c == null
				? null
				: base.Visit( c );

		public override Decl VisitEnumDecl( EnumDeclContext c )
		{
			Enum ret = new Enum {
				name = VisitId( c.id() ),
			};
			PushScope( ret );
			VisitEnumEntrys( c.idExprs() );
			PopScope();

			return ret;
		}

		public override Decl VisitFuncDef( FuncDefContext c )
		{
			// ist das hier richtig?
			PushScope();
			Func ret = new Func {
				name           = VisitId( c.id() ),
				templateParams = VisitTplParams( c.tplParams() ),
				paras          = VisitFuncTypeDef( c.funcTypeDef() ),
				retType        = VisitTypespec( c.typespec() ),
				block          = c.funcBody().Visit(),
			};
			PopScope();
			AddChild( ret );
			return ret;
		}

		public new List<Decl> VisitFunctionDecl( FunctionDeclContext c )
			=> c.funcDef().Select( VisitFuncDef ).ToList();

		public override Decl VisitProg( ProgContext c )
		{
			Namespace global = GenerateGlobalScope( c.ToSrcPos() );

			c.levDecl().Select( Visit ).Exec();

			CloseGlobalScope();

			return global;
		}

		public override Decl VisitNamespace( NamespaceContext c )
		{
			Namespace ret = null;

			CleanBodylessNamespace();

			// add new namespaces to hierarchy
			bool withBody = (c.levDecl().Length >= 1);
			foreach( IdContext idc in c.id() ) {
				Namespace ns = new Namespace {
					name     = VisitId( idc ),
					srcPos   = idc.ToSrcPos(),
					withBody = withBody,
				};
				PushScope( ns );
				if( ret == null )
					ret = ns;
			}

			// only visit children and remove hierarchy with body
			if( withBody ) {
				c.levDecl().Select( Visit ).Exec();

				// wrong order but irrelevant now
				foreach( IdContext unused in c.id() )
					PopScope();
			}

			return ret;
		}

		public override Decl VisitStructDecl( StructDeclContext c )
		{
			Console.WriteLine( "HelloVisitor struct" );

			Structural ret = new Structural {
				name      = VisitId( c.id() ),
				srcPos    = c.ToSrcPos(),
				kind      = c.v.ToStructuralKind(),
				tplParams = VisitTplParams( c.tplParams() ),
				bases     = VisitTypespecsNested( c.bases ),
				reqs      = VisitTypespecsNested( c.reqs ),
			};
			PushScope( ret );
			c.levDecl().Select( Visit ).Exec();
			PopScope();

			// HACK
			StmtFormatting.SimpleGen gen = new StmtFormatting.SimpleGen { LevelDecl = 0 };
			gen.AddStruct( ret );
			ret.Gen( gen );
			Program.Output = gen.AllDecl.Join( "\n" )
				+ "\n"
				+ gen.AllImpl.Join( "\n" );

			return ret;
		}

		public override Decl VisitCtorDecl( CtorDeclContext c )
		{
			PushScope();
			ConDestructor ret = new ConDestructor {
				kind  = ConDestructor.Kind.Constructor,
				paras = VisitFuncTypeDef( c.funcTypeDef() ),
				block = c.levStmt().Visit(),
				// TODO: cc.initList(); // opt
			};
			AddChild( ret );
			PopScope();

			return ret;
		}

		/*
		public override Decl VisitDtorDecl( DtorDeclContext c )
		{
			var cc = c.ctorDef();
			ConDestructor ret = new ConDestructor {
				kind  = ConDestructor.Kind.Constructor,
				paras = VisitFuncTypeDef( cc.funcTypeDef() ),
				//block = cc.levStmt().Visit()
				// TODO: cc.initList(); // opt
			};
			HierarchyStack.Peek().AddChild( ret );
			HierarchyStack.Push( ret );
			cc.levStmt().Visit();
			HierarchyStack.Pop();

			return ret;
		}
		*/
	}
}
