# Semantic Analysis / ScopeStack Plan

This plan describes the long-term goal of turning Myll from a syntax-to-C++ transcompiler into a language with real name and type resolution. It is the "endboss" task referenced in `AGENTS.md` and `plan/TODO.md`.

## What exists today

- `backend/Core/Scope.cs` defines `Scope`, `ScopeLeaf`, and a tree of `children`. It is built during visiting but not used for lookups.
- `backend/Visitor/VMain.cs` has push/pop helpers and builds a skeleton scope tree per module.
- `backend/Visitor/VExt.cs` keeps a static `Stack<Scope>` shared by all visitors.
- Declarations remember their scope leaf via `Decl.scope`.
- `backend/Core/Symbol.cs` and `backend/Core/Attribute.cs` are disconnected stubs designed for this work.

## What is missing

- Name-to-declaration resolution for identifiers in expressions and types.
- Type-to-declaration linking for `Typespec` nodes.
- Cross-module resolution via imports.
- Type checking and overload resolution.
- A thread-safe, instance-based visitor infrastructure.

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

The resolver should operate on small, immutable facts so it can later run in parallel rounds.

```csharp
public abstract record Resolvable( SrcPos Location );

public sealed record IdResolvable(
    SrcPos Location,
    string Name,
    Scope Scope ) : Resolvable( Location );

public sealed record TypeResolvable(
    SrcPos Location,
    Typespec Typespec,
    Scope Scope ) : Resolvable( Location );

public abstract record ResolutionFact;

public sealed record ResolvedNameFact(
    Resolvable Target,
    Decl Declaration ) : ResolutionFact;

public sealed record ResolvedTypeFact(
    Typespec Typespec,
    Decl TypeDecl ) : ResolutionFact;

public sealed class ResolutionContext
{
    public Scope GlobalScope { get; }

    public void PublishFact( ResolutionFact fact );

    public bool TryResolveName(
        string name,
        Scope scope,
        out Decl decl );

    public bool TryResolveType(
        Typespec type,
        Scope scope,
        out Decl decl );
}
```

A sequential fixed-point resolver is the first target. The same fact representation can later be processed by parallel workers that synchronize through barriers.

## First implementation milestone

1. **Instance-based visitors**  
   Replace the static `ScopeStack` and static visitor instances in `backend/Visitor/VExt.cs` with a `CompilationContext` per module. This makes per-module building thread-safe and test-isolated.

2. **Per-module scope-tree building**  
   Use the new context when the frontend builds each module's AST. Record imports but do not resolve them yet.

3. **Sequential fixed-point resolver**  
   Merge module scope trees, collect all `Resolvable` items, and resolve them in rounds. Publish `ResolutionFact` records and stop when a round makes no progress. Use the resolution states and helpers already sketched in `backend/Core/Symbol.cs`.

4. **C++ generation uses resolved AST**  
   Ensure `IdExpr`, `FuncCallExpr`, and `Typespec` generation can consume resolved declarations. Emit forward declarations / prototypes where the C++ compiler needs them.

## Relationship to existing stubs

- `backend/Core/Symbol.cs` provides the intended symbol representation and resolution-state machine. Revive it as the resolved form attached to AST nodes.
- `backend/Core/Attribute.cs` provides strongly-typed enums for dispatch, storage, linkage, and purity. Revive it when replacing string-based attribute checks, especially for `[hide]`/`[hidden]` export control.

## Open questions

1. **Linkage model**: work in progress on a different machine, not yet pushed. When it lands, revisit whether Myll should track ODR violations itself or continue delegating to the C++ compiler.
2. **Import scope per module or per file**: is `import std_vector;` in one file of module `M` visible in other files of `M`?
3. **Export control beyond `[hide]`/`[hidden]`**: will there be explicit `export` keywords or attributes?
4. **Built-ins**: which types/includes are implicit in every module and which require explicit import?
5. **Parallel resolution strategy**: keep the first resolver sequential but design it as a single-worker version of the future parallel batch resolver.

## Related documents

- `docs/analysis/01-architecture.md` — static mutable state, `Decl`→`Stmt` inheritance.
- `docs/analysis/07-future-stubs.md` — `Symbol.cs`, `Attribute.cs`, and the three-pass resolution design.
- `docs/design/features/modules.md` — module and namespace syntax.
