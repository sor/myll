# Variable Template Specialization

Status: **implemented 2026-10-04** (phase 1). Cases: `testing/cases/var_templates/`
(positive), `template_fail/` + `cross_module_clash_fail/` (negative).

## Syntax

```myll
const T pi<T>      = (T)3.1415;   // primary: template<class T> inline constexpr T pi = ...
const T pi<T=int>  = 3;           // full spec: template<> inline constexpr int pi<int> = 3;
const T q<T, U>    = (T)7;        // primary, two params
const T q<T, U=int> = (T)70;      // partial, full form: template<class T> ... q<T, int> = ...
const T q<U=int>   = (T)70;       // partial, subset form — same specialization
```

Decisions (settled 2026-10-04):

- **Named-only bindings**, in primary declaration order. `dict<K=string, V=int>` is rejected as
  out of order; positional bindings are not accepted. Partial specialization falls out of named
  bindings: a partial binds a subset of the primary's parameters; a full spec binds all. Both
  spellings of a partial are valid: the **full form** (the default/recommended) names *every*
  parameter, with unbound ones bare (`q<T, U=int>`); the **subset form** names only the bound
  ones (`q<U=int>`). Mixing bare names with skipping (`q<T, V=int>` for a 3-parameter primary)
  is rejected — bare names must cover all parameters, at each parameter's own position. An
  all-bare list (`q<T>`) parses as the primary declaration itself.
- **Phase 1: type parameters only.** Binding a non-type parameter (`N=5`) needs a grammar
  extension; the binding grammar is `id ASSIGN typespec`.
- **Specializations live in the primary's own module**, declared after the primary. Cross-module
  specializations are an ODR trap in C++ (a non-importing module would instantiate from the
  primary with a different value); the general form needs resolver visibility tracking plus
  forced includes.
- **Primary-first resolution**: uses are type-checked against the primary's signature (the
  specialization must repeat the primary's substituted type, mirroring C++'s same-declarator
  rule), and the resolver picks the matching specialization for emission. A full specialization
  wins over partials; among partials the most-bound wins; ties are ambiguous and error out.
- **Implicit `[ct]` for `const` variable templates** — a const variable template is a constexpr
  entity like `std::numbers::pi_v<T>`; emission is `inline constexpr`. `var` variable templates
  stay runtime entities.
- Emission matches std style: `inline constexpr` always.

## Mechanics

- Grammar: `tplSpec : LT tplSpecEntry (COMMA tplSpecEntry)* GT; tplSpecEntry : tplBind | id;`
  with `tplBind : id ASSIGN typespec;` on `idAccessor`. Bare entries (unbound placeholders) are
  stored as `TplBinding`s with a null type. ANTLR resolves the all-bare list to `tplParams`
  (the primary declaration), so no ambiguity arises.
- AST: `VarDecl.TplSpec` (`TplBinding { name, type }`); non-empty marks a specialization. The
  resolver fills `VarDecl.SpecInfo` (`Primary`, `Args`, `Bound`): Args lists every primary
  parameter in declaration order with symbolic placeholders for unbound positions, which is also
  the emitted argument list for partials; partials get their unbound parameters as their own
  `TplParams`.
- `VarSpecTransformer` (pre-resolution, after `TemplateParamTransformer`): validates primary
  existence/order/module, binding names and order, duplicate bound signatures, and the substituted
  type equality; substitutes bound values into the specialization's declared type syntactically
  (no resolved decls needed); injects unbound `TplParamDecl`s into the specialization's scope so
  initializers can reference them. Failed specializations are marked with a self-referencing
  `SpecInfo` so later passes treat them as consumed.
- Uses: `TryResolveVariableTemplate` prefers a matching specialization; `PickSingleCandidate`
  picks the primary among same-name declarations.
- Emission: `EmitVarSpecialization` in `HierachicalGen`.

## Deferred inventory

- Function/method specialization (C++: full only).
- Class/struct specialization (own phase: specializations may change member sets, needs
  specialization-aware member resolution).
- Explicit instantiation syntax for classes/functions (`template class Foo<int>;`).
- Extern-template declarations.
- Non-type parameter bindings (`N=5`).
- Use-site named template arguments.

See also `constinit`/`[ct] var` semantics in the `[ct]` series commits (2026-10-04).