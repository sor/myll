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
var Type name;              // value-initialized (zero for scalars, default-constructed for classes)
[noinit] var Type name;     // explicitly uninitialized
var Type name = _;          // also uninitialized (discard initializer)
const Type name = init;     // preferred form for immutable variables
let Type name = init;       // immutable binding; currently behaves the same as const
global Type name = init;    // global scope variable
```

`const Type name` is the preferred form and is equivalent to `var const Type name`. For a true compile-time constant, use `[ct] const Type name = init;`.

Omitting an initializer means value-initialization. `[noinit]` and its synonym `[uninit]` suppress this and leave the object uninitialized, mirroring C++ default initialization. `[noinit]` cannot be used with `const`.

A discard initializer (`var T a = _`) also requests no initializer; it is lowered to the same uninitialized declaration.

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
    field {
        f64 x;
        f64 y;
    }
}

class Widget : Control {
    // inherits publicly by default
}

class HiddenWidget : [priv] Control {
}

class VirtualWidget : [virtual] Control {     // virtual public
}

class MixedWidget : [pub] Control, [priv] Helper, [virtual] Mixin {
}

union Value {
    field {
        i64 int_val;
        f64 float_val;
    }
}
```

### Field declarations (current syntax)

Inside `class`, `struct`, and `union`, member variables must currently be introduced with the `field` or `var` keyword. Both keywords are accepted in both `class` and `struct`. A `field { ... }` block groups multiple fields.

```myll
class A {
    var bool blah = true;
}

struct B {
    field int i = 9;
}

class C {
    field {
        int  x = 1;
        bool y = false;
    }
}
```

### Access Control

Access attributes work in two forms.

**Section form** applies a default access to all following members until another section changes it. This mirrors C++ `public:`, `private:`, `protected:` sections.

```
class Box {
    // private by default
    field i32 _x;

[pub]:
    method get_x() -> i32 { return _x; }

[priv]:
    method helper() -> void {}
}
```

**Per-declaration form** overrides the current section default for a single member.

```
class Box {
    [priv] field i32 _x;
    [pub]  method get_x() -> i32 { return _x; }
}
```

Only `class`, `struct`, and `union` may contain access attributes. They are an error at module or namespace scope.

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
namespace Graphics:

// Forward namespace declaration
namespace Graphics;
```

## Module Declarations

```
module my_module;
```

- Implicit module name derived from filename if not explicitly declared.
- Multiple files can declare the same module; their contents merge.
- Output files: `my_module.h` and `my_module.cpp`.

## Inheritance and Method Hiding

Myll treats derived-to-base value conversions as object slicing and rejects them in assignments, function arguments, and returns. An explicit value conversion from derived to base emits a warning instead of an error. Use pointers (`Base*`) or references (`Base&`) for polymorphism.

By default, Myll automatically undoes C++ method hiding. If a derived class reintroduces a base method name with a different signature, the generated C++ class contains `using Base::name;` so overload resolution sees the base overloads as well as the derived ones. This applies separately to each class and is not inherited by further derived classes.

Control the behavior with attributes and dialect options:

- `[shadow]` on a method suppresses auto-unhiding for that method.
- `[unshadow]` on a method forces auto-unhiding for that method even when the global default is off.
- `[shadow]` on a `class` or `struct` suppresses auto-unhiding for every method declared directly in that type.
- `[unshadow]` on a `class` or `struct` forces auto-unhiding for every method declared directly in that type.

Method-level attributes take precedence over type-level attributes. The global default can be changed with `Dialect.AutoUnhideBaseMethods` or a dialect switch.

If a method name appears in multiple unrelated base classes, auto-unhiding is ambiguous. Myll skips the `using` declaration and emits a warning.

### `self` and `base`

Inside any class/struct, `self` refers to the current object as a reference (`(*this)`). Use `self.field` and `self.method()` when you want a reference.

`base` and `super` are not lexer keywords. `Dialect.BaseClassAliasName` (default `"base"`) makes the configured identifier behave as a private type alias for the first base class inside any class/struct that inherits. A setting of `null` or `""` disables the alias. If a member, parameter, or local variable uses the same name, Myll emits a shadowing warning and omits the alias from the generated C++ class to avoid duplicate members.

## Attributes on Declarations

See full list in `02-myll-language.md`.

## Implementation Notes

- `Decl` currently inherits from `Stmt` for implementation convenience. This is recognized as technical debt; see `../analysis/03-ast-core.md`.
- The generator separates declarations into access-level buckets for C++ output ordering.
- Function pointer types use a custom `TypespecFunc` AST node that correctly handles the "spiral" C++ declarator syntax.
