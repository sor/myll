# AST Core Model

## File Inventory

| File | Purpose | Status |
|------|---------|--------|
| `Decl.cs` | Declaration AST hierarchy | Active, core |
| `Expr.cs` | Expression AST | Active, core |
| `Stmt.cs` | Statement AST | Active, core |
| `Typespec.cs` | Type specifications | Active, core |
| `Scope.cs` | Scope tree | Active, partial |
| `Mixed.cs` | Utility classes (Param, Arg, Accessor, etc.) | Active |
| `Extensions.cs` | String/formatting utilities | Active |
| `Symbol.cs` | Symbol table entries | **Dead code** — future stub |
| `Attribute.cs` | Strongly-typed attribute enums | **Disconnected** — future stub |

## Critical Bugs

### 1. Broken Tree Traversal: `EnumerateDF`

**Severity: Critical**

`ForStmt.EnumerateDF`, `WhileStmt.EnumerateDF`, `DoWhileStmt.EnumerateDF`, and `TimesStmt.EnumerateDF` fail to yield their loop bodies.

**Impact:** Any analysis pass using `EnumerateDF` (symbol resolution, optimization, linting) will silently skip loop bodies.

**Fix:**
```csharp
// ForStmt.EnumerateDF should yield:
yield return this;
yield return init;
yield return condition;
yield return increment;
yield return body;      // MISSING!
yield return els;
foreach (var child in base.EnumerateDF) yield return child;
```

Same pattern for `WhileStmt` (missing `body`), `DoWhileStmt` (needs override), and `TimesStmt` (needs override).

### 2. Mutating Code Generation: `NewExpr.Gen()`

**Severity: Critical**

```csharp
public override string Gen(...) {
    type.ptrs.RemoveAt(0);  // DESTROYS AST!
    // ...
}
```

**Impact:** Second pass over the AST produces incorrect output. Any visitor that walks the tree after generation sees corrupted types.

**Fix:** Compute the string representation without modifying the `ptrs` list. Clone or use index-based access.

### 3. NullReferenceException in `Scope.UpToNamespace`

**Severity: Medium**

```csharp
public Scope UpToNamespace() {
    if (this is Namespace || parent == null) return this;
    return parent.UpToNamespace();  // CRASH if parent == null
}
```

**Fix:** Add null checks or ensure root scope is always a Namespace.

## AST Design Issues

### `Decl` Inherits from `Stmt`

A declaration is not semantically a statement. This causes confusion in:
- Visitors (duplicate logic in `VDecl` and `VStmt`)
- Tree structure (function bodies contain `Stmt` but also `Decl`)
- Analysis passes

**Future Outlook:** Either extract `AstNode` base class or accept that local declarations are statements (which they are in C++). If the latter, the duplication should be resolved by having `VStmt` delegate to `VDecl` for local variable declarations.

### `Enumeration.AttribsAssigned()` Leaks Generator Logic

This method synthesizes `Func` AST nodes for `[flags, operators(bitwise)]` enums, including hardcoded C++ expressions (`std::underlying_type`). This is backend logic in the AST model.

**Future Outlook:** Move operator synthesis to the generator or a post-AST-building pass.

## Dead Code / Future Stubs

### `Symbol.cs` (62 lines)

Marked `[Obsolete("not used ATM")]` on all members.

**Intended Purpose:** Semantic symbol table entries for type resolution and overload resolution.

**Revival Plan:** When implementing semantic analysis:
1. Replace `string`-based names with `Symbol` references.
2. Use `unresolvedCount` resolution state machine.
3. Integrate with `Scope` lookups.

**Status:** Keep. The design is sound; it just wasn't needed for the syntax-translator prototype.

### `Attribute.cs` Enums

Defines:
- `Dispatch` (`virtual`, `override`, `final`)
- `Implementation` (`pure`, `default`, `delete`)
- `Storage` (`static`, `thread_local`, `global`)
- `Linkage` (`external`, `internal`, `hidden`)
- `Purity` (`pure`, `const`, `nothrow`, `throw`)

**Current State:** Completely disconnected. `Decl` uses `Dictionary<string, List<string>>` and string matching (`HasAttrib("virtual")`).

**Revival Plan:**
1. Parse attribute strings into these enums.
2. Replace string checks with enum checks.
3. Add validation (e.g., `virtual` + `final` is invalid).

**Status:** Keep. This is the intended design; the current string-based system is the temporary implementation.

## `Scope.cs` Limitations

- **No lookup methods.** `Scope` stores children but has no `Resolve(name)` or `Lookup(name)`.
- **`importedScopes` declared but unused.**
- **No shadowing detection.**
- **No privacy/access checks.**
- **No overload sets.**

**Future Outlook:** When `Symbol.cs` is revived, `Scope` should become the primary lookup mechanism:
```csharp
public Symbol Resolve(string name) { /* walk scope stack */ }
public IEnumerable<Symbol> ResolveOverloads(string name) { /* return overload set */ }
```

## Future Outlook

1. **Fix `EnumerateDF`** — critical for any analysis pass.
2. **Remove side effects from `Gen()`** — critical for multi-pass compilation.
3. **Integrate `Symbol` and `Attribute` enums** — foundation for semantic analysis.
4. **Implement scope lookups** — enables symbol resolution and type checking.
