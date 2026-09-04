using System.Collections.Generic;
using Myll.Core;

namespace Myll.Resolver
{
	using Attribs = Dictionary<string, List<string>>;

	/// <summary>
	/// Applies per-kind default attributes from <see cref="Dialect"/> to class, struct,
	/// union, and enum declarations that do not already specify the corresponding attribute.
	/// Runs before other attribute-consuming transforms so that defaults participate in
	/// rule-of-N checks, flags handling, etc.
	/// </summary>
	public sealed class DefaultAttributesTransformer : ITransformer
	{
		public void Transform(
			IReadOnlyList<CompiledModuleResult> modules,
			List<Diagnostic> diagnostics )
		{
			foreach( CompiledModuleResult result in modules ) {
				foreach( Decl decl in result.Module.children )
					ApplyDefaults( decl );
			}
		}

		private static void ApplyDefaults( Decl decl )
		{
			Attribs defaults = GetDefaultAttributes( decl );
			if( defaults.Count > 0 )
				decl.MergeAttribs( defaults );

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					ApplyDefaults( child );
			}
		}

		private static Attribs GetDefaultAttributes( Decl decl )
		{
			string? source = decl switch {
				Structural s when s.kind == Structural.Kind.Class  => Dialect.DefaultAttributesClass,
				Structural s when s.kind == Structural.Kind.Struct => Dialect.DefaultAttributesStruct,
				Structural s when s.kind == Structural.Kind.Union  => Dialect.DefaultAttributesUnion,
				Enumeration                                        => Dialect.DefaultAttributesEnum,
				_                                                  => null,
			};

			if( source == null )
				return new Attribs();

			return AttributeStringParser.Parse( source );
		}
	}
}
