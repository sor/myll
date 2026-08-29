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

	public enum FloatKeywordMode
	{
		/// <summary>
		/// The `float` keyword is not bound to any concrete size. Untyped float literals
		/// still default to f32.
		/// </summary>
		Unspecified,
		F16,
		F32,
		F64,
		F128,
	}

	/// <summary>
	/// Controls the result type of mixed signed/unsigned arithmetic.
	/// </summary>
	public enum MixedSignednessMode
	{
		/// <summary>
		/// C/C++ behavior: unsigned wins when it has greater or equal rank.
		/// </summary>
		CStyle,

		/// <summary>
		/// Signed wins when its size is greater than or equal to the unsigned size;
		/// otherwise unsigned wins. This keeps the result signed whenever safe.
		/// </summary>
		SignedPreferred,
	}

	/// <summary>
	/// Controls how default-sized types (<c>int</c>, <c>uint</c>, <c>float</c>,
	/// <c>bint</c>) are treated by the type system. Only <see cref="SizeIndeterminate"/>
	/// is currently honored; the other values are reserved for future dialect support.
	/// </summary>
	[Flags]
	public enum DefaultTypeMode
	{
		SizeIndeterminate = 0,
		Forbidden         = 1 << 0,
		ForbiddenInStruct = 1 << 1,
		Size8             = 1 << 2,
		Size16            = 1 << 3,
		Size32            = 1 << 4,
		Size64            = 1 << 5,
		SizeFast          = 1 << 6,
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

		/// <summary>
		/// Controls which concrete size the `float` keyword refers to. `Unspecified` means the
		/// keyword has no binding; the active dialect decides whether it may still be used.
		/// </summary>
		public static FloatKeywordMode FloatKeyword = FloatKeywordMode.Unspecified;

		/// <summary>
		/// Controls the result type of mixed signed/unsigned arithmetic.
		/// Defaults to C-style "unsigned wins at equal rank".
		/// </summary>
		public static MixedSignednessMode MixedSignedness = MixedSignednessMode.CStyle;

		/// <summary>
		/// Controls how <c>int</c> is treated by the type system. Defaults to
		/// <see cref="DefaultTypeMode.SizeIndeterminate"/>.
		/// </summary>
		public static DefaultTypeMode DefaultInt = DefaultTypeMode.SizeIndeterminate;

		/// <summary>
		/// Controls how <c>uint</c> is treated by the type system. Defaults to
		/// <see cref="DefaultTypeMode.SizeIndeterminate"/>.
		/// </summary>
		public static DefaultTypeMode DefaultUInt = DefaultTypeMode.SizeIndeterminate;

		/// <summary>
		/// Controls how <c>float</c> is treated by the type system. Defaults to
		/// <see cref="DefaultTypeMode.SizeIndeterminate"/>.
		/// </summary>
		public static DefaultTypeMode DefaultFloat = DefaultTypeMode.SizeIndeterminate;

		/// <summary>
		/// Controls how <c>bint</c> is treated by the type system. Defaults to
		/// <see cref="DefaultTypeMode.SizeIndeterminate"/>.
		/// </summary>
		public static DefaultTypeMode DefaultBint = DefaultTypeMode.SizeIndeterminate;

		/// <summary>
		/// Returns the concrete float size (in bytes) used for untyped float literals and for
		/// the `float` keyword when <see cref="FloatKeyword"/> is <see cref="FloatKeywordMode.Unspecified"/>.
		/// </summary>
		public static int DefaultFloatSize()
		{
			return FloatKeyword switch {
				FloatKeywordMode.F16 => 2,
				FloatKeywordMode.F32 => 4,
				FloatKeywordMode.F64 => 8,
				FloatKeywordMode.F128 => 16,
				_                   => 4, // Unspecified fallback: f32
			};
		}
	}
}
