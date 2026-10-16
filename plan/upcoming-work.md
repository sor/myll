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
2. **Deep nesting**: specializations of nested classes inside nested classes
   (rejected today with a clean error). Needs argument lists for every
   enclosing level in emission.
3. **Non-type substitution inside declared types**: a variable specialization
   whose declared type references a bound non-type parameter (array sizes).
   Class specializations are unaffected (no declared type).
4. **Runtime primary instantiation from importing modules**: the runtime var
   template primary's definition lives in its module's `.cpp` while importers
   only see the `extern` declaration. Same-module works.
5. **`--program`-split** relaxation of the ODR guard (compile several
   separately linked programs from overlapping modules).
6. **Explicit instantiation syntax** (`template class Foo<int>;`).
7. **Extern-template declarations**.
8. **Use-site named template arguments** (`Arr<T=int, N=3>`) — deferred
   deliberately: Myll has no named non-template arguments either, and keeping
   `<T=int>` specialization-only avoids ambiguity for readers. Folded
   expressions at use sites are done (phase E).

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

- **CI/CD**: suite green at 157 cases; container image builds; the CI wiring
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
- **Failing-case stage rule**: all markers in one case must fire at the same
  stage (compile-stage diagnostics get their own case, e.g. `ctor_passing_fail`);
  the harness bails at the first error stage.
- **Commits**: impl → tests → docs, small and focused; never push.
- **Usable-scratch test syntax note**: member var templates at field scope now
  REQUIRE `[static]` once `plan/member-var-static.md` is executed.