# Architecture & Design Quality

## Overview

Myll uses a classic compiler pipeline:

```
.myll source --► Lexer (ANTLR4) --► Parser (ANTLR4) --► Visitors --► AST
                                                                         │
                                                                         ▼
                                                              Scope Stack
                                                                         │
                                                                         ▼
                                                              Code Generator
                                                                         │
                                                                         ▼
                                                           .h + .cpp output
```

The implementation is in C# (.NET 6.0), using ANTLR4 for lexing and parsing.

## What Works Well

### Pipeline Architecture
The frontend (`Program.cs`) has clear stages:
1. Parse all input files (parallel via PLINQ)
2. Classify declarations by module
3. Visit CST to build AST
4. Generate C++ declarations and implementations
5. Emit files or compile directly

### Scope Stack
`ExtendedVisitor` provides a clean push/pop scope mechanism that correctly tracks:
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

### 3. Static Mutable State

**Problem:** `VExt.cs` declares static visitor instances backed by a single static `ScopeStack`:
```csharp
public static ExprVisitor ExprVis = new ExprVisitor();
public static StmtVisitor StmtVis = new StmtVisitor();
public static DeclVisitor DeclVis = new DeclVisitor();
// All share one static ScopeStack
```

**Impact:**
- Non-thread-safe despite PLINQ parallel parsing.
- Impossible to parse multiple files simultaneously.
- Testing is harder because parser state leaks between tests.

**Future Outlook:**
- Make visitors instance-based.
- Pass `ScopeStack` as a constructor parameter or context object.
- Use thread-local storage as a minimal workaround.

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
1. **Decouple AST from C++ generation** — enables future backends.
2. **Fix `Decl → Stmt` inheritance** — clarifies the AST model.
3. **Eliminate static visitor state** — enables parallel compilation.
4. **Separate diagnostics from runtime logic** — improves performance.
