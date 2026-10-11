# Agent Instructions for Myll

Myll is an experimental programming language that compiles to C++. It aims to provide a saner C++ syntax while preserving C++ semantics and ABI compatibility. This is a living project, continuing from a research prototype. Single author: Jan "SoRDiD" Reitz (jan@sordid.de). Version: 0.0.1, early beta state. License undecided.

## Overview / Architecture

- `backend/` — compiler library. ANTLR grammar, parser, visitor pattern, AST (`Core/`), name/type resolution and validation (`Resolver/`), AST transforms (`Transform/`), and C++ code generator (`Generator/`).
- `frontend/` — CLI executable. Parses `.myll` files, invokes backend, outputs `.h`/`.cpp` files.
- `frontend/tests/` — original `.myll` test cases (enums, game of life, thesis, mixed, etc.). Some are being ported to `testing/cases/`.
- `testing/` — xUnit integration-test harness that compiles and runs the ported `.myll` cases.
- `run/` — Rider run configurations that compile test suites.
- `Documentation/` — doc files and spec examples.
- `oldParser/`, `oldCodeGen/` — legacy, not in use.
- `plan/` — living design and implementation plans.
- `REASONS.md` — decision log for deferred and rejected features.

## Language invariants

- Myll needs no separate declarations. A definition suffices; there is no C-style declaration-then-definition in user code.
- Order in a file or module does not matter. `main` may come first and call functions or use classes defined later in the same module.
- The C++ generator injects the required prototypes and class forward declarations into the top of each module's generated `.hpp`. Every generated `.cpp` includes its own `.hpp`, so the generated C++ is order-independent as well.
- Hand-written forward declarations exist only for external library bindings: the `.decl.myll` files in `std/` and `lib/` (auto-discovered), `[extern]` declarations, and OOP special members (`[abstract]`/`[default]`/`[delete]`). `VDecl.ValidateForwardDeclaration` enforces this.
- Splitting a declaration from its definition is legal, within one module and across modules. It models shipping a library's signatures separately from its implementation (signature-only distribution). A same-signature declaration plus definition is one function: overload resolution merges them and `ValidateModuleDecls` does not report a redefinition. Different signatures are ordinary overloads. See `testing/cases/cross_module_decldef/`, `testing/cases/cross_module_overload/`, and `testing/cases/same_module_decldef/`.
- `ValidateModuleDecls` (`backend/Resolver/Resolver.cs`) rejects redefinitions that C++ would reject, both across imported modules and within one module: duplicate types, duplicate non-link-safe variables, and same-signature function definitions (ODR violations). Functions that differ only in return type are rejected too, because C++ cannot overload on return type. See `testing/cases/cross_module_clash_fail/` and `testing/cases/same_module_redefinition_fail/`.
- Strict Myll rejects cross-kind name sharing: a name may not name both a type (class/struct/union/enum) and a function/variable in the same namespace, even though C++ allows it via elaborated type specifiers. Myll has no elaborated type specifier syntax; rename instead. A dialect option to allow C++-style sharing is planned (`plan/cross-kind-name-sharing.md`).

## Build & Run

- Build: `dotnet build myll.sln`
- Run compiler: `dotnet run --project frontend -- <args>`
- Run tests: `dotnet test testing/`
- Run a single integration case: `dotnet test testing/ --filter "DisplayName~caseName"` (use `dotnet test testing/ --list-tests` to discover case names)
- Targets: net10.0
- C# Language Version: 9.0

### CLI Examples

```bash
# Thesis
dotnet run --project frontend -- -i frontend/tests/thesis/*.myll -o frontend/tests/thesis/generated -cr

# Game of Life
dotnet run --project frontend -- -i testing/cases/gol/*.myll -o testing/cases/gol/generated -cr

# Mixed
dotnet run --project frontend -- -i frontend/tests/mixed/*.myll -o frontend/tests/mixed/generated -cr

# Legacy: basic compilation of test files
dotnet run --project frontend/frontend.csproj -- \
  -i frontend/tests/mixed/testcase.myll frontend/tests/mixed/main.myll \
  -o frontend/tests/mixed/generated

# Legacy: clear output, compile C++, and run
dotnet run --project frontend/frontend.csproj -- \
  -i frontend/tests/mixed/testcase.myll frontend/tests/mixed/main.myll frontend/tests/mixed/stack.myll frontend/tests/mixed/enum.myll \
  -o frontend/tests/mixed/generated \
  -Ccr
```

### CLI Options

| Short | Long | Description |
|-------|------|-------------|
| `-i` | `--in` | Input files (searches `*.myll` deeply by default) |
| `-o` | `--out` | Output directory (default: current dir) |
| `-s` | `--stdout` | Output to stdout instead of files |
| `-d` | `--deep` | Search subdirectories for input files |
| `-k` | `--keep` | Keep going when errors are encountered |
| `-n` | `--nofile` | Do not generate files |
| `-c` | `--compile` | Pass generated `.cpp` files to a C++ compiler |
| `-C` | `--clear` | Clear target directory of old build artifacts |
| `-g` | `--debug` | Generate debug executable via C++ compiler |
| `-O` | `--optimize` | Set C++ compiler optimization level |
| `-r` | `--run` | Run the compiled binary |
| `-M` | `--main` | Specify module containing `main()` |
| | `--source-map` | Emit `#line` directives for both declarations and statements |
| | `--source-map-decl` | Emit `#line` directives at declaration boundaries only |
| | `--source-map-stmt` | Emit `#line` directives at statement boundaries only |

### Environment variables

| Variable | Description |
|----------|-------------|
| `MYLL_CXX` | If set, use this executable as the C++ compiler for the internal `-c` / `-cr` path. When unset, Myll tries `clang++`, `g++`, and `cl` in platform order. |
| `MYLL_TEST_TEMP` | If set, each test case uses an isolated temp directory under `testing/generated/` instead of the fixed case directory. This prevents concurrent test runs from interfering and avoids relying on `/tmp`, which is often mounted `noexec`. |
| `MYLL_TEST` | If set, generated test binaries such as game of life reduce delays so automated test runs complete faster. |
| `MYLL_COLOR` | Overrides color output for diagnostics. `1` forces colors on, `0` forces colors off. When unset, Myll enables colors automatically when stderr is a terminal, unless `NO_COLOR` is set. |
| `NO_COLOR` | If set to any non-empty value, suppresses diagnostic colors, matching the community convention. |

## C++ baseline

- Myll targets **C++23**. The internal `-c` / `-cr` compile path and the coreutil build both pass `-std=c++23` (or `/std:c++23` for MSVC) to the C++ compiler.
- External declarations in `std/` and `lib/` should be modeled against the installed standard-library headers. On the current system the canonical reference is `/usr/include/c++/15/...`; use the highest version directory under `/usr/include/c++/` when checking attributes such as `constexpr`, `const`, `noexcept`, or `[[nodiscard]]`.

## Project Layout

```
myll/
├── backend/          # Compiler library (lexer, parser, AST, resolver, generator)
│   ├── CompilerPipeline.cs    # Orchestration: parse → compile modules → resolve → generate
│   ├── ModuleContext.cs       # Per-module compiler state (module AST, dialect)
│   ├── Core/         # AST nodes, symbols, types, scope
│   ├── Grammar/      # ANTLR4 lexer/parser + generated C# code
│   ├── Visitor/      # ANTLR4 visitors building the AST
│   ├── Resolver/     # Name/type resolution, overload resolution, validation, diagnostics
│   ├── Transform/    # Pre/post-resolution AST transforms
│   └── Generator/    # C++ code generation
├── frontend/         # CLI driver
│   ├── Program.cs            # Pipeline orchestration
│   ├── CommandLineOptions.cs # CLI argument parsing
│   ├── CppCompiler.cs        # Invokes the C++ compiler for the -c / -g / -O / -r path
│   ├── coreutil/             # Sample utility apps built via make_coreutil.sh
│   ├── sdl-asteroids/        # SDL3 sample game in Myll
│   ├── sdl-pong/             # SDL3 sample game in Myll
│   └── tests/                # Original .myll test files (copied to output on build)
├── testing/          # xUnit integration-test harness and ported cases
│   ├── IntegrationTests.cs   # Case auto-discovery, golden compare, // FAIL markers
│   ├── ResolverTests.cs      # Unit tests for the resolver
│   ├── caseconfig.json       # Per-case depths/flags (generateFailing/compile/run/...)
│   ├── cases/                # Ported .myll test sources
│   └── golden/               # Expected generated C++ output
├── docs/             # Living design & analysis documentation
│   ├── design/       # Language design rationale
│   └── analysis/     # Code architecture, gaps, actionable fixes
├── plan/             # Implementation plans and decision logs
├── Documentation/    # Thesis PDF and design notes from thesis era
├── oldParser/        # Earlier ANTLR grammar iteration (reference only)
├── oldCodeGen/       # Earlier code generator (reference only)
├── std/              # C++ standard-library bindings (.decl.myll prototype modules)
├── lib/              # Shared external-library .decl.myll bindings
├── myll/             # Myll's own library bindings (e.g. myll_dyn_array)
├── dialect/          # JSONC dialect definitions; myll.jsonc is the default
├── run/              # Rider IDE run configurations
└── myll.sln          # Solution file
```

Shared external declarations in `lib/` are auto-discovered by the frontend, alongside `std/` and `myll/`.

## ANTLR

- Grammar files: `backend/Grammar/MyllLexer.g4`, `backend/Grammar/MyllParser.g4`
- Generated output: `backend/Grammar/Generated/Myll/` (created automatically during build, not committed)
- Rider ANTLR config: `.idea/.idea.myll/.idea/misc.xml` (committed to git)
- Grammar changes: The `Antlr4BuildTasks` NuGet package regenerates the C# parser/lexer automatically during `dotnet build`. Do not commit the generated files in `backend/Grammar/Generated/`.

## Code Conventions

- **Nullable**: `backend` uses `Nullable=warnings`, `frontend` uses `Nullable=enable`.
- **Naming**: Mixed conventions in legacy code (`camelCase` and `PascalCase` coexist). Follow existing file patterns.
- **No `System.Func` conflicts**: The AST has a `Decl.Func` class — be careful with `using System;` at file scope.
- **No static stateful classes**: Do not introduce `static` classes that hold mutable state or shared mutable collections. Keep compiler state instance-based and scoped to `ModuleContext` or per-invocation pipeline objects. See Known Pitfall 2.
- **Visitor pattern**: `VExpr`, `VDecl`, `VStmt`, `VTypes` walk the ANTLR parse tree → `Core` AST → `Generator` produces C++.
- **Generator formatting**:
  - Do not embed literal C++ keywords (e.g. `"constexpr "` or `"static "`) directly in `Gen()` methods.
  - Put every format fragment in the `*Format` arrays in `backend/Generator/StmtFormatting.cs` (or the matching file for the construct).
  - Compose tokens in the call site and pass them as arguments to `String.Format`.
  - If you add a new keyword slot to a shared format, update every call site.
  - Consider giving unrelated constructs their own dedicated format array so index shifts do not break them.
- **No interpolated strings**: Do not use C# interpolated strings (`$"..."`). Prefer `String.Format` or plain concatenation.
- **Chained ternaries for single-value decisions**: When a variable is initialized from a sequence of mutually exclusive conditions, use a single chained ternary expression formatted as a flat `if / else if / else` ladder rather than multiple `if` statements or a helper function. Align the conditions and results vertically so the decision tree reads top-to-bottom.

  Example:
  ```csharp
  string initDecl = ( initIn & GenerateAt.Decl ) == 0 ? ""
                  : obj.init != null                  ? VarFormat[6] + obj.init.Gen()
                  : obj.IsNoInit || isExtern          ? ""
                  : VarEmptyInitFormat;
  ```
- **Keep simple getters simple**: Use expression-bodied members for trivial computed properties (e.g. `public Scope UpToNamespace => decl is Namespace ? this : parent?.UpToNamespace ?? UpToGlobal;`). Only expand into a block-bodied accessor when side effects or multi-step logic are required.
- **Blank line after braceless exit `if`**: Put an empty line after a simple `if` whose body exits the current block (e.g. `return` or `throw`). For other braceless `if`s the blank line is optional but still encouraged.
- **Fields vs. properties**: Prefer public (read-only) fields for new data-heavy AST and compiler state (`Decl`, `Stmt`, `Expr`, `Typespec`, `Import`, etc.). Keep computed/derived values, immutable configuration objects (`Dialect`), and stabilized public APIs as properties. Do not convert existing properties to fields unless you are already editing the surrounding code.
- **C# source formatting**:
  - Use tabs for indentation and for the continuation of broken-down statements.
  - Use spaces for alignment.
  - This matches the formatter setup: indentation/continuation are tab-based, but `AlignConsecutive*` options align with spaces.
- **Local declaration types**: Prefer an explicit type on the left with target-typed `new()` instead of `var`. Right-align short inline comments when they describe a group of members.
  ```csharp
  List<Diagnostic> diagnostics = new();                         // preferred
  var diagnostics = new List<Diagnostic>();                     // avoid
  
  public CompiledModuleResult Module { get; }                   // read-only: per-module input
  public ResolutionResult? ResolutionResult { get; }             // read-only; null pre-resolution
  public bool IsPrototype => Module.Context.IsPrototypeFile;    // read-only convenience
  ```
- **Myll indirection declarator spacing**: Put spaces on both sides of the reference/pointer block. Examples: `std::istream & in`, `T * ptr`, `const char *[] argv`, `Formatter *! fmt`. Exception: pure array brackets stay tight, e.g. `var int[4] myArray;`. In `new` expressions keep the pointer block tight: `new T*!`, `new T*`.
- **Myll class layout**: Group fields in a `field { ... }` block at the top; fields are private by default. Outdent access-section specifiers (`[pub]:`, `[priv]:`, `[prot]:`) one level so they line up with the class keyword. Keep per-declaration attributes (`[pure]`, `[override]`, `[static]`) indented with their declaration.
- **Myll method bodies**: Use the arrow form for simple expression-bodied getters: `func size() -> u64 => _size;`.
- **Rider links**: When running inside Rider's terminal, file-line references like `backend/Core/Expr.cs:42` are clickable. If using the `jetbrains://rd/navigate/reference?...` URI from the shell, the `line` value is zero-based, so subtract 1 from the 1-based source line.
- **Coreutil sample apps**: Build all utilities in `frontend/coreutil/` with `frontend/coreutil/make_coreutil.sh`. It delegates to the existing `coreutil/Makefile` and supports `clean`, `release`, and `debug` targets.

## Documentation & Code Formatting Rules

- Markdown files (`*.md`): do not insert manual line breaks inside sentences. Break only at full stops (`. `). There is no maximum line length for Markdown files. Let the editor / viewer wrap to window width.
- C# source (`*.cs`) and Myll source (`*.myll`): keep lines around 200 characters. Do not break if it is only a few characters above, but always break before 240 characters.
- Technical docs (plans, specs, ADRs): use relaxed Simplified Technical English style. Keep sentences short. Use active voice. Put one main action per bullet.

## Build & Test

- xUnit integration tests live under `testing/`. Run them with `dotnet test testing/`.
- The default harness includes `CoreutilAppsTests.GenerateCoreutilApps`, which generates C++ for every utility in `frontend/coreutil/`. The full C++ build (`bash frontend/coreutil/make_coreutil.sh`) should run in pre-submit/CI; it takes about 30 seconds from a clean state.
- Manual testing: compile `.myll` files via the CLI and inspect generated `.cpp`/`.h` output. Also pass generated C++ code to a C++ compiler with sane warnings.
- `testing/golden/` contains only Myll-generated `.cpp` and `.hpp` output. Support files such as `helper.hpp` or `inner.hpp` that live in `testing/cases/` are copied into the generated directory at test time after the golden comparison, so they must not be added to the golden baseline.
- `dialect/` contains JSONC dialect definitions. `dialect/myll.jsonc` is the default and a copy-paste base for custom dialects. A module selects a dialect with `dialect <name>;` after the `module` declaration.
- See `docs/analysis/06-test-coverage.md` for test inventory and earlier harness design notes.

## Container / CI Notes

A `Dockerfile` and `.dockerignore` are provided at the repository root. They target `mcr.microsoft.com/dotnet/sdk:10.0` and install the latest `g++-14`, `clang-20`, and `antlr4` from Ubuntu 24.04 for build/test runs.

- Build image: `podman build -t myll .`
- Run tests: `podman run --rm myll`
- The container currently runs `dotnet test testing/`; for a full pre-submit gate it should also run `bash frontend/coreutil/make_coreutil.sh`.

Tests run with `MYLL_TEST_TEMP=1` so each test case uses an isolated temp directory under `testing/generated/`. This prevents concurrent runs from interfering and avoids relying on `/tmp`, which is often mounted `noexec`.

### Podman storage and temp space

If `/opt/podman` exists, rootless podman storage and build temp files are redirected there to avoid filling `/home` and `/var/tmp`:

- `~/.config/containers/storage.conf` sets `graphroot = "/opt/podman/storage"` and `runroot = "/opt/podman/run"`.
- The interactive shell alias routes podman build temp files to `/opt/podman/tmp`:

```bash
if [ -d /opt/podman ]; then
    alias podman='TMPDIR=/opt/podman/tmp podman'
fi
```

For non-interactive use — scripts, Makefiles, or agents running shell commands — the alias is not effective. Use the explicit form or set `TMPDIR` in the environment:

```bash
TMPDIR=/opt/podman/tmp podman build -t myll .
```

### Cleaning up dangling images

Each `podman build` keeps the previous image as an untagged `<none>:<none>` image. They share most layers with the current image, so they usually do not consume much extra disk space, but they can clutter `podman images` output.

To remove dangling images:

```bash
TMPDIR=/opt/podman/tmp podman image prune
```

Run this at the end of the day or after a lot of builds to keep Podman storage tidy.

To remove everything not currently in use (including the base `dotnet/sdk` image, which will be re-downloaded on the next build):

```bash
TMPDIR=/opt/podman/tmp podman system prune -a
```

## Known Pitfalls

Commented out code does not always mean that it is old or obsolete. Maybe it just does not compile yet. It possibly is the goal.

Before making changes, read these in `docs/analysis/`:

1. **`Decl` inherits from `Stmt`** — widely considered the worst design decision. Future refactors should make them siblings under a common `Node` base. See `01-architecture.md`.
2. **Static state** — historically `VExt.cs` held a static `ScopeStack`; this was replaced by per-module `ModuleContext`. Do not introduce `static` classes that hold mutable state or shared mutable collections; keep compiler state instance-based and scoped to `ModuleContext` or per-invocation pipeline objects. See `01-architecture.md`.
3. **Mutating code generation** — `NewExpr.Gen()` used to mutate the AST (`type.ptrs.RemoveAt(0)`). Fixed in `backend/Core/Expr.cs`; the method now temporarily replaces the pointer list and restores it. See `03-ast-core.md` and `plan/suspicious-warnings.md`.
4. **Dead/future code** — `Symbol.cs` and `Attribute.cs` enums exist but are disconnected. They are stubs intended for semantic analysis. See `07-future-stubs.md`.
5. **Local target-framework override** — The working tree may temporarily change `.csproj` and Rider `.run.xml` files from `net10.0` to `net6.0` to match a local SDK. Keep these changes unstaged and do **not** commit them; the repository target remains `net10.0`.
6. **Derived-to-base conversions, implicit aliases, and default attributes** — derived-to-base conversions are implemented for pointers, references, and smart pointers. Object slicing is rejected as a compile-time error in assignments, arguments, and returns; an explicit value cast that would slice now emits a warning instead of an error. Auto-unhiding of overloaded base methods is controlled by the active `Dialect` instance (`Dialect.Default.AutoUnhideBaseMethods` by default) and can be overridden per class/method with `[shadow]` and `[unshadow]`. If a hidden name exists in multiple unrelated bases, auto-unhiding is skipped and a warning is emitted. `base` and `super` are not lexer keywords; `Dialect.Default.BaseClassAliasName` (default `"base"`) creates an implicit alias for the first base class inside any class/struct that inherits. `Dialect.Default.OwnClassAliasName` (default unset) can create an implicit alias for the enclosing class/struct. `self` is a reference to the current object (`(*this)`); `this` remains available as the pointer. `Dialect.Default.AutoReturnName` (default `"ret"`) declares an implicit result variable for non-void functions. The per-kind default attributes on the active `Dialect` instance are applied before resolution. The built-in `(move)` and `(forward)` casts return the operand's own type and are emitted as `std::move(...)` and `std::forward(...)`. The literal `null` is implicitly convertible to raw and smart pointer types; it is rejected for value types and references. Unqualified access to base members from inside a derived method is not yet supported; use `object.baseMember()` syntax. These settings live on a per-module `Dialect` record; `Dialect.Default` is the shared default instance.
7. **Template overload resolution shortcuts** — basic validation, argument deduction for `T`/`T*`/`T&`/pointer wrappers, and `.template` disambiguation are implemented. A few cases are intentionally delegated to the C++ compiler: non-template class-template methods are accepted without full Myll-side argument conversion checks; constructor calls on external C++ types and builtin calls such as `static_assert` are not validated by Myll. Dependent nested types such as `T::Nested` and `Class<T>::Nested<U>` now generate the required C++ `typename` and `template` keywords; `testing/cases/template_typename_fail/` has been converted to a passing case and additional typename cases cover return types, parameters, variables, base classes, and nested template-ids.
8. **Global namespace `using`** — module-scope `using NS;` of a namespace is controlled by the active `Dialect` instance (`Dialect.Default.GlobalUsingNS` defaults to `Contained`).
   - `Contained` (default) emits fully qualified names in the header and in the `.cpp`. The `using namespace NS;` directive is not emitted anywhere, because every name that Myll resolves is already qualified.
   - `Leaky` emits `using namespace NS;` in the generated header. For same-file namespaces this works because a forward `namespace NS {}` stub is emitted before the `using`.
   - `Disabled` rejects global namespace `using` with a diagnostic.
   - Namespace-scope `using` inside a namespace is left unchanged. Class/struct-scope namespace usings are always treated as contained because C++ does not allow `using namespace` inside a class definition.
   See `plan/contained-global-using.md` and `testing/cases/new_in_myll/using_ns.myll`.
9. **Untyped integer constants** — an untyped integer constant carries its exact value in `TypespecBasic.literalValue` (`BigInteger`): literals are parsed at type resolution, and compound expressions (`1024 * 1024 * 1024`, `0xFF << 8`, `0xF0 | 0x0F`, `100 % 7`, `1024 / 8`, ...) are constant-folded by `FoldUntypedIntegerResult` in `backend/Resolver/TypeResolver.cs` (division by zero is not folded). Unary minus folds to the negated constant, and a ternary of untyped constants carries all possible values in `TypespecBasic.literalValues`, so a target must hold every branch value. The constant binds to any numeric type that can hold it (`ConversionRules.GetUntypedLiteralRank`/`IntegerFits`), so `var u64 x = 1024 * 1024 * 1024;` and `var u64 x = 18446744073709551615;` work, and values that do not fit are rejected. Where the plain C++ emission of a folded constant would be undefined (shift count out of range, operand-type overflow, mixed signed/unsigned operand classes), `BinOp.Gen()` emits the exact folded constant instead. See `plan/untyped-constant-followups.md`, `testing/cases/new_in_myll/compound_constants.myll`, and `testing/cases/typecheck_fail/main.myll`.
10. **Char is comparable only with char** — `char` is not a number in Myll: `ValidateComparisonOperator` in `backend/Resolver/TypeChecker.cs` rejects a plain `char` compared with any other built-in scalar type (integer literals, typed `int`/`uint`, float, bool); use character literals such as `type == '\0'`. Pointer comparisons (`char*` vs `char*`) are unaffected. Built-in operator validation also runs on `if`/`while`/`for` conditions (`CheckBooleanOperand` calls `ValidateExpression`), so the check is enforced in conditions, not just expression position. See `testing/cases/typecheck_fail/main.myll`.
11. **Raw pointer semantics** — raw `T*` does not support `p[i]` or pointer arithmetic (`p + n`, `++p`, etc.) in Myll. Use `T[*]` (pointer-to-array) when you need C-style array-view semantics; it lowers to the same C++ `T*`. Arrays (`T[N]`) and smart arrays (`T[]!`/`T[]+`/`T[]?`) keep their indexing. Pointer arithmetic on `T[*]` can be disabled per module via `Dialect.AllowPointerToArrayArithmetic`.

## Current State & Priorities

- xUnit integration harness is in place under `testing/`. Remaining testing work is CI/CD.
- 108+ TODO/FIXME/HACK comments in backend.
- 14 NotImplementedException/NotSupportedException throws in backend code paths.
- Many features partially implemented; some features in grammar but not in generator.
- Last commit: net6.0→net10.0 target upgrade.

## Temporary files

Use `tmp/` inside the project root for any temporary files or scratch tests. Do not use `/tmp` because it is often mounted `noexec` and is not guaranteed to persist.

## OpenCode Workflow

- **Refresh**: Read `AGENTS.md` first, then check `git status` and `git log -3` to orient.
- **Planned Work**: Keep the "Planned Work" section current — that is our shared priority list.
- **Before big changes**: Propose a plan first, get approval, then execute.
- **Commits**: Small, focused messages. Prefer many small commits over large batches.
- **No pushes without explicit instruction**: Do not run `git push`. Do not ask whether to push. Only push if the user explicitly says "push".
- **Preserve indentation for edits**: Before constructing an `Edit`, re-check the exact leading tabs (and any spaces) from the `Read` output and match them for the changed lines so the replacement compiles cleanly.
- **Do not reformat whole files or run auto-formatters**: Do not run `dotnet format`, `clang-format`, or similar tools, and do not spend time manually realigning large blocks. If a file's formatting is significantly off, report it with a project-relative path (e.g. `backend/Resolver/Resolver.cs`) and let the user fix it in Rider.
- **Testing**: Validate with `dotnet build` and `dotnet test testing/` after any backend change.
- **ANTLR**: Grammar changes → regenerate during `dotnet build` → do not commit generated files.
- **Wrap-up**: Before ending a workday or switching topic, check whether `AGENTS.md`, `plan/*.md`, or other docs need updates so they stay consistent with the current state.

## Planned Work

1. **Finish ScopeStack / semantic analysis (endboss)** — DONE. The C++ generator uses resolved declarations for unqualified `IdExpr` generation. Shadowing diagnostics are implemented with a flag-based `ShadowingMode`. Remaining semantic-analysis work is normal bug fixing and edge-case handling. See `plan/semantic-analysis.md`, `plan/resolved-idexpr-generation.md`, `backend/Resolver/Resolver.cs`, `backend/Resolver/TypeChecker.cs`.
2. **Dialect/ModuleContext refactor and thin frontend driver** — DONE. `DialectOptions` was collapsed into `Dialect` with a static `Dialect.Default` instance; `CompilationContext` was renamed to `ModuleContext`, owns the module AST, and removed the old `GlobalNamespace.Context` back-reference; all visitors are instance-based per module. The full compiler pipeline (parse, AST construction, name/type resolution, and C++ generation) is encapsulated in `backend/CompilerPipeline.cs` (`ParseFiles` / `CompileModules` / `ResolveModules` / `GenerateFiles`). `frontend/Program.cs` is now a thin CLI/output driver that only handles options, file discovery, output I/O, and optional C++ compile/run.
3. Testing + CI/CD — xUnit harness is in place under `testing/`; remaining work is CI/CD.
4. Implement reachable NotImplementedException features:
    - Coreutil app option parsing — DONE. `frontend/coreutil/ls.myll`, `cat.myll`, `grep.myll`, etc. use `cxxopts.hpp` for option parsing. The hand-parsed duplicate variants were removed and the cxxopts variants renamed to canonical module/file names, leaving one implementation per utility.
   - discard — DONE. `_ = expr` lowers to `static_cast<void>(expr)`; `_` as a raw-pointer argument becomes `static_cast<T*>(nullptr)`; `_` as a value/reference argument introduces a hidden `myll_tmp_` local; `var`/`let T a = _` omits the initializer. See `backend/Resolver/DiscardTransformer.cs`, `testing/cases/discard/`.
   - abstract/default/delete function bodies — DONE. `[abstract]` emits `= 0` and auto-virtual; `[default]` emits `= default`; `[delete]`/`[disallow]` emit `= delete`. See `testing/cases/abstract_default_delete/` and `frontend/coreutil/ls.myll`.
    - inheritance resolution — DONE. Derived-to-base conversions work for raw pointers/references and smart pointers; member lookup walks base classes; object slicing is rejected; base methods hidden by an overloaded derived method are re-exposed with C++ `using Base::name;`. `[shadow]` and `[unshadow]` attributes can opt out or opt in per method or per class; when the hidden name exists in multiple unrelated bases a warning is emitted and no `using` is generated. See `testing/cases/inheritance/` and `testing/cases/slicing_fail/`.
    - implicit class aliases — DONE. `Dialect.Default.BaseClassAliasName` (default `"base"`) injects the first base class; `Dialect.Default.OwnClassAliasName` (default unset) injects the enclosing class/struct. Aliases are skipped and a warning is emitted when shadowed by a member, parameter, local variable, or catch parameter. See `backend/Resolver/ConfiguredAliasShadowingTransformer.cs` and `backend/Generator/HierachicalGen.cs`.
    - auto-return variable — DONE. `Dialect.Default.AutoReturnName` (default `"ret"`) declares an implicit result variable at the start of a non-void function and appends `return ret;` when control can fall off the end. It is suppressed when the return type is `void` or a reference, when the name is shadowed, or when the name is never used. See `backend/Resolver/AutoReturnTransformer.cs` and `testing/cases/auto_return/`.
    - basic templates — DONE. Function templates and class/struct templates with type parameters compile and run. Template parameters are injected as hidden scope symbols for the resolver; template definitions are emitted in the header so instantiation works. Basic template argument deduction now works for simple patterns (`T`, `T*`, `T&`, and pointer/smart-pointer wrappers). Dependent nested types such as `T::Nested` and `Class<T>::Nested<U>` generate the required C++ `typename` and `template` keywords, including in base-class lists. Explicit template arguments are still required for class/struct templates. See `backend/Resolver/TemplateParamTransformer.cs`, `backend/Resolver/TemplateInference.cs`, `backend/Core/Typespec.cs`, `backend/Generator/HierachicalGen.cs`, and `testing/cases/templates/`.
    - variable templates — DONE. Module/field-scope variable templates compile and run; Asteroids uses `std::numbers::pi_v<f32>`. See `testing/cases/var_templates/`. Variable template specializations exist (`plan/template-specialization.md`): named-only bindings in primary parameter order, full = all parameters bound, partial = subset; specializations live in the primary's module after the primary; uses resolve against the primary's signature and pick the matching specialization for emission. `const` variable templates are implicitly `[ct]` (emit `inline constexpr`), `var` variable templates stay runtime.
    - variable template specialization — DONE (2026-10-04). See `plan/template-specialization.md`. Cross-module specializations and the ODR guard followed (2026-10-08, phase 3): a specialization may live in a module that directly imports the primary's module; `FindMatchingSpecialization` picks visible specializations from a cross-module `specsByPrimary` index, and `ValidateVarSpecOdr` hard-errors the two C++ NDR traps (duplicate bound signatures across modules; uses that cannot see a matching specialization). Runtime module-scope specializations emit as inline variables.
  - class/struct/union template specialization — DONE (2026-10-08, phase 2). `class Box<T=int> { ... }` and partial `class Box<T, U=int> { ... }` (both spellings, unions included). Bodies are their own member sets (C++ first): bound names are not in scope, unbound parameters become the specialization's template parameters. Uses resolve through `TrySwapStructSpec` in `ResolvePath`, so members, constructors, and emission follow the specialization; the ODR guard covers structurals via the same `specsByPrimary` index. Emission: `template<> class Box<int> { ... }`, members are header entities, qualified names carry the spec args. A future `[interface]`/`[copy]` attribute to constrain bodies is noted in the plan. See `testing/cases/class_templates/` (same-module) and `testing/cases/cross_module_struct_spec(_fail)/` (cross-module).
  - function template specialization — DONE (2026-10-08, phase 4). `func max<T=int>( int a, int b ) -> int { ... }`: full-only (C++ has no partials), must repeat the primary's substituted signature including the return type (g++ rejects differing returns on explicit specializations). Bound parameter names stay usable in signature and body via hidden `IsBoundAlias` aliases that emission expands. Calls pick a matching spec via explicit or deduced template arguments (`TryResolveOverload` shadows the primary when a spec matches); the ODR guard covers functions via the same index. See `testing/cases/template_basics/` (same-module) and `testing/cases/cross_module_func_spec(_fail)/` (cross-module).
  - method (member function) specialization — DONE (2026-10-08, phase 5). In-class `func Val<T=int>( ... )` binds class parameters first, then the method's own; class params must be concrete, member params may stay open (`template<> template<typename U>` emission). Specs are emitted out-of-line after the class (g++/clang reject in-class member specializations; verified against the container's g++-14) and the class name inside a spec means the specialized instantiation. Calls combine receiver class args with member args in `TryResolveOverload` (most-bound spec wins); operators pick specs through `LookupOperatorOverload`. Same-module only. See `testing/cases/class_templates/spec.myll` (`run2`) and the method block in `testing/cases/template_fail/`.
  - constructor templates and specializations — DONE (2026-10-08, phase 6). `ctor<U>( ... )` declares a constructor template (args always deduced; `TryResolveOverload` has a Structor deduction branch), and `ctor<T=int>( ... )` specializes a constructor for concrete class args. Constructor names never carry template arguments — template-ids on constructor names are compiler-divergent (clang rejects), so fully bound specs emit the deduction form; plain-class constructor specializations are rejected (both compilers reject them). Emission is out-of-line with `inline` (also fixed for method specs, which are header entities). Templated conversion ctors (`ctor convert`) are deferred.
  - templated conversions, both directions — DONE (2026-10-09, phase 6c). `convert<U> <- T` / `ctor convert<U> <- T` (converting ctors) and `convert<U> -> T` (conversion operators, matching `op convert<U> -> T`). Parameters deduce from the source (ctors) or the required target (operators), mirroring the C++ conversion-template rule; see `TemplateInference.TryDeduceTemplateArgsAgainst`. Convert emission keeps pointer targets (`Gen`, not `GenType`). Copy/move ctors with a type parameter are rejected cleanly (`testing/cases/ctor_passing_fail/`), and ctor templates compose inside class specializations. See `testing/cases/convert/` (`G`/`H`) and `class_templates/` (`SpecBox`).
  - bound-name usability and non-type bindings — DONE (2026-10-09, phase 6d). Variable and class specializations keep bound parameter names usable through bound-parameter aliases (`const T pi<T=int> = (T)3;`), matching functions; the shared validator skeleton lives in `FindPrimary`/`ValidateFollowOrder`/`SignatureMatchesParas` in `SpecTransformer`. Non-type parameters bind with folded literal arithmetic (`class Arr<T=int, N=2*8>` folds to 16) and use sites fold the same way (`Arr<int, 2*8>` matches the folded spec; `TemplateArgFolding` is shared by visitor and transformer). Known mini-wart: `<-5>` lexes as left-arrow; write `< -5>` or `0-5`. Named constants, use-site named args, and non-type substitution inside declared types remain deferred. Alias template specializations (`alias X<T=int>`) are rejected cleanly — C++ has no spelling for them (`testing/cases/template_fail/`). See `testing/cases/var_templates/` (`rtv`), `testing/cases/class_templates/` (`Rebind`), and `testing/cases/non_type_template/`.
  - member variable and nested class specializations — DONE (2026-10-09, phase F). Member var templates are implicitly static (C++ allows only static member templates — the previous non-static emission was rejected by both compilers). Specs bind class params first (concrete), require at least one own param bound, and emit out-of-line after the class (`template<> template<> inline constexpr int Outer<int>::x<int> = 7;`, `template<> template<> class Outer<int>::Inner<f32> { ... };`); the collector recurses into class scopes, and nested-class uses prepend the enclosing class's args. See `class_templates/` (`Holder`, `run4`) and the member block in `template_fail/`.
  - declared specializations in `.decl.myll` — DONE (2026-10-08, phase 6b). A specialization declared in a prototype module says the real header already specializes this: uses resolve against the specialized member set, re-specializing from an authored module is rejected by the cross-module duplicate guard, and the module emits nothing. Bodiless function/ctor specs are accepted there since only the signature matters. See `testing/cases/cross_module_decl_spec(_fail)/`.
    - template argument kind validation and non-type template parameters — DONE. Added `TplParamKind`, `ValidateTemplateArgs`, grammar support for `int N` non-type parameters, and a shared `template<...>` helper. `Box<42>`, `add<int>(...)`, and similar mismatches are rejected. Added `std/std_array.decl.myll` for `std::array<T, int N>`. See `testing/cases/non_type_template/` and `testing/cases/non_type_template_fail/`.
    - local variable templates — DONE. Explicitly rejected with a diagnostic because C++ does not support block-scope variable templates. See `testing/cases/local_var_template_fail/`.
    - return-type inference and `[chain]` — DONE. Functions and methods without an explicit `-> Type` get their return type inferred from `return` statements; `[chain]` methods/operators return the enclosing class/struct by reference. Unqualified class names inside a class/struct now resolve to the class itself, so methods can return `ClassName&`.
    - pre-resolution transform migration — DONE. `AutoReturnTransformer`, `TemplateParamTransformer`, and `ChainTransformer` now run inside `backend/Resolver/Resolver.Resolve` instead of `frontend/Program.cs`. `[chain]` rejects explicit return types, warns on redundant trailing `return self;`, and cleaned up manual `-> Class&` / `return self;` boilerplate in `myll/myll_dyn_array.myll`, `testing/cases/external_algorithms/main.myll`, and `frontend/tests/thesis/container.myll`.
    - post-resolution control-flow transform migration — DONE. `ElseOnLoopTransformer` and `BreakContinueTransformer` now run inside `backend/Resolver/Resolver.Resolve` post-resolution; removed from `frontend/Program.cs`.
    - post-resolution alias-shadowing transform migration — DONE. `ConfiguredAliasShadowingTransformer` now runs inside `backend/Resolver/Resolver.Resolve` post-resolution; removed from `frontend/Program.cs`. Rewrote it to walk the resolved scope tree via `ModuleContext.LocalDecls` instead of the broken `Stmt.EnumerateDF`, adding regression tests for loop-local and catch-parameter alias shadows.
    - prototype-file skip alignment — DONE. `ElseOnLoopTransformer` and `BreakContinueTransformer` now skip prototype files consistently with `AutoReturnTransformer` and `ChainTransformer`.
    - external templated iterator algorithms — DONE. Free function templates taking iterator or pointer arguments can be called from class-template methods after the recent member-access and dependent return-type fixes. Regression case added at `testing/cases/external_algorithms/`.
    - rule-of-N enforcement and special copy/move ctors — DONE. `[rule_of_n=0|3|5]`, `[rule_of_0]`, `[rule_of_3]`, and `[rule_of_5]` validate that a class/struct satisfies one of the requested rules. Copy/move constructors are detected and no longer emitted with `explicit`, so copy-initialization and move-initialization work. `ctor copy`/`ctor move` shorthand generates the `const C& other`/`C&& other` parameter automatically, and `[default]`/`[delete]` exceptions for `= default`/`= delete`. Per-kind default attributes (`Dialect.Default.DefaultAttributesClass`/`Struct`/`Union`/`Enum`) are merged before other attribute-consuming passes. The built-in `(move)` and `(forward)` casts return the operand's own type. Regression cases added at `testing/cases/rule_of_n/`, `testing/cases/rule_of_n_fail/`, and `testing/cases/special_ctors/`.
    - direct construction syntax — DONE. Variables can be declared with `var T name(args);` instead of `var T name = T(args);`. The parser accepts `funcCall` in `idAccessor`; the AST stores this explicitly via `VarDecl.isDirectConstruct` / `VarStmt.isDirectConstruct`. Empty argument lists emit `{}` instead of `()` to avoid the C++ most-vexing-parse issue. Direct construction works at module/field scope and inside functions, including namespaced template types. Regression case added at `testing/cases/direct_ctor/`.
    - initializer-list literals — DONE. `[1,2,3]` has the first-class type `initlist<T>` (alias `ilist<T>`), which lowers to C++ `std::initializer_list<T>`. `var auto a = [1,2,3];`, `var ilist a = [1,2,3];` and `var initlist<int> a = [1,2,3];` all work; empty lists are rejected when the element type must be inferred. Container targets such as `var std::vector<int> v = [1,2,3];` or `var Myll::DynArray<int> a = [1,2,3];` emit C++ brace-initialization and rely on the container's `std::initializer_list<T>` constructor. Auto-inferred `initlist` types are resolved so member access such as `var auto a = [1,2,3]; a.size()` works without an explicit import. Added `ctor initlist` (and `operator initlist =`) as special members: they automatically take a `const std::initializer_list<T>&` parameter and are marked `[implicit]`. The default ctor parameter name is `init`; the element type defaults to the class's first template parameter or can be given explicitly (and becomes a ctor/operator template parameter when it does not name a class template parameter). Added `std/std_initializer_list.decl.myll` and updated `myll/myll_dyn_array.myll` to use `ctor initlist`. Standard C++ headers are no longer hardcoded in `StmtFormatting.DefaultIncludes`; they are emitted from the new `StdBuiltins.ImplicitStdImports` list, which is imported implicitly into every module. Regression case added/updated at `testing/cases/initializer_list/`.
   - convert — DONE. `convert -> T` emits a C++ conversion operator; `convert <- T` emits a converting constructor. Default attributes are controlled by `Dialect.DefaultAttributesConvertTo` (defaults to `pure`, so operators are const) and `Dialect.DefaultAttributesConvertFrom` (defaults to empty); every convert declaration must be `[implicit]` or `[explicit]`. User-defined conversions participate in overload resolution via `ConversionRules`. Added `std/std_string.decl.myll` `[implicit] convert <- const char*` and regression case `testing/cases/convert/`.
     - std::span binding and span-flavor `main` — DONE. Added `std/std_span.decl.myll`; functions can take `std::span<T>` parameters. Declaring `func main(std::span<char[*]> args)` or `func main(std::span<const char[*]> args)` emits the standard C++ `main` parameter list (`char* argv[]` or `const char* argv[]`) and injects the local `std::span<char*> args(argv, argc);` / `std::span<const char*> args(argv, argc);` at the top of the body. No wrapper function is emitted, and no prototype for `main` is written to the generated header. The driver auto-detects a single `main` module; `-M/--main` selects one when several modules define `main`. `std::span` and `std::vector` members (`size`, `empty`, `data`, `operator[]`) are now marked `[pure]` where C++ provides a `const` overload, and a second `const` overload of `operator[]` was added alongside the mutable one. Indexer overload resolution now respects the constness of the receiver. Regression cases: `testing/cases/span/`, `testing/cases/main_span/`, `testing/cases/main_span_const/`.
      - alias templates — DONE. `alias Vec<T> = std::vector<T>;` and mixed type/non-type alias templates like `alias FixedArray<T, int N> = std::array<T, N>;` are supported at module, namespace, class, and struct scope. The aliased type is expanded for Myll-side type checking and member lookup, while the generator emits `template<...> using ...` and keeps the alias name at use sites. Local (function-scope) alias templates are rejected with a diagnostic. Regression cases: `testing/cases/alias_template/`, `testing/cases/alias_template_fail/`, `testing/cases/local_alias_template_fail/`.
      - contained global `using` — DONE. `Dialect.GlobalUsingNS = Contained` moves module-scope namespace usings into affected function bodies and emits fully qualified names in the header. Class/struct-scope namespace usings are always contained. Namespace-scope `using` is left unchanged. Regression case: `testing/cases/contained_using_ns/`.
      - lambdas — DONE (2026-10-03). Lambdas use `kindOfFunc`, so `func`, `proc`, and `method` lambdas exist. The default capture per kind is dialect-configurable (`DefaultCaptureFunction = "[]"`, `DefaultCaptureProcedure = "[&]"`, `DefaultCaptureMethod = "[this]"`; see `dialect/myll.jsonc`); explicit capture lists are rejected for now; method lambdas require an enclosing non-static member function. Lambda bodies are transformed by the shared `DeclWalker`-based loop transforms (else-on-loop and break/continue run there; each lambda gets a fresh breakable stack, so `break N` crossing a lambda boundary is an error). Unannotated lambdas infer their return type, and `var auto` locals keep emitting `auto` (the resolver still validates the concrete type; module-scope autos and `new`-initialized autos materialize the concrete type). See `plan/bug-lambda-holes.md` and `testing/cases/lambda_basics/`.
   - named args, null coalescing call, copy-cast — lower priority; keep listed but tackle after structural/test/CI work.

   - else-on-loop — DONE. Loop `else` clauses (run only when the loop body never executes) are lowered to an entry flag plus an `if (!flag)` guard by `backend/Transform/ElseOnLoopTransformer.cs`, which runs inside `backend/Resolver/Resolver.Resolve`. Regression test at `testing/cases/synthetic_sugar/else_on_loop.myll` covers `for ... else` and `while ... else`.
   - **NOT** aspect, concept, defer — see `REASONS.md` (2026-06-10)
   - Note: the whole `myll_` name prefix is reserved for the compiler — DONE 2026-10-02. Enforced by a parse-time token scan in `CompilerPipeline.ScanReservedIdentifierPrefix` (no grammar change): user files defining `myll_*` identifiers get a clean error. Exempt: tokens inside `import` statements (so `import myll_dyn_array;` works) and files auto-discovered from the `myll/` standard-library directory (`ParserResult.IsCompilerOwned`). See `plan/bug-myll-tmp-reservation.md` and `testing/cases/reserved_prefix_fail/`.
5. Cleanup — `oldParser`/`oldCodeGen` removal, TODO triage, HACK resolution.
 6. `.idea/` misc.xml shared ANTLR config added to `.gitignore` exception.
 7. Cross-kind name sharing dialect (`AllowCpp`) — planned, see `plan/cross-kind-name-sharing.md`. Strict Myll rejects a name naming both a type and a function/variable; the dialect switch allows the C++-legal pairs (tag vs value), resolves call positions to the function and type positions to the type, and emits C++ elaborated type specifiers (`struct X`/`class X`/`enum E`) for shadowed names in generated type positions.

## Documentation Index

- Start here: `docs/README.md`
- Language spec: `docs/design/02-myll-language.md`
- C++ relationship: `docs/design/03-cpp-relationship.md`
- Architecture: `docs/analysis/01-architecture.md`
- Actionable fixes: `docs/analysis/08-actionable-fixes.md`
- Removed C/C++ features: `docs/design/06-removed-features.md`
- Decision log: `REASONS.md`
- Legacy attribute/dialect ideas: `Documentation/design_doc_cpp_myll_1.cpp` contains early design notes including the original `rule` concept, which is now referred to as "dialect". Treat as reference material, not the current spec.
