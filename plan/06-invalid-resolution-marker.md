# Invalid Resolution Marker for Variable Templates

## 1. Problem

In `backend/Resolver/Resolver.cs`, `ResolveIds` and `ResolveScopeds` call `TryResolveVariableTemplate` after a declaration has been found, but they ignore its return value and always invoke `result.Resolve(node, resolved)`.

`TryResolveVariableTemplate` emits a diagnostic and returns `false` when a variable template is referenced with the wrong number of template arguments, yet the caller still records the resolution.

As a result:
- `ResolutionResult.Apply()` writes the declaration into `IdExpr.resolvedDecl` / `ScopedExpr.resolvedDecl`.
- The C++ generator then emits a fully-qualified reference to a specialization that does not exist.
- In `--keep` mode this produces malformed output, and in the normal path the error is only hidden by the earlier `Environment.Exit`.

The current code prevents repeated diagnostics because the node is present in `result.Ids` on the next fixed-point iteration, but it does not prevent downstream use of the erroneous binding.

## 2. Goals

- A failed variable-template resolution must not be stored as a successful resolution.
- The resolver must not loop and re-emit the arity-mismatch diagnostic.
- The type-checker and generator must treat the node as unresolved after the failure.
- The existing good-path case `testing/cases/var_templates` must keep working.
- A new negative case must prove that the diagnostic is reported exactly once and that Myll exits with a non-zero status.

## 3. Design

### 3.1 Store invalid resolutions in `ResolutionResult`

Add two new dictionaries next to the existing valid-resolution dictionaries in `backend/Resolver/ResolutionResult.cs`.
Use dictionaries rather than a sentinel value so the attempted declaration can be kept for future diagnostics.

```csharp
public sealed class ResolutionResult
{
    private readonly Dictionary<IdExpr, Decl>          ids        = new();
    private readonly Dictionary<TypespecNested, Decl>  types      = new();
    private readonly Dictionary<ScopedExpr, Decl>      scopeds    = new();
    private readonly Dictionary<IdExpr, Decl>          members    = new();

    // New: nodes whose resolution was attempted but rejected.
    private readonly Dictionary<IdExpr, Decl?>         invalidIds      = new();
    private readonly Dictionary<ScopedExpr, Decl?>     invalidScopeds  = new();

    // Existing public surface stays unchanged.
    public IReadOnlyDictionary<IdExpr, Decl>          Ids      => ids;
    public IReadOnlyDictionary<TypespecNested, Decl>  Types    => types;
    public IReadOnlyDictionary<ScopedExpr, Decl>      Scopeds  => scopeds;
    public IReadOnlyDictionary<IdExpr, Decl>          Members  => members;

    // New helpers.
    public void MarkInvalid( IdExpr id, Decl? attemptedDecl = null )
        => invalidIds[id] = attemptedDecl;

    public void MarkInvalid( ScopedExpr scoped, Decl? attemptedDecl = null )
        => invalidScopeds[scoped] = attemptedDecl;

    public bool IsInvalid( IdExpr id )
        => invalidIds.ContainsKey( id );

    public bool IsInvalid( ScopedExpr scoped )
        => invalidScopeds.ContainsKey( scoped );

    public bool IsResolvedOrInvalid( IdExpr id )
        => ids.ContainsKey( id ) || invalidIds.ContainsKey( id );

    public bool IsResolvedOrInvalid( ScopedExpr scoped )
        => scopeds.ContainsKey( scoped ) || invalidScopeds.ContainsKey( scoped );

    // ... existing Resolve / TryGetResolved / ResolveMember / Apply methods ...
}
```

`Apply()` does not need to change because invalid entries live in separate dictionaries and will never be written onto `resolvedDecl`.
`TryGetResolved` only consults the valid dictionaries, so invalid nodes appear unresolved to the rest of the pipeline.

### 3.2 Update `ResolveIds`

In `backend/Resolver/Resolver.cs` around line 278, replace the current check and resolution body with the following logic.

```csharp
private bool ResolveIds( GlobalNamespace module, ModuleContext context )
{
    bool progress = false;
    foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
        if( result.IsResolvedOrInvalid( unresolved.Node ) )
            continue;

        IdTplArgs[] segments = new[] { unresolved.Node.idTplArgs };
        bool callSite = context.FuncCallCallees.Contains( unresolved.Node );
        Decl? resolved = ResolvePath( segments, unresolved.Scope, module, out _, callSite );
        if( resolved == null )
            continue;

        if( TryResolveVariableTemplate( resolved, unresolved.Node ) ) {
            result.Resolve( unresolved.Node, resolved );
            progress = true;
        }
        else {
            result.MarkInvalid( unresolved.Node, resolved );
        }
    }

    return progress;
}
```

Marking a node invalid does **not** set `progress` to true because the failure does not unblock any dependent resolution.

### 3.3 Update `ResolveScopeds`

Apply the same pattern around line 298.

```csharp
private bool ResolveScopeds( GlobalNamespace module, ModuleContext context )
{
    bool progress = false;
    foreach( UnresolvedScoped unresolved in context.UnresolvedScopeds ) {
        if( result.IsResolvedOrInvalid( unresolved.Node ) )
            continue;

        bool callSite = context.FuncCallCallees.Contains( unresolved.Node );
        Decl? resolved = ResolvePath( unresolved.Node.idTpls, unresolved.Scope, module, out _, callSite );
        if( resolved == null )
            continue;

        if( TryResolveVariableTemplate( resolved, unresolved.Node ) ) {
            result.Resolve( unresolved.Node, resolved );
            progress = true;
        }
        else {
            result.MarkInvalid( unresolved.Node, resolved );
        }
    }

    return progress;
}
```

### 3.4 Update `TryResolveVariableTemplate` documentation

The method body can stay as it is, but its XML comment must be updated so future maintainers know the contract.

```csharp
/// <summary>
/// Performs template argument validation and type substitution for variable templates.
/// Returns true when the reference is valid and should be recorded as a resolution.
/// Returns false when the arity mismatches; the caller must then mark the node invalid
/// rather than recording the resolution, and must not re-process the node.
/// </summary>
```

### 3.5 Skip invalid nodes in `ReportUnresolved`

In `ReportUnresolved` around line 2391, skip nodes that are already marked invalid so they do not produce a second "Unresolved identifier" diagnostic.

```csharp
foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
    if( result.IsResolvedOrInvalid( unresolved.Node ) )
        continue;

    // ... existing reporting logic ...
}

foreach( UnresolvedScoped unresolved in context.UnresolvedScopeds ) {
    if( result.IsResolvedOrInvalid( unresolved.Node ) )
        continue;

    // ... existing reporting logic ...
}
```

### 3.6 Optional: member variable templates

`ResolveMemberAccesses` currently does not validate member variable-template arity.
If the grammar and visitor allow member variable templates, extend `ResolveMemberAccesses` to call `TryResolveVariableTemplate` on the resolved member and, on failure, store it as an invalid member resolution.

For now this is out of scope because the known failing case is an unqualified or scoped identifier use, but a similar helper should be added once member variable templates are exercised.

## 4. Interactions with the Fixed-Point Resolver

- The resolver loop continues while any pass reports `progress == true`. Because invalid marking does not report progress, introducing an invalid node does not keep the loop alive.
- On the next iteration, `IsResolvedOrInvalid` prevents the node from being processed again, so the diagnostic is emitted exactly once.
- Other parts of the pipeline (`TypeResolver`, `TypeChecker`, overload resolution, code generation) consult only valid resolutions via `TryGetResolved` or the AST's `resolvedDecl` field. Since invalid nodes are never applied, those passes behave as if the node were still unresolved.
- This is consistent with the existing type-resolution path for `TypespecNested`, where an arity mismatch emits a diagnostic, skips the node without recording it, and lets the loop terminate naturally.

## 5. Concrete File and Method Changes

| File | Method / Location | Change |
|------|-------------------|--------|
| `backend/Resolver/ResolutionResult.cs` | Class body | Add `invalidIds` and `invalidScopeds` dictionaries plus `MarkInvalid`, `IsInvalid`, and `IsResolvedOrInvalid` helpers. |
| `backend/Resolver/Resolver.cs` | `ResolveIds` (line 278) | Skip resolved-or-invalid nodes; only call `result.Resolve` when `TryResolveVariableTemplate` succeeds; call `result.MarkInvalid` on failure. |
| `backend/Resolver/Resolver.cs` | `ResolveScopeds` (line 298) | Same pattern as `ResolveIds`. |
| `backend/Resolver/Resolver.cs` | `TryResolveVariableTemplate` (line 322) | Update XML documentation only. |
| `backend/Resolver/Resolver.cs` | `ReportUnresolved` (line 2384) | Skip invalid identifiers and scoped names so they are not reported twice. |
| `testing/caseconfig.json` | `depth.generateFailing` | Add `"var_templates_fail"`. |
| `testing/cases/var_templates_fail/main.myll` | New file | Add a module that references a variable template with the wrong arity. |

No changes are required in `backend/Resolver/TypeChecker.cs`, `backend/Resolver/TypeResolver.cs`, or the generator because the absence of a valid `resolvedDecl` already produces the desired behavior.

## 6. Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| A call site checks `result.Ids.ContainsKey` and incorrectly treats an invalid node as unresolved. | This is the desired behavior for now, except for `ReportUnresolved`, which is updated explicitly. If future code needs to know that a node was attempted and failed, it can use `result.IsInvalid`. |
| `progress` could be missed if marking invalid must unblock a dependent resolution. | A failed variable-template resolution is a permanent error; no dependent resolution can become valid because the expression type is permanently unknown. Not reporting progress is correct. |
| The generator falls back to the source text (`answer<int, int>`), which still does not compile. | Under the default path Myll exits on the diagnostic before generation. Under `--keep`, the emitted source text is consistent with the original code and the user already received the arity-mismatch message. |
| Reference equality of `IdExpr`/`ScopedExpr` instances might not match between `UnresolvedIds` and later lookups. | `ResolutionResult` already uses object-reference dictionaries for valid results; the invalid dictionaries follow the same scheme. |

## 7. Testing Steps

1. Build the solution.
   ```bash
   dotnet build myll.sln
   ```
2. Run the existing passing case.
   ```bash
   dotnet test testing/ --filter "DisplayName~var_templates"
   ```
3. Create `testing/cases/var_templates_fail/main.myll` with at least one variable-template arity mismatch, for example:
   ```myll
   module var_templates_fail;

   const T answer<T> = (T)42;

   func main() -> int {
       var int a = answer<int, int>;
       return 0;
   }
   ```
4. Add `var_templates_fail` to `testing/caseconfig.json` under `depth.generateFailing`.
5. Run the new failing case.
   ```bash
   dotnet test testing/ --filter "DisplayName~var_templates_fail"
   ```
   The test should report non-zero exit code and a single "Template argument count mismatch for variable 'answer'" diagnostic.
6. Manually verify that the diagnostic is not duplicated and that there is no generic "Unresolved identifier 'answer'" fallback.
7. Run the full suite to check for regressions.
   ```bash
   dotnet test testing/
   ```
8. Optional: run the failing case with `--keep` and inspect the generated output to confirm the identifier is emitted as written rather than as a fully-qualified specialization.
