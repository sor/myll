# Declarations

This document covers all declaration forms in Myll: variables, functions, types, type aliases, namespaces, and modules.

## General Principles

- All declarations are unambiguous at first sight. There is no most-vexing-parse problem.
- Modifiers are applied uniformly via attributes in square brackets.
- Access control is explicit via `[pub]`, `[priv]`, `[prot]` blocks.
- Initialization is preferred; uninitialized variables are possible but obvious.

## Variable Declarations

### Forms

```
var Type name = init;       // variable declaration; mutable unless Type is const-qualified
const Type name = init;     // preferred form for immutable variables
let Type name = init;       // immutable binding; currently behaves the same as const
global Type name = init;    // global scope variable
```

`const Type name` is the preferred form and is equivalent to `var const Type name`. For a true compile-time constant, use `[ct] const Type name = init;`.

### Constructor initialization

Use the assignment form for types that must be constructed with arguments.

```
var std::ifstream file = std::ifstream("path");
```

The C++ form `var Type name(args);` is intentionally not supported, because it reads like a function declaration. A `ctor(args)` keyword shortcut is planned as a more readable alternative.

### Multi-Declaration

```
var int a = 1, b = 2, c = 3;
```

Multiple names can share a type and initializer list. The number of names and initializers must match.

### Rationale for Keywords

- `var`: Universal variable declaration marker. Works with any type, including const-qualified template parameters.
- `let`: Immutable binding. Currently behaves the same as `const`.
- `const`: Shorthand for `var const`. The `const` qualifier may also appear inside the type.
- `global`: Indicates module-level or namespace-level storage. Distinguishes from locals.

### Future Directions

- Thread-local variables.
- `static` within function context (currently parsed).
- `constexpr` full semantics evaluation.

## Function Declarations

### Forms

```
// General function
func name(params) -> ReturnType { body }
func name(params) { body }          // return type inferred or void

// Procedure: guaranteed no return value
proc name(params) { body }

// Method: instance member
method name(params) -> ReturnType { body }
```

### Parameter Passing [Partially Implemented]

Planned annotations for parameter intent:

```
func foo(look x: int)      // read-only input (const reference)
func bar(edit x: int)      // mutable input/output (reference)
func baz(share x: int)     // copy (pass by value)
func qux(give x: int)      // move (rvalue reference)
```

Current syntax accepts these but full semantic enforcement is pending.

### Special Members

```
// Default constructor
 ctor Name(params) { body }

// Destructor
 dtor Name { body }

// Copy constructor
 ctor Name(other: Name)

// Move constructor
 ctor Name(other: Name) = move

// Converting constructor
 ctor Name(other: OtherType)
```

### Operator Overloading [Parsed, Not Generated]

```
operator+(lhs: Type, rhs: Type) -> Type { body }
```

Supported operators follow C++ rules.

## Type Declarations

### Struct, Class, Union

```
struct Point {
    x: f64;
    y: f64;
}

class Widget : Control {
    // inherits publicly by default
}

union Value {
    int_val: i64;
    float_val: f64;
}
```

### Access Control

```
[pub]
struct Point {
    [priv]
    internal_state: int;

    [pub]
    method get_x() -> f64 { return x; }
}
```

### Enum

```
enum Color { Red, Green, Blue }

// Flags enum with generated operators
[flags, operators(bitwise)]
enum Permissions { Read, Write, Execute }
```

The `[flags, operators(bitwise)]` attribute generates:
- `operator|`
- `operator&`
- `operator^`
- `operator~`
- `operator|=`, `operator&=`, `operator^=`
- `bool operator==(Zero)` and `bool operator!=(Zero)`

### Alias

```
alias Vec3 = Point3D;
alias Callback = func(int) -> void;
```

## Namespace Declarations

```
namespace Graphics {
    // declarations
}

// Bodyless namespace (declarations follow)
namespace Graphics;
```

## Module Declarations

```
module my_module;
```

- Implicit module name derived from filename if not explicitly declared.
- Multiple files can declare the same module; their contents merge.
- Output files: `my_module.h` and `my_module.cpp`.

## Attributes on Declarations

See full list in `02-myll-language.md`.

## Implementation Notes

- `Decl` currently inherits from `Stmt` for implementation convenience. This is recognized as technical debt; see `../analysis/03-ast-core.md`.
- The generator separates declarations into access-level buckets for C++ output ordering.
- Function pointer types use a custom `TypespecFunc` AST node that correctly handles the "spiral" C++ declarator syntax.
