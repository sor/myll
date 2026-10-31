using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Partial ordering of template specializations and CTAD guides after
	/// C++ [temp.func.order]: a side is "more specialized" when the other
	/// side's parameter list is deducible from its synthesized list (unique
	/// invented types substituted for every open parameter) while the reverse
	/// fails. The result is a three-way comparison: -1 = a is more
	/// specialized, +1 = b is, 0 = equally specialized, null = incomparable
	/// (neither deducible) — both non-minus-one outcomes keep the existing
	/// ambiguity diagnostics.
	/// </summary>
	public static class PartialOrdering
	{
		public static int? CompareSpecs( Decl a, Decl b )
		{
			Decl.SpecData aInfo = a.SpecInfo!;
			Decl.SpecData bInfo = b.SpecInfo!;

			// Member specializations carry class parameters in front of the
			// member's own; the position map is not recoverable from the
			// primary alone, so those stay with the ambiguity diagnostic.
			if( aInfo.Primary != bInfo.Primary
			 || aInfo.Primary is Func { IsInStruct: true } )
				return null;

			if( aInfo.Primary is not ITemplatedDecl primary )
				return null;

			foreach( TplParam param in primary.TplParams )
				if( param.kind != TplParamKind.Type || param.isPack )
					return null;

			List<Typespec>? aList = SpecPositionTypes( aInfo, primary.TplParams, out List<string> aParams );
			List<Typespec>? bList = SpecPositionTypes( bInfo, primary.TplParams, out List<string> bParams );
			if( aList == null || bList == null )
				return null;

			return Compare( aList, aParams, bList, bParams );
		}

		/// <summary>
		/// Partial ordering of two CTAD guides of one class: the constructor
		/// parameter lists play the role of the function parameter lists; the
		/// deduction targets are the constructor's own template parameters plus
		/// the class parameters that occur in them. Class parameters left out of
		/// the parameter list do not join (C++ compares the guides' function
		/// signatures, where the class arguments only enter via the return).
		/// </summary>
		public static int? CompareGuides( Structural owner, Structor a, Structor b )
		{
			List<TplParam> combined = new( owner.TplParams );
			combined.AddRange( a.TplParams );
			combined.AddRange( b.TplParams );
			foreach( TplParam param in combined )
				if( param.kind != TplParamKind.Type || param.isPack )
					return null;

			HashSet<string> aNames = new( StringComparer.Ordinal );
			HashSet<string> bNames = new( StringComparer.Ordinal );
			foreach( Param param in a.paras ) CollectSymbolicNames( param.type, aNames );
			foreach( Param param in b.paras ) CollectSymbolicNames( param.type, bNames );

			List<string> aParams = owner.TplParams.Where( p => aNames.Contains( p.name ) )
				.Select( p => p.name ).ToList();
			aParams.AddRange( a.TplParams.Select( p => p.name ) );

			List<string> bParams = owner.TplParams.Where( p => bNames.Contains( p.name ) )
				.Select( p => p.name ).ToList();
			bParams.AddRange( b.TplParams.Select( p => p.name ) );

			return Compare(
				a.paras.Select( p => p.type ).ToList(), aParams,
				b.paras.Select( p => p.type ).ToList(), bParams );
		}

		/// <summary>
		/// Finds the unique candidate that is strictly more specialized than
		/// every other candidate (C++ picks a single best guide; anything less
		/// keeps the ambiguity error). Returns null when no candidate
		/// dominates or two candidates are equally specialized.
		/// </summary>
		public static T? UniqueDominator<T>( List<T> candidates, Func<T, T, int?> compare ) where T : class
		{
			foreach( T candidate in candidates ) {
				if( candidates.Where( other => other != candidate )
					.All( other => compare( candidate, other ) == -1 ) )
					return candidate;
			}

			return null;
		}

		private static int? Compare(
			List<Typespec> aList, List<string> aParams,
			List<Typespec> bList, List<string> bParams )
		{
			if( aList.Count != bList.Count )
				return null;

			List<Typespec> synthesizedA = Synthesize( aList );
			List<Typespec> synthesizedB = Synthesize( bList );

			bool aDeducibleFromB = TryDeduceFrom( aList, aParams, synthesizedB );
			bool bDeducibleFromA = TryDeduceFrom( bList, bParams, synthesizedA );

			return aDeducibleFromB && bDeducibleFromA ? 0
				: aDeducibleFromB               ? +1 // b subsumes a: b is more specialized
				: bDeducibleFromA               ? -1
				:                                 null;
		}

		// Replaces every symbolic position (a pattern's named parameter, or an
		// unbound position's parameter) with a unique invented type shared per
		// parameter name, so linked positions (Pair<T, T*>) stay linked.
		private static List<Typespec> Synthesize( List<Typespec> list )
		{
			List<Typespec> synthesized = new( list.Count );
			foreach( Typespec type in list ) {
				if( type is TypespecNested nested && nested.idTpls.Count == 1 ) {
					synthesized.Add( new TypespecNested {
						idTpls = { new IdTplArgs { id = "__po_" + nested.idTpls[0].id } },
						ptrs   = nested.ptrs,
					} );
				}
				else
					synthesized.Add( type );
			}

			return synthesized;
		}

		// Deduction of one side's parameters from the other side's
		// synthesized list: every position binds strictly, and every named
		// parameter must end up bound consistently.
		private static bool TryDeduceFrom(
			List<Typespec>        list,
			List<string>          paramNames,
			List<Typespec>        synthesized )
		{
			Dictionary<string, Typespec> bindings = new( StringComparer.Ordinal );
			for( int i = 0; i < list.Count; i++ ) {
				if( !TryBindPosition( list[ i ], synthesized[ i ], paramNames, bindings ) )
					return false;
			}

			return paramNames.All( bindings.ContainsKey );
		}

		private static bool TryBindPosition(
			Typespec                      parameter,
			Typespec                      argument,
			List<string>                 paramNames,
			Dictionary<string, Typespec>  bindings )
		{
			List<Pointer> parameterPtrs = parameter.ptrs ?? _noPtrs;
			List<Pointer> argumentPtrs  = argument.ptrs  ?? _noPtrs;

			// Strip matching outer decorations pairwise, outermost first.
			int pi = parameterPtrs.Count - 1;
			int ai = argumentPtrs.Count - 1;
			while( pi >= 0 && ai >= 0 && DecorationsMatch( parameterPtrs[ pi ], argumentPtrs[ ai ] ) )
				pi--; ai--;

			string? paramName = AsParameterName( parameter, paramNames );
			if( paramName != null ) {
				if( pi >= 0 ) // parameter needs a decoration the argument spent
					return false;

				Typespec bound = StripOutermost( argument, argumentPtrs.Count - ( ai + 1 ) );
				if( bindings.TryGetValue( paramName, out Typespec? existing ) )
					return existing.Gen() == bound.Gen();

				bindings[ paramName ] = bound;
				return true;
			}

			// Concrete position: must be identical after matching decorations.
			return pi < 0 && ai < 0 && parameter.Gen() == argument.Gen();
		}

		private static bool DecorationsMatch( Pointer parameter, Pointer argument )
			=> parameter.kind == argument.kind
			 && parameter.qual == argument.qual
			 && ( parameter.expr?.Gen() ?? "" ) == ( argument.expr?.Gen() ?? "" );

		private static string? AsParameterName( Typespec type, List<string> paramNames )
		{
			if( type is TypespecNested nested && nested.idTpls.Count == 1 ) {
				string name = nested.idTpls[ 0 ].id;
				if( paramNames.Contains( name ) )
					return name;
			}

			return null;
		}

		private static Typespec StripOutermost( Typespec type, int count )
		{
			if( count == 0 )
				return type;

			Typespec ret = type.Clone();
			ret.ptrs = type.ptrs?.Take( type.ptrs.Count - count ).ToList();
			return ret;
		}

		private static void CollectSymbolicNames( Typespec type, HashSet<string> into )
		{
			if( type is not TypespecNested nested )
				return;

			foreach( IdTplArgs id in nested.idTpls ) {
				into.Add( id.id );
				foreach( TplArg tplArg in id.tplArgs )
					if( tplArg.typespec != null )
						CollectSymbolicNames( tplArg.typespec, into );
			}
		}

		// Builds the ordering list of a specialization: concrete positions
		// contribute their literal type, pattern positions their written
		// pattern, and unbound positions their parameter as a bare symbolic
		// type. Returns null when the shape is not orderable (non-type
		// bindings, member-head position maps).
		private static List<Typespec>? SpecPositionTypes(
			Decl.SpecData info, List<TplParam> primaryParams, out List<string> openParams )
		{
			List<Typespec> list = new( info.Args.Count );
			openParams = new List<string>();

			if( info.Args.Count != primaryParams.Count )
				return null;

			for( int i = 0; i < info.Args.Count; i++ ) {
				bool isPattern = i < info.Pattern.Length && info.Pattern[ i ];

				if( isPattern ) {
					if( info.Args[ i ].typespec is not TypespecNested pattern || pattern.idTpls.Count != 1 )
						return null;

					string name = pattern.idTpls[ 0 ].id;
					list.Add( info.Args[ i ].typespec! );
					if( !openParams.Contains( name ) )
						openParams.Add( name );
				}
				else if( info.Bound[ i ] ) {
					if( info.Args[ i ].typespec == null ) // non-type concrete binding
						return null;

					list.Add( info.Args[ i ].typespec! );
				}
				else {
					string name = primaryParams[ i ].name;
					list.Add( new TypespecNested { idTpls = { new IdTplArgs { id = name } } } );
					if( !openParams.Contains( name ) )
						openParams.Add( name );
				}
			}

			return list;
		}

		private static readonly List<Pointer> _noPtrs = new();
	}
}