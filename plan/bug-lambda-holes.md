# Bug: lambda holes — captures ignored, transforms skipped, no return inference

Status: **mostly fixed 2026-10-03**. Regression case: `testing/cases/lambda_basics/`;
negative cases: `testing/cases/lambda_capture_fail/`, `testing/cases/lambda_method_fail/`.
Still open: real capture lists (finding 1 stage 2), per-line source maps (finding 4),
callable-type conversions.

## Current state (verified 2026-09-30)

Lambdas work **partially**: the lambda expression itself emits correctly (`[&]` capture specifier
plus body, correct even for `[&]`-by-reference locals). But binding one via `var auto` is broken —
`var auto add = func( int a, int b ) -> int { … };` deduces **the return type** (`int add = [&](…)`
→ clang: no conversion from lambda to int), and an unannotated lambda deduces **void**
(`void twice = …`). Lambdas currently only work in positions where a typed callable parameter
provides the target type.

## Finding 1: capture list is parsed and then ignored

The grammar parses a capture list (`capture : LBRACK args? RBRACK`,
`backend/Grammar/MyllParser.g4:349`), but `VisitLambdaExpr`
(`backend/Visitor/VExpr.cs:383-409`) never reads `c.capture()`, and `Lambda.Gen()`
(`backend/Core/Expr.cs:391-415`) hard-codes `[&]` with a `// TODO capture and template` comment.
`func [x, y]( int a ) { ... }` therefore compiles silently with reference semantics for `x`/`y` —
information dropped, no diagnostic.

Fix: store the capture list on `Lambda` and emit it. Until then, reject non-empty capture lists with
a diagnostic.

## Finding 2: post-resolution transforms never enter lambda bodies

`ElseOnLoopTransformer` (`backend/Transform/ElseOnLoopTransformer.cs:22-40`) and
`BreakContinueTransformer` (`backend/Transform/BreakContinueTransformer.cs:42-63`) do not recurse
into lambda bodies (only `DiscardTransformer` handles `Lambda`,
`backend/Transform/DiscardTransformer.cs:303`). Consequences:

- `for/while/range-for ... else` inside a lambda is never lowered, and
  `WhileStmt.Gen` / `ForStmt.Gen` / `RangeForStmt.Gen` ignore `els` entirely
  (`backend/Core/Stmt.cs:364,393,412-422`) — the else body is **silently dropped**.
- `break N` / `continue N` (N > 1) inside a lambda survive untransformed; `BreakStmt.Gen` emits a
  plain `break;` (`Stmt.cs:160-161`) — loop-exit semantics change silently.

Fix: recurse into `Lambda.func.body` in both transforms. The shared walker from
`enhancement-transform-walker.md` makes this a one-line fix and prevents the same class of bug for
future transforms.

## Finding 3: no return-type inference for lambdas

`Func.IsReturningSomething => false` (`backend/Core/Decl.cs:199-200`) forces unannotated lambdas to
a `Void` return type (`backend/Visitor/VExpr.cs:393-402`), which blocks return-type inference for
lambdas even though the inference machinery exists for ordinary functions.

## Finding 4: lambda bodies lose per-line source positions

`Lambda.Gen` joins the body lines into one string, so in statement source-map mode the whole body
maps to the enclosing statement's line. Related: `bug-source-map-ordering.md`.

## Fix plan

Implemented 2026-10-03:

1. **Kinds and capture defaults**: the lambda grammar uses `kindOfFunc`, so `func`, `proc`, and
   `method` lambdas exist. The default capture specifier is dialect-configurable
   (`DefaultCaptureFunction = "[]"`, `DefaultCaptureProcedure = "[&]"`,
   `DefaultCaptureMethod = "[this]"`, plus `dialect/myll.jsonc` entries) and `Lambda.Gen` emits
   it by kind instead of the hardcoded `[&]`. Explicit capture lists are rejected with a clean
   error for now; real capture emission (including `[&x]`, `[=x]` grammar forms) stays a
   follow-up. Method lambdas require an enclosing non-static member function or constructor
   (`DeclWalker.InMemberContext`, checked by the TypeChecker); `[this]` means members are
   visible but locals and parameters are not captured.
2. **Shared walker**: `DeclWalker` (`backend/Transform/DeclWalker.cs`) with canonical AST
   enumerations (`Stmt.ReplaceChildStmts`/`VisitChildExprs`, `Expr.VisitSubExprs`). Both loop
   transforms run on it, so lambda bodies are lowered like any other body. Lambdas get their own
   breakable-construct stack in `BreakContinueTransformer` (C++ cannot jump out of a lambda), so
   `break N` crossing a lambda boundary is a clean error. Rewiring the other nine walkers is the
   immediate follow-up of `enhancement-transform-walker.md`.
3. **Return-type inference**: unannotated lambdas get `ImplicitAuto` (the hardcoded
   `IsReturningSomething` gate is gone) and `TypeChecker.LambdaReturnInferencer` runs the
   ordinary `InferReturnType` over every lambda body.
4. **`var auto` stays `auto`**: declared-auto locals keep the resolved type for validation but
   emit C++ `auto` (locals and range-for variables), so `var auto f = func(...) {...};` binds the
   closure itself. The concrete emission path is kept (commented out in `VarStmt.Gen`) for a
   future `[resolve]` attribute; `new`-initialized autos and module-scope autos still materialize
   (C++ would deduce a raw pointer from `new`, and the header needs a concrete `extern`).
   Module-scope `var auto` with a lambda initializer is rejected.
5. **Per-line source maps** through `Lambda.Gen` stay deferred (finding 4).

Also fixed on the way: `ElseOnLoopTransformer` never recursed into loop bodies of loops without
an `else`, so a nested loop-with-else inside a plain loop was silently skipped; the walker
recursion covers it now.

## Verification

- Capture list: stage 1 negative case; stage 2 positive case (`[x]` vs `[&]` vs `[&x, y]`) with
  goldens that show the emitted capture specifier.
- Else-on-loop inside a lambda: case with goldens (the else body must appear in the C++).
- `break 2` inside a nested loop in a lambda: goldens.
- Lambda return inference: `func add = (int a, int b) { return a + b; };` infers `int`.
- `dotnet test testing/` green.

## Risk

Medium. Finding 2 changes generated output for code that was silently broken before — goldens will
move, and that is the point. Findings 1/3/4 are additive.
