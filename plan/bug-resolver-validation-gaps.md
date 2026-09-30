# Bug: resolver validation gaps

Status: **open**. Found in the 2026-09-29 review. Code-verified.

A collection of places where validation is missing or uses the wrong context. Each item is small;
group them in one pass.

## Finding 1: `switch` and `try` are not validated

`ValidateStmt` (`backend/Resolver/TypeChecker.cs:254-263`):

- `SwitchStmt` validates only the case bodies and the else. The switch **condition** and each
  `cb.compare` expression never go through `ValidateExpression`.
- `TryCatchStmt` is an empty stub with a comment.

So `switch( someChar ) { case 1: ... }` and `try { var int x = "str"; } catch ...` compile without
diagnostics, while `if`/`while`/`for` conditions are checked (via `CheckBooleanOperand`).

Fix: validate `sw.cond` and every compare expression; recurse into `tcs.tryBody` and `cc.body`
(both exist — `ContainsReturnOrThrow` at `TypeChecker.cs:375-377` already walks them).

## Finding 2: `VisitTypespec(null)` returns `auto`

`backend/Visitor/VTypes.cs:58-66` returns an `ExplicitAuto` typespec whenever the `typespec` context
is absent — the commented-out original intent was `Void`. A function-pointer type with an omitted
return (`typespecFunc : funcTypeDef? (RARROW typespec)?`) gets an `auto` return in a position where
C++ performs no deduction — wrong semantics. Same hole for `new` with an omitted typespec
(`backend/Visitor/VExpr.cs:107`).

Fix: return `null` and let each caller decide: `Void` for a function-pointer return, an explicit
diagnostic for `new`.

## Finding 3: `GetUntypedLiteralRank` uses the static default dialect

`backend/Resolver/ConversionRules.cs:438-440` ranks untyped float literals against
`Dialect.Default` instead of the active module dialect. For a module dialect with
`FloatKeyword != F32`, a literal targeting the module's default float is ranked `Promotion` instead
of `Exact`, which can flip overload tie-breaks. `TypeResolver.cs:149` and `TypeChecker.cs:1442` both
use the module dialect — this is the inconsistent one.

Fix: thread the dialect (or just `DefaultFloatSize()`) into `GetRank`/`GetUntypedLiteralRank`.

## Finding 4: `self` / `this` have no type outside receiver position

`ResolveCore` (`backend/Resolver/TypeResolver.cs:51-52`) returns `SelfExpr.Type` / `ThisExpr.Type`,
but those are only populated by `TryGetMemberAccessBaseType`. A bare `self` used as a value or call
argument stays null-typed, so any template deduction involving it is deferred every fixed-point pass
and silently delegated to C++ with no Myll-side checking.

Fix: fall back to building the self type from the enclosing structural (reuse the
`NameResolver.BuildSelfType` logic) when the stored type is null.

## Finding 5: local multi-declarations share one Typespec instance

Local `var T a, b, c;` visits the shared `typespec` context exactly once, and every `VarStmt` gets
the **same** `Typespec` instance (`backend/Visitor/VStmt.cs:187,218`). The field-scope visitor
re-visits per accessor (`backend/Visitor/VDecl.cs:1123`) — locals do not. In-place type mutation then
leaks between variables: `var ilist a = [1], b = [2.5];` — `a`'s element-type inference rewrites the
shared `std::initializer_list<T>` argument in place, `b` sees a non-`auto` element and gets a spurious
conversion error (or a silently wrong element type).

Fix: re-visit `c.typedIdAcors().typespec()` per `idAccessor`, exactly like the field-scope visitor.

## Finding 6: chained relational flattening loses source positions

`FlattenRelational.VisitWithAnd` builds the inner comparison `BinOp`s of `a < b < c`
(→ `(a<b) && (b<c)`) without any `srcPos` (`backend/Visitor/VExpr.cs:303-326`); only the outer `And`
gets a location. Type-checker diagnostics on the inner comparisons (for example the char-vs-int rule)
print with no `[file:line:col]` header.

Fix: carry the `IRelEqExprContext` through the descent and stamp each synthetic `BinOp` with its
context span.

## Verification

- One negative case per finding: char switch condition, type error in a try body, function-pointer
  type with omitted return, `self` as a template argument, `var ilist a = [1], b = [2.5];`,
  chained relational with a type error in the middle. Extend `testing/cases/typecheck_fail/main.myll`
  or add dedicated cases.
- Goldens for the findings that change output (2, 5, 6).
- `dotnet test testing/` green.

## Risk

Low to medium. Each fix is local. Finding 5 may shift goldens where shared typespecs had observable
effects. Finding 2 changes what omitted types mean — check every caller of `VisitTypespec(null)`
before flipping it.
