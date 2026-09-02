# Type System and Templates

Myll's type system maps directly to C++ with syntactic improvements.

## Basic Types

See `02-myll-language.md` for the complete type table.

## Type Inference

```
var x = 42;         // x is int
let y = 3.14;       // y is f64
func foo() {        // return type inferred from body
    return 42;
}
```

`auto` is also available for explicit inference:

```
var auto x = complex_expr;
```

## Return Type Inference

A function or method without an explicit return type gets its return type from the body.

```
func noReturn() {            // void
}

func doubleIt(int x) {       // int
    return x * 2;
}

func pick(u8 a, u32 b) {     // u32
    if( a > 0 )
        return a;
    return b;
}
```

Rules for multiple return statements:

- Exact matches keep the first type.
- References that differ only by qualifiers keep the first type.
- Numeric types in the same category keep the widest size (for example, `u8` and `u32` produce `u32`).
- Mixed signed/unsigned integer pairs follow the active dialect integer-promotion rule.
- Other mismatches produce an error.

## Method Chaining

The `[chain]` attribute makes a method or operator return a reference to the enclosing class/struct.
`[pure]` chained methods return a `const` reference.
A chained method must not declare a return type.

```
class Counter {
    field { int value; }
[pub]:
    [chain]
    method add(int x) { value += x; }
      // returns Counter&

    [chain, pure]
    method peek() {}
      // returns const Counter&
}
```

The compiler inserts `return self;` at the end of a chained body that can fall through.

## Type Aliases

```
alias MyInt = int;
alias Callback = func(int) -> void;
```

## Qualifiers

```
var const int c;       // const qualifier
var volatile int v;    // volatile qualifier
```

`const` and `volatile` can be used as type qualifiers in declarations.
`stable` and `mutable` are reserved for casts: `(stable)expr` removes `volatile`, and `(mutable)expr` removes `const`.
Using `stable` or `mutable` as variable modifiers is a compile-time error, except that `mutable` may still appear on class fields.

## Templates

### Basic Templates — done

```
class Container<T> {
    field T value;

    [pub]:
    method put(T v) -> void { value = v; }
    method take() -> T     { return value; }
}

func max<T>(T a, T b) -> T {
    return a > b ? a : b;
}
```

Function and class/struct templates with type parameters compile and run.
Explicit template arguments are required on calls and instantiations for now.

### Constraints [Planned]

```
func sort<T>(items: T[*]) -> void
    requires Comparable<T>
{
    // body
}
```

### Template Specialization [Planned]

Not yet implemented in the grammar.

### Variable Templates [Planned]

Not yet implemented.

## Non-Type Template Parameters [Planned]

Need syntactic distinction from type parameters.

## Future Directions

- Concepts (`concept`) and aspects (`aspect`) — grammar stubs exist.
- Stronger type inference (Hindley-Milner style in limited contexts).
- Associated types / type families.
- `where` clauses for constraints.

## Implementation Notes

- `VTpl.cs` handles template argument and parameter visiting.
- Template arguments support type specs and literals.
- `IdTplArgs` represents explicit template instantiation arguments.
