# Multi-Level `break`/`continue`

## Current State

The grammar already accepts an optional integer literal:

```myll
break 2;
continue 3;
```

`BreakStmt.depth` and `ContinueStmt.depth` store the value.
The generator no longer throws for depth other than `1`; the transformer now lowers the construct.

## Semantics

`break N` exits the `N`th enclosing breakable construct (loops **and** switches).
`continue N` jumps to the next iteration of the `N`th enclosing **loop**; switches are not counted.
Depth `1` keeps the current behaviour: `break;` / `continue;`.

A plain `break` inside a `switch` still exits that switch.
A plain `continue` inside a loop still continues that loop.

## Lowering Strategy

Use hidden boolean flags, one per enclosing breakable construct (loop or switch).
A `break N` sets the break flag of every construct from the target up to the current construct's parent, then emits a normal `break` to exit the current construct.
A `continue N` finds the `N`th enclosing loop, sets its continue flag, and sets the break flag of every construct between that target and the current position, then emits a normal `break`.

Guards are emitted after every statement inside a function body while inside a breakable construct.
This makes the exit immediate, instead of waiting until the end of the body.

Because the helper flags are declared outside the targeted construct, they must be reset to `false` at the start of every loop iteration so that a flag set in one iteration does not affect the next.

### Example

Input:

```myll
while( a ) {
    while( b ) {
        break 2;
    }
    neverRun();
}
```

Lowered output:

```cpp
{
    bool myll_tmp_0 = false;
    while( a ) {
        myll_tmp_0 = false;
        {
            while( b ) {
                myll_tmp_0 = true;
                break;
            }
            if( myll_tmp_0 ) break;
            neverRun();
        }
    }
}
```

## Implementation

`backend/Resolver/BreakContinueTransformer.cs` keeps a stack of `BreakableContext` objects.
Each context receives two flag names eagerly from `CompilationContext.NextTempName()`.

When transforming a block, a guard is appended after every statement for every active flag:

- A break flag always triggers `break`.
- A continue flag triggers `continue` only when the context is the current top-level loop; otherwise it triggers `break` to propagate the exit outward.

A loop or switch is wrapped in a scoped block that declares the flags that are actually used.
At the start of every loop body, all flags belonging to active breakable contexts are reset to `false`.

Guards are suppressed after statements that end with an unconditional `break`, `continue`, `return`, or `throw`, so the generated C++ stays clean under high warning levels.

## Flags and Naming

Use `CompilationContext.NextTempName()` for flag names.
Names are allocated eagerly per construct because guards are generated before the transformer knows which flags will be used.

## Interaction with loop `else`

Loop `else` runs only when the body was never entered, so a non-local break does not change that rule.

## Validation

- Depth must be at least `1`.
- `break N` must not exceed the number of enclosing breakable constructs.
- `continue N` must not exceed the number of enclosing loops.
