using System;
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
			Injector walker = new( ctx.Module.Context );
			walker.WalkModule( ctx.Module.Module );
		}

	private sealed class Injector : DeclWalker
	{
		private readonly ModuleContext context;

		public Injector( ModuleContext context ) => this.context = context;

		protected override void TransformDecl( Decl decl )
		{
			if( decl is ITemplatedDecl templated
			 && templated.TplParams.Count >= 1
			 && templated.TemplateParamScope != null ) {
				InjectParameters( templated.TemplateParamScope, templated.TplParams, context );

				if( decl is VarDecl varDecl )
					LinkVariableTemplateParamTypes( varDecl );
			}

			base.TransformDecl( decl );
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
					kind   = parameter.kind,
					type   = parameter.type,
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

		/// <summary>
		/// Pre-links simple template-parameter references inside a variable template's
		/// declared type to the matching injected <see cref="TplParamDecl"/>. This lets
		/// <see cref="TemplateInference.SubstituteTemplateParams"/> rely on
		/// <c>resolvedDecl is TplParamDecl</c> instead of a plain-name fallback.
		/// </summary>
		private static void LinkVariableTemplateParamTypes( VarDecl varDecl )
		{
			if( varDecl.varScope == null || varDecl.TplParams.Count == 0 )
				return;

			Dictionary<string, TplParamDecl> paramMap = varDecl.varScope.children
				.Where( kvp => kvp.Value.Any( leaf => leaf.decl is TplParamDecl ) )
				.ToDictionary(
					kvp => kvp.Key,
					kvp => (TplParamDecl)kvp.Value.Single( leaf => leaf.decl is TplParamDecl ).decl!,
					StringComparer.Ordinal );

			LinkType( varDecl.type, paramMap );
		}

		private static void LinkType( Typespec? type, Dictionary<string, TplParamDecl> paramMap )
		{
			if( type == null )
				return;

			switch( type ) {
				case TypespecNested n
				  when n.idTpls.Count == 1
				    && ( n.idTpls[0].tplArgs == null || n.idTpls[0].tplArgs.Count == 0 )
				    && paramMap.TryGetValue( n.idTpls[0].id, out TplParamDecl? tp ): {
					n.resolvedDecl = tp!;
					break;
				}

				case TypespecNested n: {
					foreach( IdTplArgs segment in n.idTpls ) {
						if( segment.tplArgs == null )
							continue;
						foreach( TplArg arg in segment.tplArgs ) {
							if( arg.typespec != null )
								LinkType( arg.typespec, paramMap );
						}
					}

					break;
				}
			}
		}
	}
}
