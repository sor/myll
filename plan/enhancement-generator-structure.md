# Enhancement: generator structure — buckets, formats, dead paths

Status: **open**. Found in the 2026-09-29 review.

All items in `backend/Generator/HierachicalGen.cs` unless noted. Behavior-preserving refactor; the
golden suite is the gate.

## Findings and fixes

1. **PPPSourceLines bucket fragility** (`:76-102, :345-359, :393-404`). Twenty-two named buckets plus
   two hand-maintained `.Concat` ladders define output order. A new declaration kind must touch the
   field list *and* both ladders; a misplaced concat silently reorders output.
   → Replace with an ordered section registry (`List<(section, AccessSourceLines)>`) iterated by
   `GenDecl`/`GenImpl`.

2. **Dead `tplImpl` path** (`:727` TODO). The non-inline branch (`:818+`) is only reachable when
   `hasTpl == false`, so `:844-845` never adds a template line. → Delete the dead path.

3. **Dead `PrependInjectedNamespaceUsings`** (`:1195-1206`) — never called (its feeder
   `ContainedNamespaceUsingInjector` is `[Obsolete]`). The `cppOnlyNamespaceUsings` comment
   (`:115-119`) claims these usings are "emitted in the .cpp", but they are not (names are fully
   qualified instead). → Delete or wire up; fix the comment either way. Related stale doc:
   `GlobalUsingNSMode.Contained` is documented "Not yet implemented" in
   `backend/Core/Dialect.cs:281-285` although it is.

4. **Magic-indexed format slots** (`backend/Generator/StmtFormatting.cs:46-54`). `VarFormat[0..]` is
   consumed across three files (`HierachicalGen.cs:622-647`, `backend/Core/Stmt.cs:83-92`,
   `backend/Core/Typespec.cs:248/261/281` uses `VarFormat[5]` as the `typename` fragment). Inserting
   a slot shifts every call site silently. → Named fragment constants (or a small struct) per slot,
   keeping composition via `String.Format`.

5. **`template <...>` header inlined six times** (`:475-478, :611-615, :732-733, :886-887,
   :997-1000, :1322-1325`) although a `TemplateKeyword` constant exists (`StmtFormatting.cs:56`,
   used only in `Typespec.cs:275`). The span-main injection block is also duplicated verbatim
   (`:808-811`, `:848-851`). → One `TemplateHeaderFormat` constant, one span-main helper.

6. **Inline C++ keywords/statements** against the repo convention (format fragments live in
   `*Format` arrays): `extern ` prefix (`:679-695`), `= default` (`:1352-1367`),
   `using Base::name;` (`:1173-1177`), `namespace X {}` stubs (`:325/:333/:342, :236-237/:255/:381`),
   `#include` lines, `int myll_argc, ...` (`:706, :809/:849`), plus if/while/for/switch/return
   formats in `backend/Core/Stmt.cs`. → Move into `StmtFormatting` fragments.

7. **`AddConversionOperator` re-implements the `AddFunc` ladder** (`:858-959` vs `:655-856`):
   template line construction, inlined/external/hasTpl placement, source-map prepends, and the
   "must have a body" error block (repeated 6 times across `:796-800, :819-824, :921-926,
   :938-943, :1381-1386, :1393-1398`). → Extract a shared function-like emitter used by both.

8. **Rename `HierachicalGen` → `HierarchicalGen`** (the class is already spelled correctly; the
   *file* is the typo). Referenced in 3 `.cs` files (`Core/Decl.cs`, `Generator/ModuleGenerator.cs`,
   the file itself) and ~19 markdown files. No code references the path, so the rename is low-risk;
   sweep the docs in the same commit.

9. **`Dialect` property shadows the `Dialect` type name** inside the class (`:122`). → Rename the
   property (e.g. `_dialect`/`ActiveDialect`).

## Verification

- Every step: `dotnet test testing/` green with **unchanged** goldens, `bash
  frontend/coreutil/make_coreutil.sh` clean.
- Grep checks: no `"template <"` literal outside `StmtFormatting`; no raw C++ keyword strings in
  `Gen()` methods; `Hierachical` appears only in git history.

## Risk

Low-medium. Large but mechanical; do it in separate commits per item.

Effort: M.
