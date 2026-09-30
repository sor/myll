# Bug: reserved `myll_tmp_` prefix is not enforced

Status: **open**. Found in the 2026-09-29 review. Reproduced.

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

Prefer a lexer-level reservation:

1. Add a lexer rule that matches identifiers starting with `myll_tmp_` (e.g.
   `RESERVED_MYLL_TMP : 'myll_tmp_' [a-zA-Z0-9_]* ;` placed **before** `ID`).
2. Reject the token with a dedicated parser error or a visitor diagnostic:
   `identifier prefix 'myll_tmp_' is reserved for the compiler`.
3. Grep `std/`, `lib/`, `myll/`, `testing/`, `frontend/` to confirm nothing legitimate uses the
   prefix (the review found none; re-verify at implementation time).

Alternative: a parse-time check in the ID-visit path (keeps the grammar untouched, fires later).

## Verification

- The repro produces a clean diagnostic with file:line:col, no generated output.
- Add a negative case under `testing/cases/` (e.g. extend `typecheck_fail` or add
  `reserved_identifier_fail/`).
- Full test suite green — this is the check that no legitimate code uses the prefix.
- Regenerate the ANTLR output during `dotnet build`; do not commit generated files.

## Risk

Low. No existing code uses the prefix. The only blast radius is user code that accidentally relied on
the name (none in this repo).
