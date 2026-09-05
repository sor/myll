# Migrate `[chain]` and pre-resolution transforms to the backend

## Status
DONE.

## Goal
Move the transform steps that previously only ran in `frontend/Program.cs` into `backend/Resolver/Resolver.Resolve` so the backend library is usable on its own.

## What changed

### 1. Transforms moved into `Resolver.Resolve`

`backend/Resolver/Resolver.cs` now runs the following pre-resolution transforms:

```csharp
new DefaultAttributesTransformer(),
new EnumTransformer(),
new AutoReturnTransformer(),
new TemplateParamTransformer(),
new ChainTransformer(),
```

Order:
- `AutoReturnTransformer` runs before `ChainTransformer` so `[chain]` methods keep their implicit `auto` return until auto-return decides to skip them.
- `TemplateParamTransformer` runs last so template parameter symbols are injected before name resolution.

Removed the corresponding calls from `frontend/Program.cs`.

Adjusted the resolver-diagnostic handling in `frontend/Program.cs` so warnings from these transforms no longer cause a fatal exit.

### 2. `[chain]` semantics tightened

`backend/Resolver/ChainTransformer.cs` now:

1. Rejects explicit return types with an error.
2. Warns when the body already ends with an unconditional `return self;`.
3. Otherwise infers `Class&` (or `const Class&` if `[pure]`) and appends `return self;` when needed.

### 3. Boilerplate cleaned up

Removed manual `-> Class&` and trailing `return self;` from:

- `myll/dyn_array.myll`
- `testing/cases/external_algorithms/main.myll`
- `frontend/tests/thesis/container.myll`

### 4. Tests

- Added `testing/ChainTransformerTests.cs` covering return-type inference, const refs, explicit-return errors, redundant-return warnings, and early-return appending.
- Added `testing/cases/chain_explicit_return_fail/` as a generate-failing integration case.
- Added direct unit tests for `AutoReturnTransformer`, `TemplateParamTransformer`, `ElseOnLoopTransformer`, `BreakContinueTransformer`, and `ConfiguredAliasShadowingTransformer`.
- Removed the broken `Stmt.EnumerateDF` traversal and replaced it with a test-only `DescendantsAndSelf` extension.

## Completed optional follow-up

- Moved `ElseOnLoopTransformer` and `BreakContinueTransformer` into `Resolver.Resolve` post-resolution transforms for full backend consistency.
- Moved `ConfiguredAliasShadowingTransformer` into `Resolver.Resolve` post-resolution transforms, making the backend fully self-contained.
