# Bug: class before base breaks the order-independence invariant

Status: **fixed 2026-10-02**. Regression case: `testing/cases/order_independence/`;
cycle case: `testing/cases/order_independence_fail/`.

## Symptom

A class defined before its base class in the same module generates invalid C++, violating the core
invariant "order in a file or module does not matter".

## Repro

`tmp/review/order_indep.myll`:

```myll
class Derived : Base {
}

class Base {
[pub]:
	field int x;
}
```

Generated header (`tmp/review/generated/order_indep/order_indep.hpp`):

```cpp
class Derived;
class Base;
class Derived : public Base { using base = Base; };
class Base { public: int x{}; };
```

clang++: `error: base class has incomplete type` at `class Derived : public Base`.

## Root cause

`HierachicalGen` emits every forward declaration in `protoEarly`, then emits full class definitions in
`hierarchicalDecl` **in source order** (around `backend/Generator/HierachicalGen.cs:973`). No pass
topologically reorders structurals by base-class dependency. Forward declarations satisfy name
lookup (so Myll-side resolution is fine) but not C++ inheritance, which needs a complete type.

## Fix plan

Implemented as a two-part change (no generator-side sorting):

1. **`StructuralOrderTransformer`** (`backend/Transform/StructuralOrderTransformer.cs`, post-resolution
   in `Resolver.Resolve`): collects all defined structurals of the module and their hierarchical
   parents, resolves each base via `TypespecNested.resolvedDecl` (plus `TryGetResolved` fallback),
   and records a child edge at the lowest common ancestor for every same-module base. Each affected
   parent gets a stable topological sort of its children (Kahn, ready set scanned in original
   order), so only declarations whose subtree really holds a base are moved. Cross-module bases are
   left to the includes. Special cases diagnosed: self-inheritance, inheritance of an enclosing
   class, inheritance of a class nested inside the derived one, and cycles (sort cannot drain), all
   as errors.
2. **Nested-type emission in source order** (`HierachicalGen`): the `hierarchicalDecl` bucket became
   an `OrderedAccessSourceLines` — nested classes/structs/unions/enums emit in declaration order
   with access specifiers at change points instead of priv/prot/pub groups. This is what makes
   mixed-access-section inheritance (`[priv]` derived after `[pub]` base in one class) orderable at
   all, so the child reorder covers everything and no cross-section diagnostic is needed. Same
   golden byte-stability is kept for same-access sequences.

Note (2026-09-30): forward declarations / prototypes do not solve this — C++ inheritance needs a
complete type, not a name. The invariant really requires ordering the definitions.

Rejected alternative: diagnose "base class must be defined before derived". That would break the
invariant the language advertises.

Follow-up (out of scope here): module-scope `var D x;` before D's definition still needs the
complete type too; same-module value declarations are not reordered yet.

## Verification

- The repro compiles: generated C++ passes `clang++ -std=c++23 -Wall -Wextra` via the case's compile depth.
- `testing/cases/order_independence/` covers: derived before base, diamond bases, a base inside a
  later namespace, a template base defined later, and mixed access sections. Goldens recorded.
- `testing/cases/order_independence_fail/` covers the inheritance cycle.
- Existing goldens stayed byte-identical (full suite green), proving the sort moves nothing without
  need and the ordered nested-type emission preserves same-access order.

## Risk

Medium. Cases to handle: cross-module bases (already satisfied by includes; must not be re-sorted),
template bases, namespace-qualified bases, inheritance lists with multiple bases, and the
`using base = ...` alias emission, which must follow the class definition.
