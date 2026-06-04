# Future Stubs and Unused Code

This document indexes code that is currently unused but was designed for future features. Do not delete without checking this index.

## Symbol.cs

**Location:** `backend/Core/Symbol.cs`  
**Lines:** ~62  
**Status:** All members marked `[Obsolete("not used ATM")]`

**Intended Purpose:** Semantic symbol table entries for the three-pass resolution algorithm.

**Fields:**
- `Kind` enum (`Type`, `Func`, `Var`, `Namespace`, etc.)
- `unresolvedCount` with resolution states (`-2`, `-1`, `0`)
- `PairList<TKey, TValue>` and `MultiDict<TKey, TValue>` helper collections

**Revival Conditions:** When implementing semantic analysis:
1. Connect with `Scope.Resolve()` lookups.
2. Replace string-based names in AST nodes with `Symbol` references.
3. Use resolution state machine for multi-pass type checking.

**Why Keep:** The design is sound. This symbol table was explicitly designed as the foundation for semantic analysis.

## Attribute.cs Enums

**Location:** `backend/Core/Attribute.cs`  
**Lines:** ~116

**Enums Defined:**
- `Dispatch`: `virtual`, `override`, `final`
- `Implementation`: `pure`, `default`, `delete`
- `Storage`: `static`, `thread_local`, `global`
- `Linkage`: `external`, `internal`, `hidden`
- `Purity`: `pure`, `const`, `nothrow`, `throw`

**Current State:** None are referenced elsewhere in the codebase.

**Intended Purpose:** Replace the current `Dictionary<string, List<string>>` attribute system with strongly-typed enums.

**Revival Conditions:**
1. Parse attribute strings into enum values in `VDecl`.
2. Replace string checks (`HasAttrib("virtual")`) with enum checks.
3. Add conflict validation (e.g., `virtual` + `final` is invalid).

**Why Keep:** Strong typing prevents bugs and enables IDE autocomplete. The current string system is a temporary prototype solution.

## .d.ts-like Signature Caching

**Source:** Three-pass resolution design, Step 2  
**Status:** Designed but not implemented

**Description:** After global symbol resolution, save the resolved signatures to a file format similar to TypeScript's `.d.ts`. This would enable:
- Partial recompilation (read signatures instead of re-parsing dependencies).
- C++ interop (hand-written C++ signatures exported for Myll).

**Implementation Sketch:**
```csharp
// After resolving global names, serialize:
{
  "module": "std",
  "types": [{ "name": "vector", "kind": "template", "params": ["T"] }],
  "functions": [{ "name": "sort", "params": [...], "return": "void" }]
}
```

**Why Keep:** Essential for scalable compilation and IDE support.

## Aspect and Concept Grammar Stubs

**Location:** `backend/Grammar/MyllParser.g4`  
**Status:** Parser rules exist but bodies are `// TODO`

**Description:**
```
defAspect: // TODO
    ;
defConcept: // TODO
    ;
```

**Intended Purpose:**
- `aspect`: Cross-cutting concerns (similar to aspect-oriented programming or Rust traits).
- `concept`: Generic constraints (C++20 concepts equivalent).

**Revival Conditions:**
1. Define the grammar for aspect attachment point syntax.
2. Define concept requirements syntax (`requires T > 0`, `requires Copyable<T>`).
3. Implement visitors and code generation.

## Convert Functions

**Location:** `backend/Visitor/VDecl.cs`  
**Status:** `VisitDefConvert` calls `base.VisitDefConvert(c)` — stubbed

**Description:** Conversion functions for implicit type conversions beyond constructors.

**Why Stubbed:** Convert semantics intersect with C++'s conversion operators and implicit conversion rules. Required careful design to avoid conflicts.

## Range-Based For

**Status:** Grammar stub not yet added.

**Syntax Vision:**
```
for item in container {
    // body
}
```

## Template Specialization

**Status:** Not in grammar.

**Syntax Vision:**
```
struct Container<int> {
    // specialized implementation
}
```

## Variable Templates

**Status:** Not in grammar.

**Syntax Vision:**
```
var PI<T>: T = 3.14159265358979;
```

## Full Semantic Analysis (Steps 2 & 3)

**Source:** Three-pass resolution design (Step 2 and Step 3)

**Step 2:** Resolve global references (using, base classes, global variable types, function signatures).  
**Step 3:** Resolve local identifiers within function bodies using scope stack.

**Both steps are explicitly noted as "not fully implemented in the current prototype."**

The three-step architecture is sound. When returning to semantic analysis:
1. Step 1 already works (global name collection during CST walk).
2. Step 2 needs the symbol-resolution algorithm (see `Symbol.cs` stub).
3. Step 3 needs function-body resolution with scope stack.

## Non-Type Template Parameters

**Source:** "Unsolved Issues"  
**Status:** No syntax designed.

**Challenge:** How to syntactically distinguish `Container<10>` (value) from `Container<int>` (type)?

## Comment Preservation

**Source:** Original design notes, lexer has `COMMENTS` channel.  
**Status:** Comments are lexed but not attached to AST or emitted.

**Revival Plan:** Attach comment tokens to nearest AST node and emit during code generation.
