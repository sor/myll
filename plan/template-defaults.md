# Template Parameter Defaults — bare `<T=int>`

Status: **implemented 2026-10-16**. Companion vocabulary change: every
specialization now requires `[spec=...]`.

## Semantics

```myll
class Box<T=int> { ... }          // primary with default (the [spec=primary] marker is optional)
class Fixed<T, int N=5> { ... }   // non-type default, declared type carried in the spelling
const T answer<T=int> = 41;       // var primary with default
alias Vec<T=int> = std::vector<T>; // alias primary with default

[spec=all] class Box<T=int> { ... }   // struct spec (kind required)
[spec] func f<T=int>( int a ) { ... } // func/ctor spec: bare [spec] suffices (always full)
[spec=full] const T pi<T=int> = 5;    // var spec: bare [spec] or a shape assertion
```

The disambiguator is the `[spec=...]` attribute, everywhere:

| Decl | Existing primary? | Bare bindings mean |
|---|---|---|
| any kind | no | a new primary with defaults |
| any kind | yes | error: "already declared … did you mean a `[spec=…]` specialization?" |
| struct marked `[spec=all/some]` | — | the spec (kinds are not derivable) |
| func/ctor marked `[spec]` | — | the spec (always full — funcs/ctors have no partials) |
| var marked `[spec]` | — | the spec (shape derived; `[spec=full]`/`[spec=partial]` are assertions checked against the bindings) |

Alias specs stay rejected (C++ has no spelling); bare alias bindings declare
the alias primary with defaults. Ctor bare bindings declare ctor templates
with own-parameter defaults (the class-parameter space is only reachable
through the `[spec]` spelling, which keeps the class-params-first rule).

## Rules

- Defaults must be **trailing**: once a parameter is defaulted, every
  following parameter needs a default (C++ rule; clean error otherwise).
- A non-type default requires its declared type in the spelling: `<int N=5>`.
  `<N=5>` alone errors (the type is not derivable).
- Use sites may omit trailing defaulted parameters: the fill substitutes
  the provided arguments into the default (`vector<T, Alloc=std::allocator<T>>`
  with `vector<f32>` fills `std::allocator<f32>`). Bare type uses and bare
  variable uses fill when every parameter is defaulted.
- Deduction and CTAD fill unbound defaulted parameters after binding (C++
  semantics: deduction ignores defaults; unbound defaulted params take them).
- Emission: the definition carries the defaults
  (`template<typename T = int> class Box`); forward declarations spell bare
  names — C++ rejects redefining a default across declarations.

## Mechanics

- Grammar: one new `tplBind` alternative for the non-type spelling
  (`typespec id ASSIGN expr`); type defaults already parsed. `TplBinding`
  carries the declared type; `TplParam` carries a `defaultArg` (a `TplArg`).
- `SpecTransformer.ConvertBindingsToParams` converts bare bindings into
  primary parameters with defaults (structs, funcs, methods, ctors, vars,
  aliases); `ConvertBareBindings` fires the already-declared+hint error when
  a same-name primary is visible (local or imported). The `[spec=…]`
  requirement moved to every spec validator; the scan consumes the
  attribute for ctors too.
- `TemplateInference.FillOmittedDefaultArgs` builds the full argument list
  with substitution; `ValidateTemplateArgs` allows omitting trailing
  defaulted positions; `TryDeduceTemplateArgs` fills unbound defaulted
  parameters after binding (covers CTAD via the combined deduction).
- `GenTemplateParameter` emits defaults; `GenTemplateParameterListBare`
  for forward declarations.

## Cases (no new dirs)

- `class_templates/spec.myll` — defaulted primary `Dflt<U=int>` with bare
  use, non-type defaulted primary `FixedBox<T, int N=3>`, plus the
  existing pattern/spec coverage.
- `var_templates/` — defaulted var primary `answer2<T=int>` with bare use.
- `ctad/` — CTAD + defaults (`FixedPair<T, U=int>`, ctor deduces T, U
  filled).
- `template_fail/` — redefinition+hint markers (class/func), trailing-rule
  error, spec-attribute markers updated to the new messages.

## Deferred

Function-template defaults as *specs* remain full-only (defaults on func
primaries are the bare spelling now); `Box<>` empty-args spelling (grammar
ambiguity with empty `tplSpec`); default accumulation across declarations
(C++17 pre-prime); defaults after packs (packs are last and take none).