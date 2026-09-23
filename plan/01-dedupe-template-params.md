# Deduplicate template-parameter plumbing

## Motivation

Variable templates were added recently. Now `Func`, `Structural`, and `VarDecl` all carry template parameters and need a private scope in which the parameter names are visible. The same triplet is handled in three separate switch cases in `TemplateParamTransformer` and in several resolver helper methods in `TemplateInference`. This is a natural place to introduce a common interface so the template-parameter plumbing is declared once and consumed uniformly.

## Current state

### Three declarations with parallel fields

| Declaration | Template-parameter list | Owning scope for parameters |
|-------------|------------------------|-----------------------------|
| `Func`      | `List<TplParam> TplParams { get; init; }` | `public Scope? funcScope;` |
| `Structural`| `List<TplParam> TplParams { get; set; }` | `public Scope scope` (inherited from `Hierarchical`) |
| `VarDecl`   | `List<TplParam> TplParams { get; init; }` | `public Scope? varScope;` |

All three implement `Myll.Core.ITplParams`:

```csharp
interface ITplParams
{
    List<TplParam> TplParams { get; }
}
```

### Places where the triplet is repeated

1. `backend/Transform/TemplateParamTransformer.cs`, `TransformDecl`
   - One `case Func`, one `case Structural`, one `case VarDecl`, each testing `TplParams.Count` and the corresponding scope field, then calling `InjectParameters(scope, TplParams, context)`.
2. `backend/Resolver/TemplateInference.cs`, `GetTemplateParamCount`
   - `switch` over `Func`, `Structural`, `VarDecl` to return the parameter count.
3. `backend/Resolver/TemplateInference.cs`, `GetAllTemplateParams`
   - `switch` over the same three types to collect the declaration's own parameters.

### Other consumers of the template list

- `backend/Core/Decl.cs` - `Decl.IsTemplateUp` and `Decl.BuildQualifiedName` use `ITplParams` to walk enclosing scopes.
- `backend/Generator/HierachicalGen.cs` - generation uses `TplParams` directly (no scope access).
- `backend/Resolver/OperatorResolver.cs` - uses `owner.TplParams` for class-template operator return-type substitution.
- `backend/Transform/ComparisonModeTransformer.cs` - builds default comparison functions from `structural.TplParams`.
- `backend/Visitor/VDecl.cs` and `backend/Visitor/VExpr.cs` - parse-tree construction populates `TplParams`.

None of these need the scope field, but they do use the interface name.

## Proposed design

Replace `ITplParams` with `ITemplatedDecl` and add a `TemplateParamScope` property to the interface. The three declarations implement the property by returning their existing scope field.

```csharp
public interface ITemplatedDecl
{
    List<TplParam> TplParams { get; }
    Scope?         TemplateParamScope { get; }
}
```

Implementations:

```csharp
public class Func : Decl, ITemplatedDecl
{
    ...
    public List<TplParam> TplParams { get; init; } = new();
    public Scope?         funcScope;

    public Scope? TemplateParamScope => funcScope;
}

public class VarDecl : Decl, ITemplatedDecl
{
    ...
    public List<TplParam> TplParams { get; init; } = new();
    public Scope?         varScope;

    public Scope? TemplateParamScope => varScope;
}

public class Structural : Hierarchical, ITemplatedDecl
{
    ...
    public List<TplParam> TplParams { get; set; } = new();
    public Scope? TemplateParamScope => scope;
}
```

The property is nullable because non-template declarations of these kinds still implement the interface (with an empty parameter list) and because the underlying scope may legitimately be unset during AST construction. Callers that need the scope already guard against `null`.

The existing field names (`funcScope`, `varScope`, `scope`) are kept as-is. `funcScope` remains the function body scope and happens to be the template-parameter scope; renaming it would create churn in `AutoReturnTransformer` and `ConfiguredAliasShadowingTransformer` without benefit. `varScope` is only used for variable templates, so an optional follow-up could rename it to `templateParamScope` for clarity, but that is out of scope for this refactor.

## Step-by-step changes

### 1. `backend/Core/Decl.cs`

- Rename the interface from `ITplParams` to `ITemplatedDecl`.
- Add `Scope? TemplateParamScope { get; }` to the interface.
- Update the three class declarations:
  - `Func : Decl, ITemplatedDecl`
  - `VarDecl : Decl, ITemplatedDecl`
  - `Structural : Hierarchical, ITemplatedDecl`
- Add the property implementation in each class:
  - `Func`: `public Scope? TemplateParamScope => funcScope;`
  - `VarDecl`: `public Scope? TemplateParamScope => varScope;`
  - `Structural`: `public Scope? TemplateParamScope => scope;`
- Update `Decl.IsTemplateUp`: change `cur.decl is ITplParams curStruct` and `curStruct.TplParams.Count` to use `ITemplatedDecl`.
- Update `Decl.BuildQualifiedName`: change the cast and access from `ITplParams` to `ITemplatedDecl`.

### 2. `backend/Generator/HierachicalGen.cs`

- In `AddHierarchical`, change the cast `gen.hierarchical is ITplParams hierWithTpl` to `is ITemplatedDecl hierWithTpl`.
- No other generator code touches the scope, so only the interface name changes.

### 3. `backend/Resolver/TemplateInference.cs`

- Update `GetTemplateParamCount`:
  ```csharp
  public static int GetTemplateParamCount( Decl decl )
      => (decl as ITemplatedDecl)?.TplParams.Count ?? 0;
  ```
- Update `GetAllTemplateParams`:
  - Replace the `decl switch` for own parameters with `(decl as ITemplatedDecl)?.TplParams ?? new List<TplParam>()`.
  - Replace the enclosing loop cast `cur.decl is ITplParams tp` with `is ITemplatedDecl td`.
- No changes to deduction logic; this is purely mechanical.

### 4. `backend/Transform/TemplateParamTransformer.cs`

- Collapse the three switch cases into a single interface check:
  ```csharp
  if( decl is ITemplatedDecl templated
   && templated.TplParams.Count >= 1
   && templated.TemplateParamScope != null ) {
      InjectParameters( templated.TemplateParamScope, templated.TplParams, context );
  }
  ```
- Keep the recursion over `Hierarchical.children` unchanged.

### 5. Update any remaining `ITplParams` references

A final search for `ITplParams` across `backend/` should return only the renamed definition and its new usages. Expected files with name changes:
- `backend/Core/Decl.cs`
- `backend/Generator/HierachicalGen.cs`
- `backend/Resolver/TemplateInference.cs`
- `backend/Transform/TemplateParamTransformer.cs`

### 6. Unit tests

- `testing/TemplateParamTransformerTests.cs` already verifies that function-template and class-template parameters are injected into the correct scope. Add an analogous test for variable templates:
  - Parse a module with `const T answer<T> = (T)42;`.
  - Find the `VarDecl` named `answer`.
  - Assert `TplParams` has one entry.
  - Assert `TemplateParamScope` is non-null and `children` contains `"T"`.

## Risks and mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| `Structural.TemplateParamScope` becomes nullable while `Structural.scope` was previously non-null in practice. | If a structural scope were ever null, the old code would throw in `TemplateParamTransformer` while the new code skips injection. No real `Structural` should have a null `scope` after visiting, so behavior is unchanged. | Verify that every `Structural` is pushed/popped through `VDecl.VisitDefStruct`. |
| Forward-declared `Func` nodes might lack `funcScope`. | `TemplateParamTransformer` only runs for declarations with `TplParams.Count >= 1`. Forward function templates still push a scope in `VDecl.VisitDefFunc`, so `funcScope` is set. | Add unit test for a forward function template. |
| Rename of internal interface `ITplParams` breaks external consumers. | The interface is internal to the backend assembly; no public API surface is affected. | Full build confirms no compile errors. |
| `GetTemplateParamCount` now returns `0` for `Decl` subtypes that previously fell through. | The old `switch` already defaulted to `0` for non-template declaration kinds. Behavior is identical. | No additional action. |
| Generated C++ output changes unexpectedly. | The refactor only rearranges the C# code paths; no generated strings should change. | Golden files and integration tests detect output drift. |

## Verification steps

1. Build the solution:
   ```bash
   dotnet build myll.sln
   ```
2. Run the unit and integration harness:
   ```bash
   dotnet test testing/
   ```
3. Run template-related integration cases individually and check that they still pass:
   ```bash
   dotnet test testing/ --filter "DisplayName~var_templates"
   dotnet test testing/ --filter "DisplayName~template_basics"
   dotnet test testing/ --filter "DisplayName~class_templates"
   dotnet test testing/ --filter "DisplayName~template_typename"
   dotnet test testing/ --filter "DisplayName~overload_template_ranking"
   ```
4. If any golden-file cases are affected, compare generated output before and after the change. No output changes are expected.
5. Run the full coreutil build to make sure real-world generated code still compiles:
   ```bash
   bash frontend/coreutil/make_coreutil.sh
   ```

## Follow-up ideas (out of scope)

- Rename `VarDecl.varScope` to `templateParamScope` for clarity, since it is only used by variable templates.
- Consider whether `Structural` should expose `TemplateParamScope` as non-nullable, because its `scope` property is effectively guaranteed.
- Add `ITemplatedDecl` tests for forward class templates and forward function templates to lock down the null-guard behavior.
