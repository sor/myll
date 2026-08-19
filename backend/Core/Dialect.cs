using System;

namespace Myll.Core
{
	// Compile-time dialect switches. In the future these may move to a config file or CLI options.
	public enum SwitchFallthroughMode
	{
		ImplicitBreak,       // Myll inserts `break` at the end of a case when it is missing. Write `fall;` for intentional fallthrough.
		ImplicitFallthrough, // Cases fall through to the next case when no explicit jump is present.
		Explicit,            // Every case must end with `break;` or `fallthrough;`.
	}

	// If multiple, adherence to one is sufficient
	[Flags]
	public enum RuleOf
	{
		None  = 0,
		Zero  = 1 << 0,
		Three = 1 << 1,
		Five  = 1 << 2,
		Any   = Zero | Three | Five,
	}

	public static class Dialect
	{
		// When true, `new T` is illegal; you must write `new T*` (or another explicit pointer type).
		// When false, `new T` gets an implicit raw pointer, matching the C++-style default.
		public static bool StrictNew = false;

		// Controls the default behavior of `switch` cases.
		public static SwitchFallthroughMode SwitchFallthrough = SwitchFallthroughMode.ImplicitBreak;

		// Default rule-of-N enforcement for classes. Can be overridden per class with e.g. `[rule_of=5]`.
		public static RuleOf DefaultRuleOf = RuleOf.None;
	}
}
