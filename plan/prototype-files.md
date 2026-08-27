# Prototype declaration files

Myll needs a way to declare C++ entities for the resolver without emitting definitions. This document pins down the design.

## Prototype file extensions

Files ending in `.d.myll` or `.decl.myll` are **prototype files** (like D's `.di` files or TypeScript `.d.ts`).

- They are parsed and resolved like normal `.myll` files.
- They **produce no generated output** (no `.h`/`.cpp`).
- The `[extern]` attribute inside them is optional; the file extension already means "declaration only."
- `.extern.myll` is kept as a temporary legacy alias and will be removed.

Example `extern/std.decl.myll`:

```myll
module std;
namespace std {
    func getenv(string name) -> char*;
    class vector<T>;
}
```

## Inline `[extern]` in normal `.myll` files

In a normal `.myll` file, `[extern]` still means "forward declaration" and generates matching C++:

| Myll | Generated C++ |
|---|---|
| `[extern] func foo();` | `extern void foo();` |
| `[extern] namespace Ns { func foo(); }` | `namespace Ns { extern void foo(); };` |
| `[extern] class A;` | `class A;` |
| `[extern] class B { func C(); }` | `class B;` |

For an inline `[extern]` class, the generator emits only the forward declaration. The resolver still sees the class and its members, so Myll code can resolve `B::C` and call it; the real definition must come from a C++ header the user includes.

Children of an `[extern]` class or namespace inherit the external flag, so they are not emitted as definitions.

## Implementation checklist

- [ ] Update `Program.CollectExternFiles` to discover `.d.myll` and `.decl.myll` (keep `.extern.myll` as legacy).
- [ ] Add `CompilationContext.IsPrototypeFile` and pass it from input file extension.
- [ ] Skip `GenerateFiles` for prototype modules.
- [ ] Extend `[extern]` propagation from namespaces to `Structural` (class/struct/union).
- [ ] Remove `Namespace.AddToGen` early return on `IsExternal` so inline extern namespaces emit member forward declarations.
- [ ] Make `Structural.AddToGen` emit a forward declaration when `IsExternal`.
- [ ] Rename test extern files to `.decl.myll` and add tests for inline `[extern]` class.
