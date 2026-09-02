using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Lowers <c>[chain]</c> methods and operators before name resolution.
	/// A chained method returns a reference to the enclosing class/struct;
	/// if the method is <c>[pure]</c>, the reference is <c>const</c>.
	/// </summary>
	public sealed class ChainTransformer : ITransformer
	{
		public void Transform(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules,
			List<Diagnostic> diagnostics )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				if( context.IsPrototypeFile )
					continue;

				TransformDecl( module, diagnostics );
			}
		}

		public void Transform(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
			=> Transform( modules, new List<Diagnostic>() );

		private static void TransformDecl( Decl decl, List<Diagnostic> diagnostics )
		{
			switch( decl ) {
				case Func func:
					TransformChainFunc( func, diagnostics );
					break;

				case Hierarchical h:
					foreach( Decl child in h.children )
						TransformDecl( child, diagnostics );
					break;
			}
		}

		private static void TransformChainFunc( Func func, List<Diagnostic> diagnostics )
		{
			if( !func.HasAttrib( "chain" ) )
				return;

			if( !func.retTypeIsInferred ) {
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

			if( func.IsPure )
				retType.qual = Qualifier.Const;

			func.retType          = retType;
			func.retTypeIsInferred = false;

			if( func.body == null )
				return;

			MultiStmt block = func.body is MultiStmt ms
				? ms
				: new MultiStmt( new List<Stmt> { func.body }, false );

			if( !AutoReturnTransformer.HasUnconditionalReturn( block ) ) {
				block.stmts.Add( new ReturnStmt {
					srcPos = block.srcPos,
					expr   = new SelfExpr { srcPos = block.srcPos },
				} );
			}

			func.body = block;
		}
	}
}
