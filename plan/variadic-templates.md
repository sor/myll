# Variadic Template Parameters

Status: **implemented 2026-10-16** (design agreed with the author before
implementation). Deferred pieces stay listed at the bottom.

## Notes from the implementation

- Overload resolution keeps exact-arity and pack-absorbing candidates side by
  side (an exact candidate may be rejected during conversion ranking while
  the pack absorber survives); the tie-break prefers the non-pack exact
  match, then non-templates.
- Deduced template arguments that still name template parameters (a pack
  forwarded into another pack) are not written onto the callee — the C++
  deduction redoes them, which is the canonical forwarding idiom; see
  `testing/cases/variadic/` (`forwardChain`).
- Conversion ranking only ranks the fixed prefix of a pack candidate; the
  pack elements are deduced exactly.
- Member access on a pack naturally fails as `Unresolved member`, which is
  the failure-case marker for that misuse.

## Motivation

- Std bindings: `std::format`, variadic `std::println`, `std::make_unique` —
  all take pack parameters. The proven raw-emission pattern (declare loose
  param types, emit the call raw, let C++ resolve — see `std::println` in
  coreutil) needs only declarations plus call-site arity flexibility.
- Myll-authored pass-through wrappers: `func log<T...>( T args ) { impl(
  (forward) args ); }`.

Deferred on purpose: fold expressions (a more explicit Myll syntax than
C++'s will be designed later), `sizeof...`, pattern expansions
(`vector<T...>`-style element-wise packs), non-type packs, specialization
over packs.

## Spelling (the twist)

`...` appears **only in the template parameter list**:

```myll
func format<T...>( const char[*] fmt, T args ) -> std::string;   // extern decl
func make_pair<T, U...>( T first, U second )                     // mixed lists

func log<T...>( T args ) { impl( (forward) args ); }             // Myll-authored
```

- `T args` where `T` is a pack-parameter → the parameter *is* the expansion;
  emission writes `T... args`.
- A pack-typed expression in argument position expands automatically:
  `impl( args )` emits `impl( args... )`.
- `(forward) args` on a pack-typed operand emits `std::forward<Args>(args)...`
  — the cast detects pack-ness of its operand's resolved type.
- Zero-argument pack expansions are allowed (a callable with no pack args).
- Explicit template arguments bind the pack from the trailing arguments:
  `format<int, f32>(...)`.

## Rules

- A pack parameter must be the **last** template parameter; a pack-typed
  function parameter must be the **last** parameter (validated with clean
  errors; C++ allows mid-list packs but the deduction/arity machinery stays
  simple this way).
- Pack-typed variables are usable only as call arguments or `(forward)`
  operands; anything else (arithmetic, member access, assignment, return,
  parameter of a non-variadic call...) is rejected with a clean error — no
  silent instantiations.
- A pack arg passed to a **non-variadic** callee is accepted and left to the
  C++ compiler to verify at instantiation (the arity is unknown to Myll).
- Overload preference: exact-arity matches beat pack absorption; among two
  pack absorbers the more fixed parameters win.

## Representation

- `TplParam.isPack` (parser sets it from the `...`), mirrored on the injected
  `TplParamDecl.isPack` so `TypespecNested.resolvedDecl` exposes pack-ness.
- Pack-ness of parameters and expressions is **derived** at resolution:
  a `TypespecNested` resolving to a pack `TplParamDecl` is a pack type.
  No `...` markers exist outside the parameter list.
- Deduction: a pack slot collects the trailing argument types into a
  `TplArg` pack (`packArgs`); emission prints them comma-separated.

## Exact changes

1. Grammar: `tplParam : id TRP_POINT? | typespec id` — one lexer-free token
   reuse (`...` already lexes as `TRP_POINT`).
2. `Mixed.cs`: `TplParam.isPack`; `TplArg.packArgs` for deduced/explicit
   packs; `Param.Gen()` emits `T... args` for pack-typed parameters.
3. `Decl.cs`: `TplParamDecl.isPack`.
4. `VTpl.cs`: parse the `...`.
5. `TemplateParamTransformer`: carry `isPack` into the injected decls.
6. `TemplateInference`: arity gates (`TryDeduceTemplateArgsAgainst`,
   `ValidateTemplateArgs`) treat the pack slot as absorbing trailing args;
   deduced packs collect lists.
7. `Resolver` overload loop: pack absorption on arity; preference rules.
8. `TypeChecker`: pack-misuse validation; pack-param-not-last rejection.
9. `HierachicalGen`: `typename... T` in template headers; `(forward)` cast
   pack detection emits `std::forward<Args>(args)...`; `Arg.Gen` expands
   pack-typed call args.
10. Std bindings: `std_format` gains `format<T...>`; `std_print` gains
    variadic `println`/`print` overloads (loose param spelling, raw emission).
11. Cases: `variadic/` (local pass-through + forward + zero-arg + mixed
    lists + explicit args), `std_variadic/` (format/println with multiple
    args, compile+run), `variadic_fail/` (misuse + non-trailing pack), plus
    `template_fail` markers for arity/shape rejections.