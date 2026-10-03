using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
{
	/// <summary>
	/// Infers the element type for range-based <c>for</c> loops declared with <c>auto</c>.
	/// This runs after name resolution so the range expression type is known, and before
	/// type checking so that uses of the loop variable inside the body are validated
	/// against the concrete element type rather than <c>auto</c>.
	/// </summary>
	public sealed class RangeForAutoTypeTransformer : Transformer
	{
		protected override void PostTransform( TransformContext ctx )
		{
			Inferer walker = new( ctx.ResolutionResult!, ctx.Module.Context.Dialect );
			walker.WalkModule( ctx.Module.Module );
		}

		private sealed class Inferer : DeclWalker
		{
			private readonly TypeResolver typeResolver;

			public Inferer( ResolutionResult resolution, Dialect dialect )
				=> typeResolver = new TypeResolver( resolution ) {
					Dialect = dialect,
				};

			protected override Stmt TransformStmt( Stmt stmt )
			{
				if( stmt is RangeForStmt rfs )
					InferRangeVariableType( rfs );

				return base.TransformStmt( stmt );
			}

			private void InferRangeVariableType( RangeForStmt rfs )
			{
				if( rfs.varDecl == null || !IsAutoKind( rfs.varDecl.type ) )
					return;

				Typespec? rangeType = typeResolver.Resolve( rfs.range );
				Typespec? elementType = rangeType != null
					? InferRangeElementType( rangeType )
					: null;

				if( elementType == null )
					return;

				rfs.varDecl.type = elementType;
				if( rfs.varDecl.decl != null )
					rfs.varDecl.decl.type = elementType;
			}

			private static bool IsAutoKind( Typespec? type )
				=> type is TypespecBasic basic
				&& ( basic.kind == TypespecBasic.Kind.ExplicitAuto
				  || basic.kind == TypespecBasic.Kind.ImplicitAuto );

			private Typespec? InferRangeElementType( Typespec rangeType )
			{
				if( rangeType.ptrs != null && rangeType.ptrs.Count > 0 ) {
					Pointer.Kind firstKind = rangeType.ptrs[0].kind;
					if( firstKind == Pointer.Kind.RawPtr )
						return StripFirstPointer( rangeType );
					if( firstKind == Pointer.Kind.LVRef || firstKind == Pointer.Kind.RVRef )
						rangeType = StripFirstPointer( rangeType );
				}

				if( rangeType is not TypespecNested nested )
					return null;

				IdTplArgs? lastSegment = nested.idTpls.LastOrDefault();

				// Synthesized initializer_list<T> literals do not have resolvedDecl set,
				// but the element type is available as the first template argument.
				if( nested.isInitList && lastSegment != null && lastSegment.tplArgs.Count > 0 )
					return lastSegment.tplArgs[0].typespec;

				if( nested.resolvedDecl is not Structural structural )
					return null;

				if( lastSegment != null && lastSegment.tplArgs.Count > 0 )
					return lastSegment.tplArgs[0].typespec;

				foreach( Decl child in structural.children ) {
					if( child is Func func && func.name == "operator*" )
						return func.retType;
				}

				return null;
			}

			private static Typespec StripFirstPointer( Typespec type )
			{
				if( type is TypespecBasic basic )
					return new TypespecBasic {
						kind   = basic.kind,
						size   = basic.size,
						srcPos = basic.srcPos,
						qual   = basic.qual,
						ptrs   = basic.ptrs!.Count > 1
							? basic.ptrs.GetRange( 1, basic.ptrs.Count - 1 )
							: null,
					};

				if( type is TypespecNested nested )
					return new TypespecNested {
						srcPos          = nested.srcPos,
						qual            = nested.qual,
						ptrs            = nested.ptrs!.Count > 1
							? nested.ptrs.GetRange( 1, nested.ptrs.Count - 1 )
							: null,
						resolvedDecl    = nested.resolvedDecl,
						isDependent     = nested.isDependent,
						prefixDependent = nested.prefixDependent,
						isInitList      = nested.isInitList,
						idTpls          = nested.idTpls,
					};

				return type;
			}
		}
	}
}