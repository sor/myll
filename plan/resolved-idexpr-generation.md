# Plan: Use resolved targets for unqualified `IdExpr` generation — COMPLETED

> Status: **Done**. The generator now consumes `IdExpr.resolvedDecl` and emits qualified names where necessary. The original analysis is kept below for reference.

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

## Required work (all completed)

1. ✅ **Track generator scope context** — `ModuleContext` is passed through `Gen()` and the resolver marks local symbols.
2. ✅ **Distinguish member vs non-member uses** — member access keeps raw identifiers; free `IdExpr` uses `resolvedDecl`.
3. ✅ **Define a reference name that matches generated C++** — `Decl.GetReferenceName()`/`UpToNamespace` filters produce correct qualification.
4. ✅ **Suppress or remove `using namespace` emission** — global `using namespace` remains optional via `Dialect.Default.GlobalUsingNS`; name qualification reduces dependence on it.

## Outcome

The generator now has scope-aware context through `ModuleContext` and `HierarchicalGen`. `IdExpr` generation uses `resolvedDecl` to decide qualification, and the remaining edge cases are handled as ordinary resolver/generator bug fixes.

`ScopedExpr` and `TypespecNested` continue to use resolved declarations for explicit `A::B::C` references.
