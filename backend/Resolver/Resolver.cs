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

		public static (ResolutionResult Result, IReadOnlyList<Diagnostic> Diagnostics) Resolve(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
		{
			var exports     = BuildModuleExports( modules );
			var result      = new ResolutionResult();
			var diagnostics = new List<Diagnostic>();
			var resolver    = new NameResolver( exports, result, diagnostics );

		bool progress;
		do {
			progress = false;
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				progress |= resolver.ResolveUsings( module, context );
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

		var discardTransformer = new DiscardTransformer( result, diagnostics );
		discardTransformer.Transform( modules );

		resolver.ValidateTypes( modules );
		resolver.ReportUnresolved( modules );

		return (result, diagnostics);
		}

		private static IReadOnlyDictionary<string, ModuleExports> BuildModuleExports(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
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

					if( decl is Hierarchical h )
						CollectExports( h.scope, exports );
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
				if( resolved != null ) {
					result.Resolve( unresolved.Node, resolved );
					progress = true;
				}
			}

			return progress;
		}

		private bool ResolveMemberAccesses( GlobalNamespace module, CompilationContext context )
		{
			bool progress = false;
			foreach( UnresolvedMemberAccess unresolved in context.UnresolvedMemberAccesses ) {
				if( unresolved.Node.right is not IdExpr member )
					continue;

				if( result.Members.ContainsKey( member ) )
					continue;

				if( !TryGetMemberAccessBaseType( unresolved.Node.left, unresolved.Scope, out Hierarchical? baseType ) )
					continue;

				Decl? resolved = LookupMember( member.idTplArgs.id, baseType, module );
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
					if( result.Ids.ContainsKey( id ) )
						return false;

					List<Decl> candidates = LookupNameCandidates( id.idTplArgs.id, call.Scope, module );
					var outcome = TryResolveOverload( candidates, call.Call, call.Scope, module, call.Callee );
					return ApplyOverloadOutcome( outcome, id, call.Callee );
				}
				case ScopedExpr scoped: {
					if( result.Scopeds.ContainsKey( scoped ) )
						return false;

					var outcome = ResolveScopedCallOverload( scoped, call.Call, call.Scope, module, call.Callee );
					return ApplyScopedOverloadOutcome( outcome, scoped, call.Callee );
				}
				case BinOp binOp when IsMemberAccessOperation( binOp.op ): {
					if( binOp.right is not IdExpr member )
						return false;

					if( result.Members.ContainsKey( member ) )
						return false;

					if( !TryGetMemberAccessBaseType( binOp.left, call.Scope, out Hierarchical? baseType ) )
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
				diagnostics.Add( new Diagnostic(
					GetCalleeSrcPos( callee ),
					DiagnosticKind.Error,
					String.Format( "No matching overload for '{0}'", name ) ) );
			}
		}

		private static SrcPos GetCalleeSrcPos( Expr callee )
		{
			return callee switch {
				IdExpr id                => id.srcPos,
				ScopedExpr scoped        => scoped.srcPos,
				BinOp binOp              => binOp.right is IdExpr m ? m.srcPos : binOp.srcPos,
				_
					                       => callee.srcPos,
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
			List<Decl> callable = candidates
				.Where( d => d is Func or Structor )
				.ToList();

			if( callable.Count == 0 )
				return (null, false, true);

			// Prefer an arity match first; if only one candidate has the right arity, pick it.
			List<Decl> sameArity = callable
				.Where( d => GetParamCount( d ) == call.args.Count )
				.ToList();

			if( sameArity.Count == 0 )
				return (null, false, true);

			if( sameArity.Count == 1 )
				return (sameArity[0], false, false);

			// Multiple candidates with the same arity: resolve argument types and
			// rank candidates by the worst conversion required for any argument.
			List<Typespec?> argTypes = call.args
				.Select( a => typeResolver.Resolve( a.expr ) )
				.ToList();

			// If any argument type is still unknown, we cannot decide yet. Do not emit
			// a diagnostic; a later resolver pass may provide the missing type.
			if( argTypes.Any( t => t == null ) )
				return (null, false, false);

			List<(Decl Candidate, ConversionRank WorstRank)> viable = new();
			ConversionRank bestWorst = ConversionRank.None;

			foreach( Decl candidate in sameArity ) {
				ConversionRank worst = ConversionRank.Exact;
				bool ok = true;

				List<Param> paras = candidate switch {
					Func func    => func.paras,
					Structor stc => stc.paras,
					_            => new List<Param>(),
				};

				for( int i = 0; i < paras.Count; i++ ) {
					ConversionRank rank = ConversionRules.GetRank( argTypes[i]!, paras[i].type );
					if( rank > ConversionRank.Conversion ) {
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

			if( viable.Count == 1 )
				return (viable[0].Candidate, false, false);

			if( viable.Count > 1 )
				return (null, true, false);

			return (null, false, true);
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
					unresolved.Node.IsNamespaceUsing = true;
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

		private bool TryGetMemberAccessBaseType( Expr expr, Scope scope, out Hierarchical baseType )
		{
			baseType = null!;
			Typespec? type = null;

			if( expr is IdExpr id ) {
				if( id.idTplArgs.id == "self" ) {
					baseType = FindEnclosingStructural( scope )!;
					return baseType != null;
				}

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
				return TryGetMemberAccessBaseType( parenUnop.expr, scope, out baseType );
			}
			else if( expr is UnOp derefUnop && derefUnop.op == Operand.Dereference ) {
				if( TryGetMemberAccessBaseType( derefUnop.expr, scope, out Hierarchical inner ) ) {
					baseType = inner;
					return true;
				}
			}
			else if( expr is FuncCallExpr call ) {
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

		private bool TryGetBaseHierarchical( Typespec type, out Hierarchical baseType )
		{
			baseType = null!;
			if( type is TypespecNested nested ) {
				Decl? decl = nested.resolvedDecl;
				if( decl == null )
					result.TryGetResolved( nested, out decl );
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
						TypespecNested n => new TypespecNested { idTpls = n.idTpls, qual = n.qual,
							resolvedDecl = n.resolvedDecl, ptrs = new( ptrs ) },
						TypespecBasic b  => new TypespecBasic { kind = b.kind, size = b.size, align = b.align,
							qual = b.qual, ptrs = new( ptrs ) },
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

		private Decl? LookupMember( string name, Hierarchical baseType, GlobalNamespace module )
		{
			List<Decl> candidates = new();
			AddVisibleChildren( candidates, baseType.scope, name, filterHidden: false );

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

			// Inner scopes shadow outer scopes. Once we have found any visible
			// declaration at this scope level, stop looking further out.
			if( result.Count > 0 )
				break;
		}

		foreach( string importedModule in module.imps ) {
			if( !moduleExports.TryGetValue( importedModule, out ModuleExports? exports ) )
				continue;

			result.AddRange( exports.LookupAll( name ) );
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

		private void ValidateTypes(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
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

		private void ReportUnresolved(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
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
					if( TryGetMemberAccessBaseType( unresolved.Node.left, unresolved.Scope, out Hierarchical? baseType ) )
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
			string              kind,
			SrcPos              srcPos,
			IReadOnlyList<IdTplArgs> segments,
			GlobalNamespace       module,
			Scope                 scope )
		{
			ResolvePath( segments, scope, module, out int unresolvedSegmentIndex, callSite: false );
			if( unresolvedSegmentIndex < 0 )
				unresolvedSegmentIndex = 0;

			string unresolvedName = segments[unresolvedSegmentIndex].id;
			List<Decl> candidates = unresolvedSegmentIndex == 0
				? LookupNameCandidates( unresolvedName, scope, module )
				: LookupInHierarchicalCandidates(
					unresolvedName,
					(PickSingleCandidate( LookupNameCandidates( segments[0].id, scope, module ), callSite: false ) as Hierarchical)!,
					module );

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
