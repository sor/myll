# Pattern Specialization — `<T=T*>` under `[spec=all]`

Status: **implemented 2026-10-16** (design settled with the author over the
same session; decisions recorded below). Follow-ups listed at the end.

## Semantics

Two orthogonal axes:

- **Kind** (the `[spec=…]` attribute): what gets specialized — `all` (fresh
  class body) vs `some` (member specialization block). `primary` optionally
  marks a primary template.
- **Shape** (derived from the bindings, optionally asserted): `full` = every
  position concretely bound; `partial` = any open (bare) or pattern position.

```myll
[spec=primary] class Dict<Key, Val> { ... }        // optional marker on any templated primary
[spec=all] class Dict<Key=int, Val> { ... }        // partial: concrete + open
[spec=all] class Dict<Key=int, Val=Val*> { ... }   // partial: concrete + pattern — the new form
[spec=(all, partial)] class Dict<Key, Val=Val*> { ... }  // shape assertion
[spec=(all, full)] class Dict<Key=int, Val=f32> { ... }  // shape assertion

func f<T>( ... );                                  // primary (bare)
[spec=full] func f<T=int>( ... )                   // optional marker on a function spec
const T sizeOf<T> = ...;                           // var primary (bare)
[spec=partial] const T sizeOf<T=T*> = ...;         // var pattern spec, optional shape marker
```

A pattern spec is a **class-kind** specialization (fresh body like `all`,
never a member block); it is never full — a pattern position is by definition
open and deduced per use. `class Pair<T=T*, U=T*>` covers every position and
is still partial.

## Value vocabulary (per kind)

| | `primary`/`prime` | `full`/`explicit` | `partial`/`part` | `all`/`class` | `some`/`member` |
|---|---|---|---|---|---|
| class primary | ok | error | error | error | error |
| class spec | error | list-only `(all, …)` | list-only | ok (required) | ok |
| func primary | ok | error | error | error | error |
| func spec | error | ok (optional) | error | error | error |
| var primary | ok | error | error | error | error |
| var spec | error | ok (optional) | ok (optional) | error | error |

- Aliases: `class`→`all`, `member`→`some`, `explicit`→`full`, `part`→`partial`,
  `prime`→`primary`. The value list rides the existing `(a,b)` attribute form;
  the `|` spelling (`[spec=all|partial]`) is deferred to the attr rework.
- Where `spec` is consumed (structural/func/var): unknown values (`[spec=banana]`)
  are rejected — typo protection. Where `spec` is not expected (namespaces,
  converts, ctors, …): the attribute stays ignored.
- Rejections carry advice naming the valid spellings for the kind.
- The `CLASS` keyword became a legal attribute value token (`idOrLit`); it is
  not rejected anywhere else — Myll is still being shaped.

## Pattern shape rules

- Binding type must be a `TypespecNested` with a single `id` naming a primary
  **type** parameter (own or another position's), no template arguments, and
  at least one pointer/reference decoration. Qualifiers on the pattern core
  are rejected for now.
- A param named by a pattern must not be concretely bound at its own position.
- Bare `T=T` stays rejected (matches everything = the primary; C++ rejects
  "not more specialized"), nested patterns (`T=Box<T>`), array patterns
  (`T=T[3]`), non-type patterns (`N=2*K`), and packs are rejected as deferred
  features.
- Functions keep full-only specs (C++ has no partial function specializations);
  pattern bindings on function specs stay rejected by the concrete-binding
  check.

## Mechanics

- `SpecData.Pattern[]` marks pattern positions; `Args[i]` carries the written
  pattern, `Bound[i]` stays false, and the pattern-named parameter joins the
  spec's own template parameters (injected into the scope like open positions,
  so the body sees the deduced parameter).
- Emission uses the existing partial-specialization machinery:
  `template<typename Val> class Dict<int, Val*> { … }` for classes, and the
  matching `template<…> … pi<T*> …` form for variable templates.
- Matching (`MatchesBoundArgs`): pattern positions unify instead of
  string-compare — decorations must match, the remainder binds the named
  parameter. Concrete positions still require equality.
- Ranking (`FindMatchingSpecialization`): full specs win (existing early
  return), then most-concrete-bound, then pattern specs; two matching pattern
  specs (or ties) are ambiguity errors. Partial ordering among patterns is
  deferred — erroring now keeps that door open.
- ODR: the specialization signature includes pattern-position Gens, so
  cross-module duplicate detection and invisible-use divergence work as for
  concrete specs. Cross-module pattern specs ride the existing
  `specsByPrimary` visibility index.

## Cases (no new case dirs)

- `class_templates/spec.myll` — pattern specs (pointer, reference, mixed
  concrete+pattern, own+other-position naming), full-beats-pattern precedence,
  the `isPtr` trait showcase, `primary` markers.
- `cross_module_struct_spec/` — cross-module pattern specs.
- `var_templates/` — var pattern specs + `partial`/`full` assertions.
- `template_fail/` — vocabulary rejections (unknown value, kind mismatches,
  `partial` on func, bare `full` on class, `primary` on specs) and pattern
  shape rejections.

## Deferred

Partial ordering among patterns (the same bidirectional unification CTAD
needs), nested/non-type/array/pack patterns, pattern specs in `.decl.myll`
prototypes, the `|` value spelling, pattern qualifiers.