using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Validates that classes/structs annotated with a rule-of-N attribute satisfy at
	/// least one of the requested rules. The dialect default is applied when no per-class
	/// attribute is present.
	///
	/// Rule of 0: none of the five default operations are user-defined.
	/// Rule of 3: destructor, copy constructor, and copy assignment are user-defined;
	///            no move constructor or move assignment.
	/// Rule of 5: destructor, copy/move constructors, and copy/move assignments are
	///            all user-defined.
	/// </summary>
	public sealed class RuleOfTransformer : ITransformer
	{
		public void Transform(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules,
			List<Diagnostic> diagnostics )
		{
			foreach( (GlobalNamespace module, _) in modules ) {
				foreach( Decl decl in module.children )
					CheckDecl( decl, diagnostics );
			}
		}

		private static void CheckDecl( Decl decl, List<Diagnostic> diagnostics )
		{
			if( decl is Structural structural && !structural.IsForwardDeclaration && !structural.IsExternal )
				CheckStructural( structural, diagnostics );

			if( decl is Hierarchical h ) {
				foreach( Decl child in h.children )
					CheckDecl( child, diagnostics );
			}
		}

		private static void CheckStructural( Structural structural, List<Diagnostic> diagnostics )
		{
			RuleOf required = GetRequiredRuleOf( structural );
			if( required == RuleOf.None )
				return;

			SpecialMemberStatus status = AnalyzeSpecialMembers( structural );
			RuleOf satisfied = status.SatisfiedRules();

			if( (required & satisfied) != 0 )
				return;

			diagnostics.Add( new Diagnostic(
				structural.srcPos,
				DiagnosticKind.Error,
				String.Format(
					"Class '{0}' does not satisfy the required rule-of-N ({1}). Special members present: {2}",
					structural.name,
					FormatRuleOf( required ),
					status.Format() ) ) );
		}

		private static RuleOf GetRequiredRuleOf( Structural structural )
		{
			RuleOf fromAttrib = RuleOf.None;

			if( structural.HasAttrib( "rule_of_5" ) )
				fromAttrib |= RuleOf.Five;
			if( structural.HasAttrib( "rule_of_3" ) )
				fromAttrib |= RuleOf.Three;
			if( structural.HasAttrib( "rule_of_0" ) )
				fromAttrib |= RuleOf.Zero;

			if( structural.IsAttrib( "rule_of_n", "5" ) )
				fromAttrib |= RuleOf.Five;
			if( structural.IsAttrib( "rule_of_n", "3" ) )
				fromAttrib |= RuleOf.Three;
			if( structural.IsAttrib( "rule_of_n", "0" ) )
				fromAttrib |= RuleOf.Zero;

			return fromAttrib;
		}

		private static string FormatRuleOf( RuleOf rule )
		{
			if( rule == RuleOf.Any )
				return "0, 3 or 5";

			var names = new List<string>();
			if( (rule & RuleOf.Zero)  != 0 ) names.Add( "0" );
			if( (rule & RuleOf.Three) != 0 ) names.Add( "3" );
			if( (rule & RuleOf.Five)  != 0 ) names.Add( "5" );
			return String.Join( ", ", names );
		}

		private static SpecialMemberStatus AnalyzeSpecialMembers( Structural structural )
		{
			var status = new SpecialMemberStatus();

			foreach( Decl child in structural.children ) {
				switch( child ) {
					case Structor stc when stc.kind == Structor.Kind.Destructor:
						status.HasDestructor = true;
						break;

					case Structor stc when stc.kind == Structor.Kind.Constructor:
						if( IsCopyConstructor( stc, structural ) )
							status.HasCopyConstructor = true;
						else if( IsMoveConstructor( stc, structural ) )
							status.HasMoveConstructor = true;
						break;

					case Func func when func.name == "operator=":
						if( IsCopyAssignment( func, structural ) )
							status.HasCopyAssignment = true;
						else if( IsMoveAssignment( func, structural ) )
							status.HasMoveAssignment = true;
						break;
				}
			}

			return status;
		}

		private static bool IsCopyConstructor( Structor ctor, Structural owner )
			=> IsSpecialConstructor( ctor, owner, Pointer.Kind.LVRef, Qualifier.Const );

		private static bool IsMoveConstructor( Structor ctor, Structural owner )
			=> IsSpecialConstructor( ctor, owner, Pointer.Kind.RVRef, Qualifier.None );

		private static bool IsSpecialConstructor(
			Structor    ctor,
			Structural  owner,
			Pointer.Kind ptrKind,
			Qualifier    expectedQual )
		{
			if( ctor.paras.Count != 1 )
				return false;

			return MatchesClassReference( ctor.paras[0].type, owner, ptrKind, expectedQual );
		}

		private static bool IsCopyAssignment( Func op, Structural owner )
			=> IsSpecialAssignment( op, owner, Pointer.Kind.LVRef, Qualifier.Const );

		private static bool IsMoveAssignment( Func op, Structural owner )
			=> IsSpecialAssignment( op, owner, Pointer.Kind.RVRef, Qualifier.None );

		private static bool IsSpecialAssignment(
			Func         op,
			Structural   owner,
			Pointer.Kind ptrKind,
			Qualifier    expectedQual )
		{
			if( op.paras.Count != 1 )
				return false;

			return MatchesClassReference( op.paras[0].type, owner, ptrKind, expectedQual );
		}

		private static bool MatchesClassReference(
			Typespec     type,
			Structural   owner,
			Pointer.Kind ptrKind,
			Qualifier    expectedQual )
		{
			if( type is not TypespecNested nested )
				return false;

			bool nameMatches = nested.resolvedDecl == owner
				|| nested.idTpls.Last().id == owner.name;
			if( !nameMatches )
				return false;

			if( nested.ptrs == null || nested.ptrs.Count != 1 )
				return false;

			Pointer ptr = nested.ptrs[0];
			if( ptr.kind != ptrKind )
				return false;

			Qualifier actualQual = ptr.qual == Qualifier.None
				? nested.qual
				: ptr.qual;

			return actualQual == expectedQual;
		}

		private sealed class SpecialMemberStatus
		{
			public bool HasDestructor;
			public bool HasCopyConstructor;
			public bool HasMoveConstructor;
			public bool HasCopyAssignment;
			public bool HasMoveAssignment;

			public RuleOf SatisfiedRules()
			{
				RuleOf result = RuleOf.None;

				bool anySpecial = HasDestructor
				               || HasCopyConstructor
				               || HasMoveConstructor
				               || HasCopyAssignment
				               || HasMoveAssignment;

				if( !anySpecial )
					result |= RuleOf.Zero;

				if( HasDestructor && HasCopyConstructor && HasCopyAssignment
				 && !HasMoveConstructor && !HasMoveAssignment )
					result |= RuleOf.Three;

				if( HasDestructor && HasCopyConstructor && HasMoveConstructor
				 && HasCopyAssignment && HasMoveAssignment )
					result |= RuleOf.Five;

				return result;
			}

			public string Format()
			{
				var parts = new List<string>();
				if( HasDestructor )       parts.Add( "destructor" );
				if( HasCopyConstructor )  parts.Add( "copy constructor" );
				if( HasMoveConstructor )  parts.Add( "move constructor" );
				if( HasCopyAssignment )   parts.Add( "copy assignment" );
				if( HasMoveAssignment )   parts.Add( "move assignment" );

				if( parts.Count == 0 )
					return "none";

				return String.Join( ", ", parts );
			}
		}
	}
}
