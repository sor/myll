using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
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
	public sealed class AutoReturnTransformer : ITransformer
	{
		private static readonly ConfiguredAliasShadowingTransformer conflictChecker = new();

		public void Transform(
			IReadOnlyList<CompiledModuleResult> modules,
			List<Diagnostic> diagnostics )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				if( context.IsPrototypeFile )
					continue;

				TransformDecl( module, module, context, diagnostics );
			}
		}

		public void Transform( IReadOnlyList<CompiledModuleResult> modules )
			=> Transform( modules, new List<Diagnostic>() );

		private static void TransformDecl(
			Decl decl,
			GlobalNamespace module,
			CompilationContext context,
			List<Diagnostic> diagnostics )
		{
			switch( decl ) {
				case Func func:
					TryTransformFunc( func, module, context, diagnostics );
					break;

				case Hierarchical h:
					foreach( Decl child in h.children )
						TransformDecl( child, module, context, diagnostics );
					break;
			}
		}

		private static void TryTransformFunc(
			Func func,
			GlobalNamespace module,
			CompilationContext context,
			List<Diagnostic> diagnostics )
		{
			if( func.body == null )
				return;

			if( func.retType is TypespecBasic { kind: TypespecBasic.Kind.ExplicitAuto or TypespecBasic.Kind.ImplicitAuto } ) {
				if( UsesAutoReturnName( func.body, Dialect.AutoReturnName ) ) {
					diagnostics.Add( new Diagnostic(
						func.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Auto-return variable '{0}' cannot be used when the return type is inferred; declare a concrete return type or use explicit return statements.",
							Dialect.AutoReturnName ) ) );
				}

				return;
			}

			string alias = Dialect.AutoReturnName;
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

		private static bool UsesAutoReturnName( Stmt stmt, string alias )
			=> ContainsAutoReturnName( stmt, alias );

		private static bool ContainsAutoReturnName( Stmt stmt, string alias )
		{
			return stmt switch {
				MultiStmt ms => ms.stmts.Any( s => ContainsAutoReturnName( s, alias ) ),
				IfStmt ifs
					=> ifs.ifThens.Any( ct => ContainsAutoReturnName( ct.cond, alias )
					                       || ContainsAutoReturnName( ct.then, alias ) )
					|| ( ifs.els != null && ContainsAutoReturnName( ifs.els, alias ) ),
				SwitchStmt sw
					=> ContainsAutoReturnName( sw.cases.SelectMany( cb => cb.compare ), alias )
					|| sw.cases.Any( cb => ContainsAutoReturnName( cb.then, alias ) )
					|| ( sw.els != null && ContainsAutoReturnName( sw.els, alias ) ),
				ForStmt fs
					=> ( fs.init != null && ContainsAutoReturnName( fs.init, alias ) )
					|| ContainsAutoReturnName( fs.cond, alias )
					|| ContainsAutoReturnName( fs.iter, alias )
					|| ( fs.body != null && ContainsAutoReturnName( fs.body, alias ) )
					|| ( fs.els != null && ContainsAutoReturnName( fs.els, alias ) ),
				WhileStmt ws
					=> ContainsAutoReturnName( ws.cond, alias )
					|| ContainsAutoReturnName( ws.body, alias )
					|| ( ws.els != null && ContainsAutoReturnName( ws.els, alias ) ),
				DoWhileStmt dws
					=> ContainsAutoReturnName( dws.body, alias )
					|| ContainsAutoReturnName( dws.cond, alias ),
				LoopStmt ls => ContainsAutoReturnName( ls.body, alias ),
				TimesStmt ts
					=> ContainsAutoReturnName( ts.count, alias )
					|| ContainsAutoReturnName( ts.body, alias ),
				TryCatchStmt tcs
					=> ContainsAutoReturnName( tcs.tryBody, alias )
					|| tcs.catches.Any( cc => ContainsAutoReturnName( cc.body, alias ) ),
				VarStmt vs => ContainsAutoReturnName( vs.init, alias ),
				ReturnStmt rs => ContainsAutoReturnName( rs.expr, alias ),
				ThrowStmt ths => ContainsAutoReturnName( ths.expr, alias ),
				ExprStmt es => ContainsAutoReturnName( es.expr, alias ),
				AggrAssign aa
					=> ContainsAutoReturnName( aa.leftExpr, alias )
					|| ContainsAutoReturnName( aa.rightExpr, alias ),
				MultiAssign ma => ma.exprs.Any( e => ContainsAutoReturnName( e, alias ) ),
				_ => false,
			};
		}

		private static bool ContainsAutoReturnName( IEnumerable<Expr?> exprs, string alias )
			=> exprs.Any( e => ContainsAutoReturnName( e, alias ) );

		private static bool ContainsAutoReturnName( Expr? expr, string alias )
		{
			if( expr == null )
				return false;

			return expr switch {
				IdExpr id
					=> id.idTplArgs.id == alias && id.idTplArgs.tplArgs.Count == 0,
				CastExpr ce => ContainsAutoReturnName( ce.expr, alias ),
				FuncCallExpr fce
					=> ContainsAutoReturnName( fce.expr, alias )
					|| fce.funcCall.args.Any( a => ContainsAutoReturnName( a.expr, alias ) ),
				UnOp uo => ContainsAutoReturnName( uo.expr, alias ),
				BinOp bo
					=> ContainsAutoReturnName( bo.left, alias )
					|| ContainsAutoReturnName( bo.right, alias ),
				TernOp to
					=> ContainsAutoReturnName( to.left, alias )
					|| ContainsAutoReturnName( to.mid, alias )
					|| ContainsAutoReturnName( to.right, alias ),
				NewExpr ne
					=> ne.funcCall.args.Any( a => ContainsAutoReturnName( a.expr, alias ) ),
				InitListExpr ile
					=> ile.args.Any( a => ContainsAutoReturnName( a.expr, alias ) ),
				Lambda => false,
				_ => false,
			};
		}

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
