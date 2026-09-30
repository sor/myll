# Enhancement: grammar cleanup — dead tokens, unreachable rules, attr boilerplate

Status: **open**. Found in the 2026-09-29 review.

Grammar changes require ANTLR regeneration during `dotnet build` (Antlr4BuildTasks); do not commit
generated files.

## Finding 1: dead tokens reserve words

`DBL_POINT` (`..`), `AT` (`@`), `NOT`, `NAN`, `INF`, `IS`
(`backend/Grammar/MyllLexer.g4:20,42,215-218`) are referenced by no parser rule, yet they reserve
the words: `x.is` or `var nan = 1;` fails to parse with a cryptic ANTLR error.

Fix: remove the dead tokens — or wire them into `lit`/`id` if a use is intended. **Check before
deleting `DBL_POINT`**: the range expression `a...b` (`VExpr.cs:550`) may rely on `..` tokenization;
the range syntax is currently a crash path anyway (`bug-compiler-crashes-on-valid-syntax.md`), so
decide the range tokenization as part of this task.

## Finding 2: unreachable `comment` rule

`comment : COMMENT;` (`backend/Grammar/MyllParser.g4:5`) is unreachable: `COMMENT` tokens go to the
`COMMENTS` channel (`MyllLexer.g4:3-8`) and never reach the parser. → Remove the rule. (Comment
*preservation* in generated output is separately tracked in `docs/analysis/07-future-stubs.md`.)

## Finding 3: attribute rule boilerplate

The `attrUsing/attrAlias/attrConvert/attrCtor/attrOp/attrFunc/attrVar` rule pairs
(`MyllParser.g4:45-51,73-84`) and their visitor twins (`backend/Visitor/VDecl.cs:197-276`,
`backend/Visitor/VStmt.cs:98-135`, plus the `// TODO: huge overlap` at `VDecl.cs:324`) repeat the
same "attribBlk + defX + recurse + COLON" plumbing seven times per visitor. This repeated sequencing
is also where the attribute-ordering bugs live (`bug-attribute-handling.md`).

Fix: a single generic `attributed( defX )` wrapper rule (or visitor helper) collapses most of it.
Coordinate with `bug-attribute-handling.md` — do the fix first, then the collapse.

## Finding 4: `myll_tmp_` reservation

Implement the reserved-prefix enforcement here if the lexer route is chosen
(see `bug-myll-tmp-reservation.md`).

## Verification

- Grep: no parser rule references a removed token; `NUL`/`BOOL_LIT` handled separately in
  `enhancement-builtin-identifiers.md` (coordinate ordering).
- `dotnet build myll.sln` regenerates the parser/lexer; `dotnet test testing/` green; goldens
  unchanged (cleanup must not change accepted input).
- `x.is` / `var nan = 1;` either parse (if wired in) or produce a clean ANTLR token error (not a
  grammar crash).

## Risk

Medium — grammar regeneration touches every parse. The golden suite plus the full test run are the
gate. Do findings 1+2 (token/rule removal) in one commit and finding 3 in a separate, larger one.

Effort: M (finding 3 is L).
