# Upcoming Work (consolidated 2026-10-09)

Everything identified during the 2026-10-08/09 sessions that is not yet
implemented. Ordered roughly by "ready to execute" then by size. The template
specialization series through phase F is complete (see `plan/template-specialization.md`).

## Immediate

- **Member variable template `[static]` requirement + const semantics unification**
  — ready to execute, full detail in `plan/member-var-static.md`.
  Require `[static]` on member var templates (primary and spec), reject missing,
  remove the implicit-static emission, unify `const T x<U>` and
  `field const T x<U>` to implicit `[ct]` → constexpr, keep `[static,ct] var`
  → constinit.

## Template specialization follow-ups

1. ~~Cross-module member specializations~~ — **done 2026-10-16** as `[spec=some]`
   member specialization blocks with the `[spec=…]` specialization kind
   attribute (`plan/member-spec-block.md`); includes `[spec=all]` now being
   required on fresh class specs, `std::formatter` and `std::numeric_limits`
   real-world cases, and the static-member/return-substitution fixes that
   fell out. Remaining follow-ups from the same doc: deep-nested heads and
   plain (non-template) enclosing classes.
2. ~~Variadic template parameters~~ — **done 2026-10-16**
   (`plan/variadic-templates.md`): `<T...>` packs with derived expansion in
   parameters, call arguments and `(forward)`, plus `std::format` /
   variadic `std::println` bindings. Deferred from there: explicit fold
   syntax (deliberately a Myll-native design, not C++ `(...)`),
   `sizeof...`, pattern expansions, non-type packs, specializing variadics.
3. **Deep nesting**: specializations of nested classes inside nested classes
   (rejected today with a clean error). Needs argument lists for every
   enclosing level in emission.
   ~~Pattern specializations~~ — **done 2026-10-16**
   (`plan/pattern-specialization.md`): `<T=T*>` bindings under `[spec=all]`
   with unification matching, the two-axis `[spec=…]` vocabulary
   (kind/shape/primary with aliases), func/var consumption of the attribute,
   var-template pattern specs, and cross-module support. Deferred: partial
   ordering among patterns (CTAD's unification engine), nested/array/
   qualified/non-type/pack patterns, pattern specs in `.decl.myll`
   prototypes, the `|` value spelling (attr rework), cross-position
   deduction consistency.
4. **Non-type substitution inside declared types**: a variable specialization
   whose declared type references a bound non-type parameter (array sizes).
   Class specializations are unaffected (no declared type).
5. **Runtime primary instantiation from importing modules**: the runtime var
   template primary's definition lives in its module's `.cpp` while importers
   only see the `extern` declaration. Same-module works.
6. **`--program`-split** relaxation of the ODR guard (compile several
   separately linked programs from overlapping modules).
7. **Explicit instantiation syntax** (`template class Foo<int>;`).
8. **Extern-template declarations**.
9. **Use-site named template arguments** (`Arr<T=int, N=3>`) — deferred
   deliberately: Myll has no named non-template arguments either, and keeping
   `<T=int>` specialization-only avoids ambiguity for readers. Folded
   expressions at use sites are done (phase E).
10. ~~Partial ordering among same-tier specs and CTAD guides~~ — **done
   2026-11-01**: `backend/Resolver/PartialOrdering.cs` implements C++
   partial ordering (bidirectional deduction against synthesized unique
   types). Same-tier spec ties resolve in `FindMatchingSpecialization` and
   the call-site spec tier of `TryResolveOverload` (a pattern beats a plain
   unbound position; equally specialized or incomparable ties stay the
   ambiguity error, as C++ rejects those too), and CTAD ctors deducing
   different class args pick the most specialized guide (`ctor(T*)` over
   `ctor(T)` for pointers) instead of erroring. Guards keep member-spec
   positions, non-type params, and packs on the old ambiguity.

## Exception specification axis (`except` ↔ `noexcept`)

- **Rethrow exceptions like the purity axis** — design settled 2026-10-16,
  implementation not started (`plan/except-noexcept.md`). `except` is the
  antonym of `noexcept` (both already parse as plain id attributes),
  `throw` retires as an attribute, funcs default noexcept (pure's twin),
  procs default except (mutable's twin), methods and ctors stay open,
  dtors are implicitly noexcept, extern decls never default, plus
  combination validation and a local throw-inside-noexcept check. Deferred
  to the attr rework: the `[except=require/yes/no/default]` value-forms,
  class-level prefill, and whether strictness is a global dialect knob or
  per-thing controlled.

## Attribute rework

- **Parameterized attribute values** — `[except=require]` (must spell),
  `[except=yes]` / `[except=no]` (polarity), `[except=default]` (dialect
  default). The exception axis is the first customer; its deferred pieces
  (see `plan/except-noexcept.md`) build on this. While at it, sweep the
  attribute vocabulary for dangling tokens and decide the general
  spelling/combination rules once.

## CTAD (class template argument deduction)

- ~~Phase 1: implicit constructor deduction~~ — **done 2026-10-16**
  (`plan/ctad.md`): `var Box b( 5 );`, ctor-call expressions
  (`var auto b = Box( 3 );`, `var Box b = Box( 3 );`, return positions) and
  `new Box*!( 5 )` deduce the class arguments from the constructor
  arguments; every constructor is an implicit guide; ctor template
  parameters join the deduction; emission writes the deduced arguments
  explicitly. Ambiguous and failed deductions are hard errors (partial
  ordering — the same bidirectional unification pattern specs need — is
  deferred and erroring now keeps that door open). Deferred: user-written
  guides (`guide Vec<T>( T* first, T* last ) -> Vec<T>;` leading
  candidate), aggregates, alias CTAD, return-type inference, deduction
  against specializations.
- **Template parameter defaults** — take the bare `<T=int>` spelling on
  primaries (`class Box<T=int>` without an attribute = default; specs keep
  requiring `[spec=…]`, which disambiguates). The redefinition error a
  forgotten-attribute spec now produces gets a "did you mean `[spec=…]`?"
  hint. Non-type defaults use the same spelling (`class Fixed<int N=5>`).
  When defaults land, CTAD fills non-deducible parameters from them
  (mirroring C++); until then CTAD rejects them.
- **Source annotations** (`--annotate`) — post-resolution pass writing
  tagged comments back into the sources: inline where the deduction sits
  (`var Box/*<int>*/ i(9);`), trailing otherwise (`// @myll Box<int>`).
  Strips prior `@myll` annotations before writing (idempotent, hand edits
  do not go stale); off by default so goldens are unaffected. Annotates
  CTAD and `var auto` first; lambda returns and overload picks later.

- **Phase 1: implicit constructor deduction only.** `var Box b( 5 );` deduces
  `Box<int>` from constructor arguments. No user-written deduction guides:
  cppreference confirms implicit guides cover the common case; explicit guides
  only override synthesis (std iterator-pair style).
- Mechanism: constructor argument deduction already exists
  (`TryResolveOverload` ctor branch with owner args + deduction). The var decl
  path needs the class template args to be deduced from the ctor args when the
  var's type carries none.
- Deferred: aggregate deduction (C++20), alias deduction (C++20), explicit
  guides.

## Class specialization constraints attribute

- `[interface]`: enforce that a class specialization exposes the primary's
  member set. `[copy]`: copy the primary's members and only override the
  listed ones. Fresh member set (C++ first) stays the default. No compiler
  counterpart exists (Myll-side sugar), so this is pure Myll semantics.

## Known mini-warts (documented, do not chase)

- `<-5>` at template-arg positions lexes as left-arrow; write `< -5>` or
  `0-5`. C++ has an analogous wart (`<` closing templates); noted in
  `plan/template-specialization.md`.
- Grammar TODO comments for `defAlias` multiple support, `requires` clauses
  shape.

## Housekeeping

- **CI/CD**: suite green at 158 cases; container image builds; the CI wiring
   itself remains (AGENTS Planned Work item 3).
- **oldParser/oldCodeGen removal** (salvage notes: `plan/old-parser-salvage.md`,
  `plan/old-codegen-salvage.md`).
- **TODO triage**: ~59 TODO/FIXME/HACK comments in backend; ~15
  NotImplementedException/NotSupportedException throws reachable from
  specification-covered paths should move to real diagnostics.
- **Not yet done, lower priority** (carried over from earlier sessions):
  named function arguments, null-coalescing call operator, copy-cast syntax.

## Working conventions (recent sessions)

- **No generated C++ that compilers reject.** Verify semantics against g++ and
  clang (the container runs g++-14; the local machine has g++-15 and clang-20;
  divergent verdicts mean Myll rejects the form). Scratch files in `tmp/` is
  fine; verify bindings by writing a minimal `.cpp`.
- **Robin Hood deletions are reported.** Regular line churn from the task at
  hand is not.
- **Failing-case stage rule**: all markers in one case must be visible in the
   same compile run — visitor-stage diagnostics (`visit_fail`) abort the
   compile before resolver passes and would suppress resolution-stage markers
   (`template_fail`); parse-stage errors (`spec_attr_fail`) abort before the
   visitor. Same-stage cases are consolidated into one per stage+topic (e.g.
   `cross_module_spec_fail` carries the var/struct/func/decl guards);
   a second batch of stage-conflicting cases stays separate when merging
   would hide markers (lambda capture vs method).
- **Commits**: impl → tests → docs, small and focused; never push.
- **Usable-scratch test syntax note**: member var templates at field scope now
  REQUIRE `[static]` once `plan/member-var-static.md` is executed.