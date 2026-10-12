# Member Variable Template [static] Requirement + Const Semantics Unification

Status: **planned 2026-10-09, ready to execute** (agreed with the author; not yet
implemented). Follows phase F (`plan/template-specialization.md`).

## Background

Phase F made member variable templates work by emitting them implicitly
`static` (C++ only allows static member templates; both compilers reject the
non-static form). The implicit static is exactly the kind of magic Myll rejects
elsewhere: `ValidateVarAttributes` already requires `[static]` for `[inline]`
and `[ct]` on fields. Decision: require `[static]` explicitly on every member
variable template — primary and specialization alike — and reject the missing
keyword with a clean error.

The second finding: the two const spellings behaved differently. `const T x<U>`
(kind Const) triggered the implicit-`[ct]` rule (`AddVar`, no scope guard) and
emitted `constexpr`, while `field const T x<U>` (kind Field, const type
qualifier) emitted plain `const`. The spec-side emitters (`EmitVarSpecialization`,
`EmitMemberVarSpec`) already treat a const type qualifier as `[ct]`. This change
unifies all of them: const means constexpr for variable templates, in any
spelling.

## Semantics (settled 2026-10-09)

| Spelling | Emission |
|---|---|
| `[static] const T x<U>` / `[static] field const T x<U>` | `inline static constexpr T x` — implicit `[ct]`, both spellings identical |
| `[static] var T x<U>` / `[static] field T x<U>` | `inline static T x` — runtime |
| `[static,ct] var T x<U>` / `[static,ct] field T x<U>` | `inline static constinit T x` — constinit, explicit `[ct]` (ct cannot be inferred) |
| `[static] const int x` (no template parameters) | unchanged — plain `static const int x` |

- "If you don't want constexpr, don't use const."
- The implicit-`[ct]` rule extension also applies at module scope: `var const T x<T>`
  becomes constexpr (was runtime). No existing case uses that spelling (verified);
  deliberate widening per the quoted rule.
- The implicit `static` emission is removed — the resolver check is authoritative.

## Exact changes

1. **`backend/Resolver/TypeChecker.cs` — `ValidateVarAttributes`** (inside the
   `isInsideStruct` branch, next to the existing `[inline]`/`[ct]` checks):
   ```csharp
   if( isInsideStruct
    && ( vd.TplParams.Count > 0 || vd.TplSpec.Count > 0 )
    && !isStatic )
       AddError( vd, "Member variable templates must be [static]: C++ only allows static member templates." );
   ```
   Stage: resolver (`TypeChecker.Validate` runs at `ValidateTypes`, after the
   `SpecTransformer`, so existing template_fail member-var markers fire first).
   `TplSpec` survives the transform untouched; `TplParams` is the post-transform
   unbound list — either member signals a member variable template.

2. **`backend/Generator/HierachicalGen.cs` — `AddVar`**:
   - `bool isStatic = obj.IsStatic;` — drop `|| (isInsideStruct && obj.TplParams.Count > 0)`.
   - Implicit-`[ct]` rule (line ~635) gains the const-qualifier arm:
     ```csharp
     bool isCompileTime = obj.IsCompileTime
         || ( obj.TplParams.Count > 0
           && ( obj.kind == VarDecl.Kind.Const
             || obj.kind == VarDecl.Kind.Let
             || ( obj.type.qual & Qualifier.Const ) != 0 ) );
     ```

3. **Tests**:
   - `testing/cases/class_templates/spec.myll` — Holder: primary
     `[static] field const T x<U> = (U)0;`, spec `[static] const T x<T=int, U=int> = 7;`
     (one spelling each, both constexpr in the golden → pins the equivalence).
     Optionally add a `[static,ct] var T z<U> = (U)0;` member + check for the
     constinit row. Extend `run4`.
   - `testing/cases/template_fail/main.myll` — prefix `[static]` on every decl
     of the member-var block (MVBox/MVOpen/MVSpec) so existing markers stay
     isolated; add two new pins:
     - primary missing `[static]` → "Member variable templates must be [static]"
     - spec missing `[static]` beside a `[static]` primary → same error
   - Goldens regenerate (`class_templates` only; Holder primary flips
     `static const T x` → `static constexpr T x`, spec emission was already constexpr).

4. **Docs**:
   - `plan/template-specialization.md` phase F section: "implicitly static" →
     "must be `[static]`" + the const semantics table.
   - `AGENTS.md` phase F entry: same correction.

## Execution checklist

1. TypeChecker check
2. AddVar changes
3. Build, fix compile errors
4. Update both test files
5. `dotnet test testing/` — expect golden mismatches only for `class_templates`
6. Regenerate `class_templates` golden
7. Full suite green (157/157)
8. Commit sequence: impl (both files together), tests (+goldens), docs.
   Commit message notes the const-semantics unification explicitly.

## Verification points

- `[static] field const T x<U>` in a NON-template class still emits
  `inline static constexpr T x` (the `[static] const` member with implicit ct
  is fine: `isCompileTime` → `needsInline` → in-class constexpr definition).
- The new TypeChecker error and the transformer errors share the resolver
  stage; template_fail stays single-stage.
- No `std/` or `myll/` module declares a field-scope variable template
  (verified 2026-10-09) — nothing external needs `[static]` added.