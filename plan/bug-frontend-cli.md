# Bug: frontend CLI defects

Status: **open**. Found in the 2026-09-29 review. Code-verified.

## Finding 1 (high confidence): `-O<level>` is emitted with a space

`frontend/Program.cs:328-330` builds the flag as `"-O " + opt.OptimizationLevel + " "`. `-O2`
therefore becomes `clang++ -std=c++23 -O 2 file.cpp`, and the C++ compiler treats `2` as an input
file. The documented `-O` option breaks every `-c`/`-cr` run that uses it.

Fix: emit `-O{level}` without the space.

## Finding 2: MSVC flags are not translated

`-g` and `-O2` are passed through unmodified for `cl`; only `/std:c++23` is translated
(`frontend/CppCompiler.cs:139-156`). `/Zi` and `/O2` are the MSVC spellings.

Fix: translate flags per detected compiler in `CppCompiler.CreateInvocation`.

## Finding 3: `-C` leaves stale generated C++ behind

`-C` deletes object files and the executable, but not previously generated `*.cpp`/`*.h`/`*.hpp`.
The `-c` step compiles **every** `*.cpp` in the output directory. After removing a module from the
sources, `-Ccr` still compiles its stale translation unit — duplicate symbols or resurrected code.

Fix: also delete generated `*.cpp`/`*.h`/`*.hpp` under `-C` (or track generated file names and delete
exactly those).

## Finding 4: warnings abort the post-compile gate

The post-compile gate uses `PrintDiagnosticsAndBail` (`Program.cs:257`), which exits on **any**
diagnostic, while the resolution gate two lines later (`:260`) tolerates warnings. `DialectLoader`
warnings (unknown key / missing key, `backend/Core/DialectLoader.cs:103-127`) flow into
`Context.Diagnostics`, so a user dialect JSONC with one typo'd key aborts the whole build with no
output unless `-k` is passed.

Fix: use `PrintDiagnosticsAndBailOnErrors` for the compile stage.

## Finding 5: exit codes and usage text

- `Environment.Exit(-99)` (→ 157 on Unix) and `(-7)` (→ 249) are non-standard and undocumented.
- CLI parse failures exit silently with `-7`: the error-printing code is commented out
  (`frontend/CommandLineOptions.cs:67-79`).
- The usage line advertises `myll --test` (`Program.cs:231`), which does not exist.
- `-r` without `-c` is silently ignored.

Fix: document a small convention (e.g. 1 = compile error, 2 = usage error), print parse errors,
remove or implement `--test`, diagnose `-r` without `-c`.

## Finding 6: small leaks and stale messages

- `process2` (the run step) is not disposed (`Program.cs:350`).
- `Console.WriteLine("Time elapsed after last ToArray call ...")` (`Program.cs:276`) — no `ToArray`
  remains.

## Verification

- `myll -i x.myll -o gen -cr -O2` compiles with optimization enabled (check the command line the
  driver prints, or strace).
- The `-C` scenario: generate two modules, delete one source, re-run `-Ccr`, confirm the stale
  `.cpp` is gone.
- A dialect JSONC with a typo'd key warns and continues.
- Invalid CLI arguments print a usage error to stderr.
- `dotnet test testing/` green.

## Risk

Low. The `-O` fix changes the command line for an option that did not work before.
