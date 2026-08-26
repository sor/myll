# Module System vs. C++ Headers

## Problem

C++ headers have fundamental problems:
- Textual inclusion (`#include`) is order-dependent.
- Include guards are boilerplate.
- Preprocessor macros leak globally.
- Circular dependencies are common and hard to debug.
- Compilation times scale poorly with header depth.

## Solution

Myll replaces `#include` with a proper module system while still generating `.h`/`.cpp` files.

### Declaration

```
module my_module;
```

### Import

```
import other_module;
```

## How It Works

1. All `.myll` files declaring the same module merge into a single logical module. A module is not a namespace; module names do not create scopes.
2. The compiler generates `my_module.h` and `my_module.cpp`.
3. Imports resolve names against the parsed module, not by text inclusion.
4. Order independence is guaranteed — the module interface is the complete set of non-hidden exports.
5. Cyclic imports are allowed; a fixed-point resolver resolves names across the import graph.

## Why Keep `.h`/`.cpp` Output?

Instead of binary modules (like C++20 modules):
- Generated C++ can be inspected, debugged, and hand-modified.
- Interop with existing C++ toolchains is seamless.
- No lock-in: a Myll project can be "compiled to C++ once" and then maintained in C++.

## Implicit Modules

```
// file: utils.myll
// no module declaration
// implicit module is "utils"
```

This reduces boilerplate for small projects.

## Future Directions

- Binary module interfaces (`.bmi` equivalent) for faster builds.
- Module versioning and dependency management.
- Finer export controls beyond `[hide]`/`[hidden]`.

## Implementation Notes

- `ProbeModule` discovers modules from parsed files.
- Parallel parsing is supported for independent files.
- Per-module AST + scope-tree building is parallel across modules.
- Cross-module name resolution uses a fixed-point resolver. See `plan/semantic-analysis.md`.
