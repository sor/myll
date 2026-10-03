using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Transform
{
	/// <summary>
	/// Lowers the configured implicit return variable (default <c>ret</c>) to an explicit
	/// local variable and a trailing <c>return ret;</c> when needed.
	///
	/// This runs <em>before</em> name resolution so that <c>ret</c> is a normal local
	/// variable by the time the resolver walks the tree.
	///
	/// Generation is suppressed when:
	/// <list type="bullet">
	/// <item>The return type is <c>void</c> or a reference type.</item>
	/// <item>The name is shadowed by a parameter, local variable, catch parameter,
	/// class/struct member, or global declaration.</item>
	/// <item><c>ret</c> is never used in the body.</item>
	/// </list>
	/// </summary>
	public sealed class AutoReturnTransformer : Transformer
	{
		private static readonly ConfiguredAliasShadowingTransformer conflictChecker = new();

		protected override void PreTransform( TransformContext ctx )
		{
			Rewriter walker = new( ctx.Module.Module, ctx.Module.Context, Diagnostics );
			walker.WalkModule( ctx.Module.Module );
		}

		private sealed class Rewriter : DeclWalker
		{
			private readonly GlobalNamespace module;
			private readonly ModuleContext context;
			private readonly List<Diagnostic> diagnostics;

			public Rewriter( GlobalNamespace module, ModuleContext context, List<Diagnostic> diagnostics )
			{
				this.module     = module;
				this.context    = context;
				this.diagnostics = diagnostics;
			}

			protected override void TransformDecl( Decl decl )
			{
				if( decl is Func func )
					TryTransformFunc( func, module, context, diagnostics );

				base.TransformDecl( decl );
			}
		}

		private static void TryTransformFunc(
			Func func,
			GlobalNamespace module,
			ModuleContext context,
			List<Diagnostic> diagnostics )
		{
			if( func.body == null )
				return;

			if( func.retType is TypespecBasic { kind: TypespecBasic.Kind.ExplicitAuto or TypespecBasic.Kind.ImplicitAuto } ) {
				if( UsesAutoReturnName( func.body, context.Dialect.AutoReturnName ) ) {
					diagnostics.Add( new Diagnostic(
						func.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Auto-return variable '{0}' cannot be used when the return type is inferred; declare a concrete return type or use explicit return statements.",
							context.Dialect.AutoReturnName ) ) );
				}

				return;
			}

			string alias = context.Dialect.AutoReturnName;
			if( String.IsNullOrEmpty( alias ) )
				return;

			if( conflictChecker.HasAutoReturnConflict( func, context ) ) {
				diagnostics.Add( new Diagnostic(
					func.srcPos,
					DiagnosticKind.Warning,
					String.Format(
						"Function '{0}' declares '{1}'; auto-return generation is disabled.",
						func.name,
						alias ) ) );
				return;
			}

			if( IsUnsupportedReturnType( func.retType, out string? reason ) ) {
				if( reason != null && UsesAutoReturnName( func.body, alias ) ) {
					diagnostics.Add( new Diagnostic(
						func.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Auto-return disabled in '{0}': {1}",
							func.name,
							reason ) ) );
				}

				return;
			}

			if( IsShadowedByMemberOrGlobal( func, module, alias ) ) {
				diagnostics.Add( new Diagnostic(
					func.srcPos,
					DiagnosticKind.Warning,
					String.Format(
						"Auto-return disabled in '{0}': '{1}' is already declared in an outer scope.",
						func.name,
						alias ) ) );
				return;
			}

			if( !UsesAutoReturnName( func.body, alias ) )
				return;

			if( func.funcScope == null )
				return;

			MultiStmt block = func.body is MultiStmt ms
				? ms
				: new MultiStmt( new List<Stmt> { func.body }, false );

			var retVar = new VarDecl {
				srcPos = block.srcPos,
				name   = alias,
				kind   = VarDecl.Kind.Var,
				access = Access.Public,
				type   = func.retType,
				IsLocal = true,
			};

			var retStmt = new VarStmt {
				srcPos       = block.srcPos,
				kind         = VarDecl.Kind.Var,
				name         = alias,
				type         = func.retType,
				IsAutoReturn = true,
			};

			Scope functionScope = func.funcScope;

			ScopeLeaf scopeLeaf = new() {
				parent = functionScope,
				decl   = retVar,
			};
			retVar.scope = functionScope;

			if( !functionScope.children.TryGetValue( alias, out List<ScopeLeaf>? list ) ) {
				list = new List<ScopeLeaf>( 1 );
				functionScope.children.Add( alias, list );
			}

			list.Add( scopeLeaf );
			context.LocalDecls.Add( (retVar, functionScope) );

			block.stmts.Insert( 0, retStmt );

			if( !HasUnconditionalReturn( block ) ) {
				block.stmts.Add( new ReturnStmt {
					srcPos = block.srcPos,
					expr   = MakeRetId( alias, block.srcPos ),
				} );
			}

			func.body = block;
		}

		private static IdExpr MakeRetId( string alias, SrcPos srcPos )
			=> new() {
				op         = Operand.Id,
				srcPos     = srcPos,
				idTplArgs  = new IdTplArgs { id = alias },
			};

		private static bool IsUnsupportedReturnType( Typespec retType, out string? reason )
		{
			reason = null;

			if( retType is TypespecBasic { kind: TypespecBasic.Kind.Void }
			 && ( retType.ptrs == null || retType.ptrs.Count == 0 ) ) {
				reason = "return type is void";
				return true;
			}

			if( retType.ptrs is { Count: > 0 } ptrs ) {
				Pointer.Kind outer = ptrs[ptrs.Count - 1].kind;
				if( outer == Pointer.Kind.LVRef || outer == Pointer.Kind.RVRef ) {
					reason = "reference return types cannot be default-initialized";
					return true;
				}
			}

			return false;
		}

		private static bool IsShadowedByMemberOrGlobal( Func func, GlobalNamespace module, string alias )
		{
			if( func.IsInStruct && func.scope?.parent?.decl is Structural structural
			 && structural.children.Any( c => c.name == alias ) )
				return true;

			if( module.scope.children.ContainsKey( alias ) )
				return true;

			return false;
		}

		/// <summary>
		/// Scans a statement tree for a plain, non-template use of the alias name.
		/// Lambda bodies are not entered: the alias must be used in the function
		/// itself for the return-variable generation to kick in.
		/// </summary>
		private sealed class NameFinder : DeclWalker
		{
			private readonly string alias;
			private bool found;

			public NameFinder( string alias ) => this.alias = alias;

			public bool Find( Stmt? stmt )
			{
				if( stmt != null )
					WalkStmt( stmt );
				return found;
			}

			protected override void TransformExpr( Expr expr )
			{
				if( expr is IdExpr id && id.idTplArgs.id == alias && id.idTplArgs.tplArgs.Count == 0 ) {
					found = true;
					return;
				}

				expr.VisitSubExprs( WalkExpr );
			}
		}

		private static bool UsesAutoReturnName( Stmt stmt, string alias )
			=> new NameFinder( alias ).Find( stmt );

		internal static bool HasUnconditionalReturn( Stmt stmt )
		{
			return stmt switch {
				ReturnStmt or ThrowStmt => true,
				MultiStmt ms when ms.stmts.Count > 0
					=> HasUnconditionalReturn( ms.stmts[ms.stmts.Count - 1] ),
				IfStmt ifs
					=> ifs.ifThens.TrueForAll( ct => HasUnconditionalReturn( ct.then ) )
					&& ifs.els != null
					&& HasUnconditionalReturn( ifs.els ),
				SwitchStmt sw
					=> sw.cases.TrueForAll( cb => HasUnconditionalReturn( cb.then ) )
					&& sw.els != null
					&& HasUnconditionalReturn( sw.els ),
				TryCatchStmt tcs
					=> HasUnconditionalReturn( tcs.tryBody )
					&& tcs.catches.TrueForAll( cc => HasUnconditionalReturn( cc.body ) ),
				LoopStmt ls => HasUnconditionalReturn( ls.body ),
				_ => false,
			};
		}
	}
}
