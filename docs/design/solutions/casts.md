# Cast Syntax: Parenthesized Prefix Casts

## Problem

C provides a lightweight cast syntax: `(Type)expr`. However, in C++ this syntax is dangerous because the compiler tries multiple cast strategies in order and picks the first that compiles:

1. `const_cast`
2. `static_cast` (with extensions)
3. `static_cast` + `const_cast`
4. `reinterpret_cast`
5. `reinterpret_cast` + `const_cast`

This "try everything" behavior means `(Type)expr` might silently perform a `reinterpret_cast` when a `static_cast` was intended. Many C++ projects ban C-style casts entirely.

Meanwhile, the named C++ casts are verbose:
- `static_cast<T>(expr)` -- 16 characters
- `dynamic_cast<T>(expr)` -- 16 characters
- `const_cast<T>(expr)` -- 16 characters
- `reinterpret_cast<T>(expr)` -- 22 characters

## Solution

Myll revives the parenthesized cast syntax but makes each cast kind **deterministic** by adding a prefix marker inside the parentheses:

```
(Type)expr          // static_cast<Type>(expr)
(?Type)expr         // dynamic_cast<Type>(expr)
(-Type)expr         // const_cast<Type>(expr)
(!Type)expr         // std::bit_cast<Type>(expr)
(!!Type)expr        // reinterpret_cast<Type>(expr)
```

### Default Behavior: static_cast

`(Type)expr` -- no prefix marker -- is always `static_cast`. This is safe because `static_cast` is the most common and least dangerous cast. It cannot cast away `const` or perform bit-level reinterpretation.

### Prefix Markers

| Marker | Cast Kind | Mnemonic |
|--------|-----------|----------|
| (none) | `static_cast` | Default, safe |
| `?` | `dynamic_cast` | "Is this really a Type?" |
| `-` | `const_cast` | "Remove a qualifier" |
| `!` | `std::bit_cast` | "Bit-level!" (safe reinterpretation) |
| `!!` | `reinterpret_cast` | "Very dangerous -- double warning" |

### Special Casts

Myll also provides parenthesized casts for common operations that are technically casts in C++:

```
(move)expr          // std::move(expr)
(forward)expr       // std::forward<decltype(expr)>(expr)
(copy)expr          // copy cast [not yet implemented]
```

### CV-Modifier Casts

For adding or removing `const`/`volatile` in generic code:

```
(+const)expr        // std::add_const_t<decltype(expr)>(expr)
(-const)expr        // std::remove_const_t<decltype(expr)>(expr)
(+volatile)expr     // std::add_volatile_t
(-volatile)expr     // std::remove_volatile_t
```

## Examples

```
var basePtr: Base* = new Derived();
var derivedPtr = (?Derived*)basePtr;    // dynamic_cast

var constVal: const int = 42;
var mutableVal = (-int)constVal;        // const_cast

var intBits: int = 0x3F800000;
var floatVal = (!float)intBits;         // std::bit_cast

var rawPtr: void* = &intBits;
var typedPtr = (!!int*)rawPtr;          // reinterpret_cast

// Chaining casts (reads left-to-right)
(?Derived*)(-const)(Base const*)my_var;
```

## C++ Output

| Myll | C++ |
|------|-----|
| `(int)val` | `static_cast<int>(val)` |
| `(?int)val` | `dynamic_cast<int>(val)` |
| `(-int)val` | `const_cast<int>(val)` |
| `(!int)val` | `std::bit_cast<int>(val)` |
| `(!!int)val` | `reinterpret_cast<int>(val)` |
| `(move)val` | `std::move(val)` |
| `(+const)val` | `std::add_const_t<decltype(val)>(val)` |

## Design Decisions

**Why parenthesized instead of postfix?**

- Familiar to C/C++ programmers -- no new syntax to learn for the common case.
- Prefix markers fit naturally inside parentheses -- `(?Type)` clearly signals something special before the type name.
- Postfix syntax (`expr as Type`) was considered but would require Myll to adopt postfix style more broadly to avoid inconsistency. Spaces in postfix casts also complicate parsing.

**Why is `!` bit_cast and `!!` reinterpret_cast?**

- Single `!` is "bit-level" reinterpretation -- safe for trivially copyable types (C++20 `std::bit_cast`).
- Double `!!` is "dangerous reinterpretation" -- visually emphasizes the warning.

**Why keep the verbose named casts?**

The C++ named casts (`static_cast<T>(expr)`, etc.) remain valid Myll syntax for explicit documentation and migration scenarios.

## Future Considerations

A postfix cast syntax (`expr as Type`) was considered for easier chaining (e.g., `expr as Type as OtherType`). This would only work well if Myll moved more fully toward postfix notation. Currently not implemented.

## Implementation Notes

- Grammar rule: `expr` -> `PreExpr` -> `(QM|MINUS|EM|EM EM)? typespec` inside parentheses.
- `VExpr.VisitPreExpr()` dispatches on the prefix token to set the `CastKind`.
- `move` and `forward` create synthetic `TypespecNested` nodes referencing `std::move`/`std::forward` -- a pragmatic implementation hack.
