# Bug: attribute plumbing — shared state, ordering, missing validation

Status: **open**. Found in the 2026-09-29 review. Two repros verified.

## Findings

1. **Shared attribute dictionaries.** `AttributedNode.AssignAttribs` replaces `attribs` with the
   caller's dictionary (no copy). `Decl.ExtractAccessAttribs`
   (`backend/Core/Decl.cs:117-146`) *mutates* that dictionary by removing consumed keys.
   `MultiDecl.AssignAttribs` (`backend/Core/Decl.cs:335-338`) then fans the **same instance** out to
   every child. Consequences:
   - `var int a, b;` under one `[priv]` block: the first var consumes `priv`, the second silently
     falls back to the section default.
   - Any later `MergeAttribs` on one child mutates all siblings.

2. **`[implicit]` clobber on `ctor initlist`.** `BuildSpecialConstructor` merges `[implicit]` into the
   special ctor (`backend/Visitor/VDecl.cs:834`). If the user also writes an attribute block, the
   replace-semantics of `AssignAttribs` wipe the merged `[implicit]`; the generator then emits
   `explicit` (`backend/Generator/HierachicalGen.cs:1353-1357`) and copy-list-initialization
   `S s = {1,2};` fails deep in C++ with an unhelpful error.

3. **Dead `[default]`/`[delete]` ctor-body check.** `VisitDefCtor` checks
   `ret.IsDefault || ret.IsDisabled` at `backend/Visitor/VDecl.cs:740`, but attributes are assigned by
   the caller only *after* `VisitDefCtor` returns (`VDecl.cs:154-159`), so the check is always false.
   Verified: `[default] ctor() { }` (`tmp/review/ctor_default_body.myll`) compiles without a
   diagnostic and the body is silently dropped — the generator emits `C() = default;` and skips the
   body. The function path is covered generator-side (`AddFunc`,
   `backend/Generator/HierachicalGen.cs:740-747`); the constructor path is not.

4. **Duplicate attribute crashes the compiler.** `Visit(AttribBlkContext)` builds the dictionary with
   `ToDictionary` (`backend/Visitor/VExt.cs:321-329`). `[pure, pure] func f() -> int { ... }`
   (`tmp/review/attr_dup.myll`) throws an unhandled `ArgumentException` inside
   `CompilerPipeline.CompileModules` (parallel LINQ) — the compiler dies with a stack trace.

5. **`[a, b]:` access sections ignore extra attributes.** `VisitAttrColon`
   (`backend/Visitor/VDecl.cs:288-290`) honors only the first key and silently drops the rest.

6. **Base-class access attributes rejected.** `BuildBaseSpec`
   (`backend/Visitor/VDecl.cs:623-644`) throws "Unsupported attribute on base class: access" for
   `[access=priv]`, which `Decl.ExtractAccessAttribs` accepts for other declarations.

## Fix plan

1. Copy the dictionary in `AssignAttribs` (or hand each `MultiDecl` child a fresh copy). Make
   attribute application merge, never replace.
2. Move the `[default]`/`[delete]` body check to after attribute assignment (the `switch (ret)` in
   `VisitDecl` is the natural home), or add the equivalent diagnostic in `AddStructor` next to the
   `AddFunc` one.
3. Replace the `ToDictionary` with a manual loop that reports duplicate attributes as a diagnostic.
4. `[a, b]:` sections: honor all listed access attributes or diagnose the extras — pick one, document
   it in the spec.
5. Route base-spec attributes through `ExtractAccessAttribs` so both paths agree.

## Verification

- Repros: `ctor_default_body.myll` and `attr_dup.myll` produce clean diagnostics; a `[priv] var int a,
  b;` multi-decl gives both vars private access (add a golden check).
- `[pure, pure]` → diagnostic, no stack trace.
- `ctor initlist` with a user attribute block keeps `[implicit]` (extend `testing/cases/initializer_list/`).
- `dotnet test testing/` green; review golden diffs (the clobber fix may change existing goldens).

## Risk

Medium. The attribute pipeline is central to every declaration. The copy/merge change can only add
access/attributes that were silently dropped before, so existing goldens should be stable except
where a bug had observable output.
