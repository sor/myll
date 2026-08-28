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
			"double",
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

		private NameResolver(
			IReadOnlyDictionary<string, ModuleExports> moduleExports,
			ResolutionResult result,
			List<Diagnostic> diagnostics )
		{
			this.moduleExports = moduleExports;
			this.result        = result;
			this.diagnostics   = diagnostics;
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
					progress |= resolver.ResolveIds( module, context );
					progress |= resolver.ResolveScopeds( module, context );
					progress |= resolver.ResolveTypes( module, context );
				}
			} while( progress );

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
				Decl? resolved = ResolvePath( segments, unresolved.Scope, module, out _ );
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

				Decl? resolved = ResolvePath( unresolved.Node.idTpls, unresolved.Scope, module, out _ );
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

				Decl? resolved = ResolvePath( unresolved.Node.idTpls, unresolved.Scope, module, out _ );
				if( resolved != null ) {
					result.Resolve( unresolved.Node, resolved );
					progress = true;
				}
			}

			return progress;
		}

		private Decl? ResolvePath(
			IReadOnlyList<IdTplArgs> segments,
			Scope                    startScope,
			GlobalNamespace          module,
			out int                  unresolvedSegmentIndex )
		{
			unresolvedSegmentIndex = -1;
			if( segments.Count == 0 )
				return null;

			string  first   = segments[0].id;
			Decl?   current = LookupNameInScope( first, startScope )
			               ?? LookupInImports( first, module );
			if( current == null ) {
				if( segments.Count == 1
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
				Decl? next = LookupSingleInHierarchical( name, h );
				if( next == null ) {
					unresolvedSegmentIndex = i;
					return null;
				}

				current = next;
			}

			return current;
		}

		private Decl? LookupNameInScope( string name, Scope scope )
		{
			for( Scope? cur = scope; cur != null; cur = cur.parent ) {
				Decl? fromChildren = LookupSingleInScopeChildren( name, cur );
				if( fromChildren != null )
					return fromChildren;

				foreach( Scope imported in cur.importedScopes ) {
					Decl? fromImport = LookupSingleInScopeChildren( name, imported );
					if( fromImport != null )
						return fromImport;
				}
			}

			return null;
		}

		private Decl? LookupSingleInScopeChildren( string name, Scope scope )
		{
			if( !scope.children.TryGetValue( name, out List<ScopeLeaf>? leaves ) )
				return null;

			return PickSingle( leaves );
		}

		private Decl? LookupSingleInHierarchical( string name, Hierarchical h )
		{
			if( !h.scope.children.TryGetValue( name, out List<ScopeLeaf>? leaves ) )
				return null;

			return PickSingle( leaves );
		}

		private Decl? LookupInImports( string name, GlobalNamespace module )
		{
			foreach( string importedModule in module.imps ) {
				if( !moduleExports.TryGetValue( importedModule, out ModuleExports? exports ) )
					continue;

				Decl? resolved = exports.Lookup( name );
				if( resolved != null )
					return resolved;
			}

			return null;
		}

		private static Decl? PickSingle( List<ScopeLeaf> leaves )
		{
			List<Decl> visible = leaves
				.Select( l => l.decl )
				.Where( d => d != null )
				.Cast<Decl>()
				.ToList();

			if( visible.Count == 0 )
				return null;

			if( visible.Count == 1 )
				return visible[0];

			return null;
		}

		private void ReportUnresolved(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
					if( result.Ids.ContainsKey( unresolved.Node ) )
						continue;

					IdTplArgs[] segments = new[] { unresolved.Node.idTplArgs };
					ReportPathUnresolved( "identifier", unresolved.Node.srcPos, segments, 0 );
				}

				foreach( UnresolvedScoped unresolved in context.UnresolvedScopeds ) {
					if( result.Scopeds.ContainsKey( unresolved.Node ) )
						continue;

					ResolvePath( unresolved.Node.idTpls, unresolved.Scope, module, out int idx );
					if( idx < 0 )
						idx = 0;
					ReportPathUnresolved( "identifier", unresolved.Node.srcPos, unresolved.Node.idTpls, idx );
				}

				foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
					if( result.Types.ContainsKey( unresolved.Node ) )
						continue;

					ResolvePath( unresolved.Node.idTpls, unresolved.Scope, module, out int idx );
					if( idx < 0 )
						idx = 0;
					ReportPathUnresolved( "type", unresolved.Node.srcPos, unresolved.Node.idTpls, idx );
				}
			}
		}

		private void ReportPathUnresolved(
			string              kind,
			SrcPos              srcPos,
			IReadOnlyList<IdTplArgs> segments,
			int                 unresolvedSegmentIndex )
		{
			string unresolvedName = segments[unresolvedSegmentIndex].id;
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

		private sealed class ModuleExports
		{
			private readonly Dictionary<string, List<Decl>> exports = new();

			public void Add( string name, Decl decl )
			{
				if( !exports.TryGetValue( name, out List<Decl>? list ) ) {
					list = new List<Decl>( 1 );
					exports.Add( name, list );
				}

				list.Add( decl );
			}

			public Decl? Lookup( string name )
			{
				if( !exports.TryGetValue( name, out List<Decl>? list ) )
					return null;

				return PickSingleFromExports( list );
			}

			private static Decl? PickSingleFromExports( List<Decl> decls )
			{
				List<Decl> visible = decls.Where( d => !d.IsHidden ).ToList();
				if( visible.Count == 1 )
					return visible[0];

				return null;
			}
		}
	}
}
