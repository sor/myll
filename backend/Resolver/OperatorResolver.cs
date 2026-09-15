using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Resolves class/struct operator overloads and computes the substituted return type
	/// for a given receiver type. Shared between <see cref="NameResolver"/> and
	/// <see cref="TypeResolver"/> so the resolution and type-checking phases agree.
	/// </summary>
	public static class OperatorResolver
	{
		public static Func? LookupOperatorOverload(
			Structural          structural,
			string              name,
			bool                receiverIsConst,
			HashSet<Structural> visited,
			ResolutionResult    result )
		{
			if( !visited.Add( structural ) )
				return null;

			Func? strict   = null;
			Func? fallback = null;

			foreach( Decl decl in structural.children ) {
				if( decl is not Func func || func.name != name )
					continue;

				bool isConst = func.IsPure;
				if( receiverIsConst ) {
					if( isConst )
						strict ??= func;
				}
				else {
					if( !isConst )
						strict ??= func;
					fallback ??= func;
				}
			}

			if( strict != null )
				return strict;
			if( fallback != null )
				return fallback;

			foreach( BaseType bt in structural.basetypes ) {
				if( bt.type is not TypespecNested nested )
					continue;

				Decl? baseDecl = nested.resolvedDecl
				              ?? ( result.TryGetResolved( nested, out Decl? d ) ? d : null );

				if( baseDecl is Structural baseStruct ) {
					Func? fromBase = LookupOperatorOverload(
						baseStruct, name, receiverIsConst, visited, result );
					if( fromBase != null )
						return fromBase;
				}
			}

			return null;
		}

		public static bool TryGetOperatorReturnType(
			Typespec           receiverType,
			string             name,
			ResolutionResult   result,
			out Typespec?      returnType )
		{
			returnType = null;

			if( receiverType is not TypespecNested nested
			 || nested.ptrs is { Count: > 0 }
			 || nested.resolvedDecl is not Structural structural )
				return false;

			bool receiverIsConst = ( nested.qual & Qualifier.Const ) != 0;
			Func? op = LookupOperatorOverload(
				structural, name, receiverIsConst, new HashSet<Structural>(), result );
			if( op == null )
				return false;

			returnType = SubstituteMethodReturnType( op, receiverType, result ) ?? op.retType;
			return returnType != null;
		}

		public static Typespec? SubstituteMethodReturnType(
			Func             method,
			Typespec         receiverType,
			ResolutionResult result )
		{
			Structural? owner = FindEnclosingStructural( method.scope ) as Structural;
			if( owner == null || owner.TplParams.Count == 0 )
				return null;

			if( receiverType is not TypespecNested nested )
				return null;

			List<TplArg>? args = nested.idTpls.LastOrDefault()?.tplArgs;
			if( args == null || args.Count < owner.TplParams.Count )
				return null;

			if( !UsesClassTplParam( method.retType, owner.TplParams, result ) )
				return null;

			foreach( TplArg arg in args ) {
				if( arg.typespec is TypespecNested argNested && argNested.resolvedDecl == null
				 && result.TryGetResolved( argNested, out Decl? decl ) )
					argNested.resolvedDecl = decl;
			}

			return TemplateInference.SubstituteTemplateParams( method.retType, owner.TplParams, args, result );
		}

		private static bool UsesClassTplParam(
			Typespec         type,
			List<TplParam>   parameters,
			ResolutionResult result )
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
						if( arg.typespec != null
						 && UsesClassTplParam( arg.typespec, parameters, result ) )
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
	}
}
