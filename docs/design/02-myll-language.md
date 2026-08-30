# Myll Language Specification

## Status

This is a living document. Features marked [planned] exist in the grammar or design but are not yet fully implemented.

## Lexical Elements

### Literals
- Integers: decimal, hex (`0x`), octal (`0o`), binary (`0b`), with optional separators (`1_000_000`)
- Floats: standard and scientific notation
- Strings: double-quoted, with escape sequences; unicode support
- Characters: single-quoted
- Booleans: `true`, `false`
- Null: `null`

### Identifiers
- Standard ASCII identifiers
- Unicode mathematical operators as tokens: `·` (dot product), `×` (cross product), `÷` (division)

### Comments
- Line comments: `//`
- Block comments: `/* */`
- Comments are currently discarded; preserving them for output is [planned]

### Shebang
- `#!` lines are supported at file start

## Keywords

### Declarations
`alias`, `class`, `concept` [planned], `const`, `enum`, `func`, `let`, `method`, `module`, `namespace`, `operator`, `proc`, `struct`, `union`, `using`, `var`

### Control Flow
`break`, `catch` [planned], `continue` [planned], `defer` [planned], `do`, `else`, `fall`,
`for` [range-based planned], `if`, `loop`, `return`, `return_if` [planned], `switch`, `throw`, `times`, `try` [planned], `while`

### Types & Modifiers

**Integer types:** `i8`, `i16`, `i32`, `i64`, `int`, `iptr`, `isize`, `u8`, `u16`, `u32`, `u64`, `uint`, `uptr`, `usize`

**Other types:** `auto`, `bool`, `char`, `codepoint` [planned], `f16`, `f32`, `f64`, `f128` [planned], `string`, `void`, `unit` [internal]

**Modifiers:** `const`, `mutable`, `stable`, `volatile`

### Special
`as`, `bit`, `delete`, `dynamic`, `forward`, `move`, `new`, `nullptr`, `reinterpret`, `requires` [planned],
`sizeof`, `static`, `static_cast` [planned], `this`, `typename`

## Modules

```
module my_module;
```

- Files without an explicit module declaration use their filename (minus extension) as the implicit module.
- Multiple `.myll` files declaring the same module merge their output into a single `.h`/`.cpp` pair.
- Import: `import module_name;` or `import path/to/file;` [paths are buggy]

## Namespaces

```
namespace my_ns {
    // declarations
}

// Bodyless namespace: scopes the rest of the file
namespace my_ns:

// Forward namespace declaration (only in .decl.myll / [extern] / external contexts)
namespace my_ns;
```

## Declarations

### Variable Declarations

```
var Type name = init;       // variable declaration; mutable unless Type is const-qualified
const Type name = init;     // preferred form for immutable variables
let Type name = init;       // immutable binding; currently behaves the same as const
global Type name = init;    // global variable [partial]
```

The `const` keyword can either start a declaration (`const int c = 1;`) or qualify the type inside a `var` declaration (`var const int c = 1;`).
Both forms are equivalent, but the leading `const` form is preferred for readability.
`var const Type t;` is the explicit form and is required when the type comes from a template parameter that may be const-qualified.

Multi-declaration:
```
var int a = 1, b = 2, c = 3;
```

### Function Declarations

```
// Returns something (auto-detected or explicit)
func name(params) -> ReturnType { body }
func name(params) { body }  // return type inferred as auto or void

// Procedure: never returns a value
proc name(params) { body }

// Method: member function
method name(params) { body }
```

Parameter passing annotations [partially implemented]:
- `look` (in), `edit` (inout), `share` (copy), `give` (move), `forward` [planned contextually]

### Class/Struct/Union Declarations

```
struct Name {
    // fields, methods
}

class Name : Base {        // public inheritance by default
    // fields, methods
}

class PrivateBase : [priv] Base {
}

class ProtectedBase : [prot] Base {
}

class VirtualBase : [virtual] Base {     // virtual public base
}

class Mixed : [pub] Base1, [priv] Base2, [virtual] Base3 {
}

union Name {
    // variants
}
```

Access control via attributes: `[pub]`, `[priv]`, `[prot]`. They can be applied per-section (`[pub]:`) or per-declaration (`[pub] method f();`). Access attributes are valid only inside class/struct/union declarations.

### Enum Declarations

```
enum Color { Red, Green, Blue }
enum Color : u32 { Red, Green, Blue }  // explicit underlying type [planned]

// Flags enum with auto-generated operators
[flags, operators(bitwise)]
enum Permissions { Read, Write, Execute }
```

### Alias Declarations

```
alias MyInt = int;
alias FuncPtr = func(int) -> void;
```

### Using Declarations

```
using std::vector;
using namespace std;
```

### Properties / Accessors [parsed but not generated]

```
var int count {
    get { return _count; }
    set { _count = value; }
}
```

### Operator Overloading [parsed but not generated]

```
operator+(lhs: Type, rhs: Type) -> Type { body }
```

### Constructors / Destructors

```
// Default constructor
ctor Name(params) { body }

// Destructor
dtor Name { body }

// Special constructors
ctor Name(other: Name)               // copy
ctor Name(other: Name) = move        // move
ctor Name(other: OtherType)          // converting
```

## Types

### Basic Types

| Myll | Size | C++ Equivalent |
|------|------|----------------|
| void | — | void |
| bool | 1 | bool |
| i8 | 1 | int8_t |
| i16 | 2 | int16_t |
| i32 | 4 | int32_t |
| i64 | 8 | int64_t |
| int | ~4 | int |
| isize | varies | ptrdiff_t |
| u8 | 1 | uint8_t |
| u16 | 2 | uint16_t |
| u32 | 4 | uint32_t |
| u64 | 8 | uint64_t |
| uint | ~4 | unsigned int |
| usize | varies | size_t |
| f32 | 4 | float |
| f64 | 8 | double |
| f128 | 16 | __float128 / long double |
| char | 1 | char |
| string | varies | std::string |
| auto | inferred | auto |

`isize`, `usize`, `iptr`, and `uptr` vary with the target. On most systems `iptr`/`uptr` are pointer-sized, while `isize`/`usize` are the corresponding signed/unsigned `size_t` family. `f128` is available only on targets that support an IEEE 128-bit float type.

### Pointer & Reference Types

```
T*      // raw pointer
T&      // lvalue reference
T&&     // rvalue reference

// Smart pointers (suffixes on pointer syntax)
T*!     // std::unique_ptr<T>
T*+     // std::shared_ptr<T>
T*?     // std::weak_ptr<T>

// Array types
T[]     // raw array (pointer)
T[*]    // std::vector<T>
T[@]    // std::array<T, N> [size context-dependent]

// Smart array pointers
T[]!    // unique_ptr<T[]>
T[]+    // shared_ptr<T[]>
T[]?    // weak_ptr<T[]>
```

### Function Pointer Types

```
func(ReturnType, ParamType1, ParamType2) -> ReturnType
```

### Template Parameters

```
struct Container<T> { ... }
func max<T>(a: T, b: T) -> T { ... }
func sort<T>(items: T[*]) -> void requires Comparable<T> [requires planned]
```

## Statements

### Expression Statement
```
expression;
```

### Block
```
{
    // multiple statements
}
```

### Variable Declaration
See Declarations above.

### Assignment
```
x = expr;
x += expr;
x ·= expr;  // dot product assignment
x ×= expr;  // cross product assignment
```

### Return
```
return expr;
return;          // from void function
return expr if condition;  // [planned]
```

### If / Else
```
if condition {
    // then
} else if other_condition {
    // else if
} else {
    // else
}
```

### Switch
```
switch expr {
    case 1 => statement;    // implicit break
    case 2 => {
        // block
    }
    case 3 ... 10 => statement;  // range
    case A, B, C => statement;   // multiple values
    default => statement;
    case Other => {
        fall;  // explicit fallthrough
        // more code
    }
}
```

### Loops
```
// Infinite loop
loop {
    // body
}

// While loop
while condition {
    // body
}

// Do-while loop
do {
    // body
} while condition;

// For loop [C-style works; range-based planned]
for init; condition; increment {
    // body
}

// Times loop (repeat N times)
do 10 times {
    // body
}
do 10 times i {
    // body, i is the counter
}

// Break with depth
break;       // break 1
break 2;     // break out of 2 levels [depth > 1 planned]
```

### Try / Catch [grammar exists, visitor missing]
```
try {
    // body
} catch (e: ExceptionType) {
    // handler
}
```

### Defer [grammar exists, visitor missing]
```
defer {
    // runs at end of scope
}
```

### Throw
```
throw expr;
```

### Empty Statement
```
;
```

## Expressions

### Primary Expressions
- Literals
- Identifiers
- `this` / `self`
- `null`
- Parenthesized: `(expr)`

### Member Access
```
obj.field
obj.method(args)
obj?.field       // null-coalescing member access
obj?[index]      // null-coalescing index
obj?(args)       // null-coalescing call
```

### Function Calls
```
func(args)
func<T>(args)    // explicit template args
obj.method(args)
```

### Postfix Operations
```
expr++
expr--
```

### Prefix Operations
```
++expr
--expr
+expr
-expr
~expr
!expr
*expr          // dereference
&expr          // address-of
```

### Casts
```
(Type)expr          // static_cast — default, most common
(?Type)expr         // dynamic_cast
(-Type)expr         // const_cast
(!Type)expr         // std::bit_cast (C++20)
(!!Type)expr        // reinterpret_cast

// Special casts
(move)expr          // std::move(expr)
(forward)expr       // std::forward(expr)
(copy)expr          // copy cast [not yet implemented]

// CV-modifier casts
(+const)expr        // add const via std::add_const_t
(-const)expr        // remove const via std::remove_const_t
(+volatile)expr     // add volatile
(-volatile)expr     // remove volatile

// Readable aliases
(const)expr         // same as (+const)expr
(mutable)expr       // same as (-const)expr
(volatile)expr      // same as (+volatile)expr
(stable)expr        // same as (-volatile)expr
```

`mutable` and `stable` are only valid inside casts.
Using them as variable modifiers (`var mutable int x;` or `var stable int x;`) is a compile-time error, except that `mutable` may still be used on class fields.

Myll revives the C-style cast syntax but removes its dangerous multi-attempt behavior.
`(Type)expr` is *only* `static_cast` — the most common case. The verbose C++ named casts remain available when explicit documentation is needed.

### New / Delete
```
new T              // raw allocation
new T(args)        // constructed
new T[] (n)        // array [syntax varies]
delete expr;
delete[] expr;
```

### Sizeof
```
sizeof(T)
sizeof expr
```

### Binary Operations (by precedence)
1. `**` (power)
2. `*`, `/`, `%`, `·`, `×`, `÷`
3. `+`, `-`
4. `<<`, `>>`
5. `<`, `<=`, `>`, `>=`
6. `==`, `!=`
7. `&`
8. `^`
9. `|`
10. `??` (three-way conditional)
11. `&&`
12. `||`
13. `??` (null-coalescing)
14. `?:` (ternary conditional)

### Lambda Expressions
```
|params| -> ReturnType { body }
|params| { body }          // return type inferred
```

### Throw Expressions
```
throw expr  // as expression, not statement
```

## Attributes

Attributes are enclosed in square brackets and can appear before declarations:

```
[pub]              // public access
[priv]             // private access
[prot]             // protected access

[virtual]          // virtual function
[override]         // override
[inline]           // inline
[static]           // static member
[global]           // global storage
[hidden]           // hidden/internal linkage

[pure]             // pure function
[nothrow]          // noexcept
[throw]            // may throw
[chain]            // method chaining (returns *this)

[ct]               // compile-time constant (generates constexpr)

[flags]            // enum is bitwise flags
[operators(bitwise)] // generate bitwise ops for enum
[pack(n)]          // pack alignment
[align(n)]         // alignment
[rule_of_n=0]      // suppress special members
[rule_of_5]        // explicit rule of 5
```

## Formatting Conventions

Myll is whitespace-insensitive. The backend formatter handles:
- 4-space indentation
- Intelligent line spacing between declaration groups
- Brace placement (K&R style in generated C++)
