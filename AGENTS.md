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
| `-M` | `--main` | Specify module containing `main()` |

### Environment variables

| Variable | Description |
|----------|-------------|
| `MYLL_CXX` | If set, use this executable as the C++ compiler for the internal `-c` / `-cr` path. When unset, Myll tries `clang++`, `g++`, and `cl` in platform order. |
| `MYLL_TEST_TEMP` | If set, each test case uses an isolated temp directory under `testing/generated/` instead of the fixed case directory. This prevents concurrent test runs from interfering and avoids relying on `/tmp`, which is often mounted `noexec`. |
| `MYLL_TEST` | If set, generated test binaries such as game of life reduce delays so automated test runs complete faster. |

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
- Grammar changes: If you modify `.g4` files in `backend/Grammar/`, you must regenerate the C# parser/lexer using ANTLR4 and update `backend/Grammar/Generated/`.

## Code Conventions

- **Nullable**: `backend` uses `Nullable=warnings`, `frontend` uses `Nullable=enable`.
- **Naming**: Mixed conventions in legacy code (`camelCase` and `PascalCase` coexist). Follow existing file patterns.
- **No `System.Func` conflicts**: The AST has a `Decl.Func` class — be careful with `using System;` at file scope.
- **Visitor pattern**: `VExpr`, `VDecl`, `VStmt`, `VTypes` walk the ANTLR parse tree → `Core` AST → `Generator` produces C++.

## Documentation & Code Formatting Rules

- Markdown files (`*.md`): do not insert manual line breaks inside sentences. Break only at full stops (`. `). Let the editor / viewer wrap to window width.
- C# source (`*.cs`) and Myll source (`*.myll`): keep lines around 120 characters. Do not break if it is only a few characters above, but always break before 160 characters.
- Technical docs (plans, specs, ADRs): use relaxed Simplified Technical English style. Keep sentences short. Use active voice. Put one main action per bullet.

## Build & Test

- xUnit integration tests live under `testing/`. Run them with `dotnet test testing/`.
- Manual testing: compile `.myll` files via the CLI and inspect generated `.cpp`/`.h` output. Also pass generated C++ code to a C++ compiler with sane warnings.
- See `docs/analysis/06-test-coverage.md` for test inventory and earlier harness design notes.

## Known Pitfalls

Commented out code does not always mean that it is old or obsolete. Maybe it just does not compile yet. It possibly is the goal.

Before making changes, read these in `docs/analysis/`:

1. **`Decl` inherits from `Stmt`** — widely considered the worst design decision. Future refactors should make them siblings under a common `Node` base. See `01-architecture.md`.
2. **Static visitor state** — `VExt.cs` holds static `ScopeStack` and visitor instances. This makes the compiler non-thread-safe despite PLINQ parallel parsing. See `01-architecture.md`.
3. **Broken AST traversal** — `ForStmt`, `WhileStmt`, `DoWhileStmt`, and `TimesStmt` have broken `EnumerateDF` that silently skips loop bodies. See `03-ast-core.md`.
4. **Mutating code generation** — `NewExpr.Gen()` mutates the AST (`type.ptrs.RemoveAt(0)`), destroying it for any subsequent pass. See `03-ast-core.md`.
5. **Dead/future code** — `Symbol.cs` and `Attribute.cs` enums exist but are disconnected. They are stubs intended for semantic analysis. See `07-future-stubs.md`.

## Current State & Priorities

- xUnit integration harness is in place under `testing/`. Remaining testing work is CI/CD.
- 108+ TODO/FIXME/HACK comments in backend.
- 14 NotImplementedException/NotSupportedException throws in backend code paths.
- Many features partially implemented; some features in grammar but not in generator.
- Last commit: net6.0→net10.0 target upgrade.

## OpenCode Workflow

- **Refresh**: Read `AGENTS.md` first, then check `git status` and `git log -3` to orient.
- **Planned Work**: Keep the "Planned Work" section current — that is our shared priority list.
- **Before big changes**: Propose a plan first, get approval, then execute.
- **Commits**: Small, focused messages. Prefer many small commits over large batches.
- **Testing**: Validate with `dotnet build` and `dotnet test testing/` after any backend change.
- **ANTLR**: Grammar changes → regenerate → commit generated files with the grammar change.
- **Wrap-up**: Before ending a workday or switching topic, check whether `AGENTS.md`, `plan/*.md`, or other docs need updates so they stay consistent with the current state.

## Planned Work

1. Testing + CI/CD — xUnit harness is in place under `testing/`; remaining work is CI/CD.
2. Implement reachable NotImplementedException features:
   - named args, null coalescing call, copy-cast, else-on-loop, discard (empty stmt done)
   - **NOT** aspect, concept, defer, convert — see `REASONS.md` (2026-06-10)
3. Cleanup — `oldParser`/`oldCodeGen` removal, TODO triage, HACK resolution.
4. `.idea/` misc.xml shared ANTLR config added to `.gitignore` exception.

## Documentation Index

- Start here: `docs/README.md`
- Language spec: `docs/design/02-myll-language.md`
- C++ relationship: `docs/design/03-cpp-relationship.md`
- Architecture: `docs/analysis/01-architecture.md`
- Actionable fixes: `docs/analysis/08-actionable-fixes.md`
- Decision log: `REASONS.md`
