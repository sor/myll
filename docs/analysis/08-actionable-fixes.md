# Actionable Fixes — Prioritized

This document consolidates fixes across all layers, ranked by impact on future development.

## Recently Fixed / Documented Bugs

### Compiler crash on `_ = expr;`

**Status:** open bug  
**Files:** `backend/Core/Expr.cs`, `backend/Resolver/DiscardTransformer.cs`  
**Impact:** Using `_ = expr;` to intentionally discard a value causes an unhandled `NotImplementedException` during code generation.

**Repro:** In any function body write

```myll
func foo( const DirEntry & e ) -> string {
    _ = e;
    return "";
}
```

The compiler crashes with:

```
System.NotImplementedException at Myll.Core.Discard.Gen(Boolean doBrace)
   in backend/Core/Expr.cs
```

**Workaround:** Reference the value in a harmless way instead of discarding it, or give the parameter no name if your goal is only to silence an unused-parameter warning:

```myll
func format( const DirEntry & ) -> string => "";
```

**Root cause:** `Discard.Gen()` is stubbed with `throw new NotImplementedException()`. `DiscardTransformer` is supposed to remove every `Discard` node before generation, but `_ = expr;` apparently reaches the transformer as an `ExprStmt`/`AggrAssign` path that is not lowered, so the raw `Discard` survives.

**Possible fix:** Make `DiscardTransformer` handle `_ = expr` in all statement shapes, or implement a safe fallback in `Discard.Gen()`.

### Multi-level `break`/`continue` flag persistence (fixed)

**Status:** fixed  
**Files:** `backend/Resolver/BreakContinueTransformer.cs`

**Problem:** The flag-based lowering declared helper flags outside the targeted loop. A flag set during iteration N therefore leaked into iteration N+1.

Concrete example: `continue 2` from inside a `switch` that sits in an outer loop set the outer loop's continue flag. After the continue, iteration N+1 started with the flag still `true`. The guard after the first statement of the outer body immediately continued again, skipping the rest of the body.

**Fix:** Reset all active breakable flags to `false` at the start of every loop body.

**Why flags instead of goto:** Flags keep the existing loop generators untouched and work uniformly for both loops and switches. A goto-based lowering would need different label placement for `for`/`while`/`do-while` and for switch targets.

## 🔴 Critical — Blocks Progress

### 1. Fix `EnumerateDF` for Loop Statements — done

**Files:** `backend/Core/Stmt.cs`
**Impact:** Any analysis pass (symbol resolution, optimization, linting) silently skips loop bodies.
**Status:** Fixed. Overrides added for `ForStmt`, `WhileStmt`, `DoWhileStmt`, and `TimesStmt`; unit tests added in `testing/LoopEnumerationTests.cs`.

**Fix for `ForStmt`:**
```csharp
public override IEnumerable<Stmt> EnumerateDF {
    get {
        yield return this;
        yield return init;
        yield return condition;
        yield return increment;
        yield return body;  // ADD THIS
        yield return els;
        foreach (var c in base.EnumerateDF) yield return c;
    }
}
```

**Also fix:** `WhileStmt` (add `body`), `DoWhileStmt` (create override), `TimesStmt` (create override).

### 2. Remove AST Mutation from `NewExpr.Gen()` — done

**File:** `backend/Core/Expr.cs`
**Impact:** `type.ptrs.RemoveAt(0)` destroyed the AST for subsequent passes.
**Status:** Fixed. `NewExpr.Gen()` now temporarily replaces `type.ptrs` with a copy that excludes the smart pointer and restores the original list after generation. Constructor arguments are passed to `std::make_unique` / `std::make_shared` for scalar smart pointers.

### 3. Fix `Scope.UpToNamespace()` NullReference — done

**File:** `backend/Core/Scope.cs`
**Impact:** Crash if called on a scope with no namespace ancestor.
**Status:** Fixed. The implementation now uses `parent?.UpToNamespace ?? UpToGlobal` so the root scope is returned when no namespace ancestor exists.

## 🟡 High — Significantly Improves Maintainability

### 4. Wire Missing Visitor Overrides

**Priority order:**
| Visitor | File | Effort |
|---------|------|--------|
| `stmtTryCatch` | `VStmt.cs` | 1–2 hours |
| `stmtDefer` | `VStmt.cs` | 1–2 hours |
| `stmtContinue` | `VStmt.cs` | 30 minutes |
| `stmtReturnIf` | `VStmt.cs` | 30 minutes |
| `RangeExpr` | `VExpr.cs` | 30 minutes |
| `ThreeWayConditionalExpr` | `VExpr.cs` | 30 minutes |
| `ThrowExpr` | `VExpr.cs` | 30 minutes |

### 5. Consolidate Duplicate Logic

- **Remove `VisitLit` / `VisitLiteralExpr` duplication** in `VExpr.cs` — 15 minutes
- **Consolidate `VisitDefVar`** — have `VStmt.VisitDefVar` delegate to a shared `BuildVarDecl()` method or to `VDecl` — 30 minutes

### 6. Replace Mutable `curAccess` with Decl-Level Access — done

**File:** `backend/Visitor/VDecl.cs`, `backend/Core/Decl.cs`
**Impact:** Access modifiers are now extracted from per-declaration attributes in `Decl.AssignAttribs`. Section-form `[pub]:` / `[priv]:` / `[prot]:` still sets the visitor-level default.
**Status:** Implemented.

**Remaining:**
- Enforce that access specifiers outside class/struct/union are an error.
- Decide how access attributes on nested class declarations are handled.

### 7. Implement Missing Generator Methods

- **`AddOperator`** in `HierachicalGen.cs` for operator overloading — 1 hour
- **Accessor/property generation** (currently parsed but skipped) — 2–4 hours
- **Subclass/inheritance generation** (partially implemented) — 2–4 hours

### 8. Fix Resource Leaks — done

**File:** `frontend/Program.cs`
**Impact:** `StreamReader` and `FileStream` were never disposed.
**Status:** Fixed. The file contents are now read with `File.ReadAllText(...)`; there are no explicit `StreamReader`/`FileStream` instances left in the frontend code path.

## 🟢 Medium — Quality of Life Improvements

### 9. Make Visitors Instance-Based — done

**File:** `backend/Visitor/VExt.cs`
**Impact:** Enables parallel parsing and testing isolation.
**Status:** Fixed. `CompilationContext` per module owns visitor instances and the scope stack; all parser-context extension methods take an explicit `CompilationContext`.

### 10. Extract C++ Standard Library Names

**File:** `backend/Core/Typespec.cs`
**Estimated Effort:** 1 hour

**Fix:** Move `std::unique_ptr`, `std::vector`, etc. to a configurable mapping dictionary.

### 11. Add CLI Options for Toolchain

**File:** `frontend/CommandLineOptions.cs`
**Estimated Effort:** 30 minutes

**Add:** `--cxx-compiler`, `--std`, `--include-path`

### 12. Implement Basic Test Harness

**New file:** `test_runner.py` or `Myll.Tests/` (xUnit project)
**Estimated Effort:** 4–8 hours

**Minimum viable:**
- Discover all `.myll` files
- Compile each
- Compare output against reference `.cpp`/`.h` (if exists)
- Flag differences

### 13. Attach Source Locations to Compiler Errors

**Files:** `backend/Generator/HierachicalGen.cs`, `backend/Visitor/*.cs`, `backend/Core/Node.cs` (or wherever AST nodes store token/line info)
**Impact:** User-facing errors (invalid attribute combinations, duplicate declarations, unsupported constructs, etc.) currently report only a message. The user has to search the file manually.
**Estimated Effort:** 4–8 hours

**Fix:**
- Ensure every AST node can carry source location (file path, line, column) from its originating `IToken`/parser context.
- Introduce a `CompilerException` or `Diagnostic` type that stores location and message.
- Update CLI and test harness to print diagnostics as `file:line:column: error: message`.

## 🔵 Low — Nice to Have

### 14. Implement `ToString()` Diagnostics Without Reflection

**Files:** `Decl.cs`, `Expr.cs`
**Estimated Effort:** 1–2 hours

**Fix:** Write explicit `ToString()` methods or a `DiagnosticFormatter`.

### 14. Complete `BasicFormat` Type Map

**File:** `StmtFormatting.cs`
**Estimated Effort:** 10 minutes

**Add:** `Size` → `size_t`, `char8_t` → `char8_t`.

### 15. Add `IndentAll` Windows Line Ending Support

**File:** `Extensions.cs`
**Estimated Effort:** 15 minutes

**Fix:** Handle `\r\n` in `IndentAll`.

## Fix Order Recommendation

If implementing sequentially, the recommended order is:

1. **Fix `EnumerateDF`** (critical bug) — 15 min — done
2. **Fix `NewExpr.Gen()` mutation** (critical bug) — 15 min — done
3. **Fix `Scope.UpToNamespace()` NRE** (crash prevention) — 10 min
4. **Wire high-priority visitors** (`try/catch`, `defer`, `continue`) — 2–4 hours
5. **Per-declaration access modifiers** — done
6. **Member access resolution** — done
7. **Implement missing generator methods** (operators, accessors) — 3–8 hours
7. **Reduce duplication** (`VisitLit`, `VisitDefVar`) — 45 min
8. **Fix resource leaks** — 10 min
9. **Build test harness** — 4–8 hours
10. **Add CLI toolchain options** — 30 min
11. **Extract stdlib names** — 1 hour
12. **Attach source locations to compiler errors** — 4–8 hours
13. **Integrate `Symbol.cs` and `Attribute` enums** (semantic analysis prep) — 1–2 days
