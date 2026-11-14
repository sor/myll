# Dependent-Argument Call Resolution

Status: **implemented 2026-11-14** (shipped with the Myll standard library,
see `plan/myll-std.md`); this note records the semantics and the follow-ups.

## The problem

A call inside a template body can carry an argument whose type is still a
symbolic template parameter (`Map<K,V>` calling `hash(key)` with `key: K`).
Myll cannot rank such an argument against concrete parameter types before
the enclosing class is instantiated — but Myll also cannot just reject the
call, because the instantiation only exists at the C++ compiler.

## The mechanism

- **Ranking skips dependent positions.** In `Resolver.TryResolveOverload`,
  a call argument or parameter type that `IsDependentType()` marks as
  dependent contributes an Exact rank instead of a conversion rank. The
  position defers to C++, which re-checks the conversion at instantiation.
- **Hard rejections and ties defer instead of erroring.** When every
  candidate is rejected, or several tie, and any argument is dependent,
  the call stays unresolved. The emission writes the raw callee name, so
  C++ re-runs the whole overload resolution at instantiation — including
  argument-dependent lookup. This is what the `Map`/`Set` hash protocol
  rides on: user key types provide a `hash` overload next to the type,
  found by ADL, while the built-in overloads resolve through the
  definition context (`myll_map.hpp` includes `myll_hash.hpp`).
- Only calls with unresolved symbolic arguments defer; concrete-argument
  calls keep the full Myll-side diagnostics.

## Caveats

- Silent rejection inside template bodies: a genuinely broken dependent
  call surfaces as a C++ compiler error at instantiation, not as a Myll
  error. Accepted for now — the same trade-off C++ libraries make.
- A user key type that forgets its `hash` gets a linker error
  (`undefined reference to hash(MyType)`), not a Myll diagnostic.

## Follow-ups

- A deferred-calls listing: print every deferred call (name + module) when
  compiling with a flag, so the silent-rejection surface is visible.
- Reconsider a Myll-side trait: with concepts
  (`plan/upcoming-work.md`, "Concepts"), the hash protocol could become a
  checked `hash<K>` shape instead of a deferred free-function overload.
- The `as<T>()` chained-member deferral is now resolved by the template
  method return-type substitution (`TryGetMemberAccessBaseType` uses
  `SubstituteCallTemplateArgs`); if other chained shapes fail, extend that
  path rather than adding new deferral flags.