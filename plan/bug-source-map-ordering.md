# Bug: `#line` source-map directives are misplaced and desynchronize

Status: **open**. Found in the 2026-09-29 review. Code-verified; visible in existing goldens.

## Finding 1: the "prepend" appends

`PrependDeclSourceMap` (`backend/Generator/HierachicalGen.cs:178-193`) appends via `target.Add`
despite its name. `AddVar` (`:632`, `:650`) and `AddEntry` (`:429`) call it **after** adding the
declaration line, so the `#line` lands *after* the declaration it describes. Visible in
`testing/golden/source_map/source_map.hpp`: `#line 5` appears after `int value = 10;`. In
decl/full source-map mode every variable and enum-entry line maps one source line late, and its
directive instead applies to the *next* emitted line — two consecutive fields report each other's
lines.

Fix: call `PrependDeclSourceMap` before adding the line to the bucket (or make it truly insert at
index 0 and rename accordingly).

## Finding 2: `SourceMapGenerator.Apply` ignores manually prepended directives

`Apply` (`backend/Generator/SourceMapGenerator.cs:27-49`) tracks state only from lines that carry a
`SrcPos`. Manually prepended `#line` directives (`SrcPos == null`, from Finding 1's path) are neither
parsed nor accounted for. When the tracked `expectedLine` happens to equal the next mapped line's
source line, `Apply` skips its own directive and the statement maps to the wrong line (off by ≥ 1).
Trigger: a function whose generated block has 4 (`.cpp` non-template) or 5 (`.hpp` template)
no-`SrcPos` lines after the previous function's last mapped line.

Fix: have `Apply` parse existing `#line` lines (reuse `DirectiveRegex`) and update its state from
them, so manual and automatic directives share one counter.

## Finding 3: lambda bodies lose per-line positions

`Lambda.Gen` (`backend/Core/Expr.cs:391-415`) joins body lines into one string, so in statement
mode the whole lambda body maps to the enclosing statement's line. See also
`bug-lambda-holes.md` (finding 4).

## Verification

- Regenerate `testing/golden/source_map/` and review the diff: every `#line` must appear **before**
  the line it describes, and consecutive fields must report their own lines.
- Add a case with a multi-field class plus a 4-line function signature (the desync trigger) and
  assert the mapping.
- `dotnet test testing/` green.

## Risk

Low — diagnostics only, no semantic change. Expect golden churn in the `source_map` case.
