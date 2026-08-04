# Salvage from oldCodeGen

`oldCodeGen/` is an earlier C++ code generator built around an in-memory model of namespaces, classes, fields, constructors, methods, and enums.
It is simpler than the current backend generator, but it has a clean separation between declaration and definition views and a more complete enum emitter.
This file records the parts worth reusing.

## What oldCodeGen contains

- `oldCodeGen/CodeGen.sln` — solution file.
- `oldCodeGen/CodeGen/Program.cs` — manual test driver.
- `oldCodeGen/CodeGen/MyLang.cs` — namespace/class/field/ctor/method/dtor model.
- `oldCodeGen/CodeGen/MyEnum.cs` — enum code generation.
- `oldCodeGen/CodeGen/Extensions.cs` — `Fmt(...)` and `Join(...)` helpers.

## Salvage: multi-view emission interface

`oldCodeGen/CodeGen/MyLang.cs` defines an `IMyLang` interface that separates different emission views:

```csharp
interface IMyLang
{
    string VisibleDeclaration(MyAccessModifier am);
    string InlineImplementation(MyAccessModifier am);
    string ConcreteImplementation(MyAccessModifier am);
    string HiddenDeclaration(MyAccessModifier am);
    string HiddenImplementation(MyAccessModifier am);
}
```

This maps directly to the `.hpp`/`.cpp` split that the current `backend/Generator/` already does, but the interface also预留了 a hidden/PIMPL view.
Adopting this interface could unify the current generator classes and make it easier to add PIMPL-style output later without scattering if/else logic across emitters.

## Salvage: enum reflection generation

`oldCodeGen/CodeGen/MyEnum.cs` is the most complete generator in the old project.
It emits:

- `enum class Name { ... };`
- `ENUM_CLASS_BITWISE(Name)` macro invocation for flag enums.
- `std::numeric_limits<Name>` specialization.
- `std::to_string(Name)` declaration and definition.
- `operator<<(std::ostream&, Name)`.
- A name table implemented with `std::array` for auto-indexed enums or `std::map` for manually-valued enums.

The current backend generates bitwise operators for `[flags]` enums, but it does not emit:

- `std::to_string`
- `operator<<`
- `std::numeric_limits`
- a reflection name table

`MyEnum.cs` is the best starting point for adding these features.

## Salvage: class emission

`oldCodeGen/CodeGen/MyLang.cs` contains a `MyClass` emitter that walks `private:`/`protected:`/`public:` sections and emits fields, constructors, methods, and destructors in access order.
The current generator already does this in `backend/Generator/HierachicalGen.cs`, but the old code is much smaller and easier to read as a reference.

Key pieces:

- `MyClass.VisibleDeclaration()` emits access-section blocks.
- `MyCtor.VisibleDeclaration()` builds a member-initializer list from fields that have an `init` value.
- Supports `= default` and `= delete`.
- Body text indentation is handled consistently.

The automatic initializer-list construction is especially useful because the current grammar parses initializer lists but does not emit them.

## Salvage: namespace wrapping

`oldCodeGen/CodeGen/MyLang.cs` has a `MyNamespace` wrapper that emits `namespace X { ... }` and aggregates children.
This matches the current namespace handling in `backend/Generator/HierachicalGen.cs`, but the old model is self-contained and could be used in unit tests for the generator in isolation.

## Salvage: string formatting helpers

`oldCodeGen/CodeGen/Extensions.cs` provides:

- `string.Fmt(...)` — a small wrapper over `string.Format`.
- `IEnumerable<string>.Join(...)` — joins with a separator.

The current backend already uses `string.Join` and extension helpers in many places, so these are optional.
They are only useful if the project wants a tiny, consistent formatting helper that avoids parentheses everywhere.

## Recommended first steps

1. Port the enum reflection pattern from `oldCodeGen/CodeGen/MyEnum.cs` into `backend/Generator/HierachicalGen.cs` or a new `backend/Generator/EnumGen.cs`.
   Start with `std::to_string` and `operator<<`, guarded by whether the enum is auto-indexed or manually valued.
2. Use the `IMyLang` multi-view interface as a design reference the next time the generator is refactored.
3. Use the `MyCtor` initializer-list generation as a reference when wiring up the existing `initList` grammar rule.
