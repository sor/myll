using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
{
	/// <summary>
	/// Validates comparison-operator sets on classes/structs according to the active
	/// <see cref="ComparisonMode"/>, and auto-generates defaulted <c>operator==</c> and
	/// <c>operator&lt;=&gt;</c> when the mode is <see cref="ComparisonMode.Spaceship"/>.
	/// </summary>
	public sealed class ComparisonModeTransformer : Transformer
	{
		private static readonly HashSet<string> EqualOperators = new() {
			"operator=="
		};

		private static readonly HashSet<string> LessThanOperators = new() {
			"operator==",
			"operator<",
		};

		private static readonly HashSet<string> SpaceshipOperators = new() {
			"operator==",
			"operator<=>",
		};

		private static readonly HashSet<string> OldStyleOperators = new() {
			"operator<",
			"operator>",
			"operator<=",
			"operator>=",
			"operator==",
			"operator!=",
		};

		private static readonly HashSet<string> ComparisonOperators = new() {
			"operator<",
			"operator>",
			"operator<=",
			"operator>=",
			"operator==",
			"operator!=",
			"operator<=>",
		};

		protected override void PreTransform( TransformContext ctx )
		{
			Checker walker = new( ctx.Module.Context.Dialect, Diagnostics );
			walker.WalkModule( ctx.Module.Module );
		}

		private sealed class Checker : DeclWalker
		{
			private readonly Dialect dialect;
			private readonly List<Diagnostic> diagnostics;

			public Checker( Dialect dialect, List<Diagnostic> diagnostics )
			{
				this.dialect    = dialect;
				this.diagnostics = diagnostics;
			}

			protected override void TransformDecl( Decl decl )
			{
				if( decl is Structural structural && !structural.IsForwardDeclaration && !structural.IsExternal )
					CheckStructural( structural, dialect, diagnostics );

				base.TransformDecl( decl );
			}
		}

		private static void CheckStructural(
			Structural structural,
			Dialect dialect,
			List<Diagnostic> diagnostics )
		{
			ComparisonMode mode = GetMode( structural, dialect );
			if( mode == ComparisonMode.Any )
				return;

			HashSet<string> present = structural.children
				.OfType<Func>()
				.Where( f => ComparisonOperators.Contains( f.name ) )
				.Select( f => f.name )
				.ToHashSet();

			switch( mode ) {
				case ComparisonMode.None:
					CheckNone( structural, present, diagnostics );
					break;

				case ComparisonMode.Equal:
					CheckExactSet( structural, present, EqualOperators, diagnostics );
					break;

				case ComparisonMode.LessThan:
					CheckExactSet( structural, present, LessThanOperators, diagnostics );
					break;

				case ComparisonMode.Spaceship:
					GenerateSpaceshipDefaults( structural, present );
					present = structural.children
						.OfType<Func>()
						.Where( f => ComparisonOperators.Contains( f.name ) )
						.Select( f => f.name )
						.ToHashSet();
					CheckAllowedOnly( structural, present, SpaceshipOperators, diagnostics );
					break;

				case ComparisonMode.MaybeSpaceship:
					CheckAllowedOnly( structural, present, SpaceshipOperators, diagnostics );
					break;

				case ComparisonMode.All:
					CheckAllStyle( structural, present, diagnostics );
					break;
			}
		}

		private static ComparisonMode GetMode( Structural structural, Dialect dialect )
		{
			if( structural.IsAttrib( "comp", "none" ) )
				return ComparisonMode.None;
			if( structural.IsAttrib( "comp", "equal" ) )
				return ComparisonMode.Equal;
			if( structural.IsAttrib( "comp", "lessthan" ) )
				return ComparisonMode.LessThan;
			if( structural.IsAttrib( "comp", "spaceship" ) )
				return ComparisonMode.Spaceship;
			if( structural.IsAttrib( "comp", "maybespaceship" ) )
				return ComparisonMode.MaybeSpaceship;
			if( structural.IsAttrib( "comp", "all" ) )
				return ComparisonMode.All;
			if( structural.IsAttrib( "comp", "any" ) )
				return ComparisonMode.Any;

			return dialect.ComparisonOperators;
		}

		private static void CheckNone(
			Structural structural,
			HashSet<string> present,
			List<Diagnostic> diagnostics )
		{
			foreach( string op in present ) {
				diagnostics.Add( new Diagnostic(
					structural.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"[comp=none] on '{0}' disallows comparison operator '{1}'.",
						structural.name,
						op ) ) );
			}
		}

		private static void CheckExactSet(
			Structural structural,
			HashSet<string> present,
			HashSet<string> required,
			List<Diagnostic> diagnostics )
		{
			CheckAllowedOnly( structural, present, required, diagnostics );

			foreach( string op in required.Where( r => !present.Contains( r ) ) ) {
				diagnostics.Add( new Diagnostic(
					structural.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"[comp={1}] on '{0}' requires comparison operator '{2}'.",
						structural.name,
						GetModeName( required ),
						op ) ) );
			}
		}

		private static void CheckAllowedOnly(
			Structural structural,
			HashSet<string> present,
			HashSet<string> allowed,
			List<Diagnostic> diagnostics )
		{
			foreach( string op in present.Where( p => !allowed.Contains( p ) ) ) {
				diagnostics.Add( new Diagnostic(
					structural.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"[comp={1}] on '{0}' disallows comparison operator '{2}'.",
						structural.name,
						GetModeName( allowed ),
						op ) ) );
			}
		}

		private static void CheckAllStyle(
			Structural structural,
			HashSet<string> present,
			List<Diagnostic> diagnostics )
		{
			bool hasOldStyle  = OldStyleOperators.All( present.Contains );
			bool hasSpaceship = present.Contains( "operator==" ) && present.Contains( "operator<=>" );

			if( hasOldStyle && hasSpaceship ) {
				diagnostics.Add( new Diagnostic(
					structural.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"[comp=all] on '{0}' requires either the full old-style set or the full spaceship set, not a mix.",
						structural.name ) ) );
				return;
			}

			if( !hasOldStyle && !hasSpaceship ) {
				string missingOld = String.Join( ", ", OldStyleOperators.Where( o => !present.Contains( o ) ) );
				diagnostics.Add( new Diagnostic(
					structural.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"[comp=all] on '{0}' does not provide a complete comparison set. Missing old-style operators: {1}.",
						structural.name,
						missingOld ) ) );
			}
		}

		private static void GenerateSpaceshipDefaults(
			Structural structural,
			HashSet<string> present )
		{
			if( !present.Contains( "operator==" ) )
				structural.children.Add( BuildDefaultComparisonFunc( structural, "operator==", TypespecBasic.Kind.Bool ) );

			if( !present.Contains( "operator<=>" ) )
				structural.children.Add( BuildDefaultComparisonFunc( structural, "operator<=>", TypespecBasic.Kind.ExplicitAuto ) );
		}

		private static Func BuildDefaultComparisonFunc(
			Structural structural,
			string name,
			TypespecBasic.Kind returnKind )
		{
			List<TplArg> tplArgs = structural.TplParams
				.Select( p => new TplArg {
					typespec = new TypespecNested {
						idTpls = new List<IdTplArgs> { new() { id = p.name } },
					},
				} )
				.ToList();

			TypespecNested paramType = new() {
				idTpls = new List<IdTplArgs> {
					new() { id = structural.name, tplArgs = tplArgs },
				},
				qual = Qualifier.Const,
				ptrs = new List<Pointer> { new() { kind = Pointer.Kind.LVRef } },
			};

			Func func = new() {
				srcPos  = structural.srcPos,
				name    = name,
				access  = Access.Public,
				kind    = Func.Kind.Operator,
				paras   = new List<Param> {
					new() { name = "other", type = paramType },
				},
				retType = new TypespecBasic { kind = returnKind, size = returnKind == TypespecBasic.Kind.Bool ? 1 : TypespecBasic.SizeUndetermined },
			};

			func.scope = new ScopeLeaf {
				parent = structural.scope,
				decl   = func,
			};

		func.MergeAttribs( new() {
			["default"] = new List<string>(),
		} );

			return func;
		}

		private static string GetModeName( HashSet<string> operators )
		{
			if( operators.SetEquals( EqualOperators ) )
				return "equal";
			if( operators.SetEquals( LessThanOperators ) )
				return "lessthan";
			if( operators.SetEquals( SpaceshipOperators ) )
				return "spaceship";
			return "custom";
		}
	}
}
