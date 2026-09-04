# Agent Instructions for Myll

Myll is an experimental programming language that compiles to C++. It aims to provide a saner C++ syntax while preserving C++ semantics and ABI compatibility. This is a living project, continuing from a research prototype. Single author: Jan "SoRDiD" Reitz (jan@sordid.de). Version: 0.0.1, early beta state. License undecided.

## Overview / Architecture

- `backend/` — compiler library. ANTLR grammar, parser, visitor pattern, AST (`Core/`), and C++ code generator (`Generator/`).
- `frontend/` — CLI executable. Parses `.myll` files, invokes backend, outputs `.h`/`.cpp` files.
- `frontend/tests/` — original `.myll` test cases (enums, game of life, thesis, mixed, etc.). Some are being ported to `testing/cases/`.
- `testing/` — xUnit integration-test harness that compiles and runs the ported `.myll` cases.
- `run/` — Rider run configurations that compile test suites.
- `Documentation/` — doc files and spec examples.
- `oldParser/`, `oldCodeGen/` — legacy, not in use.
- `plan/` — living design and implementation plans.
- `REASONS.md` — decision log for deferred and rejected features.

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
dotnet run --project frontend -- -i frontend/tests/gol/*.myll -o frontend/tests/gol/generated -cr

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
| `-R` | `--resolve` | Run semantic name resolution and report errors (default: on) |
| `-M` | `--main` | Specify module containing `main()` |

### Environment variables

| Variable | Description |
|----------|-------------|
| `MYLL_CXX` | If set, use this executable as the C++ compiler for the internal `-c` / `-cr` path. When unset, Myll tries `clang++`, `g++`, and `cl` in platform order. |
| `MYLL_TEST_TEMP` | If set, each test case uses an isolated temp directory under `testing/generated/` instead of the fixed case directory. This prevents concurrent test runs from interfering and avoids relying on `/tmp`, which is often mounted `noexec`. |
| `MYLL_TEST` | If set, generated test binaries such as game of life reduce delays so automated test runs complete faster. |
| `MYLL_COLOR` | Overrides color output for diagnostics. `1` forces colors on, `0` forces colors off. When unset, Myll enables colors automatically when stderr is a terminal, unless `NO_COLOR` is set. |
| `NO_COLOR` | If set to any non-empty value, suppresses diagnostic colors, matching the community convention. |

## Project Layout

```
myll/
├── backend/          # Compiler library (lexer, parser, AST, generator)
│   ├── Core/         # AST nodes, symbols, types, scope
│   ├── Grammar/      # ANTLR4 lexer/parser + generated C# code
│   ├── Generator/    # C++ code generation
│   └── Visitor/      # ANTLR4 visitors building the AST
├── frontend/         # CLI driver
│   ├── Program.cs            # Pipeline orchestration
│   ├── CommandLineOptions.cs # CLI argument parsing
│   └── tests/                # Original .myll test files (copied to output on build)
├── testing/          # xUnit integration-test harness and ported cases
│   ├── cases/                # Ported .myll test sources
│   └── golden/               # Expected generated C++ output
├── docs/             # Living design & analysis documentation
│   ├── design/       # Language design rationale
│   └── analysis/     # Code architecture, gaps, actionable fixes
├── plan/             # Implementation plans and decision logs
├── Documentation/    # Thesis PDF and design notes from thesis era
├── oldParser/        # Earlier ANTLR grammar iteration (reference only)
├── oldCodeGen/       # Earlier code generator (reference only)
├── run/              # Rider IDE run configurations
└── myll.sln          # Solution file
```

## ANTLR

- Grammar files: `backend/Grammar/MyllLexer.g4`, `backend/Grammar/MyllParser.g4`
- Generated output: `backend/Grammar/Generated/Myll/`
- Rider ANTLR config: `.idea/.idea.myll/.idea/misc.xml` (committed to git)
- Grammar changes: The `Antlr4BuildTasks` NuGet package regenerates the C# parser/lexer automatically during `dotnet build`. Commit the updated `backend/Grammar/Generated/` files together with the `.g4` change.

## Code Conventions

- **Nullable**: `backend` uses `Nullable=warnings`, `frontend` uses `Nullable=enable`.
- **Naming**: Mixed conventions in legacy code (`camelCase` and `PascalCase` coexist). Follow existing file patterns.
- **No `System.Func` conflicts**: The AST has a `Decl.Func` class — be careful with `using System;` at file scope.
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
- **Indent with tabs**: Use tabs for indentation. Use tabs for alignment too whenever possible. Only use spaces for alignment that cannot be expressed with tabs.
- **Myll indirection declarator spacing**: Put spaces on both sides of the reference/pointer block. Examples: `std::istream & in`, `T * ptr`, `const char *[] argv`, `Formatter *! fmt`. Exception: pure array brackets stay tight, e.g. `var int[4] myArray;`. In `new` expressions keep the pointer block tight: `new T*!`, `new T*`.
- **Myll class layout**: Group fields in a `field { ... }` block at the top; fields are private by default. Outdent access-section specifiers (`[pub]:`, `[priv]:`, `[prot]:`) one level so they line up with the class keyword. Keep per-declaration attributes (`[pure]`, `[override]`, `[static]`) indented with their declaration.
- **Myll method bodies**: Use the arrow form for simple expression-bodied getters: `func size() -> u64 => _size;`.
- **Rider links**: When running inside Rider's terminal, file-line references like `backend/Core/Expr.cs:42` are clickable. If using the `jetbrains://rd/navigate/reference?...` URI from the shell, the `line` value is zero-based, so subtract 1 from the 1-based source line.
- **Unix sample apps**: Build all utilities in `frontend/apps/unix/` with `frontend/apps/make_unix.sh`. It delegates to the existing `unix/Makefile` and supports `clean`, `release`, and `debug` targets.

## Documentation & Code Formatting Rules

- Markdown files (`*.md`): do not insert manual line breaks inside sentences. Break only at full stops (`. `). There is no maximum line length for Markdown files. Let the editor / viewer wrap to window width.
- C# source (`*.cs`) and Myll source (`*.myll`): keep lines around 200 characters. Do not break if it is only a few characters above, but always break before 240 characters.
- Technical docs (plans, specs, ADRs): use relaxed Simplified Technical English style. Keep sentences short. Use active voice. Put one main action per bullet.

## Build & Test

- xUnit integration tests live under `testing/`. Run them with `dotnet test testing/`.
- Manual testing: compile `.myll` files via the CLI and inspect generated `.cpp`/`.h` output. Also pass generated C++ code to a C++ compiler with sane warnings.
- See `docs/analysis/06-test-coverage.md` for test inventory and earlier harness design notes.

## Container / CI Notes

A `Dockerfile` and `.dockerignore` are provided at the repository root. They target `mcr.microsoft.com/dotnet/sdk:10.0` and install the latest `g++-14`, `clang-20`, and `antlr4` from Ubuntu 24.04 for build/test runs.

- Build image: `podman build -t myll .`
- Run tests: `podman run --rm myll`

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
2. **Static visitor state** — historically `VExt.cs` held a static `ScopeStack`; this was replaced by per-module `CompilationContext`. Make sure no new shared mutable state is introduced. See `01-architecture.md`.
3. **Broken AST traversal** — `ForStmt`, `WhileStmt`, `DoWhileStmt`, and `TimesStmt` have broken `EnumerateDF` that silently skips loop bodies. See `03-ast-core.md`.
4. **Mutating code generation** — `NewExpr.Gen()` used to mutate the AST (`type.ptrs.RemoveAt(0)`). Fixed in `backend/Core/Expr.cs`; the method now temporarily replaces the pointer list and restores it. See `03-ast-core.md` and `plan/suspicious-warnings.md`.
5. **Dead/future code** — `Symbol.cs` and `Attribute.cs` enums exist but are disconnected. They are stubs intended for semantic analysis. See `07-future-stubs.md`.
6. **Local target-framework override** — The working tree may temporarily change `.csproj` and Rider `.run.xml` files from `net10.0` to `net6.0` to match a local SDK. Keep these changes unstaged and do **not** commit them; the repository target remains `net10.0`.
7. **Derived-to-base conversions, implicit aliases, and default attributes** — derived-to-base conversions are implemented for pointers, references, and smart pointers. Object slicing is rejected as a compile-time error in assignments, arguments, and returns; an explicit value cast that would slice now emits a warning instead of an error. Auto-unhiding of overloaded base methods is controlled by `Dialect.AutoUnhideBaseMethods` and can be overridden per class/method with `[shadow]` and `[unshadow]`. If a hidden name exists in multiple unrelated bases, auto-unhiding is skipped and a warning is emitted. `base` and `super` are not lexer keywords; `Dialect.BaseClassAliasName` (default `"base"`) creates an implicit alias for the first base class inside any class/struct that inherits. `Dialect.OwnClassAliasName` (default unset) can create an implicit alias for the enclosing class/struct. `self` is a reference to the current object (`(*this)`); `this` remains available as the pointer. `Dialect.AutoReturnName` (default `"ret"`) declares an implicit result variable for non-void functions. `Dialect.DefaultAttributesClass`/`Struct`/`Union`/`Enum` set per-kind default attributes applied before resolution. The built-in `(move)` and `(forward)` casts return the operand's own type and are emitted as `std::move(...)` and `std::forward(...)`. Unqualified access to base members from inside a derived method is not yet supported; use `object.baseMember()` syntax.
8. **Template overload resolution shortcuts** — basic validation, argument deduction for `T`/`T*`/`T&`/pointer wrappers, and `.template` disambiguation are implemented. A few cases are intentionally delegated to the C++ compiler: non-template class-template methods are accepted without full Myll-side argument conversion checks; constructor calls on external C++ types and builtin calls such as `static_assert` are not validated by Myll. Dependent nested types such as `T::Nested` and `Class<T>::Nested<U>` now generate the required C++ `typename` and `template` keywords; `testing/cases/template_typename_fail/` has been converted to a passing case and additional typename cases cover return types, parameters, variables, base classes, and nested template-ids.
9. **Global namespace `using`** — module-scope `using NS;` of a namespace is controlled by `Dialect.GlobalUsingNS`.
   - `Leaky` (default) emits `using namespace NS;` in the generated header. For same-file namespaces this now works because a forward `namespace NS {}` stub is emitted before the `using`.
   - `Disabled` rejects global namespace `using` with a diagnostic.
   - `Contained` is planned but not implemented: it would emit `using namespace NS;` only in the `.cpp` and fully qualify names in the `.hpp`. Scoped `using` inside a namespace is still allowed.
   See `plan/contained-global-using.md` and `testing/cases/new_in_myll/using_ns.myll`.

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
- **Preserve indentation**: Before constructing an `Edit`, re-check the exact leading tabs (and any spaces) from the `Read` output. Match them exactly in the replacement so the file's indentation stays intact.
- **Testing**: Validate with `dotnet build` and `dotnet test testing/` after any backend change.
- **ANTLR**: Grammar changes → regenerate → commit generated files with the grammar change.
- **Wrap-up**: Before ending a workday or switching topic, check whether `AGENTS.md`, `plan/*.md`, or other docs need updates so they stay consistent with the current state.

## Planned Work

1. **Finish ScopeStack / semantic analysis (endboss)** — DONE. The C++ generator uses resolved declarations for unqualified `IdExpr` generation. Shadowing diagnostics are implemented with a flag-based `ShadowingMode`. Remaining semantic-analysis work is normal bug fixing and edge-case handling. See `plan/semantic-analysis.md`, `plan/resolved-idexpr-generation.md`, `backend/Resolver/Resolver.cs`, `backend/Resolver/TypeChecker.cs`.
2. Testing + CI/CD — xUnit harness is in place under `testing/`; remaining work is CI/CD.
3. Implement reachable NotImplementedException features:
   - Unix `ls` cxxopts variant — DONE. `frontend/apps/unix/ls_cxxopts.myll` mirrors `ls.myll` and uses `cxxopts.hpp` for option parsing.
   - discard — DONE. `_ = expr` lowers to `static_cast<void>(expr)`; `_` as a raw-pointer argument becomes `static_cast<T*>(nullptr)`; `_` as a value/reference argument introduces a hidden `myll_tmp_` local; `var`/`let T a = _` omits the initializer. See `backend/Resolver/DiscardTransformer.cs`, `testing/cases/discard/`.
   - abstract/default/delete function bodies — DONE. `[abstract]` emits `= 0` and auto-virtual; `[default]` emits `= default`; `[delete]`/`[disallow]` emit `= delete`. See `testing/cases/abstract_default_delete/` and `frontend/apps/unix/ls.myll`.
    - inheritance resolution — DONE. Derived-to-base conversions work for raw pointers/references and smart pointers; member lookup walks base classes; object slicing is rejected; base methods hidden by an overloaded derived method are re-exposed with C++ `using Base::name;`. `[shadow]` and `[unshadow]` attributes can opt out or opt in per method or per class; when the hidden name exists in multiple unrelated bases a warning is emitted and no `using` is generated. See `testing/cases/inheritance/` and `testing/cases/slicing_fail/`.
    - implicit class aliases — DONE. `Dialect.BaseClassAliasName` (default `"base"`) injects the first base class; `Dialect.OwnClassAliasName` (default unset) injects the enclosing class/struct. Aliases are skipped and a warning is emitted when shadowed by a member, parameter, local variable, or catch parameter. See `backend/Resolver/ConfiguredAliasShadowingTransformer.cs` and `backend/Generator/HierachicalGen.cs`.
    - auto-return variable — DONE. `Dialect.AutoReturnName` (default `"ret"`) declares an implicit result variable at the start of a non-void function and appends `return ret;` when control can fall off the end. It is suppressed when the return type is `void` or a reference, when the name is shadowed, or when the name is never used. See `backend/Resolver/AutoReturnTransformer.cs` and `testing/cases/auto_return/`.
    - basic templates — DONE. Function templates and class/struct templates with type parameters compile and run. Template parameters are injected as hidden scope symbols for the resolver; template definitions are emitted in the header so instantiation works. Basic template argument deduction now works for simple patterns (`T`, `T*`, `T&`, and pointer/smart-pointer wrappers). Dependent nested types such as `T::Nested` and `Class<T>::Nested<U>` generate the required C++ `typename` and `template` keywords, including in base-class lists. Explicit template arguments are still required for class/struct templates. See `backend/Resolver/TemplateParamTransformer.cs`, `backend/Resolver/TemplateInference.cs`, `backend/Core/Typespec.cs`, `backend/Generator/HierachicalGen.cs`, and `testing/cases/templates/`.
    - return-type inference and `[chain]` — DONE. Functions and methods without an explicit `-> Type` get their return type inferred from `return` statements; `[chain]` methods/operators return the enclosing class/struct by reference. Unqualified class names inside a class/struct now resolve to the class itself, so methods can return `ClassName&`.
    - external templated iterator algorithms — DONE. Free function templates taking iterator or pointer arguments can be called from class-template methods after the recent member-access and dependent return-type fixes. Regression case added at `testing/cases/external_algorithms/`.
    - rule-of-N enforcement and special copy/move ctors — DONE. `[rule_of_n=0|3|5]`, `[rule_of_0]`, `[rule_of_3]`, and `[rule_of_5]` validate that a class/struct satisfies one of the requested rules. Copy/move constructors are detected and no longer emitted with `explicit`, so copy-initialization and move-initialization work. `ctor copy`/`ctor move` shorthand generates the `const C& other`/`C&& other` parameter automatically, and `[default]`/`[delete]` exceptions for `= default`/`= delete`. Per-kind default attributes (`Dialect.DefaultAttributesClass`/`Struct`/`Union`/`Enum`) are merged before other attribute-consuming passes. The built-in `(move)` and `(forward)` casts return the operand's own type. Regression cases added at `testing/cases/rule_of_n/`, `testing/cases/rule_of_n_fail/`, and `testing/cases/special_ctors/`.
   - named args, null coalescing call, copy-cast, else-on-loop
   - **NOT** aspect, concept, defer, convert — see `REASONS.md` (2026-06-10)
   - Note: the `myll_tmp_` name prefix is reserved for the compiler; add it to the list of user-unusable identifiers in the lexer/grammar.
4. Cleanup — `oldParser`/`oldCodeGen` removal, TODO triage, HACK resolution.
5. `.idea/` misc.xml shared ANTLR config added to `.gitignore` exception.

## Documentation Index

- Start here: `docs/README.md`
- Language spec: `docs/design/02-myll-language.md`
- C++ relationship: `docs/design/03-cpp-relationship.md`
- Architecture: `docs/analysis/01-architecture.md`
- Actionable fixes: `docs/analysis/08-actionable-fixes.md`
- Removed C/C++ features: `docs/design/06-removed-features.md`
- Decision log: `REASONS.md`
- Legacy attribute/dialect ideas: `Documentation/design_doc_cpp_myll_1.cpp` contains early design notes including the original `rule` concept, which is now referred to as "dialect". Treat as reference material, not the current spec.
