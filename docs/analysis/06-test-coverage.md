# Test Coverage

## Test Inventory

### `/frontend/tests/class/` — 1 test + 2 validation files
- `test.myll` — Forward declarations, nested classes, method declarations
- `validation/test.cpp`, `validation/test.h` — Handwritten reference output

Status: Minimal smoke test. Covers basic class parsing.

### `/frontend/tests/enum/` — 8 tests, no validation
Tests declaration syntax only:
- `01_basic.myll`
- `02_basic_comma.myll`
- `03_numbered.myll`
- `04_numbered_comma.myll`
- `05_numbered_partially.myll`
- `21_flags.myll`
- `22_flags_numbered.myll`
- `23_flags_numbered_partially.myll`

Status: Good syntax coverage, zero output validation.

### `/frontend/tests/int/` — 2 tests, no validation
- `01_basic.myll` — integer arithmetic operators
- `50_bint_basic.myll` — binary integer type

Status: Very shallow — expressions discarded, no assertions.

### `/frontend/tests/mixed/` — 11 tests + 3 validation files
- `stack.myll` — Generic `Stack<T>` with full features
- `main.myll` — Module imports, generics, control flow
- `enum.myll` — Flags enum with operations
- `testcase.myll` — Extensive syntax showcase (much commented out)
- `sheet.myll`, `sheet1-4.myll` — SDL presentation app (highly redundant)
- `plasma.myll` — Simple math function
- `validation/main.cpp`, `validation/main.h`, `validation/stack.h`

Status: Best integration tests. Validation only covers `stack` and `main`.

### `/frontend/tests/gol/` — 2 tests, no validation
- `game_of_life.myll` — Real-world algorithm: 2D arrays, nested loops, generics
- `main.myll` — Driver with game loop

Status: Solid integration demo. No reference output.

### `/frontend/tests/thesis/` — 3 tests, no validation
- `main.myll` — 350-line feature showcase, many commented-out features
- `container.myll` — Generic `MyStack<T>` with `unique_ptr`
- `parsertest.myll` — Parser stress test

Status: Essential for understanding language scope. Reads as development scratchpad.

### `/Documentation/the_final_test.myll`
- Advanced features showcase: smart pointers, operator overloading, properties, defer
- Many features exist only in grammar/AST, not in generator

## Cross-Reference: Features ↔ Tests

| Feature | Test Path | Notes |
|---------|-----------|-------|
| **Classes** | `tests/class/test.myll` | Forward declarations, nesting, methods |
| **Enums (basic)** | `tests/enum/01-05_*.myll` | Basic, trailing comma, numbered, partial |
| **Enums (flags)** | `tests/enum/21-23_*.myll` | `[flags, operators(bitwise)]` |
| **Integer arithmetic** | `tests/int/01_basic.myll` | `+ - * / % \| &` operators |
| **Binary integers** | `tests/int/50_bint_basic.myll` | `bint` type |
| **Generic stack** | `tests/mixed/stack.myll`, `validation/stack.h` | `Stack<T>` with ctors, dtor, methods |
| **Modules / imports** | `tests/mixed/main.myll`, `validation/main.*` | Imports, using, generics |
| **Enum operations** | `tests/mixed/enum.myll` | Flags enum in expressions |
| **Syntax showcase** | `tests/mixed/testcase.myll` | Many features, much commented out |
| **SDL presentation** | `tests/mixed/sheet*.myll` | Inheritance, override, arrays |
| **Math functions** | `tests/mixed/plasma.myll` | `f32`, `sin`, `cos`, `sqrt` |
| **Game of Life** | `tests/gol/game_of_life.myll`, `main.myll` | 2D arrays, nested loops, generics; blocked on member access in resolver |
| **Using namespaces** | `testing/cases/using_ns/` | Cross-module namespace merging and `using NS;` end-to-end |
| **Access modifiers** | `testing/cases/access_mods/` (planned) | 20+ member class mixing section and per-decl access |
| **Thesis showcase** | `tests/thesis/main.myll` | 350-line comprehensive feature catalog |
| **Parser stress test** | `tests/thesis/parsertest.myll` | Enums, structs, classes, templates, ctors, operators |
| **Generic container** | `tests/thesis/container.myll` | `MyStack<T>` with `unique_ptr` |

Many test files contain **alternative syntax explorations** not documented elsewhere — treat them as informal design notes.

## Critical Gaps

| Gap | Severity |
|-----|----------|
| **No automated test runner** | Critical |
| **xUnit harness exists but only covers ported cases** | High |
| **No validation for 80% of tests** | High |
| **No negative/error tests** | High |
| **No behavioral tests** (run generated code) | High |
| **No tests for**: unions, `requires` clauses, lambda execution, `defer`, properties, operators | Medium |
| **Handwritten validation will drift** | Medium |

## What Exists in `bin/Debug/`

Generated `.cpp` and `.h` files exist but are build artifacts, not committed reference outputs. They:
- Change with every build.
- Are not compared against anything.
- May be stale from older compiler versions.

## Future Outlook

### Immediate: Test Harness Design
Create a test runner that:
1. Discovers all `.myll` files in `tests/`.
2. Compiles each to C++.
3. Compares output against committed `.cpp`/`.h` reference files (if present).
4. If no reference exists, generates one and flags it for human review.
5. Optionally compiles generated C++ with `clang++` to verify it builds.
6. Optionally runs the resulting executable and compares stdout.

### Test Runner Implementation
```bash
# Proposed workflow
./test_runner.py
# Output:
# PASS: enum/01_basic (no reference, generated)
# PASS: class/test (matches reference)
# FAIL: mixed/stack (reference mismatch at line 42)
# ERROR: thesis/main (compilation failed)
```

### Test Categories to Add

| Category | Examples |
|----------|----------|
| **Syntax acceptance** | Does this parse? (existing) |
| **Output validation** | Does generated C++ match reference? (partial) |
| **C++ compilation** | Does generated C++ build? (missing) |
| **Behavioral** | Does running produce expected output? (missing) |
| **Negative** | Does invalid code produce an error? (missing) |
| **Feature regression** | Did adding X break Y? (missing) |

### Existing Tests That Need Validation

Priority order:
1. `enum/*.myll` — validate generated enum C++.
2. `gol/*.myll` — compile and run, verify it produces a game of life grid. Blocked on member access resolution.
3. `mixed/enum.myll` — validate flags enum generation.
4. `int/*.myll` — validate integer type mapping.
5. `alias/*.myll` — validate type and namespace alias generation.
6. `bases/*.myll` — validate base-class access specifiers and virtual inheritance.
7. `mixed/plasma.myll` — validate simple function generation.

## Recommendations

1. **Freeze the compiler behavior**, generate reference outputs for all existing tests, and commit them.
2. **Write the test harness** before adding new features.
3. **Add negative tests** as the semantic analyzer is built (e.g., "this should not compile because X").
4. **Replace handwritten validation** with generated-and-approved references to prevent drift.
