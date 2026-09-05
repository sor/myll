using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

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
			"string",
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

		private readonly IReadOnlyDictionary<string, ModuleExports> moduleExports;
		private readonly ResolutionResult result;
		private readonly List<Diagnostic> diagnostics;
		private readonly TypeResolver typeResolver;
		private readonly HashSet<Expr> ambiguousCalls = new();
		private readonly HashSet<Expr> noMatchingCalls = new();
		private readonly HashSet<Expr> templateArityMismatchCalls = new();

		private NameResolver(
			IReadOnlyDictionary<string, ModuleExports> moduleExports,
			ResolutionResult result,
			List<Diagnostic> diagnostics )
		{
			this.moduleExports = moduleExports;
			this.result        = result;
			this.diagnostics   = diagnostics;
			this.typeResolver  = new TypeResolver( result );
		}

		public static ResolveResult Resolve( IReadOnlyList<CompiledModuleResult> modules )
		{
			var result      = new ResolutionResult();
			var diagnostics = new List<Diagnostic>();

			var preResolveTransforms = new List<ITransformer> {
				new DefaultAttributesTransformer(),
				new EnumTransformer(),
				new AutoReturnTransformer(),
				new TemplateParamTransformer(),
				new ChainTransformer(),
			};

			foreach( ITransformer transformer in preResolveTransforms )
				transformer.Transform( modules, diagnostics );

			var exports  = BuildModuleExports( modules );
			var resolver = new NameResolver( exports, result, diagnostics );

			bool progress;
			do {
				progress = false;
				foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
					progress |= resolver.ResolveUsings( module, context );
					progress |= resolver.ResolveAliases( module, context );
					progress |= resolver.ResolveIds( module, context );
					progress |= resolver.ResolveScopeds( module, context );
					progress |= resolver.ResolveTypes( module, context );
					progress |= resolver.ResolveMemberAccesses( module, context );
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

			var postResolveTransforms = new List<ITransformer> {
				new DiscardTransformer( result, diagnostics ),
				new RuleOfTransformer(),
				new ElseOnLoopTransformer(),
				new BreakContinueTransformer(),
			};

			foreach( ITransformer transformer in postResolveTransforms )
				transformer.Transform( modules, diagnostics );

			resolver.RejectRequiresClauses( modules );
			resolver.ValidateTypes( modules );
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

		private bool ResolveIds( GlobalNamespace module, CompilationContext context )
		{
			bool progress = false;
			foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
				if( result.Ids.ContainsKey( unresolved.Node ) )
					continue;

				IdTplArgs[] segments = new[] { unresolved.Node.idTplArgs };
				bool callSite = context.FuncCallCallees.Contains( unresolved.Node );
				Decl? resolved = ResolvePath( segments, unresolved.Scope, module, out _, callSite );
				if( resolved != null ) {
					result.Resolve( unresolved.Node, resolved );
					progress = true;
				}
			}

			return progress;
		}

		private bool ResolveScopeds( GlobalNamespace module, CompilationContext context )
		{
			bool progress = false;
			foreach( UnresolvedScoped unresolved in context.UnresolvedScopeds ) {
				if( result.Scopeds.ContainsKey( unresolved.Node ) )
					continue;

				bool callSite = context.FuncCallCallees.Contains( unresolved.Node );
				Decl? resolved = ResolvePath( unresolved.Node.idTpls, unresolved.Scope, module, out _, callSite );
				if( resolved != null ) {
					result.Resolve( unresolved.Node, resolved );
					progress = true;
				}
			}

			return progress;
		}

		private bool ResolveTypes( GlobalNamespace module, CompilationContext context )
		{
			bool progress = false;
			foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
				if( result.Types.ContainsKey( unresolved.Node ) )
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

				IReadOnlyList<TplArg> providedArgs = unresolved.Node.idTpls.Last().tplArgs;
				if( TemplateInference.HasTemplateArityMismatch( resolved, providedArgs )
				 && !IsInjectedClassName( unresolved.Node, resolved, unresolved.Scope ) ) {
					diagnostics.Add( new Diagnostic(
						unresolved.Node.srcPos,
						DiagnosticKind.Error,
						String.Format( "Template argument count mismatch for type '{0}': expected {1}, got {2}",
							GetTypeName( unresolved.Node ),
							TemplateInference.GetTemplateParamCount( resolved ),
							providedArgs.Count ) ) );
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
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
					IsDependentNestedName( unresolved.Node, unresolved.Scope, module );
				}
			}
		}

		private void ValidateGlobalUsings( IReadOnlyList<CompiledModuleResult> modules )
		{
			if( Dialect.GlobalUsingNS == GlobalUsingNSMode.Leaky )
				return;

			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				foreach( UnresolvedUsing unresolved in context.UnresolvedUsings ) {
					UsingDecl usingDecl = unresolved.Node;

					if( !usingDecl.IsNamespaceUsing )
						continue;

					// Scoped `using` inside a namespace is always allowed.
					if( unresolved.Scope.decl is not GlobalNamespace )
						continue;

					diagnostics.Add( new Diagnostic(
						usingDecl.srcPos,
						DiagnosticKind.Error,
						Dialect.GlobalUsingNS == GlobalUsingNSMode.Disabled
							? "Global `using` of a namespace is disabled in this dialect."
							: "`Dialect.GlobalUsingNSMode.Contained` is not implemented yet."
					) );
				}
			}
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

		private bool ResolveMemberAccesses( GlobalNamespace module, CompilationContext context )
		{
			bool progress = false;
			foreach( UnresolvedMemberAccess unresolved in context.UnresolvedMemberAccesses ) {
				if( unresolved.Node.right is not IdExpr member )
					continue;

				if( result.Members.ContainsKey( member ) )
					continue;

				if( !TryGetMemberAccessBaseType( unresolved.Node.left, unresolved.Scope, module, out Hierarchical? baseType ) )
					continue;

				Decl? resolved = LookupMember( member.idTplArgs.id, (Structural) baseType, module );
				if( resolved != null ) {
					result.ResolveMember( member, resolved );
					progress = true;
				}
			}

			return progress;
		}

		private bool ResolveCalls( GlobalNamespace module, CompilationContext context )
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

					if( !TryGetMemberAccessBaseType( binOp.left, call.Scope, module, out Hierarchical? baseType ) )
						return false;

					List<Decl> candidates = LookupInHierarchicalCandidates(
						member.idTplArgs.id, baseType, module );
					var outcome = TryResolveOverload( candidates, call.Call, call.Scope, module, call.Callee );

					if( outcome.chosen != null ) {
						result.ResolveMember( member, outcome.chosen );
						return true;
					}

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
			if( outcome.ambiguous && ambiguousCalls.Add( callee ) ) {
				diagnostics.Add( new Diagnostic(
					GetCalleeSrcPos( callee ),
					DiagnosticKind.Error,
					String.Format( "Ambiguous call to '{0}'", name ) ) );
			}
			else if( outcome.noMatch && noMatchingCalls.Add( callee ) ) {
				if( !templateArityMismatchCalls.Contains( callee ) ) {
					diagnostics.Add( new Diagnostic(
						GetCalleeSrcPos( callee ),
						DiagnosticKind.Error,
						String.Format( "No matching overload for '{0}'", name ) ) );
				}
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

			// Prefer an arity match first; if only one candidate has the right arity, pick it.
			List<Decl> sameArity = callable
				.Where( d => GetParamCount( d ) == call.args.Count )
				.ToList();

			if( sameArity.Count == 0 )
				return (null, false, true);

			IReadOnlyList<TplArg>? explicitTemplateArgs = TemplateInference.GetExplicitTemplateArgs( callee );
			Dictionary<Decl, List<TplArg>> deducedArgsByCandidate = new();
			bool templateArityMismatch = false;

			// Filter candidates by template-argument availability and, when no explicit
			// arguments are supplied, try to deduce them from the call arguments.
			List<Decl> templateViable = new();
			foreach( Decl candidate in sameArity ) {
				if( explicitTemplateArgs is { Count: > 0 } ) {
					if( TemplateInference.HasTemplateArityMismatch( candidate, explicitTemplateArgs ) ) {
						templateArityMismatch = true;
						continue;
					}

					templateViable.Add( candidate );
					continue;
				}

				if( candidate is Func func
				 && TemplateInference.TryDeduceTemplateArgs( func, call, typeResolver, result, out List<TplArg>? deduced )
				 && deduced != null ) {
					deducedArgsByCandidate[candidate] = deduced;
					templateViable.Add( candidate );
					continue;
				}

				if( TemplateInference.GetTemplateParamCount( candidate ) == 0 )
					templateViable.Add( candidate );
			}

			if( templateViable.Count == 0 ) {
				if( templateArityMismatch && sameArity.Count == 1 )
					ReportTemplateArityMismatch( sameArity[0], explicitTemplateArgs!, callee );
				else if( templateArityMismatch )
					ReportTemplateArityMismatch( sameArity, explicitTemplateArgs!, callee );

				return (null, false, true);
			}

			// A lone non-template candidate (or a template candidate without explicit/deduced
			// arguments) is accepted without a conversion check. This preserves the old
			// resolver behaviour for class-template methods and other dependent calls where
			// the concrete type-checking is delegated to the C++ compiler.
			if( templateViable.Count == 1
			 && explicitTemplateArgs?.Count == 0
			 && !deducedArgsByCandidate.ContainsKey( templateViable[0] ) ) {
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

				for( int i = 0; i < paras.Count; i++ ) {
					Typespec paramType = candidate is Func func && candidateArgs != null
						? TemplateInference.SubstituteTemplateParams( paras[i].type, func.TplParams, candidateArgs, result )
						: paras[i].type;

					ConversionRank rank = ConversionRules.GetRank( argTypes[i]!, paramType );
					if( rank == ConversionRank.None ) {
						ok = false;
						break;
					}

					if( rank > worst )
						worst = rank;
				}

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

			if( viable.Count == 1 ) {
				Decl chosen = viable[0].Candidate;
				if( deducedArgsByCandidate.TryGetValue( chosen, out List<TplArg>? deduced ) )
					TemplateInference.ApplyTemplateArgs( callee, deduced );
				return (chosen, false, false);
			}

			if( viable.Count > 1 )
				return (null, true, false);

			return (null, false, true);
		}

		private void ReportTemplateArityMismatch( Decl candidate, IReadOnlyList<TplArg> args, Expr callee )
		{
			if( !templateArityMismatchCalls.Add( callee ) )
				return;

			int expected = TemplateInference.GetTemplateParamCount( candidate );
			int actual   = args.Count;
			string name  = GetTemplateCalleeName( callee );
			diagnostics.Add( new Diagnostic(
				GetCalleeSrcPos( callee ),
				DiagnosticKind.Error,
				expected == 0
					? String.Format( "'{0}' is not a template; 0 template arguments expected, {1} provided", name, actual )
					: String.Format( "Template argument count mismatch for '{0}': expected {1}, got {2}", name, expected, actual ) ) );
		}

		private void ReportTemplateArityMismatch( List<Decl> candidates, IReadOnlyList<TplArg> args, Expr callee )
		{
			if( !templateArityMismatchCalls.Add( callee ) )
				return;

			string name = GetTemplateCalleeName( callee );
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

		private bool ResolveUsings( GlobalNamespace module, CompilationContext context )
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

		private bool ResolveAliases( GlobalNamespace module, CompilationContext context )
		{
			bool progress = false;
			foreach( UnresolvedAlias unresolved in context.UnresolvedAliases ) {
				if( unresolved.Node.type is not TypespecNested path )
					continue;

				Decl? resolved = ResolvePath( path.idTpls, unresolved.Scope, module, out _, callSite: false );
				if( resolved == null )
					continue;

				Scope scope = unresolved.Scope;
				bool isNamespaceTarget = resolved is Namespace;
				if( unresolved.Node.IsNamespaceAlias != isNamespaceTarget ) {
					unresolved.Node.IsNamespaceAlias = isNamespaceTarget;
					progress = true;
				}

				progress |= AddImportedName( scope, unresolved.Node.name, resolved );
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

		private bool TryGetMemberAccessBaseType( Expr expr, Scope scope, GlobalNamespace module, out Hierarchical baseType )
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
				if( TryGetMemberAccessBaseType( derefUnop.expr, scope, module, out Hierarchical inner ) ) {
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
					}
					else if( call.expr is ScopedExpr calleeScoped && result.Scopeds.TryGetValue( calleeScoped, out Decl? calleeScopedDecl ) ) {
						if( calleeScopedDecl is Func f )
							type = f.retType;
						else if( calleeScopedDecl is Structor s && s.kind == Structor.Kind.Constructor )
							baseType = FindEnclosingStructural( s.scope )!;
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
									Typespec? substituted = SubstituteMethodReturnType( f, receiverType );
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

			return type != null && TryGetBaseHierarchical( type, out baseType );
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

		private Typespec? SubstituteMethodReturnType( Func method, Typespec receiverType )
		{
			Structural? owner = FindEnclosingStructural( method.scope ) as Structural;
			if( owner == null || owner.TplParams.Count == 0 )
				return null;

			if( receiverType is not TypespecNested nested )
				return null;

			List<TplArg>? args = nested.idTpls.LastOrDefault()?.tplArgs;
			if( args == null || args.Count < owner.TplParams.Count )
				return null;

			if( !UsesClassTplParam( method.retType, owner.TplParams ) )
				return null;

			foreach( TplArg arg in args ) {
				if( arg.typespec is TypespecNested argNested && argNested.resolvedDecl == null
				 && result.TryGetResolved( argNested, out Decl? decl ) ) {
					argNested.resolvedDecl = decl;
				}
			}

			return TemplateInference.SubstituteTemplateParams( method.retType, owner.TplParams, args, result );
		}

		private bool TryGetIndexerElementType( Expr receiver, GlobalNamespace module, out Typespec elementType )
		{
			elementType = null!;

			if( !TryGetReceiverType( receiver, out Typespec receiverType ) )
				return false;

			if( !TryGetBaseHierarchical( receiverType, out Hierarchical? container ) )
				return false;

			if( container is not Structural structural )
				return false;

			Decl? indexer = LookupMember( "operator[]", structural, module );
			if( indexer is not Func idxFunc )
				return false;

			Typespec ret = idxFunc.retType;
			Typespec? substituted = SubstituteMethodReturnType( idxFunc, receiverType );
			if( substituted != null )
				ret = substituted;

			elementType = ret;
			return true;
		}

		private bool UsesClassTplParam( Typespec type, List<TplParam> parameters )
		{
			if( type is TypespecNested nested ) {
				Decl? decl = nested.resolvedDecl;
				if( decl == null )
					result.TryGetResolved( nested, out decl );
				if( decl is TplParamDecl tpl && parameters.Any( p => p.name == tpl.name ) )
					return true;

				foreach( IdTplArgs segment in nested.idTpls ) {
					if( segment.tplArgs == null )
						continue;
					foreach( TplArg arg in segment.tplArgs ) {
						if( arg.typespec != null && UsesClassTplParam( arg.typespec, parameters ) )
							return true;
					}
				}
			}

			return false;
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

		private bool TryGetBaseHierarchical( Typespec type, out Hierarchical baseType )
		{
			baseType = null!;
			if( type is TypespecNested nested ) {
				Decl? decl = nested.resolvedDecl;
				if( decl == null )
					result.TryGetResolved( nested, out decl );
				if( decl is Structural st && st.IsForwardDeclaration )
					decl = GetDefiningStructural( st ) ?? decl;
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
					return TryGetBaseHierarchical( inner, out baseType );
				}
			}

			return false;
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
				if( !String.IsNullOrEmpty( Dialect.BaseClassAliasName )
				 && name == Dialect.BaseClassAliasName ) {
					Structural? baseClass = GetFirstBaseStructural( structural );
					if( baseClass != null )
						result.Add( baseClass );
				}
				else if( !String.IsNullOrEmpty( Dialect.OwnClassAliasName )
				      && name == Dialect.OwnClassAliasName ) {
					result.Add( structural );
				}
			}

			foreach( string importedModule in module.imps ) {
				if( !moduleExports.TryGetValue( importedModule, out ModuleExports? exports ) )
					continue;

				result.AddRange( exports.LookupAll( name ) );
			}
		}

		return DistinctCandidates( result );
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
				foreach( string importedModule in module.imps ) {
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
			if( (Dialect.DefaultFloat & DefaultTypeMode.Forbidden) != 0 ) {
				foreach( (_, CompilationContext context) in modules ) {
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

		private void ReportUnresolved( IReadOnlyList<CompiledModuleResult> modules )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
					if( result.Ids.ContainsKey( unresolved.Node ) )
						continue;

					if( ambiguousCalls.Contains( unresolved.Node ) || noMatchingCalls.Contains( unresolved.Node ) )
						continue;

					IdTplArgs[] segments = new[] { unresolved.Node.idTplArgs };
					ReportUnresolvedPath( "identifier", unresolved.Node.srcPos, segments, module, unresolved.Scope );
				}

				foreach( UnresolvedScoped unresolved in context.UnresolvedScopeds ) {
					if( result.Scopeds.ContainsKey( unresolved.Node ) )
						continue;

					if( ambiguousCalls.Contains( unresolved.Node ) || noMatchingCalls.Contains( unresolved.Node ) )
						continue;

					ReportUnresolvedPath( "identifier", unresolved.Node.srcPos, unresolved.Node.idTpls, module, unresolved.Scope );
				}

				foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
					if( result.Types.ContainsKey( unresolved.Node ) )
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

				if( ambiguousCalls.Contains( unresolved.Node ) || noMatchingCalls.Contains( unresolved.Node ) )
					continue;

				string? typeName = null;
				if( TryGetMemberAccessBaseType( unresolved.Node.left, unresolved.Scope, module, out Hierarchical? baseType ) )
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
			Scope                    scope )
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
