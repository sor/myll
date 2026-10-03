# Enhancement: shared transform/validate walker

Status: **in progress**. The walker exists (`backend/Transform/DeclWalker.cs`, 2026-10-03);
`ElseOnLoopTransformer` and `BreakContinueTransformer` are rewired (this also fixed
`bug-lambda-holes.md` finding 2). The AST gained canonical enumerations:
`Stmt.ReplaceChildStmts`/`Stmt.VisitChildExprs` and `Expr.VisitSubExprs`. Rewiring the other
nine is the immediate follow-up, one commit per transform.

## Problem

Eleven near-identical `TransformDecl`/`ValidateDecl` walkers repeat the same
`Func → body / Structor → body / Hierarchical → recurse` switch plus per-statement child
enumeration:

- `AutoReturnTransformer.cs:34-50`
- `ChainTransformer.cs:39-51`
- `DiscardTransformer.cs:31-49`
- `ElseOnLoopTransformer.cs:22-40`
- `BreakContinueTransformer.cs:42-63`
- `ComparisonModeTransformer.cs:55-67`
- `RuleOfTransformer.cs:27-36`
- `SpecialMemberTransformer.cs:19-30`
- `TemplateParamTransformer.cs:22-37`
- `ConfiguredAliasShadowingTransformer.cs:55-67`
- `RangeForAutoTypeTransformer.cs:27-45`

The per-statement child lists also diverge (compare `RangeForAutoTypeTransformer.ChildStmts`
`:74-90` with `ContainsAutoReturnName` `:212-258`). The root cause is the `Decl : Stmt`
inheritance: "everything that owns a body" is scattered over `Func`, `Structor`, and `Lambda`
(an `Expr`), so each walker hand-rolls the enumeration and they drift. The drift is not theoretical:
`ElseOnLoop` and `BreakContinue` miss lambda bodies entirely
(`bug-lambda-holes.md`, finding 2), and `TryCatch`/`If` handling differs between walkers.

## Fix plan

1. Extend the existing `IBodyOwner` interface (`backend/Core/Decl.cs:235-238`) to `Lambda`.
2. Add a single enumeration on the AST: `Decl.EnumerateBodies()` (all bodies a decl owns, including
   lambda bodies reachable from expressions) and `Stmt.EnumerateChildStmts()` (one canonical
   per-statement child list).
3. Build a small shared walker (e.g. `DeclWalker` in `backend/Transform/`) with per-decl and
   per-stmt hooks, and rewire all eleven transforms/validators to it.
4. Fix the lambda gap in `ElseOnLoopTransformer` and `BreakContinueTransformer` as part of the
   rewire (one line each once the walker exists).

Expected outcome: ~400 lines removed; new statement/decl kinds are covered automatically; the
lambda-gap bug class is closed for future transforms.

## Verification

- `dotnet test testing/` green with **unchanged** goldens for all existing transforms (behavior must
  be identical — this is a refactor gate).
- New coverage: else-on-loop and `break N` inside lambda bodies produce the lowered C++ (goldens;
  see `bug-lambda-holes.md` for the cases).
- Grep check: no transform contains a hand-rolled `case Func:` / `case Structor:` /
  `case Hierarchical:` ladder anymore.

## Risk

Medium — large surface, but the golden suite makes behavioral drift visible. Do it in one commit per
transform (rewire + verify) to keep bisecting cheap.

Effort: M-L.
