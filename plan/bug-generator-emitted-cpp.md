# Bug: generated C++ is invalid for several declaration combinations

Status: **open**. Found in the 2026-09-29 review. One repro verified with clang++.

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

Fix: reject `[extern]` on in-struct members with a diagnostic (the `AddVar` path already has this
guard at `HierachicalGen.cs:527` — mirror it for functions), or omit the `extern` keyword for
in-class declarations and keep it for the out-of-class definition. Pick one and document.

## Finding 2 (code-verified): static data member of a class template

`class Box<T> { [static] field int count = 0; }` emits the out-of-class definition
`int Box<T>::count = 0;` **without** a `template <typename T>` prefix — clang rejects it
(`'T' was not declared in this scope`). Root: `hasTpl` only inspects the variable's *own*
`TplParams` (`backend/Generator/HierachicalGen.cs:636-652`); the enclosing class's template parameters
are never prepended. The TODO at `:727` acknowledges the gap. Untested: no case, stdlib file, or
coreutil app has this combination (`[static, inline]` / `[static, ct]` work because they become
inline variables).

Fix: prepend the enclosing templated class's parameters to the impl template line.

## Finding 3 (code-verified): nested namespace alias/using stubs are missed

Forward `namespace X {}` stubs for same-file namespace aliases/usings are matched by **plain name**:
`definedNamespaces` stores `nameDecl` (`HierachicalGen.cs:1007`) while the filters compare qualified
paths (`:322-326`, `:339-343`, `:442-451`). So `alias X = Outer::Inner;` (or a Contained/Leaky
`using Outer::Inner;`) emits the alias in `protoEarly` before any `Outer::Inner` stub — a
use-before-declaration in the header.

Fix: store and match the fully qualified target name.

## Verification

- Finding 1 repro: after the fix, either a clean diagnostic or valid C++ (no `extern` in a class
  body). clang++ clean.
- Add cases: `[extern]` member (negative or positive, depending on the chosen fix), static member of
  a class template, nested-namespace alias. Update goldens.
- `dotnet test testing/` green.

## Risk

Medium for finding 2 (template header emission interacts with the `hasTpl` ladder and source-map
prepends). Low for 1 and 3.
