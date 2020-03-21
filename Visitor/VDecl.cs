using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;

using Myll.Core;
using Myll.Generator;

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

		public Enum.Entry VisitEnumEntry( IdExprContext c )
		{
			Enum.Entry ret = new Enum.Entry {
				srcPos = c.ToSrcPos(),
				name   = VisitId( c.id() ),
				value  = c.expr().Visit(),
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
				srcPos = c.ToSrcPos(),
				name   = VisitId( c.id() ),
				access = curAccess,
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
				srcPos    = c.ToSrcPos(),
				name      = VisitId( c.id() ),
				access    = curAccess,
				tplParams = VisitTplParams( c.tplParams() ),
				paras     = VisitFuncTypeDef( c.funcTypeDef() ),
				retType   = VisitTypespec( c.typespec() ),
				block     = c.funcBody().Visit(),
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

			// HACK: but less hack than before
			StmtFormatting.SimpleGen gen = new StmtFormatting.SimpleGen(-1, 0);
			// do NOT call gen.AddNamespace( ret ).
			// Instead AddToGen() is there to call the correct virtual method on the gen
			global.AddToGen( gen );
			// last call wins, writes the output
			Program.Output     = gen.GenDecl().Join( "\n" );
			Program.OutputImpl = gen.GenImpl().Join( "\n" );

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
					srcPos   = idc.ToSrcPos(),
					name     = VisitId( idc ),
					access   = curAccess,
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
			Structural ret = new Structural {
				srcPos    = c.ToSrcPos(),
				name      = VisitId( c.id() ),
				access    = curAccess,
				kind      = c.v.ToStructuralKind(),
				tplParams = VisitTplParams( c.tplParams() ),
				bases     = VisitTypespecsNested( c.bases ),
				reqs      = VisitTypespecsNested( c.reqs ),
			};
			PushScope( ret );
			// HACK: will be buggy. needs to move to ScopeStack, when ScopeStack works.
			curAccess = (ret.kind == Structural.Kind.Class)
				? Access.Private
				: Access.Public;
			c.levDecl().Select( Visit ).Exec();
			PopScope();

			return ret;
		}

		public override Decl VisitCtorDecl( CtorDeclContext c )
		{
			PushScope();

			ConDestructor ret = new ConDestructor {
				srcPos = c.ToSrcPos(),
				access = curAccess,
				kind   = ConDestructor.Kind.Constructor,
				paras  = VisitFuncTypeDef( c.funcTypeDef() ),
				block  = c.levStmt().Visit(),
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
						srcPos   = c.ToSrcPos(),
						name     = q.id().GetText(),
						access   = curAccess,
						type     = type,
						init     = q.expr().Visit(),
						accessor = q.accessorDef().Visit(),
						// TODO: Accessors, is this still valid?
					} )
				.ToList();
			ret.ForEach( var => AddChild( var ) );
			return ret;
		}

		public override Decl VisitVariableDecl( VariableDeclContext c )
		{
			Decl ret = new VarsStmt {
				vars = c.typedIdAcors()
					.Select( VisitVars )
					.SelectMany( q => q )
					.ToList(),
			};
			// TODO save the constness
			return ret;
		}

		// HACK: will be buggy. needs to move to ScopeStack, when ScopeStack works.
		private Access curAccess = Access.None;
		public override Decl VisitAccessMod( AccessModContext c )
		{
			curAccess = c.Visit();
			return null;
		}
	}
}
