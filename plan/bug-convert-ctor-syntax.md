# Bug: `ctor convert T x` drops its parameter; `VisitDefConvert` text-parsing

Status: **open — explore first** (2026-09-30). Found in the 2026-09-29 review. One repro verified, one
finding refuted.

Before fixing anything here, decide whether the `ctor convert T x` / `op convert ...` spellings
should stay at all: the raw `convert` forms (`convert -> T`, `convert <- T`) are up and running. If
the ctor/op variants are dropped from the grammar, finding 1 disappears with the rule, and finding 2
(text-parsing hardening) only matters for the spellings that remain.

## Finding 1 (verified): `defCtor` convert alternatives drop the parameter

The grammar defines two convert alternatives for constructors
(`backend/Grammar/MyllParser.g4:128-129`):

```
CONVERT  LARROW?  typespec  id?
CONVERT  typespec id?  RARROW?
```

`VisitDefCtor` (`backend/Visitor/VDecl.cs:711-751`) only handles `kindOfPassing()` and
`funcTypeDef()`. For both convert alternatives those are null, so the ctor is built with **zero
parameters** and `c.typespec()` / `c.id()` are never visited — the converting parameter is parsed and
silently dropped.

Repro `tmp/review/ctor_convert.myll`:

```myll
class C {
[pub]:
	field int i;
	ctor convert bool b { i = 1; }
}
```

Generated header: `C();` — a zero-parameter constructor, no diagnostic.

Fix: handle both alternatives in `VisitDefCtor` and build the parameter from `c.typespec()` +
`c.id()`, mirroring the `convert <-` path in `VisitDefConvert`. Also check the `defOp` convert
alternative (`MyllParser.g4:137`, `CONVERT tplParams? RARROW? typespec`) — unverified, the
implementer should confirm both arrow directions are handled there too.

## Finding 2 (refuted as a bug, kept as hardening): `VisitDefConvert` text-parsing

`VisitDefConvert` decides `convert -> T` vs `convert <- T` by parsing `c.GetText()`
(`backend/Visitor/VDecl.cs:657-659`): `raw.StartsWith("convert")` then `StartsWith("->")`.

This looks like it would break for the lexer alias `CONVERT : 'conv'|'convert'`
(`backend/Grammar/MyllLexer.g4:220`) — and the original review flagged it as a high-severity
semantic inversion. **Verification refuted that**: the `CONVERT` token belongs to the *parent*
`declConvert` rule, so the `defConvert` context's text always starts at the arrow. Both spellings
work today:

- `tmp/review/conv_alias.myll` (`conv -> bool`) → `operator bool() const;`
- `tmp/review/conv_alias2.myll` (`conv <- int` + `conv -> bool`) → `C(int other);` +
  `operator bool() const;`

The `raw.StartsWith("convert")` branch is dead code, and the whole mechanism is fragile (any grammar
change that pulls `CONVERT` into `defConvert` would silently invert the direction). Fix: branch on
`c.RARROW() != null` / `c.LARROW() != null` and delete the text parsing.

## Verification

- `tmp/review/ctor_convert.myll` generates `C(bool b)` plus the body; clang++ clean.
- `tmp/review/conv_alias.myll` / `conv_alias2.myll` still generate the same output after the
  hardening (add them as a case under `testing/cases/convert/` to pin the alias behavior).
- `dotnet test testing/` green.

## Risk

Low. Finding 1 adds a previously dead path; finding 2 is a behavior-preserving refactor.
