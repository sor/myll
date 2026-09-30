# QWEN.md — Retired working memory for Myll sessions

This file is being faded out: do not extend it and do not add new session notes
here. `AGENTS.md` is the authoritative instructions file (conventions, language
invariants, build/test), `plan/` holds the design notes and decision logs, and
`git log` plus `plan/TODO.md` document recent work. The notes below are kept
only until they migrate to `docs/` or `plan/`; they may be stale.

## Pipeline (how a compilation flows)

`frontend/Program.cs`: parse → `CompilerPipeline.CompileModules` (per-module
AST, dialect loading) → `ResolveModules` (name/type resolution) →
`SetMainModule` → `GenerateFiles` → optional `-c`/`-cr` C++ compile.

`NameResolver.Resolve(modules)` in `backend/Resolver/Resolver.cs` does:

1. pre-resolution transforms (`ContainedNamespaceUsingInjector` is intentionally
   disabled)
2. `BuildModuleExports` → per-module `ModuleExports` (`name → List<Decl>`,
   namespaces flattened in, plus `namespacesByFqn`)
3. fixed-point loop: `ResolveUsings` / `ResolveAliases` / `ResolveIds` /
   `ResolveScopeds` / `ResolveTypes` / `ResolveMemberAccesses`, then
   `result.Apply()`, `RangeForAutoTypeTransformer`, `ResolveCalls`
4. post: `Apply`, `UpdateDependentNestedNames`, `ValidateGlobalUsings`,
   `ValidateModuleDecls` (module-scope redefinition / return-type / cross-kind
   name collision check), post transforms, `RejectRequiresClauses`,
   `ValidateTypes` (runs `TypeChecker`), `ResolveSyntheticInitListTypes`,
   `ReportUnresolved`

Resolution results live in `ResolutionResult` (`resolvedDecl` maps + invalid
markers); `Apply()` commits them onto AST nodes (`resolvedDecl`, `Type`).

Key lookup machinery in Resolver.cs:

- `CollectExports`: walks `scope.children`, skips `IsHidden`, recurses only
  into `Namespace`.
- `LookupNameCandidates`: enclosing-scope walk + `importedScopes` +
  `importedNames` + per-imported-module export lookup filtered by active
  namespaces.
- `PickSingleCandidate`: merges same-FQN namespaces; call sites prefer
  `Structor`; multiple `Func` → null (overload resolution handles it later).
- `ResolvePath`: segment-by-segment qualified-name resolution.
- `TryResolveOverload`: arity filter → template viability/deduction →
  same-signature candidate merge (a decl + its def collapse to one, definition
  preferred) → conversion ranking (`ConversionRules.GetRank`) → tie-break
  (non-template preferred) → `viable.Count > 1` = ambiguous, `0` = no match.

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
- The harness shells out to **`frontend/bin/<Config>/net10.0/myll.dll`** (an
  absolute path computed in `IntegrationTests.cs`), not the backend lib
  directly. `dotnet test testing/` rebuilds the referenced projects, but if you
  hand-ran `dotnet run` in a different config, rebuild `myll.sln` in the
  matching config first so the invoked `myll.dll` is current.
- `caseconfig.json` keys: `myllCompileRun` (uses `-Ccr`), `myllExtraFlags`,
  `fixedOutput`, `timeouts`, and `depth` with `generateFailing` / `generate` /
  `compile` / `link` / `run` lists. Default depths = Run (generate → C++ compile
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

- **Compile-time vs link-time.** A redefined *class/struct/enum* fails at
  compile time in any TU that includes both headers. A same-signature
  *function* definition in two TUs compiles fine per-TU and only fails at link
  time (multiple definition). Both surface in Myll as a redefinition error.
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