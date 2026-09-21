using System.Collections.Generic;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
{
	using Attribs = Dictionary<string, List<string>>;

	/// <summary>
	/// Applies per-category default effect attributes to operator declarations from the active
	/// <see cref="Dialect"/>. Operators that already carry an explicit effect attribute
	/// (<c>pure</c>, <c>const</c>, <c>mutable</c>, or <c>mut</c>) keep their explicit choice.
	/// Runs after <see cref="ComparisonModeTransformer"/> so auto-generated comparison operators
	/// are also normalized by category defaults.
	/// </summary>
	public sealed class OperatorDefaultAttributesTransformer : Transformer
	{
		protected override void PreTransform( TransformContext ctx )
		{
			foreach( Decl decl in ctx.Module.Module.children )
				ApplyDefaults( decl, ctx.Module.Context.Dialect );
		}

		private static void ApplyDefaults( Decl decl, Dialect dialect )
		{
			if( decl is Func func
			 && func.kind == Func.Kind.Operator
			 && !func.IsExternal
			 && !HasEffectAttribute( func ) ) {
				Dialect.OperatorCategory category = Categorize( func.name );
				if( dialect.DefaultAttributesOperatorCategory.TryGetValue( category, out string? source )
				 && !string.IsNullOrEmpty( source ) ) {
					func.MergeAttribs( AttributeStringParser.Parse( source ) );
				}
			}

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					ApplyDefaults( child, dialect );
			}
		}

		private static bool HasEffectAttribute( Func func )
			=> func.HasAttrib( "pure" )
			|| func.HasAttrib( "const" )
			|| func.HasAttrib( "mutable" )
			|| func.HasAttrib( "mut" );

		private static Dialect.OperatorCategory Categorize( string opName )
		{
			if( opName.Length <= 8 )
				return Dialect.OperatorCategory.Other;

			string op = opName[8..];

			// comparison (must be before assignment because ==, !=, <=, >= end with '=')
			switch( op ) {
				case "<":
				case ">":
				case "<=":
				case ">=":
				case "==":
				case "!=":
				case "<=>":
					return Dialect.OperatorCategory.Comparison;
			}

			// assignment and compound assignment
			if( op == "=" || op.EndsWith( "=" ) )
				return Dialect.OperatorCategory.Assignment;

			// increment / decrement
			if( op == "++" || op == "--" )
				return Dialect.OperatorCategory.IncDecrement;

			// subscript, call, indirection
			switch( op ) {
				case "[]":
					return Dialect.OperatorCategory.Subscript;
				case "()":
					return Dialect.OperatorCategory.Call;
				case "->":
				case "->*":
				case "*":
				case "&":
					return Dialect.OperatorCategory.Indirection;
			}

			// arithmetic: + - / % | ^ << >> ~ !
			switch( op ) {
				case "+":
				case "-":
				case "/":
				case "%":
				case "|":
				case "^":
				case "<<":
				case ">>":
				case "~":
				case "!":
					return Dialect.OperatorCategory.Arithmetic;
			}

			return Dialect.OperatorCategory.Other;
		}
	}
}
