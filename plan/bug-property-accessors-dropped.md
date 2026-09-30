# Bug: property accessors are silently dropped

Status: **postponed** (2026-09-30). Found in the 2026-09-29 review. Reproduced.

The accessor syntax may change soon, so nothing is implemented now — not even the
stage-1 rejection diagnostic — because it would have to change either way.

## Design notes for when it returns

The backing store naming must be dialect-configurable, not hard-coded: a dialect
setting offering predefined choices or a format-string pattern, e.g. `"{0}"` (plain
`x`), `"_{0}"` (emits `_x` for accessor `x`), or `"myll_prop_{0}"`. The end user
controls what is emitted, in the same spirit as the `myll_tmp_` prefix being reserved
for the compiler — whichever form is used, it must not collide with user names.

## Symptom

`field int a { get; set; };` generates a plain `int a{};`. No accessors, no backing field, no
diagnostic. The getter/setter semantics vanish silently.

## Repro

`tmp/review/property2.myll`:

```myll
class C {
[pub]:
	field int a { get; set; };
}
```

Generated header contains `int a{};` — verified in `tmp/review/generated/property2/prop_test.hpp`.

A hand-written accessor body fails differently: `field int b { get => _b; set _b = value; };`
(`tmp/review/property.myll`) is visited, so the resolver resolves the body — and fails with
`Unresolved identifier 'value'` because the setter parameter is never added to scope. Then the
generator would drop the body anyway.

The legacy file `frontend/tests/mixed/testcase.myll:195-203` documents the intended semantics:
`{ get; set; }` creates a private backing field `_a` plus getter/setter; a hand-written accessor list
means "all manual, no backing field".

## Root cause

`HierachicalGen.AddVar` (`backend/Generator/HierachicalGen.cs:497-653`) never reads `obj.accessor`.
The `accessorDecl`/`accessorImpl` output buckets exist and are concatenated into the output, but no
`Add*` method ever writes to them. The visitor parses and stores accessors correctly
(`backend/Visitor/VDecl.cs:1148`, `backend/Visitor/VExt.cs:309-318`); the resolver resolves bodies
against the wrong scope; the generator ignores everything.

## Fix plan

Two stages:

1. **Stop the silent corruption (cheap).** Emit a diagnostic when a `VarDecl` carries a non-empty
   accessor list. Until then the syntax is accepted but rejected with a clear message.
2. **Implement (feature work).** Emit C++ accessors:
   - `{ get; set; }` → private backing field `_<name>` plus public getter/setter.
   - Hand-written list → emit the accessor bodies as methods; scope the setter's `value` parameter
     properly in the resolver; no backing field is synthesized.
   - Mixed or partial lists follow the legacy doc: if one is hand-written, all must be.

## Verification

- Stage 1: the repros produce a diagnostic with file:line:col; no generated C++ is emitted for the
  broken construct. Add a negative case under `testing/cases/` (e.g. `testing/cases/property_fail/`).
- Stage 2: port `testcase.myll:195-203` semantics into a real integration case
  (`testing/cases/property/`) with goldens; clang++ clean; the generated class exposes getters/setters
  that compile and run.
- `dotnet test testing/` green.

## Risk

Stage 1: low. Stage 2: medium — it adds a new declaration kind to the generator (backing field naming,
access inheritance, `[static]` interaction) and changes what the resolver scopes inside accessor
bodies.
