# QWEN.md — Working memory for Myll sessions

Short overview so a new session does not need to re-read the whole repo.
`AGENTS.md` is the authoritative instructions file — read it first. This file is
the cheat sheet: where things live, how the pieces fit, and what was in flight
the last time we worked.

## What the project is

Myll is an experimental language that compiles to C++23 (saner syntax, same
semantics/ABI). C#/.NET 10 codebase, ANTLR4 grammar. Single author. Early beta.

- `backend/` — compiler library: `Grammar/` (ANTLR .g4 + generated C#), `Core/`
  (AST: `Decl.cs`, `Expr.cs`, `Typespec.cs`, `Scope.cs`, `Mixed.cs`, `Node.cs`),
  `Visitor/` (VMain/VDecl/VExpr/VStmt/VTypes build the AST), `Resolver/`
  (name+type resolution, overload resolution, diagnostics), `Generator/`
  (HierachicalGen.cs emits C++), `Transform/` (pre/post-resolution AST transforms).
- `frontend/` — thin CLI driver (`Program.cs`), `CommandLineOptions.cs`,
  `CppCompiler.cs`, `coreutil/` (sample apps in Myll built via
  `make_coreutil.sh`).
- `testing/` — xUnit integration harness (`IntegrationTests.cs`, `CaseConfig.cs`,
  `GoldenFileComparer.cs`), `cases/` (ported .myll tests), `golden/` (expected
  generated C++), `caseconfig.json` (depths/flags per case).
- `std/` + `lib/` — external C++ bindings as `.decl.myll` prototype modules
  (auto-discovered, imported implicitly via `StdBuiltins.ImplicitStdImports`).
- `dialect/` — JSONC dialect files; `dialect/myll.jsonc` is the default base.
- `plan/` + `docs/` — design docs, TODO list (`plan/TODO.md`), decision log
  (`REASONS.md`).
- `tmp/` — scratch/repro files (use this, not /tmp).

## Commands

```bash
dotnet build myll.sln                     # build (regenerates ANTLR C# automatically)
dotnet test testing/                      # integration tests (134 passing as of 2026-09)
dotnet test testing/ --filter "DisplayName~<caseName>"   # one integration case
dotnet test testing/ --filter "FullyQualifiedName~ResolverTests"   # unit tests only
bash frontend/coreutil/make_coreutil.sh clean && bash frontend/coreutil/make_coreutil.sh   # C++ gate (~30s)
dotnet run --project frontend -- -i <files>.myll -o outdir -Ccr   # manual compile
```

Verify after any backend change: build + test + coreutil. Tests use
`MYLL_TEST_TEMP=1` (isolated temp dirs under `testing/generated/`).

- The integration harness shells out to **`frontend/bin/<Config>/net10.0/myll.dll`**
  (an absolute path computed in `IntegrationTests.cs`), not the backend lib directly.
  `dotnet test testing/` rebuilds the referenced projects, but if you hand-ran
  `dotnet run` in a different config, rebuild `myll.sln` in the matching config
  first so the invoked `myll.dll` is current.

## Hard rules (from AGENTS.md, repeated because they bite)

- No C# interpolated strings — `String.Format` or concatenation.
- Tabs for indentation/continuation, spaces for alignment; lines ~200 chars max.
- No literal C++ keywords in `Gen()` — use the `*Format` arrays in
  `backend/Generator/StmtFormatting.cs` etc.
- No static stateful classes; compiler state lives in `ModuleContext` /
  per-invocation pipeline objects.
- Do not run auto-formatters or reformat files. Do not commit unless explicitly
  asked. No `git push` ever without explicit "push".
- Public fields for new AST/state data; properties for computed/derived values.
- Chained ternaries (flat if/else-if ladder style) for single-value decisions.

## Pipeline (how a compilation flows)

`frontend/Program.cs`: parse → `CompilerPipeline.CompileModules` (per-module
AST, dialect loading) → `ResolveModules` (name/type resolution) →
`SetMainModule` → `GenerateFiles` → optional `-c`/`-cr` C++ compile.

`NameResolver.Resolve(modules)` in `backend/Resolver/Resolver.cs` (static entry,
~line 78) does:

1. pre-resolution transforms (list at ~line 83; `ContainedNamespaceUsingInjector`
   is intentionally disabled)
2. `BuildModuleExports` (line 178) → per-module `ModuleExports`
   (line 2690: `name → List<Decl>`, namespaces flattened in, plus
   `namespacesByFqn`)
3. fixed-point loop: `ResolveUsings` / `ResolveAliases` / `ResolveIds` /
   `ResolveScopeds` / `ResolveTypes` / `ResolveMemberAccesses`, then
   `result.Apply()`, `RangeForAutoTypeTransformer`, `ResolveCalls`
4. post: `Apply`, `UpdateDependentNestedNames`, `ValidateGlobalUsings`
   (line 490), `ValidateCrossModuleDecls` (line ~538, cross-module ODR
   redefinition check), post transforms, `RejectRequiresClauses`,
   `ValidateTypes` (runs `TypeChecker`), `ResolveSyntheticInitListTypes`,
   `ReportUnresolved`

Resolution results live in `ResolutionResult` (`resolvedDecl` maps + invalid
markers); `Apply()` commits them onto AST nodes (`resolvedDecl`, `Type`).

Key lookup machinery in Resolver.cs:

- `CollectExports` (191): walks `scope.children`, skips `IsHidden`, recurses
  only into `Namespace`.
- `LookupNameCandidates` (2179): enclosing-scope walk + `importedScopes` +
  `importedNames` + per-imported-module export lookup filtered by active
  namespaces.
- `PickSingleCandidate` (2356): merges same-FQN namespaces; call sites prefer
  `Structor`; multiple `Func` → null (overload resolution handles it later).
- `ResolvePath` (1504): segment-by-segment qualified-name resolution.
- `TryResolveOverload` (~888): arity filter → template viability/deduction →
  **same-signature candidate merge** (a decl + its def collapse to one,
  definition preferred) → conversion ranking (`ConversionRules.GetRank`) →
  tie-break (non-template preferred) → `viable.Count > 1` = ambiguous,
  `0` = no match.

## AST / resolver facts worth knowing

- `Decl` (Core/Decl.cs:30): `name`, `access`, `scope` (ScopeLeaf),
  `IsForwardDeclaration` (set in VDecl.cs for semicolon-body funcs line 1053,
  bodyless struct/enum 546/593, forward namespace 365), `IsExternal` =
  `IsExternNamespace || HasAttrib("extern")`, `IsLocal` (params/locals),
  `FullyQualifiedName` (BuildQualifiedName line 90, walks scope tree).
- Kinds: `Func` (Function/Procedure/Method/Operator/Convert), `Structor`
  (Constructor/Destructor), `VarDecl` (Var/Field/Const/Let), `Structural`
  (Struct/Class/Union), `Enumeration`, `Namespace`/`GlobalNamespace`,
  `AliasDecl`, `UsingDecl`, `TplParamDecl` (never emitted), `MultiDecl`
  (wrapper, never in the scope tree — `VMain.AddChildren` adds each leaf).
- Attribute helpers on `AttributedNode` (Core/Node.cs:36): `IsStatic`,
  `IsCompileTime` ("ct"), `IsHidden` ("hide"/"hidden"), `IsExtern`, `IsInline`,
  `IsConst`, `IsAbstract`, `IsDefault`, `IsDisabled` ("delete"/"disallow"),
  `IsNodiscard`, `IsNoexcept`, `IsPure`.
- `Scope` (Core/Scope.cs): `children` is `Dictionary<string, List<ScopeLeaf>>`
  (a name may map to several leaves = in-module overloads); `importedScopes`,
  `importedNames` (single-name usings); `UpToGlobal`/`UpToNamespace`.
- Generator (HierachicalGen.cs): `AddVar` (497) — module-scope `[hide]` vars
  emit as `static` in the .cpp only (internal linkage, duplicates legal);
  `[ct]` → constexpr (no symbol); const-typed → not extern; `[extern]` → decl
  only. `AddFunc` (655) — `IsInlined = IsInline || IsTemplateUp`;
  forward decls emit decl only.
- Prototype modules (`IsPrototypeFile`, the `std/*.decl.myll` set) generate no
  C++ of their own — the generator emits their raw `import <header>;` includes
  instead. Declarations there are *not* real definitions for ODR purposes.
- `StdBuiltins.ImplicitStdImports`: c_math, c_stddef, c_stdint, c_stdlib,
  std_initializer_list, std_memory, std_string, std_type_traits, std_utility —
  imported by every module.
- Dialect: per-module `Dialect` record (Core/Dialect.cs), loaded from
  `dialect/<name>.jsonc` via `DialectLoader` (Core/DialectLoader.cs) when a
  module says `dialect <name>;` (grammar: `dialectDecl`). `Dialect.Default`
  is the shared default instance.
- Diagnostics: `record Diagnostic(SrcPos?, DiagnosticKind {Error,Warning,Note},
  string)`; `DiagnosticFormatter` prints `[file:line:col] error: msg` + a
  source excerpt with `^` underline.

## Test harness mechanics

- `IntegrationTests.RunCase` auto-discovers every `testing/cases/<name>/` dir.
- `caseconfig.json` keys: `myllCompileRun` (uses `-Ccr`), `myllExtraFlags`,
  `fixedOutput`, `timeouts`, and `depth` with `generateFailing` / `generate` /
  `compile` / `link` / `run` lists. Default depth = Run (generate → C++ compile
  → link → run).
- `generateFailing` cases: Myll must exit non-zero; `// FAIL` (optionally
  `// FAIL 2 : substring`) markers in the .myll source are checked against the
  diagnostics with exact filename+line (see `AssertFailingDiagnostics`).
- Golden comparison is byte-exact per file; unexpected generated files fail the
  case. Goldens hold only Myll-generated .cpp/.hpp — support files
  (helper.hpp etc.) are copied from the case dir AFTER comparison.
- The generated dir is deleted/recreated per run.
- **How to make a golden:** run the compiler into a throwaway dir with `-C`
  (`dotnet run --project frontend -- -i testing/cases/<case>/*.myll -o
  tmp/<x> -C`), eyeball the `.cpp`/`.hpp`, then `cp tmp/<x>/*.cpp tmp/<x>/*.hpp
  testing/golden/<case>/`. The harness deletes stale artifacts (`.o`, `test.out`)
  before comparing, so a stray binary in the golden dir is harmless, but don't
  add one.
- **`// FAIL` marker must be on the exact line+file the diagnostic lands on.**
  The diagnostic `SrcPos` is the *declaration start* (e.g. the `struct X` /
  `func f()` line), not the closing brace. Verify with a manual run first.
- **`run`-depth cases: `func main()` must `return 0`.** The harness compiles,
  links, and executes the binary; any non-zero exit fails the case. Write the
  body to assert and return 0 on success, non-zero on the first failed check.
  (An earlier draft returned the computed value and failed because it was 3.)

## Practical lessons / pitfalls (learned 2026-09)

- **Forward declarations are restricted.** `func f() -> T;` (semicolon body)
  at top level of a *real* module is a hard error from
  `VDecl.ValidateForwardDeclaration` (VDecl.cs:173): "Forward declarations are
  only allowed in .decl.myll/.d.myll files, [extern] contexts, or OOP special
  members." Allowed contexts: prototype files (`IsPrototypeFile`), a decl with
  `IsExternal` (`[extern]`), a decl inside an `[extern]` hierarchical, or a
  `[abstract]`/`[default]`/`[delete]` OOP special member. So to model a
  header/source split across modules in a test, declare it in an
  `[extern] namespace N { func f() -> T; }` and define `f` in another module.
  This pattern is intentional, not a workaround: it models shipping a library's
  signatures separately from its implementation (signature-only distribution),
  and Myll must accept a same-signature decl+def pair as one function.
- **ODR vs ambiguity.** Two same-signature *definitions* from two imported
  modules is a C++ **redefinition/ODR** error, NOT an overload ambiguity.
  Different signatures from different modules are legal cross-module overloads
  (namespace scope merges across TUs). Decl+def (one declaration, one
  definition) is the normal legal split. The resolver's
  `TryResolveOverload` previously misread decl+def (two same-signature
  candidates) as "ambiguous"; same-signature candidates are now merged before
  ranking.
- **Compile-time vs link-time.** A redefined *class/struct/enum* fails at
  compile time in any TU that includes both headers. A same-signature
  *function* definition in two TUs compiles fine per-TU and only fails at link
  time (multiple definition). Both surface in Myll as the same
  "Redefinition of 'X'" error. (Verified with g++ on `tmp/repro_options_clash/`.)
- **Cross-module identity:** AST node instances are per-module, so the same
  fully-qualified name in two modules is *two different* `Decl` objects
  (reference-inequal). Compare by generated name/signature text, not
  reference equality, when matching across modules. Within one module,
  reference equality is fine.
- **Namespace-scoped = reachable from a C++ namespace.** When deciding what a
  module "exports" to a namespace, walk only through `Namespace` scopes and
  stop at `Structural`/`Enumeration` (class/struct/union/enum *members* are
  reached through the type, not the namespace). `IsNamespaceScoped` in
  `Resolver.cs` captures this.
- **Linkage-affecting attributes** (for ODR reasoning about module-scope vars):
  `[hide]`→`static` (internal, duplicates OK), `[ct]`→`constexpr` (no symbol),
  `[extern]`→decl only, const-typed global→no emitted symbol. Anything else is
  a real external definition.
- **`main` module detection:** `SetMainModule` picks the module with `main()`;
  a test case has exactly one. The generated `main.cpp` for the main module
  emits a real C++ `int main()`; other modules emit `namespace {...}` and their
  free funcs become `Ns::name()`.
- **Diagnostics you add in the resolver** flow into `ResolveResult.Diagnostics`
  and are printed by `PrintDiagnosticsAndBailOnErrors`; a single Error makes the
  frontend exit non-zero (bails), which is what `generateFailing` cases rely on.

## Recent work (committed)

- `844486d`/`e3cc448` — softcoded dialect configuration: `dialect/` JSONC
  files, `dialect <name>;` statement, `DialectLoader` (thread-safe,
  missing/unknown-key warnings), per-module dialect threaded through
  TypeResolver/TypeChecker/Resolver/Pointers/NewExpr.
- `bdb8e2c` — contained module/structural namespace usings
  (`Dialect.Default.GlobalUsingNS = Contained`); fully qualified names, no
  `using namespace` emission.
- `917581e`/`e02f706`/`a7867a5` — golden baselines + harness generated-dir
  cleanup.

## Recent work (completed 2026-09-25, NOT yet committed — user reviewing)

**Cross-module declaration collision detection (C++-exact).** Implemented and
fully green (build clean, 134/134 tests, coreutil 0 warnings). User asked to
review before committing — do NOT commit. Repro that motivated it:
`tmp/repro_options_clash/`.

What was added:
- `backend/Resolver/Resolver.cs` — `ValidateCrossModuleDecls` (called right
  after `ValidateGlobalUsings` in `Resolve`). For each real module it builds a
  visible-module closure (transitive named imports + implicit std imports),
  keys top-level (namespace-scoped) declarations by (namespace-FQN, name), and
  reports a C++-style redefinition when two different modules both define a
  colliding symbol that some module can see from one TU. Matrix: different
  kinds → error; class/enum/alias → error; var → error unless link-safe
  ([hide]/[ct]/[extern]/const); func → error only when both are real
  definitions with equal signatures (decl+def and different-signature
  overloads stay legal). Error on the later decl + note on the earlier, naming
  the other module. Prototype modules skipped.
- Companion fix in `TryResolveOverload`: same-signature candidates (a decl +
  its def) are merged before ranking so they are not misread as ambiguous
  (`IsSameSignatureCandidate` / `StructorSignaturesEqual` / `PrefersCandidate`
  / `IsDefinition`).
- Tests: `testing/cases/cross_module_clash_fail/` (negative, in
  `caseconfig.json` `generateFailing`), `testing/cases/cross_module_overload/`
  and `testing/cases/cross_module_decldef/` (positive, goldens added). Note:
  plain top-level `func foo();` forward decls are rejected by
  `ValidateForwardDeclaration`, so the decl/def case uses `[extern] namespace`
  for the declaration. That is the intended pattern, not a test hack: Myll
  needs forward decls only for external libraries (the `.decl.myll` bindings
  in `std/`/`lib/`), and a decl+def split across real modules models
  shipping a library's signatures separately from its implementation.
  `ResolverTests.AmbiguousImport_ProducesDiagnostic` was
  retargeted to the new redefinition diagnostic (same-signature imports are an
  ODR redefinition, not an ambiguity).
- `plan/TODO.md` item 1 marked DONE.

If asked to commit: small focused messages (impl, then tests, then docs),
no push.
