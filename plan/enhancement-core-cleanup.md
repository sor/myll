# Enhancement: core cleanup — scope hiding, SrcPos, dead code

Status: **open**. Found in the 2026-09-29 review.

## Finding 1: `ScopeLeaf`/`Scope` member hiding (the sharp edge of `Decl : Stmt`)

`Hierarchical` hides `Decl.scope` (a `ScopeLeaf` field) with `new Scope scope`, and `Scope` hides
`ScopeLeaf.decl` with `new Hierarchical? decl` (`backend/Core/Decl.cs:154-157`,
`backend/Core/Scope.cs:30-33`). Code holding a base-typed reference silently loses the typed member,
and `(base.scope as Scope)!` hides a possible null dereference. This is the sharpest point of the
documented inheritance pain (`docs/analysis/01-architecture.md`).

Fix: collapse `ScopeLeaf` into `Scope` with a nullable `Hierarchical? decl`; make `Decl.scope` a
`Scope` field; kill both `new` members. This is the small, local step toward the long-term
`Decl`/`Stmt`-as-siblings refactor — do not attempt the full split in this task.

## Finding 2: `SrcPos` is a mutable shared reference

`SrcPos` (`backend/Core/Mixed.cs:14-19`) uses mutable public fields and is shared by reference —
one `srcPos` for every var of a multi-declaration (`backend/Visitor/VDecl.cs:1107,1141`). →
Readonly record; prevents accidental shared mutation.

## Finding 3: legacy visitor constructors

The `Stack<Scope>` visitor constructors (`backend/Visitor/VMain.cs:17`, `VExpr.cs:21`,
`VStmt.cs:24`, `VDecl.cs:21`) are unused — only the `ModuleContext` constructors are called. →
Delete.

## Finding 4: dead and stub inventory in Core

- `backend/Core/Symbol.cs` — whole file `[Obsolete]`.
- `backend/Core/Attribute.cs` — all enums "inspirational / not wired".
- `Operand.SpecialId` (`backend/Core/Expr.cs:162`) — referenced only by a TODO.
- `Func.IsReturningSomething => false` (`backend/Core/Decl.cs:199-200`) — forces lambdas to `Void`;
  see `bug-lambda-holes.md` finding 3 (make it real, or delete it once inference is wired).
- `TypespecBasic.Modifier` (`backend/Core/Typespec.cs:115-123`) — `[Obsolete]`, "never used".
- Stale comment: `backend/Core/Stmt.cs:364` claims else-on-loop is unimplemented; it is.

Decision per item: delete now, or keep with a dated note if it is a planned stub. `Symbol.cs` and
`Attribute.cs` are documented future stubs (`docs/analysis/07-future-stubs.md`) — keep, but move the
decision into the housekeeping triage (`enhancement-housekeeping.md`).

## Finding 5: misleading indentation

`IsTemplateUp` (`backend/Core/Decl.cs:37-46`): `return false;` *looks* like it belongs inside the
first-iteration `if` (which would bail on the first non-templated ancestor), but syntactically it
sits after the braceless `for`, so the walk-up is correct. Add braces. Same class:
`VMain.cs:98`, `VStmt.cs:176-179`, `VTypes.cs:152-156`.

## Finding 6: csproj hygiene

- `backend/backend.csproj:15-21` has two identical `NoWarn` property groups (Debug/Release) → one.
- `frontend/frontend.csproj:22-28` duplicates `WarningsAsErrors` per configuration and carries empty
  `<Title></Title>` / `<Copyright></Copyright>` / `<FileVersion></FileVersion>`.
- `AGENTS.md` says backend uses `Nullable=warnings`, but `backend.csproj:10` is `enable` → fix the
  doc (or the csproj, after checking intent).

## Verification

- `dotnet build myll.sln` clean; `dotnet test testing/` green; goldens unchanged.
- Grep: no `new Scope` / `new Hierarchical?` hiding members; no `Stack<Scope>` visitor constructor.

## Risk

Finding 1: medium (every scope access in the compiler). Findings 2-6: low.

Effort: M-L (finding 1 is the L part).
