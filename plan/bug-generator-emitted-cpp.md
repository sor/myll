# Bug: generated C++ is invalid for several declaration combinations

Status: **fixed 2026-10-03**. Regression cases: `testing/cases/extern_member_fail/`,
`testing/cases/static_template_member/`, `testing/cases/ns_alias_stubs/`.

## Finding 1 (verified): `[extern]` member function inside a non-extern class

`tmp/review/extern_member.myll`:

```myll
class C {
[pub]:
	[extern] func foo() -> int;
}
```

Generated header:

```cpp
class C {
public:
    extern int foo();   // clang: error: storage class specified for a member declaration
};
```

The `extern ` prefix is built unconditionally
(`backend/Generator/HierachicalGen.cs:685-690`, `:785-792`) and the in-struct path declares the
function inside the class body, where `extern` is invalid C++. On the same path a provided body is
silently discarded (guarded by `if( !isExternal )` at `:794`).

Fix (done 2026-10-03): reject `[extern]` on in-struct member functions with the same diagnostic
the `AddVar` path uses (`[extern] is only valid at module/namespace scope.`). External member
declarations belong in prototype modules or extern classes.

## Finding 2 (code-verified): static data member of a class template

`class Box<T> { [static] field int count = 0; }` emits the out-of-class definition
`int Box<T>::count = 0;` **without** a `template <typename T>` prefix — clang rejects it
(`'T' was not declared in this scope`). Root: `hasTpl` only inspects the variable's *own*
`TplParams` (`backend/Generator/HierachicalGen.cs:636-652`); the enclosing class's template parameters
are never prepended. The TODO at `:727` acknowledges the gap. Untested: no case, stdlib file, or
coreutil app has this combination (`[static, inline]` / `[static, ct]` work because they become
inline variables).

**Decision (2026-09-30):** do not emit the `template <typename T>` out-of-line definition at all.
Static members of a class template should be **`inline` by default** (inline variables), so the
in-class definition suffices and no out-of-line template header is needed — same as the working
`[static, inline]` / `[static, ct]` combinations above, just applied automatically when the
enclosing structural is a template. Implemented 2026-10-03 via `IsTemplateUp` in `AddVar`'s
`needsInline` ladder.

**Intermediate-future intent:** an attribute to opt into out-of-line generation for a static member
(e.g. `[outline]`). Only then does the generator need to prepend the enclosing template's `template
<typename T>` header to the out-of-line definition; the current bug disappears with the inline
default and would return only for that opt-in.

## Finding 3 (code-verified): nested namespace alias/using stubs are missed

Forward `namespace X {}` stubs for same-file namespace aliases/usings were matched by **plain
name**: `definedNamespaces` accumulated plain names while the filters compared qualified paths —
and the accumulated set was split across generator instances besides, because nested namespaces
register on nested gens. So `alias X = Outer::Inner;` emitted the alias before any `Outer::Inner`
stub — a use-before-declaration in the header.

Fix (done 2026-10-03): collect the fully qualified names of all namespaces from the AST at
`GenDecl` time (`CollectNamespaceNames`, scoped per gen), store using targets qualified as well,
and emit the stub as a qualified `namespace Outer::Inner {}` (valid C++17+).

## Verification

- Finding 1 repro: after the fix, either a clean diagnostic or valid C++ (no `extern` in a class
  body). clang++ clean.
- Add cases: `[extern]` member (negative or positive, depending on the chosen fix), static member of
  a class template, nested-namespace alias. Update goldens.
- `dotnet test testing/` green.

## Risk

Medium for finding 2 (template header emission interacts with the `hasTpl` ladder and source-map
prepends). Low for 1 and 3.
