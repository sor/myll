# Myll by Example

This page is a quick, example-driven tour of Myll.
If you are new to programming, welcome.
Myll looks like other C-family languages, but it tries to remove the parts that confuse beginners and experienced developers alike.
It highlights what is deliberately the same as C++ and what was changed because C++'s current spelling repeatedly causes friction or bugs.
The examples use Myll syntax that the grammar accepts; a few constructs are still being wired through the generator and are called out below.

## Arguments for Myll

Myll is not a reaction against the ideas behind C++.
It keeps the control, the ABI, the zero-cost abstractions, and the library ecosystem.
The goal is to remove the parts of C++ that make the language hard to learn, easy to misuse, and tedious to refactor.

The thesis states eight principles that drive every change:

1. Don't ask the user to repeat themselves.
2. Exceptional behavior needs to be explicit.
3. What you naively expect to happen and has no side-effects can be implicit.
4. Don't break with the general semantics of C++.
5. Do break with C if there is a benefit.
6. Evolve the syntax so it contains no ambiguity.
7. Be useful even if a one-time translation to C++ is all that is wanted.
8. Don't be frugal with new keywords if readability benefits.

The examples below are grouped by which of those principles they serve.

## Hello, world

```myll
module hello;
import std_iostream;

func main() -> int
{
    using std;
    cout << "Hello, World!\n";
    return 0;
}
```

> **Kept from C++:** `main`, namespaces, the standard library, stream I/O.  
> **Changed:** `func` keyword; return type follows the parameter list.

## Variables and basic types

A variable declaration starts with `var`, `const`, `let`, or `global`. The mutability is part of the type.

```myll
var int    count   = 42;
var u8     red     = 255;
var f64    pi      = 3.14159;
var bool   ok      = true;
var string message = "Hello";

const int  answer    = 42;   // preferred immutable variable form
var const int locked = 100;  // explicit form, useful when the type comes from a template
[ct] const int magic = 42;   // compile-time constant
```

Myll's built-in scalar types are the same types C++ programmers already know, just with simpler names.

| Myll        | Size in bytes | C++ equivalent   | Meaning                       | Notes              |
|-------------|---------------|------------------|-------------------------------|--------------------|
| `int`       | usually 4     | `int`            | signed integer                |                    |
| `uint`      | usually 4     | `unsigned int`   | unsigned integer              |                    |
| `i8`        | 1             | `int8_t`         | signed 8-bit integer          |                    |
| `i16`       | 2             | `int16_t`        | signed 16-bit integer         |                    |
| `i32`       | 4             | `int32_t`        | signed 32-bit integer         |                    |
| `i64`       | 8             | `int64_t`        | signed 64-bit integer         |                    |
| `u8`        | 1             | `uint8_t`        | unsigned 8-bit integer        |                    |
| `u16`       | 2             | `uint16_t`       | unsigned 16-bit integer       |                    |
| `u32`       | 4             | `uint32_t`       | unsigned 32-bit integer       |                    |
| `u64`       | 8             | `uint64_t`       | unsigned 64-bit integer       |                    |
| `isize`     | varies        | `ptrdiff_t`      | signed pointer-sized integer  | very likely CPU bit-size in bytes |
| `usize`     | varies        | `size_t`         | unsigned pointer-sized integer| very likely CPU bit-size in bytes |
| `iptr`      | varies        | `intptr_t`       | signed pointer-sized integer  | planned; very likely CPU bit-size in bytes |
| `uptr`      | varies        | `uintptr_t`      | unsigned pointer-sized integer| planned; very likely CPU bit-size in bytes |
| `f32`       | 4             | `float`          | 32-bit floating-point number  |                    |
| `f64`       | 8             | `double`         | 64-bit floating-point number  |                    |
| `bool`      | 1             | `bool`           | true or false                 |                    |
| `char`      | 1             | `char`           | single byte character         | not a number       |
| `codepoint` | 4             | `char32_t`       | Unicode code point (UTF-32)   | for Unicode text   |
| `string`    | varies        | `std::string`    | UTF-8 text                    |                    |
| `void`      | —             | `void`           | no value                      | rarely needed      |

Use `i32` or `u32` when you need exactly 32 bits.
`int` and `uint` are the comfortable defaults.
They are usually 32-bit, but their exact size depends on the target architecture.
`void` is also far less necessary than in C and C++; a function that returns nothing is just `func name() { ... }`.
For ASCII text, one `char` already represents one character.
Use `codepoint` when you need a full Unicode scalar value, because `string` stores UTF-8 text.
Iterating over a string steps one or more bytes and yields a UTF-32 code point.

> **Kept from C++:** the same sizes, signedness, and ABI.  
> **Changed because:** C++ names such as `short`, `unsigned long`, `long long`, and `long double` have platform-dependent sizes and are made of multiple words.
> Myll replaces them with fixed-width names (`i8` to `i64`, `u8` to `u64`) and the simple aliases `int` and `uint`.  
> **Also changed:** `var`, `let`, and `const` make declarations unambiguous.
> `var` is a universal variable marker, even when the type is const-qualified (`var const int i`).
> `const` at the start of a declaration is the preferred form and is equivalent to `var const`.
> For a true compile-time constant, use the `[ct]` attribute.

## If statement

```myll
func sign(int x) -> int
{
    if (x > 0) {
        return 1;
    } else if (x < 0) {
        return -1;
    } else {
        return 0;
    }
}
```

> **Kept from C++:** the same branching semantics.  
> **Changed:** nothing important here; `if` works the way you expect, with `else if` and `else` chains.

## Control flow

```myll
loop {
    // infinite loop
}

for (var int i = 0; i < 10; ++i) {
    // C-style for
}

while (cond) {
    // ...
}

do {
    // ...
} while (cond);

do 10 times {
    // repeat ten times
}

do 5 times i {
    // i counts 0 .. 4
}
```

> **Kept from C++:** structured loops with the same semantics.  
> **Changed:** `loop` replaces `for(;;)`, and `do ... times` gives a readable counted-repeat form.

## Enums and switches

```myll
enum Color { Red, Green, Blue }

[flags]
enum Permission { Read, Write, Execute }

func describe(Color c) -> const char*
{
    switch(c) {
        case Red   => return "red";
        case Green => return "green";
        case Blue  => return "blue";
    }
}

func label(int age) -> const char*
{
    switch(age) {
        case 0  ... 12 => return "child";
        case 13 ... 17 => return "teenager";
        case 18 ... 66 => return "worker";
        else            => return "pensioner";
    }
}
```

> **Kept from C++:** enum underlying integer representation, flag-bit use, and the scoped behavior of C++ `enum class`.  
> **Changed because:** silent fall-through is a common bug.  
> Cases do not fall through unless you write `fall`, and ranges/comma cases remove tedious chains of `case` labels.

## Pointers, arrays, and smart pointers

Once you are comfortable with values, Myll adds the same indirection tools as C++ but with clearer syntax.

```myll
var int*   p = &x;    // pointer is part of the type
var int&   r = x;     // reference
var int[4] a;         // array of four ints
var int@[] fixed;     // std::array-backed fixed-size array

var int*!  uniq  = new int!;  // unique_ptr syntax sugar
var int*+  shared = new int+; // shared_ptr syntax sugar
var int*?  weak;              // weak_ptr syntax sugar
```

> **Changed because:** `int *ip, jp;` in C/C++ silently gives two different types.
> In Myll every modifier is postfix and applies to the whole declaration, so `var int* a, b;` means two pointers.
>
> Smart pointers keep the same ownership rules as `std::unique_ptr`, `std::shared_ptr`, and `std::weak_ptr`; only the spelling is shorter.

## Functions

```myll
func add(int a, int b) -> int { return a + b; }
func greet() { cout << "hi\n"; }
func square(int x) -> int => x * x;

proc side_effect_only() { /* never returns a value */ }
```

> **Kept from C++:** value semantics, overload resolution, inlining, the generated ABI.  
> **Changed:** return type after parameters; arrow bodies for single-expression functions; `proc` for procedures that never return a value.

## No forward declarations

```myll
func main() -> int
{
    later(); // defined below, no prototype needed
    return 0;
}

func later() { cout << "called\n"; }
```

> **Changed because:** C++ forces the user to write declarations twice or worry about ordering.  
> In Myll there is only one declaration; the compiler emits C++ prototypes automatically when the generated order requires them.

## Parameter passing

```myll
func inspect([look]   int* p) { /* read through p */ }
func mutate([edit]   int x)   { x = x + 1; }
func own([give]     int*! p) { /* take ownership */ }
func copy_in([share] int x)   { /* local copy */ }

// call sites
inspect(&value);
copy_in(x: 42);      // named argument
own(p: move ptr);    // explicit ownership transfer
```

> **Changed because:** C++ pointer and reference parameters hide intent in the type spelling.  
> Myll declarations say what the function intends to do with the value, and named arguments free the caller from remembering positional order.
>
> Note: parameter annotations and named arguments are parsed; full code generation is still in progress.

## Classes, methods, and constructors

```myll
class Vec2 {
    field f32 x;
    field f32 y;
[pub]:
    ctor(f32 x_, f32 y_) { x = x_; y = y_; }

    [inline]
    method len_sq() -> f32 => x * x + y * y;
}

class Point3D : Vec2 {      // public inheritance by default
    field f32 z;
}

class Checked {
    [implicit]
    ctor(int i) {}          // opt-in implicit conversion
}
```

> **Kept from C++:** inheritance, method dispatch, constructors, destructors, access control, the rule of zero/five.  
> **Changed because:** single-argument constructors are `explicit` by default; inheritance is public by default.  
> Opting in is one keyword; accidental conversions and accidental private inheritance disappear.

## Templates

```myll
class Stack<T> {
    const usize DefaultCapacity = 8;
    field usize size = 0;
    field T[]!  data = new T[DefaultCapacity]!;
[pub]:
    ctor() {}

    method push(T val) -> Stack& {
        data[size] = val;
        ++size;
        return self;
    }

    method pop() -> T {
        --size;
        return data[size];
    }
}
```

> **Kept from C++:** monomorphisation, specialization behavior, zero-cost generics.  
> **Changed:** templates live in the same file and use the same declaration syntax as non-template code.  
> Making a class generic is just adding `<T>`; no separate header shuffle is required.

## Casts

```myll
var f64 d = 3.14;
var int  i = (int)d;        // static_cast
var u32  b = (!u32)d;       // std::bit_cast
var raw  r = (!!void*)p;    // reinterpret_cast
var m    = (move)obj;       // std::move
```

> **Changed because:** C's cast tries several dangerous conversions in order.  
> Myll keeps the familiar `(Type)expr` spelling but maps it to a single, safe `static_cast`.  
> The other casts are one-symbol prefixes instead of long keywords.

## Modules and namespaces

```myll
module mylib;
import std_iostream;

namespace mylib::detail {
    func helper() { /* ... */ }
}

func public_api() -> int { /* ... */ return 0; }
```

> **Kept from C++:** namespaces, separate compilation, name lookup rules in generated code.  
> **Changed:** `import` is a real module system, not textual inclusion.  
> Files that declare the same module are merged into one generated header/implementation pair.

## Error handling

```myll
[throw]
func risky() { throw "problem"; }

func caller()
{
    try {
        risky();
    } catch (Exception e) {
        // handle
    }
}
```

> **Changed because:** in C++ every function is assumed to throw unless marked otherwise.  
> In Myll throwing is opt-in with `[throw]`, so the common case is the safe default and `nothrow` is the implied baseline.

## Casts and compile-time constants

```myll
const int c = 42;
var int i = (mutable)c;       // remove const

var int volatile* p;
var int* q = (stable)p;       // remove volatile

[ct] const int max = 100;     // compile-time constant
```

> **Changed because:** C's cast tries several dangerous conversions in order.  
> Myll keeps the familiar `(Type)expr` spelling but maps it to a single, safe `static_cast`.  
> The other casts are one-symbol prefixes or readable aliases.  
> `mutable` and `stable` are cast-only keywords; they are invalid as variable modifiers.

## A complete example: a tiny generic stack

```myll
module MyContainers;
import std_iostream;

namespace JanSordid::Container:

[rule_of_n=0]
class MyStack<T> {
    const usize _default_reserved = 8;
    field usize _reserved = _default_reserved;
    field usize _size = 0;
    field T[]!  _data = new T[_reserved]!;

[pub]:
    ctor() : ctor(_default_reserved) {}

    ctor(usize reserved) {
        _reserved = reserved;
        _data = new T[_reserved]!;
    }

    [pure]
    method isEmpty() -> bool => _size == 0;

    [pure]
    method top() -> T => _data[_size - 1];

    [chain]
    method push(T val) -> MyStack& {
        if (_size >= _reserved)
            grow();
        _data[_size] = val;
        ++_size;
        return self;
    }

    method pop() { --_size; }

[priv]:
    method grow() {
        const usize new_reserved = _reserved * 2;
        var T[]! new_data = new T[new_reserved]!;
        do _size times i {
            new_data[i] = _data[i];
        }
        _reserved = new_reserved;
        _data = (move)new_data;
    }
}
```

> **What this shows:** no split header, no forward declarations, and generic code that looks like ordinary code.  
> It also shows smart-pointer sugar, `self` in place of `return *this`, and attributes (`pure`, `chain`) that document intent.

## Where to go next

For the full syntax and semantics, read [`02-myll-language.md`](02-myll-language.md).
For the C++ contract that Myll promises to preserve, read [`03-cpp-relationship.md`](03-cpp-relationship.md).
