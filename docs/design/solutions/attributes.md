# Attribute Syntax: Unified Modifyers

## Problem

C++ scatters modifiers across declaration syntax:
```cpp
virtual void foo() const override;   // 3 modifiers, 3 positions
static inline int bar();             // 2 modifiers, 1 position
```

The positions are arbitrary and hard to remember.

## Solution

Myll collects all modifiers into a single prefix attribute block:

```
[pub, virtual, const, override]
method foo() -> void;

[static, inline]
func bar() -> int;
```

## Attribute Block Grammar

Attributes are comma-separated inside square brackets, placed before the declaration:

```
[attribute1, attribute2(arg), attribute3]
declaration;
```

## Why Prefix?

- **Visibility**: Modifiers are the first thing you read, setting mental context.
- **Extensibility**: New attributes can be added without changing grammar positions.
- **Uniformity**: Same syntax for functions, variables, types, and namespaces.

## Why Square Brackets?

- Visually distinct from other syntax.
- Consistent with C# attributes (familiar to many developers).
- Doesn't conflict with `<>` (templates) or `()` (arguments).

## Access Control via Attributes

Instead of C++'s `public:`, `private:`, `protected:` sections:

```
[pub]
struct Point {
    [priv]
    x: int;
    [pub]
    method get_x() -> int;
}
```

Attribute blocks can apply to multiple subsequent declarations until changed.

## Complex Attributes

```
[flags, operators(bitwise)]       // enum flags generation
[pack(1)]                         // packing alignment
[rule_of_n=0]                     // special member suppression
[requires T > 0]                  // template constraint [planned]
```

## Future Directions

- User-defined attributes (like Rust's procedural macros or C#'s custom attributes).
- Conditional attributes for platform-specific code.
- Attribute reflection at compile time.

## Implementation Notes

- Currently stored as `Dictionary<string, List<string>>` on `Decl` and `Stmt`.
- Strongly-typed `Attribute.cs` enums exist but are disconnected — planned future migration.
