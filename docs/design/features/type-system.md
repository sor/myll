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
var x: auto = complex_expr;
```

## Type Aliases

```
alias MyInt = int;
alias Callback = func(int) -> void;
```

## Qualifiers

```
var c: const int;       // const qualifier
var v: volatile int;    // volatile qualifier
var s: stable int;      // Myll's antonym for volatile [may be removed]
var m: mutable int;     // mutable qualifier
```

## Templates

### Basic Templates

```
struct Container<T> {
    data: T[*];
}

func max<T>(a: T, b: T) -> T {
    return a > b ? a : b;
}
```

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
