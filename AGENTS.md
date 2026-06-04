# Agent Instructions for Myll

Myll is an experimental programming language that compiles to C++. It aims to provide a saner C++ syntax while preserving C++ semantics and ABI compatibility. This is a living project, continuing from a research prototype.

## Quickstart

### Build

```bash
dotnet build myll.sln
```

Targets: .NET 6.0 (net6.0)
C# Language Version: 9.0

### Run the Compiler

```bash
# Basic compilation of test files
dotnet run --project frontend/frontend.csproj -- \
  -i frontend/tests/mixed/testcase.myll frontend/tests/mixed/main.myll \
  -o frontend/tests/mixed/generated

# Compile with C++ output and run
dotnet run --project frontend/frontend.csproj -- \
  -i frontend/tests/thesis/*.myll \
  -o frontend/tests/thesis/generated \
  -cr

# Clear output, compile C++, and run
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
| `-c` | `--compile` | Pass generated `.cpp` files to `clang++-15` |
| `-C` | `--clear` | Clear target directory of old build artifacts |
| `-g` | `--debug` | Generate debug executable via C++ compiler |
| `-O` | `--optimize` | Set C++ compiler optimization level |
| `-r` | `--run` | Run the compiled binary |
| `-M` | `--main` | Specify module containing `main()` |

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
│   └── tests/                # .myll test files (copied to output on build)
├── docs/             # Living design & analysis documentation
│   ├── design/       # Language design rationale
│   └── analysis/     # Code architecture, gaps, actionable fixes
├── oldParser/        # Earlier ANTLR grammar iteration (reference only, )
├── oldCodeGen/       # Earlier code generator (reference only)
├── Documentation/    # Thesis PDF and design notes from thesis era (for in depth queries read the Myll.odt or Myll.pdf, the other files here are ideas or scribbles)
├── run/              # Rider IDE run configurations
└── myll.sln          # Solution file
```

## Build & Test

- **No automated test harness exists yet.** See `docs/analysis/06-test-coverage.md` for the current test inventory and proposed harness design.
- **Manual testing**: Compile `.myll` files via the CLI and inspect generated `.cpp`/`.h` output. Also pass generated C++ code to a C++ compiler and let it compile with sane warnings.
- **Grammar changes**: If you modify `.g4` files in `backend/Grammar/`, you must regenerate the C# parser/lexer using ANTLR4 and update `backend/Grammar/Generated/`.

## Code Conventions

- **Nullable**: `backend` uses `Nullable=warnings`, `frontend` uses `Nullable=enable`.
- **Naming**: Mixed conventions in legacy code (`camelCase` and `PascalCase` coexist). Follow existing file patterns.
- **No `System.Func` conflicts**: The AST has a `Decl.Func` class — be careful with `using System;` at file scope.

## Known Pitfalls

Commented out code does not always mean that it is old or obsolete, maybe it just does not compile yet. It possibly is the goal.

Before making changes, read these in `docs/analysis/`:

1. **`Decl` inherits from `Stmt`** — widely considered the worst design decision. Future refactors should make them siblings under a common `Node` base. See `01-architecture.md`.
2. **Static visitor state** — `VExt.cs` holds static `ScopeStack` and visitor instances. This makes the compiler non-thread-safe despite PLINQ parallel parsing. See `01-architecture.md`.
3. **Broken AST traversal** — `ForStmt`, `WhileStmt`, `DoWhileStmt`, and `TimesStmt` have broken `EnumerateDF` that silently skips loop bodies. See `03-ast-core.md`.
4. **Mutating code generation** — `NewExpr.Gen()` mutates the AST (`type.ptrs.RemoveAt(0)`), destroying it for any subsequent pass. See `03-ast-core.md`.
5. **Dead/future code** — `Symbol.cs` and `Attribute.cs` enums exist but are disconnected. They are stubs intended for semantic analysis. See `07-future-stubs.md`.

For a prioritized fix list, see `docs/analysis/08-actionable-fixes.md`.

## Documentation Index

- Start here: `docs/README.md`
- Language spec: `docs/design/02-myll-language.md`
- C++ relationship: `docs/design/03-cpp-relationship.md`
- Architecture: `docs/analysis/01-architecture.md`
- Actionable fixes: `docs/analysis/08-actionable-fixes.md`
