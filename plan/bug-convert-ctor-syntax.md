# Bug: `ctor convert T x` drops its parameter; `VisitDefConvert` text-parsing

Status: **fixed 2026-10-04**. Regression coverage: `testing/cases/convert/convert_decl.myll`
(all spellings) and `decl_and_attributes_fail/` (missing `[implicit]`/`[explicit]`).

## What was done (2026-10-04)

Grammar decisions (the `ctor`/`op convert` spellings stay):

1. The `defCtor` trailing-RARROW alternative (`CONVERT typespec id? RARROW?`, i.e. `ctor convert T
   ->`) is removed — the raw non-ctor rule has no trailing-arrow spelling either. The remaining
   `CONVERT LARROW? typespec id?` keeps the optional arrow, so a converting constructor has three
   spellings: `convert <- T`, `ctor convert <- T`, and `ctor convert T` (the `ctor` keyword makes
   the arrow redundant).
2. `defOp`'s arrow becomes mandatory (`CONVERT tplParams? RARROW typespec`): the operator rule
   otherwise contains normal operator declarations where the type list reads like a return type, so
   `op convert T` is direction-less noise; `operator convert -> T` states its direction (and
   `operator convert -> auto` covers a deduced direction).
3. `VisitDefCtor` handles the convert spelling via the shared `BuildConversionCtor` — the identical
   constructor for every spelling, also used by the raw `convert <-` arm (one param from the
   typespec + optional id, `isConversionCtor`). `op convert` and raw `convert ->` share
   `BuildConvertOperator`, which also fixes the missing `UnresolvedTypes` registration the defOp
   path previously lacked.
4. Direction detection in `VisitDefConvert` is token-based (`c.RARROW() != null`); the text-parsing
   and its dead branch are gone.
5. The `[implicit]`/`[explicit]` requirement applies to the new spelling for free (via
   `isConversionCtor` → `ValidateConvertAttributes`). The default-visibility question (implicit /
   explicit / neither by default) is deliberately left open: any future default should be a
   dialect decision, not a hardcoded rule.

## Verification

- `convert_decl.myll` covers every spelling with runtime checks: raw `convert ->`/`convert <-`,
  `ctor convert <- int`, arrow-less `ctor convert bool b`, the `conv` alias in both directions,
  `ctor conv int`, and `operator convert -> bool`. Goldens regenerated.
- `decl_and_attributes_fail/` gained a `ctor convert <- int` without `[implicit]`/`[explicit]`
  (FAIL marker on the existing requirement diagnostic).
- The trailing-RARROW spelling no longer parses (verified: extraneous `->` error).
- Legacy thesis-era files using `convert int ->` stop parsing; they are not part of the test suite.

## Risk

Low. Finding 1 adds a previously dead path; finding 2 is a behavior-preserving refactor.
