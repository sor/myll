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
			foreach( Decl decl in ctx.Module.Module.children )
				ApplyDefaults( decl, ctx.Module.Context.Dialect );
		}

		private static void ApplyDefaults( Decl decl, Dialect dialect )
		{
			Attribs defaults = GetDefaultAttributes( decl, dialect );
			if( defaults.Count > 0 )
				decl.MergeAttribs( defaults );

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					ApplyDefaults( child, dialect );
			}
		}

		private static Attribs GetDefaultAttributes( Decl decl, Dialect dialect )
		{
			string? source = decl switch {
				Structural s when s.kind == Structural.Kind.Class  => dialect.DefaultAttributesClass,
				Structural s when s.kind == Structural.Kind.Struct => dialect.DefaultAttributesStruct,
				Structural s when s.kind == Structural.Kind.Union  => dialect.DefaultAttributesUnion,
				Enumeration                                        => dialect.DefaultAttributesEnum,
				_                                                  => null,
			};

			if( source == null )
				return new Attribs();

			return AttributeStringParser.Parse( source );
		}
	}
}
