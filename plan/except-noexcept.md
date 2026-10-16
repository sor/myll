# Exception Specification: `except` ↔ `noexcept` (the purity-axis treatment)

Status: **designed 2026-10-16; only the `throw` retirement implemented
(same day), the rest not started**. The open pieces wait for the attr
rework (see `plan/upcoming-work.md`).

## Background

The purity axis got the full treatment: `pure` (the Myll word) with `const`
(the C++ word) as accepted alias, the opposite pole `mutable`/`mut`, per-kind
defaults on the dialect (`DefaultAttributesFunction = "pure"`,
`DefaultAttributesProcedure = "mutable"`, …), and validation of nonsensical
combinations.

The exception axis never got that treatment:

- `[noexcept]` is a plain attribute today: `IsNoexcept` emits the C++ suffix,
  nothing validates it, and nothing defaults it. The std decl files sprinkle
  it per member (`std_array`, `std_filesystem`, `std_initializer_list`,
  `std_limits` …).
- `THROW` lexes (`attribId` accepts `throw`) but has no attribute semantics —
  a dangling token.
- The `numeric_limits` member specialization had to repeat `noexcept` by hand
  to satisfy g++ — a taste of what defaults + validation would own.

## Settled decisions

### Words

- `[noexcept]` (existing) and `[except]` (new antonym, the `mutable` to
  `pure`). Both parse as plain `id` attributes already — no grammar change.
- `throw` is removed from `attribId`: `[throw]` stops parsing as an attribute
  (a plain parse error is fine; no dedicated diagnostic — there is no
  preexisting code to migrate). `throw` stays the throw-statement keyword.
  **Done 2026-10-16.**

### Polarity defaults (the default dialect)

| Kind | Default | Note |
|---|---|---|
| `func` (incl. class `func` members) | noexcept | the `pure` twin |
| `proc` | except | the `mutable` twin |
| `method` | open | no claim; nothing emitted |
| `ctor` | open | do not assume all ctors throw — no-throw ctors are a thing |
| `dtor` | noexcept | only noexcept makes sense there; hardcoded |
| extern decls | never defaulted | the attrs communicate the real header |

An explicit spelling always wins over the default; the default injects only
when neither side is spelled.

### Explicit (strict) mode

- `proc`, `method`, and `ctor` must spell `[noexcept]` or `[except]`.
- `func` and `dtor` stay noexcept-defaulted (the kinds where noexcept makes
  most sense), so they never need spelling.
- Mechanism **undecided**: a global dialect knob (à la `switchFallthrough:
  Explicit`; candidate name `exceptionSpelling: Implicit | Explicit`) vs
  per-thing control via the attr value-forms. Decide together with the attr
  rework.

### Validation

- `[noexcept]` and `[except]` on one declaration → error (impossible
  combination).
- `[except]` on a dtor → error (only noexcept makes sense there).
- A `throw` statement inside a noexcept (explicit or defaulted) func/ctor →
  hard error. Local body walk only: no interprocedural checking, and calling
  possibly-throwing functions stays fine.

## Undecided — deferred to the attr rework

- **Value-forms**: `[except=require]` (must spell), `[except=yes]` ≡
  `[except]`, `[except=no]` ≡ `[noexcept]`, `[except=default]` (dialect
  default for the kind). They should arrive with the attr rework, not
  before it.
- **Class-level prefill**: `[noexcept] class … {…}` / `[except] class …`
  prefills all members. Open details: whether it overrides the per-kind
  defaults for every member kind (procs included) or fills only the open
  kinds; and a possible extension — `[except=possible]` /
  `[except=impossible]` — where an `impossible` class would forbid members
  from flipping to throwing at all. Too entangled with the value-forms, so
  it waits.
- **Strictness shape**: global dialect knob vs per-thing control (see
  Explicit mode above).

## Work impact when the time comes (not a design question)

- `func` noexcept-by-default adds ` noexcept` to nearly every generated
  signature: full golden regeneration sweep across `testing/golden/`.
- Sources that throw from funcs need `[except]` spelled
  (`synthetic_sugar/throw_if.myll`, `control_flow/try_catch.myll`);
  coreutil build check.
- New cases: `except_basics` (run), `except_fail` (combination, dtor,
  throw-inside-noexcept), plus whatever the strict mechanism turns out to
  be.

## Exact changes (draft)

1. `backend/Core/Dialect.cs`: `DefaultNoexceptFunction = true`,
   `DefaultNoexceptProcedure = false`, plus the strict mechanism once
   decided.
2. `backend/Core/Node.cs`: `IsExcept`.
3. `backend/Grammar/MyllParser.g4`: remove `THROW` from `attribId`.
4. `backend/Transform/DefaultAttributesTransformer.cs`: polarity injection
   per the table, dtor auto-noexcept, strict-mode errors.
5. `backend/Resolver/TypeChecker.cs`: the three validation rules.
6. Emission needs no new work — the existing ` noexcept` suffix is driven
   by the injected attribs.