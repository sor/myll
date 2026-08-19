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

## Features added in Myll

- Opt-in conventions via dialects (referred to as "rulesets" in earlier design notes).
- Automatic checks for code style and conventions.
- Extensible attributes (planned for later; `aspect` is currently reserved).

## References

- Legacy notes: `Documentation/design_doc_cpp_myll_1.cpp`
- Decision log: `REASONS.md`
