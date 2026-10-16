# Exception Specification: `except` ↔ `noexcept` (the purity-axis treatment)

Status: **planned 2026-10-16, needs design decisions** (task list entry requested by the
author; not started).

## Background

The purity axis got the full treatment: `pure` (the Myll word) with `const`
(the C++ word) as accepted alias, the opposite pole `mutable`/`mut`, and
per-kind defaults on the dialect (`DefaultAttributesFunction = "pure"`,
`DefaultAttributeProcedure = "mutable"`, method/convert/operator categories,
`DefaultAttribute*` for class/struct/union/enum). Validation rejects
nonsensical combinations instead of silently accepting attribute soup.

The exception axis never got that treatment:

- `[noexcept]` is a plain attribute today: `IsNoexcept` emits the C++ suffix,
  nothing validates it, and nothing defaults it. The std decl files sprinkle
  it per member (`std_array`, `std_filesystem`, `std_initializer_list`,
  `std_limits` …).
- `THROW` lexes (`attribId` accepts `throw`) but no attribute semantics,
  validators or dialect defaults exist for it — a dangling token.
- The `numeric_limits` member specialization needed `noexcept` repeated by
  hand to satisfy g++ — a taste of what a defaults + validation pass would
  own for free.

## The sketch (analogous to const/pure/mutable)

| purity axis (done) | exception axis (this task) |
|---|---|
| `pure` / `const` | `noexcept` |
| `mutable` / `mut` | `except` (spelling TBD, see Q1) |
| `Dialect.DefaultAttributes*` | `Dialect.DefaultExcept*` (or one field per kind) |
| validation of combos | validation of combos |

## Open questions

1. **Spelling of the throwing pole.** `except` reads as the natural antonym
   and keeps `throw` free for the throw-statement. Alternatives: `throws`,
   `may_throw`. Note C++17 removed dynamic exception specifications, so
   `except(x)` lists are out of scope by construction.
2. **Defaults.** Options:
   - (a) `func` = noexcept by default (pairs with "func is pure" — the
     functional core is const and cannot throw), `proc` = except, method =
     except. Breaking change: every signature gains or keeps `noexcept`
     emission; std decl files slim down.
   - (b) Keep C++'s may-throw default; `except` then only documents intent
     and would matter only in dialects that flip the default.
   - (c) Purely dialect-driven: same mechanism as the purity defaults, ship
     (b) as the default dialect, allow (a) per dialect.
   - Recommendation shape: (c) — it is the least disruptive path to (a).
3. **Conditional `noexcept(expr)`.** Needs constant-evaluation of
   expressions; defer to a later phase (same bucket as
   `noexcept(...)`-typed attributes).
4. **Semantic validation depth.** Phase 1: signature-level only (defaults +
   validation of combinations, emission). Phase 2 (optional): body analysis
   so a `noexcept` function only calls `noexcept` callees. Keep phase 2 out
   of the first cut; the C++ compiler is the gate it already is.
5. **Derived defaults.** Destructors are implicitly noexcept in C++; Myll
   `dtor` declarations should not need the attribute spelled. Whether ctors
   of noexcept bases infer anything: no, C++ says nothing — keep it.
6. **`[throw]` token.** Retire it or make it the deprecated alias for
   `except` (the `const`-for-`pure` pattern suggests the latter).
7. **Interaction with the purity pair.** Two independent effect axes
   (`[except, pure]`, `[noexcept, pure]`, …) — both validated, neither
   implies the other. `proc` default stays `mutable` + `except` under
   option (a)/(c)-(a).

## Exact changes (draft, phase 1)

1. `backend/Core/Node.cs`: `IsNoexcept` stays; add `IsExcept` from
   `except` (+ `throw` as alias per Q6).
2. `backend/Core/Dialect.cs`: `DefaultExceptFunction` / `DefaultExceptProcedure`
   / `DefaultExceptMethod` (+ convert/operator categories), applied by the
   existing per-kind default merge pass.
3. `backend/Resolver/TypeChecker.cs`: reject `except`+`noexcept` combined
   (same shape as the `[inline]`/`[hide]` mutual exclusion).
4. `backend/Generator/HierachicalGen.cs`: emission is already ` noexcept`
   on the attribute; the flip adds nothing (`except` emits nothing).
5. `std/*.decl.myll`: thin out the `[noexcept]` sprinkles once the default
   lands; `std_limits` keeps the matching case green either way.
6. Cases: a `std_limits_member_spec` follow-up already exercises the
   signature-matching side; add `template_fail` markers for the rejected
   combinations and a default-behavior case.