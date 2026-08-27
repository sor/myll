# Semantic Analysis / ScopeStack Plan

This plan describes the long-term goal of turning Myll from a syntax-to-C++ transcompiler into a language with real name and type resolution. It is the "endboss" task referenced in `AGENTS.md` and `plan/TODO.md`.

## What exists today

- `backend/Core/Scope.cs` defines `Scope`, `ScopeLeaf`, and a tree of `children`. It is built during visiting.
- `backend/Visitor/VMain.cs` has push/pop helpers and builds a scope tree per module.
- `backend/CompilationContext.cs` owns visitor instances and the scope stack for one module. The static `ScopeStack` in `VisitorExtensions` has been removed.
- `backend/Resolver/` contains `Diagnostic`, `ResolutionResult`, `UnresolvedId`, `UnresolvedType`, and `NameResolver`.
- The resolver can resolve single-segment identifiers and type names within a module and across direct imports, including cyclic imports.
- Declarations remember their scope leaf via `Decl.scope`.
- `backend/Core/Symbol.cs` and `backend/Core/Attribute.cs` are disconnected stubs kept for future reference.

## What is missing

- Qualified-name resolution (`std::vector`, `A::B`) for identifiers; types already parse as a path but are not resolved segment by segment.
- Resolution of built-in namespaces such as `std`.
- Scope tracking for parameters and local variables so function bodies can reference them.
- Type checking and overload resolution.
- Wiring the resolver into the frontend pipeline; it is currently exercised only by unit tests.

## Module model

A Myll **module** is a translation unit, not a namespace.

- The module name does not create a scope or namespace.
- Multiple `.myll` files may declare the same module; their declarations merge into one logical module.
- Two modules may declare the same top-level name. Myll leaves the resulting C++ one-definition rule violation to the C++ compiler for now. Use explicit `namespace` blocks to disambiguate.
- A module imports another module with `import other_module;`.
- Imports are order-independent. The compiler collects all imports and resolves them together.
- Cyclic imports are allowed; the fixed-point resolver handles them.
- A declaration is visible to importing modules unless marked `[hide]` or `[hidden]`.

## Declaration-before-use semantics

Myll does not require declarations to appear before uses.

```myll
func main() -> int { return square(9); }
func square(int x) -> int { return x * x; }
```

The same applies to types.

```myll
class A { B b; }
class B { A a; }
```

The compiler collects all declarations while building the per-module scope tree, resolves uses in a later pass, and emits C++ prototypes or forward declarations as needed.

## Pipeline

```
files 
  └── parse in parallel
        └── group by module
              └── per-module AST + scope tree (parallel across modules)
                    └── barrier: all module scope trees + import lists ready
                          └── global resolution
                                └── C++ generation
```

### Stage 1: Parse files in parallel
Each file is tokenized and parsed independently by ANTLR.

### Stage 2: Group files by module
Files with the same `module` declaration belong together. A file without an explicit module declaration uses its filename.

### Stage 3: Build per-module AST + local scope tree
Each module gets its own `CompilationContext` that owns the scope stack and visitor instances. All files of the module are visited into a single AST + scope tree. This stage is sequential within a module but parallel across modules.

### Stage 4: Barrier
All module scope trees and import lists are available.

### Stage 5: Global resolution
Resolve identifiers, types, and function calls. Because declarations are collected before uses, the resolver can answer most lookups immediately. Cyclic imports are handled by a fixed-point loop that publishes newly resolved facts until no module makes progress.

### Stage 6: C++ generation
Generate `.h` and `.cpp` files from the fully resolved AST.

## Resolution data structures

The resolver operates on unresolved reference records collected during AST building and stores resolved targets in a separate map so the AST stays unchanged.

```csharp
public sealed record UnresolvedId( IdExpr Node, Scope Scope );
public sealed record UnresolvedType( TypespecNested Node, Scope Scope );

public sealed class ResolutionResult
{
    public IReadOnlyDictionary<IdExpr, Decl>          Ids;
    public IReadOnlyDictionary<TypespecNested, Decl>  Types;
}
```

`IdExpr` and `TypespecNested` are reference-equal AST nodes, so they can be used as dictionary keys. The resolved target is a `Decl` instance for now; a lighter symbol representation can be introduced later if needed.

A sequential fixed-point resolver is the first target. The same input/output shape can later be processed by parallel workers that synchronize through barriers.

## First implementation milestone

1. **Instance-based visitors** ✅  
   `CompilationContext` per module replaced the static `ScopeStack` and static visitor instances. All parser-context extension methods now take an explicit `CompilationContext`.

2. **Per-module scope-tree building** ✅  
   Each module builds its own AST + scope tree. Unresolved `IdExpr` and `TypespecNested` references are recorded in the context as they are created.

3. **Sequential fixed-point resolver** ✅  
   `NameResolver.Resolve` builds per-module export tables and resolves references in rounds until no module makes progress. It handles cyclic imports and produces `Diagnostic` records for unresolved names.

4. **C++ generation uses resolved AST**  
   Not done. The resolver is currently exercised by unit tests only. Once built-ins and qualified names resolve reliably, wire it into the frontend and update code generation to use the resolution map.

## Relationship to existing stubs

- `backend/Core/Symbol.cs` is kept as a reference design. The current resolver uses `Decl` directly because it is simpler for the prototype. If memory or library-interface size becomes a concern, replace `Decl` targets with a lighter symbol record.
- `backend/Core/Attribute.cs` provides strongly-typed enums for dispatch, storage, linkage, and purity. Revive it when replacing string-based attribute checks, especially for `[hide]`/`[hidden]` export control.

## Default includes and dialect mapping

The current `DefaultIncludes` in `backend/Generator/StmtFormatting.cs` are split into three categories:

### Essential / hard to replace
Always included because they back built-in types or core generated constructs:

- `<cstddef>` — `std::byte`, `size_t`, `nullptr_t`
- `<cstdint>` — `std::int8_t`, `std::uint64_t`, etc.
- `<type_traits>` — `std::underlying_type`, `remove_const_t`, etc.

### Small / usually fine
- `<utility>` — `std::move`, `std::pair`, `std::swap`

### Heavy / mappable or opt-out via dialect
Included by default today, but should be configurable per dialect:

- `<string>` — `std::string`; could be replaced by `QString` or disabled entirely.
- `<memory>` — smart pointers; could be replaced by a custom allocator or disabled.
- `<cmath>` — math functions; could be disabled for projects that do not need them.

Implementation is deferred until dialect configuration lands.

## Resolvation-Misrouting

A risk called **Resolvation-Misrouting** exists when resolution starts locally with unbounded depth before the global module picture is known. The current design seeds the resolver with all module exports first to avoid this. The author needs to explain the exact failure mode later; this note prevents premature optimization with local-first resolution.

## Open questions

1. **Linkage model**: work in progress on a different machine, not yet pushed. When it lands, revisit whether Myll should track ODR violations itself or continue delegating to the C++ compiler.
2. **Import scope per module or per file**: is `import std_vector;` in one file of module `M` visible in other files of `M`?
3. **Export control beyond `[hide]`/`[hidden]`**: will there be explicit `export` keywords or attributes?
4. **Built-ins**: which types/includes are implicit in every module and which require explicit import? For now, primitives are implicit; `<string>` and `<memory>` are default but dialect-mappable.
5. **Parallel resolution strategy**: keep the first resolver sequential but design it as a single-worker version of the future parallel batch resolver.

## Related documents

- `docs/analysis/01-architecture.md` — static mutable state, `Decl`→`Stmt` inheritance.
- `docs/analysis/07-future-stubs.md` — `Symbol.cs`, `Attribute.cs`, and the three-pass resolution design.
- `docs/design/features/modules.md` — module and namespace syntax.
