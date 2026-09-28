using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Describes the implicit conversion needed to use a source type where a target type is expected.
	/// </summary>
	public enum ConversionRank
	{
		Exact,
		Promotion,
		Conversion,
		TemplateParameter,
		None,
	}

	public static class ConversionRules
	{
		/// <summary>
		/// Returns the conversion rank from <paramref name="source"/> to <paramref name="target"/>.
		/// Only exact matches and safe promotions are allowed by default:
		///   - exact types,
		///   - integer to wider integer of the same signedness,
		///   - float to wider float,
		///   - untyped integer literals to any fitting integer or float type,
		///   - untyped float literals to any float type,
		/// No narrowing, and no implicit typed integer/float or signed/unsigned conversions.
		/// Some of these defaults (e.g. int/float cross-conversion) may later become dialect flags.
		/// </summary>
		public static ConversionRank GetRank(
			Typespec?         source,
			Typespec?         target,
			ResolutionResult? result = null,
			bool              sourceIsLValue = false,
			bool              allowArrayDecay = false )
		{
			if( source == null || target == null )
				return ConversionRank.None;

		source = UnwrapAlias( source, result );
		target = UnwrapAlias( target, result );

			// Template parameters are opaque type variables. A value of a template
			// parameter type can bind to any target and vice versa, but this is weaker
			// than exact/conversion matches between concrete types so that concrete
			// overloads are preferred over template overloads when both are viable.
			if( source is TypespecNested { resolvedDecl: TplParamDecl }
			 || target is TypespecNested { resolvedDecl: TplParamDecl } )
				return ConversionRank.TemplateParameter;

			// The `null` literal is represented as ExplicitAuto. Allow it to bind to any
			// raw or smart pointer kind, matching C++ nullptr behavior.
			if( source is TypespecBasic { kind: TypespecBasic.Kind.ExplicitAuto }
			 && IsNullablePointerType( target ) )
				return ConversionRank.Conversion;

			if( IsExactMatch( source, target ) )
				return ConversionRank.Exact;

			if( IsBitUnsignedInterchange( source, target ) )
				return ConversionRank.Exact;

			if( IsDefaultSizedFamilyConversion( source, target ) )
				return ConversionRank.Conversion;

			ConversionRank untypedRank = GetUntypedLiteralRank( source, target );
			if( untypedRank != ConversionRank.None )
				return untypedRank;

			if( IsPromotion( source, target ) )
				return ConversionRank.Promotion;

			// Qualifier conversion: e.g., int* -> const int*, int& -> const int&,
			// int -> const int. Only adding qualifiers is allowed; removing them is invalid.
			if( TryGetQualifierConversionRank( source, target, out ConversionRank qualifierRank ) )
				return qualifierRank;

			// Value -> reference parameter binding (e.g. passing std::ostream to std::ostream&
			// or a string literal to const char*&).
			if( TryStripReference( target, out Typespec? refBase ) ) {
				Pointer.Kind refKind = target.ptrs![target.ptrs.Count - 1].kind;

				// lvalues cannot bind to rvalue references (without an explicit move/forward).
				if( refKind == Pointer.Kind.RVRef && sourceIsLValue )
					return ConversionRank.None;

				ConversionRank innerRank = GetRank( source, refBase, result, sourceIsLValue );
				if( innerRank <= ConversionRank.Conversion ) {
					// Direct reference binding (lvalue -> lvalue-ref, rvalue -> rvalue-ref)
					// does not add cost beyond the inner conversion. Indirect binding (e.g.
					// rvalue -> const T&) always counts as Conversion.
					bool directBinding = ( sourceIsLValue && refKind == Pointer.Kind.LVRef )
					                  || ( !sourceIsLValue && refKind == Pointer.Kind.RVRef );
					return directBinding ? innerRank : ConversionRank.Conversion;
				}

				// Fall through: reference binding may still be possible via an inheritance
				// conversion handled later in this method.
			}

			// Reference -> value binding (e.g. assigning const path& to path, or int& to int).
			if( TryStripReference( source, out Typespec? sourceBase )
			 && GetRank( sourceBase, target, result ) <= ConversionRank.Conversion )
				return ConversionRank.Conversion;

			// Inheritance conversions: Derived -> Base&, Derived* -> Base*, Derived*! -> Base*!, etc.
			if( TryGetInheritanceConversionRank( source, target, result, out ConversionRank inheritanceRank ) )
				return inheritanceRank;

			// User-defined conversions: converting constructors and conversion operators.
			if( target is TypespecNested { resolvedDecl: Hierarchical targetHier }
			 && TryGetImplicitConversionCtor( targetHier, source ) )
				return ConversionRank.Conversion;

			if( source is TypespecNested { resolvedDecl: Hierarchical sourceHier }
			 && TryGetImplicitConversionOperator( sourceHier, target ) )
				return ConversionRank.Conversion;

			// Pointer-to-array conversions. T[N] decays to T[*], and T[*] is interchangeable
			// with T* at the C++ ABI level, so allow these conversions unconditionally.
			if( TryGetPointerToArrayConversion( source, target ) )
				return ConversionRank.Conversion;

			// Array/pointer decay equivalence. In C++ a function parameter declared
			// `T[]` is a pointer, and `&array[i]` has type `T*`; treat `T*` and `T[]`
			// (and nested forms) as convertible in argument/parameter contexts.
			if( allowArrayDecay && TryGetArrayDecayMatch( source, target ) )
				return ConversionRank.Conversion;

			// String literal to C-style string pointer.
			if( source is TypespecBasic { kind: TypespecBasic.Kind.String }
			 && IsCharPointer( target ) )
				return ConversionRank.Promotion;

			if( IsSafeIntegerConversion( source, target ) )
				return ConversionRank.Conversion;

			if( IsAllowedConversion( source, target ) )
				return ConversionRank.Conversion;

			return ConversionRank.None;
		}

		public static bool IsImplicitlyConvertible(
			Typespec?         source,
			Typespec?         target,
			ResolutionResult? result = null,
			bool              allowArrayDecay = false )
		{
			ConversionRank rank = GetRank( source, target, result, allowArrayDecay: allowArrayDecay );
			return rank <= ConversionRank.Conversion || rank == ConversionRank.TemplateParameter;
		}

		/// <summary>
		/// Lightweight, heuristic lvalue classification for overload ranking. This is an
		/// approximation: it treats typical addressable expressions (variables, dereferences,
		/// indexing, member access, string literals, `self`, and functions/methods that return
		/// a reference) as lvalues, and everything else as an rvalue. It will be replaced by
		/// a proper value-category field on <see cref="Expr"/> if/when full value-category
		/// tracking is implemented.
		/// </summary>
		public static bool IsLValue( Expr? expr )
		{
			while( expr is UnOp parens && parens.op == Operand.Parens )
				expr = parens.expr;

			return expr switch {
				IdExpr id       => id.idTplArgs.id is not ("null" or "true" or "false" or "this"),
				SelfExpr        => true,
				Literal lit     => lit.text.Length > 0 && lit.text[0] == '"',
				FuncCallExpr fc => fc.Type?.ptrs is { Count: > 0 }
			                    && fc.Type.ptrs[fc.Type.ptrs.Count - 1].kind == Pointer.Kind.LVRef,
				UnOp u          => u.op == Operand.Dereference
			                    || u.op == Operand.PreIncr
			                    || u.op == Operand.PreDecr,
				BinOp b         => b.op is Operand.MemberAccess
			                    or Operand.NCMemberAccess
			                    or Operand.MemberPtrAccess
			                    or Operand.MemberAccessPtr
			                    or Operand.NCMemberAccessPtr
			                    or Operand.MemberPtrAccessPtr
			                    or Operand.IndexCall
			                    or Operand.NCIndexCall,
				_               => false,
			};
		}

		/// <summary>
		/// Resolves a nested type through the AST field if already applied, otherwise falls back
		/// to the identity-based map held by the current resolution pass.
		/// </summary>
		public static Decl? GetResolvedDecl( TypespecNested nested, ResolutionResult? result )
		{
			if( nested.resolvedDecl != null )
				return nested.resolvedDecl;
			if( result != null && result.TryGetResolved( nested, out Decl? decl ) )
				return decl;
			return null;
		}

		/// <summary>
		/// True when <paramref name="source"/> is a class/struct type derived from
		/// <paramref name="target"/>, considering the full base chain.
		/// </summary>
		public static bool IsDerivedFrom( TypespecNested source, TypespecNested target )
			=> IsDerivedFrom( source, target, null );

		private static bool IsDerivedFrom(
			TypespecNested    source,
			TypespecNested    target,
			ResolutionResult? result )
		{
			Decl? sourceDecl = GetResolvedDecl( source, result );
			Decl? targetDecl = GetResolvedDecl( target, result );

			if( sourceDecl == null || targetDecl == null )
				return false;

			if( sourceDecl == targetDecl )
				return false;

			return IsDerivedFrom( sourceDecl, targetDecl, new HashSet<Decl>(), result );
		}

		private static bool IsDerivedFrom(
			Decl              derived,
			Decl              target,
			HashSet<Decl>     visited,
			ResolutionResult? result )
		{
			if( derived == target )
				return true;

			if( derived is not Structural structural )
				return false;

			if( !visited.Add( structural ) )
				return false;

			foreach( BaseType bt in structural.basetypes ) {
				Decl? baseDecl = bt.type is TypespecNested nested
					? GetResolvedDecl( nested, result )
					: null;
				if( baseDecl == null )
					continue;

				if( baseDecl == target )
					return true;

				if( IsDerivedFrom( baseDecl, target, visited, result ) )
					return true;
			}

			return false;
		}

		/// <summary>
		/// True when a value of a derived class type is being used where a base class value
		/// is expected. Myll treats this as object slicing and rejects it.
		/// </summary>
		public static bool IsSlicingAttempt(
			Typespec          source,
			Typespec          target,
			ResolutionResult? result = null )
		{
			if( source.ptrs is { Count: > 0 } )
				return false;

			if( target.ptrs is { Count: > 0 } )
				return false;

			if( source is not TypespecNested srcNest || target is not TypespecNested tgtNest )
				return false;

			return IsDerivedFrom( srcNest, tgtNest, result );
		}

		private static bool TryGetInheritanceConversionRank(
			Typespec           source,
			Typespec           target,
			ResolutionResult?  result,
			out ConversionRank rank )
		{
			rank = ConversionRank.None;

			// Reference binding: Derived -> Base& / Derived -> Base&&.
			if( target.ptrs is { Count: > 0 }
			 && TryStripReference( target, out Typespec? refBase )
			 && refBase is TypespecNested refBaseNest
			 && source is TypespecNested srcNest
			 && IsDerivedFrom( srcNest, refBaseNest, result ) ) {
				rank = ConversionRank.Conversion;
				return true;
			}

			// Pointer / smart-pointer conversion: Derived* -> Base*, Derived*! -> Base*!, ...
			if( source.ptrs == null || target.ptrs == null
			 || source.ptrs.Count != target.ptrs.Count
			 || source.ptrs.Count != 1 )
				return false;

			Pointer.Kind srcKind = source.ptrs[0].kind;
			Pointer.Kind tgtKind = target.ptrs[0].kind;

			if( !IsInheritancePointerKind( srcKind ) || srcKind != tgtKind )
				return false;

			Typespec sourceInner = CloneTypespecBase( source );
			sourceInner.ptrs = new();

			Typespec targetInner = CloneTypespecBase( target );
			targetInner.ptrs = new();

			TypespecNested? sourceNested = sourceInner as TypespecNested;
			TypespecNested? targetNested = targetInner as TypespecNested;
			if( sourceNested != null
			 && targetNested != null
			 && IsDerivedFrom( sourceNested, targetNested, result ) ) {
				rank = ConversionRank.Conversion;
				return true;
			}

			return false;
		}

		private static bool IsInheritancePointerKind( Pointer.Kind kind )
			=> kind is Pointer.Kind.RawPtr
			    or Pointer.Kind.Unique
			    or Pointer.Kind.Shared
			    or Pointer.Kind.Weak;

		private static bool IsNullablePointerType( Typespec? type )
		{
			if( type?.ptrs is not { Count: > 0 } )
				return false;

			return type.ptrs[type.ptrs.Count - 1].kind is
				   Pointer.Kind.RawPtr
				or Pointer.Kind.PtrToAry
				or Pointer.Kind.Unique
				or Pointer.Kind.UniqueArray
				or Pointer.Kind.Shared
				or Pointer.Kind.SharedArray
				or Pointer.Kind.Weak
				or Pointer.Kind.WeakArray;
		}

		public static bool IsExactMatch( Typespec? a, Typespec? b )
		{
			if( a == null || b == null )
				return false;

			return IsExactMatchCore( a, b );
		}

		private static bool IsExactMatchCore( Typespec source, Typespec target )
		{
			if( source.GetType() != target.GetType() )
				return false;

			if( source.qual != target.qual )
				return false;

			// Pointer/reference structure must match exactly, including the qualifier on
			// each pointer block. A qualifier mismatch is handled as a separate conversion.
			if( !HaveSamePointers( source.ptrs, target.ptrs ) )
				return false;

			return source switch {
				TypespecBasic srcBasic => target is TypespecBasic tgtBasic
				                       && srcBasic.kind == tgtBasic.kind
				                       && srcBasic.size == tgtBasic.size
				                       && srcBasic.isDefaultSized == tgtBasic.isDefaultSized,
				TypespecNested srcNest => target is TypespecNested tgtNest
				                       && NestedTypesEqual( srcNest, tgtNest ),
				TypespecFunc   srcFunc => false, // function pointer identity not yet supported
				_                      => false,
			};
		}

		private static ConversionRank GetUntypedLiteralRank( Typespec source, Typespec target )
		{
			if( source is not TypespecBasic srcBasic || target is not TypespecBasic tgtBasic )
				return ConversionRank.None;

			if( !HaveSamePointers( source.ptrs, target.ptrs ) )
				return ConversionRank.None;

			if( srcBasic.kind == TypespecBasic.Kind.UntypedInteger ) {
				// Prefer the exact constant value (parsed literal or folded compound
				// expression); fall back to parsing the original literal text.
				BigInteger value;
				if( srcBasic.literalValue is BigInteger folded )
					value = folded;
				else if( !TryParseInteger( srcBasic.literalText, out value ) )
					return ConversionRank.None;

				// An unannotated literal is naturally a platform int when it fits;
				// anything that does not fit cannot bind to int at all.
				if( tgtBasic.kind == TypespecBasic.Kind.Integer && ( tgtBasic.isDefaultSized || tgtBasic.size == 4 ) )
					return IntegerFits( value, TypespecBasic.Kind.Integer, 4 )
						? ConversionRank.Exact
						: ConversionRank.None;

				// Fits into another integer type? (value range check)
				if( ( tgtBasic.kind == TypespecBasic.Kind.Integer
				   || tgtBasic.kind == TypespecBasic.Kind.Unsigned
				   || tgtBasic.kind == TypespecBasic.Kind.Bitwise
				   || tgtBasic.kind == TypespecBasic.Kind.Byte )
				 && IntegerFits( value, tgtBasic.kind == TypespecBasic.Kind.Bitwise
					|| tgtBasic.kind == TypespecBasic.Kind.Byte
					? TypespecBasic.Kind.Unsigned
					: tgtBasic.kind, tgtBasic.size ) )
					return ConversionRank.Promotion;

				// Integer literal can be used to initialize a float.
				if( tgtBasic.kind == TypespecBasic.Kind.Float )
					return ConversionRank.Promotion;

				return ConversionRank.None;
			}

			if( srcBasic.kind == TypespecBasic.Kind.UntypedFloat ) {
				if( !TryParseFloat( srcBasic.literalText, out _ ) )
					return ConversionRank.None;

				// Default-sized `float` is the natural target for an unannotated literal.
				if( tgtBasic.kind == TypespecBasic.Kind.Float && tgtBasic.isDefaultSized )
					return ConversionRank.Exact;

				// The default float type for an unannotated literal is configured by the
				// dialect (falls back to f32). Any other concrete float size is accepted
				// via promotion.
				int defaultSize = Dialect.Default.DefaultFloatSize();
				if( tgtBasic.kind == TypespecBasic.Kind.Float && tgtBasic.size == defaultSize )
					return ConversionRank.Exact;

				if( tgtBasic.kind == TypespecBasic.Kind.Float )
					return ConversionRank.Promotion;

				return ConversionRank.None;
			}

			return ConversionRank.None;
		}

		private static bool IsPromotion( Typespec source, Typespec target )
		{
			// Promotion preserves pointer structure exactly; only the underlying scalar
			// width changes. Qualifier differences (e.g., binding to const) are allowed.
			if( !HaveSamePointers( source.ptrs, target.ptrs ) )
				return false;

			if( source is not TypespecBasic srcBasic || target is not TypespecBasic tgtBasic )
				return false;

			// Integer promotion: same signedness, target wider or equal.
			if( ( srcBasic.kind == TypespecBasic.Kind.Integer || srcBasic.kind == TypespecBasic.Kind.Unsigned )
			 && srcBasic.kind == tgtBasic.kind
			 && tgtBasic.size > srcBasic.size )
				return true;

			// Float promotion: target wider.
			if( srcBasic.kind == TypespecBasic.Kind.Float
			 && tgtBasic.kind == TypespecBasic.Kind.Float
			 && tgtBasic.size > srcBasic.size )
				return true;

			return false;
		}

		private static bool IsAllowedConversion( Typespec source, Typespec target )
		{
			// String literal to C-style string pointer.
			if( source is TypespecBasic { kind: TypespecBasic.Kind.String }
			 && IsCharPointer( target ) )
				return true;

			// String literal to the dialect's backing string class (e.g. std::string).
			if( source is TypespecBasic { kind: TypespecBasic.Kind.String }
			 && target is TypespecNested targetNest ) {
				if( targetNest.resolvedDecl is Hierarchical h && h.name == "string" )
					return true;

				// Declarations in prototype modules (e.g. .decl.myll) may not have their
				// parameter types fully resolved, but the textual name is still enough to
				// recognise the backing string type.
				if( targetNest.resolvedDecl == null
				 && targetNest.idTpls.Count > 0
				 && targetNest.idTpls[targetNest.idTpls.Count - 1].id == "string" )
					return true;
			}

			// Currently all other non-exact/non-promotion conversions are rejected.
			// This is where future safe conversions (e.g., derived-to-base pointers,
			// char32_t ↔ int32_t if desired) would go.
			return false;
		}

		private static bool IsSafeIntegerConversion( Typespec source, Typespec target )
		{
			if( !HaveSamePointers( source.ptrs, target.ptrs ) )
				return false;

			if( source is not TypespecBasic srcBasic || target is not TypespecBasic tgtBasic )
				return false;

			// Char is not a number in Myll and does not implicitly convert to/from integers.
			if( srcBasic.kind == TypespecBasic.Kind.Char || tgtBasic.kind == TypespecBasic.Kind.Char )
				return false;

			if( !IsIntegralKind( srcBasic.kind ) || !IsIntegralKind( tgtBasic.kind ) )
				return false;

			// bool -> any integer/unsigned is allowed.
			if( srcBasic.kind == TypespecBasic.Kind.Bool )
				return true;

			if( srcBasic.kind == tgtBasic.kind )
				return tgtBasic.size > srcBasic.size;

			if( srcBasic.kind == TypespecBasic.Kind.Integer && tgtBasic.kind == TypespecBasic.Kind.Unsigned )
				return tgtBasic.size > srcBasic.size;

			if( srcBasic.kind == TypespecBasic.Kind.Unsigned && tgtBasic.kind == TypespecBasic.Kind.Integer )
				return tgtBasic.size > srcBasic.size;

			return false;
		}

		private static bool IsIntegralKind( TypespecBasic.Kind kind )
			=> kind is TypespecBasic.Kind.Integer
			     or TypespecBasic.Kind.Unsigned
			     or TypespecBasic.Kind.Bool;

		private static bool IsDefaultSizedFamilyConversion( Typespec source, Typespec target )
		{
			if( !HaveSamePointers( source.ptrs, target.ptrs ) )
				return false;

			if( source is not TypespecBasic srcBasic || target is not TypespecBasic tgtBasic )
				return false;

			if( srcBasic.kind != tgtBasic.kind )
				return false;

			if( srcBasic.size != tgtBasic.size )
				return false;

			return srcBasic.isDefaultSized != tgtBasic.isDefaultSized;
		}

		private static bool IsBitUnsignedInterchange( Typespec source, Typespec target )
		{
			if( !HaveSamePointers( source.ptrs, target.ptrs ) )
				return false;

			if( source is not TypespecBasic srcBasic || target is not TypespecBasic tgtBasic )
				return false;

			if( srcBasic.size != tgtBasic.size )
				return false;

			return ( srcBasic.kind == TypespecBasic.Kind.Bitwise
			      && tgtBasic.kind == TypespecBasic.Kind.Unsigned )
			    || ( srcBasic.kind == TypespecBasic.Kind.Unsigned
			      && tgtBasic.kind == TypespecBasic.Kind.Bitwise );
		}

		private static bool IsCharPointer( Typespec type )
		{
			if( type.ptrs == null || type.ptrs.Count != 1 )
				return false;

			if( type.ptrs[0].kind != Pointer.Kind.RawPtr )
				return false;

			return type is TypespecBasic { kind: TypespecBasic.Kind.Char, size: 1 };
		}

		private static bool IntegerFits( BigInteger value, TypespecBasic.Kind kind, int size )
		{
			return kind switch {
				TypespecBasic.Kind.Integer => size switch {
					1 => FitsIn( value, sbyte.MinValue, sbyte.MaxValue ),
					2 => FitsIn( value, short.MinValue, short.MaxValue ),
					4 => FitsIn( value, int.MinValue, int.MaxValue ),
					8 => FitsIn( value, long.MinValue, long.MaxValue ),
					_ => false,
				},
				TypespecBasic.Kind.Unsigned => size switch {
					1 => FitsIn( value, 0, byte.MaxValue ),
					2 => FitsIn( value, 0, ushort.MaxValue ),
					4 => FitsIn( value, 0, uint.MaxValue ),
					8 => FitsIn( value, 0, ulong.MaxValue ),
					_ => false,
				},
				_ => false,
			};
		}

		private static bool FitsIn( BigInteger value, BigInteger min, BigInteger max )
			=> value >= min && value <= max;

		internal static bool TryParseInteger( string? text, out BigInteger value )
		{
			value = BigInteger.Zero;
			if( string.IsNullOrEmpty( text ) )
				return false;

			ReadOnlySpan<char> span = text.AsSpan();

			try {
				if( span.Length >= 2 && span[0] == '0' ) {
					if( span[1] == 'x' || span[1] == 'X' )
						return TryParseRadix( span[2..].ToString(), 16, out value );
					if( span[1] == 'o' || span[1] == 'O' )
						return TryParseRadix( span[2..].ToString(), 8, out value );
					if( span[1] == 'b' || span[1] == 'B' )
						return TryParseRadix( span[2..].ToString(), 2, out value );
				}

				value = BigInteger.Parse( span.ToString(), System.Globalization.NumberStyles.None );
				return true;
			}
			catch {
				return false;
			}
		}

		private static bool TryParseRadix( string digits, int radix, out BigInteger value )
		{
			value = BigInteger.Zero;
			if( digits.Length == 0 )
				return false;

			foreach( char c in digits ) {
				int digit = c switch {
					>= '0' and <= '9' => c - '0',
					>= 'a' and <= 'f' => c - 'a' + 10,
					>= 'A' and <= 'F' => c - 'A' + 10,
					_                 => -1,
				};
				if( digit < 0 || digit >= radix )
					return false;
				value = value * radix + digit;
			}

			return true;
		}

		private static bool TryParseFloat( string? text, out double value )
		{
			value = 0;
			if( string.IsNullOrEmpty( text ) )
				return false;

			return double.TryParse( text, System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out value );
		}

		public static bool TryStripReference( Typespec target, out Typespec? stripped )
		{
			stripped = null;
			if( target.ptrs == null || target.ptrs.Count == 0 )
				return false;

			Pointer last = target.ptrs[target.ptrs.Count - 1];
			if( last.kind != Pointer.Kind.LVRef && last.kind != Pointer.Kind.RVRef )
				return false;

			stripped = CloneTypespecBase( target );
			stripped.ptrs = target.ptrs.Take( target.ptrs.Count - 1 ).ToList();
			return true;
		}

		public static Typespec CloneTypespecBase( Typespec source )
		{
			Typespec ret = source switch {
				TypespecBasic b   => new TypespecBasic {
					kind           = b.kind,
					size           = b.size,
					isDefaultSized = b.isDefaultSized,
				},
				TypespecNested n  => new TypespecNested {
					resolvedDecl = n.resolvedDecl,
					isInitList   = n.isInitList,
					idTpls       = n.idTpls.Select( it => new IdTplArgs { id = it.id, tplArgs = it.tplArgs } ).ToList(),
				},
				TypespecFunc f    => new TypespecFunc {
					paras   = f.paras,
					retType = f.retType,
				},
				_                 => throw new InvalidOperationException( "unknown Typespec variant" ),
			};

			ret.srcPos = source.srcPos;
			ret.qual   = source.qual;
			ret.ptrs   = source.ptrs?.ToList();
			return ret;
		}

		private static bool HaveSamePointers( List<Pointer>? a, List<Pointer>? b )
		{
			if( a == null || a.Count == 0 )
				return b == null || b.Count == 0;
			if( b == null || b.Count == 0 )
				return false;
			if( a.Count != b.Count )
				return false;

			for( int i = 0; i < a.Count; i++ ) {
				if( a[i].kind != b[i].kind )
					return false;
				if( a[i].qual != b[i].qual )
					return false;
			}

			return true;
		}

		/// <summary>
		/// Returns true when the two types have the same base type ignoring any qualifiers
		/// on the base type or on pointer/reference blocks. Used by qualifier conversion.
		/// </summary>
		private static bool BaseTypesEqual( Typespec a, Typespec b )
		{
			if( a.GetType() != b.GetType() )
				return false;

			return a switch {
				TypespecBasic ab => b is TypespecBasic bb
				                 && ab.kind == bb.kind
				                 && ab.size == bb.size
				                 && ab.isDefaultSized == bb.isDefaultSized,
				TypespecNested an => b is TypespecNested bn
				                  && NestedTypesEqual( an, bn ),
				_ => false,
			};
		}

		/// <summary>
		/// Qualifier conversion is allowed when the base type and pointer structure are the
		/// same. For value types (no pointers), top-level const/mutable/volatile can be
		/// added or removed because the value is copied. For pointer/reference types, only
		/// adding qualifiers on the pointee is allowed (e.g., int* -> const int*);
		/// removing them (const int* -> int*) is rejected.
		/// </summary>
		private static bool TryGetQualifierConversionRank(
			Typespec           source,
			Typespec           target,
			out ConversionRank rank )
		{
			rank = ConversionRank.None;

			if( !HaveSamePointers( source.ptrs, target.ptrs ) )
				return false;
			if( !BaseTypesEqual( source, target ) )
				return false;
			if( source.qual == target.qual )
				return false;

			bool isValueType = source.ptrs == null || source.ptrs.Count == 0;
			if( isValueType ) {
				rank = ConversionRank.Conversion;
				return true;
			}

			if( ( source.qual & ~target.qual ) != 0 )
				return false;

			rank = ConversionRank.Conversion;
			return true;
		}

		private static bool NestedTypesEqual( TypespecNested a, TypespecNested b )
		{
			// If both types were resolved, compare the underlying declaration.
			if( a.resolvedDecl != null && b.resolvedDecl != null )
				return a.resolvedDecl == b.resolvedDecl;

			// Fall back to textual comparison of the name path.
			if( a.idTpls.Count != b.idTpls.Count )
				return false;

			for( int i = 0; i < a.idTpls.Count; i++ ) {
				if( a.idTpls[i].id != b.idTpls[i].id )
					return false;
				if( a.idTpls[i].tplArgs.Count != b.idTpls[i].tplArgs.Count )
					return false;
			}

			return true;
		}

		public static Typespec UnwrapAlias( Typespec type, ResolutionResult? result = null )
		{
			HashSet<AliasDecl> visited = new();
			while( type is TypespecNested useSite
			    && GetResolvedDecl( useSite, result ) is AliasDecl alias
			    && visited.Add( alias ) ) {
				if( TryExpandAliasTemplate( useSite, alias, result, out Typespec expanded ) ) {
					type = expanded;
					continue;
				}

				type = CloneTypespecBase( alias.type );
			}

			return type;
		}

		/// <summary>
		/// Expands an alias-template use like <c>Vec&lt;int&gt;</c> into the aliased type
		/// with template parameters substituted. Returns false for non-template aliases
		/// or for dependent uses where no arguments are supplied.
		/// </summary>
		public static bool TryExpandAliasTemplate(
			TypespecNested    useSite,
			AliasDecl         alias,
			ResolutionResult? result,
			out Typespec      expanded )
		{
			expanded = useSite;

			if( alias.TplParams.Count == 0 )
				return false;

			IReadOnlyList<TplArg>? providedArgs = useSite.idTpls.LastOrDefault()?.tplArgs;
			if( providedArgs == null || providedArgs.Count == 0 )
				return false;

			Dictionary<string, TplArg> paramMap = new( StringComparer.Ordinal );
			int limit = Math.Min( alias.TplParams.Count, providedArgs.Count );
			for( int i = 0; i < limit; i++ )
				paramMap[alias.TplParams[i].name] = providedArgs[i];

			expanded = SubstituteAliasParams( CloneTypespecDeep( alias.type ), paramMap, result );
			return true;
		}

		private static Typespec SubstituteAliasParams(
			Typespec                     type,
			Dictionary<string, TplArg>   paramMap,
			ResolutionResult?            result )
		{
			if( type is TypespecNested placeholder
			 && GetResolvedDecl( placeholder, result ) is TplParamDecl tp
			 && paramMap.TryGetValue( tp.name, out TplArg? replacement ) ) {
				if( replacement.typespec != null ) {
					Typespec ret = CloneTypespecDeep( replacement.typespec );
					ret.qual = CombineQualifiers( ret.qual, placeholder.qual );
					if( placeholder.ptrs != null && placeholder.ptrs.Count > 0 ) {
						ret.ptrs = ( ret.ptrs ?? new List<Pointer>() )
							.Concat( placeholder.ptrs.Select( p => new Pointer { kind = p.kind, qual = p.qual, expr = p.expr } ) )
							.ToList();
					}
					return ret;
				}

				return type;
			}

			if( type is not TypespecNested nested )
				return type;

			foreach( IdTplArgs segment in nested.idTpls ) {
				if( segment.tplArgs == null )
					continue;
				for( int i = 0; i < segment.tplArgs.Count; i++ ) {
					TplArg arg = segment.tplArgs[i];
					if( arg.typespec != null ) {
						arg.typespec = SubstituteAliasParams( arg.typespec, paramMap, result );
					}
					else if( arg.lit is IdExpr id
					      && GetResolvedDecl( id, result ) is TplParamDecl litTp
					      && paramMap.TryGetValue( litTp.name, out TplArg? litReplacement ) ) {
						segment.tplArgs[i] = new TplArg {
							typespec = litReplacement.typespec,
							lit      = litReplacement.lit,
						};
					}
				}
			}

			return type;
		}

		private static Decl? GetResolvedDecl( IdExpr id, ResolutionResult? result )
		{
			if( id.resolvedDecl != null )
				return id.resolvedDecl;
			if( result != null && result.TryGetResolved( id, out Decl? decl ) )
				return decl;
			return null;
		}

		private static Typespec CloneTypespecDeep( Typespec source )
		{
			Typespec ret = CloneTypespecBase( source );
			if( ret is TypespecNested rn && source is TypespecNested sn ) {
			rn.idTpls = sn.idTpls.Select( it => new IdTplArgs {
				id      = it.id,
				tplArgs = it.tplArgs?.Select( CloneTplArgDeep ).ToList() ?? new List<TplArg>(),
			} ).ToList();
			}
			return ret;
		}

		private static TplArg CloneTplArgDeep( TplArg source )
			=> new() {
				typespec = source.typespec == null ? null : CloneTypespecDeep( source.typespec ),
				lit      = source.lit,
			};

		private static Qualifier CombineQualifiers( Qualifier a, Qualifier b )
			=> a == Qualifier.None ? b : a;

		/// <summary>
		/// Allows implicit conversions along the decay chain T[N] -> T[*] -> T*.
		/// These are ABI-compatible in C++, so Myll permits them everywhere.
		/// </summary>
		private static bool TryGetPointerToArrayConversion( Typespec source, Typespec target )
		{
			if( source.ptrs == null || target.ptrs == null )
				return false;

			if( source.ptrs.Count == 0 || target.ptrs.Count == 0 )
				return false;

			Pointer.Kind srcOuter = source.ptrs[ ^1 ].kind;
			Pointer.Kind tgtOuter = target.ptrs[ ^1 ].kind;

			bool srcIsArray     = srcOuter == Pointer.Kind.RawArray;
			bool srcIsPtrToAry  = srcOuter == Pointer.Kind.PtrToAry;
			bool tgtIsPtrToAry  = tgtOuter == Pointer.Kind.PtrToAry;
			bool tgtIsRawPtr    = tgtOuter == Pointer.Kind.RawPtr;

			if( !(( srcIsArray && tgtIsPtrToAry ) || ( srcIsPtrToAry && tgtIsRawPtr )) )
				return false;

			// Normalize the outer pointer to RawPtr so exact-match/qualifier logic applies
			// to the same kind on both sides.
			Typespec modSource = CloneTypespecBase( source );
			modSource.ptrs = source.ptrs.ToList();
			Pointer srcPtr = modSource.ptrs[ modSource.ptrs.Count - 1 ];
			modSource.ptrs[ modSource.ptrs.Count - 1 ] = new() {
				kind  = Pointer.Kind.RawPtr,
				qual  = srcPtr.qual,
			};

			Typespec modTarget = CloneTypespecBase( target );
			modTarget.ptrs = target.ptrs.ToList();
			Pointer tgtPtr = modTarget.ptrs[ modTarget.ptrs.Count - 1 ];
			modTarget.ptrs[ modTarget.ptrs.Count - 1 ] = new() {
				kind  = Pointer.Kind.RawPtr,
				qual  = tgtPtr.qual,
			};

			if( IsExactMatch( modSource, modTarget ) )
				return true;

			return TryGetQualifierConversionRank( modSource, modTarget, out _ );
		}

		private static bool TryGetArrayDecayMatch( Typespec source, Typespec target )
		{
			if( source.ptrs == null || target.ptrs == null )
				return false;

			if( source.ptrs.Count == 0 || target.ptrs.Count == 0 )
				return false;

			Pointer.Kind srcOuter = source.ptrs[ ^1 ].kind;
			Pointer.Kind tgtOuter = target.ptrs[ ^1 ].kind;

			bool srcArray = srcOuter == Pointer.Kind.RawArray;
			bool tgtArray = tgtOuter == Pointer.Kind.RawArray;

			if( srcArray == tgtArray )
				return false;

			Typespec modSource = CloneTypespecBase( source );
			modSource.ptrs = source.ptrs.ToList();
			modSource.ptrs[ modSource.ptrs.Count - 1 ] = new() { kind = Pointer.Kind.RawPtr };

			Typespec modTarget = CloneTypespecBase( target );
			modTarget.ptrs = target.ptrs.ToList();
			modTarget.ptrs[ modTarget.ptrs.Count - 1 ] = new() { kind = Pointer.Kind.RawPtr };

			return IsExactMatch( modSource, modTarget );
		}

		private static bool TryGetImplicitConversionCtor( Hierarchical target, Typespec source )
		{
			foreach( Decl child in target.children ) {
				if( child is not Structor stc
				 || stc.kind != Structor.Kind.Constructor
				 || !stc.isConversionCtor
				 || !stc.IsImplicit )
					continue;

				if( stc.paras.Count != 1 )
					continue;

				if( IsImplicitlyConvertible( source, stc.paras[0].type ) )
					return true;
			}
			return false;
		}

		private static bool TryGetImplicitConversionOperator( Hierarchical source, Typespec target )
		{
			foreach( Decl child in source.children ) {
				if( child is not Func func
				 || func.kind != Func.Kind.Convert
				 || !func.IsImplicit )
					continue;

				if( IsImplicitlyConvertible( func.retType, target ) )
					return true;
			}
			return false;
		}
	}
}
