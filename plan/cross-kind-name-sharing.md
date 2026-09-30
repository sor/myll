# Cross-kind name sharing dialect (`AllowCpp`)

Status: **planned**. Decided in the 2026-09-30 review session of the unpushed commits. The strict half is already implemented: `ValidateModuleDecls` (`backend/Resolver/Resolver.cs`) rejects cross-kind name sharing via `CollisionKind.KindMismatch`, both within one module and across imported modules. See `testing/cases/same_module_redefinition_fail/` and `testing/cases/cross_module_clash_fail/`.

## Problem

Strict Myll rejects a name naming both a type (class/struct/union/enum) and a function/variable in the same namespace. C++ allows exactly that: the class name is hidden by the ordinary name and must be referenced through an elaborated type specifier (`struct X`, `class X`, `enum E`). Myll has no elaborated type specifier syntax, so the strict rejection is the safe default: Myll source cannot express the disambiguation, and users must rename instead.

For porting C++ code (e.g. POSIX-style `struct stat` + `stat()` pairs) a dialect switch should allow the C++-legal pairs and make Myll generate the elaborated specifiers on its own.

## Design

- **Switch**: a `CrossKindNameCollisions` mode on the `Dialect` record (`Reject` default, `AllowCpp`), parsed from `dialect/*.jsonc` via the existing `DialectLoader` enum handling. No loader changes needed.
- **Allowed pairs only**: tag (class/struct/union/enum) vs value (function/variable). Everything else (namespace, alias, using) stays a collision in both modes, because C++ rejects those pairs too. Encode the allowed pairs explicitly in `ClassifyCollision`, not as "any different kind".
- **Resolver**:
  - Type positions are syntactically unambiguous in Myll (`var`/`let`/`field` introduce a type; parameters, return types, casts, template arguments are type positions), so name lookup picks the `Hierarchical` leaf. `PickSingleCandidate` already prefers `Hierarchical` at non-call sites.
  - Call positions prefer the function: extend the `callSite` branch of `PickSingleCandidate` to filter to `Func`/`Structor` candidates first and fall back to `Hierarchical` only when no callable exists (constructor invocation).
  - The same rules apply within one module and across modules; `ValidateModuleDecls` gates the `KindMismatch` collision on the dialect mode.
- **Generator**: emit elaborated type specifiers (`struct X`, `class X`, `enum E`) in C++ type positions for names that are shadowed in the generated translation unit.
  - Do **not** elaborate all names unconditionally. The collision validation already knows exactly which names are shadowed and co-visible; mark those decls (or record a per-module shadowed-name set) and elaborate only marked names. Default dialect output stays byte-identical.
  - Never elaborate template classes (`class X<T>` elaboration is ill-formed). `enum E` elaboration requires a complete enum; only elaborate complete ones.
  - Value positions (calls, operands) never get a prefix.

## Verification

- Strict default: unchanged goldens; `same_module_redefinition_fail/` and `cross_module_clash_fail/` still reject.
- `AllowCpp` dialect case (new `testing/cases/cross_kind_name_sharing/` with a dedicated dialect JSONC): a class + same-named function compile, calls bind to the function, `var` decls of the type emit `struct X`-elaborated C++, clang++ `-std=c++23 -Wall -Wextra` clean.
- Same-signature decl+def and different-signature overloads stay legal in both modes (existing `cross_module_decldef/`, `cross_module_overload/`).

## Risk

Medium. The call-site fallback interacts with constructor-style calls (`X(...)` where `X` is both a class and a function: C++ resolves to the function, the ctor then needs `X()` to remain a ctor when no function exists). The elaboration needs to cover every type-emission site in the generator (`GenType` paths, template arguments, casts, base-class lists). Both are gated behind the dialect switch, so the default dialect is unaffected.