using System;
using System.Collections.Generic;
using System.Linq;
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
		/// Some of these defaults (e.g., int/float cross-conversion) may later become dialect flags.
		/// </summary>
		public static ConversionRank GetRank( Typespec? source, Typespec? target )
		{
			if( source == null || target == null )
				return ConversionRank.None;

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

			// Value -> reference parameter binding (e.g. passing std::ostream to std::ostream&).
			if( TryStripReference( target, out Typespec? refBase )
			 && GetRank( source, refBase ) <= ConversionRank.Promotion )
				return ConversionRank.Promotion;

			// Inheritance conversions: Derived -> Base&, Derived* -> Base*, Derived*! -> Base*!, etc.
			if( TryGetInheritanceConversionRank( source, target, out ConversionRank inheritanceRank ) )
				return inheritanceRank;

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

		public static bool IsImplicitlyConvertible( Typespec? source, Typespec? target ) {
			ConversionRank rank = GetRank( source, target );
			return rank <= ConversionRank.Conversion || rank == ConversionRank.TemplateParameter;
		}

		/// <summary>
		/// True when <paramref name="source"/> is a class/struct type derived from
		/// <paramref name="target"/>, considering the full base chain.
		/// </summary>
		public static bool IsDerivedFrom( TypespecNested source, TypespecNested target )
		{
			if( source.resolvedDecl == null || target.resolvedDecl == null )
				return false;

			if( source.resolvedDecl == target.resolvedDecl )
				return false;

			return IsDerivedFrom( source.resolvedDecl, target.resolvedDecl, new HashSet<Decl>() );
		}

		private static bool IsDerivedFrom( Decl derived, Decl target, HashSet<Decl> visited )
		{
			if( derived == target )
				return true;

			if( derived is not Structural structural )
				return false;

			if( !visited.Add( structural ) )
				return false;

			foreach( BaseType bt in structural.basetypes ) {
				Decl? baseDecl = bt.type is TypespecNested nested ? nested.resolvedDecl : null;
				if( baseDecl == null )
					continue;

				if( baseDecl == target )
					return true;

				if( IsDerivedFrom( baseDecl, target, visited ) )
					return true;
			}

			return false;
		}

		/// <summary>
		/// True when a value of a derived class type is being used where a base class value
		/// is expected. Myll treats this as object slicing and rejects it.
		/// </summary>
		public static bool IsSlicingAttempt( Typespec source, Typespec target )
		{
			if( source.ptrs is { Count: > 0 } )
				return false;

			if( target.ptrs is { Count: > 0 } )
				return false;

			if( source is not TypespecNested srcNest || target is not TypespecNested tgtNest )
				return false;

			return IsDerivedFrom( srcNest, tgtNest );
		}

		private static bool TryGetInheritanceConversionRank(
			Typespec           source,
			Typespec           target,
			out ConversionRank rank )
		{
			rank = ConversionRank.None;

			// Reference binding: Derived -> Base& / Derived -> Base&&.
			if( target.ptrs is { Count: > 0 }
			 && TryStripReference( target, out Typespec? refBase )
			 && refBase is TypespecNested refBaseNest
			 && source is TypespecNested srcNest
			 && IsDerivedFrom( srcNest, refBaseNest ) ) {
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

			if( sourceInner is TypespecNested sourceNested
			 && targetInner is TypespecNested targetNested
			 && IsDerivedFrom( sourceNested, targetNested ) ) {
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

			// Qualifiers are intentionally not required to match here. A value can be
			// bound to a more-qualified target (e.g., assigned to a const variable).
			// Pointer/reference structure must still match exactly.
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
				if( !TryParseInteger( srcBasic.literalText, out long value ) )
					return ConversionRank.None;

				// Default-sized `int` / `float` is the natural target for an unannotated literal.
				if( tgtBasic.kind == TypespecBasic.Kind.Integer && tgtBasic.isDefaultSized )
					return ConversionRank.Exact;

				// Integer literal defaults to the platform int size when the target is int.
				if( tgtBasic.kind == TypespecBasic.Kind.Integer && tgtBasic.size == 4 )
					return ConversionRank.Exact;

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
				int defaultSize = Dialect.DefaultFloatSize();
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

		private static bool IntegerFits( long value, TypespecBasic.Kind kind, int size )
		{
			return kind switch {
				TypespecBasic.Kind.Integer => size switch {
					1 => value >= sbyte.MinValue && value <= sbyte.MaxValue,
					2 => value >= short.MinValue && value <= short.MaxValue,
					4 => value >= int.MinValue && value <= int.MaxValue,
					8 => true,
					_ => false,
				},
				TypespecBasic.Kind.Unsigned => value >= 0 && size switch {
					1 => value <= byte.MaxValue,
					2 => value <= ushort.MaxValue,
					4 => value <= uint.MaxValue,
					8 => true,
					_ => false,
				},
				_ => false,
			};
		}

		private static bool TryParseInteger( string? text, out long value )
		{
			value = 0;
			if( string.IsNullOrEmpty( text ) )
				return false;

			ReadOnlySpan<char> span = text.AsSpan();

			try {
				if( span.Length >= 2 && span[0] == '0' ) {
					if( span[1] == 'x' || span[1] == 'X' )
						value = Convert.ToInt64( span[2..].ToString(), 16 );
					else if( span[1] == 'o' || span[1] == 'O' )
						value = Convert.ToInt64( span[2..].ToString(), 8 );
					else if( span[1] == 'b' || span[1] == 'B' )
						value = Convert.ToInt64( span[2..].ToString(), 2 );
					else
						value = long.Parse( span.ToString(), System.Globalization.NumberStyles.None );
				}
				else {
					value = long.Parse( span.ToString(), System.Globalization.NumberStyles.None );
				}

				return true;
			}
			catch {
				return false;
			}
		}

		private static bool TryParseFloat( string? text, out double value )
		{
			value = 0;
			if( string.IsNullOrEmpty( text ) )
				return false;

			return double.TryParse( text, System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out value );
		}

		private static bool TryStripReference( Typespec target, out Typespec? stripped )
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

		private static Typespec CloneTypespecBase( Typespec source )
		{
			Typespec ret = source switch {
				TypespecBasic b   => new TypespecBasic { kind = b.kind, size = b.size },
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
			}

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
	}
}
