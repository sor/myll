using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Warns when the configured base-class alias name (default "base") is shadowed
	/// by a class/struct member, method parameter, local variable, or catch parameter.
	/// This runs independently of name resolution so the warnings are available even
	/// when the semantic resolver is not enabled.
	/// </summary>
	public sealed class BaseAliasShadowingTransformer
	{
		private readonly List<Diagnostic> diagnostics = new();

		public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

		public void Transform(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
		{
			if( String.IsNullOrEmpty( Dialect.BaseClassAliasName ) )
				return;

			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				if( context.IsPrototypeFile )
					continue;

				VisitHierarchical( module );
			}
		}

		private void VisitHierarchical( Hierarchical hierarchical )
		{
			foreach( Decl child in hierarchical.children ) {
				if( child is Structural structural )
					VisitStructural( structural );

				if( child is Hierarchical nested )
					VisitHierarchical( nested );
			}
		}

		private void VisitStructural( Structural structural )
		{
			if( structural.basetypes.Count == 0 )
				return;

			string alias = Dialect.BaseClassAliasName;

			foreach( Decl child in structural.children ) {
				if( child.name == alias ) {
					diagnostics.Add( new Diagnostic(
						child.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Name '{0}' matches the configured base-class alias and shadows it in '{1}'.",
							alias,
							structural.name ) ) );
				}

				if( child is Func func )
					VisitFunc( func, structural );
			}
		}

		private void VisitFunc( Func func, Structural structural )
		{
			string alias = Dialect.BaseClassAliasName;

			foreach( Param p in func.paras ) {
				if( p.name == alias ) {
					diagnostics.Add( new Diagnostic(
						func.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Parameter '{0}' in '{1}.{2}' shadows the configured base-class alias.",
							alias,
							structural.name,
							func.name ) ) );
				}
			}

			if( func.body != null )
				VisitStmt( func.body, structural, func );
		}

		private void VisitStmt( Stmt stmt, Structural structural, Func func )
		{
			string alias = Dialect.BaseClassAliasName;

			foreach( VarStmt varStmt in stmt.EnumerateDF.OfType<VarStmt>() ) {
				if( varStmt.name == alias ) {
					diagnostics.Add( new Diagnostic(
						varStmt.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Local variable '{0}' in '{1}.{2}' shadows the configured base-class alias.",
							alias,
							structural.name,
							func.name ) ) );
				}
			}

			if( stmt is TryCatchStmt tryCatch ) {
				foreach( CatchClause cc in tryCatch.catches ) {
					if( cc.param?.name == alias ) {
						diagnostics.Add( new Diagnostic(
							tryCatch.srcPos,
							DiagnosticKind.Warning,
							String.Format(
								"Catch parameter '{0}' in '{1}.{2}' shadows the configured base-class alias.",
								alias,
								structural.name,
								func.name ) ) );
					}
				}
			}
		}
	}
}
