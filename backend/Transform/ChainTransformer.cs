using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Transform
{
	/// <summary>
	/// Lowers <c>[chain]</c> methods and operators before name resolution.
	/// A chained method returns a reference to the enclosing class/struct;
	/// if the method is <c>[pure]</c>, the reference is <c>const</c>.
	/// </summary>
	public sealed class ChainTransformer : Transformer
	{
		protected override void PreTransform( TransformContext ctx )
		{
			Rewriter walker = new( Diagnostics );
			walker.WalkModule( ctx.Module.Module );
		}

	private sealed class Rewriter : DeclWalker
	{
		private readonly List<Diagnostic> diagnostics;

		public Rewriter( List<Diagnostic> diagnostics ) => this.diagnostics = diagnostics;

		protected override void TransformDecl( Decl decl )
		{
			if( decl is Func func )
				TransformChainFunc( func, diagnostics );

			base.TransformDecl( decl );
		}
	}

		private static bool HasTrailingReturnSelf( Stmt stmt )
		{
			return stmt switch {
				ReturnStmt rs when rs.expr is SelfExpr => true,
				MultiStmt ms when ms.stmts.Count > 0
					=> HasTrailingReturnSelf( ms.stmts[ms.stmts.Count - 1] ),
				IfStmt ifs when ifs.els != null
					=> ifs.ifThens.TrueForAll( ct => HasTrailingReturnSelf( ct.then ) )
					&& HasTrailingReturnSelf( ifs.els ),
				SwitchStmt sw when sw.els != null
					=> sw.cases.TrueForAll( cb => HasTrailingReturnSelf( cb.then ) )
					&& HasTrailingReturnSelf( sw.els ),
				TryCatchStmt tcs
					=> HasTrailingReturnSelf( tcs.tryBody )
					&& tcs.catches.TrueForAll( cc => HasTrailingReturnSelf( cc.body ) ),
				_ => false,
			};
		}

	private static void TransformChainFunc( Func func, List<Diagnostic> diagnostics )
		{
			if( !func.HasAttrib( "chain" ) )
				return;

			if( func.retType is not TypespecBasic { kind: TypespecBasic.Kind.ImplicitAuto } ) {
				diagnostics.Add( new Diagnostic(
					func.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"[chain] method/operator '{0}' must not declare a return type",
						func.name ) ) );
				return;
			}

			if( !func.IsInStruct || func.scope?.parent?.decl is not Structural structural ) {
				diagnostics.Add( new Diagnostic(
					func.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"[chain] can only be used on instance methods and operators ('{0}')",
						func.name ) ) );
				return;
			}

			TypespecNested retType = new() {
				srcPos       = func.srcPos,
				resolvedDecl = structural,
				ptrs         = new List<Pointer> { new() { kind = Pointer.Kind.LVRef } },
			};

			if( func.IsConstMethod )
				retType.qual = Qualifier.Const;

			func.retType = retType;

			if( func.body == null )
				return;

			MultiStmt block = func.body is MultiStmt ms
				? ms
				: new MultiStmt( new List<Stmt> { func.body }, false );

			if( HasTrailingReturnSelf( block ) ) {
				diagnostics.Add( new Diagnostic(
					func.srcPos,
					DiagnosticKind.Warning,
					String.Format(
						"[chain] method/operator '{0}' has a redundant trailing 'return self;'.",
						func.name ) ) );
			}
			else if( !AutoReturnTransformer.HasUnconditionalReturn( block ) ) {
				block.stmts.Add( new ReturnStmt {
					srcPos = block.srcPos,
					expr   = new SelfExpr { srcPos = block.srcPos },
				} );
			}

			func.body = block;
		}
	}
}
