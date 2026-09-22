# Unimplemented features and cleanup TODO — 2026-09-08

This list comes from the static audit in `plan/audit-2026-09-07.md`. Items are grouped by how close they are to being finishable.

## Ready to implement (parsing/AST hooks already exist)

These have grammar, AST nodes, or visitor entry points already; they mainly need generator work and tests.

- **Property / accessor generation**
  - Files: `backend/Core/Mixed.cs:27`, `backend/Core/Decl.cs:255`, `backend/Visitor/VDecl.cs:961`, `backend/Generator/HierachicalGen.cs:88–97`
  - Accessories are parsed but never emitted.

- **Named function arguments**
  - Files: `backend/Core/Mixed.cs:59–64`
  - `Arg.name` exists; `Arg.Gen()` throws `NotImplementedException`.

- **Null-coalescing calls / indexers**
  - Files: `backend/Core/Mixed.cs:79–81`, `backend/Visitor/VExt.cs:131–139`
  - `FuncCall.nullCoal` parsed; generation throws.

- **Pointer-to-member access (`.*` / `->*`)**
  - Files: `backend/Generator/ExprFormatting.cs:16–17`, `backend/Core/Expr.cs:105–109`, `backend/Visitor/VExt.cs:137–139`
  - Operands exist; generator emits `TODO ...` placeholders.

- **Copy cast `(copy)(expr)`**
  - Files: `backend/Visitor/VExpr.cs:124–127`, `backend/Core/Expr.cs:560`
  - Visitor throws; generator format already exists.

- **`for` loop multi-init / `MultiStmt` init**
  - Files: `backend/Core/Stmt.cs:343–350`
  - Throws `NotImplementedException`.

- **Enum inheritance from non-basic types**
  - Files: `backend/Visitor/VDecl.cs:438`
  - Grammar supports a base type; only basic types are handled.

## Medium effort (need design or more wiring)

- **Namespace aliases (`alias fs = std::filesystem;`)** — DONE.
  - `alias` declarations that resolve to a namespace produce `namespace fs = std::filesystem;`.
  - Used in `frontend/coreutil/find.myll` and regression-tested in `testing/cases/synthetic_sugar/alias.myll`.

- **Contained module-scope `using NS`**
  - Files: `backend/Core/Dialect.cs:57–60, 218–220`, `backend/Resolver/Resolver.cs:322–348`
  - Only `Leaky` and `Disabled` modes work.

- **`switch` fallthrough modes**
  - Files: `backend/Core/Dialect.cs:48, 166–171`, `backend/Visitor/VStmt.cs:386–387`
  - `SwitchFallthroughMode` defined; generator always inserts `break`.

- **`DefaultTypeMode` concrete-size flags**
  - Files: `backend/Core/Dialect.cs:98–117`
  - Only `SizeIndeterminate` is honored; `Size8`–`SizeFast` are reserved.

- **Range-based `for` loops (`for (a : b)`)**
  - Files: `backend/Grammar/MyllParser.g4:188`
  - Grammar TODO comment.

- **`[extern]` propagation into class/struct members**
  - `plan/TODO.md` medium-priority item 5.
  - Prototype files exist, but members are not suppressed for inline `[extern]` classes.

- **Auto-property / combined field+getter syntax**
  - `plan/TODO.md` medium-priority item 4.
  - Hook in `Accessor` parsing.

## Visitor-level stubs

These parse-tree nodes exist but the visitor throws:

- Range expressions (`backend/Visitor/VExpr.cs:548–549`)
- Throw expressions (`backend/Visitor/VExpr.cs:551–552`)
- Three-way conditional expressions (`backend/Visitor/VExpr.cs:554–556`)
- `continue case/default/else` (`backend/Visitor/VStmt.cs:293–295`)
- Lambda visitor cleanup (`backend/Visitor/VExpr.cs:382–408`; works but labeled a hack)

## Reserved / intentionally shelved

Do not implement these unless `REASONS.md` is reopened.

- `defer` statement (`backend/Visitor/VStmt.cs:297–298`)
- `aspect` / `concept` (`backend/Visitor/VDecl.cs:427–433`; grammar reserved at `backend/Grammar/MyllParser.g4:46–47, 90–92`)

## Safe cleanup (dead code)

- `backend/Core/Extensions.cs:40–43` — unused `string Curly(this string, bool)` overload.
- `backend/Core/Extensions.cs:85–93` — `Indent2nd` unused.
- `backend/Core/Extensions.cs:188–192` — `ForAll<T>` unused.
- `backend/Resolver/OperatorRules.cs:53–54` — `IsScalar(Typespec?)` unused.
- `backend/Generator/HierachicalGen.cs:88–97, 197, 203, 243, 248, 619` — empty accessor lists and the `AddAccessor` placeholder comment.
- `backend/Core/Typespec.cs:114–122` — `[Obsolete] TypespecBasic.Modifier` enum.
- `backend/Core/Expr.cs:137, 142` — `Operand.OrderOps_Begin` / `OrderOps_End` placeholders.
- `backend/Core/Typespec.cs:293–296` — unused `Pointer.Kind.Map` / `OrderedMap` / `MultiMap` / `OrderedMultiMap`.
- `backend/Visitor/VExt.cs:17–23` — empty `partial class MyllParserBaseVisitor<Result>`.
- `backend/Visitor/VStmt.cs:154` and `backend/Visitor/VDecl.cs:410` — discarded `useMe = VisitTplParams(...)` results.
- `backend/Visitor/VDecl.cs:18, 257–258, 491` — outdated `// HACK: ... ScopeStack` comments.
- `frontend/tests/readme.txt` — references removed `Stmt.EnumerateDF`.
- `oldParser/` and `oldCodeGen/` directories — not built, already classified as legacy.

## Future-design stubs (do not delete without a design decision)

- `backend/Core/Symbol.cs` — intended semantic-symbol table.
- `backend/Core/Attribute.cs` — strongly-typed attribute enums.
- `backend/Core/Typespec.cs:293–296` `Map` pointer kinds — future container pointer syntax.
- `backend/Core/Stmt.cs:335–341` `ForStmt.els` — Python-style `for ... else`.
