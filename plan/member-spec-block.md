# [spec=…] Specialization Kind Attribute + Member Specialization Blocks

Status: **implemented 2026-10-16** (agreed with the author same day). Follows
phase F (`plan/template-specialization.md`). Companion pre-fix: symbolic
class-specialization bindings (`class S<T=T*>`) are now rejected like every
other spec kind (they previously validated silently and emitted ill-formed
C++).

## Background

Class specializations had one form: a fresh body (`class Box<T=int> { … }`).
C++ has a second mechanism: out-of-line member specializations of an implicit
instantiation (`template<> int Box<int>::Load() { … }`), which replace only
the named members. Myll had no spelling for it, and C++ has no class
reopening to fake one with member specs in an authored class.

Both forms now carry an explicit `[spec=…]` attribute. The attribute content
is ordinary identifiers, so the grammar needed no change — the head is an
ordinary attributed `defStruct` with a `tplSpec`.

## Semantics

```myll
// a.myll — the primary
class Box<T> { [pub]: ctor(T v) {…} field T _v; func Get() -> T => _v;
	func Find<U>( U a ) -> U => a; [static] field int count;
	class Inner<V> { [pub]: field V v; func W() -> V => v; } }

// b.myll — imports a
[spec=some] class Box<T=int> {          // head: concrete instantiation Box<int>
[pub]:
	func Get() -> int => _v * 2;        // template<> inline int Box<int>::Get() const
	func Find<U=f32>( f32 a ) -> f32 => a;  // template<> template<> … Find<float>
	ctor( int v ) { _v = v * 10; }      // template<> inline Box<int>::Box(int)
	[static] field int count = 7;       // template<> inline int Box<int>::count = 7
	[spec=all] class Inner<V=int> { … } // template<> class Box<int>::Inner<int>
}
```

Rules (validated in `SpecTransformer`, stage: transform, before resolution):

- `[spec=all]` — the existing fresh-body form. **Required**: a bare
  `class Box<T=int>` is an error now. `[spec=some]` is the member block.
- `[spec=…]` unknown value on a class/struct/union is an error;
  `[spec=all, spec=some]` is a hard parse error (both name opposite kinds);
  `[spec=…]` on a non-template class is an error. Elsewhere (funcs, vars,
  namespaces) the attribute is ignored — no overzealous rejections.
- Head bindings name every class parameter with concrete values (C++
  requires concrete enclosing arguments; g++ rejects
  "…is a member template, so its enclosing A must be specialized also").
- Members bind only their **own** template parameters; class parameters are
  the head's. Plain members (no bindings) specialize the concrete
  instantiation's member directly. Member templates must bind at least one
  own parameter (an all-open block member specializes nothing).
- Every member must match a primary member by name, kind, and substituted
  signature (paras plus return); the block cannot add members. Fields must be
  `[static]`; copy/move ctor specializations are rejected (their parameters
  are generated per instantiation).
- The head must follow the primary by source position (same module) and the
  block module must import the primary's module directly (same visibility
  rule as every other spec kind).
- Mixing: a `[spec=all]` and a `[spec=some]` of the same primary and class
  arguments may not coexist visibly — the class would be both a full
  specialization and an implicit instantiation. Reported in both directions
  while scanning own plus directly imported modules.

## Representation

The head keeps `SpecInfo` with `MemberSpecHead = true`, the head bindings as
`Args`, and — for the emission split — the primary's parameter list as its
own `TplParams`. The head itself is **transparent**:

- Name lookup skips it (`Resolver.AddVisibleChildren`), so uses of `Box`
  resolve the primary; uses never swap into a half-populated class.
- Module exports skip it (`CollectExports`), so importer modules see the
  primary too.
- The spec index skips it (`CollectSpecs` — the recursion still collects the
  block's *member* specs under their member primaries, which is what the ODR
  duplicate guard consumes).
- The generator emits no class for it (`AddHierarchical`); it only flushes
  its members' out-of-line specs.

Each member gets ordinary in-class member-spec `SpecInfo` (primary = the
matched member, args = head class args + own member args), so the existing
emitters (`EmitMethodSpec`, `EmitStructSpec`, `EmitMemberVarSpec`,
`EmitNestedStructSpec`) work unchanged. Unqualified names inside block bodies
fall back to the primary's members through the head scope's `importedScopes`
(the same mechanism as base-class lookup), and head bindings become
bound-parameter aliases (`T` means the bound value).

## Fixes that fell out

- `EmitMemberVarSpec` emitted a second `template<>` for plain static field
  specializations (only member templates carry it).
- Static member functions emitted a stray `const` — C++ rejects
  const-qualified static member functions (`Node.IsConstMethod` now excludes
  static).
- Qualified member calls now substitute the enclosing class's template
  parameter in return types (`TypeResolver.SubstituteCallTemplateArgs` +
  `Resolver.TryGetMemberAccessBaseType`), so `Box<int>::Get()` yields `int`,
  not `T`. The dot-path (`b.Get()`) return substitution landed on 2026-10-16
  as its own commit (see `testing/cases/member_return_type/`).

## Cases

- `testing/cases/member_spec_block/` — cross-module and same-module blocks,
  ctor overload disambiguation, method template with open/bound member
  params, static field, nested class, transparency from the spec module.
- `testing/cases/std_formatter_spec/` + `std/std_format.decl.myll` — the
  `[spec=all]` real-world case: `std::formatter<T=Vec2>` for a user type;
  compiling `std::println("{}", Vec2(1,2))` is the assertion (the primary is
  disabled in libstdc++, so the format-string check fails without an enabled
  specialization). `format` is templated on the context's output iterator
  exactly like libstdc++'s enabled specializations.
- `testing/cases/std_limits_member_spec/` + `std/std_limits.decl.myll` — the
  `[spec=some]` real-world case: `std::numeric_limits<T=ByteCount>` with only
  `max` specialized; `min` keeps the primary's generic zero default.
- `template_fail` covers the attribute rules and block validation; the
  duplicate `[spec]` is a parse-stage error, so it lives in `spec_attr_fail`.

## Follow-ups (not blocking)

- Deep nesting (`[spec=some]` on a nested class head) — rejected today with
  a clean error.
- Plain (non-template) enclosing classes — a `[spec=some] class Plain {…}`
  head; C++ spelling exists, kept out of v1.
- Member spec blocks in `.decl.myll` prototype modules (bodiless members)
  — the transformer accepts bodiless members when `isDeclModule`, but the
  flow is untested.
- Dot-path return substitution (see above).
- The invisible-divergence ODR trap: a use module that cannot see a member
  specialization still instantiates the primary's member. C++ has the same
  hazard; Myll hard-rejects only the *visible* conflicts (duplicates and
  mixing).