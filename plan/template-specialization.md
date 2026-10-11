# Variable Template Specialization

Status: **implemented 2026-10-04** (phase 1: same-module variable specializations),
**2026-10-08** (phase 3: cross-module + ODR guard; phase 2: class/struct/union
specializations; phase 4: function specializations, same-module and cross-module;
phase 5: method specializations; phase 6/6b: constructor templates and
specializations, plus declared specializations in prototype modules;
phase 6c: templated conversions in both directions).
Cases: `testing/cases/var_templates/`, `testing/cases/class_templates/`,
`testing/cases/cross_module_var_spec/`, `testing/cases/cross_module_struct_spec/`,
`testing/cases/cross_module_func_spec/` and `testing/cases/cross_module_decl_spec/`
(positive), `template_fail/`, `cross_module_clash_fail/`,
`cross_module_var_spec_fail/`, `cross_module_struct_spec_fail/`,
`cross_module_func_spec_fail/` and `cross_module_decl_spec_fail/` (negative).

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
  specializations need an out-of-line qualified syntax (see the deferred inventory).
  The ODR guard is unchanged: same-module duplicates are caught by the transformer's
  per-primary signature check.

## Constructor templates and specializations (phase 6)

- **Constructor templates** (`ctor<U>( U u, T t )`): their arguments are always
  deduced from the call arguments (C++ has no spelling for them at call sites;
  the class arguments come from the callee's template-id). `TryResolveOverload`
  gained a Structor deduction branch with enclosing class substitution;
  `TemplateInference.TryDeduceTemplateArgs` is generalized over `Func` and
  `Structor`. Emission adds the `template<...>` prefix in-class.
- **Constructor specializations** bind class parameters first, then the
  constructor's own. Scratch-verified against g++ and clang:
  - the deduction spelling is portable on both (`template<> template<>
    Store<int>::Store(char, int)` and `template<> Store<int>::Store(int)`),
  - template-ids on constructor names are compiler-divergent (clang rejects
    them), so constructor names never carry template arguments — fully bound
    specs use the deduction form,
  - plain-class specializations are rejected by both compilers → Myll rejects
    them ("constructor specializations need a class template").
- Emission is out-of-line after the class like method specializations, with the
  `inline` keyword (also fixed for method specializations: they are header
  entities, so every importing TU would define them otherwise) and bodies
  indented relative to the emission level. `explicit`/`[[nodiscard]]` are not
  repeated out-of-line (class-scope only). Bound-parameter aliases work for
  constructors too and no longer trip the local shadowing diagnostics.

## Declared specializations in prototype modules (phase 6b)

- A specialization declared in a `.decl.myll` says "the real header already
  specializes this": it feeds resolution (uses resolve against the specialized
  member set) and the cross-module duplicate guard (re-specializing from an
  authored module is rejected, because the generated definition would collide
  with the header's), and the module emits nothing.
- The `SpecTransformer` runs in prototype files; bodiless function/constructor
  specializations are accepted there since only the signature matters. The spec
  collector no longer skips prototype modules. Visibility flows through the
  existing sets: implicit std modules everywhere, explicitly imported ones
  otherwise.

## Templated conversions (phase 6c)

- Both directions take template parameters after the `convert` keyword:
  `convert<U> <- T`, `ctor convert<U> <- T` (converting constructors) and
  `convert<U> -> T` (conversion operators, matching the existing
  `op convert<U> -> T` form). A `template<typename U> Wrap(const U&)` maps to
  `convert<U> <- U`.
- Resolution follows the C++ rule for conversion function templates: parameters
  deduce from the source type (constructors) or the required target type
  (operators) via `TemplateInference.TryDeduceTemplateArgsAgainst`; their
  specializations are invisible to normal lookup (target-driven deduction always
  uses the primary — noted, nothing to enforce).
- Convert builders register their parameter/return types inside their own scope
  so types naming a template parameter resolve. Conversion operator emission
  uses `Gen` so pointer targets keep their pointers (`convert -> char*` is
  `operator char*()`).
- Copy/move constructors with a type parameter are rejected cleanly
  (C++ forbids copy/move constructor templates); the passing forms take no
  parameter lists, so anything else there is a parse error.
- Constructor templates compose inside class specializations (pinned in
  `class_templates`).

## Bound-name usability and non-type bindings (phase 6d)

- The 2026-10-09 consolidation folded the five validators' shared skeleton
  into one set of helpers (`FindPrimary`, `ValidateFollowOrder`,
  `SignatureMatchesParas`, `ClassParamsBound`, `InjectOwnerAlias`); constructor
  specializations gained the owner-name alias that method specializations
  already had.
- Variable and class/struct specializations keep bound parameter names usable
  through the same bound-parameter aliases as functions: `const T pi<T=int> =
  (T)3;` and `class Box<T=int> { func get() -> T ... }` both resolve T to the
  bound value, and emission expands it. Spelling the concrete value stays
  valid; both forms are equivalent under the substitution-based checks.
- Non-type parameters bind with literals (`class Arr<T=int, N=4>`, `const int
  x<N=8>`); a binding counts as bound with either a type or a literal, and
  ValidateBindingShape rejects kind mismatches (type parameter bound to a
  value, non-type parameter bound to a type). Non-type bindings do not create
  aliases: bodies spell the value like C++. Fully bound specializations emit
  the template<> form for non-type positions too.

## Member variable and nested class specializations (phase F)

- Member variable templates are implicitly `static` in the emission — C++ only
  allows static member templates, and both compilers reject the non-static form
  that Myll previously emitted (a pre-existing breakage with no test coverage).
  Qualified uses substitute the enclosing class's parameters with the qualified
  name's class arguments (`Box<int>::x<int>` yields `int`, not `T`).
- Member variable and nested class specializations follow the method pattern:
  bindings cover the enclosing class's parameters first (concrete values, since
  C++ requires concrete enclosing arguments), and at least one of the member's
  own parameters must be bound — an all-open member specialization does not
  specialize anything (both compilers reject it; clang also rejects the nested
  class form, g++ accepts it — a divergence, so Myll rejects).
- Emission is out-of-line after the enclosing class closes (in-class member
  specializations are rejected by both compilers): `template<> template<>
  inline constexpr int Outer<int>::x<int> = 7;` and `template<> template<>
  class Outer<int>::Inner<f32> { ... };`. `static` is not repeated out-of-line.
- The spec collector recurses into class scopes, so member specializations enter
  the `specsByPrimary` index (method and constructor specializations continue to
  ride the overload candidate lists). The ODR signature includes the
  class-parameter positions. Nested-class use matching prepends the enclosing
  class's arguments from the qualified name's earlier segment, mirroring method
  resolution. Nested classes inside nested classes (deeper than one level) are
  rejected for now — the emission would need argument lists for every
  enclosing level.

## Deferred inventory

- Use-site named template arguments (`Arr<T=int, N=3>`): deferred — Myll has
  no named non-template arguments either, and keeping the `<T=int>` spelling
  specialization-only avoids ambiguity for readers. Folded expressions at use
  sites are implemented (phase E).
- Non-type substitution inside declared types: a variable specialization whose
  declared type references a bound non-type parameter (array sizes) is not
  substituted yet; class specializations are unaffected (no declared type).
- Non-type bindings fold literal integer arithmetic only (`N=2*8` folds to 16);
  named constants or calls stay unfolded. Use sites fold the same way
  (`Arr<int, 2*8>` folds to 16 and matches the folded specialization). Known
  mini-wart: `<-5>` lexes as the left-arrow token; write `< -5>` or `0-5`.
- Cross-module member specializations: the imported-primary search still follows
  namespaces only. Same-module member variable and nested class specializations are
  implemented (phase F, see below).
- A future attribute to constrain class specializations: e.g. `[interface]` to enforce that a
  specialization exposes the primary's member set, or `[copy]` to copy the primary's members
  and only override the listed ones. The C++-first semantics (fresh member set, bound names
  not in scope) stay the default.
- Cross-module method/constructor specializations: C++ has no class reopening.
  The C++-native mechanism is out-of-line qualified declarations at namespace
  scope — exactly what the emission already produces. Leading syntax candidate:
  a `[specialize]` block (`[specialize] class Box<T=int> { func Load<U>(...) ... }`)
  that groups several members under one binding, so the qualified args are not
  repeated per member. TBD.
- Runtime primary instantiation from an importing module: the primary's definition lives in
  its module's `.cpp` while importers only see the `extern` declaration, so importing
  translation units cannot implicitly instantiate it. Same-module runtime primaries work.
  (Const primaries are fully defined in the header and unaffected.)
- `--program`-split relaxation of the ODR guard.
- Explicit instantiation syntax for classes/functions (`template class Foo<int>;`).
- Extern declarations.

See also `constinit`/`[ct] var` semantics in the `[ct]` series commits (2026-10-04).