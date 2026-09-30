# Enhancement: one shared Typespec cloner

Status: **open**, partially addressed 2026-09-30. Found in the 2026-09-29 review. Field drift verified.

## Problem

Five near-identical Typespec clone helpers have drifted. **2026-09-30 update:** the
`literalText`/`literalValue` drift (plus the new `literalValues`) is fixed — all five now copy the
literal fields; three of five copy every `TypespecBasic` field. Remaining drops:

| Clone | Location | Still drops |
|---|---|---|
| public `CloneTypespecBase` | `backend/Resolver/ConversionRules.cs:681` | `align` |
| `CloneTypespecBase` | `backend/Resolver/TemplateInference.cs:455` | `srcPos`, `qual` (its Nested arm: `isInitList`, `isDependent`, `prefixDependent`) |
| `CloneTypespec` | `backend/Resolver/TypeChecker.cs:555` | Nested arm: `isDependent`, `prefixDependent`; `Pointer.Dialect` in the deep copy |
| `CloneTypespecBase` | `backend/Resolver/TypeResolver.cs:765` | Nested arm: `isDependent`, `prefixDependent` |
| `CloneTypespecBase` | `backend/Transform/DiscardTransformer.cs:482` | Nested arm: `isInitList`, `isDependent`, `prefixDependent` |

All five `TypespecNested` arms copy only `resolvedDecl` + `idTpls`.

Plus ad-hoc hand-written clones on top:

- `Resolver.cs` (`TryGetBaseHierarchical`) builds a `TypespecBasic` inline and drops
  `isDefaultSized`.
- `ConversionRules.cs` (`StripFirstPointer`) is the **only** clone that copies all nested
  flags — evidence the others drifted independently.

## Consequences (verified or traced)

- **Const deduction**: `TemplateInference.CloneTypespecBase` drops `qual`; `StripOutermostPointers`
  never restores it. `f(c)` with `c : const int*` and `func f<T>(T* p)` fails with "No matching
  overload" instead of deducing `T = const int` (repro: `tmp/review/const_deduce.myll`).
- **Dependent types**: a `T::Nested` stored after substitution loses its dependent flags, so
  `GenType` omits the `typename`/`template` disambiguators
  (path: `OperatorResolver.SubstituteMethodReturnType` → `TemplateInference.Substitute`).
- **Dialect pointer templates**: all pointer deep-copies drop `Pointer.Dialect` (set per module at
  `VTypes.cs:136`, `VExpr.cs:112`), so inferred return types and deduced arguments emit the default
  `std::unique_ptr<...>` form instead of the dialect's.
- ~~Folded constants through the discard transform~~ — **fixed 2026-09-30** (see
  `plan/untyped-constant-followups.md`).

## Fix plan

1. Add a single cloner on the AST: `Typespec Clone(bool deep)` (base + deep modes) in
   `backend/Core/Typespec.cs` — or one shared static if the AST should stay allocation-light.
   It must copy **every** field: base scalars, `qual`, `srcPos`, `align`, `literalText`,
   `literalValue`, all nested flags (`isInitList`, `isDependent`, `prefixDependent`,
   `isDefaultSized`), and deep copies of `ptrs` including `Pointer.Dialect`.
2. Delete all five helpers and the two ad-hoc clones; replace call sites with the shared cloner.
3. Re-verify the const-deduction repro and the `template_typename` cases (see
   `bug-template-return-substitution.md` for the return-type substitution fix that rides on this).

## Verification

- `tmp/review/const_deduce.myll` deduces `T = const int`.
- `testing/cases/template_typename/` still passes (dependent flags survive substitution).
- A folded constant (`1024 * 1024 * 1024`) survives a discard transform (extend
  `testing/cases/discard/` or `new_in_myll/compound_constants.myll`).
- Dialect pointer template: a custom-dialect case shows the dialect's `unique_ptr` form in inferred
  return types.
- `dotnet test testing/` green; review golden diffs (expect none if the cloner only *adds* copied
  fields that were previously lost on paths without observable output).

## Risk

Medium. Every type copy in the resolver goes through this. The golden suite is the gate: any diff
that is not an intended fix must be investigated.

Effort: M.
