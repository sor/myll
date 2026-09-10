# Frontend / Driver

## Files

| File | Purpose |
|------|---------|
| `Program.cs` | Compiler driver — pipeline orchestration |
| `CommandLineOptions.cs` | CLI argument parsing |

## Pipeline Stages

1. **Parse**: ANTLR4 tokenization and parsing (parallel via PLINQ across input files).
2. **Classify**: Group files by module.
3. **Compile**: Build per-module AST + scope tree via instance-based `ModuleContext` visitors (parallel across modules since Phase 2A).
4. **Implicit imports**: Inject `StdBuiltins.ImplicitStdImports` into every module.
5. **Resolve**: Cross-module name and type resolution (`NameResolver.Resolve`), including cyclic imports. This stage also runs two AST transformation passes: pre-resolution transforms (default attributes, enum normalization, auto-return, template params, chain methods) and post-resolution transforms (discard, rule-of-N, loop lowering, break/continue lowering, alias-shadowing checks).
6. **Raw imports / prototype filter**: Build the raw-import map and identify prototype-only modules.
7. **Generate**: Produce C++ declarations and implementations per module (parallel across non-prototype modules).
8. **Emit**: Write files or output to stdout.
9. **Compile C++**: Optional invocation of the platform C++ compiler.
10. **Run**: Optional execution.

## Strengths

- Clear pipeline separation.
- PLINQ parallel parsing for multiple input files.
- PLINQ parallel module AST construction.
- Per-module `ModuleContext` owns visitors and scope stack, so modules compile independently.
- Module grouping before generation.
- Support for both file output and stdout output.

## Issues

### Resource Leak — FIXED
The driver now reads input with `File.ReadAllText` and constructs per-call lexer/parser/token-stream objects. There is no longer a leaked `StreamReader`/`FileStream`.

*Historical snippet:*
```csharp
static ITokenSource CreateLexer(string path) {
    var s = new FileStream(path, FileMode.Open);
    var r = new StreamReader(s);  // NEVER DISPOSED
    return new MyllLexer(new AntlrInputStream(r));
}
```

### Hardcoded Toolchain
```csharp
string command = "clang++-15";
string args = $"-std=c++20 {optimization} ...";
```

**Impact:** Not portable to other compilers or C++ standards.

**Fix:** Add `--cxx-compiler` and `--std` options to CLI.

### Broad Exception Catch
```csharp
try {
    tree = parser.ParseCST();
} catch {
    parser = new Parser(...);  // recreate parser
    tree = parser.ParseCST();
}
```

**Impact:** Swallows all exceptions, makes debugging harder.

**Fix:** Catch specific ANTLR exceptions.
### Thread Pool Configuration

```csharp
ThreadPool.SetMinThreads(cpus * 2, cpus * 2);
```

This is arbitrary. PLINQ typically manages its own thread pool.

### Static ScopeStack — FIXED

The static `Stack<Scope>` in `backend/Visitor/VExt.cs` was replaced by instance-based visitors owned by a per-module `ModuleContext`. This enables parallel module compilation and test isolation. See `docs/analysis/01-architecture.md` for the current design.

## CLI Options

| Flag | Purpose | Status |
|------|---------|--------|
| `-i`, `--input` | Input files | Working |
| `-o`, `--output` | Output path | Working |
| `-s`, `--stdout` | Print to stdout | Working |
| `-d`, `--deep` | Recursive search | Working |
| `-k`, `--keep-going` | Continue on errors | Working |
| `-n`, `--no-file` | Don't write files | Working |
| `-c`, `--compile` | Compile C++ | Working (hardcoded clang) |
| `--clear` | Clear output | Working |
| `--debug` | Debug info | Working |
| `--optimize` | Optimization level | Working |
| `-r`, `--run` | Execute after compile | Working |
| `--main` | Main module for `run` | Working |

## Missing Options

- `--cxx-compiler` (custom compiler path)
- `--std` (C++ standard)
- `--include-path` (additional include directories)
- `-v`, `--verbose` (logging level)
- `--emit-ast` (output AST for debugging)
- `--dump-symbols` (output symbol table)

## Future Outlook

### Short-Term
1. Add `--cxx-compiler` and `--std` options.
2. Narrow exception handling in `ParseCST`.
3. Add `--include-path` support.
4. Add verbose logging for debugging.

### Medium-Term
5. Add incremental compilation support (only recompile changed modules).
6. Parallelize remaining sequential stages (resolution, pre/post-resolution transforms, type checking) by splitting shared `ResolutionResult` and diagnostics per module.
7. Add `--emit-ast` / `--dump-symbols` debugging options.

### Long-Term
8. Watch mode (recompile on file changes).
9. IDE integration (language server protocol).
10. Package manager integration.
