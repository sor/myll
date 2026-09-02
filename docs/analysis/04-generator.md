# Code Generator

## Files

| File | Purpose | Lines |
|------|---------|-------|
| `HierachicalGen.cs` | Main C++ generator for declarations | ~559 |
| `ExprFormatting.cs` | Operator-to-C++ format strings | ~82 |
| `StmtFormatting.cs` | Statement/type format strings | ~125 |

## What Works

### Declaration Ordering
`HierachicalGen` organizes output into:
1. Prototypes / forward declarations
2. Type definitions (enums, structs, classes)
3. Static members
4. Fields
5. Constructors
6. Destructor
7. Accessors / properties
8. Operators
9. Methods / functions

This produces clean, readable C++ headers.

### Access-Level Bucketing
Declarations are bucketed by `private`, `protected`, `public`, producing proper C++ access specifier sections.

### Multi-File Output
Module declarations are merged and output as `.h` / `.cpp` pairs.

## Missing Emission Paths

| Feature | Parser | AST | Generator | Status |
|---------|--------|-----|-----------|--------|
| Property accessors | Yes | Partial | **No** | Parsed, not emitted |
| Operator overloading | Yes | Yes | **No** | Parsed, AST built, no `AddOperator` |
| Try/catch | Yes | No | No | No AST to generate |
| Defer | Yes | No | No | No AST to generate |
| Subclasses | Yes | Partial | **Partial** | Inheritance parsed, generation incomplete |
| Global variables | Yes | Yes | **Partial** | `extern`/`inline` strategy not finalized |

## Code Quality Issues

### Memory Inefficiency
Author acknowledges: "Super memory inefficient but I don't care for the moment."
- Intermediate `List<string>` allocations for generation strings.
- `AccessStrings` materialization.

### Complex Conditional Logic
`AddFunc` has tangled logic for:
- Inline vs. external placement
- Inside-struct vs. outside-struct placement
- Virtual/override qualifiers

This is brittle and hard to modify.

### Hardcoded C++ Standard Library
`Typespec.cs` hardcodes:
```csharp
{ Pointer.Kind.UniquePtr, "std::unique_ptr<{0}>" },
{ Pointer.Kind.SharedPtr, "std::shared_ptr<{0}>" },
```

This makes retargeting impossible.

### Resource Leaks
`StreamReader` in `frontend/Program.cs` is never disposed.

## Formatting Gaps

- `BasicFormat` map is incomplete (`Size` type is TODO).
- `char8_t` is noted as TODO.
- `<string>` include is commented out in `DefaultIncludes` despite `string` mapping to `std::string`.

## Future Outlook

### Short-Term
1. Implement accessor/property emission.
2. Implement operator overloading emission (`AddOperator`).
3. Finalize global variable strategy: `extern` in `.h` + definition in one `.cpp`, vs. `inline` everywhere.

### Medium-Term
4. Extract C++ standard library names into a configurable dictionary.
5. Add support for generating functions above variables (required by C++ single-pass parsing).
6. Refactor `AddFunc` conditional logic into strategy objects or simpler branches.

### Long-Term
7. Introduce `ICodeEmitter` interface to decouple from C++.
8. Support multiple output backends (C, LLVM IR, other languages).
9. Implement automatic include generation (e.g., use `std::vector` → auto-include `<vector>`).

## Current Generated Output Example

Input (`stack.myll`):
```
[pub]
struct Stack<T> {
    [priv] field T[*] data;

    [pub] ctor() { }
}
```

Output (`stack.h`):
```cpp
#pragma once

#include <vector>

template <typename T>
struct Stack {
private:
    std::vector<T> data;
public:
    Stack();
};
```
