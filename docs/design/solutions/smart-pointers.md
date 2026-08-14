# Smart Pointer Suffixes: Why `*!`, `*+`, `*?`

## Problem

C++ smart pointers are verbose and require cognitive overhead:
- `std::unique_ptr<T>` — 16 characters, awkward to type
- Semantics are hidden in the template name, not visually obvious at the declaration site

The author argues that messy smart pointer parameter lists are not contrived examples.
Real production code is often far worse than any constructed demonstration.

## Solution

Myll adds single-character suffixes to the familiar pointer syntax:

```
var T*! up;    // unique_ptr
var T*+ sp;    // shared_ptr
var T*? wp;    // weak_ptr
```

## Rationale for Each Symbol

| Suffix | Memory | Mnemonic |
|--------|--------|----------|
| `!` | Exclusive | "I own this!" |
| `+` | Shared | "Add to refcount" |
| `?` | Weak | "Maybe valid?" |

## Why Not Named Types?

Named types like `unique_ptr<T>` were considered but rejected because:
- They add verbosity to a construct used very frequently.
- They require `import` or `using` to bring into scope.
- The suffix visually groups with the `*` operator, reinforcing the "pointer" mental model.

## Extension to Arrays

```
var T[]! arr;   // unique_ptr<T[]>
var T[]+ arr;   // shared_ptr<T[]>
```

The array bracket form reuses the same suffixes, maintaining consistency.

## C++ Output

Generated C++:
```cpp
std::unique_ptr<T> up;
std::shared_ptr<T> sp;
std::weak_ptr<T> wp;
```

## Alternative Syntaxes Considered

- `@T`, `+T`, `?T` as prefix operators: rejected — inconsistent with pointer `*` placement.
- `T^` ( borrowed from C++/CLI): rejected — `^` is bitwise XOR, too valuable.
- `T&!`, `T&+`, `T&?` for references: rejected — references don't have the same ownership semantics.

## Future Directions

- Generic ownership annotations beyond smart pointers (e.g., borrowed references).
- Static analysis to verify unique_ptr is never copied.
