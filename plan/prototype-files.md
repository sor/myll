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

## Forward declarations in `.decl.myll` and `[extern]` contexts

A forward declaration (no body, just a trailing `;`) should be valid **only** in prototype files or when marked `[extern]`:

```myll
// .decl.myll or [extern]
namespace ns;
class A;
struct S;
union U;
enum E;       // C++ only allows this with explicit underlying type; relax for Myll shims
func foo();
method bar(); // when methods are supported
```

In normal `.myll` files unqualified bodyless functions are not allowed, except for future OOP special member functions (`= 0`, `= default`, `= delete`, abstract/virtual methods).

### Implementation plan

1. Reuse existing body rules for forward declarations:
   - `defStruct`: make the body optional (`class A;`)
   - `defEnum`: make the body optional (`enum E;`)
   - `defFunc`: a function body that is a single empty statement (`func foo();`)
   - `defNamespace`: `namespace N;` is a forward declaration; `namespace N:` remains the bodyless scoped form
2. Build regenerates ANTLR output automatically.
3. Mark forward declarations in the AST (`Decl.IsForwardDeclaration`; `Namespace` also uses `withBody = false` and `IsForwardDeclaration`).
4. Allow them only when:
   - the file is a prototype (`.d.myll` / `.decl.myll` / `.extern.myll`), **or**
   - the declaration or an enclosing hierarchical is `[extern]`.
5. Emit matching C++ forward declarations in the generator.
6. Update the `std/` shims to use the new syntax.

## OOP special member functions (deferred)

Future support is needed for methods without a body for C++ OOP features:

- pure virtual: `func draw() -> void = 0;`
- defaulted:   `ctor() = default;`
- deleted:     `func copy() = delete;`
- abstract methods in interfaces/aspects

These are intentionally out of scope for the first prototype-file pass.

## Implementation checklist

- [x] Update `Program.CollectExternFiles` to discover `.d.myll` and `.decl.myll` (keep `.extern.myll` as legacy).
- [x] Add `CompilationContext.IsPrototypeFile` and pass it from input file extension.
- [x] Skip `GenerateFiles` for prototype modules.
- [x] Extend `[extern]` propagation from namespaces to `Structural` (class/struct/union).
- [x] Remove `Namespace.AddToGen` early return on `IsExternal` so inline extern namespaces emit member forward declarations.
- [x] Make `Structural.AddToGen` emit a forward declaration when `IsExternal`.
- [x] Add a `GenerateOnly` integration-harness flag and an end-to-end golden test for inline `[extern]` class/namespace.
- [ ] Add grammar/visitor/generator support for forward declarations in prototype and `[extern]` contexts.
- [ ] Convert the `std/` shims to use forward-declaration syntax.
- [ ] Rename test extern files to `.decl.myll` and add tests for `.decl.myll` prototype files.
