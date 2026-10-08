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
	/// Class/struct/union specializations follow the same binding rules, live in
	/// the primary's module, and are linked the same way; their bodies are their
	/// own member sets (C++ semantics) with unbound parameters injected into the
	/// class scope.
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
			List<Decl> children = hier.children.ToList();
			string? nsFqn = NamespaceFqnOf( hier );

			ValidateSpecsInScope( children.OfType<VarDecl>().ToList(), nsFqn );
			ValidateStructSpecs( children.OfType<Myll.Core.Structural>().ToList(), nsFqn );
			ValidateFuncSpecs( children.OfType<Func>().ToList(), hier is Myll.Core.Structural, nsFqn );

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

		private static bool IsVarPrimary( Decl decl )
			=> decl is VarDecl { SpecInfo: null } v
			 && v.TplParams.Count > 0
			 && !v.IsHidden;

		private static bool IsStructPrimary( Decl decl )
			=> decl is Myll.Core.Structural { SpecInfo: null } s
			 && s.TplParams.Count > 0
			 && !s.IsHidden;

		private static bool IsFuncPrimary( Decl decl )
			=> decl is Func { SpecInfo: null } f
			 && f.TplParams.Count > 0
			 && !f.IsHidden;

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
					: ( nsFqn != null ? FindImportedPrimary( spec.name, nsFqn, IsVarPrimary ) as VarDecl : null );

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
				if( !ValidateBindingShape( spec.name, spec.TplSpec, primary.TplParams, spec.srcPos ) ) {
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
				List<TplArg> args = BuildSpecArgs( primary.TplParams, spec.TplSpec, out bool[] bound, out List<TplParam> unboundParams );

				spec.SpecInfo = new Decl.SpecData {
					Primary = primary,
					Args    = args,
					Bound   = bound,
				};
				spec.type = specType;
				spec.TplParams = unboundParams;

				// Unbound parameters become the specialization's own template
				// parameters, injected into its scope so initializers can use them.
				InjectParams( spec.varScope, unboundParams, spec.srcPos );
			}
		}

		/// <summary>
		/// Validates class/struct/union specializations in one scope and links them
		/// to their same-module primary: the primary must precede the
		/// specialization by source position and carry the same name; the body is
		/// the specialization's own member set (C++ semantics); the unbound
		/// parameters become the specialization's own template parameters and are
		/// injected into its class scope so members can reference them. Bound
		/// parameter names are not in scope: the body is written with the bound
		/// values spelled out, like C++.
		/// </summary>
		private void ValidateStructSpecs( List<Myll.Core.Structural> structs, string? nsFqn )
		{
			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<Myll.Core.Structural, List<string>> seenSignatures = new();

			foreach( Myll.Core.Structural spec in structs ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				List<Myll.Core.Structural> primaries = structs
					.Where( s => s.name == spec.name && s.SpecInfo == null && s.TplParams.Count > 0 )
					.ToList();

				Myll.Core.Structural? primary = primaries.Count > 0
					? primaries[0]
					: ( nsFqn != null ? FindImportedPrimary( spec.name, nsFqn, IsStructPrimary ) as Myll.Core.Structural : null );

				if( primary == null ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "No matching template '{0}' in this module or its imports; a class/struct/union specialization must live in the primary's module or import it",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				bool isImportedPrimary = primaries.Count == 0;

				// Within one module the specialization must follow the primary by
				// source position, not processing order, so multi-file modules stay
				// order-independent. A cross-module primary needs no check: the
				// include of the primary's module precedes every declaration of this one.
				if( !isImportedPrimary && !IsLaterPos( spec, primary ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must follow its primary template",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				if( spec.IsForwardDeclaration ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must be a full definition",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ValidateBindingShape( spec.name, spec.TplSpec, primary.TplParams, spec.srcPos ) ) {
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

				List<TplArg> args = BuildSpecArgs( primary.TplParams, spec.TplSpec, out bool[] bound, out List<TplParam> unboundParams );

				spec.SpecInfo = new Decl.SpecData {
					Primary = primary,
					Args    = args,
					Bound   = bound,
				};
				spec.TplParams = unboundParams;

				// Unbound parameters become the specialization's own template
				// parameters, injected into its class scope like
				// TemplateParamTransformer does for primaries.
				InjectParams( spec.scope, unboundParams, spec.srcPos );
			}
		}

		/// <summary>
		/// Validates function template specializations and links them to their
		/// primary: a specialization is a full one (C++ allows no partial function
		/// specializations), its parameters and return type must repeat the
		/// primary's after substituting the bound values, and bound parameter
		/// names stay usable inside the specialization through bound-parameter
		/// aliases (`T` keeps meaning the bound value), in the signature and the
		/// body alike.
		/// </summary>
		private void ValidateFuncSpecs( List<Func> funcs, bool insideStruct, string? nsFqn )
		{
			if( funcs.All( f => f.TplSpec.Count == 0 ) )
				return;

			if( insideStruct ) {
				foreach( Func spec in funcs.Where( f => f.TplSpec.Count > 0 ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of method '{0}' is not supported yet; move it to module or namespace scope",
							spec.name ) ) );
					MarkInvalidSpec( spec );
				}
				return;
			}

			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<Func, List<string>> seenSignatures = new();

			foreach( Func spec in funcs ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				if( spec.body == null ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must be a full definition",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				List<Func> primaries = funcs
					.Where( f => f.name == spec.name && f.SpecInfo == null && f.TplParams.Count > 0 )
					.ToList();

				Func? primary = primaries.Count > 0
					? primaries[0]
					: ( nsFqn != null ? FindImportedPrimary( spec.name, nsFqn, IsFuncPrimary ) as Func : null );

				if( primary == null ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "No matching function template '{0}' in this module or its imports; a specialization must live in the primary's module or import it",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				bool isImportedPrimary = primaries.Count == 0;

				// Within one module the specialization must follow the primary by
				// source position; a cross-module primary needs no check because
				// its include precedes this module's declarations.
				if( !isImportedPrimary && !IsLaterPos( spec, primary ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must follow its primary template",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				// Function templates allow only full specializations: every
				// parameter must be bound, so no bare names and a full count.
				if( spec.TplSpec.Any( b => b.type == null )
				 || spec.TplSpec.Count != primary.TplParams.Count ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format(
							"Function templates allow only full specializations: specialization of '{0}' must bind all {1} parameters",
							spec.name,
							primary.TplParams.Count ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ValidateBindingShape( spec.name, spec.TplSpec, primary.TplParams, spec.srcPos ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// The specialization repeats the primary's substituted signature:
				// parameter types and return type. Writing the bound parameter
				// name (T) or the bound value (int) is equivalent here because
				// both sides are substituted before the comparison.
				if( IsAutoType( spec.retType ) || IsAutoType( primary.retType ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must repeat the primary's return type; inferred returns cannot specialize",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				Dictionary<string, Typespec> boundValues = new( StringComparer.Ordinal );
				foreach( TplBinding binding in spec.TplSpec )
					boundValues[ binding.name ] = binding.type!;

				bool signatureMatches = spec.paras.Count == primary.paras.Count;
				if( signatureMatches ) {
					for( int i = 0; i < spec.paras.Count; i++ ) {
						if( SubstituteBindings( spec.paras[i].type, boundValues ).Gen()
						 != SubstituteBindings( primary.paras[i].type, boundValues ).Gen() ) {
							signatureMatches = false;
							break;
						}
					}
				}
				if( signatureMatches
				 && SubstituteBindings( spec.retType, boundValues ).Gen()
				 != SubstituteBindings( primary.retType, boundValues ).Gen() )
					signatureMatches = false;

				if( !signatureMatches ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must repeat the primary's substituted parameter and return types",
							spec.name ) ) );
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

				List<TplArg> args = BuildSpecArgs( primary.TplParams, spec.TplSpec, out bool[] bound, out List<TplParam> unboundParams );
				System.Diagnostics.Debug.Assert( unboundParams.Count == 0 );

				spec.SpecInfo = new Decl.SpecData {
					Primary = primary,
					Args    = args,
					Bound   = bound,
				};
				spec.TplParams = new(); // full: nothing unbound

				// Bound parameter names stay usable: an alias per bound parameter
				// injected into the specialization's scope, expanded at emission.
				InjectBoundAliases( spec, boundValues );
			}
		}

		private static bool IsAutoType( Typespec type )
			=> type is TypespecBasic { kind: TypespecBasic.Kind.ImplicitAuto or TypespecBasic.Kind.ExplicitAuto };

		// A bound parameter keeps its meaning inside the specialization through a
		// hidden alias: `T` resolves to the alias, the alias resolves to the bound
		// value, and emission expands it so no `T` reaches the generated C++.
		private void InjectBoundAliases( Func spec, Dictionary<string, Typespec> boundValues )
		{
			Scope? scope = spec.funcScope;
			if( scope == null )
				return;

			foreach( (string name, Typespec value) in boundValues ) {
				if( spec.paras.Any( p => p.name == name ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Parameter '{0}' of specialization '{1}' shadows the bound template parameter; rename it",
							name, spec.name ) ) );
					continue;
				}

				AliasDecl alias = new() {
					name          = name,
					type          = value,
					srcPos        = spec.srcPos,
					IsBoundAlias  = true,
				};

				ScopeLeaf leaf = new() {
					parent = scope,
					decl   = alias,
				};
				alias.scope = scope;

				if( !scope.children.TryGetValue( name, out List<ScopeLeaf>? list ) ) {
					list = new List<ScopeLeaf>( 1 );
					scope.children.Add( name, list );
				}
				list.Add( leaf );
				context.LocalDecls.Add( (alias, scope) );
			}
		}

		/// <summary>
		/// Validates the binding list shape against the primary's parameter list:
		/// the full form ("name everything") must name every parameter, each bare
		/// name at its own position; the subset form must name known parameters in
		/// the primary's declaration order.
		/// </summary>
		private bool ValidateBindingShape( string specName, List<TplBinding> tplSpec, IReadOnlyList<TplParam> primaryParams, SrcPos srcPos )
		{
			Dictionary<string, int> paramPos = new( StringComparer.Ordinal );
			for( int i = 0; i < primaryParams.Count; i++ )
				paramPos[ primaryParams[i].name ] = i;

			bool isFullForm = tplSpec.Any( b => b.type == null );

			if( isFullForm ) {
				if( tplSpec.Count != primaryParams.Count ) {
					Diagnostics.Add( new Diagnostic(
						srcPos,
						DiagnosticKind.Error,
						String.Format(
							"Specialization '{0}' names {1} of {2} parameters; either name all of them or only the bound ones",
							specName,
							tplSpec.Count,
							primaryParams.Count ) ) );
					return false;
				}

				for( int i = 0; i < tplSpec.Count; i++ ) {
					string expected = primaryParams[i].name;
					string actual   = tplSpec[i].name;
					if( actual != expected ) {
						Diagnostics.Add( new Diagnostic(
							srcPos,
							DiagnosticKind.Error,
							String.Format(
								"Specialization '{0}' names '{1}' at parameter '{2}''s position; unbound names must match the primary's parameters",
								specName, actual, expected ) ) );
						return false;
					}
				}

				return true;
			}

			int lastPos = -1;
			foreach( TplBinding binding in tplSpec ) {
				if( !paramPos.TryGetValue( binding.name, out int pos ) ) {
					Diagnostics.Add( new Diagnostic(
						srcPos,
						DiagnosticKind.Error,
						String.Format( "Unknown template parameter '{0}' in specialization of '{1}'",
							binding.name, specName ) ) );
					return false;
				}
				if( pos <= lastPos ) {
					Diagnostics.Add( new Diagnostic(
						srcPos,
						DiagnosticKind.Error,
						String.Format( "Bindings of '{0}' must follow the primary's parameter order; '{1}' comes after a later parameter",
							specName, binding.name ) ) );
					return false;
				}
				lastPos = pos;
			}

			return true;
		}

		// The specialization argument list: every primary parameter in
		// declaration order, bound values for bound positions and symbolic
		// placeholders for unbound ones; the unbound parameters become the
		// specialization's own template parameters.
		private static List<TplArg> BuildSpecArgs( IReadOnlyList<TplParam> primaryParams, List<TplBinding> tplSpec, out bool[] bound, out List<TplParam> unboundParams )
		{
			bound = new bool[ primaryParams.Count ];
			List<TplArg> args = new( primaryParams.Count );
			unboundParams = new();

			for( int i = 0; i < primaryParams.Count; i++ ) {
				TplParam param = primaryParams[ i ];
				TplBinding? binding = tplSpec
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

			return args;
		}

		// Unbound parameters become the specialization's own template parameters,
		// injected into its scope so members can use them.
		private void InjectParams( Scope? scope, List<TplParam> unboundParams, SrcPos fallbackPos )
		{
			if( unboundParams.Count == 0 || scope == null )
				return;

			foreach( TplParam param in unboundParams ) {
				TplParamDecl placeholder = new() {
					name   = param.name,
					kind   = param.kind,
					type   = param.type,
					srcPos = scope.decl?.srcPos ?? fallbackPos,
				};

				ScopeLeaf leaf = new() {
					parent = scope,
					decl   = placeholder,
				};
				placeholder.scope = scope;

				if( !scope.children.TryGetValue( param.name, out List<ScopeLeaf>? list ) ) {
					list = new List<ScopeLeaf>( 1 );
					scope.children.Add( param.name, list );
				}
				list.Add( leaf );
				context.LocalDecls.Add( (placeholder, scope) );
			}
		}

		/// <summary>
		/// Finds the primary template for a cross-module specialization: same
		/// namespace path and same name, in a module the specialization's module
		/// imports directly, or in an implicitly imported std module. The generated
		/// include of that module precedes this module's declarations, so include
		/// order puts the primary before the specialization.
		/// </summary>
		private Decl? FindImportedPrimary( string name, string nsFqn, Func<Decl, bool> isPrimary )
		{
			foreach( string importedName in ImportedModuleNames( moduleAst ) ) {
				foreach( CompiledModuleResult m in allModules ) {
					if( m.Module.module != importedName )
						continue;

					Myll.Core.Hierarchical? scope = FindNamespaceScope( m.Module, nsFqn );
					if( scope == null || scope.scope == null
					 || !scope.scope.children.TryGetValue( name, out List<ScopeLeaf>? leaves ) )
						continue;

					foreach( ScopeLeaf leaf in leaves ) {
						if( isPrimary( leaf.decl ) )
							return leaf.decl;
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
		private static void MarkInvalidSpec( Decl spec )
			=> spec.SpecInfo = new Decl.SpecData { Primary = spec };

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
