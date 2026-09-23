using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Transform
{
	/// <summary>
	/// Injects hidden <see cref="TplParamDecl"/> symbols into the scopes that own
	/// template parameters so that uses of those parameters inside the template body
	/// are accepted by name resolution and generated as the raw parameter name.
	/// </summary>
	public sealed class TemplateParamTransformer : Transformer
	{
		protected override bool RunInPrototypeFiles => true;

		protected override void PreTransform( TransformContext ctx )
		{
			TransformDecl( ctx.Module.Module, ctx.Module.Context );
		}

		private static void TransformDecl( Decl decl, ModuleContext context )
		{
			if( decl is ITemplatedDecl templated
			 && templated.TplParams.Count >= 1
			 && templated.TemplateParamScope != null ) {
				InjectParameters( templated.TemplateParamScope, templated.TplParams, context );
			}

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					TransformDecl( child, context );
			}
		}

		private static void InjectParameters(
			Scope scope,
			IReadOnlyList<TplParam> parameters,
			ModuleContext context )
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
