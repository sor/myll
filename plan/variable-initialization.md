# Variable and constructor initialization syntax

This document collects the current state, the planned direct-constructor change, and the longer-term outlook for aggregate and list initializers in Myll.
Myll is a C++-targeting language, so every source form is judged by whether it can be lowered reliably to C++.
Because this is new syntax, the design intentionally stays at the level of intent and clear C++ mapping rather than over-specifying edge cases.
Nothing here is implemented unless explicitly marked as done.

> **Status: partially implemented.** Direct construction, initializer-list literals, and reference variables are implemented. Aggregate/designated initialization, raw-array list initialization, and static/constexpr direct construction are still future work.

## Current state

A variable declaration today is `kindOfVar` followed by `typespec idAccessors SEMI`.
The current grammar therefore supports these init shapes on the declaration itself:

- No initializer: `var T name;`.
- Assignment initializer: `var T name = expr;`.
- Multiple names: `var T a, b = expr, c;`.
- Property/accessor block: `var T name { get; set; } = expr;`.

The property block syntax is **not set in stone** and is probably not fully working at the moment.
It can be redesigned if another shape serves the language better.

The RHS expression can be any expression, including heap allocation with `new`:

- `var T* p = new T;`
- `var T* p = new T(args);`
- `var T*! p = new T*!(args);`
- `var T*+ p = new T*+(args);`
- `var T[] a = new T[N];`
- `var T[]! a = new T[N]!;`

`new` is heap allocation only.
The most recent implementation makes `var T name;` emit `T name{};` in C++.
Uninitialized Myll variables are value-initialized by default.
`[noinit]` / `[uninit]` opt out of that for the rare low-level case where C++ default-initialization is intended.

## C++ variable initialization forms we need to cover

Myll should expose three distinct brace shapes, each with one clear responsibility:

1. `(...)` for function and constructor calls.
2. `{...}` for aggregate and designated initialization.
3. `[...]` for homogeneous list literals (arrays and list/container constructors).

`=` stays as the copy-initializer from an arbitrary expression.

| C++ output | Myll source | Status |
| --- | --- | --- |
| `T name{};` | `var T name;` | Done |
| `T name(args...);` | `var T name(args...);` | Done |
| `T name{...};` | `var T name{...};` or `var T name = {...};` | Future |
| `T name = expr;` | `var T name = expr;` | Works |
| `T* name = new T(args...);` | `var T* name = new T(args...);` | Works |
| `auto name = std::make_unique<T>(args...);` | `var T*! name = new T*!(args...);` | Works |
| `T name{std::initializer_list<T>{...}};` | `var T name = [...];` or `var T name([...]);` | Done for containers; raw arrays still future |
| `T& name = expr;` | `var T& name = expr;` | Done |
| `static T name(args...);` | `[static] var T name(args...);` | Future |
| `constexpr T name(args...);` | `[ct] let T name(args...);` | Future |

## Planned: direct constructor initializer

Add constructor-call syntax on the declared name itself, so stack/value construction looks like heap construction minus the `new` keyword.

```myll
var Widget w(1, 2);
var Vec2 v(1.0, 2.0);
```

would emit:

```cpp
Widget w(1, 2);
Vec2 v(1.0, 2.0);
```

### Open decisions

1. `var T name();` is **allowed** and emits `T name{};` to avoid the C++ most-vexing-parse problem. `var T name;` remains the normal spelling for default value-init.
2. Reject `var T name(args) = expr;`; a variable is either direct-init or assigned, never both.
3. Multi-declaration direct init works: `var T a(1), b(2);` is supported.

## Outlook: aggregate and list syntax

### `{...}` for aggregates

`{...}` is the Myll aggregate / designated initializer.
It maps directly to the C++ braced-init-list when the target is an aggregate, a class with an `std::initializer_list` constructor that the user intends as aggregate-like, or a C++20 designated-initializer target.

```myll
struct Vec2 { float x; float y; }
var Vec2 v{1.0, 2.0};
var Vec2 w{.x = 1.0, .y = 2.0};
```

would emit:

```cpp
Vec2 v{1.0, 2.0};
Vec2 w{.x = 1.0, .y = 2.0};
```

Because `{...}` has this meaning, the property block syntax must move away from `{...}` after a declared name.
That is fine because the property syntax is a placeholder anyway.

### `[...]` for list literals and arrays

`[...]` is a first-class list literal.
It is the Myll way to say “a sequence of N homogeneous values.”
Depending on the target type it can initialize an array or a list/container constructor.

For an array type it mirrors the array syntax and lowers to aggregate initialization:

```myll
var int[3] arr = [1, 2, 3];
```

would emit something like:

```cpp
int arr[3]{1, 2, 3};
```

For a list/container type it explicitly uses the `std::initializer_list` constructor:

```myll
var vector<int> v = [1, 2];
```

would emit:

```cpp
std::vector<int> v{std::initializer_list<int>{1, 2}};
```

The same literal can be passed as a function argument:

```myll
f([1, 2, 3]);
```

would lower to:

```cpp
f(std::initializer_list<int>{1, 2, 3});
```

If the target type cannot consume a list, that is a compile error; there is no silent conversion to `std::vector`, `std::array`, or a raw array.

### `{...}` is forbidden for arrays and non-aggregate classes

To keep the three syntaxes pulling their weight:

- Arrays use `[...]`, not `{...}`.
- Non-aggregate classes use `(...)` for normal constructors or `[...]` for the `std::initializer_list` constructor.
- `{...}` stays reserved for aggregates and designated initializers.

So these are errors:

```myll
var vector<int> v3{10, 10}; // error: vector is not an aggregate
var int[3] arr{1, 2, 3};    // error: arrays use [...]
```

This removes the C++ footgun where `std::vector<int> v{10,10}` silently calls the initializer-list constructor and gives you 2 elements instead of 10.

### Narrowing conversions

C++ `{}` initialization is attractive because it rejects narrowing conversions.
In Myll that safety is preserved wherever the C++ output naturally uses braces:

- Default value-init: `var T t;` → `T t{};`.
- Aggregate init: `var T t{...};` → `T t{...};`.
- Array init via `[...]`: `var int[3] arr = [1,2,3];` → `int arr[3]{1,2,3};`.
- List init via `[...]`: `var vector<int> v = [1,2,3];` → `std::vector<int> v{std::initializer_list<int>{1,2,3}};`.

Constructor calls written with `(...)` map to C++ function-style init, which does allow narrowing.
That is acceptable because `(...)` is explicitly the “call a constructor like a function” path.
If Myll wants to forbid narrowing there, it should do so in its own semantic analysis rather than by switching to `{...}`, because switching would change overload resolution in dangerous ways for types like `std::vector`.

### Does `[...]` cover all `= {...}` cases?

No, by design it does not.
`[...]` covers homogeneous sequences and arrays.
`{...}` covers aggregate and designated initialization.
Keeping them separate is the whole point.

## Property syntax note

The current `var T name { get; set; }` shape is only a placeholder.
A future property syntax can use a keyword such as `prop` or `accessor`, or a completely different block marker, so it does not collide with `{...}` for aggregate initialization.

## Risks and constraints

- `new` must stay heap-only.
Direct init on the declared name is the stack/value counterpart to `new T(args...)`.
- Direct init with more than one argument parses cleanly only if the argument list is consumed inside `idAccessor` before the `idAccessors` comma loop.
Otherwise `var Vec2 v(1.0, 2.0);` would split at the inner comma.
- Empty direct init `var T name();` must be rejected, because its C++ form would be a function declaration.
- `(...)`, `{...}`, and `[...]` must each have a single, clear responsibility so Myll is more predictable than C++ around initialization.
