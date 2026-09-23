# Resolve variable-template parameter types before substitution

## Background

Myll recently gained variable templates. A declaration such as `const T answer<T> = (T)42;` produces a `VarDecl` that owns its own template parameters. When a use site is resolved, e.g. `answer<int>`, the resolver computes the expression's type by substituting the provided template arguments into the variable template's declared type.

The substitution lives in `backend/Resolver/TemplateInference.cs` (`SubstituteTemplateParams` / `Substitute`). The current implementation contains an explicit fallback: if the variable template's type has not yet been resolved to a `TplParamDecl`, it also matches the type by its plain identifier name. That fallback was added in commit `9dd86ad` because variable-template substitution runs earlier in the resolution pipeline than function-template substitution, before `ResolveTypes` has had a chance to bind the parameter type `T` to the injected `TplParamDecl`.

This plan describes how to remove that plain-name fallback by pre-linking the variable template's declared type to the matching `TplParamDecl` at template-parameter injection time.

## Current flow and root cause

1. Parsing (`backend/Visitor/VDecl.cs`, `VisitDefVar`) creates a `VarDecl` for a variable template, including:
   - `TplParams` (the declared parameter names).
   - `varScope`, a child scope that encloses the type and initializer.
   - `type`, the declared `Typespec` (e.g. a `TypespecNested` for `T`).
   - An `UnresolvedType` for `type` whose lookup scope is `varScope`.
2. Pre-resolution transforms run. `TemplateParamTransformer` injects a hidden `TplParamDecl` named `T` into `varScope`.
3. The fixed-point resolution loop runs in this order:
   - `ResolveUsings`
   - `ResolveAliases`
   - `ResolveIds` / `ResolveScopeds`  -- `TryResolveVariableTemplate` is called here.
   - `ResolveTypes`                       -- this is where `varDecl.type` would finally resolve to the `TplParamDecl`.
   - `ResolveMemberAccesses`
   - `ResolveCalls`

Because `TryResolveVariableTemplate` is invoked from `ResolveIds`/`ResolveScopeds`, it runs before `ResolveTypes`. At that moment `varDecl.type.resolvedDecl` is still null and the `ResolutionResult` has not yet mapped the type, so `GetResolvedDecl` returns null. The plain-name fallback in `Substitute` is therefore reached.

For function templates the same problem does not arise: parameter/return types are substituted in `ResolveCalls`, which runs after `ResolveTypes`, so the `TplParamDecl` is already resolved.

## Proposed solution

Pre-link every simple template-parameter reference inside a variable template's declared type to the corresponding injected `TplParamDecl`. Once that is done, `Substitute` can rely on `resolvedDecl is TplParamDecl` and the plain-name fallback can be deleted.

The right place to do this is `TemplateParamTransformer`, because it already owns the knowledge of which `TplParamDecl` belongs to which scope and it runs before any resolution.

### Why pre-linking instead of reordering

Moving `TryResolveVariableTemplate` to run after `ResolveTypes` would require either:
- Reordering the fixed-point loop so that types are resolved before identifiers, which is a global change with unknown fallout for member-access and call resolution; or
- Adding a separate deferred pass that re-visits every resolved variable-template identifier. That is more bookkeeping than pre-linking and still leaves the type unresolved during the first identifier-resolution pass.

Pre-linking keeps the change local, preserves the existing resolution order, and models what is already true: inside `varScope`, the identifier `T` denotes the template parameter.

## Detailed implementation plan

### 1. Extend `TemplateParamTransformer` to link parameter types

File: `backend/Transform/TemplateParamTransformer.cs`

After injecting the `TplParamDecl`s for a variable template, walk `varDecl.type` and set `resolvedDecl` on every matching `TypespecNested`.

Add a private helper that builds a name-to-`TplParamDecl` map from the just-injected scope entries and then recurses over the type:

- If the type is a `TypespecNested` with exactly one segment, no template arguments, and that segment's identifier matches a parameter name, set `resolvedDecl` to the corresponding `TplParamDecl`.
- Otherwise, recurse into the template arguments of every segment. This handles types such as `std::array<T, N>` or `Wrapper<T>*` where the parameter appears inside an argument list or under pointer wrappers.
- Do not set `resolvedDecl` for multi-segment names such as `T::Nested`; the current fallback did not handle them either, and resolving such names correctly is the job of `ResolveTypes` and `UpdateDependentNestedNames`.

Call this helper immediately after `InjectParameters( varDecl.varScope, varDecl.TplParams, context )` in the `VarDecl` branch of `TransformDecl`.

### 2. Remove the plain-name fallback from `Substitute`

File: `backend/Resolver/TemplateInference.cs`

In `Substitute`:

- Delete the `plainTplParam` local and the comment that explains the fallback.
- Reduce the `if` condition to a single check: `resolved is TplParamDecl tpl && substitution.TryGetValue( tpl.name, out Typespec? replacement )`.

No other call site of `SubstituteTemplateParams` should be affected, because all other sites operate on types that have already passed through `ResolveTypes`.

### 3. Regression tests

Files under `testing/cases/var_templates/`:

- Keep the existing `main.myll` (`const T answer<T> = (T)42;`).
- Add `pointers.myll` covering pointer/reference wrappers, e.g. `const T* ptr<T> = null;` and an instantiation that reads `ptr<int>`.
- Add `nested_args.myll` covering a template parameter used inside another template-id, e.g. a local `Pair<T, U>` or `Container<T>` variable template.

If a suitable reusable container is not already bound, define a small local struct template inside the test module so the generated C++ remains self-contained.

## Ordering constraints

1. `TemplateParamTransformer` must continue to run as a pre-resolution transform, before the fixed-point loop in `NameResolver.Resolve`.
2. The linking helper must run after the `TplParamDecl`s have been injected into `varScope` but before `NameResolver.Resolve` begins. Using the same instance that was injected guarantees identity with the scope entry.
3. `ResolveTypes` will later resolve the same type again and add it to `ResolutionResult.Types`, which is harmless and idempotent because `ResolutionResult.Apply` writes the same `TplParamDecl` back to `resolvedDecl`.
4. The fallback removal must not be committed before the pre-linking code is in place and tested.

## Files and methods to modify

| File | Method/location | Change |
|------|-----------------|--------|
| `backend/Transform/TemplateParamTransformer.cs` | `TransformDecl` `VarDecl` branch | Call new linking helper after `InjectParameters`. |
| `backend/Transform/TemplateParamTransformer.cs` | New private helper | `LinkVariableTemplateParamTypes(VarDecl, Scope, IReadOnlyList<TplParam>)` or similar. Builds the parameter map and calls the recursive linker. |
| `backend/Transform/TemplateParamTransformer.cs` | New private helper | `LinkType(Typespec?, Dictionary<string, TplParamDecl>)` or similar. Recursively sets `resolvedDecl` on matching simple `TypespecNested`s. |
| `backend/Resolver/TemplateInference.cs` | `Substitute` | Remove `plainTplParam` and the plain-name fallback branch; keep only `resolved is TplParamDecl tpl`. |
| `testing/cases/var_templates/main.myll` | existing | Keep as primary regression case. |
| `testing/cases/var_templates/pointers.myll` | new | Add pointer/reference wrapper coverage. |
| `testing/cases/var_templates/nested_args.myll` | new | Add nested template-argument coverage. |

## Risks and mitigation

### Risk: other code paths still rely on the fallback

`SubstituteTemplateParams` is also called from overload ranking, operator return-type substitution, and base-type substitution. Those paths operate on types that have already been resolved by `ResolveTypes`, so they should not need the fallback. However, removing it surface latent bugs.

Mitigation: run the full test suite (`dotnet test testing/`). If any test fails with unresolved template-parameter substitution, investigate whether the relevant type is missing a `resolvedDecl` and fix the root cause rather than restoring the fallback.

### Risk: pre-linking creates a `resolvedDecl` that `ResolveTypes` later disagrees with

This cannot happen for a simple parameter reference: the injected `TplParamDecl` is the same declaration that `ResolveTypes` will find in `varScope`. For multi-segment or aliased names the helper deliberately does not set `resolvedDecl`.

Mitigation: ensure the helper only sets `resolvedDecl` for single-segment, zero-argument identifiers that exactly match a parameter name.

### Risk: pre-linking skips `result.Types` registration, leaving downstream code unaware

The type will still be processed by `ResolveTypes`, which will call `result.Resolve(...)` and later `result.Apply()`. Pre-linking only sets the AST field early; it does not bypass the resolution map.

Mitigation: verify that `result.Types` contains the variable-template type after resolution and that `UpdateDependentNestedNames` sees it correctly.

### Risk: variable templates inside prototype files

`TemplateParamTransformer.RunInPrototypeFiles` is true, so pre-linking will also run for `.decl.myll` prototypes. This is correct because the same type-dependency rules apply.

## Testing steps

1. Implement the changes in `TemplateParamTransformer.cs` and `TemplateInference.cs`.
2. Build the solution: `dotnet build myll.sln`.
3. Run the existing variable-template test: `dotnet test testing/ --filter "DisplayName~var_templates"`.
4. Run the full integration suite: `dotnet test testing/`.
5. Manually compile the new regression cases with the CLI and inspect the generated C++ to confirm the substituted types are emitted correctly, e.g.:
   ```bash
   dotnet run --project frontend -- -i testing/cases/var_templates/*.myll -o testing/cases/var_templates/generated -cr
   ```
6. If all tests pass, the fallback is no longer needed.

## Follow-up ideas

The same pre-linking helper could later be extended to function templates and class/struct templates for consistency, although it is not required today because their substitution happens after `ResolveTypes`. If that extension is done, it should be a separate cleanup and include its own tests.
