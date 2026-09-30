# Enhancement: resolver performance — fixed-point and shadowing hot paths

Status: **open**. Found in the 2026-09-29 review. Behavior-preserving.

## Finding 1: every call is re-ranked on every fixed-point pass

`ResolveSingleCall` returns `false` unconditionally for Id/Scoped callees
(`backend/Resolver/Resolver.cs:941,955`), so once *any other* resolution makes progress,
`TryResolveOverload` re-runs conversion ranking for **every** call again. Argument types are cached
on `Expr.Type`, but `GetRank` per candidate per argument is re-executed. Member-access resolution
*does* report progress (`:989`), making the loop's progress accounting asymmetric.

Fix: cache per call "resolved callee + arg-type fingerprint"; skip re-ranking when both are
unchanged. Report progress when the cached result changes.

## Finding 2: `RangeForAutoTypeTransformer` is re-instantiated per pass per module

`backend/Resolver/Resolver.cs:139-145`. → Hoist the instance out of the loop.

## Finding 3: quadratic shadowing detection

For every local, `context.LocalDecls.Count(...)` scans all locals
(`backend/Resolver/TypeChecker.cs:1342-1344`), and `CheckOuterScopeShadowing` scans them again
(`:1364-1379`) — O(n²) per module, with `n` growing because `AutoReturnTransformer` and
`TemplateParamTransformer` inject one `LocalDecls` entry per function/template.

Fix: group `LocalDecls` into a `Dictionary<(Scope, string), List<Decl>>` once and answer both
queries from it.

## Verification

- `dotnet test testing/` green with unchanged goldens (behavior must be identical).
- Optional timing check: compile the full coreutil set (`bash
  frontend/coreutil/make_coreutil.sh`) before/after and compare the Myll-side "Time elapsed"
  number. No correctness gate beyond the goldens.

## Risk

Low — behavior-preserving, but the fixed-point loop is the heart of the resolver; the cache key must
be conservative (when in doubt, re-rank).

Effort: M.
