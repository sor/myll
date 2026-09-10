# Architecture & Design Quality

## Overview

Myll uses a classic compiler pipeline:

```
.myll source --► Lexer (ANTLR4) --► Parser (ANTLR4) --► CSTs
                                                               │
                                                               ▼
                                        Group by module --► Per-module ModuleContext
                                                               │
                                                               ▼
                                        Visit CSTs --► AST + Scope Stack
                                                               │
                                                               ▼
                                        NameResolver.Resolve
                                          - pre-resolution transforms
                                          - fixed-point name/type resolution
                                          - post-resolution transforms
                                                               │
                                                               ▼
                                        Code Generator (per module)
                                                               │
                                                               ▼
                                                     .h + .cpp output
```

The implementation targets .NET 10.0 and uses ANTLR4 for lexing and parsing.

## What Works Well

### Pipeline Architecture
The frontend (`Program.cs`) has clear stages:
1. Parse all input files (parallel via PLINQ).
2. Classify declarations by module and build a per-module AST + scope tree in parallel. Each module gets its own `ModuleContext` that owns the visitors and scope stack.
3. Add implicit standard-library imports (`StdBuiltins.ImplicitStdImports`).
4. Resolve names and types (`NameResolver.Resolve`), including cyclic imports. This runs a fixed-point loop and performs two AST transformation passes: pre-resolution transforms (auto-return, templates, chain, etc.) and post-resolution transforms (discard, rule-of-N, loop lowering, alias-shadowing checks).
5. Build the raw-import map and filter prototype-only modules.
6. Generate C++ declarations and implementations per module (parallel via PLINQ).
7. Emit files or invoke the C++ compiler and run the binary.

### Scope Stack
`ExtendedVisitor` provides a clean push/pop scope mechanism backed by a per-module scope stack in `ModuleContext`. It correctly tracks:
- Namespaces (including bodyless)
- Classes, structs, unions
- Functions and methods
- Lambda bodies

### Bucket-Based Generator
`HierachicalGen` separates declarations by access level (`public`, `protected`, `private`) and category, producing well-ordered C++ output.

## Major Architectural Issues

### 1. AST-Generator Coupling

**Problem:** AST nodes (`Decl`, `Expr`, `Stmt`, `Typespec`) embed `Gen()` methods that directly emit C++ strings.

**Impact:**
- Impossible to retarget to another backend (LLVM, C, another language).
- AST nodes know about C++ formatting, indentation, and standard library names.
- Changes to output format require touching core model files.

**Future Outlook:** Introduce an `ICodeEmitter` interface or visitor pattern. The `Gen()` methods become `Accept(ICodeEmitter)` calls. This decouples the model from C++-specific logic.

### 2. Decl → Stmt Inheritance

**Problem:** `Decl` inherits from `Stmt`. A declaration is semantically not a statement.

**Impact:**
- `Decl` nodes appear in statement contexts where they shouldn't.
- Causes code duplication in visitors (`VDecl` and `VStmt` both handle variable declarations).
- Confuses tree traversal and semantic analysis.

The author identifies this as the single worst design decision in the implementation. It happened because `Decl` needs everything `Stmt` can do, but `Decl` is not a `Stmt` in Myll's definition. The workaround was `MultiDecl`, a container for multiple declarations produced when convenience syntaxes created more than one decl.

**Future Outlook:** Either:
- Extract a common base (`AstNode`) that both `Decl` and `Stmt` inherit from.
- Or use composition: a function body contains a list of `Stmt`, and variable declarations are a kind of `Stmt` (making the inheritance valid, but only if local declarations truly are statements).

### 3. Static Mutable State — FIXED

**Problem (historical):** `VExt.cs` once declared static visitor instances backed by a single static `ScopeStack`. All files shared one scope stack, making parallel parsing unsafe and causing state to leak between tests.

**Current State:**
- Visitors are instance-based.
- Each module has its own `ModuleContext` that owns the scope stack and visitor instances.
- The context is passed through the frontend pipeline; `CompiledModuleResult` wraps a single `ModuleContext`.
- `GlobalNamespace.Context` was removed; the context now owns the module AST (`ModuleContext.Module`).

This was a prerequisite for parallel module compilation and is no longer an open issue.

### 4. Reflection-Based Diagnostics

**Problem:** `ToString()` in `Decl` and `Expr` uses `GetType().GetProperties()` to format debug output.

**Impact:**
- Slow at runtime.
- Fragile if property semantics change.
- Can expose internal properties unexpectedly.

**Future Outlook:**
- Implement explicit `ToString()` methods or a `DiagnosticFormatter` visitor.

## Naming & Conventions

- Inconsistent casing: `name` (lowerCamelCase) alongside `Kind` (PascalCase).
- Abbreviations: `paras` instead of `parameters`.
- `Func` conflicts with `System.Func`.
- `inindent` instead of `indentation`.

## Future Outlook

Priority architectural improvements:
1. **Decouple AST from C++ generation** — enables future backends and per-module dialect control everywhere. Currently in progress; see `plan/HierarchicalGen_Migration.md`.
2. **Fix `Decl → Stmt` inheritance** — clarifies the AST model.
3. **Parallelize remaining sequential stages** — resolution, pre/post-resolution transforms, and type checking still process all modules together. Requires splitting shared `ResolutionResult`/diagnostics state per module.
4. **Separate diagnostics from runtime logic** — improves performance.
