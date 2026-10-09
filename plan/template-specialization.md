# Variable Template Specialization

Status: **implemented 2026-10-04** (phase 1: same-module variable specializations),
**2026-10-08** (phase 3: cross-module + ODR guard; phase 2: class/struct/union
specializations; phase 4: function specializations, same-module and cross-module;
phase 5: method specializations).
Cases: `testing/cases/var_templates/`, `testing/cases/class_templates/`,
`testing/cases/cross_module_var_spec/`, `testing/cases/cross_module_struct_spec/`
and `testing/cases/cross_module_func_spec/` (positive), `template_fail/`,
`cross_module_clash_fail/`, `cross_module_var_spec_fail/`,
`cross_module_struct_spec_fail/` and `cross_module_func_spec_fail/` (negative).

## Syntax

```myll
const T pi<T>      = (T)3.1415;   // primary: template<class T> inline constexpr T pi = ...
const T pi<T=int>  = 3;           // full spec: template<> inline constexpr int pi<int> = 3;
const T q<T, U>    = (T)7;        // primary, two params
const T q<T, U=int> = (T)70;      // partial, full form: template<class T> ... q<T, int> = ...
const T q<U=int>   = (T)70;       // partial, subset form — same specialization

func max<T>( T a, T b ) -> T { ... }             // primary
func max<T=int>( int a, int b ) -> int { ... }   // full spec: template<> int max<int>(...) { ... }
```

Function specializations are full-only (C++ has no partial function specializations);
class/struct/union specializations use the same forms as variables.

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
- **Cross-module specializations (phase 3)** live in a module that directly imports the
  primary's module (or in an implicitly imported std module), inside the primary's
  namespace. The specializing module's generated header includes the primary's header, so
  every translation unit sees the primary before the specialization. Transitive imports do
  not qualify: Myll name resolution only sees direct imports, and the specialization rule
  matches that. Same-module specializations must follow the primary by source position.
- **One Myll compilation is one program — the ODR guard is a hard error.** C++ only makes
  the diverging patterns ill-formed with no diagnostic required; Myll diagnoses them
  instead. The two rejected patterns:
  - the same bound signature for one primary, defined in two modules (duplicate
    specialization), and
  - a primary instantiation in a module that cannot see a matching specialization
    (that translation unit would instantiate the primary while others use the
    specialization).
  The cppreference basis:
  - inline specifier, Notes: "If an inline function or variable with external linkage is
    defined differently in different translation units, the program is ill-formed, no
    diagnostic required."
  - explicit (full) template specialization: "Specialization must be declared before the
    first use that would cause implicit instantiation, in every translation unit where
    such use occurs."
  A future `--program`-split option could relax the guard for separately linked programs;
  it is not implemented.
- **Class/struct/union specializations (phase 2)** follow the same binding rules and the
  same module rules as variable specializations, and their bodies are their own member
  sets (C++ first): bound parameter names are not in scope — the body spells out the
  bound values — while unbound parameters stay the specialization's own template
  parameters. A future attribute (`[interface]` / `[copy]`) could enforce interface parity
  or copy members from the primary; see the deferred inventory.
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
- `SpecTransformer` (pre-resolution, after `TemplateParamTransformer`): validates primary
  existence/order/module, binding names and order, duplicate bound signatures, and the substituted
  type equality; substitutes bound values into the specialization's declared type syntactically
  (no resolved decls needed); injects unbound `TplParamDecl`s into the specialization's scope so
  initializers can reference them. Failed specializations are marked with a self-referencing
  `SpecInfo` so later passes treat them as consumed. The transformer receives all compiled
  modules: when the specialization's own module has no primary, it searches the directly
  imported modules (plus the implicit std modules) along the specialization's namespace path.
  The same-module primary-first check compares source positions (file name, then line), so
  multi-file modules do not depend on file processing order.
- Uses: `TryResolveVariableTemplate` prefers a matching specialization; `PickSingleCandidate`
  picks the primary among same-name declarations. The resolver builds `specsByPrimary`
  (every valid specialization keyed by its primary, with its module) and the transitive
  `visibleByModule` import sets once after the pre-resolution transforms.
  `FindMatchingSpecialization` picks from that index, filtered to the specializations the
  use's module can see; prototype modules never see one. A use that falls through to the
  primary is recorded as an implicit instantiation.
- `ValidateVarSpecOdr` (after `ValidateModuleDecls`) rejects the two ODR patterns as errors,
  each with a note on the other definition. `ValidateModuleDecls` no longer classifies a
  specialization against its own primary or a sibling specialization of the same primary as
  a redefinition; the guard owns those pairs.
- Emission: `EmitVarSpecialization` in `HierachicalGen`. A runtime (non-const) module-scope
  specialization is an inline variable (`template<> inline int rt<int> = 5;`): C++ offers no
  declaration/definition split for specializations (re-defining one is an error), so inline
  is the only header-safe form. The primary's out-of-line definition no longer repeats its
  own template arguments (`obj.ReferenceName` instead of `FullyQualifiedName` in `AddVar`).

## Class/struct/union specializations (phase 2)

- Grammar: `defStruct` accepts `tplSpec` after the type name (`class Box<T=int> { ... }`),
  mirroring the variable rule. An all-bare list still parses as `tplParams`, so a primary
  declaration never parses as a specialization.
- AST: `Structural.TplSpec`; `VarDecl.VarSpecInfo` became the shared `Decl.SpecData` with
  a `SpecInfo` property on `Decl`.
- `SpecTransformer` validates structural specializations with the same binding rules
  (shape, order, duplicates) and injects unbound parameters into the class scope. A
  bodiless specialization is rejected by the earlier visit-stage forward-declaration check.
- Resolution: `PickSingleCandidate` picks the primary among same-name structurals, and
  `TrySwapStructSpec` (inside `ResolvePath`) swaps a concrete-argument use to its matching
  specialization, so member lookup, constructors, and generation all use the
  specialization's declarations; dependent argument lists keep the primary.
  `TemplateInference.ValidateTemplateArgs`/`GetTemplateParamCount` validate against the
  primary. The ODR guard covers structurals through the same `specsByPrimary` index; type
  and name resolution record primary uses for the divergence check (only the main
  resolution paths, which carry source positions).
- Emission: `template<> class Box<int> { ... };` for full specs,
  `template<typename T> class Box<T, int> { ... };` for partials, with no forward
  declaration. Members of a specialization are header entities (`Decl.IsTemplateUp`), and
  qualified names carry the specialization's arguments (`Decl.BuildQualifiedName`). Union
  members no longer get default member initializers (C++ rejects initializing multiple
  members of a union).

## Function specializations (phase 4)

- Grammar: `defCoreFunc` accepts `tplSpec` (`func max<T=int>( int a, int b ) -> int { ... }`).
- **Full only** — C++ allows no partial function specializations; any bare binding or
  missing count is rejected.
- **The signature must repeat the primary's substituted parameters and return type** —
  verified against g++ that a differing return type is rejected ("template-id ... does not
  match any template declaration"), so covariant-style returns are out: a specialization
  replaces the primary for those arguments, and a differing return would diverge across
  translation units just like the ODR traps.
- **Bound parameter names stay usable** in the signature and body: the transformer injects
  a hidden `IsBoundAlias` alias per bound parameter (`T -> int`) into the spec's function
  scope, instead of rewriting the AST. Name resolution resolves `T` through the alias
  (works for locals, casts, `new`, nested template arguments), inner declarations named
  `T` shadow it naturally, and emission expands such aliases to their target
  (`TypespecNested.GenType`) so no `T` reaches the generated C++. Writing the bound value
  (`int`) directly is equally valid; the signature check substitutes both sides.
- Resolution: a spec is a viable call candidate only when the explicit template arguments
  match its bound values, or when the primary's deduced arguments do; a viable spec
  shadows its primary in `TryResolveOverload`'s candidate list, an unmatched one steps
  aside. Calls that resolved to a spec are not re-resolved (the candidate list would
  shrink to the spec alone). Calls instantiating a primary are recorded for the ODR
  guard, deduplicated across re-resolution iterations.
- Emission: `template<>` prefix plus the decorated name (`template<> int max<int>(...)`)
  with the body as a header entity; the emitted call keeps plain deduction
  (`max( 21 )`), and C++ overload resolution picks the specialization.
- Same ODR guard as the other kinds through the shared `specsByPrimary` index.

## Method specializations (phase 5)

- **Syntax**: in-class, named bindings covering the class parameters first, then the
  method's own:
  ```myll
  class Store<T> {
      func Val( T t ) -> T => t;
      func Val<T=int>( T t ) -> T => t + 1;      // plain method: template<> int Store<int>::Val(int)
      func Load<U>( U u ) -> U => u;
      func Load<T=int>( U u ) -> U => u + 1;     // open member: template<> template<typename U> ... Store<int>::Load(U)
      func Load<T=int, U=int>( U u ) -> U => u + 3; // full: template<> template<> ... Store<int>::Load<int>(int)
  }
  ```
  Plain-class method templates work the same way with only member parameters
  (full-only); operators ride along (`operator +<T=int>(...)`).
- **Rules**: class parameters must be fully bound to concrete types (C++ requires
  concrete class arguments; symbolic bindings like `T=U` are rejected); member
  parameters may stay open, but only when the class is a template; specs inside a
  class specialization are rejected (specialize the class instead); method/class
  parameter name collisions are rejected. Signature equality, bound-parameter
  aliases, and duplicate signatures reuse the free-function machinery.
- **Emission**: out-of-line right after the class body closes — g++ and clang
  reject in-class member specializations (verified against g++-14, the container
  compiler line). The owner class name inside the specialization stands for the
  specialized instantiation (`OpBox` = `OpBox<int>`) through a bound alias, because
  the injected class name is not usable out-of-line (verified: g++ rejects it).
  Operators drop the name template arguments (deduced from the signature).
- **Resolution**: member lookup returns primary and specialization as candidates;
  `TryResolveOverload` combines the receiver's class arguments
  (`GetOwnerTemplateInfo`) with explicit or deduced member arguments before
  matching, and among matching specs the most-bound wins (C++ partial ordering).
  Calls whose class arguments are not yet known defer instead of erroring; calls
  that resolved to a spec are not re-resolved. Operator picks
  (`LookupOperatorOverload`) prefer the spec matching the receiver's class
  arguments.
- **Scope**: same-module only (specs live in the class body). Cross-module method
  specializations need a class-reopen or out-of-line syntax — deferred. The ODR
  guard is unchanged: same-module duplicates are caught by the transformer's
  per-primary signature check.

## Deferred inventory

- Specialization of member (field-scope) variable and class templates: the transformer's
  imported-primary search follows namespaces only; those stay same-module for now.
  Method (member function) specializations are implemented (phase 5, same-module);
  cross-module method specializations need a class-reopen or out-of-line syntax.
  Constructor templates and their specializations remain unimplemented (Myll has no
  general constructor template parameters yet).
- A future attribute to constrain class specializations: e.g. `[interface]` to enforce that a
  specialization exposes the primary's member set, or `[copy]` to copy the primary's members
  and only override the listed ones. The C++-first semantics (fresh member set, bound names
  not in scope) stay the default.
- Retrofitting bound-parameter aliases to variables (initializer expressions) and
  class/struct specializations (bodies), so all kinds allow writing `T` after binding.
- Runtime primary instantiation from an importing module: the primary's definition lives in
  its module's `.cpp` while importers only see the `extern` declaration, so importing
  translation units cannot implicitly instantiate it. Same-module runtime primaries work.
  (Const primaries are fully defined in the header and unaffected.)
- `--program`-split relaxation of the ODR guard.
- Explicit instantiation syntax for classes/functions (`template class Foo<int>;`).
- Extern declarations.
- Non-type parameter bindings (`N=5`).
- Use-site named template arguments.

See also `constinit`/`[ct] var` semantics in the `[ct]` series commits (2026-10-04).