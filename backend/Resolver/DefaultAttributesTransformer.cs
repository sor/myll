using System.Collections.Generic;
using Myll.Core;

namespace Myll.Resolver
{
	using Attribs = Dictionary<string, List<string>>;

	/// <summary>
	/// Applies per-kind default attributes from <see cref="Dialect.Default"/> to class, struct,
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
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				foreach( Decl decl in module.children )
					ApplyDefaults( decl, context.Dialect );
			}
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
