# Bug: `#line` source-map directives are misplaced and desynchronize

Status: **fixed 2026-10-04** (findings 1 and 2, option B — one mechanism). Finding 3
(lambda bodies) stays deferred. Regression case: `testing/cases/source_map/`
(goldens regenerated; consecutive and gapped fields added).

## Fix (2026-10-04): declaration stamps instead of manual directives

Chosen over patching the two bugs separately: declarations now carry their source position
directly — `MarkDeclSourceMap` (the renamed `PrependDeclSourceMap`) stamps `SrcPos` onto the
declaration's first emitted line (shared buckets accumulate multiple declarations, so callers
capture the index before adding their lines). `SourceMapGenerator.Apply` — which already handles
statement lines this way — turns stamps into directives, so:

- Finding 1 is fixed by construction: directives precede their line.
- Finding 2 is fixed by construction: one counter, no manual directives to parse, no
  relative-vs-absolute path reconciliation.
- Redundant directives disappear: consecutive declarations on consecutive source lines map
  automatically without emitting anything, and namespaces no longer produce stray
  prototype-bucket directives.

The `#line` density in the goldens drops accordingly; `NormalizeFileDirectives` stays as the
harmless post-pass.

## Finding 3: lambda bodies lose per-line positions

`Lambda.Gen` (`backend/Core/Expr.cs:391-415`) joins body lines into one string, so in statement
mode the whole lambda body maps to the enclosing statement's line. See also
`bug-lambda-holes.md` (finding 4).

**Accepted for now (2026-09-30)**: lambda bodies sticking to their first line is considered fine
until the other source-map issues matter.

## Verification

- Regenerate `testing/golden/source_map/` and review the diff: every `#line` must appear **before**
  the line it describes, and consecutive fields must report their own lines.
- Add a case with a multi-field class plus a 4-line function signature (the desync trigger) and
  assert the mapping.
- `dotnet test testing/` green.

## Risk

Low — diagnostics only, no semantic change. Expect golden churn in the `source_map` case.
