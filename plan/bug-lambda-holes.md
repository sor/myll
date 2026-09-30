# Bug: lambda holes — captures ignored, transforms skipped, no return inference

Status: **open**. Found in the 2026-09-29 review. Code-verified.

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

1. Reject non-empty capture lists with a diagnostic (stage 1), then implement capture emission
   (stage 2).
2. Recurse both loop transforms into lambda bodies (preferably via the shared walker).
3. Make `IsReturningSomething` real for lambdas so `return`-based inference applies.
4. Keep per-line `SrcPos` through `Lambda.Gen`.

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
