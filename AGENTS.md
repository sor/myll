# Myll

## Overview
Myll is a programming language that compiles to C++. Written in C#.
Single author: Jan "SoRDiD" Reitz (jan@sordid.de).
Version: 0.0.1, early beta state. License undecided.

## Architecture
- backend/ — library (myll_lib). ANTLR grammar, parser, visitor pattern, AST (Core/), and C++ code generator (Generator/).
- frontend/ — CLI executable (myll). Parses .myll files, invokes backend, outputs .h/.cpp files.
- frontend/tests/ — .myll test cases (enums, game of life, thesis, mixed, etc.)
- run/ — Rider run configurations that compile test suites
- Documentation/ — doc files and spec examples
- oldParser/, oldCodeGen/ — legacy, not in use

## Build & Run
- `dotnet build` — builds both projects
- `dotnet run --project frontend -- <args>` — runs the compiler
- Targets: net6.0;net10.0

## Test Examples (via CLI)
- Thesis:   `dotnet run --project frontend -- -i frontend/tests/thesis/*.myll -o frontend/tests/thesis/generated -cr`
- GameLof:  `dotnet run --project frontend -- -i frontend/tests/gol/*.myll -o frontend/tests/gol/generated -cr`
- Mixed:    `dotnet run --project frontend -- -i frontend/tests/mixed/*.myll -o frontend/tests/mixed/generated -cr`

## ANTLR
- Grammar files: backend/Grammar/MyllLexer.g4, MyllParser.g4
- Generated output: backend/Grammar/Generated/Myll/
- Rider ANTLR config: .idea/.idea.myll/.idea/misc.xml (committed to git)

## Conventions
- C# 9, nullable enabled (frontend), nullable warnings (backend)
- Visitor pattern: VExpr, VDecl, VStmt, VTypes walk the ANTLR parse tree → Core AST → Generator produces C++
- No unit test project. Tests are .myll source files that should compile to valid C++.

## Current State & Priorities
- No automated tests, no CI/CD
- 108+ TODO/FIXME/HACK comments in backend
- 14 NotImplementedException/NotSupportedException throws in backend code paths
- Many features partially implemented; some features in grammar but not in generator
- Last commit: net6.0→net10.0 target upgrade (commit 645a259)

## OpenCode Workflow
- **Refresh**: Read AGENTS.md first, then check `git status` and `git log -3` to orient
- **Planned Work**: Keep the "Planned Work" section current — that's our shared priority list
- **Before big changes**: Propose a plan first, get approval, then execute
- **Commits**: Small, focused messages. Prefer many small commits over large batches
- **Testing**: Validate with `dotnet build` and at least one compiled test suite after any backend change
- **ANTLR**: Grammar changes → regenerate → commit generated files with the grammar change

## Planned Work
1. Testing + CI/CD first — harness existing .myll files into automated validation
2. Implement reachable NotImplementedException features (named args, null coalescing call, copy-cast, else-on-loop, discard, empty stmt)
3. Cleanup — oldParser/oldCodeGen removal, TODO triage, HACK resolution
4. .idea/ misc.xml shared ANTLR config added to .gitignore exception
