# Control Flow

Myll provides structured control flow with safer defaults than C++.

## If / Else

Standard if-else chain:

```
if condition {
    // then
} else if other {
    // else if
} else {
    // else
}
```

- No parenthesis required around condition.
- Braces are mandatory.

## Switch

```
switch expr {
    case 1 => statement;           // implicit break
    case 2 => {                    // block case
        statement1;
        statement2;
    }
    case 3 ... 10 => statement;    // range
    case A, B, C => statement;     // multiple values
    default => statement;
    case Other => {
        fall;                       // explicit fallthrough
        // more code
    }
}
```

Key differences from C++:
- **No implicit fallthrough.** Each case terminates automatically.
- `fall` keyword required for intentional fallthrough.
- Arrow syntax `=>` for single-statement cases.
- Range cases: `case low ... high`.
- Multiple values: `case A, B, C`.

## Loops

### Infinite Loop

```
loop {
    // runs forever until break
}
```

### While Loop

```
while condition {
    // body
}
```

### Do-While Loop

```
do {
    // body
} while condition;
```

### For Loop

```
for init; condition; increment {
    // body
}
```

Range-based for is planned but not yet implemented.

### Times Loop

```
do 10 times {
    // body, repeated 10 times
}

do n times i {
    // body, i is the counter from 0 to n-1
}
```

The `times` loop is a productivity feature for the common "repeat N times" pattern.

## Loop Else

```
for init; condition; increment {
    // body
} else {
    // runs only if the body was never entered
}

while condition {
    // body
} else {
    // runs only if the body was never entered
}
```

Myll loop `else` is the **"was NOT entered"** branch.
The `else` body runs **ONLY** if the loop condition is false on the first check and the loop body never executes.
If the body runs even once — even if it immediately `break`s — the `else` body is skipped.

> **Warning: This is deliberately NOT Python's loop `else`.**
> Python's `else` runs when the loop finishes normally without a `break`.
> Myll's `else` runs only when the body has executed zero times.
> If you expect Python semantics you will be surprised: in Myll, `break` does not influence whether `else` runs; only whether the body was entered at all matters.

## Break and Continue

```
break;              // break innermost loop or switch
break N;            // break N enclosing breakable constructs (loops + switches)
continue;           // continue innermost loop
continue N;         // continue the Nth enclosing loop; switches are NOT counted
continue case X;    // [planned: continue to specific case]
continue default;   // [planned]
```

- `break N` counts both loops and switches as levels.
- `continue N` counts only loops; switches are skipped when determining the target.
- A plain `break` inside a switch still exits that switch.
- A plain `continue` inside a loop still continues that loop.

`break` and `continue` with an explicit depth are lowered to hidden flags so the exit works through arbitrary nesting of loops and switches.

## Return

```
return expr;
return;              // void return
return expr if condition;  // [planned: conditional return]
```

## Throw

```
throw expr;
```

## Try / Catch [Grammar Exists, Visitor Missing]

```
try {
    // body
} catch (e: ExceptionType) {
    // handler
}
```

## Defer [Grammar Exists, Visitor Missing]

```
defer {
    // runs at end of current scope
}
```

## Future Directions

- `guard` statement (early return with condition).
- Pattern matching switch (beyond current literal/range matching).
- Async/await syntax.

## Implementation Notes

- `ForStmt`, `WhileStmt`, `DoWhileStmt`, and `TimesStmt` have broken `EnumerateDF` implementations that omit the loop body from tree traversal. See `../analysis/03-ast-core.md`.
- `TimesStmt` currently uses a static counter for variable naming — not thread-safe.
- `SwitchStmt` cases insert implicit `break` unless `fall` is present.
