using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Extracts the enclosing class template arguments that a member call
	/// carries: from a receiver expression (`b.method()`, `b->method()`), or
	/// from a qualified name's prefix (`Outer<int>::Inner::method()`).
	/// Shared by the member overload ranking (GetOwnerTemplateInfo) and the
	/// member call return-type substitution
	/// (TypeResolver.SubstituteCallTemplateArgs).
	/// </summary>
	internal static class OwnerTemplateArgs
	{
		/// <summary>
		/// Returns the chain of enclosing class/struct/union declarations, from outermost
		/// to innermost, for a method or constructor candidate.
		/// </summary>
		public static List<Structural> GetStructuralChain( Decl decl )
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

		public static List<TplArg>? TryExtract(
			Expr                 callee,
			List<Structural>     ownerChain,
			TypeResolver         typeResolver,
			ResolutionResult     result )
		{
			return callee switch {
				BinOp binOp when OperatorRules.IsMemberAccess( binOp.op )
					=> TryExtractFromReceiver( binOp.left, ownerChain, typeResolver, result ),
				ScopedExpr scoped
					=> TryExtractFromScopedPrefix( scoped, ownerChain, result ),
				_ => null,
			};
		}

		private static List<TplArg>? TryExtractFromReceiver(
			Expr                 receiverExpr,
			List<Structural>     ownerChain,
			TypeResolver         typeResolver,
			ResolutionResult     result )
		{
			Typespec? receiverType = typeResolver.Resolve( receiverExpr );
			if( receiverType == null )
				return null;

			Typespec baseType = StripReferenceAndPointerWrappers( ConversionRules.UnwrapAlias( receiverType, result ) );
			if( baseType is TypespecNested nested ) {
				List<TplArg>? direct = TryExtractFromTypePath( nested, ownerChain, result );
				if( direct != null )
					return direct;

				return TryExtractFromBases( nested, ownerChain, result );
			}

			return null;
		}

		private static List<TplArg>? TryExtractFromScopedPrefix(
			ScopedExpr       scoped,
			List<Structural> ownerChain,
			ResolutionResult result )
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

			return TryExtractFromTypePath( prefix, ownerChain, result );
		}

		private static List<TplArg>? TryExtractFromTypePath(
			TypespecNested     typePath,
			List<Structural>   ownerChain,
			ResolutionResult   result )
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
					 && result.TryGetResolved( unresolved, out Decl? decl ) )
						unresolved.resolvedDecl = decl;

					ownerArgs.Add( arg );
				}

				segIdx++;
			}

			return ownerArgs;
		}

		private static bool TypeSegmentMatchesStructural( IdTplArgs segment, Structural structural )
			=> segment.id == structural.name;

		private static List<TplArg>? TryExtractFromBases(
			TypespecNested     receiverType,
			List<Structural>   ownerChain,
			ResolutionResult   result )
		{
			Decl? derivedDecl = receiverType.resolvedDecl;
			if( derivedDecl == null && result.TryGetResolved( receiverType, out Decl? d ) )
				derivedDecl = d;
			if( derivedDecl is not Structural derived )
				return null;

			IdTplArgs? derivedSegment = receiverType.idTpls.LastOrDefault();
			if( derivedSegment == null || derived.TplParams.Count != derivedSegment.tplArgs.Count )
				return null;

			List<TplArg> derivedArgs = derivedSegment.tplArgs;

			foreach( BaseType baseType in derived.basetypes ) {
				Typespec concreteBase = TemplateInference.SubstituteTemplateParams(
					baseType.type, derived.TplParams, derivedArgs, result );

				if( concreteBase is not TypespecNested baseNested )
					continue;

				Decl? baseDecl = baseNested.resolvedDecl;
				if( baseDecl == null && result.TryGetResolved( baseNested, out Decl? bd ) )
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

				List<TplArg>? inherited = TryExtractFromBases( baseNested, ownerChain, result );
				if( inherited != null )
					return inherited;
			}

			return null;
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

		private static Typespec StripReferenceAndPointerWrappers( Typespec type )
		{
			Typespec current = type.Clone();

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

		public static bool AreConcrete( List<TplArg> args, List<TplParam> ownerParams )
		{
			HashSet<string> ownerNames = new( ownerParams.Select( p => p.name ) );
			return args.All( a => a.typespec != null && !ContainsTemplateParameterName( a.typespec, ownerNames ) );
		}

		/// <summary>
		/// Derives the enclosing class's parameter values for a member of a
		/// (partial or pattern) specialization: concrete positions map the use
		/// argument directly, pattern positions unify the written pattern with
		/// the use argument (the remainder deduces the parameter), open
		/// positions map the use argument. Returns null when a pattern fails
		/// to unify.
		/// </summary>
		public static Dictionary<string, Typespec>? DeriveSpecParamValues(
			Structural           enclosing,
			List<TplArg>         ownerArgs,
			ResolutionResult     result )
		{
			Decl.SpecData info = enclosing.SpecInfo!;
			List<TplParam> primaryParams = ( (ITemplatedDecl)info.Primary ).TplParams;

			if( ownerArgs.Count < info.Bound.Length )
				return null;

			Dictionary<string, Typespec> values = new( StringComparer.Ordinal );
			for( int i = 0; i < info.Bound.Length; i++ ) {
				Typespec? argType = ownerArgs[ i ].typespec;
				if( argType == null )
					continue;

				bool isPattern = i < info.Pattern.Length && info.Pattern[ i ];
				if( isPattern ) {
					Typespec pattern = info.Args[ i ].typespec!;
					if( pattern is not TypespecNested pn || pn.ptrs == null
					 || argType.ptrs == null || argType.ptrs.Count != pn.ptrs.Count )
						return null;

					for( int d = 0; d < pn.ptrs.Count; d++ ) {
						if( pn.ptrs[ d ].kind != argType.ptrs[ d ].kind )
							return null;
					}

					Typespec deduced = argType.Clone();
					deduced.ptrs = null;
					values[ primaryParams[ i ].name ] = deduced;
				}
				else {
					values[ primaryParams[ i ].name ] = argType;
				}
			}

			return values;
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
	}
}