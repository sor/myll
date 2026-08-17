# Open TODOs

This file consolidates the remaining work items that came out of the visitor audit, the Unix-app porting work, and the `AGENTS.md` planned list.

## High priority

1. **Fix broken AST traversal for loops**
   - `ForStmt`, `WhileStmt`, `DoWhileStmt`, and `TimesStmt` have broken `EnumerateDF` that skips loop bodies.
   - See `backend/Core/Stmt.cs` and `docs/analysis/03-ast-core.md`.

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
   - Decide and implement per-declaration access modifiers (`[pub]`, `[priv]`) or reject/warn on them.
   - Emit a clear error when `alias` is used on a namespace until namespace aliases are supported.

## Medium priority

5. **Language syntax gaps**
   - Direct constructor syntax (`T name(args);`, `T(args);`) instead of only `var T name = T(args);`.
   - Range-based `for` loops (`for( a : b )`).
   - Namespace aliases via `alias` (`alias fs = std::filesystem;` should generate `namespace fs = std::filesystem;`).
   - Auto-property / combined private-field + public-getter syntax. Lexer tokens `get`/`set`/`refget` exist and old tests show `field int b { get => _b; set _b = value; };`, but the parser does not currently support property blocks. Consider a shorthand such as `prop u64 size => _size;` that lowers to a `[pure]` getter method. Later, semantic analysis should allow property-style access without parentheses (`obj.size` resolves to the getter call).

6. **Cleanup**
   - Remove `oldParser/` and `oldCodeGen/` once nothing references them.
   - Triage the 134 TODO/FIXME/HACK markers across backend/frontend/testing.
   - Clean up 43 `= null!` initializers by introducing real constructors or nullable annotations.

7. **Partial/broken generators**
   - Complete `VisitScopedExpr` (`backend/Visitor/VExpr.cs`).
   - Review/replace the lambda hack in `VisitLambdaExpr`.
   - Validate the switch/case generator paths (`caseBlock`, `defaultBlock`, `StmtSwitch`).

## Lower priority / design decisions

8. **Imports**
   - Support subdirectory paths in import names or add a string-form import (`import "some/lib/header.hpp";`).

9. **CI/CD**
   - Set up a CI workflow for automated builds and tests.
   - Decide whether to commit the generated ANTLR files or always regenerate during build.

10. **Open language design questions**
    - Should `return _` mean "return default"?
    - Should `switch` support `else:` as an alias for `default:`?
    - Should `(copy)(expr)` be allowed on prvalues?
    - **Switch-case fallthrough dialect**: allow the user/program to select whether cases implicitly `break`, implicitly fall through, or require an explicit `break`/`fallthrough` annotation per case. The mode is modeled in `backend/Core/Dialect.cs` (`SwitchFallthroughMode`) but is not wired through the parser/generator yet.
    - **Rule-of-N class enforcement**: allow `[rule_of=0|3|5]` on a class to require the corresponding C++ special member functions. A default is modeled as a `[Flags]` enum in `backend/Core/Dialect.cs` (`RuleOf`) and should be checked during/after class declaration analysis. The dialect mask can list multiple allowed rules; a class passes if it adheres to any one of them.

## Recently completed

- Visitor audit (`plan/visitor-audit.md`) and fail-loudly overrides.
- `continue`, dotted imports, `try/catch` codegen, `\0` char literal fix.
- `var` parameter error message.
- ProcessRunner thread-safety fix.
- `ls` vertical slice with classes, inheritance, smart-pointer shorthand, sorting, and hidden files.
