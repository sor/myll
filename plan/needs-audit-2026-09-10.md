# Needs Audit

Generated 2026-09-10 after the Transformer/ModuleContext refactor and `Diagnostic` namespace move.
This file is a ranked backlog of reliability gaps, architectural debt, documentation drift, and cleanup.
New features are intentionally out of scope until these are addressed.

## Recently completed

- `backend/Core/Expr.cs` — `Discard.Gen()` no longer throws; it emits a stub C++ expression with a comment so the C++ compiler reports a readable error if a discard escapes lowering.
- `backend/Generator/HierachicalGen.cs` — `[default]` on free functions is rejected with a Myll diagnostic instead of emitting invalid C++.
- `backend/Visitor/VStmt.cs` — switch `case` and `default` blocks now honor `Dialect.SwitchFallthrough` (`ImplicitBreak` / `ImplicitFallthrough` / `Explicit`).
- Added `testing/SwitchFallthroughTests.cs` and `testing/cases/default_free_function_fail/` regression coverage.

## 1. Critical bug risk

The most urgent remaining items are places where user code can crash the compiler or produce invalid C++.

- Compound untyped-integer expressions (e.g. `1024 * 1024 * 1024`) lose their `literalText` in `TypeResolver.CommonArithmeticType`, so `ConversionRules.GetUntypedLiteralRank` cannot verify they fit and rejects them when assigned to `u64`/etc. The intended behavior is that an untyped integer constant fits into any numeric type that can hold it. Workaround: add an explicit cast such as `(u64)1024 * 1024 * 1024`. See `backend/Resolver/TypeResolver.cs` (`CommonArithmeticType`) and `backend/Resolver/ConversionRules.cs` (`GetUntypedLiteralRank`).
- Reachable `NotImplementedException` throws that users can already hit: named arguments, null-coalescing calls, `(copy)` cast, range expressions, throw expressions, three-way conditional, `continue case/default/else`, `defer`.
  These are technically features, but leaving them as unhandled throws is a bug-level risk.

## 2. CI / test gaps

- `bash frontend/coreutil/make_coreutil.sh` is not part of the runtime test entrypoint.
  The container build runs it, but there is no PR gate.
- There is no `.github/workflows` or equivalent CI file.
  Pre-submit relies on manual container builds.

## 3. Architectural dead weight

- `backend/Core/Symbol.cs` is entirely `[Obsolete("not used ATM")]` and unused by the current resolver.
- `backend/Core/Attribute.cs` defines enums that are never referenced; they are now marked `[Obsolete]` as inspirational future attributes.
- `oldParser/` and `oldCodeGen/` are explicitly legacy and not in use.

## 4. Documentation drift — FIXED

- `docs/analysis/08-actionable-fixes.md` now marks the `_ = expr;` discard crash as fixed.
- `docs/analysis/02-grammar-visitors.md` now correctly reports that `try/catch`, `continue`, and `return-if` visitors are implemented; only `defer` is missing.
- `plan/TODO.md` now marks semantic analysis / ScopeStack as done instead of an active "endboss".

## 5. Cleanup

- Pre-`ModuleContext` `// HACK: ... ScopeStack` comments remain in `backend/Visitor/VDecl.cs` at lines 18, 286, 520.
- `backend/Generator/ExprFormatting.cs:16-17` contains literal `"TODO MAP .*"` and `"TODO MPAP ->*"` placeholders.
- 68 TODO / FIXME / HACK markers remain in `backend/**/*.cs`.
- Duplicate `VisitLit` / `VisitLiteralExpr` code in `backend/Visitor/VExpr.cs:451-470` could be consolidated.

## Suggested order

1. Remove/update stale HACK comments and TODOs in `VDecl.cs` and `Stmt.cs`.
2. Clean up stale docs.
3. Decide whether to delete `Symbol.cs`, `Attribute.cs`, `oldParser/`, and `oldCodeGen/`.
4. Address remaining reachable `NotImplementedException` throws (start with the ones most likely to be hit).
5. Add a minimal CI workflow that runs `dotnet test testing/` and `bash frontend/coreutil/make_coreutil.sh`.
