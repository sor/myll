# Open TODOs

This file consolidates the remaining work items that came out of the visitor audit, the Unix-app porting work, and the `AGENTS.md` planned list.
The endboss architecture is described in `plan/semantic-analysis.md`.

## Endboss — semantic analysis / ScopeStack

This is the overarching long-term goal that blocks many smaller features. Finish the scope-stack based semantic-analysis pass so the compiler can resolve names to declarations, type-check expressions, and disambiguate language constructs that currently rely on string matching or syntactic guesswork.

- `backend/Core/Scope.cs` holds the tree (`Scope`, `ScopeLeaf`, `children`, `importedScopes`).
- `backend/Visitor/VMain.cs` pushes/pops scopes during visiting and records unresolved identifiers, types, and member accesses.
- `backend/Resolver/Resolver.cs` runs a fixed-point resolver across imported modules and produces diagnostics for unresolved or ambiguous names.
- The C++ generator consumes `ResolutionResult` for `TypespecNested`, `ScopedExpr`, and member access; `IdExpr` resolution is recorded but not yet used for unqualified identifiers.
- `backend/Core/Symbol.cs` and `backend/Core/Attribute.cs` remain disconnected stubs for a future stable design.

What finishing this would unlock:
- Type checking and overload resolution (implemented for core cases).
- Clean semantic errors with source locations.
- Type-driven disambiguation in the generator (e.g. `enum` inheritance, operator synthesis, `convert`).

Completed endboss pieces:
- **Expression type model**: `Expr.Type`, `TypeResolver`, and `ConversionRules`.
- **Overload resolution**: ranked selection (exact + promotion), arity filtering, and diagnostics for ambiguous and no-matching calls. End-to-end test: `testing/cases/overload/`.
- **Core type checking**: assignments, variable/field initializers, return statements, and single-candidate argument compatibility. Negative tests: `testing/cases/typecheck_fail/`.
- **Declaration validation**: attribute combination and duplicate-name checks moved from `backend/Generator/HierachicalGen.cs` into `backend/Resolver/TypeChecker.cs`, producing source-location diagnostics. Negative test: `testing/cases/decl_fail/`.
- **Built-in operator checks**: hard-coded rule-based validation and result-type computation for arithmetic, bitwise, shift, comparison, and unary operators on scalar built-in types. Pointer arithmetic is rejected by default. Negative test: `testing/cases/operator_fail/`.
- **Spaceship operator**: the generator now emits C++ `<=>` instead of the old `cmp(...)` placeholder.
- **Literal typing**: integer literals are untyped until bound to a target; valid targets are fitting integer types and floats. Float literals are untyped and only bind to floats.

See `docs/analysis/01-architecture.md` (static mutable state, `Decl`→`Stmt` inheritance) and `docs/analysis/07-future-stubs.md` for related context.

## High priority

1. **Per-declaration access modifiers** — done
   - Implemented `[pub]`, `[priv]`, `[prot]` on individual class/struct/union members; the attribute overrides the current section default from `[pub]:` / `[priv]:` / `[prot]:`.
   - Field order is preserved; access labels change inline.
   - End-to-end test: `testing/cases/access_mods/`.
   - Enforced: access attributes are only valid inside class/struct/union declarations.

2. **Repair the `try/catch` grammar**
   - Change `catchClause` from `CATCH funcTypeDef? stmt` to `CATCH LPAREN param? RPAREN stmt`.
   - Update `VStmt.VisitStmtTryCatch` and any generator formatting that depends on the catch clause.

3. **Implement reachable missing expression/statement features**
   - Named function arguments (`Arg.Gen` in `backend/Core/Mixed.cs`).
   - Null-coalescing function calls (`FuncCall.Gen` in `backend/Core/Mixed.cs`).
   - Copy-cast (`VisitPreExpr` in `backend/Visitor/VExpr.cs`).
   - Discard expression (`Discard.Gen` in `backend/Core/Expr.cs`).

4. **Improve compiler diagnostics**
   - Replace empty `throw new NotImplementedException()` calls with messages that include the construct and source location.
   - Emit a clear error when `alias` is used on a namespace until namespace aliases are supported.

5. **Prototype declaration files**
   - Adopt `.d.myll` and `.decl.myll` as the extensions for silent resolver-only prototypes.
   - Inline `[extern]` in normal `.myll` files should emit C++ forward declarations.
   - Extend `[extern]` propagation to classes/structs so members resolve but are not emitted.
   - Add grammar/visitor/generator support for forward declarations in prototype files.
   - See `plan/prototype-files.md`.

## Medium priority

4. **Language syntax gaps**
   - Direct constructor syntax (`T name(args);`, `T(args);`) instead of only `var T name = T(args);`.
   - Range-based `for` loops (`for( a : b )`).
   - Namespace aliases via `alias` (`alias fs = std::filesystem;` should generate `namespace fs = std::filesystem;`).
   - Auto-property / combined private-field + public-getter syntax.

5. **Cleanup**
   - Remove `oldParser/` and `oldCodeGen/` once nothing references them.
   - Triage the TODO/FIXME/HACK markers across backend/frontend/testing.
   - Clean up the `= null!` initializers by introducing real constructors or nullable annotations.

6. **Partial/broken generators**
   - Complete `VisitScopedExpr` (`backend/Visitor/VExpr.cs`).
   - Review/replace the lambda hack in `VisitLambdaExpr`.
   - Validate the switch/case generator paths (`caseBlock`, `defaultBlock`, `StmtSwitch`).

7. **Other generators / language features**
   - Accessor/property generation (currently parsed but skipped).
   - Subclass/inheritance generation (partially implemented).

## Lower priority / design decisions

8. **Imports**
   - Support subdirectory paths in import names or add a string-form import (`import "some/lib/header.hpp";`).
   - File-wide imports are the end goal; module-wide imports may be used as a first step.

9. **CI/CD**
   - Set up a CI workflow for automated builds and tests.
   - Decide whether to commit the generated ANTLR files or always regenerate during build.

10. **Open language design questions**
   - Should `return _` mean "return default"?
   - Should `switch` support `else:` as an alias for `default:`?
   - Should `(copy)(expr)` be allowed on prvalues?
   - **Switch-case fallthrough dialect**: allow the user/program to select whether cases implicitly `break`, implicitly fall through, or require an explicit `break`/`fallthrough` annotation per case. The mode is modeled in `backend/Core/Dialect.cs` (`SwitchFallthroughMode`) but is not wired through the parser/generator yet.
   - **Rule-of-N class enforcement**: allow `[rule_of=0|3|5]` on a class to require the corresponding C++ special member functions. A default is modeled as a `[Flags]` enum in `backend/Core/Dialect.cs` (`RuleOf`) and should be checked during/after class declaration analysis. The dialect mask can list multiple allowed rules; a class passes if it adheres to any one of them.
   - **Linkage model**: work in progress on another machine. Decide which declarations get internal/external/module linkage and how it interacts with cross-module imports.
   - **Export control**: is `[hide]`/`[hidden]` the only module-export mechanism, or will there be explicit `export`?

## Recently completed

- Visitor audit (`plan/visitor-audit.md`) and fail-loudly overrides.
- `continue`, dotted imports, `try/catch` codegen, `\0` char literal fix.
- `var` parameter error message.
- ProcessRunner thread-safety fix.
- `ls` vertical slice with classes, inheritance, smart-pointer shorthand, sorting, and hidden files.
- Fixed `EnumerateDF` for `ForStmt`, `WhileStmt`, `DoWhileStmt`, and `TimesStmt` and added unit tests in `testing/LoopEnumerationTests.cs`.
- Instance-based `CompilationContext` per module; removed static visitor state.
- Name resolver skeleton with single-segment, cross-module, cyclic-import, and qualified-path resolution.
- `-R` / `--resolve` flag and `--extern-dir` for opt-in semantic checks.
- Function parameters, local `var`s, lambdas, catch variables, and `times` indices are now added to the scope tree.
- Forward declarations via optional bodies in normal and prototype files: `class A;`, `enum E;`, `func f();`, `namespace Ns;`.
- Prototype/extern declaration files (`std/*.decl.myll`) and inline `[extern]` forwarding for classes/namespaces.
- `using namespace` / `using Name` resolution and C++ emission.
- Cross-module namespace merging and ambiguity diagnostics.
- Resolver-to-generator wiring for `TypespecNested` and `ScopedExpr` (resolved `FullyQualifiedName`).
- End-to-end integration test `testing/cases/using_ns/` for merged namespaces and `using`.
