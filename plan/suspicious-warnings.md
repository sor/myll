# Suspicious warning areas

This plan tracks the warning clusters in the backend that need a decision before they are fixed.

The `HierarchicalGen` cast warnings, the nullable-reference-type migration, and the `NewExpr` smart-pointer codegen issue are resolved and documented in `plan/code-health.md`. No suspicious clusters remain open.

## 1. NewExpr code generation (backend/Core/Expr.cs) — resolved

Original problem:

- The smart-pointer branch dropped constructor arguments. `funcCall` was only used in the non-smart-pointer branch, so `new Widget*!( 1, 2 )` became `std::make_unique<Widget>()`.
- The method mutated the AST with `type.ptrs.RemoveAt( 0 )`.

Fix chosen: option D — combine B and C, then align the raw-pointer rule with the smart-pointer rule.

- `new` always consumes the outermost pointer of the type written after it.
- For a bare type, the outermost pointer is an implicit raw pointer, so `new T` (and `new T*`) returns `T*`.
- For an outermost raw pointer (`T**`, `T*!*`), `new` strips that outermost raw pointer and emits C++ `new` of the inner type.
- For an outermost smart pointer (`T*!`, `T*!*!`), `new` strips that smart pointer and emits `std::make_unique` / `std::make_shared` of the inner type.
- Save the original `type.ptrs` list, temporarily replace it with the list minus the outermost pointer, generate the inner type, then restore the original list. This avoids mutating the AST.
- Pass constructor arguments to `std::make_unique` / `std::make_shared` for scalar smart pointers and `ptr.expr` for the size of smart arrays.

Verification:

- Added `testing/cases/class_ctor/main.myll` covering a zero-param ctor, a two-param ctor, raw `new`, and smart-pointer `new`.
- The generated `testing/generated/class_ctor/main.cpp:22` now emits `std::make_unique<Widget>(5, 6)`.
- `dotnet test testing/` passes (8/8).
