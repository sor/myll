using System.Collections.Generic;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
{
	using Attribs = Dictionary<string, List<string>>;

	/// <summary>
	/// Applies per-kind default attributes from <see cref="Dialect.Default"/> to class, struct,
	/// union, and enum declarations that do not already specify the corresponding attribute.
	/// Runs before other attribute-consuming transforms so that defaults participate in
	/// rule-of-N checks, flags handling, etc.
	/// </summary>
	public sealed class DefaultAttributesTransformer : Transformer
	{
		protected override void PreTransform( TransformContext ctx )
		{
			Applier walker = new( ctx.Module.Context.Dialect );
			walker.WalkModule( ctx.Module.Module );
		}

	private sealed class Applier : DeclWalker
	{
		private readonly Dialect dialect;

		public Applier( Dialect dialect ) => this.dialect = dialect;

		protected override void TransformDecl( Decl decl )
		{
			Attribs defaults = GetDefaultAttributes( decl, dialect );
			if( defaults.Count > 0 )
				decl.MergeAttribs( defaults );

			base.TransformDecl( decl );
		}
	}

		private static Attribs GetDefaultAttributes( Decl decl, Dialect dialect )
		{
			// External function effect attributes are intentionally not defaulted, because
			// Myll cannot verify the body of a C++ function. Convert declarations are an
			// exception: their implicit/explicit/const nature is part of the interface
			// Myll needs to know, so defaults apply even to external bindings.
			if( decl is Func func && func.IsExternal && func.kind != Func.Kind.Convert )
				return new Attribs();

			string? source = decl switch {
				Structural s when s.kind == Structural.Kind.Class  => dialect.DefaultAttributesClass,
				Structural s when s.kind == Structural.Kind.Struct => dialect.DefaultAttributesStruct,
				Structural s when s.kind == Structural.Kind.Union  => dialect.DefaultAttributesUnion,
				Enumeration                                        => dialect.DefaultAttributesEnum,
				Func f when f.kind == Func.Kind.Convert                                 => dialect.DefaultAttributesConvertTo,
				Func f when f.kind == Func.Kind.Function && !HasEffectAttribute( f )    => dialect.DefaultAttributesFunction,
				Func f when f.kind == Func.Kind.Procedure && !HasEffectAttribute( f )   => dialect.DefaultAttributesProcedure,
				Func f when f.kind == Func.Kind.Method && !HasEffectAttribute( f )      => dialect.DefaultAttributesMethod,
				Structor s when s.kind == Structor.Kind.Constructor && s.isConversionCtor => dialect.DefaultAttributesConvertFrom,
				_                                                                       => null,
			};

			if( source == null )
				return new Attribs();

			return AttributeStringParser.Parse( source );
		}

		private static bool HasEffectAttribute( Func func )
			=> func.HasAttrib( "pure" )
			|| func.HasAttrib( "const" )
			|| func.HasAttrib( "mutable" )
			|| func.HasAttrib( "mut" );
	}
}
