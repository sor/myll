# Bug: the reserved `myll_` prefix is not enforced

**Scope widened 2026-09-30**: the whole `myll_` prefix is reserved, not just `myll_tmp_`. All
user-created identifiers starting with `myll_` must be rejected.

Status: **fixed 2026-10-02** — token-scan in `CompilerPipeline.ParseCST`
(`ScanReservedIdentifierPrefix`), no grammar change. Regression case:
`testing/cases/reserved_prefix_fail/`.

## Symptom

The `myll_tmp_` name prefix is reserved for compiler-generated temporaries (tracked in `AGENTS.md`:
"add it to the list of user-unusable identifiers in the lexer/grammar"). Nothing enforces it: there is
no lexer rule and no parser/visitor/resolver check. The only producers are
`ModuleContext.NextTempName()` (`backend/Core/ModuleContext.cs:124-129`) and the fallback at
`backend/Core/Stmt.cs:452`.

Verified: `tmp/review/tmp_prefix.myll` compiles cleanly today.

```myll
func main() -> int
{
	var int myll_tmp_0 = 1;
	return myll_tmp_0;
}
```

## Why it matters

The temporary counter starts at 0 per module. User code that declares `myll_tmp_0`, `myll_tmp_1`,
... can collide with temporaries synthesized by `ElseOnLoopTransformer`
(`backend/Transform/ElseOnLoopTransformer.cs:112,128,142`), `BreakContinueTransformer`
(`:33-34`), and `DiscardTransformer` (`:422`). The outcome is a shadowing diagnostic for the user's
own code, or — worse — a silent rebind where the compiler's temporary writes the user's variable (or
vice versa).

## Fix plan

Implemented as a parse-time token scan (no grammar change, so no ANTLR regeneration):

1. After parsing, `ScanReservedIdentifierPrefix` walks every token of the file and reports
   `identifier prefix 'myll_' is reserved for the compiler` for any text starting with `myll_`.
   String and comment literals cannot trigger it (their token text starts with quotes/comment
   markers).
2. **Import exemption**: tokens inside an `import` statement are skipped, so user code can name
   compiler-shipped modules like `import myll_dyn_array;`. Module names and definitions are not
   exempt.
3. **Standard-library exemption**: files auto-discovered from the `myll/` directory (the language's
   own standard library) skip the scan entirely (`ParserResult.IsCompilerOwned`), so the stdlib may
   freely use `myll_`-prefixed names. `std/` and `lib/` bindings and user `extern/` dirs are not
   exempt.
4. The frontend marks those files while collecting inputs (`CollectMyllLibraryFiles` fills the
   compiler-owned set passed to `CompilerPipeline.ParseFiles`).

Rejected alternative: a lexer rule for `myll_*` — it would also flag import statements, so the
import positions would need lexer-context awareness anyway; the token scan is simpler and keeps the
grammar untouched.

## Verification

- The repro produces a clean diagnostic with file:line:col, no generated output.
- Negative case `testing/cases/reserved_prefix_fail/` (variable and module-name violations).
- Positive coverage via the stdlib rename `myll/dyn_array.myll` → `myll/myll_dyn_array.myll`
  (`module myll_dyn_array;`), imported by `testing/cases/dyn_array/` and
  `testing/cases/init_syntax/`; goldens regenerated.
- Full test suite green — this is the check that no legitimate code uses the prefix.

## Risk

Low. No existing code uses the prefix. The only blast radius is user code that accidentally relied on
the name (none in this repo).
