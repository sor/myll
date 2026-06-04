# Enum Flags and Operator Generation

## Problem

C++ enums are weak for bitmask flags:
- No bitwise operators by default.
- Manual operator overloading is verbose and repetitive.
- Error-prone: `Flags a = 3;` compiles even if `3` isn't a valid combination.

## Solution

```
[flags, operators(bitwise)]
enum Permissions {
    Read,
    Write,
    Execute
}
```

This single attribute generates:
```cpp
enum class Permissions : std::underlying_type_t<int> {
    Read = 1,
    Write = 2,
    Execute = 4
};

// Generated operators:
constexpr Permissions operator|(Permissions lhs, Permissions rhs) { ... }
constexpr Permissions operator&(Permissions lhs, Permissions rhs) { ... }
constexpr Permissions operator^(Permissions lhs, Permissions rhs) { ... }
constexpr Permissions operator~(Permissions rhs) { ... }
Permissions& operator|=(Permissions& lhs, Permissions rhs) { ... }
Permissions& operator&=(Permissions& lhs, Permissions rhs) { ... }
Permissions& operator^=(Permissions& lhs, Permissions rhs) { ... }
bool operator==(Permissions lhs, std::nullptr_t) { ... }
bool operator!=(Permissions lhs, std::nullptr_t) { ... }
```

## Why `.underlying_type_t`?

Using `std::underlying_type_t` ensures the enum's storage size is minimal while bitwise operations remain well-defined.

## Why Generate All Operators?

Partial generation (only `|`) would be inconsistent. A flags enum should support full boolean-algebra-style manipulation.

## The Zero Comparison

Generated `operator==` and `operator!=` against `std::nullptr_t` allow the idiom:
```cpp
if (flags == nullptr)  // check if no flags are set
```

This uses `nullptr` as a stand-in for "none" due to C++ type system constraints.

## Implementation Notes

- `Enumeration.AttribsAssigned()` in `Decl.cs` synthesizes `Func` AST nodes for these operators.
- This is currently the only place where AST mutation generates new declarations during attribute processing.
