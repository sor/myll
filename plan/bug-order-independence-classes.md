# Bug: class before base breaks the order-independence invariant

Status: **open**. Found in the 2026-09-29 review. Reproduced.

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

Topologically sort the `hierarchicalDecl` children by base-class dependency before concatenation:

1. Build the dependency graph among the module's own structurals (edge: `Derived` depends on
   `Base` when the base names a class/struct defined in the same module; namespace-qualified bases are
   matched by fully qualified name).
2. Stable topological sort **with minimal churn** (decided 2026-09-30): the generated C++ must
   stay recognizable as the Myll source — only move a declaration when a same-module base actually
   requires it, and preserve source order for everything else. Do not sort more than necessary.
3. Cycle (`A : B` and `B : A`) → diagnostic, do not emit.

Note (2026-09-30): forward declarations / prototypes do not solve this — C++ inheritance needs a
complete type, not a name. The invariant really requires ordering the definitions.

Rejected alternative: diagnose "base class must be defined before derived". That would break the
invariant the language advertises.

## Verification

- The repro compiles: generated C++ passes `clang++ -std=c++23 -Wall -Wextra`.
- Add a case (`testing/cases/order_independence/`) with: derived before base, diamond bases, and a
  base inside a namespace; update goldens.
- Existing goldens must not change (stable sort keeps independent classes in source order).
- `dotnet test testing/` green.

## Risk

Medium. Cases to handle: cross-module bases (already satisfied by includes; must not be re-sorted),
template bases, namespace-qualified bases, inheritance lists with multiple bases, and the
`using base = ...` alias emission, which must follow the class definition.
