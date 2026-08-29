# Plan: Use resolved targets for unqualified `IdExpr` generation

## Goal

Make the C++ generator emit qualified names when needed instead of always emitting the raw source identifier. This is the last unresolved piece of the semantic-analysis endboss.

## Why it is not trivial

The generator currently has no concept of "current scope". An `IdExpr` only carries the source name and the resolved declaration. Deciding whether to emit `x`, `Ns::x`, or `Class::x` requires knowing:

- Whether `x` is local to the current function/structor.
- Whether `x` is a non-static member being accessed inside a member function.
- Whether `x` lives in a namespace and needs qualification because no `using namespace` was emitted.
- Whether the same name is shadowed by a local or another import.

A naive attempt to emit `resolvedDecl.FullyQualifiedName` for every non-local name breaks valid code:

- Member access (`obj.field`) would become `obj.Class::field`, which is invalid for non-static members.
- Local variables inside constructors/functions get scopes whose immediate parent is the class/module, not the function declaration, so a scope-walking "is local" check fails without extra bookkeeping.
- Field access inside member functions (`field = 0;`) must stay raw/unqualified; `Class::field` refers to the static member.

## Required work

1. **Track generator scope context**
   - Pass a scope/context object through `Gen()` calls, or annotate `Decl` with an `IsLocal` flag during visiting.
   - Mark parameters and local variables as local when they are added via `AddScopeOnly`.

2. **Distinguish member vs non-member uses**
   - In `BinOp` member-access generation, use the raw right-hand identifier (already safe).
   - For free `IdExpr`, emit the resolved reference name only when it is a module/global symbol and the current context is outside its containing namespace/class.

3. **Define a reference name that matches generated C++**
   - Build it from the namespace/class hierarchy, but skip the module/global-namespace prefix because Myll modules do not emit C++ namespaces.
   - For templated types, preserve template argument syntax.

4. **Suppress or remove `using namespace` emission**
   - `using namespace` in generated C++ can be avoided once names are qualified, which is part of the point of this work.

## Recommendation

Defer until either:
- the generator gets a proper scope-aware context, or
- we decide to emit Myll modules as C++ namespaces (which would make `FullyQualifiedName` correct as-is).

For now, `ScopedExpr` and `TypespecNested` already use resolved declarations and produce correct qualified names for explicit `A::B::C` references.
