using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Warns when a configured alias name is shadowed by a class/struct member,
	/// method parameter, local variable, or catch parameter.
	/// This runs independently of name resolution so the warnings are available even
	/// when the semantic resolver is not enabled.
	/// </summary>
	public sealed class ConfiguredAliasShadowingTransformer
	{
		private readonly List<Diagnostic> diagnostics = new();

		public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

		public void Transform( IReadOnlyList<CompiledModuleResult> modules )
		{
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
			CheckClassAlias( structural, Dialect.BaseClassAliasName, "base-class alias", structural.basetypes.Count >= 1 );

			CheckClassAlias( structural, Dialect.OwnClassAliasName, "own-class alias", true );
		}

		private void CheckClassAlias(
			Structural structural,
			string alias,
			string aliasKind,
			bool applies )
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
					CheckFunctionNameShadows( func, structural, alias, aliasKind );
				}
			}
		}

		private void CheckFunctionNameShadows(
			Func func,
			Structural structural,
			string alias,
			string aliasKind )
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

			if( func.body != null )
				CheckStmtForNameShadows( func.body, structural, func, alias, aliasKind );
		}

		private void CheckStmtForNameShadows(
			Stmt stmt,
			Structural? structural,
			Func func,
			string alias,
			string aliasKind )
		{
			foreach( VarStmt varStmt in stmt.EnumerateDF.OfType<VarStmt>() ) {
				if( varStmt.IsAutoReturn )
					continue;

				if( varStmt.name == alias ) {
					diagnostics.Add( new Diagnostic(
						varStmt.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Local variable '{0}' in '{1}.{2}' shadows the configured {3}.",
							alias,
							structural?.name ?? func.name,
							func.name,
							aliasKind ) ) );
				}
			}

			foreach( TryCatchStmt tryCatch in stmt.EnumerateDF.OfType<TryCatchStmt>() ) {
				foreach( CatchClause cc in tryCatch.catches ) {
					if( cc.param?.name == alias ) {
						diagnostics.Add( new Diagnostic(
							tryCatch.srcPos,
							DiagnosticKind.Warning,
							String.Format(
								"Catch parameter '{0}' in '{1}.{2}' shadows the configured {3}.",
								alias,
								structural?.name ?? func.name,
								func.name,
								aliasKind ) ) );
					}
				}
			}
		}

		public bool HasAutoReturnConflict( Func func )
		{
			if( String.IsNullOrEmpty( Dialect.AutoReturnName ) )
				return false;

			string alias = Dialect.AutoReturnName;

			foreach( Param p in func.paras ) {
				if( p.name == alias )
					return true;
			}

			if( func.body != null ) {
				foreach( VarStmt varStmt in func.body.EnumerateDF.OfType<VarStmt>() ) {
					if( !varStmt.IsAutoReturn && varStmt.name == alias )
						return true;
				}

				foreach( TryCatchStmt tryCatch in func.body.EnumerateDF.OfType<TryCatchStmt>() ) {
					foreach( CatchClause cc in tryCatch.catches ) {
						if( cc.param?.name == alias )
							return true;
					}
				}
			}

			return false;
		}

		public void CheckAutoReturnConflicts( IReadOnlyList<CompiledModuleResult> modules )
		{
			if( String.IsNullOrEmpty( Dialect.AutoReturnName ) )
				return;

			string alias = Dialect.AutoReturnName;

			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				if( context.IsPrototypeFile )
					continue;

				CheckAutoReturnConflictsInHierarchical( module, alias );
			}
		}

		private void CheckAutoReturnConflictsInHierarchical( Hierarchical hierarchical, string alias )
		{
			foreach( Decl child in hierarchical.children ) {
				if( child is Func func
				 && func.body != null
				 && HasParameterOrLocalNamed( func, alias ) ) {
					diagnostics.Add( new Diagnostic(
						func.srcPos,
						DiagnosticKind.Warning,
						String.Format(
							"Function '{0}' declares '{1}'; auto-return generation is disabled.",
							func.name,
							alias ) ) );
				}

				if( child is Hierarchical nested )
					CheckAutoReturnConflictsInHierarchical( nested, alias );
			}
		}

		private bool HasParameterOrLocalNamed( Func func, string alias )
		{
			foreach( Param p in func.paras )
				if( p.name == alias )
					return true;

			if( func.body == null )
				return false;

			foreach( VarStmt varStmt in func.body.EnumerateDF.OfType<VarStmt>() )
				if( !varStmt.IsAutoReturn && varStmt.name == alias )
					return true;

			foreach( TryCatchStmt tryCatch in func.body.EnumerateDF.OfType<TryCatchStmt>() ) {
				foreach( CatchClause cc in tryCatch.catches )
					if( cc.param?.name == alias )
						return true;
			}

			return false;
		}
	}
}
