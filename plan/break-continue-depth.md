# Multi-Level `break`/`continue` (Loops Only)

## Current State

The grammar already accepts an optional integer literal:

```myll
break 2;
continue 3;
```

`BreakStmt.depth` and `ContinueStmt.depth` store the value.
The generator no longer throws for depth other than `1`; the transformer now lowers the construct.

## Semantics

`break N` exits `N` enclosing loop levels.
`continue N` jumps to the next iteration of the `N`th enclosing loop.
Depth `1` keeps the current behaviour: `break;` / `continue;`.

Switches are NOT counted as loop levels. A `break` inside a `switch` still targets the switch itself.
Making `break N` count switches as well is option 2 and not implemented yet.

## Lowering Strategy

Use hidden boolean flags, one per enclosing loop.
A `break N` sets the break flag of every loop from the target up to the current loop's parent, then emits a normal `break` to exit the current loop.
A `continue N` sets the continue flag of the target loop and the break flag of every intermediate loop, then emits a normal `break`.

Guards are emitted after every statement inside a function body while inside a loop.
This makes the exit immediate, instead of waiting until the end of the loop body.

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
        {
            while( b ) {
                myll_tmp_0 = true;
                break;
                if( myll_tmp_0 ) break;
            }
            if( myll_tmp_0 ) break;
            neverRun();
            if( myll_tmp_0 ) break;
        }
    }
}
```

The dead guard after the explicit `break` is harmless.
Modern C++ compilers remove it and the unreachable branch.

## Implementation

`backend/Resolver/BreakContinueTransformer.cs` keeps a stack of `LoopContext` objects.
Each context receives two flag names eagerly from `CompilationContext.NextTempName()`.

When transforming a block inside a loop, a guard is appended after every statement for every active flag:

- The current loop's own break flag triggers `break`.
- The current loop's own continue flag triggers `continue`.
- Any outer loop's flag triggers `break` to propagate the exit outward.

A loop is wrapped in a scoped block that declares the flags that are actually used.

## Flags and Naming

Use `CompilationContext.NextTempName()` for flag names.
Names are allocated eagerly per loop because guards are generated before the transformer knows which flags will be used.

## Interaction with loop `else`

Loop `else` runs only when the body was never entered, so a non-local break does not change that rule.

## Validation

- Depth must be at least `1`.
- Depth must not exceed the number of enclosing loops.

## Future Work

- Option 2: count switches as break-able levels. This needs a different strategy because switches do not have a single end-of-body point where a guard would catch the exit.
