# Enhancement: centralize builtin identifiers, delete dead tokens

Status: **open**. Found in the 2026-09-29 review.

## Problem

Built-in word handling is scattered magic strings:

- `"self"` / `"null"` — `backend/Core/Expr.cs:485-488`
- `"<builtin>"` — `Expr.cs:435,493`, `backend/Core/Typespec.cs:245`, `backend/Resolver/Resolver.cs:36`
- `"std::move"` / `"std::forward"` treated as single-segment ids — `backend/Visitor/VExpr.cs:133,140`
- `"other"` as default ctor/operator parameter name — `backend/Visitor/VDecl.cs:688,932`
- `"unknown_fix_me"` fallback — `backend/Core/Decl.cs:95`

Worse, the `null` mechanism depends on subtle **lexer ordering**: `ID`
(`backend/Grammar/MyllLexer.g4:229`) shadows `NUL` (`:234`) and `BOOL_LIT` (`:235`), so
`null`/`true`/`false` are lexed as **IDs**. That makes the following all dead code:

- the `NUL` and `BOOL_LIT` tokens and the `lit` grammar alternatives that reference them,
- the `t == "true"/"false"` branch in `ResolveLiteral` (`backend/Resolver/TypeResolver.cs:78`),
- the `t == "null"` guard in `Literal.IsFloatLiteral` (`backend/Core/Expr.cs:709`).

The live path is `IdExpr`-based (`TypeResolver.cs:158`, `IdExpr.Gen:487`).

## Fix plan

1. Add a `BuiltinId` constants class (or enum) in `backend/Core/` holding every special name:
   `self`, `null`, `true`, `false`, `other`, `<builtin>`, `std::move`, `std::forward`,
   `unknown_fix_me`, the `myll_tmp_` prefix. Route all special-name checks through it.
2. Delete the dead `NUL`/`BOOL_LIT` tokens and the dead branches listed above.
3. Pin the ID-shadows-NUL ordering with a comment in the lexer (or, as a follow-up, reorder the
   grammar so `null`/`true`/`false` are real tokens — larger change, keep out of this task).

## Verification

- `dotnet test testing/` green; goldens unchanged (`null`, `true`, `false`, `self`, `(move)` casts
  all covered by existing cases).
- Grep: no raw `"null"` / `"self"` / `"<builtin>"` string literals remain outside the constants
  class.
- Grep: `NUL` and `BOOL_LIT` appear nowhere (grammar + generated output excluded).

## Risk

Low-medium. The `null` literal path is hot; the change is mechanical but must keep the
IdExpr-based resolution intact.

Effort: M.
