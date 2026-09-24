# Modules and Namespaces

Myll replaces C++'s header/include system with a module system that still compiles to traditional `.h`/`.cpp` files.

## Module Declaration

```
module my_module;
```

### Rules

- If no module declaration is present, the filename (minus `.myll`) becomes the implicit module.
  This means a file named `my_module.myll` can omit the `module my_module;` line.
- Multiple `.myll` files can declare the same module; their declarations merge into one logical module.
- A module is a translation unit, not a namespace. The module name does not create a scope.
- Output: `my_module.h` and `my_module.cpp`.

```myll
// File: utils.myll
// The module name is implicitly "utils"; no `module utils;` line is required.
func helper() -> int { return 42; }
```

## Import

```
import other_module;
import path/to/file;    // [paths currently buggy]
```

Imports make names from another module available. There is no textual inclusion (unlike `#include`); names are resolved against the imported module's exported declarations.

- Imports are order-independent inside a module.
- Cyclic imports are allowed.
- A module must be imported before its names are visible; importing is not automatic.

## Visibility

By default every declaration in a module is visible to any module that imports it.
Declarations marked `[hide]` or `[hidden]` are not exported.

```myll
[hide] func internalHelper() -> void;
```

## Namespaces

```
namespace Graphics {
    // declarations
}

// Bodyless namespace
namespace Graphics:

// Forward namespace declaration
namespace Graphics;
```

- Namespaces can nest.
- Bodyless namespaces merge with named namespace declarations.
- Because modules do not provide namespace isolation, use explicit `namespace` blocks to avoid name collisions across modules.

## Using Directives

```
using std::vector;       // bring specific name into scope
using std;               // bring all names from the namespace into the current scope
```

Myll writes `using std::vector;` and `using namespace std;` in C++. When the target resolves to a namespace, `using NS;` becomes `using namespace NS;`; when it resolves to a specific declaration, it becomes `using NS::name;`.

Using directives are currently supported inside function, namespace, and class/struct/union bodies. Global module-level `using namespace` is restricted to prevent namespace pollution.

## Prototype files and dialect-aware names

Prototype declaration files (`.decl.myll` / `.d.myll`) describe existing C++ APIs. Because they are resolved by the same name lookup as normal code, be careful with dialect-mapped keywords such as `string`, which resolves to `Dialect.StringClassName` rather than always meaning `std::string`.

When a prototype binds an API that literally uses `std::string`, write the type as `std::string`. Bare `string` is only correct when the prototype should track the active dialect's string type.

## Future Directions

- Module partitions.
- Package management / dependency resolution.
- Finer export controls beyond `[hide]`/`[hidden]`.

## Implementation Notes

- `ProbeModule` in `VMain.cs` handles module discovery and grouping.
- The full pipeline is described in `plan/semantic-analysis.md`:
  1. Parse files in parallel.
  2. Group by module.
  3. Build per-module AST + scope tree in parallel.
  4. Resolve names across modules, including cyclic imports, via a fixed-point resolver.
  5. Generate C++.
