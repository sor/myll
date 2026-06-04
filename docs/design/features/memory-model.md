# Pointers, References, Arrays, and Memory

This document covers Myll's type syntax for indirection, smart pointers, arrays, and memory management declarations.

## Problem Being Solved

C++'s declarator syntax is notoriously difficult:
- `int* a, b;` — only `a` is a pointer
- `int (*fp)(int);` — function pointer readability
- `int arr[10];` vs `int* arr;` — arrays decay
- Smart pointer verbosity: `std::unique_ptr<std::vector<int>>`

Myll moves all type modifiers to follow the base type in a consistent, unambiguous way.

## Raw Pointers and References

```
var p: int*;        // int* (pointer)
var r: int&;        // int& (lvalue reference)
var rr: int&&;      // int&& (rvalue reference)

// Multi-declaration is unambiguous:
var a, b: int*;     // BOTH a and b are int* — impossible in C++
```

## Arrays

```
var arr: int[*];        // std::vector<int>  (dynamic array)
var arr: int[@N];       // std::array<int, N> (fixed size) — N from context
var raw: int[];         // int* (raw array/pointer)
```

The `[*]` syntax was chosen over `[]` for dynamic arrays to distinguish from raw pointers and to visually suggest "box/container".

## Smart Pointers

Suffixes on the pointer syntax:

```
var up: T*!;        // std::unique_ptr<T>
var sp: T*+;        // std::shared_ptr<T>
var wp: T*?;        // std::weak_ptr<T>
```

Mnemonic:
- `!` = "I own this exclusively!"
- `+` = "shared ownership, add refcount"
- `?` = "maybe valid? check before use"

### Smart Arrays

```
var up_arr: T[]!;   // std::unique_ptr<T[]>
var sp_arr: T[]+;   // std::shared_ptr<T[]>
var wp_arr: T[]?;   // std::weak_ptr<T[]>
```

## Complex Combinations

```
var p_arr: int*[*];     // std::vector<int*> (vector of pointers)
var arr_p: int[*]*;     // pointer to vector of ints
var func_ptr: func(int) -> void*;  // function returning void pointer
```

## Function Pointer Types

```
var fp: func(int) -> void;              // vs. void (*fp)(int) in C++
var fp: func(int, int) -> int*;         // function returning int*
var fp: func() -> func(int) -> void;    // function returning function pointer
```

The `func()` syntax is borrowed from type-as-usage philosophy: you describe how the value is used.

## Memory Management

### Allocation

```
var p = new T;          // raw allocation
var p = new T(args);    // constructed allocation
var arr = new T[N];     // array allocation
```

### Deallocation

```
delete p;
delete[] arr;
```

### RAII

Myll relies on C++'s RAII semantics. Smart pointers and containers manage their own memory. `new`/`delete` are available for low-level scenarios but are discouraged.

## Future Directions

- `owned`, `borrowed`, `shared` as explicit type qualifiers (more verbose alternative to suffixes).
- Custom allocators.
- Arena/scoped memory regions.

## Implementation Notes

- `Typespec.Pointer` handles the C++ declarator "spiral" syntax generation via `PointerizeName()`.
- Smart pointer template strings are currently hardcoded to `std::unique_ptr`, `std::shared_ptr`, `std::weak_ptr`.
- `NewExpr.Gen()` currently mutates the AST (`type.ptrs.RemoveAt(0)`) — a critical bug. See `../analysis/03-ast-core.md`.
