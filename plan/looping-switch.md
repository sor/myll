# Looping switch / `continue` targeting switches

Priority: low

## Idea

Allow `continue` (and `continue N`) to target a `switch` statement.
A switch case could then jump back to the start of the switch and re-evaluate the condition.
This is useful for state-machine parsers that consume input and repeat the same decision logic.

With this semantics, `loop > switch > loop` would support both `break 3` and `continue 3`, because switches would be counted as levels for both statements.

## Possible lowering

Wrap every switch that can be the target of `continue` in a `while(true)` loop:

```cpp
while( true ) {
    bool myll_restart = false;
    switch( x ) {
        case A:
            // ...
            if( needRestart ) {
                myll_restart = true;
            }
            if( myll_restart ) break;
            // ...
        case B:
            // normal case
            break;
    }
    if( !myll_restart )
        break;
}
```

A `continue` from inside a case sets a restart flag and breaks out of the switch.
After the switch, if the restart flag is set, the wrapper loop repeats.

## Open questions

- Should `continue` inside a case act like a normal loop continue, or should it preserve fallthrough until the end of the case body?
- How does this interact with the `fall` keyword and explicit case fallthrough?
- Should every switch pay this wrapper cost, or only switches that are actually targeted by `continue`?
- Does this make generated code harder to follow for the common case where `continue` inside a switch should target the nearest loop?

## Decision

Not implemented for now.
Multi-level `break`/`continue` currently treats only loops as valid `continue` targets.
Revisit this plan if parser or state-machine use cases become a priority.
