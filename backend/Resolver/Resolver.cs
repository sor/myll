using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Myll.Core;
using Myll.Transform;

namespace Myll.Resolver
{
	public sealed class NameResolver
	{
		private static readonly HashSet<string> BuiltInIdentifiers = new( StringComparer.Ordinal ) {
			"static_assert",
			"null",
			"true",
			"false",
		};

		private static readonly HashSet<string> BuiltInTypes = new( StringComparer.Ordinal ) {
			"void",
			"bool",
			"char",
			"int",
			"float",
			"auto",
			"byte",
			"int", "uint",
			"i8", "i16", "i32", "i64",
			"u8", "u16", "u32", "u64",
			"isize", "usize",
			"iptr", "uptr",
			"f16", "f32", "f64", "f128",
		};

		private static readonly Decl BuiltInDecl = new VarDecl {
			name   = "<builtin>",
			access = Access.Public,
			kind   = VarDecl.Kind.Const,
		};

		private static readonly bool DebugOverloadResolution =
			Environment.GetEnvironmentVariable( "MYLL_DEBUG_OVERLOAD" ) == "1";

		// LONE: emergency toggle for the lone-candidate overload shortcut.  It is kept
		// in the source even though it is currently off so that a future larger testset
		// can fall back to it quickly if new single-candidate conversion failures surface.
		// Off by default because the known failure modes are documented and fixed in
		// plan/lone-candidate-investigation.md.
		//
		// Set to true to restore the original behaviour where a single viable candidate
		// is accepted without a Myll-side conversion check.
		private const bool UseLoneCandidateShortcut = false;

		private readonly IReadOnlyDictionary<string, ModuleExports> moduleExports;
		private readonly IReadOnlyDictionary<GlobalNamespace, ModuleContext> moduleContexts;
		private readonly ResolutionResult result;
		private readonly List<Diagnostic> diagnostics;
		private readonly TypeResolver typeResolver;

		private NameResolver(
			IReadOnlyDictionary<string, ModuleExports> moduleExports,
			IReadOnlyDictionary<GlobalNamespace, ModuleContext> moduleContexts,
			ResolutionResult result,
			List<Diagnostic> diagnostics )
		{
			this.moduleExports = moduleExports;
			this.moduleContexts = moduleContexts;
			this.result        = result;
			this.diagnostics   = diagnostics;
			this.typeResolver  = new TypeResolver( result, diagnostics );
		}

		public void SetDialect( Dialect dialect )
			=> typeResolver.Dialect = dialect;

		private ModuleContext GetContext( GlobalNamespace module )
			=> moduleContexts[module];

		public static ResolveResult Resolve( IReadOnlyList<CompiledModuleResult> modules )
		{
			var result      = new ResolutionResult();
			var diagnostics = new List<Diagnostic>();

			var transformers = new List<Transformer> {
				new DefaultAttributesTransformer(),
				new ComparisonModeTransformer(),
				new OperatorDefaultAttributesTransformer(),
				new EnumTransformer(),
				new AutoReturnTransformer(),
				new TemplateParamTransformer(),
				new ChainTransformer(),
				new DiscardTransformer(),
				new RuleOfTransformer(),
				new SpecialMemberAttributeValidator(),
				new ElseOnLoopTransformer(),
				new BreakContinueTransformer(),
				new ConfiguredAliasShadowingTransformer(),
				new RangeForAutoTypeTransformer(),
				// ContainedNamespaceUsingInjector is disabled for now; Myll emits fully
				// qualified names, so per-function `using namespace` injection is redundant.
				// new ContainedNamespaceUsingInjector(),
			};

			foreach( CompiledModuleResult module in modules )
				foreach( Transformer t in transformers )
					diagnostics.AddRange( t.DoPreTransform( new TransformContext( module, null ) ) );

			var exports  = BuildModuleExports( modules );
			var contexts = modules.ToDictionary( m => m.Module, m => m.Context );
			var resolver = new NameResolver( exports, contexts, result, diagnostics );
			resolver.ResolveDialectBackings( modules, exports );

		bool progress;
		do {
			progress = false;
			foreach( (GlobalNamespace module, ModuleContext context) in modules ) {
				resolver.SetDialect( context.Dialect );
				progress |= resolver.ResolveUsings( module, context );
				progress |= resolver.ResolveAliases( module, context );
				progress |= resolver.ResolveIds( module, context );
				progress |= resolver.ResolveScopeds( module, context );
				progress |= resolver.ResolveTypes( module, context );
				progress |= resolver.ResolveMemberAccesses( module, context );
			}

			// Commit resolved declarations after type/member resolution and before
			// overload resolution so that conversion checks (e.g., derived-to-base
			// pointer/reference) see populated resolvedDecl fields. Any resolutions
			// made later will be committed again in the final Apply() below.
			result.Apply();

			foreach( (GlobalNamespace module, ModuleContext context) in modules )
				resolver.SetDialect( context.Dialect );

			// Infer the concrete type of range-based for loop variables declared with
			// 'auto' as soon as the range expression type is known. This must happen
			// before overload resolution so that uses of the loop variable inside the
			// body are checked against the real element type.
			var rangeForTransformer = new RangeForAutoTypeTransformer();
			foreach( CompiledModuleResult module in modules ) {
				if( module.Context.IsPrototypeFile )
					continue;
				diagnostics.AddRange( rangeForTransformer.DoPostTransform(
					new TransformContext( module, result ) ) );
			}

			foreach( (GlobalNamespace module, ModuleContext context) in modules ) {
				resolver.SetDialect( context.Dialect );
				progress |= resolver.ResolveCalls( module, context );
			}
		} while( progress );

			// Commit resolved declarations to the AST so that type checking can use
			// resolvedDecl directly on identifiers, member accesses, and type specs.
			result.Apply();

			// Recompute dependent nested-name information after all template
			// parameters and type arguments have been resolved. The incremental
			// resolution pass may have marked a prefix as non-dependent because
			// a TplParamDecl was not yet resolved when the prefix was checked.
			resolver.UpdateDependentNestedNames( modules );

			resolver.ValidateGlobalUsings( modules );
			resolver.ValidateModuleDecls( modules );

			foreach( CompiledModuleResult module in modules )
				foreach( Transformer t in transformers )
					diagnostics.AddRange( t.DoPostTransform( new TransformContext( module, result ) ) );

			resolver.RejectRequiresClauses( modules );
			resolver.ValidateTypes( modules );
			resolver.ResolveSyntheticInitListTypes( modules );
			result.Apply();
			resolver.UpdateDependentNestedNames( modules );
			resolver.ReportUnresolved( modules );

			return new( result, diagnostics );
		}

		private static IReadOnlyDictionary<string, ModuleExports> BuildModuleExports(
			IReadOnlyList<CompiledModuleResult> modules )
		{
			Dictionary<string, ModuleExports> ret = new();
			foreach( (GlobalNamespace module, _) in modules ) {
				var exports = new ModuleExports();
				CollectExports( module.scope, exports );
				ret[module.module] = exports;
			}

			return ret;
		}

		private static void CollectExports( Scope scope, ModuleExports exports )
		{
			foreach( (string name, List<ScopeLeaf> leaves) in scope.children ) {
				foreach( ScopeLeaf leaf in leaves ) {
					Decl? decl = leaf.decl;
					if( decl == null )
						continue;
					if( decl.IsHidden )
						continue;

					exports.Add( name, decl );

					// Only namespaces expose their children to unqualified cross-module lookup.
					// Members of classes/structs/enums are accessed through their enclosing type.
					if( decl is Namespace ns )
						CollectExports( ns.scope, exports );
				}
			}
		}

		private static IEnumerable<string> ImportedModuleNames( GlobalNamespace module )
		{
			foreach( NamedImportSpec spec in module.imports.OfType<NamedImportSpec>() )
				yield return spec.name;

			foreach( string imp in StdBuiltins.ImplicitStdImports )
				if( imp != module.module )
					yield return imp;
		}

		private void ResolveDialectBackings(
			IReadOnlyList<CompiledModuleResult> modules,
			IReadOnlyDictionary<string, ModuleExports> exports )
		{
			// Build a combined view of all module exports so a class like `std::string`
			// declared in an imported prototype module can back the `string` keyword.
			var combinedExports = new ModuleExports();
			foreach( (GlobalNamespace module, _) in modules ) {
				if( exports.TryGetValue( module.module, out ModuleExports? moduleExports ) )
					moduleExports.CopyTo( combinedExports );
			}

			Decl? ResolveName( string fqn )
			{
				string[] parts = fqn.Split( "::" );
				List<Decl> candidates = new( combinedExports.LookupAll( parts[0] ) );
				if( candidates.Count == 0 )
					return null;

				for( int i = 1; i < parts.Length; i++ ) {
					List<Decl> next = new();
					foreach( Decl current in candidates ) {
						Scope? scope = current switch {
							Namespace ns => ns.scope,
							Structural s => s.scope,
							_            => null,
						};

						if( scope == null )
							continue;

						if( scope.children.TryGetValue( parts[i], out List<ScopeLeaf>? leaves ) ) {
							next.AddRange(
								leaves
									.Where( l => l.decl != null && !l.decl.IsHidden )
									.Select( l => l.decl! ) );
						}
					}

					if( next.Count == 0 )
						return null;
					candidates = next;
				}

				return candidates.Count == 1 ? candidates[0] : null;
			}

			foreach( (GlobalNamespace module, ModuleContext context) in modules ) {
				Decl? stringDecl = ResolveName( context.Dialect.StringClassName );
				if( stringDecl is Hierarchical ) {
					context.StringBackingClass = stringDecl;

					if( !module.scope.importedNames.ContainsKey( "string" ) )
						module.scope.importedNames.Add( "string", new List<Decl> { stringDecl } );
				}
				else if( stringDecl != null ) {
					diagnostics.Add( new Diagnostic(
						null,
						DiagnosticKind.Error,
						String.Format(
							"Dialect setting 'StringClassName = {0}' does not name a class or struct.",
							context.Dialect.StringClassName ) ) );
				}
				// If it is null, unresolved member diagnostics on string will guide the user.
			}
		}

		private readonly struct NameRef<TNode>
			where TNode : Expr
		{
			public readonly TNode                  Node;
			public readonly Scope                  Scope;
			public readonly IReadOnlyList<IdTplArgs> Segments;

			public NameRef( TNode node, Scope scope, IReadOnlyList<IdTplArgs> segments )
			{
				Node     = node;
				Scope    = scope;
				Segments = segments;
			}
		}

		private bool ResolveNameReferences<TNode>(
			GlobalNamespace                         module,
			ModuleContext                           context,
			IEnumerable<NameRef<TNode>>             refs,
			IReadOnlyDictionary<TNode, Decl>        alreadyResolved,
			Action<TNode, Decl>                     recordResolution,
			Action<TNode, Decl?>                    markInvalid )
			where TNode : Expr
		{
			bool progress = false;
			foreach( NameRef<TNode> r in refs ) {
				if( alreadyResolved.ContainsKey( r.Node ) || result.IsResolvedOrInvalid( r.Node ) )
					continue;

				bool callSite = context.FuncCallCallees.Contains( r.Node );
				Decl? resolved = ResolvePath( r.Segments, r.Scope, module, out _, callSite );
				if( resolved == null )
					continue;

				if( TryResolveVariableTemplate( resolved, r.Node ) ) {
					recordResolution( r.Node, resolved );
					progress = true;
				}
				else {
					markInvalid( r.Node, resolved );
				}
			}

			return progress;
		}

		private bool ResolveIds( GlobalNamespace module, ModuleContext context )
		{
			return ResolveNameReferences(
				module,
				context,
				context.UnresolvedIds.Select(
					u => new NameRef<IdExpr>( u.Node, u.Scope, new[] { u.Node.idTplArgs } ) ),
				result.Ids,
				( node, decl ) => result.Resolve( node, decl ),
				( node, decl ) => result.MarkInvalid( node, decl ) );
		}

		private bool ResolveScopeds( GlobalNamespace module, ModuleContext context )
		{
			return ResolveNameReferences(
				module,
				context,
				context.UnresolvedScopeds.Select(
					u => new NameRef<ScopedExpr>( u.Node, u.Scope, u.Node.idTpls ) ),
				result.Scopeds,
				( node, decl ) => result.Resolve( node, decl ),
				( node, decl ) => result.MarkInvalid( node, decl ) );
		}

		/// <summary>
		/// Performs template argument validation and type substitution for variable templates.
		/// Returns true when the reference is valid and should be recorded as a resolution.
		/// Returns false when the template arity mismatches; the caller must then mark the
		/// node invalid rather than recording the resolution, and must not re-process the node.
		/// </summary>
		private bool TryResolveVariableTemplate( Decl resolved, Expr expr )
		{
			if( resolved is not VarDecl varDecl || varDecl.TplParams.Count == 0 )
				return true;

			IdTplArgs idTpl = expr switch {
				IdExpr     id     => id.idTplArgs,
				ScopedExpr scoped => scoped.idTpls.Last(),
				_                 => throw new InvalidOperationException(
					"Unexpected expression kind for variable template resolution" ),
			};

			TemplateArgValidation validation = TemplateInference.ValidateTemplateArgs( varDecl, idTpl.tplArgs );
			if( validation.IsMismatch ) {
				string message = validation.MismatchKind == TemplateArgMismatchKind.Arity
					? String.Format( "Template argument count mismatch for variable '{0}': expected {1}, got {2}",
						varDecl.name,
						varDecl.TplParams.Count,
						idTpl.tplArgs.Count )
					: String.Format( "Template argument kind mismatch for variable '{0}': parameter {1} ('{2}') expects a {3}, but argument '{4}' is a {5}",
						varDecl.name,
						validation.MismatchIndex!.Value,
						validation.ParameterName,
						validation.Expected,
						idTpl.tplArgs[validation.MismatchIndex.Value].Gen(),
						validation.Actual );
				diagnostics.Add( new Diagnostic(
					expr.srcPos,
					DiagnosticKind.Error,
					message ) );
				return false;
			}

			expr.Type = TemplateInference.SubstituteTemplateParams(
				varDecl.type,
				varDecl.TplParams,
				idTpl.tplArgs.ToList(),
				result );
			return true;
		}

		private bool ResolveTypes( GlobalNamespace module, ModuleContext context )
		{
			bool progress = false;
			foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
				if( result.IsResolvedOrInvalid( unresolved.Node ) )
					continue;

				// Types are never call sites: prefer the type over a constructor.
				Decl? resolved = ResolvePath( unresolved.Node.idTpls, unresolved.Scope, module, out _, callSite: false );

				// Compute per-prefix dependency information for all nested names so
				// that the generator can emit the C++ 'typename' and 'template'
				// disambiguators, even when the final segment itself resolves.
				bool isDependentNestedName = IsDependentNestedName( unresolved.Node, unresolved.Scope, module );

				if( resolved == null ) {
					if( isDependentNestedName ) {
					unresolved.Node.isDependent = true;
						progress = true;
					}

					continue;
				}

				// The name resolved but may still depend on a template parameter.
				// IsDependentNestedName has already populated prefixDependent.
				unresolved.Node.isDependent = isDependentNestedName;

				if( resolved is TplParamDecl tplParam && tplParam.kind == TplParamKind.NonType ) {
					diagnostics.Add( new Diagnostic(
						unresolved.Node.srcPos,
						DiagnosticKind.Error,
						String.Format(
							"Non-type template parameter '{0}' cannot be used as a type",
							GetTypeName( unresolved.Node ) ) ) );
					result.MarkInvalid( unresolved.Node, resolved );
					continue;
				}

				IReadOnlyList<TplArg> providedArgs = unresolved.Node.idTpls.Last().tplArgs;
				TemplateArgValidation typeValidation = TemplateInference.ValidateTemplateArgs( resolved, providedArgs );
				if( typeValidation.IsMismatch
				 && !IsInjectedClassName( unresolved.Node, resolved, unresolved.Scope ) ) {
					string message = typeValidation.MismatchKind == TemplateArgMismatchKind.Arity
						? String.Format( "Template argument count mismatch for type '{0}': expected {1}, got {2}",
							GetTypeName( unresolved.Node ),
							TemplateInference.GetTemplateParamCount( resolved ),
							providedArgs.Count )
						: String.Format( "Template argument kind mismatch for type '{0}': parameter {1} ('{2}') expects a {3}, but argument '{4}' is a {5}",
							GetTypeName( unresolved.Node ),
							typeValidation.MismatchIndex!.Value,
							typeValidation.ParameterName,
							typeValidation.Expected,
							providedArgs[typeValidation.MismatchIndex.Value].Gen(),
							typeValidation.Actual );
					diagnostics.Add( new Diagnostic(
						unresolved.Node.srcPos,
						DiagnosticKind.Error,
						message ) );
					result.MarkInvalid( unresolved.Node, resolved );
					continue;
				}

				result.Resolve( unresolved.Node, resolved );
				progress = true;
			}

			return progress;
		}

		/// <summary>
		/// Returns true when the type name is a nested name that depends on a template
		/// parameter, e.g. T::Nested, Class<T>::Nested, or T::Nested<U>.
		/// Populates <paramref name="type"/>.prefixDependent so the generator can emit
		/// the C++ 'typename' and 'template' disambiguators.
		/// </summary>
		private void UpdateDependentNestedNames( IReadOnlyList<CompiledModuleResult> modules )
		{
			foreach( (GlobalNamespace module, ModuleContext context) in modules ) {
				foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
					IsDependentNestedName( unresolved.Node, unresolved.Scope, module );
				}
			}
		}

		private void ValidateGlobalUsings( IReadOnlyList<CompiledModuleResult> modules )
		{
			foreach( (GlobalNamespace module, ModuleContext context) in modules ) {
				Dialect dialect = context.Dialect;

				foreach( UnresolvedUsing unresolved in context.UnresolvedUsings ) {
					UsingDecl usingDecl = unresolved.Node;

					if( !usingDecl.IsNamespaceUsing )
						continue;

					Scope scope = unresolved.Scope;

					// Namespace-scope `using` is always emitted in the header and is valid C++.
					if( scope.decl is Namespace and not GlobalNamespace )
						continue;

					// Class/struct/union scope is always treated as contained because
					// C++ does not allow `using namespace` inside a class definition.
					if( scope.decl is Structural )
						continue;

					// Module/global scope is controlled by the dialect.
					if( scope.decl is GlobalNamespace ) {
						if( dialect.GlobalUsingNS != GlobalUsingNSMode.Disabled )
							continue;
					}

					diagnostics.Add( new Diagnostic(
						usingDecl.srcPos,
						DiagnosticKind.Error,
						"Global `using` of a namespace is disabled in this dialect."
					) );
				}
			}
		}

		/// <summary>
		/// Rejects declarations that merge into a single translation unit and collide,
		/// mirroring what C++ would reject: redefined classes/structs/enums/aliases,
		/// duplicate variables, and same-signature function definitions (ODR
		/// violations). Applies to two declarations from different modules that are
		/// visible from one module, and to duplicate declarations within one module.
		/// Declarations and definitions with different signatures (overloads) stay
		/// legal, as does a same-signature declaration plus its definition; functions
		/// that differ only in return type are rejected because C++ cannot overload
		/// on return type. Only real modules participate; prototype (.decl.myll)
		/// modules are replaced by their raw C++ includes and never contribute their
		/// own definitions.
		/// </summary>
		private void ValidateModuleDecls( IReadOnlyList<CompiledModuleResult> modules )
		{
			// Key: (enclosing namespace fqn, name) → declarations from real modules.
			Dictionary<(string Ns, string Name), List<ModuleDecl>> declsByKey = new();
			Dictionary<string, HashSet<string>> visibleByModule = new( StringComparer.Ordinal );

			foreach( CompiledModuleResult m in modules ) {
				GlobalNamespace module = m.Module;
				if( m.Context.IsPrototypeFile )
					continue;

				HashSet<string> visible = new( StringComparer.Ordinal ) { module.module };
				Queue<string> pending = new( module.imports.OfType<NamedImportSpec>().Select( s => s.name ) );
				while( pending.Count > 0 ) {
					string next = pending.Dequeue();
					if( !visible.Add( next ) )
						continue;
					foreach( CompiledModuleResult candidate in modules ) {
						if( candidate.Module.module != next )
							continue;
						foreach( string imp in candidate.Module.imports.OfType<NamedImportSpec>().Select( s => s.name ) )
							pending.Enqueue( imp );
						break;
					}
				}

				foreach( string imp in StdBuiltins.ImplicitStdImports )
					visible.Add( imp );

				visibleByModule[module.module] = visible;
				CollectModuleDecls( module.scope, "", m, declsByKey );
			}

			foreach( ((string ns, string name), List<ModuleDecl> decls) in declsByKey ) {
				List<ModuleDecl> distinct = decls
					.GroupBy( d => d.Decl, ReferenceEqualityComparer.Instance )
					.Select( g => g.First() )
					.ToList();
				if( distinct.Count < 2 )
					continue;

				ReportModuleCollisions( ns, name, distinct, visibleByModule );
			}
		}

		private static void CollectModuleDecls(
			Scope                                      scope,
			string                                     nsPrefix,
			CompiledModuleResult                       module,
			Dictionary<(string, string), List<ModuleDecl>> declsByKey )
		{
			foreach( (string name, List<ScopeLeaf> leaves) in scope.children ) {
				foreach( ScopeLeaf leaf in leaves ) {
					Decl? decl = leaf.decl;
					if( decl == null || decl.IsHidden )
						continue;

					if( decl is Namespace ns ) {
						CollectModuleDecls(
							ns.scope,
							nsPrefix.Length == 0 ? name : nsPrefix + "::" + name,
							module,
							declsByKey );
						continue;
					}

					// Only declarations that live in namespace scope reach the C++
					// namespace; class/struct/union members are accessed through the type.
					if( !IsNamespaceScoped( scope ) )
						continue;

					if( !declsByKey.TryGetValue( (nsPrefix, name), out List<ModuleDecl>? list ) ) {
						list = new List<ModuleDecl>();
						declsByKey[(nsPrefix, name)] = list;
					}

					list.Add( new ModuleDecl( decl, module.Module.module ) );
				}
			}
		}

		private static bool IsNamespaceScoped( Scope scope )
		{
			for( Scope? cur = scope; cur != null; cur = cur.parent ) {
				if( cur.decl is GlobalNamespace )
					return true;
				if( cur.decl is Structural or Enumeration )
					return false;
			}

			return false;
		}

		private void ReportModuleCollisions(
			string                             ns,
			string                             name,
			List<ModuleDecl>                  decls,
			Dictionary<string, HashSet<string>> visibleByModule )
		{
			string fqn = ns.Length == 0 ? name : ns + "::" + name;

			for( int i = 0; i < decls.Count; i++ ) {
				for( int j = i + 1; j < decls.Count; j++ ) {
					ModuleDecl a = decls[i];
					ModuleDecl b = decls[j];
					// A module always sees its own declarations; only cross-module pairs
					// need a module that imports (transitively) both sides.
					bool crossModule = a.Module != b.Module;
					if( crossModule && !HasCommonVisibleModule( a.Module, b.Module, visibleByModule ) )
						continue;
					CollisionKind kind = ClassifyCollision( a.Decl, b.Decl );
					if( kind == CollisionKind.None )
						continue;

					// Anchor the error on the later declaration; note points to the earlier one.
					ModuleDecl later   = IsLater( a.Decl, b.Decl ) ? a : b;
					ModuleDecl earlier = later == a ? b : a;

					string error = crossModule
					             ? ( kind == CollisionKind.Redefinition       ? String.Format( "Redefinition of '{0}' (first defined in module '{1}').", fqn, earlier.Module )
					               : kind == CollisionKind.ReturnTypeMismatch ? String.Format( "Function '{0}' differs only in return type (first defined in module '{1}').", fqn, earlier.Module )
					               :                                            String.Format( "Name '{0}' is used for both a {1} and a {2} (the {2} is defined in module '{3}').", fqn, KindName( later.Decl ), KindName( earlier.Decl ), earlier.Module ) )
					             : ( kind == CollisionKind.Redefinition       ? String.Format( "Redefinition of '{0}' in this module.", fqn )
					               : kind == CollisionKind.ReturnTypeMismatch ? String.Format( "Function '{0}' differs only in return type in this module.", fqn )
					               :                                            String.Format( "Name '{0}' is used for both a {1} and a {2} in this module.", fqn, KindName( later.Decl ), KindName( earlier.Decl ) ) );

					string note = kind == CollisionKind.KindMismatch
					            ? String.Format( "The {0} declaration of '{1}' is here.", KindName( earlier.Decl ), fqn )
					            : String.Format( "Previous definition of '{0}' here.",    fqn );

					diagnostics.Add( new Diagnostic( later.Decl.srcPos,   DiagnosticKind.Error, error ) );
					diagnostics.Add( new Diagnostic( earlier.Decl.srcPos, DiagnosticKind.Note,  note  ) );
				}
			}
		}

		private enum CollisionKind
		{
			None,
			Redefinition,
			ReturnTypeMismatch,
			KindMismatch,
		}

		/// <summary>
		/// Classifies how two declarations of the same name collide when merged into one
		/// translation unit. Mirrors what C++ rejects: redefinitions, same-parameter
		/// functions that differ only in return type (C++ cannot overload on return
		/// type), and name sharing between different declaration kinds (Myll has no
		/// elaborated type specifiers). A declaration plus its matching definition is
		/// not a collision; different parameters are ordinary overloads.
		/// </summary>
		private static CollisionKind ClassifyCollision( Decl a, Decl b )
		{
			if( !SameKind( a, b ) )
				return CollisionKind.KindMismatch;

			if( a is Func fa && b is Func fb ) {
				if( !FunctionParamsEqual( fa, fb ) )
					return CollisionKind.None;
				if( fa.retType.Gen() != fb.retType.Gen() )
					return CollisionKind.ReturnTypeMismatch;

				bool aDef = !fa.IsForwardDeclaration && !fa.IsExternal;
				bool bDef = !fb.IsForwardDeclaration && !fb.IsExternal;
				return aDef && bDef ? CollisionKind.Redefinition : CollisionKind.None;
			}

			if( a is VarDecl va && b is VarDecl vb )
				return !IsLinkSafeVar( va ) && !IsLinkSafeVar( vb )
				 ? CollisionKind.Redefinition
				 : CollisionKind.None;

			return CollisionKind.Redefinition; // Structural, Enumeration, AliasDecl, Structor
		}

		private static string KindName( Decl d )
			=> d is Structural or Enumeration ? "type"
			 : d is Func                     ? "function"
			 : d is Structor                 ? "constructor"
			 : d is VarDecl                  ? "variable"
			 : d is AliasDecl                ? "alias"
			 :                                 "declaration";

		private static bool SameKind( Decl a, Decl b )
			=> ( a is Func ) == ( b is Func )
			&& ( a is Structor ) == ( b is Structor )
			&& ( a is VarDecl ) == ( b is VarDecl )
			&& ( a is Structural ) == ( b is Structural )
			&& ( a is Enumeration ) == ( b is Enumeration )
			&& ( a is AliasDecl ) == ( b is AliasDecl );

		private static bool IsLinkSafeVar( VarDecl v )
			=> v.IsHidden          // emitted as `static` (internal linkage) only
			 || v.IsCompileTime   // emitted as `constexpr`, no symbol
			 || v.IsExternal      // declaration only, no definition
			 || ( v.type.qual & Qualifier.Const ) != 0; // const global: no symbol

		private static bool FunctionParamsEqual( Func a, Func b )
		{
			if( TemplateInference.GetTemplateParamCount( a ) != TemplateInference.GetTemplateParamCount( b ) )
				return false;
			if( a.paras.Count != b.paras.Count )
				return false;
			for( int i = 0; i < a.paras.Count; i++ ) {
				if( a.paras[i].type.Gen() != b.paras[i].type.Gen() )
					return false;
			}

			return true;
		}

		// Full signature equality (parameters plus return type). Used for merging a
		// declaration with its definition; only a fully equal pair is one function.
		private static bool FunctionSignaturesEqual( Func a, Func b )
			=> FunctionParamsEqual( a, b )
			 && a.retType.Gen() == b.retType.Gen();

		private static bool IsSameSignatureCandidate( Decl a, Decl b )
			=> ( a is Func fa && b is Func fb )
			 ? FunctionSignaturesEqual( fa, fb )
			 : ( a is Structor sa && b is Structor sb ) && StructorSignaturesEqual( sa, sb );

		private static bool StructorSignaturesEqual( Structor a, Structor b )
		{
			if( a.paras.Count != b.paras.Count )
				return false;
			for( int i = 0; i < a.paras.Count; i++ ) {
				if( a.paras[i].type.Gen() != b.paras[i].type.Gen() )
					return false;
			}

			return true;
		}

		// When merging the same signature, prefer the real definition over the
		// declaration/forward variant.
		private static bool PrefersCandidate( Decl a, Decl b )
			=> IsDefinition( a ) && !IsDefinition( b );

		private static bool IsDefinition( Decl d )
		{
			if( d is Func f )
				return !f.IsForwardDeclaration && !f.IsExternal;
			return true;
		}

		private static bool HasCommonVisibleModule(
			string                             a,
			string                             b,
			Dictionary<string, HashSet<string>> visibleByModule )
		{
			foreach( HashSet<string> visible in visibleByModule.Values ) {
				if( visible.Contains( a ) && visible.Contains( b ) )
					return true;
			}

			return false;
		}

		// Order declarations by file name first, then by line. The input file list
		// arrives sorted by file name (the harness and the driver both do this),
		// which matches the order a reader encounters the sources.
		private static bool IsLater( Decl a, Decl b )
		{
			int fileCompare = StringComparer.Ordinal.Compare(
				Path.GetFileName( a.srcPos.file ),
				Path.GetFileName( b.srcPos.file ) );
			if( fileCompare != 0 )
				return fileCompare > 0;
			return a.srcPos.from.line > b.srcPos.from.line;
		}

		private sealed class ModuleDecl
		{
			public readonly Decl   Decl;
			public readonly string Module;

			public ModuleDecl( Decl decl, string module )
			{
				Decl   = decl;
				Module = module;
			}
		}

		private static void RecordContainedNamespaceUsingIfNeeded(
			ModuleContext context,
			UsingDecl     usingDecl,
			Scope         scope,
			Namespace     ns )
		{
			bool needsRecording = scope.decl switch {
				Structural        => true,
				GlobalNamespace   => context.Dialect.GlobalUsingNS == GlobalUsingNSMode.Contained,
				_                 => false,
			};

			if( !needsRecording )
				return;

			string nsName = ns.FullyQualifiedName;
			if( !context.ContainedNamespaceUsingsByScope.TryGetValue( scope, out List<string>? list ) ) {
				list = new List<string>();
				context.ContainedNamespaceUsingsByScope[scope] = list;
			}

			if( !list.Contains( nsName ) )
				list.Add( nsName );
		}

		private bool IsDependentNestedName( TypespecNested type, Scope scope, GlobalNamespace module )
		{
			bool anyDependent = false;
			type.prefixDependent.Clear();

			for( int i = 0; i < type.idTpls.Count; i++ ) {
				IdTplArgs segment = type.idTpls[i];

				IReadOnlyList<IdTplArgs> prefix = type.idTpls.Take( i + 1 ).ToList();
				Decl? prefixDecl = ResolvePath( prefix, scope, module, out _, callSite: false );
				bool prefixIsDependent = prefixDecl is TplParamDecl;

				foreach( IdTplArgs prefixSegment in prefix ) {
					foreach( TplArg arg in prefixSegment.tplArgs ) {
						if( arg.typespec == null )
							continue;

						if( IsDependentArg( arg.typespec ) ) {
							prefixIsDependent = true;
							break;
						}
					}

					if( prefixIsDependent )
						break;
				}

				type.prefixDependent.Add( prefixIsDependent );
				anyDependent |= prefixIsDependent;
			}

			// Mark the whole nested name as dependent whenever any prefix depends on a
			// template parameter. GenType uses this flag together with prefixDependent
			// to emit the required 'typename' and 'template' C++ disambiguators.
			type.isDependent = anyDependent;

			return anyDependent;
		}

		private bool IsDependentArg( Typespec typespec )
		{
			if( typespec is not TypespecNested nested )
				return false;

			if( nested.resolvedDecl is TplParamDecl )
				return true;

			if( nested.resolvedDecl == null
			 && result.TryGetResolved( nested, out Decl? resolved )
			 && resolved is TplParamDecl )
				return true;

			return nested.IsDependentType();
		}

		private static string GetTypeName( TypespecNested type )
		{
			return String.Join( "::", type.idTpls.Select( s => s.id ) );
		}

		private static bool IsInjectedClassName( TypespecNested type, Decl resolved, Scope scope )
		{
			if( resolved is not Structural structural )
				return false;

			if( type.idTpls.Count != 1 || type.idTpls[0].tplArgs.Count != 0 )
				return false;

			for( Scope? cur = scope; cur != null; cur = cur.parent ) {
				if( cur.decl == structural )
					return true;
			}

			return false;
		}

		private bool ResolveMemberAccesses( GlobalNamespace module, ModuleContext context )
		{
			bool progress = false;
			foreach( UnresolvedMemberAccess unresolved in context.UnresolvedMemberAccesses ) {
				if( unresolved.Node.right is not IdExpr member )
					continue;

				if( result.Members.ContainsKey( member ) )
					continue;

			if( !TryGetMemberAccessBaseType(
					unresolved.Node.left,
					unresolved.Scope,
					module,
					out Hierarchical? baseType,
					viaArrow: IsArrowMemberAccess( unresolved.Node.op ) ) )
				continue;

			Decl? resolved = LookupMember( member.idTplArgs.id, (Structural) baseType!, module );
				if( resolved != null ) {
					result.ResolveMember( member, resolved );
					progress = true;
				}
			}

			return progress;
		}

		private bool ResolveCalls( GlobalNamespace module, ModuleContext context )
		{
			bool progress = false;
			foreach( UnresolvedCall call in context.UnresolvedCalls ) {
				progress |= ResolveSingleCall( call, module );
			}

			return progress;
		}

		private bool ResolveSingleCall( UnresolvedCall call, GlobalNamespace module )
		{
			switch( call.Callee ) {
				case IdExpr id: {
					bool alreadyResolved = result.Ids.TryGetValue( id, out Decl? resolved );
					if( alreadyResolved && resolved is not Func ) {
						// Callee resolved to a non-function (builtin variable, external type, ...).
						// Do not re-validate; the C++ compiler handles the call.
						return false;
					}

					List<Decl> candidates = alreadyResolved
						? new List<Decl> { resolved! }
						: LookupNameCandidates( id.idTplArgs.id, call.Scope, module );

					var outcome = TryResolveOverload( candidates, call.Call, call.Scope, module, call.Callee );
					ApplyOverloadOutcome( outcome, id, call.Callee );
					return false;
				}
				case ScopedExpr scoped: {
					bool alreadyResolved = result.Scopeds.TryGetValue( scoped, out Decl? resolved );
					if( alreadyResolved && resolved is not Func ) {
						// Scoped callee resolved to a type/namespace; accept constructor-style
						// calls without Myll-side validation.
						return false;
					}

					var outcome = alreadyResolved
						? TryResolveOverload( new List<Decl> { resolved! }, call.Call, call.Scope, module, call.Callee )
						: ResolveScopedCallOverload( scoped, call.Call, call.Scope, module, call.Callee );
					ApplyScopedOverloadOutcome( outcome, scoped, call.Callee );
					return false;
				}
				case BinOp binOp when IsMemberAccessOperation( binOp.op ): {
					if( binOp.right is not IdExpr member )
						return false;

					if( result.Members.TryGetValue( member, out Decl? memberResolved ) ) {
						if( memberResolved is not Func ) {
							// Member resolved to a non-function (e.g. a template type alias).
							// Leave validation to the C++ compiler.
							return false;
						}

						var memberOutcome = TryResolveOverload(
							new List<Decl> { memberResolved }, call.Call, call.Scope, module, call.Callee );
						if( memberOutcome.chosen == null )
							RecordOverloadFailure( memberOutcome, call.Callee, member.idTplArgs.id );
						return false;
					}

					if( !TryGetMemberAccessBaseType(
							binOp.left,
							call.Scope,
							module,
							out Hierarchical? baseType,
							viaArrow: IsArrowMemberAccess( binOp.op ) ) )
						return false;

					List<Decl> candidates = LookupInHierarchicalCandidates(
						member.idTplArgs.id, baseType, module );
					var outcome = TryResolveOverload( candidates, call.Call, call.Scope, module, call.Callee );

					if( outcome.chosen != null ) {
						result.ResolveMember( member, outcome.chosen );
						return true;
					}

					// When the base type is known but the member is not declared there,
					// do not emit a "no matching overload" diagnostic here. The missing
					// member will be reported at the end if it remains unresolved.
					if( candidates.Count > 0 )
						RecordOverloadFailure( outcome, call.Callee, member.idTplArgs.id );
					return false;
				}
			}

			return false;
		}

		private bool ApplyOverloadOutcome( (Decl? chosen, bool ambiguous, bool noMatch) outcome, IdExpr id, Expr callee )
		{
			if( outcome.chosen != null ) {
				result.Resolve( id, outcome.chosen );
				return true;
			}

			RecordOverloadFailure( outcome, callee, id.idTplArgs.id );
			return false;
		}

		private bool ApplyScopedOverloadOutcome( (Decl? chosen, bool ambiguous, bool noMatch) outcome, ScopedExpr scoped, Expr callee )
		{
			if( outcome.chosen != null ) {
				result.Resolve( scoped, outcome.chosen );
				return true;
			}

			RecordOverloadFailure( outcome, callee, scoped.idTpls.Last().id );
			return false;
		}

		private void RecordOverloadFailure( (Decl? chosen, bool ambiguous, bool noMatch) outcome, Expr callee, string name )
		{
			if( outcome.ambiguous ) {
				if( result.IsInvalidCall( callee ) )
					return;
				result.MarkInvalidCall( callee, new InvalidCallInfo(
					InvalidCallKind.Ambiguous, name, null, 0 ) );
				diagnostics.Add( new Diagnostic(
					GetCalleeSrcPos( callee ),
					DiagnosticKind.Error,
					String.Format( "Ambiguous call to '{0}'", name ) ) );
			}
			else if( outcome.noMatch ) {
				if( result.IsInvalidCall( callee ) )
					return;
				result.MarkInvalidCall( callee, new InvalidCallInfo(
					InvalidCallKind.NoMatchingOverload, name, null, 0 ) );
				diagnostics.Add( new Diagnostic(
					GetCalleeSrcPos( callee ),
					DiagnosticKind.Error,
					String.Format( "No matching overload for '{0}'", name ) ) );
			}
		}

		private static SrcPos GetCalleeSrcPos( Expr callee )
		{
			return callee switch {
				IdExpr     id      => id.srcPos,
				ScopedExpr scoped  => scoped.srcPos,
				BinOp      binOp   => binOp.right is IdExpr m ? m.srcPos : binOp.srcPos,
				_                  => callee.srcPos,
			};
		}

		private (Decl? chosen, bool ambiguous, bool noMatch) ResolveScopedCallOverload(
			ScopedExpr      scoped,
			FuncCall        call,
			Scope           scope,
			GlobalNamespace module,
			Expr            callee )
		{
			IReadOnlyList<IdTplArgs> segments = scoped.idTpls;
			if( segments.Count == 0 )
				return (null, false, true);

			List<IdTplArgs> prefix = segments.Take( segments.Count - 1 ).ToList();
			if( prefix.Count > 0 ) {
				Decl? prefixDecl = ResolvePath( prefix, scope, module, out _, callSite: false );
				if( prefixDecl == null || prefixDecl is not Hierarchical h )
					return (null, false, true);

				List<Decl> candidates = LookupInHierarchicalCandidates(
					segments.Last().id, h, module );
				return TryResolveOverload( candidates, call, scope, module, callee );
			}

			// Single-segment scoped call: treat like an unqualified name.
			List<Decl> single = LookupNameCandidates( segments[0].id, scope, module );
			return TryResolveOverload( single, call, scope, module, callee );
		}

		private static bool IsMemberAccessOperation( Operand op )
			=> op is Operand.MemberAccess
			|| op is Operand.NCMemberAccess
			|| op is Operand.MemberPtrAccess
			|| op is Operand.MemberAccessPtr
			|| op is Operand.NCMemberAccessPtr
			|| op is Operand.MemberPtrAccessPtr;

		private static bool IsArrowMemberAccess( Operand op )
			=> op is Operand.MemberAccessPtr
			|| op is Operand.NCMemberAccessPtr
			|| op is Operand.MemberPtrAccess;

		private static void LogOverloadResolution( string message, params object[] args )
		{
			if( !DebugOverloadResolution )
				return;
			Console.Error.WriteLine( "[Myll overload] " + String.Format( message, args ) );
		}

		private static string FormatParamsForDebug( Decl decl )
		{
			List<Param> paras = decl switch {
				Func func    => func.paras,
				Structor stc => stc.paras,
				_            => new List<Param>(),
			};
			return "(" + String.Join( ", ", paras.Select( p => FormatTypeForDebug( p.type ) ) ) + ")";
		}

		private static string FormatTypeForDebug( Typespec? type )
		{
			if( type == null )
				return "<?>";
			string suffix = "";
			if( type.ptrs is { Count: > 0 } )
				suffix = " " + String.Join( "", type.ptrs.Select( p => p.kind switch {
					Pointer.Kind.RawPtr    => "*",
					Pointer.Kind.LVRef     => "&",
					Pointer.Kind.RVRef     => "&&",
					Pointer.Kind.Unique     => "*!",
					Pointer.Kind.Shared     => "*@",
					Pointer.Kind.Weak       => "*?",
					Pointer.Kind.UniqueArray => "*![]",
					Pointer.Kind.SharedArray => "*@[]",
					Pointer.Kind.WeakArray   => "*?[]",
					Pointer.Kind.PtrToAry    => "[]",
					_                        => "?",
				} ) );
			string prefix = type.qual == Qualifier.Const ? "const " : "";
			string name = type switch {
				TypespecBasic b         => b.kind.ToString(),
				TypespecNested n        => String.Join( "::", n.idTpls.Select( it => it.id ) ),
				TypespecFunc f          => "<func>",
				_                       => "<?>",
			};
			return prefix + name + suffix;
		}

		private (Decl? chosen, bool ambiguous, bool noMatch) TryResolveOverload(
			List<Decl>      candidates,
			FuncCall        call,
			Scope           scope,
			GlobalNamespace module,
			Expr            callee )
		{
			List<Decl> callable = new();
			foreach( Decl candidate in candidates ) {
				if( candidate is Func || candidate is Structor ) {
					callable.Add( candidate );
					continue;
				}

			if( candidate is Structural structural ) {
				callable.AddRange(
					structural.children
						.OfType<Structor>()
						.Where( s => s.kind == Structor.Kind.Constructor ) );
			}
		}

		if( callable.Count == 0 )
			return (null, false, true);

		// Collect argument/parameter pairs that rejected with None so we can decide
		// whether the failure is definitive or just premature (types not resolved yet).
		List<(Typespec source, Typespec target)> rejectedPairs = new();

		string calleeName = callee switch {
			IdExpr id                                => id.idTplArgs.id,
			ScopedExpr scoped                        => scoped.idTpls.Count > 0 ? scoped.idTpls.Last().id : "?",
			BinOp binOp when binOp.right is IdExpr m => m.idTplArgs.id,
			_                                        => "?",
		};
		LogOverloadResolution( "Resolving '{0}' with {1} arg(s) among {2} candidate(s)",
			calleeName, call.args.Count, callable.Count );
		for( int i = 0; i < callable.Count; i++ ) {
			Decl c = callable[i];
			LogOverloadResolution( "  candidate[{0}]: {1}{2} arity={3} templateParams={4}",
				i, c.name, FormatParamsForDebug( c ), GetParamCount( c ), TemplateInference.GetTemplateParamCount( c ) );
		}

		// Prefer an arity match first; if only one candidate has the right arity, pick it.
		List<Decl> sameArity = callable
			.Where( d => GetParamCount( d ) == call.args.Count )
			.ToList();

		LogOverloadResolution( "  {0} candidate(s) match arity {1}", sameArity.Count, call.args.Count );

		if( sameArity.Count == 0 )
			return (null, false, true);

		IReadOnlyList<TplArg>? explicitTemplateArgs = TemplateInference.GetExplicitTemplateArgs( callee );
		Dictionary<Decl, List<TplArg>> deducedArgsByCandidate = new();
		bool templateMismatch = false;

		// Filter candidates by template-argument availability and, when no explicit
		// arguments are supplied, try to deduce them from the call arguments.
		List<Decl> templateViable = new();
		foreach( Decl candidate in sameArity ) {
			if( explicitTemplateArgs is { Count: > 0 } ) {
				TemplateArgValidation validation = TemplateInference.ValidateTemplateArgs( candidate, explicitTemplateArgs );
				if( validation.IsMismatch ) {
					LogOverloadResolution( "    candidate {0}: template mismatch ({1})", candidate.name, validation.MismatchKind );
					templateMismatch = true;
					continue;
				}

				LogOverloadResolution( "    candidate {0}: explicit template args, accepted", candidate.name );
				templateViable.Add( candidate );
				continue;
			}

			// Forward/external functions whose return type does not depend on template
			// parameters are usually left to the C++ compiler. Non-template externals
			// are always accepted; external templates are accepted without Myll
			// deduction when they are the only arity match. When there are other
			// same-arity candidates we must still try to deduce the external template
			// so it can compete in ranking, but we do not apply the deduced arguments
			// to the callee expression.
			if( candidate is Func fwd
			 && ( fwd.IsForwardDeclaration || fwd.IsExternal )
			 && !fwd.retType.IsDependentType() ) {
				if( fwd.TplParams.Count == 0 ) {
					LogOverloadResolution( "    candidate {0}: external non-template, accepted", candidate.name );
					templateViable.Add( candidate );
					continue;
				}

				List<TplArg>? externalDeduced = null;
				bool externalDeducedOk = sameArity.Count > 1
					&& TemplateInference.TryDeduceTemplateArgs( fwd, call, typeResolver, result, out externalDeduced )
					&& externalDeduced != null;
				if( externalDeducedOk ) {
					LogOverloadResolution( "    candidate {0}: external template deduced ({1}), accepted",
						candidate.name, String.Join( ", ", externalDeduced!.Select( a => FormatTypeForDebug( a.typespec ) ) ) );
					deducedArgsByCandidate[candidate] = externalDeduced!;
					templateViable.Add( candidate );
				}
				else if( sameArity.Count == 1 ) {
					LogOverloadResolution( "    candidate {0}: external template alone, accepted", candidate.name );
					templateViable.Add( candidate );
				}
				else {
					LogOverloadResolution( "    candidate {0}: external template deduction failed", candidate.name );
				}

				continue;
			}

			if( candidate is Func func ) {
				(List<TplParam> ownerParams, List<TplArg> ownerArgs) = GetOwnerTemplateInfo( func, callee );

				bool deductionOk = TemplateInference.TryDeduceTemplateArgs(
					func, call, typeResolver, result, out List<TplArg>? deduced,
					ownerParams.Count > 0 ? ownerParams : null,
					ownerArgs.Count > 0 ? ownerArgs : null );

				if( deductionOk && deduced != null ) {
					LogOverloadResolution( "    candidate {0}: internal template deduced ({1}), accepted",
						candidate.name, String.Join( ", ", deduced.Select( a => FormatTypeForDebug( a.typespec ) ) ) );
					deducedArgsByCandidate[candidate] = deduced;
					templateViable.Add( candidate );
					continue;
				}

				if( deductionOk && deduced == null && func.TplParams.Count == 0 ) {
					LogOverloadResolution( "    candidate {0}: non-template method, accepted", candidate.name );
					templateViable.Add( candidate );
					continue;
				}

				LogOverloadResolution( "    candidate {0}: internal template deduction failed", candidate.name );
				continue;
			}

			if( TemplateInference.GetTemplateParamCount( candidate ) == 0 ) {
				LogOverloadResolution( "    candidate {0}: non-template, accepted", candidate.name );
				templateViable.Add( candidate );
			}
			else {
				LogOverloadResolution( "    candidate {0}: template deduction failed", candidate.name );
			}
		}

		// Merge candidates that are the same declaration (a declaration and its
		// definition, e.g. from different modules, or two identical definitions).
		// C++ treats them as one overload; Myll must not read them as ambiguous.
		if( templateViable.Count > 1 ) {
			List<Decl> deduplicated = new( templateViable.Count );
			foreach( Decl candidate in templateViable ) {
				int index = -1;
				for( int i = 0; i < deduplicated.Count; i++ ) {
					if( IsSameSignatureCandidate( candidate, deduplicated[i] ) ) {
						index = i;
						break;
					}
				}

				if( index < 0 ) {
					deduplicated.Add( candidate );
				}
				else if( PrefersCandidate( candidate, deduplicated[index] ) ) {
					deduplicated[index] = candidate;
				}
			}

			templateViable = deduplicated;
		}

		LogOverloadResolution( "  {0} candidate(s) survived template viability" , templateViable.Count );
		foreach( Decl c in templateViable )
			LogOverloadResolution( "    viable: {0}{1}", c.name, FormatParamsForDebug( c ) );
		if( templateViable.Count == 0 ) {

			if( templateMismatch && sameArity.Count == 1 )
				ReportTemplateMismatch( sameArity[0], explicitTemplateArgs!, callee );
			else if( templateMismatch )
				ReportTemplateMismatch( sameArity, explicitTemplateArgs!, callee );

			return (null, false, true);
		}

		// A lone non-template candidate (or a template candidate without explicit/deduced
		// arguments) is accepted without a conversion check. This preserves the old
		// resolver behaviour for class-template methods and other dependent calls where
		// the concrete type-checking is delegated to the C++ compiler.
		// Only do this when it is the *only* same-arity candidate. If other same-arity
		// candidates were filtered out (e.g. a template whose deduction failed because an
		// argument type is not known yet), we must run the conversion check or wait for
		// another resolver pass instead of blindly picking the remaining candidate.
		// LONE: lone-candidate shortcut.  Currently disabled (UseLoneCandidateShortcut
		// is false).  If re-enabled, this short-circuit accepts any single viable
		// candidate without a Myll conversion check.  See plan/lone-candidate-investigation.md.
		if( UseLoneCandidateShortcut
		 && templateViable.Count == 1
		 && sameArity.Count == 1
		 && explicitTemplateArgs?.Count == 0
		 && !deducedArgsByCandidate.ContainsKey( templateViable[0] ) ) {
			LogOverloadResolution( "  lone candidate {0}, skipping conversion check", templateViable[0].name );
			return (templateViable[0], false, false);
		}

		// Resolve argument types and rank every remaining viable candidate by the worst
		// conversion required. This rejects explicit template instantiations with
		// incompatible arguments (e.g. identity<int>("hi")) and keeps deduction honest.
		List<Typespec?> argTypes = call.args
			.Select( a => typeResolver.Resolve( a.expr ) )
			.ToList();

		// If any argument type is still unknown, we cannot decide yet. Do not emit
		// a diagnostic; a later resolver pass may provide the missing type.
		if( argTypes.Any( t => t == null ) )
			return (null, false, false);

		LogOverloadResolution( "  argument types: {0}",
			String.Join( ", ", argTypes.Select( t => FormatTypeForDebug( t ) ) ) );

		List<(Decl Candidate, ConversionRank WorstRank)> viable = new();
		ConversionRank bestWorst = ConversionRank.None;

		foreach( Decl candidate in templateViable ) {
			ConversionRank worst = ConversionRank.Exact;
			bool ok = true;

			List<Param> paras = candidate switch {
				Func func    => func.paras,
				Structor stc => stc.paras,
				_            => new List<Param>(),
			};

			List<TplArg>? candidateArgs = explicitTemplateArgs is { Count: > 0 }
				? explicitTemplateArgs as List<TplArg>
				: deducedArgsByCandidate.TryGetValue( candidate, out List<TplArg>? deduced )
					? deduced
					: null;

			(List<TplParam> ownerParams, List<TplArg> ownerArgs) = GetOwnerTemplateInfo( candidate, callee );

			List<TplParam> combinedParams = TemplateInference.GetAllTemplateParams( candidate );
			List<TplArg> combinedArgs = new( ownerArgs.Count + ( candidateArgs?.Count ?? 0 ) );
			combinedArgs.AddRange( ownerArgs );
			if( candidateArgs != null )
				combinedArgs.AddRange( candidateArgs );

			string rankDetails = "";
			for( int i = 0; i < paras.Count; i++ ) {
				Typespec paramType = combinedParams.Count > 0 && combinedArgs.Count > 0
					? TemplateInference.SubstituteTemplateParams( paras[i].type, combinedParams, combinedArgs, result )
					: paras[i].type;

				ConversionRank rank = ConversionRules.GetRank(
					argTypes[i]!,
					paramType,
					result,
					ConversionRules.IsLValue( call.args[i].expr ),
					allowArrayDecay: true );
				rankDetails += String.Format( " [{0}] {1}->{2}={3}",
					i, FormatTypeForDebug( argTypes[i] ), FormatTypeForDebug( paramType ), rank );
				if( rank == ConversionRank.None ) {
					ok = false;
					rejectedPairs.Add( (argTypes[i]!, paramType) );
					break;
				}

				if( rank > worst )
					worst = rank;
			}

			LogOverloadResolution( "    ranking {0}{1} -> {2}{3}",
				candidate.name, FormatParamsForDebug( candidate ), ok ? "" : " REJECTED", rankDetails );

			if( !ok )
				continue;

			if( worst < bestWorst ) {
				viable.Clear();
				viable.Add( (candidate, worst) );
				bestWorst = worst;
			}
			else if( worst == bestWorst ) {
				viable.Add( (candidate, worst) );
			}
		}

		LogOverloadResolution( "  {0} candidate(s) with best worst-rank {1}", viable.Count, bestWorst );
		foreach( var v in viable )
			LogOverloadResolution( "    best: {0}{1} rank={2} templateParams={3}",
				v.Candidate.name, FormatParamsForDebug( v.Candidate ), v.WorstRank, TemplateInference.GetTemplateParamCount( v.Candidate ) );

		// C++ tie-breaker: when several candidates require the same conversion,
		// prefer a non-template over a template (or a less-specialized template).
		if( viable.Count > 1 ) {
			List<(Decl Candidate, ConversionRank WorstRank)> nonTemplates = viable
				.Where( v => TemplateInference.GetTemplateParamCount( v.Candidate ) == 0 )
				.ToList();

			if( nonTemplates.Count > 0 ) {
				LogOverloadResolution( "  tie-breaker: preferring {0} non-template candidate(s)", nonTemplates.Count );
				viable = nonTemplates;
			}
		}

		if( viable.Count == 1 ) {
			Decl chosen = viable[0].Candidate;
			LogOverloadResolution( "  chosen: {0}{1}", chosen.name, FormatParamsForDebug( chosen ) );
			// External/forward templates are ranked with Myll deduction, but the C++
			// compiler is still responsible for the real deduction; do not emit
			// explicit template arguments for them.
			bool shouldApplyDeduced = chosen is Func chosenFunc
			                       && !( chosenFunc.IsForwardDeclaration || chosenFunc.IsExternal );
			if( shouldApplyDeduced
			 && deducedArgsByCandidate.TryGetValue( chosen, out List<TplArg>? deduced ) )
				TemplateInference.ApplyTemplateArgs( callee, deduced );
			return (chosen, false, false);
		}

		if( viable.Count > 1 ) {
			LogOverloadResolution( "  ambiguous" );
			return (null, true, false);
		}

		if( rejectedPairs.Count > 0
		 && rejectedPairs.Any( p => TypeMightResolveLater( p.source ) || TypeMightResolveLater( p.target ) ) ) {
			LogOverloadResolution( "  no viable candidate yet; deferring because types are still unresolved" );
			return (null, false, false);
		}

		LogOverloadResolution( "  no viable candidate" );
		return (null, false, true);
	}

		/// <summary>
		/// Returns true when a type still contains unresolved nested names. In that case a
		/// conversion that currently ranks as None might succeed once the fixed-point
		/// resolver fills in resolvedDecl and canonicalizes the type path.
		/// </summary>
		private static bool TypeMightResolveLater( Typespec? type )
		{
			if( type == null )
				return true;

			switch( type ) {
				case TypespecBasic:
					return false;

				case TypespecNested nested: {
					if( nested.resolvedDecl == null )
						return true;

					if( nested.resolvedDecl is TplParamDecl )
						return false;

					foreach( IdTplArgs segment in nested.idTpls ) {
						foreach( TplArg arg in segment.tplArgs ) {
							if( TypeMightResolveLater( arg.typespec ) )
								return true;
						}
					}

					// A class/struct might have base classes that are not resolved yet.
					// If a derived-to-base conversion is rejected now, it could succeed once
					// the base type is resolved in a later fixed-point pass.
					if( nested.resolvedDecl is Structural structural ) {
						foreach( BaseType bt in structural.basetypes ) {
							if( TypeMightResolveLater( bt.type ) )
								return true;
						}
					}

					return false;
				}

				case TypespecFunc func:
					return TypeMightResolveLater( func.retType )
					    || func.paras.Any( p => TypeMightResolveLater( p.type ) );

				default:
					return false;
			}
		}

		private void ReportTemplateMismatch( Decl candidate, IReadOnlyList<TplArg> args, Expr callee )
		{
			if( result.IsInvalidCall( callee ) )
				return;

			string name = GetTemplateCalleeName( callee );
			TemplateArgValidation validation = TemplateInference.ValidateTemplateArgs( candidate, args );

			string message;
			InvalidCallKind kind;
			if( validation.IsMismatch
			 && validation.MismatchKind == TemplateArgMismatchKind.ArgumentKind ) {
				kind    = InvalidCallKind.TemplateKindMismatchSingle;
				message = String.Format(
					"Template argument kind mismatch for '{0}': parameter {1} ('{2}') expects a {3}, but argument '{4}' is a {5}",
					name,
					validation.MismatchIndex!.Value,
					validation.ParameterName,
					validation.Expected,
					args[validation.MismatchIndex.Value].Gen(),
					validation.Actual );
			}
			else {
				int expected = TemplateInference.GetTemplateParamCount( candidate );
				int actual   = args.Count;
				kind         = InvalidCallKind.TemplateArityMismatchSingle;
				message      = expected == 0
					? String.Format( "'{0}' is not a template; 0 template arguments expected, {1} provided", name, actual )
					: String.Format( "Template argument count mismatch for '{0}': expected {1}, got {2}", name, expected, actual );
			}

			result.MarkInvalidCall( callee, new InvalidCallInfo( kind, name, candidate, args.Count ) );
			diagnostics.Add( new Diagnostic( GetCalleeSrcPos( callee ), DiagnosticKind.Error, message ) );
		}

		private void ReportTemplateMismatch( List<Decl> candidates, IReadOnlyList<TplArg> args, Expr callee )
		{
			if( result.IsInvalidCall( callee ) )
				return;

			string name = GetTemplateCalleeName( callee );

			foreach( Decl candidate in candidates ) {
				TemplateArgValidation validation = TemplateInference.ValidateTemplateArgs( candidate, args );
				if( !validation.IsMismatch
				 || validation.MismatchKind != TemplateArgMismatchKind.ArgumentKind )
					continue;

				result.MarkInvalidCall( callee, new InvalidCallInfo(
					InvalidCallKind.TemplateKindMismatchMultiple,
					name,
					candidate,
					args.Count ) );
				diagnostics.Add( new Diagnostic(
					GetCalleeSrcPos( callee ),
					DiagnosticKind.Error,
					String.Format(
						"No matching template overload for '{0}' with {1} template argument(s) (parameter '{2}' expects a {3})",
						name,
						args.Count,
						validation.ParameterName,
						validation.Expected ) ) );
				return;
			}

			result.MarkInvalidCall( callee, new InvalidCallInfo(
				InvalidCallKind.TemplateArityMismatchMultiple,
				name,
				null,
				args.Count ) );
			diagnostics.Add( new Diagnostic(
				GetCalleeSrcPos( callee ),
				DiagnosticKind.Error,
				String.Format( "No matching template overload for '{0}' with {1} template argument(s)", name, args.Count ) ) );
		}

		private static string GetTemplateCalleeName( Expr callee )
		{
			return callee switch {
				IdExpr     id                                       => id.idTplArgs.id,
				ScopedExpr scoped                                   => scoped.idTpls.Last().id,
				BinOp      binOp when binOp.right is IdExpr member  => member.idTplArgs.id,
				_                                                   => "?",
			};
		}

		private static Typespec BuildSelfType( Hierarchical hierarchical, bool isReference )
		{
			if( hierarchical is not Structural structural )
				return new TypespecBasic { kind = TypespecBasic.Kind.ExplicitAuto };

			var tplArgList = structural.TplParams
				.Select( p => new TplArg {
					typespec = new TypespecNested {
						resolvedDecl = new TplParamDecl { name = p.name },
						idTpls       = new List<IdTplArgs> { new() { id = p.name } },
					},
				} )
				.ToList();

			var type = new TypespecNested {
				resolvedDecl = structural,
				idTpls       = new List<IdTplArgs> {
					new() {
						id      = structural.name,
						tplArgs = tplArgList,
					},
				},
			};

			if( isReference )
				type.ptrs = new List<Pointer> { new() { kind = Pointer.Kind.LVRef } };

			return type;
		}

		private static int GetParamCount( Decl decl )
			=> decl switch {
				Func func      => func.paras.Count,
				Structor stc   => stc.paras.Count,
				_              => -1,
			};

		private bool ResolveUsings( GlobalNamespace module, ModuleContext context )
		{
			bool progress = false;

			foreach( UnresolvedUsing unresolved in context.UnresolvedUsings ) {
				if( unresolved.Node.type is not TypespecNested path )
					continue;

				Decl? resolved = ResolvePath( path.idTpls, unresolved.Scope, module, out _, callSite: false );
				if( resolved == null )
					continue;

				Scope scope = unresolved.Scope;
				if( resolved is Namespace ns ) {
					if( !unresolved.Node.IsNamespaceUsing ) {
						unresolved.Node.IsNamespaceUsing = true;
						progress = true;
					}
					if( !scope.importedScopes.Contains( ns.scope ) ) {
						scope.importedScopes.Add( ns.scope );
						progress = true;
					}

					RecordContainedNamespaceUsingIfNeeded( context, unresolved.Node, scope, ns );
				}
				else {
					string importedName = path.idTpls.Last().id;
					if( !scope.importedNames.TryGetValue( importedName, out List<Decl>? list ) ) {
						list = new List<Decl>( 1 );
						scope.importedNames.Add( importedName, list );
					}

					if( !list.Contains( resolved ) ) {
						list.Add( resolved );
						progress = true;
					}
				}
			}

			foreach( UnresolvedUsingStmt unresolved in context.UnresolvedUsingsStmt ) {
				if( unresolved.Node.type is not TypespecNested path )
					continue;

				Decl? resolved = ResolvePath( path.idTpls, unresolved.Scope, module, out _, callSite: false );
				if( resolved == null )
					continue;

				Scope scope = unresolved.Scope;
				if( resolved is Namespace ns ) {
					if( !unresolved.Node.IsNamespaceUsing ) {
						unresolved.Node.IsNamespaceUsing = true;
						progress = true;
					}
					if( !scope.importedScopes.Contains( ns.scope ) ) {
						scope.importedScopes.Add( ns.scope );
						progress = true;
					}
				}
				else {
					string importedName = path.idTpls.Last().id;
					if( !scope.importedNames.TryGetValue( importedName, out List<Decl>? list ) ) {
						list = new List<Decl>( 1 );
						scope.importedNames.Add( importedName, list );
					}

					if( !list.Contains( resolved ) ) {
						list.Add( resolved );
						progress = true;
					}
				}
			}

			return progress;
		}

		private bool ResolveAliases( GlobalNamespace module, ModuleContext context )
		{
			bool progress = false;
		foreach( UnresolvedAlias unresolved in context.UnresolvedAliases ) {
			if( unresolved.Node.type is not TypespecNested path )
				continue;

			// The alias body is resolved in the alias's own scope so that template
			// parameters are visible; the alias name itself lives in the parent scope.
			Scope aliasScope = unresolved.Scope;
			Scope parentScope = aliasScope.parent!;

			Decl? resolved = ResolvePath( path.idTpls, aliasScope, module, out _, callSite: false );
			if( resolved == null )
				continue;

			bool isNamespaceTarget = resolved is Namespace;
			if( unresolved.Node.IsNamespaceAlias != isNamespaceTarget ) {
				unresolved.Node.IsNamespaceAlias = isNamespaceTarget;
				progress = true;
			}

			if( isNamespaceTarget && !parentScope.importedScopes.Contains( ( (Namespace)resolved ).scope ) ) {
				parentScope.importedScopes.Add( ( (Namespace)resolved ).scope );
				progress = true;
			}

			// Non-namespace aliases must be registered under their AliasDecl so that uses
			// of the alias name keep the concrete template arguments stored on the alias.
			Decl target = isNamespaceTarget ? resolved : unresolved.Node;
			progress |= AddImportedName( parentScope, unresolved.Node.name, target );

			if( !isNamespaceTarget
			 && unresolved.Node.type is TypespecNested aliasPath
			 && aliasPath.resolvedDecl == null ) {
				aliasPath.resolvedDecl = resolved;
				progress = true;
			}
		}

			return progress;
		}

		private static bool AddImportedName( Scope scope, string? name, Decl target )
		{
			if( name == null )
				return false;

			if( !scope.importedNames.TryGetValue( name, out List<Decl>? list ) ) {
				list = new List<Decl>( 1 );
				scope.importedNames.Add( name, list );
			}

			if( list.Contains( target ) )
				return false;

			list.Add( target );
			return true;
		}

		private Decl? ResolvePath(
			IReadOnlyList<IdTplArgs> segments,
			Scope                    startScope,
			GlobalNamespace          module,
			out int                  unresolvedSegmentIndex,
			bool                     callSite = false )
		{
			unresolvedSegmentIndex = -1;
			if( segments.Count == 0 )
				return null;

			string first = segments[0].id;
			List<Decl> candidates = LookupNameCandidates( first, startScope, module );
			Decl? current = PickSingleCandidate( candidates, callSite );
			if( current == null ) {
				if( candidates.Count == 0
				 && segments.Count == 1
				 && (BuiltInIdentifiers.Contains( first ) || BuiltInTypes.Contains( first )) )
					return BuiltInDecl;

				unresolvedSegmentIndex = 0;
				return null;
			}

			for( int i = 1; i < segments.Count; i++ ) {
				if( current is not Hierarchical h ) {
					unresolvedSegmentIndex = i;
					return null;
				}

				string name = segments[i].id;
				candidates = LookupInHierarchicalCandidates( name, h, module );
				Decl? next = PickSingleCandidate( candidates, callSite );
				if( next == null ) {
					unresolvedSegmentIndex = i;
					return null;
				}

				current = next;
			}

			return current;
		}

		private bool TryGetMemberAccessBaseType(
			Expr expr,
			Scope scope,
			GlobalNamespace module,
			out Hierarchical baseType,
			bool viaArrow = false )
		{
			baseType = null!;
			Typespec? type = null;

			if( expr is SelfExpr selfExpr ) {
				baseType = FindEnclosingStructural( scope )!;
				if( baseType != null && selfExpr.Type == null )
					selfExpr.Type = BuildSelfType( baseType, isReference: true );
				return baseType != null;
			}

			if( expr is ThisExpr thisExpr ) {
				baseType = FindEnclosingStructural( scope )!;
				if( baseType != null && thisExpr.Type == null )
					thisExpr.Type = BuildSelfType( baseType, isReference: false );
				return baseType != null;
			}

			if( expr is IdExpr id ) {
				if( result.Ids.TryGetValue( id, out Decl? decl ) || result.Members.TryGetValue( id, out decl ) ) {
					switch( decl ) {
						case VarDecl vd:
							type = vd.type;
							break;
						case Func f:
							type = f.retType;
							break;
					}
				}
			}
			else if( expr is ScopedExpr scoped ) {
				if( result.Scopeds.TryGetValue( scoped, out Decl? decl ) ) {
					switch( decl ) {
						case VarDecl vd:
							type = vd.type;
							break;
						case Func f:
							type = f.retType;
							break;
					}
				}
			}
			else if( expr is UnOp parenUnop && parenUnop.op == Operand.Parens ) {
				return TryGetMemberAccessBaseType( parenUnop.expr, scope, module, out baseType );
			}
			else if( expr is UnOp derefUnop && derefUnop.op == Operand.Dereference ) {
				if( TryGetMemberAccessBaseType( derefUnop.expr, scope, module, out Hierarchical inner, viaArrow: false ) ) {
					Typespec? operandType = typeResolver.Resolve( derefUnop.expr );
					if( operandType != null
					 && OperatorResolver.TryGetOperatorReturnType(
					 	operandType, "operator*", result, out Typespec? starReturnType )
					 && starReturnType != null
					 && TryGetBaseHierarchical( starReturnType, module, out Hierarchical operatorBase, viaArrow: false ) ) {
						baseType = operatorBase;
						return true;
					}

					baseType = inner;
					return true;
				}
			}
			else if( expr is FuncCallExpr call ) {
				if( call.funcCall.indexer ) {
					if( TryGetIndexerElementType( call.expr, module, out Typespec elementType ) )
						type = elementType;
				}

				if( type == null ) {
					// `ctor()` or other calls: derive the returned type from the callee.
					if( call.expr is IdExpr calleeId && result.Ids.TryGetValue( calleeId, out Decl? calleeIdDecl ) ) {
						if( calleeIdDecl is Func f )
							type = f.retType;
						else if( calleeIdDecl is Structor s && s.kind == Structor.Kind.Constructor )
							baseType = FindEnclosingStructural( s.scope )!;
						else if( calleeIdDecl is Structural structural )
							type = BuildClassValueType( structural, new[] { calleeId.idTplArgs } );
					}
					else if( call.expr is ScopedExpr calleeScoped && result.Scopeds.TryGetValue( calleeScoped, out Decl? calleeScopedDecl ) ) {
						if( calleeScopedDecl is Func f )
							type = f.retType;
						else if( calleeScopedDecl is Structor s && s.kind == Structor.Kind.Constructor )
							baseType = FindEnclosingStructural( s.scope )!;
						else if( calleeScopedDecl is Structural structural )
							type = BuildClassValueType( structural, calleeScoped.idTpls );
					}
					else if( call.expr is BinOp calleeBin
					     && calleeBin.op.In( Operand.MemberAccess, Operand.MemberPtrAccess )
					     && calleeBin.right is IdExpr member
					     && result.Members.TryGetValue( member, out Decl? memberDecl ) ) {
						switch( memberDecl ) {
							case VarDecl vd:
								type = vd.type;
								break;
							case Func f:
								type = f.retType;
								if( TryGetReceiverType( calleeBin.left, out Typespec receiverType ) ) {
									Typespec? substituted = OperatorResolver.SubstituteMethodReturnType( f, receiverType, result );
									if( substituted != null )
										type = substituted;
								}
								break;
						}
					}
				}
			}
			else if( expr is BinOp binop && binop.op.In( Operand.MemberAccess, Operand.MemberPtrAccess ) ) {
				if( binop.right is IdExpr prevMember && result.Members.TryGetValue( prevMember, out Decl? member ) ) {
					switch( member ) {
						case VarDecl vd:
							type = vd.type;
							break;
						case Func f:
							type = f.retType;
							break;
					}
				}
			}

			if( baseType != null )
				return true;

			return type != null && TryGetBaseHierarchical( type, module, out baseType, viaArrow );
		}

		private bool TryGetReceiverType( Expr expr, out Typespec receiverType )
		{
			receiverType = null!;

			if( expr is IdExpr id ) {
				if( result.Ids.TryGetValue( id, out Decl? decl ) || result.Members.TryGetValue( id, out decl ) ) {
					switch( decl ) {
						case VarDecl vd:
							receiverType = vd.type;
							return true;
						case Func f:
							receiverType = f.retType;
							return true;
					}
				}
			}
			else if( expr is BinOp binop && IsMemberAccessOperation( binop.op )
			     && binop.right is IdExpr member
			     && result.Members.TryGetValue( member, out Decl? memberDecl ) ) {
				switch( memberDecl ) {
					case VarDecl vd:
						receiverType = vd.type;
						return true;
					case Func f:
						receiverType = f.retType;
						return true;
				}
			}

			return false;
		}

		private bool TryGetIndexerElementType( Expr receiver, GlobalNamespace module, out Typespec elementType )
		{
			elementType = null!;

			if( !TryGetReceiverType( receiver, out Typespec receiverType ) )
				return false;

			Typespec? builtinElement = TryGetArrayElementType( receiverType );
			if( builtinElement != null ) {
				elementType = builtinElement;
				return true;
			}

			if( !TryGetBaseHierarchical( receiverType, module, out Hierarchical? container ) )
				return false;

			if( container is not Structural structural )
				return false;

			bool receiverIsConst = ( receiverType.qual & Qualifier.Const ) != 0;
			Func? idxFunc = OperatorResolver.LookupOperatorOverload(
				structural, "operator[]", receiverIsConst, new HashSet<Structural>(), result );
			if( idxFunc == null )
				return false;


			Typespec ret = idxFunc.retType;
			Typespec? substituted = OperatorResolver.SubstituteMethodReturnType( idxFunc, receiverType, result );
			if( substituted != null )
				ret = substituted;

			elementType = ret;
			return true;
		}

		/// <summary>
		/// For an array/pointer typespec, returns the element type after stripping the outermost
		/// subscriptable pointer (pointer-to-array, raw array, or smart array).
		/// Raw pointers (T*) are not subscriptable. Returns null if the type is not subscriptable.
		/// </summary>
		private static Typespec? TryGetArrayElementType( Typespec? type )
		{
			if( type?.ptrs == null || type.ptrs.Count == 0 )
				return null;

			Pointer.Kind kind = type.ptrs[ type.ptrs.Count - 1 ].kind;
			if( kind != Pointer.Kind.PtrToAry
			 && kind != Pointer.Kind.RawArray
			 && kind != Pointer.Kind.UniqueArray
			 && kind != Pointer.Kind.SharedArray
			 && kind != Pointer.Kind.WeakArray
			 && kind != Pointer.Kind.Array
			 && kind != Pointer.Kind.Vector )
				return null;

			Typespec ret = ConversionRules.CloneTypespecBase( type );
			ret.ptrs = type.ptrs.Take( type.ptrs.Count - 1 ).ToList();
			return ret;
		}

		private static Hierarchical? FindEnclosingStructural( Scope scope )
		{
			for( Scope? cur = scope; cur != null; cur = cur.parent ) {
				if( cur.decl is Hierarchical h && h is not GlobalNamespace )
					return h;
			}

			return null;
		}

		private static Hierarchical? FindEnclosingStructural( ScopeLeaf? leaf )
		{
			for( ScopeLeaf? cur = leaf; cur != null; cur = cur.parent ) {
				if( cur.decl is Hierarchical h && h is not GlobalNamespace )
					return h;
			}

			return null;
		}

		private Structural? GetFirstBaseStructural( Structural structural )
		{
			if( structural.basetypes.Count == 0 )
				return null;

			if( structural.basetypes[0].type is not TypespecNested nested )
				return null;

			Decl? decl = nested.resolvedDecl;
			if( decl == null )
				result.TryGetResolved( nested, out decl );

			return decl as Structural;
		}

		private static Structural? GetDefiningStructural( Structural structural )
		{
			if( !structural.IsForwardDeclaration )
				return structural;

			Scope? parent = structural.scope?.parent;
			if( parent == null )
				return null;

			if( !parent.children.TryGetValue( structural.name, out List<ScopeLeaf>? leaves ) )
				return null;

			foreach( ScopeLeaf leaf in leaves ) {
				if( leaf.decl is Structural s && !s.IsForwardDeclaration )
					return s;
			}

			return null;
		}

		private Typespec UnwrapAliasWithIndirection(
			Typespec           type,
			HashSet<AliasDecl>? visited = null )
		{
			visited ??= new HashSet<AliasDecl>();

			if( type is not TypespecNested useSite
			 || ConversionRules.GetResolvedDecl( useSite, result ) is not AliasDecl alias
			 || !visited.Add( alias ) )
				return type;

			Typespec expanded = ConversionRules.TryExpandAliasTemplate( useSite, alias, result, out Typespec aliasExpanded )
				? aliasExpanded
				: ConversionRules.CloneTypespecBase( alias.type );

			// Preserve the outer qualifiers and pointer/reference/smart-pointer wrappers
			// from the use site unless the alias itself already supplies them.
			if( expanded.qual == Qualifier.None )
				expanded.qual = type.qual;

			if( ( expanded.ptrs == null || expanded.ptrs.Count == 0 )
			 && type.ptrs is { Count: > 0 } )
				expanded.ptrs = type.ptrs.Select( p => new Pointer { kind = p.kind, qual = p.qual, expr = p.expr } ).ToList();

			return UnwrapAliasWithIndirection( expanded, visited );
		}

		private bool TryGetBaseHierarchical(
			Typespec type,
			GlobalNamespace module,
			out Hierarchical baseType,
			bool viaArrow = false )
		{
			baseType = null!;

			type = UnwrapAliasWithIndirection( type );

			if( type is TypespecNested nested ) {
				Decl? decl = nested.resolvedDecl;
				if( decl == null )
					result.TryGetResolved( nested, out decl );
				if( decl is Structural st && st.IsForwardDeclaration )
					decl = GetDefiningStructural( st ) ?? decl;

				// `a->member` where `a` is a class uses `operator->`.
				if( viaArrow
				 && decl is Structural arrowStruct
				 && ( type.ptrs == null || type.ptrs.Count == 0 )
				 && OperatorResolver.TryGetOperatorReturnType(
				 	type, "operator->", result, out Typespec? arrowReturnType )
				 && arrowReturnType != null ) {
					// Recursive `operator->` is intentionally not supported yet.
					return TryGetBaseHierarchical( arrowReturnType, module, out baseType, viaArrow: false );
				}

				if( decl is Hierarchical h ) {
					baseType = h;
					return true;
				}
			}

			// strip one layer of pointer/reference/smart-pointer for `.`/`->`
			List<Pointer>? ptrs = type.ptrs;
			if( ptrs is { Count: > 0 } ) {
				Pointer outer = ptrs.Last();
				if( outer.kind == Pointer.Kind.RawPtr
				 || outer.kind == Pointer.Kind.LVRef
				 || outer.kind == Pointer.Kind.RVRef
				 || outer.kind.Between( Pointer.Kind.SmartPtr_Begin, Pointer.Kind.SmartPtr_End ) ) {
					Typespec inner = type switch {
						TypespecNested n => new TypespecNested { idTpls = n.idTpls, qual = n.qual, resolvedDecl = n.resolvedDecl, ptrs = new( ptrs ) },
						TypespecBasic b  => new TypespecBasic { kind = b.kind, size = b.size, align = b.align, qual = b.qual, ptrs = new( ptrs ) },
						_                => throw new NotSupportedException( "unsupported typespec in member access" ),
					};
					if( inner.ptrs == null )
						return false;
				inner.ptrs.RemoveAt( inner.ptrs.Count - 1 );
				return TryGetBaseHierarchical( inner, module, out baseType, viaArrow );
				}
			}

			return false;
		}

		// ------------------------------------------------------------------------
		// Owner template-parameter handling for class-template method ranking.
		// ------------------------------------------------------------------------

		/// <summary>
		/// Returns the chain of enclosing class/struct/union declarations, from outermost
		/// to innermost, for a method or constructor candidate.
		/// </summary>
		private static List<Structural> GetOwnerStructuralChain( Decl decl )
		{
			List<Structural> result = new();
			// Start at the parent scope so we do not include the candidate itself.
			for( ScopeLeaf? cur = decl.scope?.parent; cur != null; cur = cur.parent ) {
				if( cur.decl is Structural s )
					result.Add( s );
			}
			result.Reverse();
			return result;
		}

		private (List<TplParam> ownerParams, List<TplArg> ownerArgs) GetOwnerTemplateInfo(
			Decl candidate,
			Expr callee )
		{
			List<Structural> ownerChain = GetOwnerStructuralChain( candidate );
			if( ownerChain.Count == 0 )
				return (new List<TplParam>(), new List<TplArg>());

			List<TplParam> ownerParams = ownerChain.SelectMany( s => s.TplParams ).ToList();
			List<TplArg>? ownerArgs = TryExtractOwnerTemplateArgs( callee, ownerChain );

			// If the receiver type is unavailable or still dependent, do not substitute
			// owner parameters. This keeps the old behaviour for unqualified calls inside
			// class-template methods and for chained member calls whose receiver type has
			// not been materialised yet; when the receiver is concrete, substitution will
			// happen on the next resolver pass.
			if( ownerArgs == null || !AreConcreteTemplateArgs( ownerArgs, ownerParams ) )
				return (new List<TplParam>(), new List<TplArg>());

			return (ownerParams, ownerArgs);
		}

		private static bool AreConcreteTemplateArgs( List<TplArg> args, List<TplParam> ownerParams )
		{
			HashSet<string> ownerNames = new( ownerParams.Select( p => p.name ) );
			return args.All( a => a.typespec != null && !ContainsTemplateParameterName( a.typespec, ownerNames ) );
		}

		private static bool ContainsTemplateParameterName( Typespec type, HashSet<string> names )
		{
			while( true ) {
				if( type is TypespecNested { resolvedDecl: TplParamDecl tpl }
				 && names.Contains( tpl.name ) )
					return true;

				if( type is not TypespecNested nested || nested.idTpls == null )
					return false;

				foreach( IdTplArgs segment in nested.idTpls ) {
					foreach( TplArg arg in segment.tplArgs ) {
						if( arg.typespec != null && ContainsTemplateParameterName( arg.typespec, names ) )
							return true;
					}
				}

				return false;
			}
		}

		private List<TplArg>? TryExtractOwnerTemplateArgs( Expr callee, List<Structural> ownerChain )
		{
			return callee switch {
				BinOp binOp when IsMemberAccessOperator( binOp.op )
					=> TryExtractOwnerArgsFromReceiver( binOp.left, ownerChain ),
				ScopedExpr scoped
					=> TryExtractOwnerArgsFromScopedPrefix( scoped, ownerChain ),
				_ => null,
			};
		}

		private static bool IsMemberAccessOperator( Operand op )
			=> op is Operand.MemberAccess
			    or Operand.NCMemberAccess
			    or Operand.MemberPtrAccess
			    or Operand.MemberAccessPtr
			    or Operand.NCMemberAccessPtr
			    or Operand.MemberPtrAccessPtr;

		private List<TplArg>? TryExtractOwnerArgsFromReceiver(
			Expr             receiverExpr,
			List<Structural> ownerChain )
		{
			Typespec? receiverType = typeResolver.Resolve( receiverExpr );
			if( receiverType == null )
				return null;

			Typespec baseType = StripReferenceAndPointerWrappers( ConversionRules.UnwrapAlias( receiverType, result ) );
			if( baseType is TypespecNested nested ) {
				List<TplArg>? direct = TryExtractOwnerArgsFromTypePath( nested, ownerChain );
				if( direct != null )
					return direct;

				return TryExtractOwnerArgsFromBases( nested, ownerChain );
			}

			return null;
		}

		private List<TplArg>? TryExtractOwnerArgsFromScopedPrefix(
			ScopedExpr       scoped,
			List<Structural> ownerChain )
		{
			TypespecNested prefix = new() {
				idTpls = scoped.idTpls
					.Select( s => new IdTplArgs {
						id      = s.id,
						tplArgs = s.tplArgs
							.Select( a => new TplArg { typespec = a.typespec } )
							.ToList(),
					} )
					.ToList(),
			};

			return TryExtractOwnerArgsFromTypePath( prefix, ownerChain );
		}

		private List<TplArg>? TryExtractOwnerArgsFromTypePath(
			TypespecNested   typePath,
			List<Structural> ownerChain )
		{
			List<TplArg> ownerArgs = new();
			int segIdx = 0;

			foreach( Structural owner in ownerChain ) {
				while( segIdx < typePath.idTpls.Count
				    && !TypeSegmentMatchesStructural( typePath.idTpls[segIdx], owner ) )
					segIdx++;

				if( segIdx >= typePath.idTpls.Count )
					return null;

				IdTplArgs segment = typePath.idTpls[segIdx];
				if( segment.tplArgs.Count != owner.TplParams.Count )
					return null;

				foreach( TplArg arg in segment.tplArgs ) {
					if( arg.typespec is TypespecNested unresolved && unresolved.resolvedDecl == null
					 && this.result.TryGetResolved( unresolved, out Decl? decl ) )
						unresolved.resolvedDecl = decl;

					ownerArgs.Add( arg );
				}

				segIdx++;
			}

			return ownerArgs;
		}

		private static bool TypeSegmentMatchesStructural( IdTplArgs segment, Structural structural )
			=> segment.id == structural.name;

		private List<TplArg>? TryExtractOwnerArgsFromBases(
			TypespecNested   receiverType,
			List<Structural> ownerChain )
		{
			Decl? derivedDecl = receiverType.resolvedDecl;
			if( derivedDecl == null && this.result.TryGetResolved( receiverType, out Decl? d ) )
				derivedDecl = d;
			if( derivedDecl is not Structural derived )
				return null;

			IdTplArgs? derivedSegment = receiverType.idTpls.LastOrDefault();
			if( derivedSegment == null || derived.TplParams.Count != derivedSegment.tplArgs.Count )
				return null;

			List<TplArg> derivedArgs = derivedSegment.tplArgs;

			foreach( BaseType baseType in derived.basetypes ) {
				Typespec concreteBase = TemplateInference.SubstituteTemplateParams(
					baseType.type, derived.TplParams, derivedArgs, this.result );

				if( concreteBase is not TypespecNested baseNested )
					continue;

				Decl? baseDecl = baseNested.resolvedDecl;
				if( baseDecl == null && this.result.TryGetResolved( baseNested, out Decl? bd ) )
					baseDecl = bd;

				if( baseDecl is not Structural baseStruct )
					continue;

				foreach( Structural owner in ownerChain ) {
					if( baseStruct == owner || GetDefiningStructural( baseStruct ) == owner ) {
						IdTplArgs? baseSegment = baseNested.idTpls.LastOrDefault();
						if( baseSegment != null && baseSegment.tplArgs.Count == owner.TplParams.Count )
							return baseSegment.tplArgs;
					}
				}

				List<TplArg>? inherited = TryExtractOwnerArgsFromBases( baseNested, ownerChain );
				if( inherited != null )
					return inherited;
			}

			return null;
		}

		private static Typespec StripReferenceAndPointerWrappers( Typespec type )
		{
			Typespec current = ConversionRules.CloneTypespecBase( type );

			while( current.ptrs is { Count: > 0 } ) {
				Pointer last = current.ptrs[current.ptrs.Count - 1];
				if( last.kind == Pointer.Kind.RawPtr
				 || last.kind == Pointer.Kind.LVRef
				 || last.kind == Pointer.Kind.RVRef
				 || last.kind.Between( Pointer.Kind.SmartPtr_Begin, Pointer.Kind.SmartPtr_End ) ) {
					current.ptrs.RemoveAt( current.ptrs.Count - 1 );
				}
				else {
					break;
				}
			}

			return current;
		}

		private static Typespec? BuildClassValueType(
			Structural              structural,
			IReadOnlyList<IdTplArgs> nameSegments )
		{
			return new TypespecNested {
				resolvedDecl = structural,
				idTpls       = nameSegments
					.Select( s => new IdTplArgs { id = s.id, tplArgs = s.tplArgs } )
					.ToList(),
			};
		}

		private Decl? LookupMember( string name, Structural baseType, GlobalNamespace module )
			=> LookupMember( name, baseType, module, new HashSet<Structural>() );

		private Decl? LookupMember( string name, Structural baseType, GlobalNamespace module, HashSet<Structural> visited )
		{
			if( !visited.Add( baseType ) )
				return null;

			List<Decl> candidates = new();
			AddVisibleChildren( candidates, baseType.scope, name, filterHidden: false );

			if( candidates.Count > 0 )
				return PickSingleCandidate( candidates );

			foreach( BaseType bt in baseType.basetypes ) {
				Decl? baseDecl = bt.type is TypespecNested nested
					? nested.resolvedDecl ?? ( result.TryGetResolved( nested, out Decl? d ) ? d : null )
					: null;

				if( baseDecl is not Structural baseStruct )
					continue;

				Decl? fromBase = LookupMember( name, baseStruct, module, visited );
				if( fromBase != null )
					candidates.Add( fromBase );
			}

			return PickSingleCandidate( candidates );
		}

	private List<Decl> LookupNameCandidates( string name, Scope scope, GlobalNamespace module )
	{
		List<Decl> result = new();

		for( Scope? cur = scope; cur != null; cur = cur.parent ) {
			AddVisibleChildren( result, cur, name, filterHidden: false );

			foreach( Scope imported in cur.importedScopes )
				AddVisibleChildren( result, imported, name, filterHidden: true );

			if( cur.importedNames.TryGetValue( name, out List<Decl>? importedNameList ) )
				result.AddRange( importedNameList );

			// Inside a class/struct, the unqualified class name refers to the
			// class itself (C++ injected-class-name), not just the constructor.
			if( cur.decl is Hierarchical h && h.name == name )
				result.Add( h );

			// Inner scopes shadow outer scopes. Once we have found any visible
			// declaration at this scope level, stop looking further out.
			if( result.Count > 0 )
				break;
		}

		// If no visible declaration was found in the enclosing scopes, consider
		// configured class aliases and names exported by imported modules.
		// This mirrors C++ behavior: inner-scope names hide imported ones.
			if( result.Count == 0 ) {
				if( FindEnclosingStructural( scope ) is Structural structural ) {
					Dialect dialect = GetContext( module ).Dialect;
				if( !String.IsNullOrEmpty( dialect.BaseClassAliasName )
				 && name == dialect.BaseClassAliasName ) {
					Structural? baseClass = GetFirstBaseStructural( structural );
					if( baseClass != null )
						result.Add( baseClass );
				}
				else if( !String.IsNullOrEmpty( dialect.OwnClassAliasName )
				      && name == dialect.OwnClassAliasName ) {
					result.Add( structural );
				}
			}

			List<string> activeNs = GetActiveNamespaceFqns( scope );
			foreach( string importedModule in ImportedModuleNames( module ) ) {
				if( !moduleExports.TryGetValue( importedModule, out ModuleExports? exports ) )
					continue;

				result.AddRange(
					exports.LookupAll( name )
					        .Where( d => IsEnclosingNamespaceActive( d, activeNs ) ) );
			}
		}

		return DistinctCandidates( result );
	}

		private static List<string> GetActiveNamespaceFqns( Scope lookupScope )
		{
			List<string> active = new();
			for( Scope? cur = lookupScope; cur != null; cur = cur.parent ) {
				if( cur.decl is Namespace ns )
					active.Add( ns.FullyQualifiedName );

				foreach( Scope imported in cur.importedScopes ) {
					if( imported.decl is Namespace ins )
						active.Add( ins.FullyQualifiedName );
				}
			}

			return active;
		}

		private static bool IsEnclosingNamespaceActive( Decl decl, List<string> activeNs )
		{
			string? declNs = GetEnclosingNamespaceName( decl );
			if( declNs == null )
				return true;

			foreach( string lookupNs in activeNs ) {
				if( declNs == lookupNs
				 || lookupNs.StartsWith( declNs + "::", StringComparison.Ordinal ) )
					return true;
			}

			return false;
		}

		private static string? GetEnclosingNamespaceName( Decl decl )
		{
			Scope? cur = decl switch {
				Namespace ns => ns.scope.parent,
				_            => decl.scope.parent,
			};

			for( ; cur != null; cur = cur.parent ) {
				if( cur.decl is Namespace ns )
					return ns.FullyQualifiedName;
			}

			return null;
		}

		private List<Decl> LookupInHierarchicalCandidates(
			string      name,
			Hierarchical h,
			GlobalNamespace module )
		{
			List<Decl> result = new();
			AddVisibleChildren( result, h.scope, name, filterHidden: false );

			if( h is Structural structural ) {
				foreach( BaseType bt in structural.basetypes ) {
					Decl? baseDecl = bt.type is TypespecNested nested
						? nested.resolvedDecl ?? ( this.result.TryGetResolved( nested, out Decl? d ) ? d : null )
						: null;

					if( baseDecl is not Structural baseStruct )
						continue;

					result.AddRange( LookupInHierarchicalCandidates( name, baseStruct, module ) );
				}
			}

			if( h is Namespace ns ) {
				string fqn = ns.FullyQualifiedName;
				foreach( string importedModule in ImportedModuleNames( module ) ) {
					if( !moduleExports.TryGetValue( importedModule, out ModuleExports? exports ) )
						continue;

					foreach( Namespace otherNs in exports.LookupNamespaces( fqn ) )
						AddVisibleChildren( result, otherNs.scope, name, filterHidden: true );
				}
			}

			return DistinctCandidates( result );
		}

		private static void AddVisibleChildren(
			List<Decl> result,
			Scope      scope,
			string     name,
			bool       filterHidden )
		{
			if( !scope.children.TryGetValue( name, out List<ScopeLeaf>? leaves ) )
				return;

			foreach( ScopeLeaf leaf in leaves ) {
				if( leaf.decl == null )
					continue;
				// Using declarations are transparent for name lookup; they bring
				// names in via importedScopes / importedNames instead.
				if( leaf.decl is UsingDecl )
					continue;
				if( filterHidden && leaf.decl.IsHidden )
					continue;

				result.Add( leaf.decl );
			}
		}

		private static List<Decl> DistinctCandidates( List<Decl> candidates )
		{
			HashSet<Decl> seen = new( ReferenceEqualityComparer.Instance );
			List<Decl> result = new( candidates.Count );
			foreach( Decl d in candidates ) {
				if( seen.Add( d ) )
					result.Add( d );
			}

			return result;
		}

		private static Decl? PickSingleCandidate( List<Decl> candidates )
		{
			return PickSingleCandidate( candidates, callSite: false );
		}

		private static Decl? PickSingleCandidate( List<Decl> candidates, bool callSite )
		{
			if( candidates.Count == 0 )
				return null;

			if( candidates.Count == 1 )
				return candidates[0];

			// Multiple namespaces with the same fully-qualified name are merged
			// into a single logical namespace for resolution purposes.
			if( candidates.All( d => d is Namespace ) ) {
				string? fqn = null;
				foreach( Decl d in candidates ) {
					string otherFqn = ((Namespace)d).FullyQualifiedName;
					if( fqn == null )
						fqn = otherFqn;
					else if( fqn != otherFqn )
						return null; // ambiguous: different namespaces
				}

				if( fqn != null )
					return candidates[0];
			}

			if( callSite ) {
				List<Decl> callable = candidates
					.Where( d => d is Func or Structor or Hierarchical )
					.ToList();

				// prefer a constructor over its class at a call site
				Decl? ctor = callable.OfType<Structor>().FirstOrDefault();
				if( ctor != null )
					return ctor;

				// a class used as a call site is a constructor invocation
				if( callable.Count == 1 )
					return callable[0];
				if( callable.Count > 1 && callable.All( d => d is Func ) )
					return null; // overload resolution is not implemented yet
			}
			else {
				// At a type/non-call site a type name wins over its constructor.
				List<Decl> typeCandidates = candidates
					.Where( d => d is Hierarchical )
					.ToList();
				if( typeCandidates.Count == 1 )
					return typeCandidates[0];
			}

			return null;
		}

		private void ValidateTypes( IReadOnlyList<CompiledModuleResult> modules )
		{
			foreach( (_, ModuleContext context) in modules ) {
				if( (context.Dialect.DefaultFloat & DefaultTypeMode.Forbidden) != 0 ) {
					foreach( SrcPos srcPos in context.FloatKeywordUsages ) {
						diagnostics.Add( new Diagnostic(
							srcPos,
							DiagnosticKind.Error,
							"The 'float' keyword is disabled by the active dialect" ) );
					}
				}
			}

			var checker = new TypeChecker( result, diagnostics );
			checker.Validate( modules );
		}

		private void RejectRequiresClauses(
			IReadOnlyList<CompiledModuleResult> modules )
		{
			foreach( (GlobalNamespace module, _) in modules ) {
				RejectRequiresInDecl( module );
			}
		}

		private void RejectRequiresInDecl( Decl decl )
		{
			if( decl is Func func && func.Requires.Count > 0 ) {
				diagnostics.Add( new Diagnostic(
					func.srcPos,
					DiagnosticKind.Error,
					"Template constraints ('requires') are not yet supported" ) );
			}

			if( decl is Structural structural && structural.reqs.Count > 0 ) {
				diagnostics.Add( new Diagnostic(
					structural.srcPos,
					DiagnosticKind.Error,
					"Template constraints ('requires') are not yet supported" ) );
			}

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					RejectRequiresInDecl( child );
			}
		}

	private void ResolveSyntheticInitListTypes( IReadOnlyList<CompiledModuleResult> modules )
	{
		bool added;
		do {
			added = false;
			foreach( (GlobalNamespace module, ModuleContext context) in modules ) {
				added |= RegisterSyntheticInitListTypes( module, context );
			}
		} while( added );

		bool progress;
		do {
			progress = false;
			foreach( (GlobalNamespace module, ModuleContext context) in modules ) {
				progress |= ResolveTypes( module, context );
				progress |= ResolveMemberAccesses( module, context );
			}
		} while( progress );
	}

	private bool RegisterSyntheticInitListTypes( GlobalNamespace module, ModuleContext context )
	{
		var collector = new InitListTypeCollector();
		collector.Collect( module );
		foreach( (Decl local, _) in context.LocalDecls )
			collector.Collect( local );

		bool added = false;
		foreach( TypespecNested type in collector.Types ) {
			if( type.resolvedDecl != null )
				continue;
			if( result.Types.ContainsKey( type ) )
				continue;
			if( context.UnresolvedTypes.Any( ut => ReferenceEquals( ut.Node, type ) ) )
				continue;

			context.UnresolvedTypes.Add( new UnresolvedType( type, module.scope ) );
			added = true;
		}

		return added;
	}

	private sealed class InitListTypeCollector
	{
		private readonly HashSet<TypespecNested> collected = new();

		public IEnumerable<TypespecNested> Types => collected;

		public void Collect( Decl decl )
		{
			if( decl == null )
				return;

			switch( decl ) {
				case VarDecl vd:
					CollectType( vd.type );
					break;

				case Func func:
					CollectType( func.retType );
					foreach( Param para in func.paras )
						CollectType( para.type );
					break;

				case Structor stc:
					foreach( Param para in stc.paras )
						CollectType( para.type );
					break;
			}

			if( decl is Structural s ) {
				foreach( BaseType bt in s.basetypes )
					CollectType( bt.type );
			}

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					Collect( child );
			}
		}

		private void CollectType( Typespec? type )
		{
			if( type is not TypespecNested nested )
				return;

			if( nested.idTpls.Count >= 2
			 && nested.idTpls[0].id == "std"
			 && nested.idTpls[1].id == "initializer_list" )
				collected.Add( nested );

			foreach( IdTplArgs segment in nested.idTpls ) {
				foreach( TplArg arg in segment.tplArgs ) {
					if( arg.typespec != null )
						CollectType( arg.typespec );
				}
			}
		}
	}

		private void ReportUnresolved( IReadOnlyList<CompiledModuleResult> modules )
	{
			foreach( (GlobalNamespace module, ModuleContext context) in modules ) {
				HashSet<Expr> callCallees = context.UnresolvedCalls
					.Select( c => c.Callee )
					.ToHashSet();

			foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
				if( result.IsResolvedOrInvalid( unresolved.Node ) )
					continue;

				IdTplArgs[] segments = new[] { unresolved.Node.idTplArgs };
				bool isCallCallee = callCallees.Contains( unresolved.Node );
				ReportUnresolvedPath( "identifier", unresolved.Node.srcPos, segments, module, unresolved.Scope, isCallCallee );
			}

			foreach( UnresolvedScoped unresolved in context.UnresolvedScopeds ) {
				if( result.IsResolvedOrInvalid( unresolved.Node ) )
					continue;

				bool isCallCallee = callCallees.Contains( unresolved.Node );
				ReportUnresolvedPath( "identifier", unresolved.Node.srcPos, unresolved.Node.idTpls, module, unresolved.Scope, isCallCallee );
			}

			foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
				if( result.IsResolvedOrInvalid( unresolved.Node ) )
					continue;

				if( unresolved.Node.IsDependentType() )
					continue;

				ReportUnresolvedPath( "type", unresolved.Node.srcPos, unresolved.Node.idTpls, module, unresolved.Scope );
			}

			foreach( UnresolvedMemberAccess unresolved in context.UnresolvedMemberAccesses ) {
				if( unresolved.Node.right is not IdExpr member )
					continue;

				if( result.Members.ContainsKey( member ) )
					continue;

					string? typeName = null;
					if( TryGetMemberAccessBaseType(
							unresolved.Node.left,
							unresolved.Scope,
							module,
							out Hierarchical? baseType,
							viaArrow: IsArrowMemberAccess( unresolved.Node.op ) ) )
						typeName = baseType!.FullyQualifiedName;

					diagnostics.Add( new Diagnostic(
						member.srcPos,
						DiagnosticKind.Error,
						String.Format(
							typeName != null
								? "Unresolved member '{0}' in '{1}'"
								: "Unresolved member '{0}'",
							member.idTplArgs.id,
							typeName ) ) );
				}
		}
	}

		private void ReportUnresolvedPath(
			string                   kind,
			SrcPos                   srcPos,
			IReadOnlyList<IdTplArgs> segments,
			GlobalNamespace          module,
			Scope                    scope,
			bool                     isCallCallee = false )
		{
			ResolvePath( segments, scope, module, out int unresolvedSegmentIndex, callSite: false );
			if( unresolvedSegmentIndex < 0 )
				unresolvedSegmentIndex = 0;

			string unresolvedName = segments[unresolvedSegmentIndex].id;
			List<Decl> candidates;
			if( unresolvedSegmentIndex == 0 ) {
				candidates = LookupNameCandidates( unresolvedName, scope, module );
			}
			else {
				Decl? prefixCandidate = PickSingleCandidate( LookupNameCandidates( segments[0].id, scope, module ), callSite: false );
				candidates = prefixCandidate is Hierarchical h
					? LookupInHierarchicalCandidates( unresolvedName, h, module )
					: new List<Decl>();
			}

			if( candidates.Count > 1 && PickSingleCandidate( candidates, callSite: false ) == null ) {
				// If the identifier is a call callee and all candidates are callable
				// overloads, leave the ambiguity to call-level overload resolution.
				if( kind == "identifier"
				 && isCallCallee
				 && candidates.All( d => d is Func or Structor ) )
					return;

				ReportAmbiguous( kind, srcPos, unresolvedName, candidates );
				return;
			}

			string message;
			if( unresolvedSegmentIndex == 0 ) {
				message = String.Format( "Unresolved {0} '{1}'", kind, unresolvedName );
			}
			else {
				string resolvedPrefix = segments
					.Take( unresolvedSegmentIndex )
					.Select( s => s.id )
					.Join( "::" );
				message = String.Format(
					"Unresolved {0} '{1}' in '{2}'",
					kind,
					unresolvedName,
					resolvedPrefix );
			}

			diagnostics.Add( new Diagnostic( srcPos, DiagnosticKind.Error, message ) );
		}

		private void ReportAmbiguous(
			string       kind,
			SrcPos       srcPos,
			string       name,
			IReadOnlyList<Decl> candidates )
		{
			string locations = candidates
				.Select( d => String.Format( "{0} ({1})", d.name, d.GetType().Name ) )
				.Join( ", " );

			diagnostics.Add( new Diagnostic(
				srcPos,
				DiagnosticKind.Error,
				String.Format( "Ambiguous {0} '{1}': {2}", kind, name, locations ) ) );
		}

		private sealed class ModuleExports
		{
			private readonly Dictionary<string, List<Decl>> exports = new();
			private readonly Dictionary<string, List<Namespace>> namespacesByFqn = new();

			public void Add( string name, Decl decl )
			{
				if( !exports.TryGetValue( name, out List<Decl>? list ) ) {
					list = new List<Decl>( 1 );
					exports.Add( name, list );
				}

				list.Add( decl );

				if( decl is Namespace ns ) {
					string fqn = ns.FullyQualifiedName;
					if( !namespacesByFqn.TryGetValue( fqn, out List<Namespace>? nsList ) ) {
						nsList = new List<Namespace>( 1 );
						namespacesByFqn.Add( fqn, nsList );
					}

					nsList.Add( ns );
				}
			}

			public void CopyTo( ModuleExports other )
			{
				foreach( (string name, List<Decl> list) in exports ) {
					foreach( Decl decl in list ) {
						other.Add( name, decl );
					}
				}
			}

			public Decl? Lookup( string name )
			{
				IReadOnlyList<Decl> all = LookupAll( name );
				return all.Count == 1 ? all[0] : null;
			}

			public IReadOnlyList<Decl> LookupAll( string name )
			{
				if( !exports.TryGetValue( name, out List<Decl>? list ) )
					return Array.Empty<Decl>();

				List<Decl> visible = list.Where( d => !d.IsHidden ).ToList();
				return visible;
			}

			public IReadOnlyList<Namespace> LookupNamespaces( string fqn )
			{
				if( !namespacesByFqn.TryGetValue( fqn, out List<Namespace>? list ) )
					return Array.Empty<Namespace>();

				return list.Where( ns => !ns.IsHidden ).ToList();
			}
		}
	}
}
