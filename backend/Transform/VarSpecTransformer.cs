using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
{
	/// <summary>
	/// Validates variable template specializations and links them to their primary:
	/// the primary must live in the same module and scope and precede the
	/// specialization, or in a module directly imported by the specialization's
	/// module (then include order puts the primary first). Bindings must name
	/// primary parameters in declaration order; and the specialization's declared
	/// type must match the primary's after substituting the bound values. For
	/// partial specializations the unbound parameters become the specialization's
	/// own template parameters and are injected into its scope so initializers
	/// can reference them.
	/// </summary>
	public sealed class VarSpecTransformer : Transformer
	{
		private readonly IReadOnlyList<CompiledModuleResult> allModules;

		private ModuleContext   context  = null!;
		private GlobalNamespace moduleAst = null!;

		public VarSpecTransformer( IReadOnlyList<CompiledModuleResult> allModules )
		{
			this.allModules = allModules;
		}

		protected override void PreTransform( TransformContext ctx )
		{
			GlobalNamespace module = ctx.Module.Module;
			if( ctx.Module.Context.IsPrototypeFile )
				return;

			context   = ctx.Module.Context;
			moduleAst = module;
			ValidateScope( module );
		}

		private void ValidateScope( Myll.Core.Hierarchical hier )
		{
			List<Decl>    children = hier.children.ToList();
			List<VarDecl> vars     = children.OfType<VarDecl>().ToList();

			ValidateSpecsInScope( vars, NamespaceFqnOf( hier ) );

			foreach( Decl child in children.OfType<Myll.Core.Hierarchical>() )
				ValidateScope( (Myll.Core.Hierarchical)child );
		}

		/// <summary>
		/// Namespace path from the module root down to <paramref name="hier"/>'s
		/// own scope, or null inside a class/struct: member specializations stay
		/// same-module for now.
		/// </summary>
		private static string? NamespaceFqnOf( Myll.Core.Hierarchical hier )
			=> hier is Myll.Core.Structural ? null
			: hier is Myll.Core.Namespace ns ? ns.FullyQualifiedName
			: "";

		private void ValidateSpecsInScope( List<VarDecl> vars, string? nsFqn )
		{
			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<VarDecl, List<string>> seenSignatures = new();

			foreach( VarDecl spec in vars ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				List<VarDecl> primaries = vars
					.Where( v => v.name == spec.name && v.SpecInfo == null && v.TplParams.Count > 0 )
					.ToList();

				VarDecl? primary = primaries.Count > 0
					? primaries[0]
					: ( nsFqn != null ? FindImportedPrimary( spec, nsFqn ) : null );

				if( primary == null ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "No matching variable template '{0}' in this module or its imports; a specialization must live in the primary's module or import it",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				bool isImportedPrimary = primaries.Count == 0;

				// Within one module the specialization must follow the primary by
				// source position, not processing order, so multi-file modules stay
				// order-independent. A cross-module primary needs no check: the include
				// of the primary's module precedes every declaration of this one.
				if( !isImportedPrimary && !IsLaterPos( spec, primary ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must follow its primary template",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				// Bindings must name primary parameters in declaration order. The "name
				// everything" form lists unbound parameters bare (q<T, U=int>); then every
				// primary parameter must be listed, each at its own position.
				Dictionary<string, int> paramPos = new( StringComparer.Ordinal );
				for( int i = 0; i < primary.TplParams.Count; i++ )
					paramPos[ primary.TplParams[i].name ] = i;

				bool isFullForm = spec.TplSpec.Any( b => b.type == null );
				bool bindingsValid = true;

				if( isFullForm ) {
					if( spec.TplSpec.Count != primary.TplParams.Count ) {
						Diagnostics.Add( new Diagnostic(
							spec.srcPos,
							DiagnosticKind.Error,
							String.Format(
								"Specialization '{0}' names {1} of {2} parameters; either name all of them or only the bound ones",
								spec.name,
								spec.TplSpec.Count,
								primary.TplParams.Count ) ) );
						bindingsValid = false;
					}
					else {
						for( int i = 0; i < spec.TplSpec.Count; i++ ) {
							string expected = primary.TplParams[i].name;
							string actual   = spec.TplSpec[i].name;
							if( actual != expected ) {
								Diagnostics.Add( new Diagnostic(
									spec.srcPos,
									DiagnosticKind.Error,
									String.Format(
										"Specialization '{0}' names '{1}' at parameter '{2}''s position; unbound names must match the primary's parameters",
										spec.name, actual, expected ) ) );
								bindingsValid = false;
								break;
							}
						}
					}
				}
				else {
					int lastPos = -1;
					foreach( TplBinding binding in spec.TplSpec ) {
						if( !paramPos.TryGetValue( binding.name, out int pos ) ) {
							Diagnostics.Add( new Diagnostic(
								spec.srcPos,
								DiagnosticKind.Error,
								String.Format( "Unknown template parameter '{0}' in specialization of '{1}'",
									binding.name, spec.name ) ) );
							bindingsValid = false;
							break;
						}
						if( pos <= lastPos ) {
							Diagnostics.Add( new Diagnostic(
								spec.srcPos,
								DiagnosticKind.Error,
								String.Format( "Bindings of '{0}' must follow the primary's parameter order; '{1}' comes after a later parameter",
									spec.name, binding.name ) ) );
							bindingsValid = false;
							break;
						}
						lastPos = pos;
					}
				}
				if( !bindingsValid ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Substitute the bound values into the declared type, then compare
				// with the primary's substituted type. The original symbolic type was
				// registered as unresolved at visit time; the substituted type needs
				// no resolution of its own.
				Dictionary<string, Typespec> boundValues = new( StringComparer.Ordinal );
				foreach( TplBinding binding in spec.TplSpec ) {
					if( binding.type != null )
						boundValues[ binding.name ] = binding.type;
				}

				Typespec originalType = spec.type;
				Typespec specType = SubstituteBindings( spec.type, boundValues );
				Typespec primaryType = SubstituteBindings( primary.type, boundValues );

				context.UnresolvedTypes.RemoveAll( ut => ReferenceEquals( ut.Node, originalType ) );

				if( specType.Gen() != primaryType.Gen() ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' declares type '{1}' but the primary's substituted type is '{2}'",
							spec.name, specType.Gen(), primaryType.Gen() ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				// Duplicate specialization signature check.
				string signature = String.Join( ",",
					spec.TplSpec
						.Where( b => b.type != null )
						.Select( b => bindingSignature( b ) ) );
				if( !seenSignatures.TryGetValue( primary, out List<string>? seen ) ) {
					seen = new();
					seenSignatures[ primary ] = seen;
				}
				if( seen.Contains( signature ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Duplicate specialization of '{0}' for {1}",
							spec.name, signature ) ) );
					MarkInvalidSpec( spec );
					continue;
				}
				seen.Add( signature );

				// Fill the spec info: every primary parameter in order, bound or symbolic.
				int paramCount = primary.TplParams.Count;
				bool[] bound = new bool[ paramCount ];
				List<TplArg> args = new( paramCount );
				List<TplParam> unboundParams = new();

				for( int i = 0; i < paramCount; i++ ) {
					TplParam param = primary.TplParams[ i ];
					TplBinding? binding = spec.TplSpec
						.FirstOrDefault( b => b.name == param.name );

					if( binding != null && binding.type != null ) {
						bound[ i ] = true;
						args.Add( new TplArg { typespec = binding.type } );
					}
					else {
						args.Add( new TplArg {
							typespec = new TypespecNested {
								idTpls = new List<IdTplArgs> { new() { id = param.name } },
							},
						} );
						unboundParams.Add( param );
					}
				}

				spec.SpecInfo = new VarDecl.VarSpecInfo {
					Primary = primary,
					Args    = args,
					Bound   = bound,
				};
				spec.type = specType;
				spec.TplParams = unboundParams;

				// Unbound parameters become the specialization's own template
				// parameters, injected into its scope so initializers can use them.
				InjectUnboundParams( spec, unboundParams );
			}
		}

		private void InjectUnboundParams( VarDecl spec, List<TplParam> unboundParams )
		{
			if( unboundParams.Count == 0 || spec.varScope == null )
				return;

			foreach( TplParam param in unboundParams ) {
				TplParamDecl placeholder = new() {
					name   = param.name,
					kind   = param.kind,
					type   = param.type,
					srcPos = spec.varScope.decl?.srcPos ?? spec.srcPos,
				};

				ScopeLeaf leaf = new() {
					parent = spec.varScope,
					decl   = placeholder,
				};
				placeholder.scope = spec.varScope;

				if( !spec.varScope.children.TryGetValue( param.name, out List<ScopeLeaf>? list ) ) {
					list = new List<ScopeLeaf>( 1 );
					spec.varScope.children.Add( param.name, list );
				}
				list.Add( leaf );
				context.LocalDecls.Add( (placeholder, spec.varScope) );
			}
		}

		/// <summary>
		/// Finds the primary template for a cross-module specialization: same
		/// namespace path and same name, in a module the specialization's module
		/// imports directly, or in an implicitly imported std module. The generated
		/// include of that module precedes this module's declarations, so include
		/// order puts the primary before the specialization.
		/// </summary>
		private VarDecl? FindImportedPrimary( VarDecl spec, string nsFqn )
		{
			foreach( string importedName in ImportedModuleNames( moduleAst ) ) {
				foreach( CompiledModuleResult m in allModules ) {
					if( m.Module.module != importedName )
						continue;

					Myll.Core.Hierarchical? scope = FindNamespaceScope( m.Module, nsFqn );
					if( scope == null || scope.scope == null
					 || !scope.scope.children.TryGetValue( spec.name, out List<ScopeLeaf>? leaves ) )
						continue;

					foreach( ScopeLeaf leaf in leaves ) {
						if( leaf.decl is VarDecl v
						 && v.SpecInfo == null
						 && v.TplParams.Count > 0
						 && !v.IsHidden )
							return v;
					}
				}
			}

			return null;
		}

		private static Myll.Core.Hierarchical? FindNamespaceScope( GlobalNamespace root, string fqn )
		{
			Myll.Core.Hierarchical cur = root;
			if( fqn.Length != 0 ) {
				foreach( string segment in fqn.Split( "::" ) ) {
					Myll.Core.Hierarchical? next = null;
					foreach( Decl child in cur.children ) {
						if( child is Myll.Core.Namespace ns && ns.name == segment ) {
							next = ns;
							break;
						}
					}
					if( next == null )
						return null;
					cur = next;
				}
			}
			return cur;
		}

		// Mirrors Resolver.ImportedModuleNames: direct imports plus the implicitly
		// imported std modules.
		private static IEnumerable<string> ImportedModuleNames( GlobalNamespace module )
		{
			foreach( NamedImportSpec imp in module.imports.OfType<NamedImportSpec>() )
				yield return imp.name;

			foreach( string imp in StdBuiltins.ImplicitStdImports )
				if( imp != module.module )
					yield return imp;
		}

		// Compare by source position (file name, then line), like
		// Resolver.IsLater: the harness and the driver hand files in sorted
		// order, but the check must not depend on that.
		private static bool IsLaterPos( Decl a, Decl b )
		{
			int fileCompare = StringComparer.Ordinal.Compare(
				Path.GetFileName( a.srcPos.file ),
				Path.GetFileName( b.srcPos.file ) );
			if( fileCompare != 0 )
				return fileCompare > 0;
			return a.srcPos.from.line > b.srcPos.from.line;
		}

		private static string bindingSignature( TplBinding binding )
			=> binding.name + "=" + binding.type.Gen();

		/// <summary>
		/// Marks a specialization whose validation failed so later passes treat it
		/// as consumed (no duplicate-name error, no overload candidate).
		/// </summary>
		private static void MarkInvalidSpec( VarDecl spec )
			=> spec.SpecInfo = new VarDecl.VarSpecInfo { Primary = spec };

		/// <summary>
		/// Syntactic substitution of bound parameter names with their values. Unlike
		/// <see cref="TemplateInference.SubstituteTemplateParams"/> it does not need
		/// resolved declarations, so it can run before name resolution.
		/// </summary>
		private static Typespec SubstituteBindings( Typespec type, Dictionary<string, Typespec> substitution )
		{
			if( substitution.Count == 0 )
				return type;

			if( type is TypespecNested nested
			 && nested.idTpls.Count == 1
			 && ( nested.idTpls[0].tplArgs == null || nested.idTpls[0].tplArgs.Count == 0 )
			 && substitution.TryGetValue( nested.idTpls[0].id, out Typespec? replacement ) ) {
				Typespec ret = replacement.Clone( true );
				ret.qual |= type.qual;
				ret.ptrs = type.ptrs == null || type.ptrs.Count == 0
					? ret.ptrs
					: ( ret.ptrs ?? new List<Pointer>() )
					.Concat( type.ptrs.Select( p => new Pointer { kind = p.kind, qual = p.qual, expr = p.expr } ) )
					.ToList();
				return ret;
			}

			if( type is TypespecBasic )
				return type.Clone( true );

			if( type is TypespecNested n ) {
				TypespecNested ret = (TypespecNested) type.Clone( true );
				ret.idTpls = n.idTpls
					.Select( it => new IdTplArgs {
						id = it.id,
						tplArgs = it.tplArgs == null
							? new List<TplArg>()
							: it.tplArgs
								.Select( a => new TplArg {
									typespec = a.typespec == null
										? null
										: SubstituteBindings( a.typespec, substitution ),
								} )
								.ToList(),
					} )
					.ToList();
				return ret;
			}

			if( type is TypespecFunc f ) {
				TypespecFunc ret = (TypespecFunc) type.Clone( true );
				if( f.retType != null )
					ret.retType = SubstituteBindings( f.retType, substitution );
				if( f.paras != null ) {
					ret.paras = f.paras
						.Select( p => new Param {
							name = p.name,
							type = SubstituteBindings( p.type, substitution ),
						} )
						.ToList();
				}
				return ret;
			}

			return type.Clone( true );
		}
	}
}
