using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Injects hidden <see cref="TplParamDecl"/> symbols into the scopes that own
	/// template parameters so that uses of those parameters inside the template body
	/// are accepted by name resolution and generated as the raw parameter name.
	/// </summary>
	public sealed class TemplateParamTransformer : ITransformer
	{
		public void Transform(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules,
			List<Diagnostic> diagnostics )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				if( context.IsPrototypeFile )
					continue;

				TransformDecl( module, context );
			}
		}

		public void Transform(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
			=> Transform( modules, new List<Diagnostic>() );

		private static void TransformDecl( Decl decl, CompilationContext context )
		{
			switch( decl ) {
				case Func func when func.TplParams.Count >= 1 && func.funcScope != null:
					InjectParameters( func.funcScope, func.TplParams, context );
					break;

				case Structural structural when structural.TplParams.Count >= 1:
					InjectParameters( structural.scope, structural.TplParams, context );
					break;
			}

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					TransformDecl( child, context );
			}
		}

		private static void InjectParameters(
			Scope scope,
			IReadOnlyList<TplParam> parameters,
			CompilationContext context )
		{
			foreach( TplParam parameter in parameters ) {
				if( scope.children.ContainsKey( parameter.name ) )
					continue;

				var placeholder = new TplParamDecl {
					name   = parameter.name,
					srcPos = scope.decl?.srcPos ?? new SrcPos(),
				};

				ScopeLeaf leaf = new() {
					parent = scope,
					decl   = placeholder,
				};
				placeholder.scope = scope;

				scope.children.Add( parameter.name, new List<ScopeLeaf> { leaf } );
				context.LocalDecls.Add( (placeholder, scope) );
			}
		}
	}
}
