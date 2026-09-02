# Removed C and C++ Features in Myll

This document lists features from C and C++ that Myll deliberately drops. Functionality that has only been renamed is not listed here.

## Removed features

- **The preprocessor** — replaced by attributes and compile-time evaluation where possible.
- **Manual header/implementation split** — Myll generates `.hpp`/`.cpp` files from modules and handles includes automatically.
- **The comma operator** — considered a frequent source of bugs.
- **Unscoped enums** — Myll enums are always scoped, matching the C++ `enum class` behavior.
- **`typedef`** — replaced by `using`.
- **Inline object declarations after a type definition** — e.g. `struct B{} b;` is not valid.
- **Local classes, structs, enums, and functions inside functions** — lambdas replace local functions; types live at module or namespace scope.
- **Forward declarations** — declaration order is irrelevant within a module; import what you need from other modules.
- **Assignment inside expressions** — avoids the classic `if (a = b)` bug.
- **`throw` inside expressions** — exception throwing is statement-level.
- **`goto`** — replaced by structured control flow.
- **Legacy multi-word C type names** — e.g. `unsigned long long` and similar spellings are gone.
- **Uniform `{}` initialization everywhere** — Myll avoids the C++ initializer-list ambiguity.

## Intentional divergences from C++ behavior

- **Object slicing is a hard error** — Passing a derived class value where a base class value is expected (e.g. `Base b = derived;` or `func takeBase( Base b )` called with a `Derived`) is rejected. An explicit value conversion from derived to base emits a warning instead of an error. Use a pointer or reference.
- **Method hiding is automatically undone** — If a derived class redeclares a base method name with a different signature, Myll emits C++ `using Base::name;` inside the derived class so overload resolution sees both the base and the derived overloads. This matches the behavior in C# and avoids the C++ pitfall where `d.do(99)` silently resolves to an unrelated `do(f32)`. The behavior is controlled by `Dialect.AutoUnhideBaseMethods` (default true) and can be overridden per class or per method with `[shadow]` (suppress) and `[unshadow]` (force). A class-level attribute applies to all methods declared directly in that class and is not inherited by further derived classes. If the same name exists in multiple unrelated bases, auto-unhiding is skipped and a warning is emitted.
- **`base` is not a keyword** — `base` and `super` were removed from `CLASS_LIT`. `Dialect.BaseClassAliasName` (default `"base"`) makes the configured name behave as a private type alias for the first base class inside any inheriting class/struct. Set it to `null` or `""` to disable the alias and leave the name as an ordinary identifier. A warning is emitted if the alias is shadowed by a member, parameter, or local variable.
- **`self` is a reference to the current object** — Inside a non-static method `self` resolves to `(*this)`, so member access uses the dot operator (`self.field`). `this` remains available as the raw pointer.
- **`x: Type;` field shorthand is intentionally not supported** — Class/struct fields must use the `field` or `var` keyword: `field int x;`.
- **`method` / `meth` are the preferred aliases for `func` inside classes and structs** — they are valid everywhere `func` is valid.

## Features added in Myll

- Opt-in conventions via dialects (referred to as "rulesets" in earlier design notes).
- Automatic checks for code style and conventions.
- Extensible attributes (planned for later; `aspect` is currently reserved).

## References

- Legacy notes: `Documentation/design_doc_cpp_myll_1.cpp`
- Decision log: `REASONS.md`
