using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	public enum TemplateArgMismatchKind
	{
		None,
		Arity,
		ArgumentKind,
	}

	public readonly struct TemplateArgValidation
	{
		public readonly bool                    IsMismatch;
		public readonly TemplateArgMismatchKind MismatchKind;
		public readonly int?                    MismatchIndex;
		public readonly string?                 ParameterName;
		public readonly string?                 Expected;
		public readonly string?                 Actual;

		public TemplateArgValidation(
			bool                    isMismatch   = false,
			TemplateArgMismatchKind mismatchKind = TemplateArgMismatchKind.None,
			int?                    mismatchIndex = null,
			string?                 parameterName = null,
			string?                 expected      = null,
			string?                 actual        = null )
		{
			IsMismatch    = isMismatch;
			MismatchKind  = mismatchKind;
			MismatchIndex = mismatchIndex;
			ParameterName = parameterName;
			Expected      = expected;
			Actual        = actual;
		}
	}

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

		// Specializations carry only their unbound parameters; template argument
		// validation always targets the primary.
		private static Decl PrimaryOf( Decl decl )
			=> decl.SpecInfo != null ? decl.SpecInfo.Primary : decl;

		public static int GetTemplateParamCount( Decl decl )
			=> ( PrimaryOf( decl ) as ITemplatedDecl )?.TplParams.Count ?? 0;

		/// <summary>
		/// Returns all template parameters in scope for a declaration: the template parameters
		/// of every enclosing class/struct/union/template-class, followed by the declaration's
		/// own template parameters. This is what should be substituted when ranking the
		/// parameters of a class-template method or constructor.
		/// </summary>
		public static List<TplParam> GetAllTemplateParams( Decl decl )
		{
			List<TplParam> own = ( decl as ITemplatedDecl )?.TplParams
				                 ?? new List<TplParam>();

			List<TplParam> enclosing = new();
			// Start at the parent scope so the declaration's own parameters are not
			// collected twice as "enclosing" template parameters.
			for( ScopeLeaf? cur = decl.scope?.parent; cur != null; cur = cur.parent ) {
				if( cur.decl is ITemplatedDecl td && td.TplParams.Count > 0 )
					enclosing.AddRange( td.TplParams );
			}
			enclosing.Reverse();

			List<TplParam> result = new( enclosing.Count + own.Count );
			result.AddRange( enclosing );
			result.AddRange( own );
			return result;
		}

		public static TemplateArgValidation ValidateTemplateArgs(
			Decl                   candidate,
			IReadOnlyList<TplArg>? explicitArgs )
		{
			List<TplParam>? parameters = ( PrimaryOf( candidate ) as ITemplatedDecl )?.TplParams;
			int expected = parameters?.Count ?? 0;
			int actual   = explicitArgs?.Count ?? 0;

			if( expected == 0 )
				return actual > 0
					? new TemplateArgValidation( true, TemplateArgMismatchKind.Arity, null, null, null, null )
					: new TemplateArgValidation();

			if( actual == 0 ) {
				// No explicit args on a template candidate: deduction may fill them in.
				// Only consider this a mismatch if deduction is impossible (non-function templates).
				return candidate is Func
					? new TemplateArgValidation()
					: new TemplateArgValidation( true, TemplateArgMismatchKind.Arity, null, null, null, null );
			}

			// A pack parameter absorbs the trailing arguments; it must be the
			// last parameter, so everything before it is fixed. parameters is
			// non-null here: expected > 0 only when the list exists.
			bool hasPack = parameters!.Any( p => p.isPack );
			int  fixedCount = hasPack ? expected - 1 : expected;

			if( ( hasPack && actual < fixedCount )
			 || ( !hasPack && actual != expected ) )
				return new TemplateArgValidation( true, TemplateArgMismatchKind.Arity, null, null, null, null );

			for( int i = 0; i < parameters.Count; i++ ) {
				TplParam param = parameters[i];

				if( param.isPack ) {
					// the pack consumes the remaining arguments
					for( int a = i; a < actual; a++ ) {
						if( explicitArgs![a].IsTypeArgument )
							continue;

						return new TemplateArgValidation(
							true, TemplateArgMismatchKind.ArgumentKind, a, param.name,
							"type", explicitArgs[a].IsValueArgument ? "value" : "unknown" );
					}
					break;
				}

				TplArg arg   = explicitArgs![i];
				bool isTypeArg  = arg.IsTypeArgument;
				bool isValueArg = arg.IsValueArgument;

				switch( param.kind ) {
					case TplParamKind.Type when !isTypeArg:
						return new TemplateArgValidation(
							true, TemplateArgMismatchKind.ArgumentKind, i, param.name,
							"type", isValueArg ? "value" : "unknown" );

					case TplParamKind.NonType when !isValueArg:
						return new TemplateArgValidation(
							true, TemplateArgMismatchKind.ArgumentKind, i, param.name,
							"non-type value", isTypeArg ? "type" : "unknown" );
				}
			}

			return new TemplateArgValidation();
		}

		/// <summary>
		/// Index of the trailing parameter that expands a pack template
		/// parameter (`<T...>`), or -1 when the list has no pack. Name-based:
		/// deduction runs before parameter types resolve.
		/// </summary>
		public static int PackParamIndex( List<TplParam> tplParams, List<Param> paras )
		{
			if( tplParams == null || paras == null || tplParams.Count == 0 )
				return -1;

			List<string> packNames = tplParams.Where( p => p.isPack ).Select( p => p.name ).ToList();
			if( packNames.Count == 0 )
				return -1;

			for( int i = paras.Count - 1; i >= 0; i-- ) {
				if( paras[i].type is TypespecNested n
				 && n.idTpls.Count == 1
				 && ( n.idTpls[0].tplArgs == null || n.idTpls[0].tplArgs.Count == 0 )
				 && packNames.Contains( n.idTpls[0].id ) )
					return i;
			}

			return -1;
		}

		public static int PackParamIndexOf( Decl decl )
			=> decl switch {
				Func f     => PackParamIndex( f.TplParams, f.paras ),
				Structor c => PackParamIndex( c.TplParams, c.paras ),
				_          => -1,
			};

		public enum CtadResult { Deduced, Ambiguous, Failed }

		/// <summary>
		/// CTAD: deduces a class template's parameters from a constructor
		/// call's arguments. Every constructor is an implicit deduction guide
		/// (C++ model): its own template parameters join the class's, and the
		/// class arguments are the deduced prefix. Ambiguous guides (viable
		/// constructors deducing different class arguments) report their
		/// variants through <paramref name="deductions"/> for the diagnostic.
		/// </summary>
		public static CtadResult TryDeduceClassTemplateArgs(
			Structural           structural,
			FuncCall             call,
			TypeResolver         typeResolver,
			ResolutionResult     result,
			out List<TplArg>     classArgs,
			out List<string>     deductions )
		{
			classArgs = new List<TplArg>();
			deductions = new List<string>();

			bool anyViable = false;
			List<TplArg>? best = null;

			foreach( Structor ctor in structural.children.OfType<Structor>()
				.Where( c => c.kind == Structor.Kind.Constructor && c.SpecInfo == null ) ) {
				List<TplParam> combined = new( structural.TplParams );
				combined.AddRange( ctor.TplParams );

				if( !TryDeduceTemplateArgs( combined, ctor.paras, call, typeResolver, result,
					out List<TplArg>? deduced ) || deduced == null )
					continue;

				anyViable = true;
				List<TplArg> ctorClassArgs = deduced
					.Take( structural.TplParams.Count )
					.ToList();
				string variant = ctorClassArgs.Select( a => a.Gen() ).Join( "," );
				if( !deductions.Contains( variant ) )
					deductions.Add( variant );

				best ??= ctorClassArgs;
			}

			if( !anyViable )
				return CtadResult.Failed;
			if( deductions.Count > 1 )
				return CtadResult.Ambiguous;

			classArgs = best ?? new List<TplArg>();
			return CtadResult.Deduced;
		}

		public static bool HasTemplateArityMismatch(
			Decl                   candidate,
			IReadOnlyList<TplArg>? explicitArgs )
			=> ValidateTemplateArgs( candidate, explicitArgs ).IsMismatch;

		/// <summary>
		/// Tries to deduce the candidate's own template arguments after first substituting any
		/// concrete enclosing (owner-class) template arguments into the parameter types. This
		/// allows a method like <c>Outer&lt;T&gt;::Inner::store&lt;U&gt;(U x, T y)</c> to deduce
		/// <c>U</c> from <c>x</c> while treating <c>T</c> as already-known from the receiver.
		/// </summary>
		public static bool TryDeduceTemplateArgs(
			Func              candidate,
			FuncCall          call,
			TypeResolver      typeResolver,
			ResolutionResult  result,
			out List<TplArg>? deduced,
			List<TplParam>?   enclosingParameters = null,
			List<TplArg>?     enclosingArguments  = null )
			=> TryDeduceTemplateArgs( candidate.TplParams, candidate.paras, call, typeResolver, result,
				out deduced, enclosingParameters, enclosingArguments );

		public static bool TryDeduceTemplateArgs(
			Structor          candidate,
			FuncCall          call,
			TypeResolver      typeResolver,
			ResolutionResult  result,
			out List<TplArg>? deduced,
			List<TplParam>?   enclosingParameters = null,
			List<TplArg>?     enclosingArguments  = null )
			=> TryDeduceTemplateArgs( candidate.TplParams, candidate.paras, call, typeResolver, result,
				out deduced, enclosingParameters, enclosingArguments );

		internal static bool TryDeduceTemplateArgs(
			List<TplParam>    tplParams,
			List<Param>       paras,
			FuncCall          call,
			TypeResolver      typeResolver,
			ResolutionResult  result,
			out List<TplArg>? deduced,
			List<TplParam>?   enclosingParameters = null,
			List<TplArg>?     enclosingArguments  = null )
		{
			deduced = null;

			if( tplParams.Count == 0 )
				return true;

			// Non-type parameters can not be deduced from call arguments; they must be
			// supplied explicitly. Reject mixed parameter lists immediately.
			if( tplParams.Any( p => p.kind == TplParamKind.NonType ) )
				return false;

			// A pack parameter (`<T...>`) absorbs the remaining call arguments;
			// everything before its parameter is fixed. Name-based, since
			// deduction runs before parameter types resolve.
			int packIndex = PackParamIndex( tplParams, paras );
			int fixedParas = packIndex >= 0 ? packIndex : paras.Count;

			if( call.args.Count != fixedParas
			 && ( packIndex < 0 || call.args.Count < fixedParas ) )
				return false;

			// The pack binds to the trailing argument types (an empty pack is fine)
			List<Typespec>? packTypes = null;
			if( packIndex >= 0 && call.args.Count > fixedParas ) {
				packTypes = new List<Typespec>( call.args.Count - fixedParas );
				for( int i = fixedParas; i < call.args.Count; i++ ) {
					Typespec? argType = typeResolver.Resolve( call.args[i].expr );
					if( argType == null )
						return false;

					packTypes.Add( argType );
				}
			}

			bool substituteEnclosing = enclosingParameters is { Count: > 0 }
			                        && enclosingArguments  is { Count: > 0 };

			var bindings = new Dictionary<string, Typespec>( StringComparer.Ordinal );

			for( int i = 0; i < fixedParas; i++ ) {
				Typespec? argType = typeResolver.Resolve( call.args[i].expr );
				if( argType == null )
					return false;

				Typespec paramType = paras[i].type;
				if( substituteEnclosing )
					paramType = SubstituteTemplateParams( paramType, enclosingParameters!, enclosingArguments!, result );

				if( !TryBindParameter( paramType, argType, bindings, result ) )
					return false;
			}

			if( bindings.Count != tplParams.Count - ( packIndex >= 0 ? 1 : 0 ) )
				return false;

			deduced = tplParams
				.Select( p => p.isPack
					? new TplArg {
						packArgs = ( packTypes ?? new List<Typespec>() )
							.Select( t => new TplArg { typespec = NormalizeDeducedType( t ).Clone( true ) } )
							.ToList(),
					}
					: new TplArg { typespec = NormalizeDeducedType( bindings[p.name] ).Clone( true ) } )
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
			for( int i = 0; i < Math.Min( parameters.Count, args.Count ); i++ ) {
				if( args[i].typespec != null )
					substitution[parameters[i].name] = args[i].typespec!;
			}

			return Substitute( type, substitution, result ) ?? type;
		}

		private static Typespec? Substitute( Typespec type, Dictionary<string, Typespec> substitution, ResolutionResult result )
		{
			Typespec CloneAndRecurseBase()
			{
				return type.Clone( true );
			}

			Decl? resolved = type is TypespecNested nestedType
				? GetResolvedDecl( nestedType, result )
				: null;

			if( resolved is TplParamDecl tpl
			 && substitution.TryGetValue( tpl.name, out Typespec? replacement ) ) {
				Typespec ret = Substitute( replacement, substitution, result )
				            ?? replacement.Clone( true );
				ret.qual = CombineQualifiers( ret.qual, type.qual );
				if( type.ptrs != null && type.ptrs.Count > 0 ) {
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
		ResolutionResult?             result )
	{
		Decl? parameterDecl = parameterType is TypespecNested nested
			? GetResolvedDecl( nested, result )
			: null;

		if( parameterDecl is not TplParamDecl tpl )
			return true; // no template parameter to bind; let overload resolution decide

		Typespec strippedArgument = argumentType;
		bool wrappersMatched = false;

		// Strip matching outer pointer/reference/smart-pointer wrappers.
		if( parameterType.ptrs is { Count: > 0 } pPtrs
		 && argumentType.ptrs is { Count: > 0 } aPtrs
		 && pPtrs.Count == aPtrs.Count ) {
			wrappersMatched = true;
			for( int i = 0; i < pPtrs.Count; i++ ) {
				if( pPtrs[i].kind != aPtrs[i].kind || pPtrs[i].qual != aPtrs[i].qual ) {
					wrappersMatched = false;
					break;
				}
			}

			if( wrappersMatched )
				strippedArgument = StripOutermostPointers( argumentType, aPtrs.Count );
		}

		if( !wrappersMatched && parameterType.ptrs is { Count: > 0 } ) {
			// A reference wrapper around the template parameter can bind to an
			// argument with different or no outer wrappers; in that case T is
			// the argument type itself and the C++ compiler decides whether the
			// reference can be formed (e.g. const T& to a temporary is fine).
			if( parameterType.ptrs.Count == 1 && IsReferenceKind( parameterType.ptrs[0].kind ) )
				strippedArgument = argumentType;
			else
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

	private static bool IsReferenceKind( Pointer.Kind kind )
		=> kind is Pointer.Kind.LVRef or Pointer.Kind.RVRef;

		/// <summary>
		/// Deduces template parameters of a conversion operator from a required
		/// target type: the declared return type is the pattern and the target
		/// the concrete type. Returns true when every parameter got bound.
		/// Mirrors the C++ rule for conversion function templates, whose
		/// parameters are deduced from the target (their specializations are
		/// invisible to normal lookup).
		/// </summary>
		public static bool TryDeduceTemplateArgsAgainst( Typespec pattern, List<TplParam> tplParams, Typespec concrete )
		{
			Dictionary<string, Typespec> bindings = new( StringComparer.Ordinal );
			BindTypePattern( pattern, concrete, bindings );
			return tplParams.All( p => bindings.ContainsKey( p.name ) );
		}

		private static void BindTypePattern( Typespec pattern, Typespec concrete, Dictionary<string, Typespec> bindings )
		{
			TryBindParameter( pattern, concrete, bindings, null );
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

			if( basic.kind == TypespecBasic.Kind.String )
				return new TypespecBasic {
					kind = TypespecBasic.Kind.Char,
					size = 1,
					qual = Qualifier.Const,
					ptrs = new List<Pointer> { new() { kind = Pointer.Kind.RawPtr } },
					srcPos = basic.srcPos,
				};

			return type;
		}
		private static Decl? GetResolvedDecl( TypespecNested type, ResolutionResult? result )
		{
			if( type.resolvedDecl != null )
				return type.resolvedDecl;

			return result?.TryGetResolved( type, out Decl? decl ) == true ? decl : null;
		}

		private static Typespec StripOutermostPointers( Typespec source, int count )
		{
			Typespec ret = source.Clone();
			ret.ptrs = source.ptrs?.Take( source.ptrs.Count - count ).ToList();
			return ret;
		}

		private static bool IsSameType( Typespec a, Typespec b )
			=> ConversionRules.IsExactMatch( a, b );
	}
}
