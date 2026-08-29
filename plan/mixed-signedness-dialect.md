# Plan: `Dialect.MixedSignedness` mode

## Goal

Make the result type of mixed signed/unsigned arithmetic configurable, because C's "unsigned always wins at equal rank" rule is surprising. The user wants `int * uint` to produce a signed type when safe.

## Design

1. Add a new enum and `Dialect` property:

```csharp
public enum MixedSignednessMode
{
    CStyle,         // current behavior, matches C/C++ usual arithmetic conversions
    SignedPreferred // signed wins if its size is >= the unsigned size, else unsigned
}

public static class Dialect
{
    public static MixedSignednessMode MixedSignedness = MixedSignednessMode.CStyle;
}
```

2. In `TypeResolver.UsualArithmeticConversions`, switch on `Dialect.MixedSignedness` for the mixed signed/unsigned case.

### `CStyle` (default)

```text
i32 + u32 -> u32   // unsigned wins at equal rank
i32 + u64 -> u64   // unsigned has greater rank
i64 + u32 -> i64   // signed has greater rank
```

### `SignedPreferred`

```text
i32 + u32 -> i32   // signed has equal size, so it wins
i32 + u64 -> u64   // unsigned is strictly larger, must win to avoid overflow
i64 + u32 -> i64   // signed is strictly larger, can represent all u32 values
```

The rule can be expressed as:

- If `signedType.size >= unsignedType.size`, result is signed with that size.
- Otherwise, result is unsigned with the unsigned size.

This matches the intuition "keep the result signed whenever the signed type is large enough to hold every value of the unsigned type".

## Integration

- Default stays `CStyle` so existing tests and C++-idiomatic code do not change unexpectedly.
- Existing tests that exercise mixed arithmetic should be updated or duplicated once the flag is added.
- The flag can be set in unit tests via `Dialect.MixedSignedness = ...` with a `try/finally`, or exposed through the CLI later.

## Future

A richer design could allow per-expression annotations or per-module defaults, but a global dialect switch is the right first step.
