# Enhancement: one shared signature-equality helper

Status: **open**. Found in the 2026-09-29 review.

## Problem

Three implementations of "do these two functions have the same signature":

1. `HasSameSignature` — `backend/Generator/HierachicalGen.cs:1294-1305`. Structural, via
   `ConversionRules.IsExactMatch`, **parameters only**.
2. `FunctionSignaturesEqual` — `backend/Resolver/Resolver.cs:702-716`. Compares `type.Gen()`
   **strings**, parameters **plus return type**.
3. `StructorSignaturesEqual` — `backend/Resolver/Resolver.cs:723-733`. Compares `type.Gen()`
   strings, parameters only.

The string-based variants depend on dialect pointer templates and on the generator's formatting:
distinct types that format identically compare equal, and the semantics silently diverge from the
generator's structural check. Concrete consequence: two cross-module functions with identical
parameters but different return types are not treated as a redefinition
(`bug-cross-module-decl-validation.md`, finding 2).

## Fix plan

1. Add one shared helper in `backend/Resolver/ConversionRules.cs` (structural,
   `IsExactMatch`-based) with two variants:
   - `SameParams( Func a, Func b )` / the `Structor` equivalent,
   - `SameParamsAndReturn( Func a, Func b )`.
2. Rewrite `FunctionSignaturesEqual`, `StructorSignaturesEqual`, and `HasSameSignature` on top of it.
3. Keep the template-parameter-count check (it is not a type comparison and stays as-is).

## Verification

- `bug-cross-module-decl-validation.md` scenarios: same params + different return → diagnostic;
  class + function same name → accepted.
- Existing `testing/cases/cross_module_clash_fail/`, `cross_module_decldef/`,
  `cross_module_overload/` unchanged.
- `dotnet test testing/` green.

## Risk

Low-medium. Pure comparison logic; the existing cross-module cases are the safety net.

Effort: M (S if the cloner work from `enhancement-typespec-cloner.md` lands first, since
`IsExactMatch` then runs on fully-faithful copies).
