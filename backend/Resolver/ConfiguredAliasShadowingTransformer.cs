using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Warns when a configured alias name is shadowed by a class/struct member,
	/// method parameter, local variable, or catch parameter.
	///
	/// The transform walks the resolved scope tree via
	/// <see cref="CompilationContext.LocalDecls"/>, so locals inside loops are
	/// correctly checked.
	/// </summary>
	public sealed class ConfiguredAliasShadowingTransformer : ITransformer
	{
		private readonly List<Diagnostic> diagnostics = new();

		public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

		public void Transform( IReadOnlyList<CompiledModuleResult> modules )
		{
			diagnostics.Clear();
			Transform( modules, diagnostics );
		}

		public void Transform(
			IReadOnlyList<CompiledModuleResult> modules,
			List<Diagnostic> diagnostics )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				if( context.IsPrototypeFile )
					continue;

				VisitHierarchical( module, context, diagnostics );
			}
		}

		/// <summary>
		/// Returns true when the function has a parameter, local variable, or catch
		/// parameter whose name matches the configured auto-return variable name.
		/// </summary>
		public bool HasAutoReturnConflict( Func func, CompilationContext context )
		{
			string alias = Dialect.AutoReturnName;
			if( String.IsNullOrEmpty( alias ) )
				return false;

			foreach( Param p in func.paras ) {
				if( p.name == alias )
					return true;
			}

			if( func.funcScope == null )
				return false;

			foreach( (Decl local, Scope scope) in context.LocalDecls ) {
				if( local.name != alias )
					continue;
				if( local is TplParamDecl )
					continue;
				if( !IsScopeInsideFunction( scope, func.funcScope ) )
					continue;

				return true;
			}

			return false;
		}

		private static void VisitHierarchical(
			Hierarchical hierarchical,
			CompilationContext context,
			List<Diagnostic> diagnostics )
		{
			foreach( Decl child in hierarchical.children ) {
				if( child is Structural structural )
					VisitStructural( structural, context, diagnostics );

				if( child is Hierarchical nested )
					VisitHierarchical( nested, context, diagnostics );
			}
		}

		private static void VisitStructural(
			Structural structural,
			CompilationContext context,
			List<Diagnostic> diagnostics )
		{
			CheckAlias( structural, Dialect.BaseClassAliasName, "base-class alias",
				structural.basetypes.Count >= 1, context, diagnostics );
			CheckAlias( structural, Dialect.OwnClassAliasName, "own-class alias",
				true, context, diagnostics );
		}

		private static void CheckAlias(
			Structural structural,
			string alias,
			string aliasKind,
			bool applies,
			CompilationContext context,
			List<Diagnostic> diagnostics )
		{
			if( String.IsNullOrEmpty( alias ) || !applies )
				return;

			foreach( Decl child in structural.children ) {
				if( child.name == alias ) {
					diagnostics.Add( new Diagnostic(
						child.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Name '{0}' matches the configured {1} and shadows it in '{2}'.",
							alias,
							aliasKind,
							structural.name ) ) );
				}

				if( child is Func func ) {
					CheckFunctionNameShadows( func, structural, alias, aliasKind, context, diagnostics );
				}
			}
		}

		private static void CheckFunctionNameShadows(
			Func func,
			Structural structural,
			string alias,
			string aliasKind,
			CompilationContext context,
			List<Diagnostic> diagnostics )
		{
			foreach( Param p in func.paras ) {
				if( p.name == alias ) {
					diagnostics.Add( new Diagnostic(
						func.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Parameter '{0}' in '{1}.{2}' shadows the configured {3}.",
							alias,
							structural.name,
							func.name,
							aliasKind ) ) );
				}
			}

			if( func.funcScope == null )
				return;

			ISet<string> autoReturnNames = GetAutoReturnNames( func );

			foreach( (Decl local, Scope scope) in context.LocalDecls ) {
				if( local.name != alias )
					continue;
				if( local is TplParamDecl )
					continue;
				if( autoReturnNames.Contains( local.name ) )
					continue;
				if( IsParameterOf( func, local ) )
					continue;
				if( !IsScopeInsideFunction( scope, func.funcScope ) )
					continue;

				// The source position of a compiler-generated VarDecl may be empty;
				// fall back to the function position so the warning is clickable.
				SrcPos srcPos = local.srcPos?.file != null
					? local.srcPos
					: func.srcPos;

				diagnostics.Add( new Diagnostic(
					srcPos,
					DiagnosticKind.Warning,
					String.Format(
						"Local variable or catch parameter '{0}' in '{1}.{2}' shadows the configured {3}.",
						alias,
						structural.name,
						func.name,
						aliasKind ) ) );
			}
		}

		private static bool IsParameterOf( Func func, Decl local )
		{
			if( local is not VarDecl varDecl )
				return false;

			return func.paras.Any( p => p.name == varDecl.name );
		}

		private static ISet<string> GetAutoReturnNames( Func func )
		{
			HashSet<string> result = new();

			Stmt? body = func.body;
			if( body is MultiStmt ms && ms.stmts.Count > 0 ) {
				if( ms.stmts[0] is VarStmt vs && vs.IsAutoReturn && !String.IsNullOrEmpty( vs.name ) )
					result.Add( vs.name );
			}

			return result;
		}

		private static bool IsScopeInsideFunction( Scope scope, Scope functionScope )
		{
			for( Scope? cur = scope; cur != null; cur = cur.parent ) {
				if( cur == functionScope )
					return true;

				// If we hit another function/constructor scope before the target,
				// the local belongs to a nested lambda/closure, not this function.
				Decl? owner = ((ScopeLeaf)cur).decl;
				if( owner is Func or Structor )
					return false;
			}

			return false;
		}
	}
}
