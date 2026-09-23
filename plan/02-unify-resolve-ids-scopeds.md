# Unify `ResolveIds` and `ResolveScopeds` in `backend/Resolver/Resolver.cs`

## Status

Planned.

## Goal

Eliminate the near-duplicate loops in `ResolveIds` and `ResolveScopeds` by introducing one generic helper that performs the shared resolution work, while the existing wrapper methods only supply the per-kind segment extraction and the correct resolution dictionary.

## Background

Both methods currently do exactly the same thing after the segment list is obtained:

1. Skip entries that are already resolved.
2. Determine whether the node is a call site via `context.FuncCallCallees`.
3. Call `ResolvePath` with the segments, scope, and `callSite` flag.
4. Call `TryResolveVariableTemplate` on the resolved declaration.
5. Call `result.Resolve` to record the mapping and mark progress.

The only real difference is that `ResolveIds` builds a single-segment list from `IdExpr.idTplArgs`, while `ResolveScopeds` uses `ScopedExpr.idTpls` directly.

Current code, `backend/Resolver/Resolver.cs` lines 278-351:

```csharp
private bool ResolveIds( GlobalNamespace module, ModuleContext context )
{
    bool progress = false;
    foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
        if( result.Ids.ContainsKey( unresolved.Node ) )
            continue;

        IdTplArgs[] segments = new[] { unresolved.Node.idTplArgs };
        bool callSite = context.FuncCallCallees.Contains( unresolved.Node );
        Decl? resolved = ResolvePath( segments, unresolved.Scope, module, out _, callSite );
        if( resolved != null ) {
            TryResolveVariableTemplate( resolved, unresolved.Node );
            result.Resolve( unresolved.Node, resolved );
            progress = true;
        }
    }
    return progress;
}

private bool ResolveScopeds( GlobalNamespace module, ModuleContext context )
{
    bool progress = false;
    foreach( UnresolvedScoped unresolved in context.UnresolvedScopeds ) {
        if( result.Scopeds.ContainsKey( unresolved.Node ) )
            continue;

        bool callSite = context.FuncCallCallees.Contains( unresolved.Node );
        Decl? resolved = ResolvePath( unresolved.Node.idTpls, unresolved.Scope, module, out _, callSite );
        if( resolved != null ) {
            TryResolveVariableTemplate( resolved, unresolved.Node );
            result.Resolve( unresolved.Node, resolved );
            progress = true;
        }
    }
    return progress;
}
```

## Proposed changes

All changes are in `backend/Resolver/Resolver.cs`.

### 1. Add a small local value type to carry node, scope, and segments

```csharp
private readonly struct NameRef<TNode>
    where TNode : Expr
{
    public readonly TNode Node;
    public readonly Scope Scope;
    public readonly IReadOnlyList<IdTplArgs> Segments;

    public NameRef( TNode node, Scope scope, IReadOnlyList<IdTplArgs> segments )
    {
        Node     = node;
        Scope    = scope;
        Segments = segments;
    }
}
```

Use a `readonly struct`, not a record struct, because the project targets C# 9.0.

### 2. Add the generic helper

```csharp
private bool ResolveNameReferences<TNode>(
    GlobalNamespace module,
    ModuleContext context,
    IEnumerable<NameRef<TNode>> refs,
    IReadOnlyDictionary<TNode, Decl> alreadyResolved,
    Action<TNode, Decl> recordResolution )
    where TNode : Expr
{
    bool progress = false;
    foreach( NameRef<TNode> r in refs ) {
        if( alreadyResolved.ContainsKey( r.Node ) )
            continue;

        bool callSite = context.FuncCallCallees.Contains( r.Node );
        Decl? resolved = ResolvePath( r.Segments, r.Scope, module, out _, callSite );
        if( resolved != null ) {
            TryResolveVariableTemplate( resolved, r.Node );
            recordResolution( r.Node, resolved );
            progress = true;
        }
    }

    return progress;
}
```

### 3. Make `ResolveIds` and `ResolveScopeds` thin wrappers

```csharp
private bool ResolveIds( GlobalNamespace module, ModuleContext context )
{
    return ResolveNameReferences(
        module,
        context,
        context.UnresolvedIds.Select(
            u => new NameRef<IdExpr>( u.Node, u.Scope, new[] { u.Node.idTplArgs } ) ),
        result.Ids,
        ( node, decl ) => result.Resolve( node, decl ) );
}

private bool ResolveScopeds( GlobalNamespace module, ModuleContext context )
{
    return ResolveNameReferences(
        module,
        context,
        context.UnresolvedScopeds.Select(
            u => new NameRef<ScopedExpr>( u.Node, u.Scope, u.Node.idTpls ) ),
        result.Scopeds,
        ( node, decl ) => result.Resolve( node, decl ) );
}
```

The existing public-facing signatures are kept, so the call site in `Resolver.Resolve` does not have to change. The wrappers now only contain the segment-extraction differences.

## Why this preserves behaviour

- The loop body is byte-for-byte identical to the original except for how the segment list and resolution map are supplied.
- `context.FuncCallCallees` is `HashSet<Expr>`, and `TNode : Expr`, so the `Contains` call works for both `IdExpr` and `ScopedExpr` nodes just as before.
- `new[] { node.idTplArgs }` is an `IdTplArgs[]`, which implements `IReadOnlyList<IdTplArgs>`, so it is compatible with `ResolvePath`.
- `TryResolveVariableTemplate` accepts an `Expr` and internally switches between `IdExpr` and `ScopedExpr`, so passing `r.Node` still works for both.
- The resolution dictionaries passed to the helper are the exact typed dictionaries from `ResolutionResult.Ids` and `ResolutionResult.Scopeds`, so the `ContainsKey` check and the recorded mapping stay correct.

## Edge cases

- **Scoped names with multiple segments.** `ResolveScopeds` passes `u.Node.idTpls` directly, so `ResolvePath` still walks `A::B::C` exactly as it does today.
- **Call-site detection.** Both unqualified identifiers and scoped names may be recorded in `FuncCallCallees`; the helper preserves the same `callSite` computation.
- **Variable templates.** `TryResolveVariableTemplate` looks at the terminal template arguments on the expression. Because the wrapper supplies the full segment list and the node, the logic that decides whether a `VarDecl` is a variable template and substitutes its type arguments is unchanged.
- **Built-in identifiers and types.** `ResolvePath` may return the shared `BuiltInDecl`; `TryResolveVariableTemplate` returns `true` for non-variables, and the helper still records the resolution.
- **Already-resolved nodes.** The helper skips nodes that are already present in the relevant dictionary, matching the current early `continue`.
- **Fixed-point iteration.** The helper returns `true` only when at least one new resolution is recorded, exactly like the original methods.

## Optional follow-up

`TryResolveVariableTemplate` re-derives the terminal `IdTplArgs` from the expression with a switch on `IdExpr` / `ScopedExpr`. Since the caller now naturally has the full segment list available, a later small refactor could change `TryResolveVariableTemplate` to accept the resolved declaration, the expression, and the terminal `IdTplArgs`, removing the duplication in segment extraction. This is not required for the current unification and can be done separately.

## Files changed

- `backend/Resolver/Resolver.cs` only.

## Validation steps

1. Build the backend: `dotnet build backend` or `dotnet build myll.sln`.
2. Run the integration harness: `dotnet test testing/`.
3. Smoke-test the compiler on cases that exercise both simple and scoped identifiers:
   - `dotnet run --project frontend -- -i testing/cases/templates/*.myll -o testing/cases/templates/generated -cr`
   - `dotnet run --project frontend -- -i frontend/tests/thesis/*.myll -o frontend/tests/thesis/generated -cr`
   - `dotnet run --project frontend -- -i testing/cases/external_algorithms/*.myll -o testing/cases/external_algorithms/generated -cr`
4. Verify that no diagnostic regressions appear for unresolved identifiers, scoped names, or template variable uses.

## Open questions for the implementer

- Should `ResolveIds` and `ResolveScopeds` be kept as thin wrappers, or should the call site in `Resolver.Resolve` be updated to call `ResolveNameReferences` directly and the wrappers removed? Keeping the wrappers is recommended for the first pass because it limits the change to one file and keeps the diff small.
- Is the per-pass allocation from `new[] { ... }` and the LINQ `Select` acceptable? It mirrors the existing allocation of a single-segment array inside `ResolveIds`. If this becomes hot in profiling, a follow-up can cache the segment list inside `UnresolvedId` or use `Array.Empty<T>()` for the empty case.
