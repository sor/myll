# Enhancement: restore the thin-frontend-driver contract

Status: **open**. Found in the 2026-09-29 review.

`frontend/Program.cs` is supposed to be a thin CLI/output driver (options, file discovery, output
I/O, optional C++ compile/run). It still owns more than that.

## Findings and fixes

1. **Main-module selection** (`SetMainModule`, `frontend/Program.cs:183-223`) is a semantic property
   of the module graph. → Move into `backend/CompilerPipeline.cs`.

2. **C++ build/run orchestration** (`Program.cs:317-366`, including `Process` handling and a
   non-disposed `process2`). → Move into a `CppCompiler` driver (the file already exists for flag
   translation; see `bug-frontend-cli.md` for the flag bugs that move with it).

3. **Repo-root discovery duplicated in 4 places**, all "4 levels up from
   `AppContext.BaseDirectory`": `frontend/Program.cs:66-70`,
   `testing/IntegrationTests.cs:17-18,28-35`, `testing/CaseConfig.cs:30-33`,
   `testing/CoreutilAppsTests.cs:16-17,64-71`. Any layout change (installed binary, custom output
   path) silently disables `std/`/`lib/`/`dialect/` auto-discovery. → One shared helper, optionally
   overridable via an env var (e.g. `MYLL_REPO_ROOT`).

4. **Regex import scanning** (`ExtractImportedModules`, `Program.cs:140-181`) re-implements the
   grammar's `import` rule with `^\s*import\s+([^;]+);`, ignores block comments, and does no
   transitive closure. The parser already extracts imports in `VDecl.VisitProgs`
   (`backend/Visitor/VDecl.cs:82-115`). → Replace the text scan with a cheap pre-parse or a
   manifest in `myll/`.

5. **Two "module name from prog" implementations**: `ClassifyModule`
   (`backend/CompilerPipeline.cs:67-72`, fallback `Path.GetFileNameWithoutExtension`) vs
   `ProbeModule` (`backend/Visitor/VDecl.cs:27-32`, fallback `.Replace(".myll","")`).
   `ProbeModule` is dead code (only referenced by old docs). → Delete `ProbeModule`.

6. **Double parse with leaking diagnostics** (`backend/CompilerPipeline.cs:48-64`): every file is
   first parsed with `PredictionMode.LL_EXACT_AMBIG_DETECTION` (expensive), and on any exception the
   token stream is reset and the file re-parsed in plain `LL` mode. Diagnostics from the first pass
   remain in the shared list, so edge cases can report the same error twice. → Clear `diagnostics`
   before the fallback parse; make exact-ambiguity detection opt-in.

7. **Dead state**: `selectedPos` is written but never read (`CompilerPipeline.cs:115,126`). →
   Delete.

## Verification

- `dotnet test testing/` green; `bash frontend/coreutil/make_coreutil.sh` clean.
- `--main` selection behavior unchanged (existing cases cover the single-main auto-detect; add a
  quick manual check of the multi-main + `-M` path).
- Grep: `AppContext.BaseDirectory` appears once (in the shared helper); no `Environment.Exit` left
  in `Program.cs` for the moved paths.

## Risk

Low-medium. Moves only; the test harness exercises the pipeline end to end.

Effort: M.
