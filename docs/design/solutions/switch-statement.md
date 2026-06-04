# The `switch` Statement: Safe by Default

## Problem

C++ `switch` fallthrough is a notorious source of bugs:
```cpp
switch (x) {
    case 1: do_something();
    case 2: do_other();   // BUG: fell through from 1
}
```

Programmers forget `break`, or add it and then later add code above, breaking the fallthrough. The German saying **"Switch fällt von selbst"** ("The switch falls by itself") captures exactly this unwanted behavior.

## Solution

Myll makes non-falling the default and requires explicit opt-in:

```
switch x {
    case 1 => do_something();       // auto-break
    case 2 => do_other();           // auto-break
    case 3 => {
        fall;                        // explicit fallthrough
        more_code();
    }
}
```

## Design Elements

### Arrow Syntax for Single Statements

`=>` visually suggests "maps to" or "yields," reinforcing that the case terminates:
```
case 1 => statement;
```

### Block Syntax for Multiple Statements

```
case 2 => {
    stmt1;
    stmt2;
    fall;
    stmt3;
}
```

### Ranges

```
case 0 ... 10 => handle_small();
```

### Multiple Values

```
case A, B, C => handle_group();
```

### Default

```
default => handle_default();
```

## Why `fall` and Not `break`?

In C++, `break` means "stop falling through." In Myll, there's no fallthrough by default, so `break` would be meaningless. `fall` is the new concept — "deliberately fall through to next case" — so it gets the new keyword.

## Future Directions

- Pattern matching: `case Some(x) => ...`.
- Guard expressions: `case n if n > 0 => ...`.
