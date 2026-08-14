# Myll's Relationship to C++

## Core Rule

**Myll preserves C++ semantics.** Any valid Myll program should compile to C++ that behaves identically to how a C++ programmer would expect.
Deviations from C++ behavior are intentional, documented here, and require explicit syntax.

## What We Preserve

### Type System
- All C++ types have Myll equivalents with identical sizes and alignment.
- Overload resolution rules remain C++'s (delegated to the C++ compiler).
- Template instantiation semantics are preserved.
- Name mangling is delegated to the C++ compiler.

### Object Model
- Construction and destruction order.
- Virtual dispatch via vtables.
- Memory layout of structs, classes, and unions.
- RAII semantics.

### ABI Compatibility
- Generated C++ can be linked with hand-written C++ in the same executable.
- Myll structs/classes can be used from C++ and vice versa.
- Function calling conventions are unchanged.

## Intentional Deviations from C++

These deviations are the core value proposition of Myll. Each addresses a specific C++ pain point.

### 1. Variable Declaration Syntax

**C++ Problem:** `T v` is ambiguous — declaration or expression? The "most vexing parse."

**Myll Solution:**
```
var Type name;     // Always unambiguously a variable declaration
let Type name;     // Immutable binding
```

### 2. Pointer, Reference & Array Syntax

**C++ Problem:** `int* a, b;` — `a` is pointer, `b` is not. Array and pointer syntax interleave confusingly with the identifier.

**Myll Solution:**
```
var int* a;        // pointer follows type, applies to all declared names
var int[*] a;      // array/vector type
var int[][] a;     // array of arrays (pointer to pointer)
```

### 3. Function Pointer Syntax

**C++ Problem:** Function pointer syntax is notoriously unreadable.

**Myll Solution:**
```
var func(int) -> void fp;     // vs. void (*fp)(int) in C++
```

### 4. Smart Pointer Syntax

**C++ Problem:** `std::unique_ptr<T>`, `std::shared_ptr<T>`, `std::weak_ptr<T>` are verbose and require includes.

**Myll Solution:**
```
var T*! up;        // unique_ptr
var T*+ sp;        // shared_ptr
var T*? wp;        // weak_ptr
```

### 5. Constructor/Destructor Defaults

**C++ Problem:** Implicit constructors can be dangerous (copy, conversion). Private inheritance is the default for `class`.

**Myll Solution:**
- Single-argument constructors are **implicitly explicit** (must use `as`/`static_cast` to invoke).
- `class` inheritance is **public by default**.
- Constructors that can throw must be annotated `[throw]`; default is `[nothrow]`.

### 6. Switch Fallthrough

**C++ Problem:** `switch` cases fall through by default — a common source of bugs.

**Myll Solution:**
- Cases **do not fall through** by default.
- Explicit `fall` keyword required for intentional fallthrough.
- Arrow syntax `=>` for single-statement cases emphasizes the non-falling behavior.

### 7. Implicit Conversions

**C++ Problem:** Integer promotion and narrowing conversions happen silently.

**Myll Solution:**
- Narrowing conversions require explicit cast.
- Integer promotion rules are preserved but flagged at Myll level [planned].
- `null` does not implicitly convert to integer `0`.
- `bool` does not implicitly participate in arithmetic.

### 8. Cast Syntax

**C++ Problem:** C-style casts `(T)expr` are dangerous.
The compiler tries `const_cast`, then `static_cast`, then `reinterpret_cast`, and finally `reinterpret_cast` + `const_cast`.
It stops at the first one that compiles, even if it's not what was intended.
Meanwhile, the named casts (`static_cast`, `dynamic_cast`, etc.) are verbose and inconsistent.

**Myll Solution:**
Myll revives the parenthesized cast syntax but gives each cast kind a **deterministic, single-purpose prefix marker**.
The dangerous multi-attempt behavior of C-style casts is removed.

```
(Type)expr          // static_cast only — the most common case
(?Type)expr         // dynamic_cast
(-Type)expr         // const_cast
(!Type)expr         // std::bit_cast (C++20, safe bit-level reinterpretation)
(!!Type)expr        // reinterpret_cast
```

This preserves the brevity of C-style syntax while making the cast kind explicit and safe. The verbose named casts remain available for documentation purposes.

### 9. East vs. West Const

**C++ Problem:** `const T*` vs `T* const` confusion.

**Myll Solution:**
Qualifiers follow the same pattern; `const` is part of the type declaration syntax, not a separate syntactic concern.

### 10. Attribute Distribution

**C++ Problem:** Attributes (`virtual`, `override`, `inline`, `const`) are scattered syntactically.

**Myll Solution:**
All modifiers are attributes in square brackets before the declaration, uniformly applied:
```
[pub, virtual, override]
method foo() -> void;
```

### 11. No Uncontained Using-Namespac

**C++ Problem:** `using namespace std;` at global scope leaks names uncontrollably.

**Myll Solution:**
`using namespace` is only permitted inside namespace or function scopes, never at global/file scope.

### 12. Modules Replacing Headers

**C++ Problem:** Headers are order-dependent, require include guards, encourage macro abuse.

**Myll Solution:**
The `module` system groups declarations into `.h`/`.cpp` output automatically. No include guards needed. Order independence is enforced.

## Features That Are C++-Compatible But Syntactically Different

These don't change semantics, just syntax:

| Feature | Myll Syntax | C++ Equivalent |
|---------|-------------|----------------|
| Power | `a ** b` | `std::pow(a, b)` |
| Dot Product | `a · b` | manual loop / `std::inner_product` |
| Cross Product | `a × b` | manual computation |
| Division | `a ÷ b` | `(double)a / (double)b` [integral promotion] |
| Times Loop | `do N times { ... }` | `for (int i = 0; i < N; ++i)` |
| Null-Coalescing | `obj?.field` | `obj ? obj->field : nullptr` |

## What Myll Does Not Change (And Why)

### Address-of and Dereference Operators
The `&` and `*` operators remain as in C++. Swapping them was considered (making `*` create a pointer and `&` dereference, which some find more intuitive).
But this would break with every C-family language including C#, Rust, D, and Go.
Rejected for ecosystem consistency.

### Operator Precedence
Precedence is largely preserved. The only changes are where ambiguity was resolved (e.g., power operator `**`).

### Memory Model
`new`, `delete`, constructors, destructors, and RAII are unchanged. Smart pointers are syntactic sugar that expand to standard C++ library types.

## What Requires the C++ Compiler

Myll does not replicate the full C++ type checker.
The generated C++ code is expected to be compiled by a standards-compliant C++ compiler (currently targeting `clang++-15` with `-std=c++20`).
This means:

- Template instantiation errors are caught by the C++ compiler, not Myll.
- Overload resolution is delegated.
- Some invalid Myll programs will generate invalid C++ that fails at the C++ compile stage.

This is an acknowledged limitation. Moving to direct LLVM-IR generation is a future possibility but would sacrifice seamless C++ interop.
