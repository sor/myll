# Contained global `using NS` plan

## Goal

With `Dialect.GlobalUsingNS = GlobalUsingNSMode.Contained`, a module-scope `using NS;` of a namespace must not emit `using namespace NS;` in the generated header. It should be emitted only in the implementation (`.cpp`) file, and all names resolved through that `using` in the header must be emitted fully qualified.

## Current state

- `Dialect.GlobalUsingNS` enum exists with `Leaky`, `Contained`, `Disabled`.
- `Leaky` emits `using namespace NS;` in the header and works thanks to a forward-namespace stub for same-file namespaces.
- `Disabled` rejects global namespace `using` with a diagnostic.
- `Contained` currently produces a diagnostic saying it is not implemented.

## Approach

### 1. Track module-scope namespace `using` declarations

In the resolver, when a `UsingDecl` is resolved to a namespace and its scope is a `GlobalNamespace`, record it in a list associated with the module.

### 2. Do not emit the `using namespace` in the header for Contained mode

In `HierachicalGen.AddUsing`, when `Dialect.GlobalUsingNS == GlobalUsingNSMode.Contained` and the `UsingDecl` is a namespace-using at module scope:

- Do not add `using namespace NS;` to `protoEarly`.
- Instead add it to a new cpp-only list (e.g. `cppOnlyUsings`).

Scoped `using` inside a namespace or class should still be emitted in the header, because the name leakage is contained to that scope.

### 3. Emit the `using namespace` lines in the `.cpp` file only

In `HierachicalGen.GenImplGlobal`, output the strings stored in `cppOnlyUsings` after the include of the corresponding header.

### 4. Fully qualify names in the header

This is the hard part. The resolver already knows which declaration an unqualified identifier resolves to. The generator must use the fully qualified name for identifiers that were resolved through a module-scope namespace `using`.

Options:

- **Option 4a — always use fully qualified names when resolvedDecl is outside the current module scope.**
  - Change `IdExpr.Gen` to prefer `resolvedDecl.FullyQualifiedName` over `name` when `resolvedDecl` is defined in a different namespace/module.
  - Pros: robust, no need to track `using` usage per identifier.
  - Cons: changes a lot of generated output (more verbose), might change golden files for many cases.

- **Option 4b — mark identifiers resolved via a global using.**
  - During resolution, when an identifier is looked up and one of the candidate scopes was imported through a namespace `using`, flag the identifier.
  - The generator uses `FullyQualifiedName` only for flagged identifiers.
  - Pros: minimal output changes.
  - Cons: requires passing extra metadata through the AST or resolution result.

### 5. Test plan

Add a test case that runs with `Dialect.GlobalUsingNS = GlobalUsingNSMode.Contained`:

```myll
module contained_using_test;

namespace NS {
    func value() -> int { return 42; }
}

using NS;

func check_contained() -> int
{
    return value() == 42 ? 0 : 1;
}
```

Expected generated header:

```cpp
namespace NS { int value(); }

inline int check_contained()
{
    return NS::value() == 42 ? 0 : 1;
}
```

Expected generated cpp:

```cpp
#include "contained_using_test.hpp"
using namespace NS;
```

Also test an imported namespace:

```myll
module contained_using_test;
import ns_helper;
using NSHelper;

func check() -> int { return helperValue() == 42 ? 0 : 1; }
```

In the header the call should become `NSHelper::helperValue()`; in the cpp `using namespace NSHelper;` is emitted.

## Open questions

- Should `using NS;` inside a namespace also be affected by `Contained` mode? The enum name says "global", so scoped `using` is likely always "Leaky" but within a contained scope. We can keep scoped `using` emitting in the header for now.
- Should an earlier `using` resolved in a function body (in the header) also be qualified? Yes, any usage in the header must be qualified because the `.cpp` `using namespace` is not visible to header consumers.
- How does this interact with inline/template functions whose bodies are emitted in the header? They must also use qualified names, so Option 4a is safer.

## Recommended first step

Prototype Option 4a locally: make `IdExpr.Gen` use `resolvedDecl.FullyQualifiedName` whenever `resolvedDecl` is in a different namespace/module. See how many golden files change. If the diff is small, Option 4a is the right choice. If large, switch to Option 4b.
