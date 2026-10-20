using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
{
	/// <summary>
	/// Validates template specializations (variable, class/struct/union, function)
	/// and links them to their primary: the primary must live in the same module
	/// and scope and precede the specialization, or in a module directly imported
	/// by the specialization's module (then include order puts the primary first).
	/// Bindings must name primary parameters in declaration order, and a
	/// specialization's bound-signature may only be defined once per module.
	///
	/// Variable specializations must repeat the primary's substituted type; for
	/// partial specializations the unbound parameters become the specialization's
	/// own template parameters, injected into its scope so initializers can
	/// reference them.
	/// Class/struct/union specializations follow the same binding rules; their
	/// bodies are their own member sets (C++ semantics), with unbound parameters
	/// injected into the class scope and bound parameters not in scope.
	/// Function specializations are full-only (C++ has no partials), must repeat
	/// the primary's substituted parameters and return type, and keep bound
	/// parameter names usable in signature and body through bound-parameter
	/// aliases.
	/// </summary>
	public sealed class SpecTransformer : Transformer
	{
		private readonly IReadOnlyList<CompiledModuleResult> allModules;

		private ModuleContext   context  = null!;
		private GlobalNamespace moduleAst = null!;
		private bool            isDeclModule;

		public SpecTransformer( IReadOnlyList<CompiledModuleResult> allModules )
		{
			this.allModules = allModules;
		}

		protected override bool RunInPrototypeFiles => true;

		protected override void PreTransform( TransformContext ctx )
		{
			// Prototype (.decl.myll) modules run too: a specialization declared
			// there says "the real header already specializes this" — it feeds
			// resolution and the duplicate guard, and emits nothing.
			context      = ctx.Module.Context;
			moduleAst    = ctx.Module.Module;
			isDeclModule = ctx.Module.Context.IsPrototypeFile;
			ValidateScope( moduleAst );
		}

		private void ValidateScope( Myll.Core.Hierarchical hier )
		{
			List<Decl> children = hier.children.ToList();
			string? nsFqn = NamespaceFqnOf( hier );
			Myll.Core.Structural? owner = hier is Myll.Core.Structural s ? s : null;

			FoldSpecBindings( children );

			// Every declaration that consumes [spec=...] gets its values validated:
			// structurals (kind), functions and variables (shape markers/primary).
			// Unknown values are rejected there — typo protection where the
			// attribute means something; other declarations ignore it.
			List<Myll.Core.Structural> structs = children.OfType<Myll.Core.Structural>().ToList();
			foreach( Myll.Core.Structural childStruct in structs ) {
				SpecAttribValues? values = ParseSpecAttrib( childStruct, "class", "" );
				if( values != null )
					ValidateSpecAttribUsage( childStruct, values,
						childStruct.TplParams.Count > 0, childStruct.TplSpec.Count > 0, "class" );
			}

			foreach( Func func in children.OfType<Func>() ) {
				if( func.kind == Func.Kind.Convert )
					continue; // converts do not consume the attribute
				SpecAttribValues? values = ParseSpecAttrib( func, "func", "" );
				if( values != null )
					ValidateSpecAttribUsage( func, values,
						func.TplParams.Count > 0, func.TplSpec.Count > 0, "func" );
			}

			foreach( VarDecl var in children.OfType<VarDecl>() ) {
				SpecAttribValues? values = ParseSpecAttrib( var, "var", "" );
				if( values != null )
					ValidateSpecAttribUsage( var, values,
						var.TplParams.Count > 0, var.TplSpec.Count > 0, "var" );
			}

			ValidateSpecsInScope( children.OfType<VarDecl>().ToList(), owner, nsFqn );
			ValidateStructSpecs( structs, owner, nsFqn );
			ValidateFuncSpecs( children.OfType<Func>().ToList(), owner, nsFqn );
			if( hier is Myll.Core.Structural structHier )
				ValidateCtorSpecs( children.OfType<Structor>().ToList(), structHier );

			foreach( AliasDecl alias in children.OfType<AliasDecl>() ) {
				if( alias.TplSpec.Count == 0 )
					continue;
				// C++ has no spelling for specializing an alias template; the
				// C++ way is to specialize the underlying template instead.
				Diagnostics.Add( new Diagnostic(
					alias.srcPos,
					DiagnosticKind.Error,
					String.Format( "Alias templates cannot be specialized; specialize the underlying template instead ('{0}')",
						alias.name ) ) );
				alias.SpecInfo = new Decl.SpecData { Primary = alias };
			}

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

		private void ValidateSpecsInScope( List<VarDecl> vars, Myll.Core.Structural? owner, string? nsFqn )
		{
			if( owner != null ) {
				ValidateMemberVarSpecs( vars, owner );
				return;
			}

			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<Decl, List<string>> seenSignatures = new();

			foreach( VarDecl spec in vars ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				(VarDecl? primary, bool isImported) = FindPrimary( vars, spec,
					v => v.name == spec.name && v.SpecInfo == null && v.TplParams.Count > 0,
					IsVarPrimary, nsFqn,
					String.Format( "No matching variable template '{0}' in this module or its imports; a specialization must live in the primary's module or import it",
						spec.name ) );
				if( primary == null ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ValidateFollowOrder( spec, primary, isImported, String.Format( "Specialization of '{0}' must follow its primary template",
					spec.name ) ) ) {
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

				// Bindings must be concrete or patterns (same rule as class specs).
				if( !ClassifySpecBindings( spec, primary.TplParams, out List<TplBinding> patternBindings ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Pattern binding types name template parameters, which only
				// exist inside the specialization's scope; the visitor
				// registered them at the outer scope. They are fully handled
				// by the pattern machinery and need no name resolution.
				RemovePatternTypeRegistrations( patternBindings );

				// The asserted shape ([spec=full] / [spec=partial]) must match
				// the derived binding shape.
				if( !ValidateAssertedSpecShape( spec, patternBindings ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Substitute the bound values into the declared type, then compare
				// with the primary's substituted type. The original symbolic type was
				// registered as unresolved at visit time; the substituted type needs
				// no resolution of its own.
				Dictionary<string, Typespec> boundValues = BoundValuesOf( spec.TplSpec, patternBindings );

				Typespec specType = SubstituteBindings( spec.type, boundValues );
				Typespec primaryType = SubstituteBindings( primary.type, boundValues );

				context.UnresolvedTypes.RemoveAll( ut => ReferenceEquals( ut.Node, spec.type ) );

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
				if( !TryAddSpecSignature( primary, spec.name, spec.TplSpec, spec.srcPos, seenSignatures ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Fill the spec info: every primary parameter in order, bound or symbolic.
				// Unbound parameters become the specialization's own template
				// parameters, injected into its scope so initializers can use them.
				List<TplArg> args = BuildSpecArgs( primary.TplParams, spec.TplSpec, out bool[] bound, out List<TplParam> unboundParams, out bool[] pattern );

				spec.SpecInfo = new Decl.SpecData {
					Primary = primary,
					Args    = args,
					Bound   = bound,
					Pattern = pattern,
				};
				spec.type = specType;
				spec.TplParams = unboundParams;
				InjectParams( spec.varScope, unboundParams, spec.srcPos );

				// Bound parameter names stay usable in the initializer through
				// the same aliases as functions: (T)3 resolves T to the bound
				// value, emission expands it.
				InjectBoundAliases( spec.varScope, new List<Param>(), spec.srcPos, boundValues );
			}
		}

		/// <summary>
		/// The shared primary search: same-module primaries from the predicate,
		/// then the imported fallback (module-scope kinds only). Emits the
		/// no-primary diagnostic itself; the caller marks the spec invalid.
		/// </summary>
		private (TSpec? Primary, bool IsImported) FindPrimary<TSpec>(
			List<TSpec>            candidates,
			TSpec                  spec,
			Func<TSpec, bool>      isLocalPrimary,
			Func<Decl, bool>?      importedFilter,
			string?                nsFqn,
			string                 noPrimaryMessage )
			where TSpec : Decl
		{
			List<TSpec> primaries = candidates.Where( isLocalPrimary ).ToList();

			TSpec? primary = primaries.Count > 0
				? primaries[0]
				: ( importedFilter != null && nsFqn != null
					? FindImportedPrimary( spec.name, nsFqn, importedFilter ) as TSpec
					: null );

			if( primary == null )
				Diagnostics.Add( new Diagnostic( spec.srcPos, DiagnosticKind.Error, noPrimaryMessage ) );

			// An imported primary needs no follow check: the include of the
			// primary's module precedes every declaration of this one.
			return (primary, primaries.Count == 0);
		}

		/// <summary>
		/// Within one module the specialization must follow the primary by
		/// source position, not processing order, so multi-file modules stay
		/// order-independent.
		/// </summary>
		private bool ValidateFollowOrder( Decl spec, Decl primary, bool isImported, string message )
		{
			if( isImported || IsLaterPos( spec, primary ) )
				return true;

			Diagnostics.Add( new Diagnostic( spec.srcPos, DiagnosticKind.Error, message ) );
			return false;
		}

		// The bound values of a specialization's bindings, keyed by parameter
		// name; unbound placeholders are skipped.
		private static Dictionary<string, Typespec> BoundValuesOf( List<TplBinding> tplSpec )
		{
			Dictionary<string, Typespec> boundValues = new( StringComparer.Ordinal );
			foreach( TplBinding binding in tplSpec ) {
				if( binding.type != null )
					boundValues[ binding.name ] = binding.type;
			}
			return boundValues;
		}

		// Bound values without pattern bindings: a pattern position's value is
		// the written pattern, not a concrete type, so it gets no alias.
		private static Dictionary<string, Typespec> BoundValuesOf( List<TplBinding> tplSpec, List<TplBinding> patternBindings )
		{
			Dictionary<string, Typespec> boundValues = BoundValuesOf( tplSpec );
			foreach( TplBinding pattern in patternBindings )
				boundValues.Remove( pattern.name );
			return boundValues;
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
		/// <summary>
		/// Folds literal integer arithmetic in non-type bindings into their
		/// canonical value (N=2*8 becomes N=16), so binding signatures, matching,
		/// and emission compare and emit plain constants. Mirrors the
		/// untyped-integer folding rules: division by zero and out-of-range
		/// shifts stay unfolded for the C++ compiler to reject.
		/// </summary>
		private static void FoldSpecBindings( List<Decl> children )
		{
			foreach( Decl decl in children ) {
				List<TplBinding>? bindings = decl switch {
					VarDecl v => v.TplSpec,
					Myll.Core.Structural s => s.TplSpec,
					Func f => f.TplSpec,
					Structor c => c.TplSpec,
					AliasDecl a => a.TplSpec,
					_ => null,
				};
				if( bindings == null )
					continue;

				foreach( TplBinding binding in bindings ) {
					if( binding.lit == null )
						continue;

					if( TemplateArgFolding.TryFoldLiteral( binding.lit, out BigInteger value ) )
						binding.lit = new Literal { text = value.ToString() };
				}
			}
		}

		private enum SpecAttribKind { None, Fresh, Member }

		// Normalized [spec=...] values: `all`/`class` name the fresh-body kind,
		// `some`/`member` the member-block kind, `full`/`explicit` and
		// `partial`/`part` the binding shapes (assertable), `primary`/`prime`
		// the primary-template marker. The list spelling `[spec=(all, full)]`
		// combines a kind with a shape; the `|` spelling is deferred to the
		// attr rework.
		private sealed class SpecAttribValues
		{
			public SpecAttribKind Kind;
			public bool? IsFull;      // shape: true = full, false = partial, null = not asserted
			public bool IsPrimary;    // the primary marker

			public bool HasKind  => Kind != SpecAttribKind.None;
			public bool HasShape => IsFull != null;
		}

		private SpecAttribValues? ParseSpecAttrib( Decl decl, string kindWord, string what )
		{
			if( !decl.HasAttrib( "spec" ) )
				return null;

			IReadOnlyList<string> values = decl.AttribValues( "spec" );
			SpecAttribValues parsed = new();
			bool anyUnknown = false;

			foreach( string raw in values ) {
				string value = raw == "class" ? "all"
					: raw == "member" ? "some"
					: raw == "explicit" ? "full"
					: raw == "part" ? "partial"
					: raw == "prime" ? "primary"
					: raw;

				bool known = true;
				if( value == "all" || value == "some" ) {
					SpecAttribKind kind = value == "all" ? SpecAttribKind.Fresh : SpecAttribKind.Member;
					if( parsed.HasKind ) {
						Diagnostics.Add( new Diagnostic(
							decl.srcPos, DiagnosticKind.Error,
							String.Format( "The [spec=...] attribute of '{0}' names both '{1}' and '{2}'; pick one",
								decl.name, parsed.Kind == SpecAttribKind.Fresh ? "all" : "some", value ) ) );
						known = false;
					}
					else
						parsed.Kind = kind;
				}
				else if( value == "full" || value == "partial" ) {
					bool isFull = value == "full";
					if( parsed.HasShape ) {
						Diagnostics.Add( new Diagnostic(
							decl.srcPos, DiagnosticKind.Error,
							String.Format( "The [spec=...] attribute of '{0}' asserts both '{1}' and '{2}'; pick one",
								decl.name, parsed.IsFull == true ? "full" : "partial", value ) ) );
						known = false;
					}
					else
						parsed.IsFull = isFull;
				}
				else if( value == "primary" ) {
					if( parsed.HasKind || parsed.HasShape ) {
						Diagnostics.Add( new Diagnostic(
							decl.srcPos, DiagnosticKind.Error,
							String.Format( "The [spec=primary] marker of '{0}' cannot combine with kind or shape values", decl.name ) ) );
						known = false;
					}
					else
						parsed.IsPrimary = true;
				}
				else {
					Diagnostics.Add( new Diagnostic(
						decl.srcPos, DiagnosticKind.Error,
						String.Format( "Unknown [spec={0}] on '{1}'; the known values are 'all', 'some', 'full', 'partial' and 'primary' (with aliases 'class', 'member', 'explicit', 'part', 'prime')",
							raw, decl.name ) ) );
					known = false;
				}

				anyUnknown |= !known;
			}

			if( anyUnknown )
				return null;
			return parsed;
		}

		// Validates the parsed [spec=...] values against the declaration kind.
		// Returns false when the combination is invalid (diagnostics fired).
		private bool ValidateSpecAttribUsage( Decl decl, SpecAttribValues values, bool isTemplate, bool isSpec, string kindWord )
		{
			// Primary marker: only on a template primary.
			if( values.IsPrimary ) {
				if( isSpec ) {
					Diagnostics.Add( new Diagnostic(
						decl.srcPos, DiagnosticKind.Error,
						String.Format( "The [spec=primary] marker of '{0}' marks the primary template, not a specialization", decl.name ) ) );
					return false;
				}
				if( !isTemplate ) {
					Diagnostics.Add( new Diagnostic(
						decl.srcPos, DiagnosticKind.Error,
						String.Format( "The [spec=primary] marker of '{0}' requires a template", decl.name ) ) );
					return false;
				}
				return true;
			}

			if( isSpec ) {
				// Specialization: func/var specs may assert their shape
				// (funcs only full, vars also partial); class specs take kinds.
				if( kindWord == "func" || kindWord == "var" ) {
					if( !values.HasShape || values.HasKind ) {
						Diagnostics.Add( new Diagnostic(
							decl.srcPos, DiagnosticKind.Error,
							String.Format( "The [spec=...] attribute of this {0} specialization must be '[spec=full]'{1}; the all/some kinds apply to class/struct/union specializations",
								kindWord, kindWord == "var" ? " or '[spec=partial]'" : "" ) ) );
						return false;
					}
					if( kindWord == "func" && values.IsFull == false ) {
						Diagnostics.Add( new Diagnostic(
							decl.srcPos, DiagnosticKind.Error,
							"Function specializations are always full; partial (open or pattern bindings) has no function form" ) );
						return false;
					}
					return true;
				}

				// Class spec: a kind is required; a bare shape must combine with one.
				if( !values.HasKind ) {
					Diagnostics.Add( new Diagnostic(
						decl.srcPos, DiagnosticKind.Error,
						String.Format( "The '{0}' specialization of '{1}' must name its kind: [spec=all]{2}",
							kindWord == "var" ? "var" : "class", decl.name,
							values.HasShape ? " and combine the shape: [spec(all, " + ( values.IsFull == true ? "full" : "partial" ) + ")]" : "" ) ) );
					return false;
				}
				return true;
			}

			// Primary: no kind/shape values apply.
			Diagnostics.Add( new Diagnostic(
				decl.srcPos, DiagnosticKind.Error,
				String.Format( "The [spec=...] values of '{0}' apply only to specializations; the primary takes [spec=primary] or nothing",
					decl.name ) ) );
			return false;
		}

		// Reads the [spec=...] attribute of a class/struct/union: [spec=all]
		// declares a specialization with a fresh body, [spec=some] a member
		// specialization block. Unknown values are rejected on every
		// structural, and the attribute only fits a specialization — a
		// primary or plain class carrying it is rejected too.
		private SpecAttribKind ReadSpecAttrib( Myll.Core.Structural s )
		{
			SpecAttribValues? values = ParseSpecAttrib( s, "class", "" );
			if( values == null )
				return SpecAttribKind.None;

			if( values.IsPrimary || ( values.HasShape && !values.HasKind ) )
				return SpecAttribKind.None;

			return values.Kind;
		}

		// Pattern binding types are fully handled by the pattern machinery (shape
		// checks, unification, emission read them structurally); drop their
		// visitor-time unresolved registrations so they do not error at the
		// outer scope where the named parameters do not exist.
		private void RemovePatternTypeRegistrations( List<TplBinding> patternBindings )
		{
			foreach( TplBinding pattern in patternBindings )
				context.UnresolvedTypes.RemoveAll( ut => ReferenceEquals( ut.Node, pattern.type ) );
		}

		private void ValidateStructSpecs( List<Myll.Core.Structural> structs, Myll.Core.Structural? owner, string? nsFqn )
		{
			if( owner != null ) {
				ValidateNestedStructSpecs( structs, owner );
				return;
			}

			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<Decl, List<string>> seenSignatures = new();

			foreach( Myll.Core.Structural spec in structs ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				// A specialization's kind must be declared: fresh body vs member
				// block. Unknown values were already reported while scanning the
				// scope.
				if( !spec.IsAttrib( "spec", "all" ) ) {
					if( spec.IsAttrib( "spec", "some" ) ) {
						ValidateMemberSpecBlock( spec, structs, nsFqn );
						continue;
					}

					if( !spec.HasAttrib( "spec" ) )
						Diagnostics.Add( new Diagnostic(
							spec.srcPos,
							DiagnosticKind.Error,
							String.Format( "Specialization of '{0}' must declare its kind: [spec=all] for a fresh body or [spec=some] for a member specialization block",
								spec.name ) ) );

					MarkInvalidSpec( spec );
					continue;
				}

				(Myll.Core.Structural? primary, bool isImported) = FindPrimary( structs, spec,
					s => s.name == spec.name && s.SpecInfo == null && s.TplParams.Count > 0,
					IsStructPrimary, nsFqn,
					String.Format( "No matching template '{0}' in this module or its imports; a class/struct/union specialization must live in the primary's module or import it",
						spec.name ) );
				if( primary == null ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ValidateFollowOrder( spec, primary, isImported, String.Format( "Specialization of '{0}' must follow its primary template",
					spec.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( spec.IsForwardDeclaration && !isDeclModule ) {
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

				// Bindings must be concrete or patterns: a value naming one of the
				// primary's parameters is only legal as a pattern (T=T*), which
				// deduces the parameter per use; anything else (T=U bare,
				// nested, array or qualified patterns) is rejected.
				if( !ClassifySpecBindings( spec, primary.TplParams, out List<TplBinding> patternBindings ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Pattern binding types name template parameters, which only
				// exist inside the specialization's scope; the visitor
				// registered them at the outer scope. They are fully handled
				// by the pattern machinery and need no name resolution.
				RemovePatternTypeRegistrations( patternBindings );

				// The declared shape ([spec=(all, full)] / (all, partial))
				// must match the derived binding shape.
				if( !ValidateAssertedSpecShape( spec, patternBindings ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Duplicate specialization signature check.
				if( !TryAddSpecSignature( primary, spec.name, spec.TplSpec, spec.srcPos, seenSignatures ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Unbound parameters become the specialization's own template
				// parameters, injected into its class scope like
				// TemplateParamTransformer does for primaries. Pattern-bound
				// positions stay open the same way: their parameter is the
				// deduced one.
				List<TplArg> args = BuildSpecArgs( primary.TplParams, spec.TplSpec, out bool[] bound, out List<TplParam> unboundParams, out bool[] pattern );

				spec.SpecInfo = new Decl.SpecData {
					Primary = primary,
					Args    = args,
					Bound   = bound,
					Pattern = pattern,
				};
				spec.TplParams = unboundParams;
				InjectParams( spec.scope, unboundParams, spec.srcPos );

				// Bound parameter names stay usable in member signatures and
				// bodies: field T _v; or func get() -> T inside class Box<T=int>
				// resolves T to the bound value, and emission expands it, so the
				// spelling concrete types stays optional. Pattern positions get
				// no alias: their parameter is the deduced one.
				InjectBoundAliases( spec.scope, new List<Param>(), spec.srcPos, BoundValuesOf( spec.TplSpec, patternBindings ) );

				// A fresh body cannot coexist with a member specialization block
				// of the same class+arguments: the class would be both a full
				// specialization and an implicit instantiation.
				Myll.Core.Structural? memberHead = FindVisibleSpecOfKind( primary, args, wantMemberHead: true, nsFqn, structs );
				if( memberHead != null ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' conflicts with the member specialization block of the same arguments",
							spec.name ) ) );
					Diagnostics.Add( new Diagnostic(
						memberHead.srcPos,
						DiagnosticKind.Note,
						String.Format( "The [spec=some] block of '{0}' is defined here.", memberHead.name ) ) );
					MarkInvalidSpec( spec );
				}
			}
		}

		/// <summary>
		/// Validates a [spec=some] block: the head names one concrete
		/// instantiation of an imported (or same-module) class template, and
		/// the body members specialize members of that instantiation. The head
		/// itself is transparent to name lookup and emits no class — uses of
		/// the class keep resolving to the primary, which stays implicitly
		/// instantiated; each member validates and emits like an in-class
		/// member specialization, with the head's class arguments standing in
		/// for the class-parameter bindings.
		/// </summary>
		private void ValidateMemberSpecBlock( Myll.Core.Structural head, List<Myll.Core.Structural> structs, string? nsFqn )
		{
			(Myll.Core.Structural? primary, bool isImported) = FindPrimary( structs, head,
				s => s.name == head.name && s.SpecInfo == null && s.TplParams.Count > 0,
				IsStructPrimary, nsFqn,
				String.Format( "No matching template '{0}' in this module or its imports; a member specialization block must live in the primary's module or import it",
					head.name ) );
			if( primary == null ) {
				MarkInvalidSpec( head );
				return;
			}

			if( !ValidateFollowOrder( head, primary, isImported, String.Format( "Member specialization of '{0}' must follow its primary template",
				head.name ) ) ) {
				MarkInvalidSpec( head );
				return;
			}

			if( head.IsForwardDeclaration && !isDeclModule ) {
				Diagnostics.Add( new Diagnostic(
					head.srcPos,
					DiagnosticKind.Error,
					String.Format( "Member specialization block of '{0}' must be a full definition",
						head.name ) ) );
				MarkInvalidSpec( head );
				return;
			}

			if( !ValidateBindingShape( head.name, head.TplSpec, primary.TplParams, head.srcPos ) ) {
				MarkInvalidSpec( head );
				return;
			}

			// Members replace members of one concrete instantiation: the head
			// must bind every class parameter to a concrete type (C++ requires
			// concrete enclosing arguments for member specializations).
			if( !ClassParamsBound( head, primary, String.Format( "Member specialization head of '{0}' must bind all class template parameters of '{1}'",
					head.name, primary.name ) ) ) {
				MarkInvalidSpec( head );
				return;
			}

			if( !HasConcreteBindings( head.TplSpec, primary.TplParams, head.name, head.srcPos ) ) {
				MarkInvalidSpec( head );
				return;
			}

			List<TplArg> classArgs = BuildSpecArgs( primary.TplParams, head.TplSpec, out bool[] classBound, out List<TplParam> classUnbound );

			// A fresh body cannot coexist with a member specialization block of
			// the same class+arguments.
			Myll.Core.Structural? freshSpec = FindVisibleSpecOfKind( primary, classArgs, wantMemberHead: false, nsFqn, structs );
			if( freshSpec != null ) {
				Diagnostics.Add( new Diagnostic(
					head.srcPos,
					DiagnosticKind.Error,
					String.Format( "Member specialization block of '{0}' conflicts with the full specialization of the same arguments",
						head.name ) ) );
				Diagnostics.Add( new Diagnostic(
					freshSpec.srcPos,
					DiagnosticKind.Note,
					String.Format( "The [spec=all] specialization of '{0}' is defined here.", freshSpec.name ) ) );
				MarkInvalidSpec( head );
				return;
			}

			// The member emission splits its arguments at the class parameter
			// count, so the head keeps the primary's parameter list.
			head.SpecInfo = new Decl.SpecData {
				Primary        = primary,
				Args           = classArgs,
				Bound          = classBound,
				MemberSpecHead = true,
			};
			head.TplParams = primary.TplParams;

			// Unqualified names inside member bodies fall back to the
			// primary's members, like base-class lookup; the specialized
			// members themselves come from the block.
			if( !head.scope.importedScopes.Contains( primary.scope ) )
				head.scope.importedScopes.Add( primary.scope );

			// Bound class parameters stay usable in member signatures and
			// bodies, like in a fresh specialization.
			InjectBoundAliases( head.scope, new List<Param>(), head.srcPos, BoundValuesOf( head.TplSpec ) );

			Dictionary<Decl, List<string>> seenSignatures = new();

			foreach( Decl member in head.children ) {
				switch( member ) {
					case Func method:
						ValidateBlockMethodSpec( method, head, primary, classArgs, seenSignatures );
						break;
					case Structor ctor:
						ValidateBlockCtorSpec( ctor, head, primary, classArgs, seenSignatures );
						break;
					case VarDecl field:
						ValidateBlockVarSpec( field, head, primary, classArgs, seenSignatures );
						break;
					case Myll.Core.Structural nested:
						ValidateBlockNestedStructSpec( nested, head, primary, classArgs, seenSignatures );
						break;
					default:
						Diagnostics.Add( new Diagnostic(
							member.srcPos,
							DiagnosticKind.Error,
							String.Format( "Only functions, constructors, [static] fields and nested classes can be specialized in the block of '{0}'",
								head.name ) ) );
						break;
				}
			}
		}

		// A method member of a [spec=some] block: bindings address only the
		// method's own template parameters (the class arguments come from the
		// head). Plain methods specialize the concrete instantiation's method
		// directly; method templates may stay partially open.
		private void ValidateBlockMethodSpec( Func spec, Myll.Core.Structural head, Myll.Core.Structural primary, List<TplArg> classArgs, Dictionary<Decl, List<string>> seenSignatures )
		{
			if( spec.body == null && !isDeclModule ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Member specialization of '{0}' must be a full definition",
						spec.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			if( !RejectBlockMemberIssues( spec, primary ) )
				return;

			List<Func> candidates = primary.children
				.OfType<Func>()
				.Where( f => f.name == spec.name && f.SpecInfo == null )
				.ToList();

			if( candidates.Count == 0 ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Class '{0}' has no method '{1}'; a member specialization block cannot add members",
						primary.name, spec.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			Dictionary<string, Typespec> boundValues = BlockBoundValuesOf( head, spec );
			Func? memberPrimary = null;
			Func? coverageMatch = null;

			foreach( Func candidate in candidates ) {
				if( !BlockSpecCoversParams( spec, candidate ) )
					continue;
				coverageMatch ??= candidate;
				if( SignatureMatchesParas( spec, candidate, boundValues ) ) {
					memberPrimary = candidate;
					break;
				}
			}

			if( memberPrimary == null ) {
				if( coverageMatch == null )
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "No matching method template '{0}' in '{1}'; a specialization must name its own template parameters",
							spec.name, primary.name ) ) );
				else if( IsAutoType( spec.retType ) || IsAutoType( coverageMatch.retType ) )
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Member specialization of '{0}' must repeat the primary's return type; inferred return cannot specialize",
							spec.name ) ) );
				else
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Member specialization of '{0}' must repeat the primary's substituted parameter and return types",
							spec.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			if( !RejectVariadicPrimary( memberPrimary, spec, "method" ) )
				return;

			FinishBlockMemberSpec( spec, memberPrimary, head, classArgs, seenSignatures );
		}

		// A constructor member of a [spec=some] block: like methods, bindings
		// address only the constructor's own template parameters. Copy and
		// move constructor specializations are rejected: their parameters are
		// generated per instantiation, so a specialization cannot repeat them
		// portably.
		private void ValidateBlockCtorSpec( Structor spec, Myll.Core.Structural head, Myll.Core.Structural primary, List<TplArg> classArgs, Dictionary<Decl, List<string>> seenSignatures )
		{
			if( spec.body == null && !isDeclModule ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Member specialization of the constructor of '{0}' must be a full definition",
						primary.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			if( IsCopyOrMoveBlockCtor( spec, head ) ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					"Copy and move constructor specializations are not supported in member specialization blocks" ) );
				MarkInvalidSpec( spec );
				return;
			}

			if( !RejectBlockMemberIssues( spec, primary ) )
				return;

			List<Structor> candidates = primary.children
				.OfType<Structor>()
				.Where( c => c.SpecInfo == null )
				.ToList();

			Dictionary<string, Typespec> boundValues = BlockBoundValuesOf( head, spec );
			Structor? memberPrimary = null;

			foreach( Structor candidate in candidates ) {
				if( BlockSpecCoversParams( spec, candidate )
				 && SignatureMatchesParas( spec, candidate, boundValues ) ) {
					memberPrimary = candidate;
					break;
				}
			}

			if( memberPrimary == null ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Member specialization of the constructor of '{0}' must repeat the primary's substituted parameter types",
						primary.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			if( !RejectVariadicPrimary( memberPrimary, spec, "constructor" ) )
				return;

			FinishBlockMemberSpec( spec, memberPrimary, head, classArgs, seenSignatures );
		}

		// A field member of a [spec=some] block: only [static] fields can be
		// specialized (a value member would change the class layout, which
		// C++ forbids), and the declared type must repeat the primary's
		// substituted type.
		private void ValidateBlockVarSpec( VarDecl spec, Myll.Core.Structural head, Myll.Core.Structural primary, List<TplArg> classArgs, Dictionary<Decl, List<string>> seenSignatures )
		{
			if( !spec.IsStatic ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Field specializations in the block of '{0}' must be [static]",
						head.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			if( spec.init == null && !isDeclModule ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Member specialization of '{0}' must be a full definition",
						spec.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			if( !RejectBlockMemberIssues( spec, primary ) )
				return;

			List<VarDecl> candidates = primary.children
				.OfType<VarDecl>()
				.Where( v => v.name == spec.name && v.SpecInfo == null )
				.ToList();

			if( candidates.Count == 0 ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Class '{0}' has no field '{1}'; a member specialization block cannot add members",
						primary.name, spec.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			Dictionary<string, Typespec> boundValues = BlockBoundValuesOf( head, spec );
			VarDecl? memberPrimary = null;

			foreach( VarDecl candidate in candidates ) {
				if( BlockSpecCoversParams( spec, candidate )
				 && SubstituteBindings( spec.type, boundValues ).Gen()
				 == SubstituteBindings( candidate.type, boundValues ).Gen() ) {
					memberPrimary = candidate;
					break;
				}
			}

			if( memberPrimary == null ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Member specialization of '{0}' must repeat the primary's substituted type",
						spec.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			FinishBlockMemberSpec( spec, memberPrimary, head, classArgs, seenSignatures );
		}

		// A nested class member of a [spec=some] block: a full specialization
		// of the nested class for the concrete enclosing arguments, so it
		// carries its own [spec=all] attribute and a fresh body.
		private void ValidateBlockNestedStructSpec( Structural spec, Myll.Core.Structural head, Myll.Core.Structural primary, List<TplArg> classArgs, Dictionary<Decl, List<string>> seenSignatures )
		{
			if( !spec.IsAttrib( "spec", "all" ) ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Nested class specializations in a block must declare [spec=all] with a fresh body",
						spec.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			if( !RejectBlockMemberIssues( spec, primary ) )
				return;

			List<Structural> candidates = primary.children
				.OfType<Structural>()
				.Where( s => s.name == spec.name && s.SpecInfo == null && s.TplParams.Count > 0 )
				.ToList();

			if( candidates.Count == 0 ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Class '{0}' has no nested class template '{1}'",
						primary.name, spec.name ) ) );
				MarkInvalidSpec( spec );
				return;
			}

			FinishBlockStructSpec( spec, candidates[0], head, classArgs, seenSignatures );
		}

		// Shared early guard for block members: bindings must not address
		// class parameters, which the head carries.
		private bool RejectBlockMemberIssues( Decl spec, Myll.Core.Structural primary )
		{
			if( TplBindings( spec ).Any( b => primary.TplParams.Any( p => p.name == b.name ) ) ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Member specialization of '{0}' must not bind class parameters; the block head carries the class arguments",
						spec.name ) ) );
				MarkInvalidSpec( spec );
				return false;
			}

			return true;
		}

		// Merged bound values of the head's class bindings and the member's
		// own bindings, for substituted-signature comparison.
		private static Dictionary<string, Typespec> BlockBoundValuesOf( Myll.Core.Structural head, Decl memberSpec )
		{
			Dictionary<string, Typespec> boundValues = BoundValuesOf( head.TplSpec );
			foreach( (string name, Typespec value) in BoundValuesOf( TplBindings( memberSpec ) ) )
				boundValues[name] = value;
			return boundValues;
		}

		// A block member's bindings may only name the member primary's own
		// template parameters.
		private static bool BlockSpecCoversParams( Decl spec, ITemplatedDecl memberPrimary )
		{
			HashSet<string> names = new( memberPrimary.TplParams.Select( p => p.name ) );
			return TplBindings( spec ).All( b => names.Contains( b.name ) );
		}

		private static bool IsCopyOrMoveBlockCtor( Structor spec, Myll.Core.Structural head )
		{
			if( spec.kind != Structor.Kind.Constructor || spec.paras.Count != 1 )
				return false;
			if( spec.paras[0].type is not TypespecNested nested )
				return false;
			if( nested.idTpls.Count != 1 || nested.idTpls[0].id != head.name )
				return false;
			if( nested.ptrs == null || nested.ptrs.Count != 1 )
				return false;

			return nested.ptrs[0].kind == Pointer.Kind.LVRef
			    || nested.ptrs[0].kind == Pointer.Kind.RVRef;
		}

		// Shared tail for func and ctor block members: shape and concreteness
		// of own bindings, the at-least-one-bound rule, the in-block duplicate
		// check, and the SpecInfo wiring with the head's class arguments.
		private bool FinishBlockMemberSpec( Decl spec, ITemplatedDecl memberPrimary, Myll.Core.Structural head, List<TplArg> classArgs, Dictionary<Decl, List<string>> seenSignatures )
		{
			List<TplBinding> bindings = TplBindings( spec );

			if( !ValidateBindingShape( spec.name, bindings, memberPrimary.TplParams, spec.srcPos ) ) {
				MarkInvalidSpec( spec );
				return false;
			}

			if( !HasConcreteBindings( bindings, memberPrimary.TplParams, spec.name, spec.srcPos ) ) {
				MarkInvalidSpec( spec );
				return false;
			}

			// A member template specialization must bind at least one of its
			// own parameters: an all-open one specializes nothing.
			if( bindings.Count > 0 && !bindings.Any( b => b.type != null || b.lit != null ) ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Member specialization of '{0}' must bind at least one of its own template parameters",
						spec.name ) ) );
				MarkInvalidSpec( spec );
				return false;
			}

			if( !TryAddBlockMemberSignature( (Decl) memberPrimary, spec, seenSignatures ) ) {
				MarkInvalidSpec( spec );
				return false;
			}

			List<TplArg> memberArgs = BuildSpecArgs( memberPrimary.TplParams, bindings, out bool[] memberBound, out List<TplParam> unboundParams );

			List<TplArg> args = new( classArgs );
			args.AddRange( memberArgs );

			bool[] boundFlags = new bool[classArgs.Count + memberBound.Length];
			for( int i = 0; i < classArgs.Count; i++ )
				boundFlags[i] = true;
			memberBound.CopyTo( boundFlags, classArgs.Count );

			spec.SpecInfo = new Decl.SpecData {
				Primary = (Decl) memberPrimary,
				Args    = args,
				Bound   = boundFlags,
			};

			Scope?       memberScope = null;
			List<Param>  paras       = new();
			switch( spec ) {
				case Func f:
					f.TplParams   = unboundParams;
					memberScope   = f.funcScope;
					paras         = f.paras;
					break;
				case Structor c:
					c.TplParams   = unboundParams;
					memberScope   = c.ctorScope;
					paras         = c.paras;
					break;
				case VarDecl v:
					v.TplParams   = unboundParams;
					memberScope   = v.varScope;
					break;
				case Myll.Core.Structural s:
					s.TplParams   = unboundParams;
					break;
			}
			InjectParams( memberScope, unboundParams, spec.srcPos );
			InjectBoundAliases( memberScope, paras, spec.srcPos, BoundValuesOf( bindings ) );

			return true;
		}

		// Shared tail for nested class block members: the func/ctor tail plus
		// class-scope bound aliases like a fresh specialization.
		private void FinishBlockStructSpec( Structural spec, Structural memberPrimary, Myll.Core.Structural head, List<TplArg> classArgs, Dictionary<Decl, List<string>> seenSignatures )
		{
			if( !FinishBlockMemberSpec( spec, memberPrimary, head, classArgs, seenSignatures ) )
				return;

			InjectBoundAliases( spec.scope, new List<Param>(), spec.srcPos, BoundValuesOf( spec.TplSpec ) );
		}

		// Duplicate check inside one block, keyed by bound bindings plus the
		// written signature: plain member overloads differ by their
		// parameters, not by bindings.
		private bool TryAddBlockMemberSignature( Decl memberPrimary, Decl spec, Dictionary<Decl, List<string>> seenSignatures )
		{
			string body = spec switch {
				Func f     => String.Join( ",", f.paras.Select( p => p.type.Gen() ) ) + "|" + f.retType.Gen(),
				Structor c => String.Join( ",", c.paras.Select( p => p.type.Gen() ) ),
				VarDecl v  => v.type.Gen() + "|" + ( v.init?.Gen() ?? "" ),
				_          => "",
			};
			string signature = String.Join( ",", TplBindings( spec ).Where( b => b.type != null || b.lit != null ).Select( b => bindingSignature( b ) ) )
			                  + "|" + body;

			if( !seenSignatures.TryGetValue( memberPrimary, out List<string>? seen ) ) {
				seen = new();
				seenSignatures[memberPrimary] = seen;
			}

			if( seen.Contains( signature ) ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Duplicate member specialization of '{0}' in the block",
						spec.name ) ) );
				return false;
			}

			seen.Add( signature );
			return true;
		}

		// Finds a visible specialization of the requested kind for the same
		// primary and class arguments: own module first, then directly
		// imported modules (the same visibility as primary lookup).
		private Myll.Core.Structural? FindVisibleSpecOfKind( Myll.Core.Structural primary, List<TplArg> args, bool wantMemberHead, string? nsFqn, List<Myll.Core.Structural> localStructs )
		{
			foreach( Myll.Core.Structural s in localStructs ) {
				if( s.SpecInfo == null || s.SpecInfo.MemberSpecHead != wantMemberHead )
					continue;
				if( !ReferenceEquals( s.SpecInfo.Primary, primary ) || !ArgsMatch( s.SpecInfo.Args, args ) )
					continue;

				return s;
			}

			foreach( string importedName in ImportedModuleNames( moduleAst ) ) {
				foreach( CompiledModuleResult m in allModules ) {
					if( m.Module.module != importedName )
						continue;

					Myll.Core.Hierarchical? hier = FindNamespaceScope( m.Module, nsFqn );
					if( hier == null || hier.scope == null
					 || !hier.scope.children.TryGetValue( primary.name, out List<ScopeLeaf>? leaves ) )
						continue;

					foreach( ScopeLeaf leaf in leaves ) {
						if( leaf.decl is not Myll.Core.Structural s || s.SpecInfo == null )
							continue;
						if( s.SpecInfo.MemberSpecHead != wantMemberHead )
							continue;
						if( !ReferenceEquals( s.SpecInfo.Primary, primary ) || !ArgsMatch( s.SpecInfo.Args, args ) )
							continue;

						return s;
					}
				}
			}

			return null;
		}

		private static bool ArgsMatch( List<TplArg> a, List<TplArg> b )
		{
			if( a.Count != b.Count )
				return false;
			for( int i = 0; i < a.Count; i++ ) {
				if( a[i].Gen() != b[i].Gen() )
					return false;
			}

			return true;
		}

		/// primary: a specialization is a full one (C++ allows no partial function
		/// specializations), its parameters and return type must repeat the
		/// <summary>
		/// Validates function template specializations and links them to their
		/// primary: a specialization is a full one (C++ allows no partial function
		/// specializations), its parameters and return type must repeat the
		/// primary's after substituting the bound values, and bound parameter
		/// names stay usable inside the specialization through bound-parameter
		/// aliases (`T` keeps meaning the bound value), in the signature and the
		/// body alike.
		/// </summary>
		private void ValidateFuncSpecs( List<Func> funcs, Myll.Core.Structural? owner, string? nsFqn )
		{
			if( funcs.All( f => f.TplSpec.Count == 0 ) )
				return;

			if( owner != null ) {
				ValidateMethodSpecs( funcs, owner );
				return;
			}

			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<Decl, List<string>> seenSignatures = new();

			foreach( Func spec in funcs ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				if( spec.body == null && !isDeclModule ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must be a full definition",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				(Func? primary, bool isImported) = FindPrimary( funcs, spec,
					f => f.name == spec.name && f.SpecInfo == null && f.TplParams.Count > 0,
					IsFuncPrimary, nsFqn,
					String.Format( "No matching function template '{0}' in this module or its imports; a specialization must live in the primary's module or import it",
						spec.name ) );
				if( primary == null ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !RejectVariadicPrimary( primary, spec, "function" ) )
					continue;

				if( !ValidateFollowOrder( spec, primary, isImported, String.Format( "Specialization of '{0}' must follow its primary template",
					spec.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Function templates allow only full specializations: every
				// parameter must be bound, so no bare names and a full count.
				if( spec.TplSpec.Any( b => b.type == null && b.lit == null )
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

				if( !HasConcreteBindings( spec.TplSpec, primary.TplParams, spec.name, spec.srcPos ) ) {
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

				if( !SignatureMatchesParas( spec, primary, boundValues ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must repeat the primary's substituted parameter and return types",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				// Duplicate specialization signature check.
				if( !TryAddSpecSignature( primary, spec.name, spec.TplSpec, spec.srcPos, seenSignatures ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

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

		// The specialization repeats the primary's substituted parameter types
		// and, for functions, the return type.
		private static bool SignatureMatchesParas( Func spec, Func primary, Dictionary<string, Typespec> boundValues )
		{
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

			return signatureMatches;
		}

		// Constructors have no return type.
		private static bool SignatureMatchesParas( Structor spec, Structor primary, Dictionary<string, Typespec> boundValues )
		{
			if( spec.paras.Count != primary.paras.Count )
				return false;

			for( int i = 0; i < spec.paras.Count; i++ ) {
				if( SubstituteBindings( spec.paras[i].type, boundValues ).Gen()
				 != SubstituteBindings( primary.paras[i].type, boundValues ).Gen() )
					return false;
			}

			return true;
		}

		/// <summary>
		/// Validates member specializations inside one class: the bindings name
		/// the class parameters first, then the method's own parameters, each in
		/// declaration order. All class parameters must be bound to concrete
		/// types (C++ requires concrete class arguments for member
		/// specializations); method parameters may stay open, but only when the
		/// class is a template. The body is emitted out-of-line after the class
		/// (C++ rejects in-class member specializations), and bound names stay
		/// usable through the same bound-parameter aliases as free functions.
		/// </summary>
		private void ValidateMethodSpecs( List<Func> funcs, Myll.Core.Structural owner )
		{
			if( owner.SpecInfo != null ) {
				// A [spec=some] block's members are validated by the block pass.
				if( owner.SpecInfo.MemberSpecHead )
					return;

				foreach( Func spec in funcs.Where( f => f.TplSpec.Count > 0 ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' sits inside the specialization of '{1}; specialize the class instead",
							spec.name, owner.name ) ) );
					MarkInvalidSpec( spec );
				}
				return;
			}

			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<Decl, List<string>> seenSignatures = new();

			foreach( Func spec in funcs ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				if( spec.body == null && !isDeclModule ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must be a full definition",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				// The primary is a same-name method of the same class whose
				// parameter list covers all binding names: the class parameters
				// followed by the method's own.
				(Func? primary, _) = FindPrimary( funcs, spec,
					f => f != spec && f.name == spec.name && f.SpecInfo == null && CoversBindings( owner, f, spec ),
					null, null,
					String.Format( "No matching method template '{0}' in this class; a specialization must name class and method parameters",
						spec.name ) );
				if( primary == null ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !RejectVariadicPrimary( primary, spec, "method" ) )
					continue;

				// Same-name class and method parameters make the bindings ambiguous;
				// the bindings map by name, so rename one.
				List<TplParam> probeTarget = new();
				probeTarget.AddRange( owner.TplParams );
				probeTarget.AddRange( primary.TplParams );
				if( probeTarget.GroupBy( p => p.name ).Any( g => g.Count() > 1 ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Method '{0}' has a template parameter shadowing the class parameter of '{1}; rename it",
							spec.name, owner.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				List<TplParam> target = new();
				target.AddRange( owner.TplParams );
				target.AddRange( primary.TplParams );

				if( !ValidateFollowOrder( spec, primary, false, String.Format( "Specialization of '{0}' must follow its primary template",
					spec.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ValidateBindingShape( spec.name, spec.TplSpec, target, spec.srcPos ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Every class parameter must be bound to a concrete value: C++
				// requires concrete class arguments for member specializations.
				if( !ClassParamsBound( spec, owner, String.Format( "Specialization of '{0}' must bind all class template parameters of '{1}' with concrete types",
						spec.name, owner.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !HasConcreteBindings( spec.TplSpec, target, spec.name, spec.srcPos ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				// Open member parameters are only expressible when the class is a
				// template (template<> template<typename U> ...): a plain class
				// allows full member specializations only.
				bool hasOpenMembers = spec.TplSpec.Any( b => b.type == null && b.lit == null )
				                       || spec.TplSpec.Count < target.Count;
				if( hasOpenMembers && owner.TplParams.Count == 0 ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Function templates allow only full specializations: specialization of '{0}' must bind all {1} parameters",
							spec.name,
							target.Count ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				// The specialization repeats the primary's substituted signature:
				// parameter types and return type, with class and member
				// bindings substituted alike.
				if( IsAutoType( spec.retType ) || IsAutoType( primary.retType ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must repeat the primary's return type; inferred returns cannot specialize",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				Dictionary<string, Typespec> boundValues = BoundValuesOf( spec.TplSpec );

				if( !SignatureMatchesParas( spec, primary, boundValues ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must repeat the primary's substituted parameter and return types",
							spec.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				if( !TryAddSpecSignature( primary, spec.name, spec.TplSpec, spec.srcPos, seenSignatures ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				List<TplArg> args = BuildSpecArgs( target, spec.TplSpec, out bool[] bound, out List<TplParam> unboundParams );

				spec.SpecInfo = new Decl.SpecData {
					Primary = primary,
					Args    = args,
					Bound   = bound,
				};
				spec.TplParams = unboundParams; // the open member parameters

				// Open member parameters become the specialization's own template
				// parameters, injected into its scope like the free-function case;
				// bound ones stay usable through bound-parameter aliases.
				InjectParams( spec.funcScope, unboundParams, spec.srcPos );
				InjectBoundAliases( spec, boundValues );
				InjectOwnerAlias( spec, owner, spec.funcScope );
			}
		}

		/// <summary>
		/// Validates constructor specializations inside one class: the bindings name
		/// the class parameters first, then the constructor's own template
		/// parameters. Constructors can only be specialized for a class template
		/// (both compilers reject the plain-class form); the member parameters may
		/// stay open or be fully bound — both spellings are emittable, with the
		/// deduction form for fully bound ones because template-ids on constructor
		/// names are compiler-divergent. Bodies emit out-of-line after the class,
		/// like method specializations.
		/// </summary>
		/// <summary>
		/// Validates member variable specializations inside one class: the bindings
		/// name the class parameters first, then the variable's own parameters —
		/// the same rule as method specializations. All class parameters must be
		/// bound to concrete values (C++ requires concrete enclosing arguments),
		/// and at least one of the variable's own parameters must be bound: an
		/// all-open member specialization does not specialize anything (both
		/// compilers reject it). The emission is out-of-line after the class.
		/// </summary>
		private void ValidateMemberVarSpecs( List<VarDecl> vars, Myll.Core.Structural owner )
		{
			if( owner.SpecInfo != null ) {
				// A [spec=some] block's members are validated by the block pass.
				if( owner.SpecInfo.MemberSpecHead )
					return;

				foreach( VarDecl spec in vars.Where( v => v.TplSpec.Count > 0 ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' sits inside the specialization of '{1}'; specialize the class instead",
							spec.name, owner.name ) ) );
					MarkInvalidSpec( spec );
				}
				return;
			}

			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<Decl, List<string>> seenSignatures = new();

			foreach( VarDecl spec in vars ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				(VarDecl? primary, _) = FindPrimary( vars, spec,
					v => v != spec && v.SpecInfo == null && CoversBindings( owner, v, spec ),
					null, null,
					String.Format( "No matching variable template '{0}' in this class; a specialization must name class and variable parameters",
						spec.name ) );
				if( primary == null ) {
					MarkInvalidSpec( spec );
					continue;
				}

				List<TplParam> target = MemberTarget( owner, primary );

				if( !MemberGuards( spec, owner, primary, target, "variable" ) )
					continue;

				if( !ValidateFollowOrder( spec, primary, false, String.Format( "Specialization of '{0}' must follow its primary template",
					spec.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ValidateBindingShape( spec.name, spec.TplSpec, target, spec.srcPos ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ClassParamsBound( spec, owner, String.Format( "Specialization of '{0}' must bind all class template parameters of '{1}' with concrete types",
						spec.name, owner.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !HasConcreteBindings( spec.TplSpec, target, spec.name, spec.srcPos ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !BindsMemberParam( spec, owner, target ) )
					continue;

				Dictionary<string, Typespec> boundValues = BoundValuesOf( spec.TplSpec );

				Typespec specType = SubstituteBindings( spec.type, boundValues );
				Typespec primaryType = SubstituteBindings( primary.type, boundValues );

				context.UnresolvedTypes.RemoveAll( ut => ReferenceEquals( ut.Node, spec.type ) );

				if( specType.Gen() != primaryType.Gen() ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' declares type '{1}' but the primary's substituted type is '{2}'",
							spec.name, specType.Gen(), primaryType.Gen() ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				if( !TryAddSpecSignature( primary, spec.name, spec.TplSpec, spec.srcPos, seenSignatures ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				List<TplArg> args = BuildSpecArgs( target, spec.TplSpec, out bool[] bound, out List<TplParam> unboundParams );

				spec.SpecInfo = new Decl.SpecData {
					Primary = primary,
					Args    = args,
					Bound   = bound,
				};
				spec.type      = specType;
				spec.TplParams = unboundParams;

				InjectParams( spec.varScope, unboundParams, spec.srcPos );
				InjectBoundAliases( spec.varScope, new List<Param>(), spec.srcPos, boundValues );
				InjectOwnerAlias( spec, owner, spec.varScope );
			}
		}

		/// <summary>
		/// Validates nested class specializations inside one class: bindings name
		/// the enclosing class parameters first, then the nested class's own.
		/// Same rules as member variable specializations: concrete class
		/// arguments, at least one own parameter bound, emission out-of-line.
		/// </summary>
		private void ValidateNestedStructSpecs( List<Myll.Core.Structural> structs, Myll.Core.Structural owner )
		{
			if( owner.SpecInfo != null ) {
				// A [spec=some] block's members are validated by the block pass.
				if( owner.SpecInfo.MemberSpecHead )
					return;

				foreach( Myll.Core.Structural spec in structs.Where( s => s.TplSpec.Count > 0 ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' sits inside the specialization of '{1}'; specialize the enclosing class instead",
							spec.name, owner.name ) ) );
					MarkInvalidSpec( spec );
				}
				return;
			}

			if( owner.scope?.parent?.decl is Myll.Core.Structural ) {
				foreach( Myll.Core.Structural spec in structs.Where( s => s.TplSpec.Count > 0 ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specializations of nested classes inside another nested class are not supported yet; declare '{0}' at class or namespace scope",
							spec.name ) ) );
					MarkInvalidSpec( spec );
				}
				return;
			}

			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<Decl, List<string>> seenSignatures = new();

			foreach( Myll.Core.Structural spec in structs ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				// Same [spec=all] requirement as outer specializations; the
				// fresh body of a nested class spec replaces all its members.
				if( !spec.IsAttrib( "spec", "all" ) ) {
					if( !spec.HasAttrib( "spec" ) )
						Diagnostics.Add( new Diagnostic(
							spec.srcPos,
							DiagnosticKind.Error,
							String.Format( "Specialization of '{0}' must declare its kind: [spec=all] for a fresh body or [spec=some] for a member specialization block",
								spec.name ) ) );
					else if( spec.IsAttrib( "spec", "some" ) )
						Diagnostics.Add( new Diagnostic(
							spec.srcPos,
							DiagnosticKind.Error,
							String.Format( "Member specialization blocks of nested classes are not supported; declare the block for '{1}' at module or namespace scope",
								spec.name, owner.name ) ) );

					MarkInvalidSpec( spec );
					continue;
				}

				(Myll.Core.Structural? primary, _) = FindPrimary( structs, spec,
					s => s != spec && s.SpecInfo == null && CoversBindings( owner, s, spec ),
					null, null,
					String.Format( "No matching template '{0}' in this class; a specialization must name class and nested class parameters",
						spec.name ) );
				if( primary == null ) {
					MarkInvalidSpec( spec );
					continue;
				}

				List<TplParam> target = MemberTarget( owner, primary );

				if( !MemberGuards( spec, owner, primary, target, "class" ) )
					continue;

				if( !ValidateFollowOrder( spec, primary, false, String.Format( "Specialization of '{0}' must follow its primary template",
					spec.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ValidateBindingShape( spec.name, spec.TplSpec, target, spec.srcPos ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ClassParamsBound( spec, owner, String.Format( "Specialization of '{0}' must bind all class template parameters of '{1}' with concrete types",
						spec.name, owner.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !HasConcreteBindings( spec.TplSpec, target, spec.name, spec.srcPos ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !BindsMemberParam( spec, owner, target ) )
					continue;

				if( !TryAddSpecSignature( primary, spec.name, spec.TplSpec, spec.srcPos, seenSignatures ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				List<TplArg> args = BuildSpecArgs( target, spec.TplSpec, out bool[] bound, out List<TplParam> unboundParams );

				spec.SpecInfo = new Decl.SpecData {
					Primary = primary,
					Args    = args,
					Bound   = bound,
				};
				spec.TplParams = unboundParams;

				InjectParams( spec.scope, unboundParams, spec.srcPos );
				InjectBoundAliases( spec.scope, new List<Param>(), spec.srcPos, BoundValuesOf( spec.TplSpec ) );
			}
		}

		// Shared guards for member specializations: the shadow-name check and the
		// at-least-one-bound-parameter rule. Returns true when the spec may
		// continue validation.
		private bool MemberGuards( Decl spec, Myll.Core.Structural owner, ITemplatedDecl primary, List<TplParam> target, string kindWord )
		{
			// Same-name class and member parameters make the bindings ambiguous;
			// the bindings map by name, so rename one.
			List<TplParam> probeTarget = new();
			probeTarget.AddRange( owner.TplParams );
			probeTarget.AddRange( primary.TplParams );
			if( probeTarget.GroupBy( p => p.name ).Any( g => g.Count() > 1 ) ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "The {0} '{1}' has a template parameter shadowing the class parameter of '{2}'; rename it",
						kindWord, spec.name, owner.name ) ) );
				MarkInvalidSpec( spec );
				return false;
			}

			return true;
		}

		// A specialization that binds none of the member's own parameters does not
		// specialize anything: both compilers reject it ("does not specialize any
		// template argument").
		private bool BindsMemberParam( Decl spec, Myll.Core.Structural owner, List<TplParam> target )
		{
			int classParamCount = owner.TplParams.Count;
			bool anyMemberBound = TplBindings( spec ).Any( b => ( b.type != null || b.lit != null )
				&& target.Any( p => p.name == b.name && target.IndexOf( p ) >= classParamCount ) );
			if( anyMemberBound )
				return true;

			Diagnostics.Add( new Diagnostic(
				spec.srcPos,
				DiagnosticKind.Error,
				String.Format( "Specialization of '{0}' must bind at least one of '{1}'s own template parameters",
					spec.name, owner.name ) ) );
			MarkInvalidSpec( spec );
			return false;
		}

		private void ValidateCtorSpecs( List<Structor> ctors, Myll.Core.Structural owner )
		{
			if( ctors.All( c => c.TplSpec.Count == 0 ) )
				return;

			if( owner.SpecInfo != null ) {
				// A [spec=some] block's members are validated by the block pass.
				if( owner.SpecInfo.MemberSpecHead )
					return;

				foreach( Structor spec in ctors.Where( c => c.TplSpec.Count > 0 ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of the constructor sits inside the specialization of '{0}; specialize the class instead",
							owner.name ) ) );
					MarkInvalidSpec( spec );
				}
				return;
			}

			// per primary: bound-value signatures already seen (duplicate check)
			Dictionary<Decl, List<string>> seenSignatures = new();

			foreach( Structor spec in ctors ) {
				if( spec.TplSpec.Count == 0 )
					continue;

				if( spec.body == null && !isDeclModule ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of the constructor of '{0}' must be a full definition",
							owner.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				if( owner.TplParams.Count == 0 ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Constructor specializations need a class template; '{0}' is not a template",
							owner.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				// The primary is a same-class constructor whose parameter list
				// covers all binding names: the class parameters followed by its
				// own.
				(Structor? primary, _) = FindPrimary( ctors, spec,
					c => c != spec && c.SpecInfo == null && CoversCtorBindings( owner, c, spec ),
					null, null,
					String.Format( "No matching constructor template in '{0}'; a specialization must name class and constructor parameters",
						owner.name ) );
				if( primary == null ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !RejectVariadicPrimary( primary, spec, "constructor" ) )
					continue;

				List<TplParam> target = new();
				target.AddRange( owner.TplParams );
				target.AddRange( primary.TplParams );

				if( !ValidateFollowOrder( spec, primary, false, String.Format( "Specialization of the constructor of '{0}' must follow its primary",
					owner.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ValidateBindingShape( owner.name, spec.TplSpec, target, spec.srcPos ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !ClassParamsBound( spec, owner, String.Format( "Specialization of the constructor of '{0}' must bind all class template parameters with concrete types",
						owner.name ) ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				if( !HasConcreteBindings( spec.TplSpec, target, owner.name, spec.srcPos ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				Dictionary<string, Typespec> boundValues = BoundValuesOf( spec.TplSpec );

				// Constructors have no return type: the signature repeats the
				// primary's substituted parameters only.
				if( !SignatureMatchesParas( spec, primary, boundValues ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of the constructor of '{0}' must repeat the primary's substituted parameters",
							owner.name ) ) );
					MarkInvalidSpec( spec );
					continue;
				}

				if( !TryAddSpecSignature( primary, "ctor of " + owner.name, spec.TplSpec, spec.srcPos, seenSignatures ) ) {
					MarkInvalidSpec( spec );
					continue;
				}

				List<TplArg> args = BuildSpecArgs( target, spec.TplSpec, out bool[] bound, out List<TplParam> unboundParams );

				spec.SpecInfo = new Decl.SpecData {
					Primary = primary,
					Args    = args,
					Bound   = bound,
				};
				spec.TplParams = unboundParams; // the open constructor parameters

				InjectParams( spec.ctorScope, unboundParams, spec.srcPos );
				InjectBoundAliases( spec.ctorScope, spec.paras, spec.srcPos, boundValues );
				InjectOwnerAlias( spec, owner, spec.ctorScope );
			}
		}

		// The primary covers the spec when every binding name exists in the
		// primary's parameter list: the class parameters followed by its own.
		private static bool CoversCtorBindings( Myll.Core.Structural owner, Structor candidate, Structor spec )
			=> spec.TplSpec.All(
				b => owner.TplParams.Any( p => p.name == b.name )
				 || candidate.TplParams.Any( p => p.name == b.name ) );

		private static bool CoversBindings( Myll.Core.Structural owner, ITemplatedDecl candidate, Decl spec )
			=> TplBindings( spec ).All(
				b => owner.TplParams.Any( p => p.name == b.name )
				 || candidate.TplParams.Any( p => p.name == b.name ) );

		private static List<TplParam> MemberTarget( Myll.Core.Structural owner, ITemplatedDecl primary )
		{
			List<TplParam> target = new();
			target.AddRange( owner.TplParams );
			target.AddRange( primary.TplParams );
			return target;
		}

		// Bindings accessor shared by the hooks (each kind has its own TplSpec field).
		private static List<TplBinding> TplBindings( Decl spec )
			=> spec switch {
				VarDecl v => v.TplSpec,
				Myll.Core.Structural s => s.TplSpec,
				Func f => f.TplSpec,
				Structor c => c.TplSpec,
				_ => throw new InvalidOperationException(),
			};

		private bool ClassParamsBound( Decl spec, Myll.Core.Structural owner, string message )
		{
			bool classParamsBound = owner.TplParams
				.All( p => TplBindings( spec ).Any( b => b.name == p.name && ( b.type != null || b.lit != null ) ) );
			if( classParamsBound )
				return true;

			Diagnostics.Add( new Diagnostic( spec.srcPos, DiagnosticKind.Error, message ) );
			return false;
		}

		// Bindings must name concrete types: a value naming one of the target
		// parameters (T=U) has no C++ equivalent for function templates, and
		// for class templates it is a pattern specialization, which Myll
		// does not model yet.
		private bool HasConcreteBindings( List<TplBinding> tplSpec, List<TplParam> target, string specName, SrcPos srcPos )
		{
			HashSet<string> names = new( target.Select( p => p.name ) );

			foreach( TplBinding binding in tplSpec ) {
				if( binding.type is TypespecNested nested
				 && nested.idTpls.Count == 1
				 && nested.idTpls[0].tplArgs.Count == 0
				 && names.Contains( nested.idTpls[0].id ) ) {
					Diagnostics.Add( new Diagnostic(
						srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must bind '{1}' to a concrete type, not the parameter '{2}'",
							specName, binding.name, nested.idTpls[0].id ) ) );
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// A valid pattern binding: the written type names one of the primary's
		/// type parameters with at least one pointer/reference decoration and
		/// no qualifiers — `T*`, `T&`, `T**`. The named parameter is deduced
		/// per use by unification.
		/// </summary>
		private static bool IsPatternShape( Typespec? type, IReadOnlyList<TplParam> primaryParams, out string? paramName )
		{
			paramName = null;
			if( type is not TypespecNested nested
			 || nested.idTpls.Count != 1
			 || nested.idTpls[0].tplArgs.Count != 0 )
				return false;

			TplParam? named = primaryParams
				.FirstOrDefault( p => p.name == nested.idTpls[0].id && p.kind != TplParamKind.NonType );
			if( named == null )
				return false;

			paramName = named.name;
			return nested.qual == Qualifier.None
			 && nested.ptrs is { Count: >= 1 }
			 && nested.ptrs.All( p => p.kind is Pointer.Kind.RawPtr or Pointer.Kind.LVRef or Pointer.Kind.RVRef );
		}

		// Whether the type mentions any of the names symbolically at any depth
		// (bare ids or nested template arguments): such bindings must be valid
		// patterns, anything else is a deferred pattern form.
		private static bool MentionsPrimaryParam( Typespec? type, HashSet<string> names )
		{
			if( type is not TypespecNested nested )
				return false;

			foreach( IdTplArgs segment in nested.idTpls ) {
				if( names.Contains( segment.id ) )
					return true;
				if( segment.tplArgs == null )
					continue;
				foreach( TplArg arg in segment.tplArgs ) {
					if( arg.typespec != null && MentionsPrimaryParam( arg.typespec, names ) )
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Classifies every binding of a class or variable specialization:
		/// concrete values pass, valid patterns are collected into
		/// <paramref name="patternBindings"/>, and anything naming a primary
		/// parameter otherwise is rejected (bare self, nested, array or
		/// qualified patterns — deferred forms). A parameter named by a pattern
		/// must not also be concretely bound.
		/// </summary>
		private bool ClassifySpecBindings( Decl spec, IReadOnlyList<TplParam> primaryParams, out List<TplBinding> patternBindings )
		{
			patternBindings = new();
			List<TplBinding> bindings = TplBindings( spec );
			HashSet<string> names = new( primaryParams.Select( p => p.name ) );
			HashSet<string> patternNamed = new( StringComparer.Ordinal );

			foreach( TplBinding binding in bindings ) {
				if( binding.type == null )
					continue;

				if( IsPatternShape( binding.type, primaryParams, out string? named ) ) {
					patternBindings.Add( binding );
					patternNamed.Add( named! );
					continue;
				}

				if( MentionsPrimaryParam( binding.type, names ) ) {
					string reason;
					TypespecNested? n2 = binding.type as TypespecNested;
					if( n2 != null && n2.idTpls.Count == 1 && n2.idTpls[0].tplArgs.Count == 0
					 && ( n2.ptrs == null || n2.ptrs.Count == 0 ) )
						reason = "a bare parameter binding matches every instantiation like the primary; make it concrete or a pattern (e.g. "
							+ binding.name + "=" + binding.name + "*)";
					else if( n2 != null && n2.qual != Qualifier.None )
						reason = "qualified patterns are not supported yet";
					else if( n2 != null && n2.ptrs != null
					 && n2.ptrs.Any( p => p.kind != Pointer.Kind.RawPtr && p.kind != Pointer.Kind.LVRef && p.kind != Pointer.Kind.RVRef ) )
						reason = "array patterns are not supported yet";
					else
						reason = "nested patterns are not supported yet";
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' has an unsupported pattern binding '{1}': {2}",
							spec.name, binding.name + "=" + binding.type.Gen(), reason ) ) );
					return false;
				}
			}

			foreach( TplBinding binding in bindings ) {
				if( patternNamed.Contains( binding.name )
				 && ( binding.type != null || binding.lit != null )
				 && !patternBindings.Any( p => p.name == binding.name ) ) {
					Diagnostics.Add( new Diagnostic(
						spec.srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}': the parameter '{1}' cannot be concretely bound and pattern-deduced at once",
							spec.name, binding.name ) ) );
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// The asserted shape ([spec=(all, full)] / (all, partial)) must match
		/// the derived shape: full means every position concretely bound,
		/// partial means any open or pattern position.
		/// </summary>
		private bool ValidateAssertedSpecShape( Decl spec, List<TplBinding> patternBindings )
		{
			SpecAttribValues? values = ParseSpecAttrib( spec, "class", "" );
			if( values == null || !values.HasShape )
				return true;

			bool hasBare = TplBindings( spec ).Any( b => b.type == null && b.lit == null );
			bool derivedFull = patternBindings.Count == 0 && !hasBare;

			if( values.IsFull == true && !derivedFull ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Specialization of '{0}' declares [spec=(all, full)] but the bindings are partial",
						spec.name ) ) );
				return false;
			}

			if( values.IsFull == false && derivedFull ) {
				Diagnostics.Add( new Diagnostic(
					spec.srcPos,
					DiagnosticKind.Error,
					String.Format( "Specialization of '{0}' declares [spec=(all, partial)] but every parameter is concretely bound",
						spec.name ) ) );
				return false;
			}

			return true;
		}

		// A bound parameter keeps its meaning inside the specialization through a
		// hidden alias: `T` resolves to the alias, the alias resolves to the bound
		// value, and emission expands it so no `T` reaches the generated C++.
		private void InjectBoundAliases( Func spec, Dictionary<string, Typespec> boundValues )
			=> InjectBoundAliases( spec.funcScope, spec.paras, spec.srcPos, boundValues );

		private void InjectBoundAliases( Scope? scope, List<Param> paras, SrcPos srcPos, Dictionary<string, Typespec> boundValues )
		{
			if( scope == null )
				return;

			foreach( (string name, Typespec value) in boundValues ) {
				if( paras.Any( p => p.name == name ) ) {
					Diagnostics.Add( new Diagnostic(
						srcPos,
						DiagnosticKind.Error,
						String.Format( "Parameter '{0}' shadows a bound template parameter; rename it",
							name ) ) );
					continue;
				}

				AddBoundAlias( scope, name, value, srcPos );
			}
		}

		// Registers a hidden alias in the specialization's scope: resolution
		// binds the name to it, emission expands it to the aliased type.
		private void AddBoundAlias( Scope scope, string name, Typespec type, SrcPos srcPos )
		{
			AliasDecl alias = new() {
				name          = name,
				type          = type,
				srcPos        = srcPos,
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

		/// <summary>
		/// The owner's class name inside a member specialization means the
		/// specialized instantiation (OpBox stands for OpBox<int>): the
		/// out-of-line emission cannot use the injected class name.
		/// </summary>
		private void InjectOwnerAlias( Decl spec, Myll.Core.Structural owner, Scope? funcScope )
		{
			if( owner.TplParams.Count == 0 || funcScope == null )
				return;

			List<TplArg> ownerArgs = spec.SpecInfo.Args
				.Take( owner.TplParams.Count )
				.ToList();
			TypespecNested ownerType = new() {
				idTpls = new List<IdTplArgs> { new() { id = owner.name, tplArgs = ownerArgs } },
			};
			AddBoundAlias( funcScope, owner.name, ownerType, spec.srcPos );

			// Resolve the aliased type against the class scope: the class
			// name would resolve through the alias itself otherwise.
			context.UnresolvedTypes.Add(
				new UnresolvedType( ownerType, funcScope.parent ?? funcScope ) );
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

			bool isFullForm = tplSpec.Any( b => b.type == null && b.lit == null );

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
			}

			// Bindings must match their parameter's kind: non-type parameters
			// bind values (N=5), type parameters bind types (T=int).
			Dictionary<string, TplParam> paramsByName = new( StringComparer.Ordinal );
			foreach( TplParam param in primaryParams )
				paramsByName[ param.name ] = param;

			foreach( TplBinding binding in tplSpec ) {
				if( !paramsByName.TryGetValue( binding.name, out TplParam param ) )
					continue;

				if( param.kind == TplParamKind.NonType && binding.lit == null && binding.type != null ) {
					Diagnostics.Add( new Diagnostic(
						srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must bind the non-type parameter '{1}' to a value, not a type",
							specName, binding.name ) ) );
					return false;
				}
				if( param.kind != TplParamKind.NonType && binding.lit != null ) {
					Diagnostics.Add( new Diagnostic(
						srcPos,
						DiagnosticKind.Error,
						String.Format( "Specialization of '{0}' must bind the type parameter '{1}' to a type, not a value",
							specName, binding.name ) ) );
					return false;
				}
			}

			if( isFullForm )
				return true;

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
		// Builds the specialization argument list: every primary parameter in
		// declaration order, concrete values for concrete positions, the written
		// pattern for pattern positions, and symbolic placeholders for unbound
		// ones; both pattern and unbound positions' parameters become the
		// specialization's own template parameters.
		private static List<TplArg> BuildSpecArgs( IReadOnlyList<TplParam> primaryParams, List<TplBinding> tplSpec,
			out bool[] bound, out List<TplParam> unboundParams, out bool[] pattern )
		{
			bound = new bool[ primaryParams.Count ];
			pattern = new bool[ primaryParams.Count ];
			List<TplArg> args = new( primaryParams.Count );
			unboundParams = new();

			for( int i = 0; i < primaryParams.Count; i++ ) {
				TplParam param = primaryParams[ i ];
				TplBinding? binding = tplSpec
					.FirstOrDefault( b => b.name == param.name );

				if( binding != null && binding.type != null
				 && IsPatternShape( binding.type, primaryParams, out string? named ) ) {
					pattern[ i ] = true;
					args.Add( new TplArg { typespec = binding.type } );
					// The pattern-named parameter deduces; the position's own
					// parameter only exists if named or bare elsewhere.
					if( !unboundParams.Any( p => p.name == named ) )
						unboundParams.Add( primaryParams.First( p => p.name == named ) );
				}
				else if( binding != null && ( binding.type != null || binding.lit != null ) ) {
					bound[ i ] = true;
					args.Add( new TplArg {
						typespec = binding.type,
						lit      = binding.lit,
					} );
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

		private static List<TplArg> BuildSpecArgs( IReadOnlyList<TplParam> primaryParams, List<TplBinding> tplSpec, out bool[] bound, out List<TplParam> unboundParams )
		{
			List<TplArg> args = BuildSpecArgs( primaryParams, tplSpec, out bound, out unboundParams, out _ );
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
			=> binding.name + "=" + ( binding.type?.Gen() ?? binding.lit!.Gen() );

		/// <summary>
		/// Records the bound-value signature of a specialization per primary;
		/// a duplicate within one module is rejected. Cross-module duplicates
		/// are reported by the resolver's ODR guard.
		/// </summary>
		private bool TryAddSpecSignature( Decl primary, string specName, List<TplBinding> tplSpec, SrcPos srcPos, Dictionary<Decl, List<string>> seenSignatures )
		{
			string signature = String.Join( ",",
				tplSpec
					.Where( b => b.type != null || b.lit != null )
					.Select( b => bindingSignature( b ) ) );

			if( !seenSignatures.TryGetValue( primary, out List<string>? seen ) ) {
				seen = new();
				seenSignatures[ primary ] = seen;
			}

			if( seen.Contains( signature ) ) {
				Diagnostics.Add( new Diagnostic(
					srcPos,
					DiagnosticKind.Error,
					String.Format( "Duplicate specialization of '{0}' for {1}",
						specName, signature ) ) );
				return false;
			}

			seen.Add( signature );
			return true;
		}

		/// <summary>
		/// Marks a specialization whose validation failed so later passes treat it
		/// as consumed (no duplicate-name error, no overload candidate).
		/// </summary>
		private static void MarkInvalidSpec( Decl spec )
			=> spec.SpecInfo = new Decl.SpecData { Primary = spec };

		// Specializing a variadic template is not modeled yet: the bindings
		// would have to name pack elements.
		private bool RejectVariadicPrimary( Decl primary, Decl spec, string kindWord )
		{
			if( primary is not ITemplatedDecl td
			 || !td.TplParams.Any( p => p.isPack ) )
				return true;

			Diagnostics.Add( new Diagnostic(
				spec.srcPos,
				DiagnosticKind.Error,
				String.Format( "Specializing the variadic {0} template '{1}' is not supported yet",
					kindWord, primary.name ) ) );
			MarkInvalidSpec( spec );
			return false;
		}

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
