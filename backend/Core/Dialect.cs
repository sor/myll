using System;
using System.Collections.Generic;

namespace Myll.Core
{
	/// <summary>
	/// Per-compilation dialect options. A copy is stored in each <see cref="ModuleContext"/>
	/// so that different modules can use different mappings in the future.
	/// </summary>
	public sealed record Dialect
	{
		/// <summary>
		/// Default dialect settings. Individual compilations can override by creating a new
		/// <see cref="Dialect"/> instance and assigning it to <see cref="ModuleContext.Dialect"/>.
		/// </summary>
		public static readonly Dialect Default = new();

		/// <summary>
		/// Fully-qualified Myll name of the class that backs the <c>string</c> keyword.
		/// The class must be declared in a <c>.decl.myll</c> file (or built in).
		/// </summary>
		public string StringClassName { get; init; } = "std::string";

		public string UniquePtrTemplate      { get; init; } = "std::unique_ptr<{0}>";
		public string UniqueArrayPtrTemplate { get; init; } = "std::unique_ptr<{0}[]>";
		public string SharedPtrTemplate      { get; init; } = "std::shared_ptr<{0}>";
		public string SharedArrayPtrTemplate { get; init; } = "std::shared_ptr<{0}[]>";
		public string WeakPtrTemplate        { get; init; } = "std::weak_ptr<{0}>";
		public string WeakArrayPtrTemplate   { get; init; } = "std::weak_ptr<{0}[]>";

		public string UniquePtrMakeTemplate      { get; init; } = "std::make_unique<{0}>({1})";
		public string UniqueArrayPtrMakeTemplate { get; init; } = "std::make_unique<{0}[]>({1})";
		public string SharedPtrMakeTemplate      { get; init; } = "std::make_shared<{0}>({1})";
		public string SharedArrayPtrMakeTemplate { get; init; } = "std::make_shared<{0}[]>({1})";

		// When true, `new T` is illegal; you must write `new T*` (or another explicit pointer type).
		// When false, `new T` gets an implicit raw pointer, matching the C++-style default.
		public bool StrictNew { get; init; } = false;

		/// <summary>
		/// Controls which shadowing situations produce warnings or errors.
		/// Defaults to warnings for local-local and local-instance collisions.
		/// </summary>
		public ShadowingMode Shadowing { get; init; }
			= ShadowingMode.WarnLocalShadowing
			| ShadowingMode.WarnLocalMemberCollision;

		// Controls the default behavior of `switch` cases.
		public SwitchFallthroughMode SwitchFallthrough { get; init; } = SwitchFallthroughMode.ImplicitBreak;

		// When true, a derived class that reintroduces a base method name with a different
		// signature automatically emits C++ `using Base::name;` so overload resolution sees both.
		// Can be overridden per class/method with [shadow] and [unshadow].
		public bool AutoUnhideBaseMethods { get; init; } = true;

		/// <summary>
		/// Controls whether and where module-scope `using NS;` of a namespace is
		/// emitted. Defaults to <see cref="GlobalUsingNSMode.Leaky"/> for backward
		/// compatibility.
		/// </summary>
		public GlobalUsingNSMode GlobalUsingNS { get; init; } = GlobalUsingNSMode.Leaky;

		// Name of the implicit alias for the first base class inside a class/struct.
		// If empty or null, no alias is created and the name is treated as an ordinary identifier.
		public string BaseClassAliasName { get; init; } = "base";

		// Name of the implicit alias for the enclosing class/struct itself.
		// If empty or null, no alias is created.
		public string OwnClassAliasName { get; init; } = "";

		// Name of the implicit return variable inside a non-void function.
		// If empty or null, no implicit variable is created.
		public string AutoReturnName { get; init; } = "ret";

		/// <summary>
		/// Controls how comparison operators are validated and auto-generated for classes/structs.
		/// <see cref="ComparisonMode.Any"/> means no enforcement.
		/// </summary>
		public ComparisonMode ComparisonOperators { get; init; } = ComparisonMode.Any;

		// Default attributes applied to class, struct, union, and enum declarations that do not
		// already specify the corresponding attribute. Use the same syntax as source attributes,
		// e.g. "rule_of_n=5" or "[rule_of_n=3, pub]". Brackets are optional.
		public string DefaultAttributesClass  { get; init; } = "";
		public string DefaultAttributesStruct { get; init; } = "";
		public string DefaultAttributesUnion  { get; init; } = "";
		public string DefaultAttributesEnum   { get; init; } = "";

		// Default attributes applied to func/proc/method declarations that do not already
		// specify an effect attribute. Not applied to operators (see operator categories below).
		public string DefaultAttributesFunction  { get; init; } = "pure";    // func
		public string DefaultAttributesProcedure { get; init; } = "mutable"; // proc
		public string DefaultAttributesMethod    { get; init; } = "";        // method

		/// <summary>
		/// Categories for operator default-effect policies. The active dialect map supplies an
		/// attribute string such as "pure" or "mutable"; operators that already carry an effect
		/// attribute keep their explicit choice.
		/// </summary>
		public enum OperatorCategory
		{
			Comparison,
			Arithmetic,
			Assignment,
			IncDecrement,
			Subscript,
			Call,
			Indirection,
			Other,
		}

		/// <summary>
		/// Per-category default attributes for operator declarations. Comparison and arithmetic
		/// operators default to pure; assignment and increment/decrement default to mutable.
		/// Subscript, call, indirection, and other operators have no default effect.
		/// </summary>
		public IReadOnlyDictionary<OperatorCategory, string> DefaultAttributesOperatorCategory { get; init; }
			= new Dictionary<OperatorCategory, string> {
				[OperatorCategory.Comparison]   = "pure",
				[OperatorCategory.Arithmetic]   = "pure",
				[OperatorCategory.Assignment]   = "mutable",
				[OperatorCategory.IncDecrement] = "mutable",
			};

		/// <summary>
		/// Controls which concrete size the `float` keyword refers to. `Unspecified` means the
		/// keyword has no binding; the active dialect decides whether it may still be used.
		/// </summary>
		public FloatKeywordMode FloatKeyword { get; init; } = FloatKeywordMode.Unspecified;

		/// <summary>
		/// Controls the result type of mixed signed/unsigned arithmetic.
		/// Defaults to C-style "unsigned wins at equal rank".
		/// </summary>
		public MixedSignednessMode MixedSignedness { get; init; } = MixedSignednessMode.CStyle;

		/// <summary>
		/// Controls how <c>int</c> is treated by the type system. Defaults to
		/// <see cref="DefaultTypeMode.SizeIndeterminate"/>.
		/// </summary>
		public DefaultTypeMode DefaultInt { get; init; } = DefaultTypeMode.SizeIndeterminate;

		/// <summary>
		/// Controls how <c>uint</c> is treated by the type system. Defaults to
		/// <see cref="DefaultTypeMode.SizeIndeterminate"/>.
		/// </summary>
		public DefaultTypeMode DefaultUInt { get; init; } = DefaultTypeMode.SizeIndeterminate;

		/// <summary>
		/// Controls how <c>float</c> is treated by the type system. Defaults to
		/// <see cref="DefaultTypeMode.SizeIndeterminate"/>.
		/// </summary>
		public DefaultTypeMode DefaultFloat { get; init; } = DefaultTypeMode.SizeIndeterminate;

		/// <summary>
		/// Controls how <c>bint</c> is treated by the type system. Defaults to
		/// <see cref="DefaultTypeMode.SizeIndeterminate"/>.
		/// </summary>
		public DefaultTypeMode DefaultBint { get; init; } = DefaultTypeMode.SizeIndeterminate;

		public string GetPointerTemplate( Pointer.Kind kind )
			=> kind switch {
				Pointer.Kind.RawPtr      => "{0}*",
				Pointer.Kind.PtrToAry    => "{0}*",
				Pointer.Kind.LVRef       => "{0}&",
				Pointer.Kind.RVRef       => "{0}&&",
				Pointer.Kind.RawArray    => "{0}[{1}]",
				Pointer.Kind.Unique      => UniquePtrTemplate,
				Pointer.Kind.UniqueArray => UniqueArrayPtrTemplate,
				Pointer.Kind.Shared      => SharedPtrTemplate,
				Pointer.Kind.SharedArray => SharedArrayPtrTemplate,
				Pointer.Kind.Weak        => WeakPtrTemplate,
				Pointer.Kind.WeakArray   => WeakArrayPtrTemplate,
				Pointer.Kind.Array       => "std::array<{0},{1}>",
				Pointer.Kind.Vector      => "std::vector<{0}>",
				Pointer.Kind.Set         => "std::unordered_set<{0}>",
				Pointer.Kind.OrderedSet  => "std::set<{0}>",
				Pointer.Kind.MultiSet    => "std::unordered_multiset<{0}>",
				Pointer.Kind.OrderedMultiSet => "std::multiset<{0}>",
				_ => throw new ArgumentOutOfRangeException( nameof( kind ), kind, null ),
			};

		public string GetPointerMakeTemplate( Pointer.Kind kind )
			=> kind switch {
				Pointer.Kind.Unique      => UniquePtrMakeTemplate,
				Pointer.Kind.UniqueArray => UniqueArrayPtrMakeTemplate,
				Pointer.Kind.Shared      => SharedPtrMakeTemplate,
				Pointer.Kind.SharedArray => SharedArrayPtrMakeTemplate,
				_ => throw new ArgumentOutOfRangeException( nameof( kind ), kind, null ),
			};

		/// <summary>
		/// Returns the concrete float size (in bytes) used for untyped float literals and for
		/// the `float` keyword when <see cref="FloatKeyword"/> is <see cref="FloatKeywordMode.Unspecified"/>.
		/// </summary>
		public int DefaultFloatSize()
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
	/// Controls where module-scope (global) `using NS;` declarations are emitted.
	/// Scoped `using` inside a namespace is always allowed.
	/// </summary>
	public enum GlobalUsingNSMode
	{
		/// <summary>
		/// Emit `using namespace NS;` in the header. This is simple but leaks names
		/// into every translation unit that includes the header (C++ antipattern).
		/// </summary>
		Leaky,

		/// <summary>
		/// Emit `using namespace NS;` only in the implementation file and fully
		/// qualify names in the header. Not yet implemented.
		/// </summary>
		Contained,

		/// <summary>
		/// Reject module-scope `using NS;` entirely.
		/// </summary>
		Disabled,
	}

	/// <summary>
	/// Controls which comparison operators a class/struct is expected to provide, and whether
	/// the compiler may auto-generate defaulted <c>operator==</c> / <c>operator&lt;=&gt;</c>.
	/// </summary>
	public enum ComparisonMode
	{
		/// <summary>No comparison operators are allowed.</summary>
		None = 0,

		/// <summary>Only <c>operator==</c> is allowed/required.</summary>
		Equal,

		/// <summary><c>operator==</c> and <c>operator&lt;</c> are allowed/required.</summary>
		LessThan,

		/// <summary>
		/// Only <c>operator==</c> and <c>operator&lt;=&gt;</c> are allowed; missing ones are
		/// auto-generated as <c>= default</c>.
		/// </summary>
		Spaceship,

		/// <summary>
		/// Only <c>operator==</c> and <c>operator&lt;=&gt;</c> are allowed, but they are not
		/// required and are never auto-generated.
		/// </summary>
		MaybeSpaceship,

		/// <summary>
		/// The class must provide either the full pre-spaceship set
		/// (<c>== != &lt; &gt; &lt;= &gt;=</c>) or the full spaceship set
		/// (<c>== &lt;=&gt;</c>). Mixing the two styles is an error.
		/// </summary>
		All,

		/// <summary>No checks are performed. This is the dialect default.</summary>
		Any,
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

	/// <summary>
	/// Controls shadowing diagnostics. Each category has both a warning and an error bit so
	/// they can be combined independently. Future categories (e.g. base-member collisions)
	/// can add more bits.
	/// </summary>
	[Flags]
	public enum ShadowingMode
	{
		None                          = 0,
		WarnLocalShadowing            = 1 << 0,
		ErrorLocalShadowing           = 1 << 1,
		WarnLocalMemberCollision      = 1 << 2,
		ErrorLocalMemberCollision     = 1 << 3,
	}
}
