# Bug: cross-module redefinition check over- and under-reports

Status: **fixed on 2026-09-30**, except Finding 3 (open). Found in the 2026-09-29
review. Code-verified at `backend/Resolver/Resolver.cs:669-733` (pre-fix names
`IsCollidingPair` / `ValidateCrossModuleDecls`).

Resolution per finding:

1. **Cross-kind over-reporting** — kept as intentional strict behavior. Strict Myll
   has no elaborated type specifiers, so cross-kind name sharing is rejected with a
   dedicated `KindMismatch` diagnostic ("Name 'X' is used for both a ... and a ...
   in this module / across modules"). An `AllowCpp` dialect is planned to allow the
   C++-legal tag-vs-value pairs with elaborated C++ emission: see
   `plan/cross-kind-name-sharing.md`. Documented in `AGENTS.md` (Language invariants).
2. **Same-params-different-return under-reporting** — fixed. `ClassifyCollision`
   returns `ReturnTypeMismatch` whenever parameter lists are equal and return types
   differ, regardless of decl/def status. Covered by `testing/cases/cross_module_clash_fail/`
   (def+def and decl+def variants) and `ResolverTests.ImportedReturnTypeMismatch_ProducesDiagnostic`.
   The check was also extended to same-module duplicates (`ValidateModuleDecls`,
   `testing/cases/same_module_redefinition_fail/`).
3. **Signature equality via generated strings** — still open; see
   `enhancement-signature-equality.md`.

## Known limitations (documented scope)

- **Link-time-only collisions.** Two modules that end up linked into the same binary but are never co-visible from one module (no common importer) are **not** reported; Myll emits both definitions and the linker reports the multiple-definition error. This mirrors translation-unit semantics on purpose (a symbol duplicated per TU is legal per-TU); catching it Myll-side would be new work, not a regression.
- Same-module duplicate variables additionally produce the `TypeChecker` "Duplicate variable/field declaration" diagnostic alongside the redefinition error (both fire, by design).
- No dedicated positive test for the link-safe duplicate edge (e.g. two `[hide]` vars with the same name across modules).
- `IsLater` compares file basenames; two files with the same basename in different directories fall back to line order (only affects which decl gets the error anchor).

The original findings are recorded below.

## Finding 1: over-reporting — different kinds collide

`IsCollidingPair` returns `true` whenever the two kinds differ
(`Resolver.cs:671`: `if( !SameKind( a, b ) ) return true;`). So a `class foo` in module A and a
`func foo()` in module B are reported as "Redefinition of 'foo'". C++ allows a class and a function
to share a name (they live in different name spaces).

## Finding 2: under-reporting — same parameters, different return types

`FunctionSignaturesEqual` (`Resolver.cs:702-716`) compares `retType.Gen()` strings and returns false
when the return types differ. Two cross-module functions with **identical parameter lists** but
different return types are therefore treated as ordinary overloads, both are emitted, and C++
rejects the result. Repro (two modules):

```myll
// a.myll
func f( int x ) -> int { ret = x; }
// b.myll
func f( int x ) -> void { }
```

## Finding 3: signature equality via generated strings

`FunctionSignaturesEqual` / `StructorSignaturesEqual` compare `type.Gen()` output, which depends on
dialect pointer templates and formatting. Distinct types that format identically compare equal; the
semantics silently diverge from the generator's structural `HasSameSignature`
(`backend/Generator/HierachicalGen.cs:1294-1305`). See `enhancement-signature-equality.md` for the
shared helper.

## Fix plan

1. In `IsCollidingPair`, allow cross-kind name sharing only for the combinations C++ permits
   (type vs function, type vs variable, namespace vs anything — encode the allowed pairs
   explicitly instead of `!SameKind → true`).
2. For two `Func`s: collide when the **parameter lists** are structurally equal, regardless of return
   type. Any return-type difference is a C++ redefinition error, not an overload.
3. Use the shared structural signature helper from `enhancement-signature-equality.md` instead of
   `Gen()` string comparison.

## Verification

- Class + function sharing a name across modules: accepted (no diagnostic).
- Same parameters, different return type across modules: diagnostic.
- Same signature declaration + definition across modules: still merged silently (existing behavior,
  covered by `testing/cases/cross_module_decldef/` and `cross_module_overload/`).
- Extend `testing/cases/cross_module_clash_fail/` with the two new scenarios; update goldens.
- `dotnet test testing/` green.

## Risk

Medium. Tightens and loosens the check at the same time; the existing clash/decldef/overload cases
are the safety net.
