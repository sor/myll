# Bug: frontend CLI defects

Status: **open** (findings 1-3 fixed 2026-09-30). Found in the 2026-09-29 review. Code-verified.

## Finding 1 (high confidence): `-O<level>` is emitted with a space — FIXED 2026-09-30

`frontend/Program.cs` built the flag as `"-O " + opt.OptimizationLevel + " "`, so `-O2` became
`clang++ -std=c++23 -O 2 file.cpp` and the C++ compiler treated `2` as an input file. The flag is
now emitted as one token (`-O2`), verified with a full `-Ccr -O2` run compiling, linking, and
running cleanly.

Clarification (2026-09-30): the `-O<level>` flag is the **C++ compiler's** optimization level —
built into the `cxxFlags` string that `CppCompiler` appends to the `clang++`/`g++`/`cl` invocation
(alongside `-std=c++23`); the MSVC side needs the `/O2` spelling (finding 2).

## Finding 2: MSVC flags are not translated — FIXED 2026-09-30

`-g` and `-O2` were passed through unmodified for `cl`; only `/std:c++23` was translated.
`CppCompiler.TranslateFlags` now translates the frontend's own flags (`-g` → `/Zi`,
`-O<level>` → `/O<level>`) whenever the detected compiler is `cl`; custom `MYLL_CXXFLAGS`/`CXXFLAGS`
tokens pass through untouched. Not runtime-verified (no MSVC on the current system) — re-check on
a Windows build.

## Finding 3: `-C` leaves stale generated C++ behind — FIXED 2026-09-30

`-C` deletes object files and the executable, but not previously generated `*.cpp`/`*.h`/`*.hpp`,
and the `-c` step compiled **every** `*.cpp` in the output directory, so a module removed from the
sources was silently compiled back into the binary (verified: the stale `extra.cpp` re-linked the
`extraValue` symbol into the final executable). The test harness never caught this because it
deletes/recreates its generated dir itself.

**Fix (no state written to the output dir):** the CLI's compile step now compiles **exactly the
`.cpp` files this run generated** (from the in-memory generation results, no directory glob, no
manifest). Stale files remain on disk — **intentionally**: `-C` only removes artifacts and Myll
overwrites its own files in place, so an IDE holding a generated file open keeps its session and
sees the new version without a delete/reopen cycle — but stale code is never compiled again,
silently (no notice). Hand-written helper `.cpp` files (arbitrary names, not renameable) are
compiled by the **test harness**, which knows the files it copies into its clean generated dir;
the CLI only compiles its own output. If a future `myllCompileRun` case needs helper `.cpp`, the
support-file copy must move before the compile and the golden comparer must tolerate them —
known limitation, not implemented.

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
