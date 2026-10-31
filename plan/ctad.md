# CTAD Phase 1 — Implicit Constructor Deduction

Status: **implemented 2026-10-16**. Decisions agreed with the author before
implementation.

## Semantics

```myll
class Box<T> { ctor( T v ); ctor( T a, T b ); ctor<T...>( T args ); }

var Box a( 5 );          // → Box<int>
var Box b( 1, 2 );       // ctor overload picks the two-argument one
var Box c( 7, 8.5 );     // variadic ctor pack deduction → Box<int, f32>
var auto d = Box( 3 );   // → Box<int>
var Box e = Box( 3 );    // declared bare + initializer deduces
func make() -> Box => Box( 7 );   // deduced value Box<int>
var Box* p = new Box( 5 );        // new-expression deduction → Box<int>
```

- Every constructor is an implicit deduction guide (C++ model): its own
  template parameters join the class's for the deduction, so
  `ctor<U>(U, T)` deduces both. Explicit template arguments
  (`var Box<f32> b(5)`) keep today's path — no deduction.
- Non-deducible class parameters are a hard error today. When template
  parameter defaults land (see below) they fill those, mirroring C++; until
  then Myll is strictly more conservative, and loosening later never flips
  an existing result.
- Ambiguous deduction (two viable ctors deducing different class arguments)
  resolves by **guide partial ordering** (`backend/Resolver/PartialOrdering.cs`,
  shipped 2026-11-01): the ctor parameter lists rank by C++ partial ordering
  (the more specialized guide wins, e.g. `ctor(T*)` over `ctor(T)` for a
  pointer argument, deducing `Box<int>`), and equally specialized or
  incomparable guides stay the hard error listing the variants — an
  approximation of [over.match.class] that never flips an existing result.
- Emission always writes the deduced arguments (`Box<int> a( 5 );`,
  `new Box<int>( 5 );`): Myll's resolution is authoritative, goldens
  document the inference, and C++ never re-deduces differently. (This is
  deliberately unlike `var auto`, which emits `auto` — auto deduction is
  one identical rule in both languages, CTAD is an overload-resolution
  process where Myll's ranking approximates C++.)

## Mechanics

- Call-site discovery: constructor calls never enter `TryResolveOverload`
  (the callee resolves type-first and `ResolveSingleCall` bails on
  non-Func callees). The CTAD pass hooks there: a callee that resolved to
  a class template with no explicit template arguments and call arguments
  triggers `TemplateInference.TryDeduceClassTemplateArgs` (IdExpr and
  ScopedExpr callees).
- Deduction reuses `TryDeduceTemplateArgs` with the combined parameter
  list (class parameters + the ctor's own), requiring full binding; the
  class arguments are the prefix. Variadic ctors ride the pack machinery.
- The deduced class arguments are written onto the callee (`ApplyTemplateArgs`),
  so the call's type materializes as `Box<int>` (the `ResolveCallReturnType`
  structural branch uses the callee's arguments) and the emission carries
  them.
- `var Box b( 5 )` / `var Box b = Box( 5 )`: the declared type is marked
  CTAD-pending at visit time (bare trailing segment + construction
  initializer). `ResolveTypes` skips the template-argument arity check for
  pending bare types; a fill pass (run with the call resolution, so the
  fixed-point loop iterates) copies the callee's deduced arguments into
  the declared type. A failed deduction is reported by the call pass.
- `new Box( 5 )`: new-expressions do not route through overload resolution
  either; the TypeChecker's NewExpr case runs the same deduction and fills
  the new-expression's type.

## Deferred

- User-written deduction guides (spelling settled later; `guide Vec<T>(
  T* first, T* last ) -> Vec<T>;` is the leading candidate).
- Aggregate CTAD, alias CTAD, return-type inference from ctor bodies.
- Partial explicit arguments (`Box<int>( 1, 2.5 )` deducing the rest).
- Deduction against `[spec=…]` specializations (primaries only today).

## Companion decisions (recorded 2026-10-16, implemented separately)

- **Template parameter defaults** take the bare `<T=int>` spelling on
  primaries (`class Box<T=int>` without an attribute = default; specs
  keep requiring `[spec=…]`, which disambiguates). The redefinition error
  a forgotten-attribute spec now produces gets a "did you mean [spec=…]?"
  hint. Non-type defaults use the same spelling (`class Fixed<int N=5>`).
- **Source annotations** (`--annotate`): post-resolution pass writing
  tagged comments back into the sources — inline where the deduction sits
  (`var Box/*<int>*/ i(9);`), trailing otherwise (`// @myll Box<int>`);
  strips prior `@myll` annotations before writing; off by default so
  goldens are unaffected. Annotates CTAD and `var auto` first; lambda
  returns and overload picks later.