# Enhancement: housekeeping — stale docs, dead code, TODO triage

Status: **open**. Found in the 2026-09-29 review.

## Finding 1: docs drift

- `AGENTS.md`:
  - Generated ANTLR output is actually in `backend/obj/Debug/net10.0/` (Antlr4BuildTasks default),
    not `backend/Grammar/Generated/Myll/`.
  - Backend `Nullable` is `enable`, not `warnings`.
  - The thesis CLI example compiles `frontend/tests/thesis/main.myll`, whose live code at line 358
    uses `defer` and hits the `NotImplementedException` stub — the documented example cannot run
    clean. Fix the example or mark it.
- `docs/analysis/07-future-stubs.md` is stale in four places: convert functions (implemented —
  `REASONS.md` 2026-09-22), range-based for, variable templates, and non-type template parameters
  (all implemented).
- `backend/Core/Dialect.cs:283` — the doc comment on `GlobalUsingNSMode.Contained` still says
  "Not yet implemented"; it is (`testing/cases/contained_using_ns/`).
- `frontend/Program.cs:276` — stale message "Time elapsed after last ToArray call"; no `ToArray`
  remains.

## Finding 2: dead code

- No-op `c.id()?.Visit()` calls: `backend/Visitor/VDecl.cs:679` and `:1006`.
- Always-true ternary: `backend/Visitor/VTypes.cs:177` (`tc.CHAR() != null ? 1 : 4` inside the
  `CHAR` case; `CODEPOINT` falls through to `throw`).
- `myll.sln` carries a `# Visual Studio 15` header.
- Dead/stub Core files: see `enhancement-core-cleanup.md` finding 4 for the keep/delete decision.

## Finding 3: TODO/FIXME/HACK triage

63 lines in backend+frontend. Compact inventory by file (from the 2026-09-29 review):

- `backend/Generator/HierachicalGen.cs`: 314, 389 (ISourceLines perf), 727 (tplImpl — covered by
  `enhancement-generator-structure.md`), 775 (move inlines to header bottom), 1353 (parameterizable
  ctor counts)
- `backend/Generator/ExprFormatting.cs`: 16, 17 (`.*`/`->*` — covered by
  `bug-compiler-crashes-on-valid-syntax.md`), 32, 62 (`÷` lowering)
- `backend/Generator/StmtFormatting.cs`: 102 (char8_t), 146 (commented block)
- `backend/Core/Decl.cs`: 61 (Symbol), 179 (`Requires` type), 199, 224 (initlist), 331 (unwrap
  MultiDecl), 370
- `backend/Core/Expr.cs`: 137 (relational ops), 393 (lambda capture/template — covered by
  `bug-lambda-holes.md`), 484, 689
- `backend/Core/Extensions.cs`: 65 (bottleneck bench)
- `backend/Core/Symbol.cs`: 35
- `backend/Core/Stmt.cs`: 364 (stale — else-on-loop is implemented), 484
- `backend/Core/Mixed.cs`: 83 (multi-arg indexer)
- `backend/Core/Attribute.cs`: 51 (enum merge)
- `backend/Core/Typespec.cs`: 13, 53, 198 (`*::ctor`), 314
- `backend/Visitor/VExt.cs`: 20, 283 (null-tolerant visits)
- `backend/Visitor/VStmt.cs`: 35 (srcPos propagation), 86 (arrow body in c/dtor), 158, 176 (local
  alias scope)
- `backend/Visitor/VDecl.cs`: 18, 286-287, 581 (`curAccess` HACK — instance state), 324 (covered by
  `enhancement-grammar-cleanup.md`), 352 (namespace merge check), 438, 517/521 (aspect/concept
  stubs), 528 (enum inheritance), 732/830 (ctor initList), 932 (configurable `other` param name)
- `backend/Visitor/VExpr.cs`: 212 (MemPtrExpr untested), 273 (disable `a<b<c` flatten), 385 (lambda
  hack), 462 (literal text), 467 (duplicate VisitLit)
- `backend/Visitor/VTypes.cs`: 33, 44, 45, 50, 51 (HACK native sizes), 114 (smartpointed
  containers), 180 (codepoint)
- `backend/Visitor/VMain.cs`: 50 (CleanBodylessNamespace "unreadable SHIT")

Triage action: for each line, either (a) it is covered by a task file in this review (mark it with a
reference), (b) it is a real gap (add to `plan/TODO.md`), or (c) it is obsolete (delete the comment).

## Verification

- Docs: no build needed; cross-check every claim against the code before committing.
- Dead code: `dotnet build myll.sln` + `dotnet test testing/` green.
- Triage: update `plan/TODO.md` and `AGENTS.md` "Current State & Priorities" counts (108+
  TODO/FIXME/HACK, 14 NotImplementedException) to the re-counted numbers.

## Risk

Low.

Effort: S.
