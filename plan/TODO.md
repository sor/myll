# Open TODOs

This file consolidates the remaining work items that came out of the visitor audit, the Unix-app porting work, and the `AGENTS.md` planned list.
The endboss architecture is described in `plan/semantic-analysis.md`.

## Endboss — semantic analysis / ScopeStack

This is the overarching long-term goal that blocks many smaller features. Finish the scope-stack based semantic-analysis pass so the compiler can resolve names to declarations, type-check expressions, and disambiguate language constructs that currently rely on string matching or syntactic guesswork.

- `backend/Core/Scope.cs` holds the tree (`Scope`, `ScopeLeaf`, `children`, `importedScopes`), but lookups and resolution are largely unused.
- `backend/Visitor/VExt.cs` keeps a static `Stack<Scope>` shared by all visitors, which is a hack and not thread-safe.
- `backend/Visitor/VMain.cs` pushes/pops scopes during visiting, but only builds the skeleton; no resolution happens.
- `backend/Core/Symbol.cs` and `backend/Core/Attribute.cs` are disconnected stubs meant for this phase.

What finishing this would unlock:
- Namespace aliases (`alias fs = std::filesystem;` knowing the RHS is a namespace).
- Per-declaration access modifiers (`[pub] field int x;` resolving against the current class scope).
- Type-driven disambiguation in the generator (e.g. `enum` inheritance, operator synthesis, `convert`).
- Proper overload resolution and semantic errors with source locations.
- Remove the provisional duplicate-name check in `backend/Generator/HierachicalGen.cs`.

See `docs/analysis/01-architecture.md` (static mutable state, `Decl`→`Stmt` inheritance) and `docs/analysis/07-future-stubs.md` for related context.

## High priority

1. **Repair the `try/catch` grammar**
   - Change `catchClause` from `CATCH funcTypeDef? stmt` to `CATCH LPAREN param? RPAREN stmt`.
   - Update `VStmt.VisitStmtTryCatch` and any generator formatting that depends on the catch clause.

2. **Implement reachable missing expression/statement features**
   - Named function arguments (`Arg.Gen` in `backend/Core/Mixed.cs`).
   - Null-coalescing function calls (`FuncCall.Gen` in `backend/Core/Mixed.cs`).
   - Copy-cast (`VisitPreExpr` in `backend/Visitor/VExpr.cs`).
   - Discard expression (`Discard.Gen` in `backend/Core/Expr.cs`).

3. **Improve compiler diagnostics**
   - Replace empty `throw new NotImplementedException()` calls with messages that include the construct and source location.
   - Decide and implement per-declaration access modifiers (`[pub]`, `[priv]`) or reject/warn on them.
   - Emit a clear error when `alias` is used on a namespace until namespace aliases are supported.

## Medium priority

4. **Language syntax gaps**
   - Direct constructor syntax (`T name(args);`, `T(args);`) instead of only `var T name = T(args);`.
   - Range-based `for` loops (`for( a : b )`).
   - Namespace aliases via `alias` (`alias fs = std::filesystem;` should generate `namespace fs = std::filesystem;`).
   - Auto-property / combined private-field + public-getter syntax. Lexer tokens `get`/`set`/`refget` exist and old tests show `field int b { get => _b; set _b = value; };`, but the parser does not currently support property blocks. Consider a shorthand such as `prop u64 size => _size;` that lowers to a `[pure]` getter method. Later, semantic analysis should allow property-style access without parentheses (`obj.size` resolves to the getter call).

5. **Cleanup**
   - Remove `oldParser/` and `oldCodeGen/` once nothing references them.
   - Triage the TODO/FIXME/HACK markers across backend/frontend/testing.
   - Clean up the `= null!` initializers by introducing real constructors or nullable annotations.

6. **Partial/broken generators**
   - Complete `VisitScopedExpr` (`backend/Visitor/VExpr.cs`).
   - Review/replace the lambda hack in `VisitLambdaExpr`.
   - Validate the switch/case generator paths (`caseBlock`, `defaultBlock`, `StmtSwitch`).

## Lower priority / design decisions

7. **Imports**
   - Support subdirectory paths in import names or add a string-form import (`import "some/lib/header.hpp";`).
   - Decide whether imports are module-wide or file-wide.

8. **CI/CD**
   - Set up a CI workflow for automated builds and tests.
   - Decide whether to commit the generated ANTLR files or always regenerate during build.

9. **Open language design questions**
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
