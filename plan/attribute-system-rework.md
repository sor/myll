# Attribute system rework

Status: **planned**; Stage 0 (immediate fixes) **implemented** 2026-09-30. Follow-up to the 2026-09-29
review (`plan/bug-attribute-handling.md`) and the 2026-09-30 design session.

## Problem

- `[x]` attribute blocks work, but `[x]:` sections only work for **access** attributes
  (`VisitAttrColon` reads one key and drops the rest), even though the grammar allows sections for
  every declaration kind (seven `attrX` rule pairs).
- Attributes cannot be **reset**: `MergeAttribs` is first-write-wins, so a later `[mutable]` cannot
  override an earlier `[pure]`, and a `[pure]:` section cannot be opted out per declaration.
- Attribute knowledge is scattered: ~25 boolean `IsX` properties on `AttributedNode`, dialect
  defaults, per-transform special cases, validation spread across resolver/checker/generator — no
  single source of truth.
- The attribute dictionaries are shared mutable state (fixed copy-on-assign in Stage 0, replaced
  entirely in Stage 2).

Decisions (2026-09-30): **group-override, last/most-specific wins** for reset semantics. Future
extensions to support: cross-attribute forbidding (`[pod]` on a struct forbids `[virtual]` on its
members) and dialect-level `forbid` of attributes per declaration kind.

## Stages

### Stage 0 — immediate fixes (implemented)

Copy-on-assign in `AttributedNode.AssignAttribs` (the `MultiDecl`/`MultiStmt` fan-outs then hand
every child its own dictionary, and `ExtractAccessAttribs` only mutates the node's own copy);
`[default]`/`[delete]` ctor with a body errors after attribute assignment; duplicate attributes
warn instead of crashing. See `plan/bug-attribute-handling.md` findings 1/3/4.

### Stage 1 — attribute registry

One `AttributeDefinition` table in Core: name and aliases, arity form (flag / `=value` /
`(args)`), applicable declaration kinds, mutual-exclusion groups, reset semantics, dialect-default
slot. Validation (unknown attributes warn, group violations error, arity/applicability checked),
the dialect defaults, and parsing all read it. Reserved future extension: constraint rules between
definitions (an attribute on a container forbids attributes on members) and dialect-level forbid
lists per declaration kind.

### Stage 2 — immutable Attribs

`Attribs` becomes a readonly value type with pure operations (`With`, `Without`, `Merge` with an
explicit override policy). `Assign`/`Merge` become ownership transfers / total merges; the four
in-place `Remove`s become `Without`. Shared-instance bugs become structurally impossible.

### Stage 3 — uniform `[x]` / `[x]:` semantics

Sections apply **all** attributes as an overlay for the declarations that follow until the next
section; per-declaration attributes override the section **within the same group** (group-override,
last wins), cross-group merges additively, and dialect defaults are the lowest layer
(dialect < section < decl). The seven duplicated `attrX` grammar rule pairs collapse into one
generic form (`enhancement-grammar-cleanup.md` finding 3 lands here).

### Stage 4 — emission and defaults on the new model

The generator and all special cases (enum transformer attribute injection, `[implicit]` merge for
`ctor initlist`, base-spec attributes) route through the registry/immutable model; dialect
defaults merge as the lowest layer with group-override.

### Stage 5 — tests and docs

Negative cases for registry validation (unknown attribute, group violation, arity, applicability,
dialect forbids); positive cases for sections on non-access attributes, per-decl override of
sections, dialect-default override; regenerate goldens. Rewrite the Attributes section in
`docs/design/02-myll-language.md` with the section/reset semantics.

## Verification

- Stage 0: `tmp/collision_check/attr_priv/` repro keeps `priv` on `b`; new golden case for struct
  multi-decl; `decl_and_attributes_fail` gains the `[default] ctor() { body }` marker; a unit test
  for the duplicate-attribute warning; full suite green.
- Stages 1-5: per-stage cases; goldens regenerate only where the new semantics apply; the existing
  attribute-related cases (`decl_and_attributes_fail`, `rule_of_n`, `special_members`, `convert`,
  dialect cases) must stay green throughout.