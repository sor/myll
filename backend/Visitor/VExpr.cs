using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Myll.Core;
using Myll.Resolver;

using static Myll.MyllParser;

namespace Myll
{
	/**
	 * Visit rejects a null tree by throwing, just like StmtVisitor.
	 * The other Visit... methods do not support null parameters.
	 */
	public class ExprVisitor
		: ExtendedVisitor<Expr>
	{
		public ExprVisitor( Stack<Scope> scopeStack ) : base( scopeStack ) {}

		public ExprVisitor( CompilationContext context ) : base( context ) {}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override Expr Visit( IParseTree c )
		{
			if( c == null )
				throw new ArgumentNullException();

			Expr ret = base.Visit( c )
				?? throw new InvalidOperationException( "Unexpected terminal or unrecognized expression context" );

			// Any expression node that did not get a source position from its
			// specific visitor inherits the span of the context that produced it.
			// This keeps resolver/type-checker diagnostics location-aware.
			if( ret.srcPos == null && c is ParserRuleContext prc )
				ret.srcPos = prc.ToSrcPos();

			return ret;
		}

		public override ScopedExpr VisitScopedExpr( ScopedExprContext c )
		{
			ScopedExpr ret = new() {
				srcPos = c.ToSrcPos(),
				op     = Operand.Scoped,
				idTpls = c.idTplArgs().Select( VisitIdTplArgs ).ToList(),
			//	expr   = c.expr().Visit(),
			};
			Context.UnresolvedScopeds.Add( new( ret, Context.ScopeStack.Peek() ) );
			return ret;
		}

		public override Expr VisitPostExpr( PostExprContext c )
		{
			Expr ret;
			Expr left = c.expr().Visit( Context );
			if( c.postOP() != null ) {
				ret = new UnOp {
					expr = left,
					op   = c.postOP().v.ToOp(),
				};
			}
			else if( c.funcCall() != null ) {
				Context.FuncCallCallees.Add( left );
				FuncCall funcCall = VisitFuncCall( c.funcCall() );
				Context.UnresolvedCalls.Add( new UnresolvedCall( left, funcCall, Context.ScopeStack.Peek() ) );
				ret = new FuncCallExpr {
					op       = c.funcCall().ary.ToOp(),
					expr     = left,
					funcCall = funcCall,
				};
			}
			else if( c.indexCall() != null ) {
				ret = new FuncCallExpr {
					op       = c.indexCall().ary.ToOp(),
					expr     = left,
					funcCall = VisitIndexCall( c.indexCall() ),
				};
			}
			else if( c.memAccOP() != null ) {
				IdExpr right = new() {
					srcPos    = c.idTplArgs().ToSrcPos(),
					op        = Operand.Id,
					idTplArgs = VisitIdTplArgs( c.idTplArgs() ),
				};
				ret = new BinOp {
					srcPos = c.ToSrcPos(),
					op     = c.memAccOP().v.ToOp(),
					left   = left,
					right  = right,
				};
				Context.UnresolvedMemberAccesses.Add( new( (BinOp) ret, Context.ScopeStack.Peek() ) );
			}
			else {
				throw new Exception( "unknown post op" );
			}

			return ret;
		}

		public override NewExpr VisitNewExpr( NewExprContext c )
		{
			NewExpr ret = new() {
				op       = Operand.New,
				type     = VisitTypespec( c.typespec() ),
				funcCall = c.funcCall() != null
					? VisitFuncCall( c.funcCall() )
					: new FuncCall(),
			};
			return ret;
		}

		public override Expr VisitPreExpr( PreExprContext c )
		{
			Expr ret;
			Expr expr = c.expr().Visit( Context );

			if( c.LPAREN() != null ) {
				// c style cast
				Operand  op;
				Typespec type;
				if( c.COPY() != null ) {
					op = Operand.CopyCast;
					throw new NotImplementedException( "copy-cast might need to introduce a new local to work" );
				}
				else if( c.MOVE() != null ) {
					op = Operand.MoveCast;
					type = new TypespecNested {
						srcPos = c.ToSrcPos(),
						idTpls = new() { new() { id = "std::move" } }
					};
				}
				else if( c.FORWARD() != null ) {
					op = Operand.ForwardCast;
					type = new TypespecNested {
						srcPos = c.ToSrcPos(),
						idTpls = new() { new() { id = "std::forward" } }
					};
				}
				else if( c.cv != null ) {
					bool isPlus  = (c.PLUS()  != null);
					bool isConst = (c.CONST() != null);
					op = isPlus
						? Operand.AddCVCast
						: Operand.RemoveCVCast;

					/*
					string typemod = isPlus
						? (isConst
							? "std::add_const_t"
							: "std::add_volatile_t")
						: (isConst
							? "std::remove_const_t"
							: "std::remove_volatile_t");
					*/

					string typemod = string.Format(
						"std::{0}_{1}_t",
						isPlus  ? "add"   : "remove",
						isConst ? "const" : "volatile" );

					type = new TypespecNested {
						srcPos = c.ToSrcPos(),
						idTpls = new() { new() { id = typemod } }
					};
				}
				else {
					int emCount = c.EM().Length;
					op = c.QM()   != null ? Operand.DynamicCast :
						c.MINUS() != null ? Operand.ConstCast :
						emCount   == 1    ? Operand.BitCast :
						emCount   == 2    ? Operand.ReinterpretCast :
						                    Operand.StaticCast;

					type = VisitTypespec( c.typespec() );
				}

				ret = new CastExpr {
					op   = op,
					type = type,
					expr = expr,
				};
			}
			else if( c.preOP() != null ) {
				ret = new UnOp {
					op   = c.preOP().v.ToPreOp(),
					expr = expr,
				};
			}
			else if( c.SIZEOF() != null ) {
				ret = new UnOp {
					op   = Operand.SizeOf,
					expr = expr,
				};
			}
			else if( c.DELETE() != null ) {
				ret = new UnOp {
					op   = c.ary != null ? Operand.DeleteAry : Operand.Delete,
					expr = expr,
				};
			}
			else {
				throw new InvalidOperationException( "unknown pre-expr " + c );
			}

			return ret;
		}

		// TODO: check if this really works
		public override BinOp VisitMemPtrExpr( MemPtrExprContext c )
		{
			BinOp ret = new() {
				op    = c.memAccPtrOP().v.ToOp(),
				left  = c.expr( 0 ).Visit( Context ),
				right = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		public override BinOp VisitPowExpr( PowExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.Pow,
				left  = c.expr( 0 ).Visit( Context ),
				right = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		public override BinOp VisitMultExpr( MultExprContext c )
		{
			BinOp ret = new() {
				op    = c.multOP().v.ToOp(),
				left  = c.expr( 0 ).Visit( Context ),
				right = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		public override BinOp VisitAddExpr( AddExprContext c )
		{
			BinOp ret = new() {
				op    = c.addOP().v.ToOp(),
				left  = c.expr( 0 ).Visit( Context ),
				right = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		public override BinOp VisitShiftExpr( ShiftExprContext c )
		{
			BinOp ret = new() {
				op    = c.shiftOP().LSHIFT() != null ? Operand.LeftShift : Operand.RightShift,
				left  = c.expr( 0 ).Visit( Context ),
				right = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		public override BinOp VisitComparisonExpr( ComparisonExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.Comparison,
				left  = c.expr( 0 ).Visit( Context ),
				right = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		// TODO: Add a way to disable a < b < c
		//                      to be a < b && b < c
		// do braces help?
		private class FlattenRelational
		{
			private readonly List<Expr>          exprs   = new( 4 );
			private readonly List<Operand>       ops     = new( 4 );
			private readonly CompilationContext  context;

			public FlattenRelational( IRelEqExprContext c, CompilationContext context )
			{
				this.context = context;
				Descent( c );
			}

			private void Descent( IRelEqExprContext c )
			{
				{
					ExprContext l = c.expr( 0 );
					if( l is IRelEqExprContext lre ) Descent( lre );
					else exprs.Add( l.Visit( context ) );
				}
				ops.Add( c.Op );
				{
					ExprContext r = c.expr( 1 );
					if( r is IRelEqExprContext rre ) Descent( rre );
					else exprs.Add( r.Visit( context ) );
				}
			}

			public BinOp VisitWithAnd()
			{
				BinOp left = new() {
					op    = ops[0],
					left  = exprs[0],
					right = exprs[1],
				};

				for( int i = 1; i < ops.Count; ++i ) {
					BinOp right = new() {
						op    = ops[i],
						left  = exprs[i],
						right = exprs[i +1],
					};
					left = new() {
						op    = Operand.And,
						left  = left,
						right = right,
					};
				}

				return left;
			}
		}

		public override BinOp VisitRelationExpr( RelationExprContext c )
		{
			FlattenRelational flat = new( c, Context );
			BinOp             ret  = flat.VisitWithAnd();
			return ret;
		}

		public override BinOp VisitEqualityExpr( EqualityExprContext c )
		{
			FlattenRelational flat = new( c, Context );
			BinOp             ret  = flat.VisitWithAnd();
			return ret;
		}

		public override BinOp VisitAndExpr( AndExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.And,
				left  = c.expr( 0 ).Visit( Context ),
				right = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		public override BinOp VisitOrExpr( OrExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.Or,
				left  = c.expr( 0 ).Visit( Context ),
				right = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		public override BinOp VisitNullCoalesceExpr( NullCoalesceExprContext c )
		{
			BinOp ret = new() {
				op    = Operand.NullCoalesce,
				left  = c.expr( 0 ).Visit( Context ),
				right = c.expr( 1 ).Visit( Context ),
			};
			return ret;
		}

		public override TernOp VisitConditionalExpr( ConditionalExprContext c )
		{
			TernOp ret = new() {
				op    = Operand.Conditional,
				left  = c.expr( 0 ).Visit( Context ),
				mid   = c.expr( 1 ).Visit( Context ),
				right = c.expr( 2 ).Visit( Context ),
			};
			return ret;
		}

		public override Lambda VisitLambdaExpr( LambdaExprContext c )
		{
			// TODO this is probably just one big hack
			PushScope(); // is this needed here?
			Lambda ret = new();
			Func func = new() {
				srcPos    = c.ToSrcPos(),
				TplParams = VisitTplParams( c.tplParams() ),
				paras     = VisitFuncTypeDef( c.funcTypeDef() ).ToList(),
			};
			func.retType = c.typespec() != null ? VisitTypespec( c.typespec() ) :
				func.IsReturningSomething ?
					new TypespecBasic {
						kind = TypespecBasic.Kind.Auto,
						size = TypespecBasic.SizeUndetermined,
					} :
					new TypespecBasic {
						kind = TypespecBasic.Kind.Void,
						size = TypespecBasic.SizeInvalid,
					};
			AddParamsToScope( func.paras );
			func.body = c.funcBody().Visit( Context );
			ret.func = func;
			PopScope();

			return ret;
		}

		public override UnOp VisitParenExpr( ParenExprContext c )
		{
			UnOp ret = new() {
				op   = Operand.Parens,
				expr = c.expr().Visit( Context ),
			};
			return ret;
		}

		public override Expr VisitWildIdExpr( WildIdExprContext c )
		{
			Expr          ret;
			WildIdContext cc = c.wildId();
			if( cc.USCORE() != null ) {
				ret = new Discard {
					srcPos = c.ToSrcPos(),
				};
			}
			else if( cc.AUTOINDEX() != null ) {
				IdTplArgs idTplArgs = new() { id = cc.AUTOINDEX().GetText() };
				ret = new IdExpr {
					srcPos    = c.ToSrcPos(),
					op        = Operand.WildId,
					idTplArgs = idTplArgs,
				};
			}
			else {
				throw new Exception( "unknown wildId op" );
			}

			return ret;
		}

		public new Expr VisitLit( LitContext c )
		{
			string text = c.GetText();
			if( text == "self" )
				return CreateSelfExpr( c );
			if( text == "this" )
				return CreateThisExpr( c );

			Literal ret = new() {
				op   = Operand.Literal,
				text = text // TODO
			};
			return ret;
		}

		// TODO remove this or the other above?
		public override Expr VisitLiteralExpr( LiteralExprContext c )
		{
			return VisitLit( c.lit() );
		}

		private Structural? FindEnclosingStructural()
		{
			Scope? scope = Context.ScopeStack.Peek();
			for( Scope? cur = scope; cur != null; cur = cur.parent )
				if( cur.decl is Structural structural )
					return structural;

			return null;
		}

		private SelfExpr CreateSelfExpr( ParserRuleContext c )
		{
			SelfExpr ret = new() {
				op     = Operand.Literal,
				srcPos = c.ToSrcPos(),
			};

			if( FindEnclosingStructural() is Structural structural ) {
				ret.Type = new TypespecNested {
					resolvedDecl = structural,
					idTpls       = new() { new() { id = structural.name } },
				};
			}

			return ret;
		}

		private ThisExpr CreateThisExpr( ParserRuleContext c )
		{
			ThisExpr ret = new() {
				op     = Operand.Literal,
				srcPos = c.ToSrcPos(),
			};

			if( FindEnclosingStructural() is Structural structural ) {
				ret.Type = new TypespecNested {
					resolvedDecl = structural,
					idTpls       = new() { new() { id = structural.name } },
					ptrs         = new() { new Pointer { kind = Pointer.Kind.RawPtr } },
				};
			}

			return ret;
		}

		public override IdExpr VisitIdTplExpr( IdTplExprContext c )
		{
			IdExpr ret = new() {
				srcPos    = c.ToSrcPos(),
				op        = Operand.Id,
				idTplArgs = VisitIdTplArgs( c.idTplArgs() ),
			};
			Context.UnresolvedIds.Add( new( ret, Context.ScopeStack.Peek() ) );
			return ret;
		}

		public override Expr VisitRangeExpr( RangeExprContext c )
			=> throw new NotImplementedException( "range expressions are not implemented yet" );

		public override Expr VisitThrowExpr( ThrowExprContext c )
			=> throw new NotImplementedException( "throw expressions are not implemented yet" );

		public override Expr VisitThreeWayConditionalExpr( ThreeWayConditionalExprContext c )
			=> throw new NotImplementedException(
				"three-way conditional expressions are not implemented yet" );
	}
}
