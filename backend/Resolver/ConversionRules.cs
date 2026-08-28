using System;
using System.Collections.Generic;
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
		None,
	}

	public static class ConversionRules
	{
		/// <summary>
		/// Returns the conversion rank from <paramref name="source"/> to <paramref name="target"/>.
		/// Only exact matches and safe promotions are allowed by default:
		///   - integer to wider integer of the same signedness,
		///   - float to wider float,
		/// no narrowing, and no implicit integer/float or signed/unsigned conversions.
		/// </summary>
		public static ConversionRank GetRank( Typespec? source, Typespec? target )
		{
			if( source == null || target == null )
				return ConversionRank.None;

			if( IsExactMatch( source, target ) )
				return ConversionRank.Exact;

			if( IsPromotion( source, target ) )
				return ConversionRank.Promotion;

			if( IsAllowedConversion( source, target ) )
				return ConversionRank.Conversion;

			return ConversionRank.None;
		}

		public static bool IsImplicitlyConvertible( Typespec? source, Typespec? target )
			=> GetRank( source, target ) <= ConversionRank.Promotion;

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

			// Both have the same pointer structure.
			if( !HaveSamePointers( source.ptrs, target.ptrs ) )
				return false;

			return source switch {
				TypespecBasic srcBasic => target is TypespecBasic tgtBasic
				                       && srcBasic.kind == tgtBasic.kind
				                       && srcBasic.size == tgtBasic.size,
				TypespecNested srcNest => target is TypespecNested tgtNest
				                       && NestedNamesEqual( srcNest, tgtNest ),
				TypespecFunc   srcFunc => false, // function pointer identity not yet supported
				_                      => false,
			};
		}

		private static bool IsPromotion( Typespec source, Typespec target )
		{
			// Promotion preserves qualifiers and pointer structure exactly; only the
			// underlying scalar width changes.
			if( source.qual != target.qual )
				return false;

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
			// Currently all non-exact/non-promotion conversions are rejected.
			// This is where future safe conversions (e.g., derived-to-base pointers,
			// char32_t ↔ int32_t if desired) would go.
			return false;
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

		private static bool NestedNamesEqual( TypespecNested a, TypespecNested b )
		{
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
