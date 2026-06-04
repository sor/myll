# Frontend / Driver

## Files

| File | Purpose |
|------|---------|
| `Program.cs` | Compiler driver — pipeline orchestration |
| `CommandLineOptions.cs` | CLI argument parsing |

## Pipeline Stages

1. **Parse**: ANTLR4 tokenization and parsing (parallel via PLINQ)
2. **Classify**: Group files by module
3. **Compile**: Build AST via visitors
4. **Generate**: Produce C++ declarations and implementations
5. **Emit**: Write files or output to stdout
6. **Compile C++**: Optional invocation of `clang++-15`
7. **Run**: Optional execution

## Strengths

- Clear pipeline separation.
- PLINQ parallel parsing for multiple input files.
- Module grouping before generation.
- Support for both file output and stdout output.

## Issues

### Resource Leak
```csharp
static ITokenSource CreateLexer(string path) {
    var s = new FileStream(path, FileMode.Open);
    var r = new StreamReader(s);  // NEVER DISPOSED
    return new MyllLexer(new AntlrInputStream(r));
}
```

**Fix:** Use `using` statements or return an `IDisposable` wrapper.

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
1. Fix `StreamReader` / `FileStream` disposal.
2. Add `--cxx-compiler` and `--std` options.
3. Narrow exception handling in `ParseCST`.

### Medium-Term
4. Add incremental compilation support (only recompile changed modules).
5. Add configurable include path.
6. Add verbose logging for debugging.

### Long-Term
7. Watch mode (recompile on file changes).
8. IDE integration (language server protocol).
9. Package manager integration.
