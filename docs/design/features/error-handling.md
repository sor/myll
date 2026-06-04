# Error Handling

Myll's error handling builds on C++'s exception mechanism with explicit annotations.

## Throw Statements

```
throw expr;
```

## Throw Expressions

```
var result = may_fail() ?? throw Exception();
```

## Try / Catch [Planned]

```
try {
    // body
} catch (e: ExceptionType) {
    // handler
} catch {
    // catch-all
}
```

## Function Annotations

```
[nothrow]
func safe() -> void { }

[throw]
func risky() -> void {
    throw MyException();
}
```

- Functions default to `[throw]` (may throw).
- `[nothrow]` declares noexcept.
- Calling a `[throw]` function from a `[nothrow]` function is an error [checked by C++ compiler, not yet by Myll].

## Defer [Planned]

```
defer {
    // cleanup code
}
```

`defer` executes at scope exit (function return, exception, or end of block), providing Go-like cleanup semantics.

## Future Directions

- `Result<T, E>` type for explicit error handling without exceptions.
- `try?` and `try!` shorthand (inspired by Rust/Swift).
- Algebraic effects.

## Implementation Notes

- `try/catch` grammar exists but visitor override is missing.
- `defer` grammar exists but visitor override is missing.
- The `[throw]` / `[nothrow]` attributes are stored as strings and passed through to C++ `noexcept`.
