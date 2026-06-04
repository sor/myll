# Modules and Namespaces

Myll replaces C++'s header/include system with a module system that still compiles to traditional `.h`/`.cpp` files.

## Module Declaration

```
module my_module;
```

### Rules

- If no module declaration is present, the filename (minus `.myll`) becomes the implicit module.
- Multiple `.myll` files can declare the same module; their contents merge.
- Output: `my_module.h` and `my_module.cpp`.

## Import

```
import other_module;
import path/to/file;    // [paths currently buggy]
```

Imports make names from another module available. There is no textual inclusion (unlike `#include`); names are resolved against the imported module's symbol table.

## Namespaces

```
namespace Graphics {
    // declarations
}

// Bodyless namespace
namespace Graphics;
```

- Namespaces can nest.
- Bodyless namespaces merge with named namespace declarations.

## Using Directives

```
using std::vector;       // bring specific name into scope
using namespace std;     // bring all names [scope-limited]
```

Myll restricts `using namespace` to non-global scopes to prevent namespace pollution.

## Future Directions

- Visibility controls on module exports (public/private module interface).
- Module partitions.
- Package management / dependency resolution.

## Implementation Notes

- `ProbeModule` in `VMain.cs` handles module discovery and grouping.
- Modules are built in two phases: parallel parsing, then ordered code generation.
