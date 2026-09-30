# Untyped-constant folding follow-ups

Status: **implemented** (2026-09-30, same session as the unpushed-commits review). Follow-up
to the 2026-09-29 review of the untyped-integer folding work (commits `47cb86e`/`931c5e0`).
Four findings, four fixes; nothing committed yet at the time of writing this doc.

## 1. Fold-vs-emission UB gap

**Problem.** The fold only fed Myll-side fit checks; the generator emitted the original
expression verbatim. `var u64 x = 1 << 62;` passed Myll (2^62 fits u64) but emitted C++
`1 << 62` — UB for a 32-bit int shift count. Same class: products like
`1024 * 1024 * 1024 * 8` (signed overflow), and intermediate-only overflow like
`2000000000 + 2000000000 - 2000000000`.

**Fix** (chosen option B, "replace only when unsafe"): `BinOp.Gen()` in
`backend/Core/Expr.cs` checks its resolved type; when the node is a folded untyped
constant, it decides whether the plain emission is well-defined C++:

- Shifts: replace when the count is negative or ≥ the left operand's natural width.
  Negative left operands are replaced too (C++20 made signed left shifts wrap, which
  would silently produce a wrong value).
- Other folded ops: compute each operand's natural literal type (`int`, then `long long`,
  then `unsigned long long` — the clang++/g++ extension past `long long`). Replace when
  the folded result does not fit the common natural type, or when the operand
  signedness classes are mixed (C++ converts the signed side by wrap-around).

The replacement emits the exact decimal constant (`4611686018427387904`), braced when
negative. Readable expressions that C++ evaluates correctly stay as written
(`1024 * 1024 * 1024`); only unsafe sub-expressions are replaced, so intermediates are
handled recursively (`4000000000 - 2000000000`).

## 2. `literalValue` clone drift

**Problem.** The folded-value field was propagated through only 3 of 5 clone helpers;
`ConversionRules.CloneTypespecBase` and `DiscardTransformer.CloneTypespecBase` dropped
it (and `literalText`).

**Fix.** Both clones copy `literalText` + `literalValue` now. The new `literalValues`
field (see 3) was added to all five clones from the start. Full unification remains the
separate `enhancement-typespec-cloner.md` work. Unit test:
`ResolverTests.CloneTypespec_PreservesUntypedConstantValues`.

## 3. `/`, unary minus, ternary

**Problem.** `Operand.Divide` resolved to a null type (delegated to C++, so no
fit-check and no typed type); unary minus kept the positive literal value (`var i8 x =
-128;` was rejected because 128 does not fit i8); a ternary of untyped constants got a
value-less untyped type, so even `var i32 x = cond ? 5 : 3;` was rejected.

**Fix** (`backend/Resolver/TypeResolver.cs`):

- `Divide` joins `IsArithmeticOperation` and folds like the others (divide-by-zero is
  not folded). BigInteger division truncates toward zero, matching C++.
- `FoldUntypedNegation`: negating an untyped constant keeps the untyped type with the
  negated exact value.
- `ResolveTernOp`: a ternary over two untyped constants keeps the untyped type with
  `TypespecBasic.literalValues` — the union of the possible branch values (a nested
  ternary contributes its set). `ConversionRules.CollectUntypedValues`/`GetUntypedLiteralRank`
  then require **all** values to fit the target, mirroring "a constant binds to any type
  that can hold it". A single `literalValue` cannot express this for mixed-sign sets.

**Known edge:** the negation of a value beyond `long Long` range (e.g. `-18446744073709551615`)
cannot bind to any target (rank None), which is correct; its raw emission would wrap in
C++ either way (pre-existing, unchanged).

## 4. `TryParseInteger` negatives + the `[flags]` enum crash

**Problem.** `TryParseInteger` used `NumberStyles.None`, so it could not parse negatives
(the review recommended it for the `[flags]` enum fix where negatives are legal). And
`EnumTransformer.NumberFlags` used `uint.Parse(lit.text)`, which crashed
(`FormatException`) for hex/binary values.

**Fix:**

- `ConversionRules.TryParseInteger` accepts an optional leading `+`/`-` (the lexer never
  produces one; the sign applies to all radixes) and parses decimal via the same
  `TryParseRadix` path (no exception-based fallback anymore).
- `EnumTransformer.NumberFlags` uses `TryParseInteger` and requires literal values to be
  non-negative u32 values (negative values spelled as unary-minus-literal are
  recognized). Auto numbering only applies to entries without a value. Computed values
  (e.g. `Yellow = Red | Green`, `A = 1 + 2`) are **left untouched** — the transform runs
  pre-resolution, so it cannot distinguish constants from computed expressions — and are
  emitted as-is; they are transparent to the auto-numbering chain (it keeps doubling), and
  C++ validates them as constant expressions. Previously they were silently replaced by
  the next auto number (silent corruption). `IsPowerOfTwo` now takes
  `BigInteger` so the doubling cannot overflow.

Tests: `testing/cases/language_basics/enum.myll` gains a hex `[flags]` enum
(`TestFlagsHex`) and a composite one (`TestFlagsComposite`: `Yellow = Red | Green`,
`Mask = Red | Green | Blue`, plus a value-less entry after the composites locking the
transparent chain at `Extra = 8`); `testing/cases/decl_and_attributes_fail/` gains the
negative-value negative.

## Verification

- `dotnet build myll.sln` clean; `dotnet test testing/` green (142 tests).
- `testing/cases/new_in_myll/compound_constants.myll` covers every fix end-to-end: unsafe
  shift/product/intermediate emission, readable-safe emission kept, division, unary minus,
  ternary value-set binding; goldens regenerated and clang++-compiled.
- Manual repros in `tmp/collision_check/fold34/`, `flags_neg/`, `flags_pos/`.

## Follow-ups not done here

- Structural signature/type equality instead of `Gen()` strings (`enhancement-signature-equality.md`).
- Shared Typespec cloner (`enhancement-typespec-cloner.md`).
- Myll-side validation of enum-vs-integer comparisons (the clang++ "invalid operands"
  gap surfaced while writing the flags test; pre-existing, not folded-constant related).