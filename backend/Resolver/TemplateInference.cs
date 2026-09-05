using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Validation and basic type deduction for explicit template arguments.
	/// </summary>
	public static class TemplateInference
	{
		public static IReadOnlyList<TplArg>? GetExplicitTemplateArgs( Expr callee )
		{
			return callee switch {
				IdExpr     id                                        => id.idTplArgs.tplArgs,
				ScopedExpr scoped when scoped.idTpls.Count > 0       => scoped.idTpls.Last().tplArgs,
				BinOp      binOp  when binOp.right is IdExpr member  => member.idTplArgs.tplArgs,
				_                                                    => null,
			};
		}

		public static int GetTemplateParamCount( Decl decl )
			=> decl switch {
				Func       func        => func.TplParams.Count,
				Structural structural  => structural.TplParams.Count,
				_                      => 0,
			};

		public static bool HasTemplateArityMismatch(
			Decl                   candidate,
			IReadOnlyList<TplArg>? explicitArgs )
		{
			int expected = GetTemplateParamCount( candidate );
			int actual   = explicitArgs?.Count ?? 0;

			if( expected == 0 )
				return actual > 0;

			if( actual == 0 ) {
				// No explicit args on a template candidate: deduction may fill them in.
				// Only consider this a mismatch if deduction is impossible (non-function templates).
				return candidate is not Func;
			}

			return expected != actual;
		}

		/// <summary>
		/// Tries to deduce template arguments for a function template from the call arguments.
		/// Only handles simple patterns: T, T*, T&, T[] and other pointer-like wrappers around T.
		/// </summary>
		public static bool TryDeduceTemplateArgs(
			Func              candidate,
			FuncCall          call,
			TypeResolver      typeResolver,
			ResolutionResult  result,
			out List<TplArg>? deduced )
		{
			deduced = null;

			if( candidate.TplParams.Count == 0 )
				return true;

			if( call.args.Count != candidate.paras.Count )
				return false;

			var bindings = new Dictionary<string, Typespec>( StringComparer.Ordinal );

			for( int i = 0; i < candidate.paras.Count; i++ ) {
				Typespec? argType = typeResolver.Resolve( call.args[i].expr );
				if( argType == null )
					return false;

				if( !TryBindParameter( candidate.paras[i].type, argType, bindings, result ) )
					return false;
			}

			if( bindings.Count != candidate.TplParams.Count )
				return false;

			deduced = candidate.TplParams
				.Select( p => new TplArg { typespec = CloneTypespec( NormalizeDeducedType( bindings[p.name] ) ) } )
				.ToList();
			return true;
		}

		public static void ApplyTemplateArgs( Expr callee, List<TplArg> args )
		{
			switch( callee ) {
				case IdExpr id:
					id.idTplArgs.tplArgs = args;
					break;

				case ScopedExpr scoped when scoped.idTpls.Count > 0:
					scoped.idTpls.Last().tplArgs = args;
					break;

				case BinOp binOp when binOp.right is IdExpr member:
					member.idTplArgs.tplArgs = args;
					break;
			}
		}

		/// <summary>
		/// Returns a copy of <paramref name="type"/> with every occurrence of a template parameter
		/// from <paramref name="parameters"/> replaced by the matching argument from <paramref name="args"/>.
		/// </summary>
		public static Typespec SubstituteTemplateParams(
			Typespec         type,
			List<TplParam>   parameters,
			List<TplArg>     args,
			ResolutionResult result )
		{
			if( parameters.Count == 0 || args.Count == 0 )
				return type;

			var substitution = new Dictionary<string, Typespec>( StringComparer.Ordinal );
			for( int i = 0; i < Math.Min( parameters.Count, args.Count ); i++ )
				substitution[parameters[i].name] = args[i].typespec!;

			return Substitute( type, substitution, result ) ?? type;
		}

		private static Typespec? Substitute( Typespec type, Dictionary<string, Typespec> substitution, ResolutionResult result )
		{
			Typespec CloneAndRecurseBase()
			{
				Typespec ret = CloneTypespecBase( type );
				ret.srcPos = type.srcPos;
				ret.qual   = type.qual;
				if( type.ptrs != null ) {
				ret.ptrs = type.ptrs
					.Select( p => new Pointer {
						kind = p.kind,
						qual = p.qual,
						expr = p.expr,
					} )
					.ToList();
				}
				return ret;
			}

			Decl? resolved = type is TypespecNested nestedType
				? GetResolvedDecl( nestedType, result )
				: null;

			if( resolved is TplParamDecl tpl
			 && substitution.TryGetValue( tpl.name, out Typespec? replacement ) ) {
				Typespec ret = Substitute( replacement, substitution, result ) ?? replacement; // expand nested args
				ret.qual = CombineQualifiers( ret.qual, type.qual );
				if( type.ptrs != null && type.ptrs.Count > 0 ) {
					ret = CloneTypespecBase( ret );
					ret.ptrs = (ret.ptrs ?? new List<Pointer>())
						.Concat( type.ptrs.Select( p => new Pointer { kind = p.kind, qual = p.qual, expr = p.expr } ) )
						.ToList();
				}
				return ret;
			}

			if( type is TypespecBasic )
				return CloneAndRecurseBase();

			if( type is TypespecNested n ) {
				Typespec ret = CloneAndRecurseBase();
				if( ret is TypespecNested rn ) {
					rn.idTpls = n.idTpls
						.Select( it => new IdTplArgs {
							id = it.id,
							tplArgs = it.tplArgs == null
								? new List<TplArg>()
								: it.tplArgs
									.Select( a => new TplArg {
										typespec = a.typespec == null
											? null
											: Substitute( a.typespec, substitution, result ),
									} )
									.ToList(),
						} )
						.ToList();
				}
				return ret;
			}

			if( type is TypespecFunc f ) {
				TypespecFunc ret = (TypespecFunc)CloneAndRecurseBase();
				if( f.retType != null )
					ret.retType = Substitute( f.retType, substitution, result ) ?? f.retType;
				if( f.paras != null ) {
					ret.paras = f.paras
						.Select( p => new Param {
							name = p.name,
							type = Substitute( p.type, substitution, result ) ?? p.type,
						} )
						.ToList();
				}
				return ret;
			}

			return null;
		}

		private static Qualifier CombineQualifiers( Qualifier a, Qualifier b )
			=> b == Qualifier.None ? a : b;

		/// <summary>
		/// Returns true when the underlying type is or contains a template parameter.
		/// Pointer/reference wrappers do not remove dependence.
		/// </summary>
		public static bool IsDependentType( Typespec? type )
		{
			if( type == null )
				return false;

			if( type is not TypespecNested nested )
				return false;

			if( nested.resolvedDecl is TplParamDecl )
				return true;

			foreach( IdTplArgs segment in nested.idTpls ) {
				if( segment.tplArgs != null
				 && segment.tplArgs.Any( a => a.typespec != null && IsDependentType( a.typespec ) ) )
					return true;
			}

			return false;
		}
		private static bool TryBindParameter(
			Typespec                      parameterType,
			Typespec                      argumentType,
			Dictionary<string, Typespec>  bindings,
			ResolutionResult              result )
		{
			Decl? parameterDecl = parameterType is TypespecNested nested
				? GetResolvedDecl( nested, result )
				: null;

			if( parameterDecl is not TplParamDecl tpl )
				return true; // no template parameter to bind; let overload resolution decide

			Typespec strippedArgument = argumentType;

			// Strip matching outer pointer/reference/smart-pointer wrappers.
			if( parameterType.ptrs is { Count: > 0 } pPtrs
			 && argumentType.ptrs is { Count: > 0 } aPtrs
			 && pPtrs.Count == aPtrs.Count ) {
				for( int i = 0; i < pPtrs.Count; i++ ) {
					if( pPtrs[i].kind != aPtrs[i].kind || pPtrs[i].qual != aPtrs[i].qual )
						return true;
				}

				strippedArgument = StripOutermostPointers( argumentType, aPtrs.Count );
			}
			else if( parameterType.ptrs is { Count: > 0 } ) {
				return true; // wrappers differ; can't bind, let overload resolution decide
			}

			if( bindings.TryGetValue( tpl.name, out Typespec? existing ) ) {
				if( IsUntypedLiteral( strippedArgument ) )
					return true;

				if( IsUntypedLiteral( existing ) ) {
					bindings[tpl.name] = strippedArgument;
					return true;
				}

				return IsSameType( existing, strippedArgument );
			}

			bindings[tpl.name] = strippedArgument;
			return true;
		}

		private static bool IsUntypedLiteral( Typespec type )
			=> type is TypespecBasic b
			 && ( b.kind == TypespecBasic.Kind.UntypedInteger
			   || b.kind == TypespecBasic.Kind.UntypedFloat );

		private static Typespec NormalizeDeducedType( Typespec type )
		{
			if( type is not TypespecBasic basic )
				return type;

			if( basic.kind == TypespecBasic.Kind.UntypedInteger )
				return new TypespecBasic { kind = TypespecBasic.Kind.Integer, isDefaultSized = true, srcPos = basic.srcPos };

			if( basic.kind == TypespecBasic.Kind.UntypedFloat )
				return new TypespecBasic { kind = TypespecBasic.Kind.Float, isDefaultSized = true, srcPos = basic.srcPos };

			return type;
		}
		private static Decl? GetResolvedDecl( TypespecNested type, ResolutionResult result )
		{
			if( type.resolvedDecl != null )
				return type.resolvedDecl;

			return result.TryGetResolved( type, out Decl? decl ) ? decl : null;
		}

		private static Typespec StripOutermostPointers( Typespec source, int count )
		{
			Typespec ret = CloneTypespecBase( source );
			ret.ptrs = source.ptrs?.Take( source.ptrs.Count - count ).ToList();
			return ret;
		}

		private static bool IsSameType( Typespec a, Typespec b )
			=> ConversionRules.IsExactMatch( a, b );

		private static Typespec CloneTypespec( Typespec source )
		{
			Typespec ret = CloneTypespecBase( source );
			ret.srcPos = source.srcPos;
			ret.qual   = source.qual;
			ret.ptrs   = source.ptrs?.Select( p => new Pointer {
				kind = p.kind,
				qual = p.qual,
				expr = p.expr,
			} ).ToList();
			return ret;
		}

		private static Typespec CloneTypespecBase( Typespec source )
		{
			return source switch {
				TypespecBasic b => new TypespecBasic {
					kind             = b.kind,
					size             = b.size,
					align            = b.align,
					isDefaultSized   = b.isDefaultSized,
					literalText      = b.literalText,
				},
				TypespecNested n => new TypespecNested {
					resolvedDecl = n.resolvedDecl,
					idTpls       = n.idTpls
						.Select( it => new IdTplArgs { id = it.id, tplArgs = it.tplArgs } )
						.ToList(),
				},
				TypespecFunc f => new TypespecFunc {
					paras   = f.paras,
					retType = f.retType,
				},
				_ => throw new InvalidOperationException(
					String.Format( "unknown Typespec variant: {0}", source.GetType().Name ) ),
			};
		}
	}
}
