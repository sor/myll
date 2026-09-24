# Unified Resolution Failure State for Class/Struct Templates and Overload Failures

**Status:** Implemented in commit `f41d478`.

## 1. Goal

Move every currently ad-hoc resolution failure into `ResolutionResult`:

- class/struct template arity mismatches currently reported by `ResolveTypes` but not recorded anywhere, and
- function/constructor overload failures currently stored in the `Resolver` instance fields `ambiguousCalls`, `noMatchingCalls`, and `templateArityMismatchCalls`.

The refactor removes all three `HashSet<Expr>` fields from `Resolver` and keeps the existing diagnostic messages byte-for-byte identical. Failed resolutions must never be written onto AST `resolvedDecl`.

## 2. Why This Matters

### 2.1 `ResolveTypes` leaves failed type resolutions unmarked

When a class/struct template is referenced with the wrong number of template arguments, `ResolveTypes` emits a diagnostic and `continue`s without storing the failure. On the next fixed-point iteration the same node is processed again, and a later `ReportUnresolved` can emit a second, generic `"Unresolved type"` diagnostic. The existing `invalidIds` / `invalidScopeds` mechanism already solves the equivalent problem for variable templates; types need the same treatment.

### 2.2 Overload failure state is tied to the resolver instance

`Resolver` tracks failed call sites with three `HashSet<Expr>` fields. These are not visible to post-resolution passes, cannot be tested in isolation, and are queried by `ReportUnresolved` with ad-hoc `Contains` checks. Consolidating them into `ResolutionResult` makes failure state first-class and prepares the ground for richer overload diagnostics.

## 3. Design

### 3.1 Add failure kinds and a call-failure record

In `backend/Resolver/ResolutionResult.cs`, add:

```csharp
public enum InvalidCallKind
{
    Ambiguous,
    NoMatchingOverload,
    TemplateArityMismatchSingle,
    TemplateArityMismatchMultiple,
}

public sealed record InvalidCallInfo(
    InvalidCallKind Kind,
    string          Name,
    Decl?           AttemptedDecl,
    int             TemplateArgCount );
```

`AttemptedDecl` stores the single candidate for `TemplateArityMismatchSingle`. The multiple-candidates case does not need the candidate list for the current refactor because the existing diagnostic only uses the name and the argument count. A later spinoff can extend `InvalidCallInfo` with a candidate list without changing the basic storage shape.

### 3.2 Add invalid dictionaries

In `ResolutionResult` add two private dictionaries next to the existing ones:

```csharp
private readonly Dictionary<TypespecNested, Decl?> invalidTypes = new();
private readonly Dictionary<Expr, InvalidCallInfo> invalidCalls = new();
```

Expose them read-only so unit tests and future diagnostics can inspect them:

```csharp
public IReadOnlyDictionary<TypespecNested, Decl?> InvalidTypes => invalidTypes;
public IReadOnlyDictionary<Expr, InvalidCallInfo> InvalidCalls => invalidCalls;
```

### 3.3 Add helper methods

Add typed `MarkInvalid`, `IsInvalid`, and `IsResolvedOrInvalid` overloads for each key kind.

```csharp
// Types
public void MarkInvalid( TypespecNested type, Decl? attemptedDecl = null )
    => invalidTypes[type] = attemptedDecl;

public bool IsInvalid( TypespecNested type )
    => invalidTypes.ContainsKey( type );

public bool IsResolvedOrInvalid( TypespecNested type )
    => types.ContainsKey( type ) || invalidTypes.ContainsKey( type );

// Calls
public void MarkInvalidCall( Expr callee, InvalidCallInfo info )
    => invalidCalls[callee] = info;

public bool IsInvalidCall( Expr callee )
    => invalidCalls.ContainsKey( callee );

public bool TryGetInvalidCall( Expr callee, out InvalidCallInfo? info )
    => invalidCalls.TryGetValue( callee, out info );

// Expression-level aggregate (reports failure from any source)
public bool IsInvalid( Expr expr )
    => expr switch {
        IdExpr     id     => invalidIds.ContainsKey( id ) || invalidCalls.ContainsKey( id ),
        ScopedExpr scoped => invalidScopeds.ContainsKey( scoped ) || invalidCalls.ContainsKey( scoped ),
        _                 => invalidCalls.ContainsKey( expr ),
    };

public bool IsResolvedOrInvalid( Expr expr )
    => expr switch {
        IdExpr     id     => IsResolvedOrInvalid( id ),
        ScopedExpr scoped => IsResolvedOrInvalid( scoped ),
        _                 => IsInvalidCall( expr ),
    };
```

Also update the existing `IsResolvedOrInvalid` overloads to include call failures for identifiers and scoped names, so that a failed call site is not later reported as an unresolved identifier:

```csharp
public bool IsResolvedOrInvalid( IdExpr id )
    => ids.ContainsKey( id )
    || invalidIds.ContainsKey( id )
    || invalidCalls.ContainsKey( id );

public bool IsResolvedOrInvalid( ScopedExpr scoped )
    => scopeds.ContainsKey( scoped )
    || invalidScopeds.ContainsKey( scoped )
    || invalidCalls.ContainsKey( scoped );
```

### 3.4 Keep `Apply()` unchanged

`Apply()` only iterates the four valid dictionaries (`ids`, `types`, `scopeds`, `members`). Invalid entries live in separate dictionaries and are never written onto `resolvedDecl`. Failed types will remain with `resolvedDecl == null`, so `TypeResolver`, `TypeChecker`, and the generator treat them as unresolved. Failed calls have no `resolvedDecl` to update.

### 3.5 `TryGetResolved` semantics

`TryGetResolved` for `TypespecNested` continues to return `false` for an invalid type. Callers that need to know whether a node was attempted and rejected can use `IsInvalid` or `TryGetInvalidCall`.

## 4. Resolver Changes

All changes are in `backend/Resolver/Resolver.cs` unless otherwise noted.

### 4.1 Remove the HashSets

Delete the three fields:

```csharp
private readonly HashSet<Expr> ambiguousCalls = new();
private readonly HashSet<Expr> noMatchingCalls = new();
private readonly HashSet<Expr> templateArityMismatchCalls = new();
```

### 4.2 `ResolveTypes` — mark invalid class/struct templates

Change the skip check at the top of the loop:

```csharp
foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
    if( result.IsResolvedOrInvalid( unresolved.Node ) )
        continue;
    // ... existing dependency and resolution logic ...
```

Replace the diagnostic-only arity-mismatch branch with a mark-and-continue:

```csharp
if( TemplateInference.HasTemplateArityMismatch( resolved, providedArgs )
 && !IsInjectedClassName( unresolved.Node, resolved, unresolved.Scope ) ) {
    diagnostics.Add( new Diagnostic(
        unresolved.Node.srcPos,
        DiagnosticKind.Error,
        String.Format( "Template argument count mismatch for type '{0}': expected {1}, got {2}",
            GetTypeName( unresolved.Node ),
            TemplateInference.GetTemplateParamCount( resolved ),
            providedArgs.Count ) ) );

    result.MarkInvalid( unresolved.Node, resolved );
    continue;
}
```

This stops the diagnostic from being emitted repeatedly and prevents `ReportUnresolved` from emitting a fallback `"Unresolved type"`.

### 4.3 `ResolveSingleCall` — short-circuit permanently failed calls

To avoid re-diagnosing terminal failures and to reduce work, skip the call if it is already marked invalid:

```csharp
private bool ResolveSingleCall( UnresolvedCall call, GlobalNamespace module )
{
    if( result.IsInvalidCall( call.Callee ) )
        return false;

    // ... existing body ...
}
```

This short-circuit is safe because the only failure states that are written into `invalidCalls` are terminal: ambiguity, no matching overload, and template arity mismatch. Calls that could still succeed because argument types are not yet known return `noMatch = false` from `TryResolveOverload` and are not marked invalid.

### 4.4 `RecordOverloadFailure` — use `ResolutionResult`

Rewrite to store the failure in `result`:

```csharp
private void RecordOverloadFailure(
    (Decl? chosen, bool ambiguous, bool noMatch) outcome,
    Expr   callee,
    string name )
{
    if( result.IsInvalidCall( callee ) )
        return;

    if( outcome.ambiguous ) {
        result.MarkInvalidCall( callee, new InvalidCallInfo(
            InvalidCallKind.Ambiguous,
            name,
            null,
            0 ) );

        diagnostics.Add( new Diagnostic(
            GetCalleeSrcPos( callee ),
            DiagnosticKind.Error,
            String.Format( "Ambiguous call to '{0}'", name ) ) );
    }
    else if( outcome.noMatch ) {
        result.MarkInvalidCall( callee, new InvalidCallInfo(
            InvalidCallKind.NoMatchingOverload,
            name,
            null,
            0 ) );

        diagnostics.Add( new Diagnostic(
            GetCalleeSrcPos( callee ),
            DiagnosticKind.Error,
            String.Format( "No matching overload for '{0}'", name ) ) );
    }
}
```

The old guard `if( !templateArityMismatchCalls.Contains( callee ) )` disappears because template arity mismatches are recorded with their own `InvalidCallKind` and the top-of-method `IsInvalidCall` guard suppresses the generic message.

### 4.5 `ReportTemplateArityMismatch` — use `ResolutionResult`

Both overloads should record an invalid call and then emit the existing message. Add a guard at the start of each to avoid duplicate diagnostics:

```csharp
private void ReportTemplateArityMismatch(
    Decl                  candidate,
    IReadOnlyList<TplArg> args,
    Expr                  callee )
{
    if( result.IsInvalidCall( callee ) )
        return;

    int expected = TemplateInference.GetTemplateParamCount( candidate );
    int actual   = args.Count;
    string name  = GetTemplateCalleeName( callee );

    result.MarkInvalidCall( callee, new InvalidCallInfo(
        InvalidCallKind.TemplateArityMismatchSingle,
        name,
        candidate,
        actual ) );

    diagnostics.Add( new Diagnostic(
        GetCalleeSrcPos( callee ),
        DiagnosticKind.Error,
        expected == 0
            ? String.Format( "'{0}' is not a template; 0 template arguments expected, {1} provided", name, actual )
            : String.Format( "Template argument count mismatch for '{0}': expected {1}, got {2}", name, expected, actual ) ) );
}

private void ReportTemplateArityMismatch(
    List<Decl>            candidates,
    IReadOnlyList<TplArg> args,
    Expr                  callee )
{
    if( result.IsInvalidCall( callee ) )
        return;

    string name = GetTemplateCalleeName( callee );

    result.MarkInvalidCall( callee, new InvalidCallInfo(
        InvalidCallKind.TemplateArityMismatchMultiple,
        name,
        null,
        args.Count ) );

    diagnostics.Add( new Diagnostic(
        GetCalleeSrcPos( callee ),
        DiagnosticKind.Error,
        String.Format( "No matching template overload for '{0}' with {1} template argument(s)", name, args.Count ) ) );
}
```

### 4.6 `ReportUnresolved` — skip all invalid/failed resolutions

Update the four reporting loops to use the unified helpers.

For unresolved identifiers:

```csharp
foreach( UnresolvedId unresolved in context.UnresolvedIds ) {
    if( result.IsResolvedOrInvalid( unresolved.Node ) )
        continue;

    // remove the old ambiguousCalls / noMatchingChecks checks
    // ...
}
```

Do the same for unresolved scoped names:

```csharp
foreach( UnresolvedScoped unresolved in context.UnresolvedScopeds ) {
    if( result.IsResolvedOrInvalid( unresolved.Node ) )
        continue;

    // ...
}
```

For unresolved types:

```csharp
foreach( UnresolvedType unresolved in context.UnresolvedTypes ) {
    if( result.IsResolvedOrInvalid( unresolved.Node ) )
        continue;

    if( unresolved.Node.IsDependentType() )
        continue;

    // ...
}
```

For unresolved member accesses, replace the HashSet checks with an aggregate expression check. The unresolved node is the `BinOp`; it is the same expression object that will be stored as a failed call callee:

```csharp
foreach( UnresolvedMemberAccess unresolved in context.UnresolvedMemberAccesses ) {
    if( unresolved.Node.right is not IdExpr member )
        continue;

    if( result.Members.ContainsKey( member ) )
        continue;

    if( result.IsResolvedOrInvalid( unresolved.Node ) )
        continue;

    // ...
}
```

Because `IsResolvedOrInvalid(Expr)` falls back to `IsInvalidCall`, member-access call failures are skipped the same way identifier call failures are.

## 5. Files and Methods Changed

| File | Method / Location | Change |
|------|-------------------|--------|
| `backend/Resolver/ResolutionResult.cs` | class body | Add `InvalidCallKind`, `InvalidCallInfo`, `invalidTypes`, `invalidCalls`, `MarkInvalid(TypespecNested)`, `MarkInvalidCall`, `IsInvalid(TypespecNested)`, `IsInvalidCall`, `TryGetInvalidCall`, `IsInvalid(Expr)`, and update `IsResolvedOrInvalid` overloads. |
| `backend/Resolver/Resolver.cs` | fields | Remove `ambiguousCalls`, `noMatchingCalls`, `templateArityMismatchCalls`. |
| `backend/Resolver/Resolver.cs` | `ResolveTypes` | Skip resolved-or-invalid types; `MarkInvalid` on class/struct template arity mismatch. |
| `backend/Resolver/Resolver.cs` | `ResolveSingleCall` | Early `return false` if `result.IsInvalidCall`. |
| `backend/Resolver/Resolver.cs` | `RecordOverloadFailure` | Record `InvalidCallInfo` in `result`; emit same diagnostic messages. |
| `backend/Resolver/Resolver.cs` | `ReportTemplateArityMismatch` (both) | Record `InvalidCallInfo`; emit same diagnostic messages; guard against duplicates. |
| `backend/Resolver/Resolver.cs` | `ReportUnresolved` | Replace HashSet and `ContainsKey` checks with `IsResolvedOrInvalid` for identifiers, scoped names, types, and member accesses. |
| `testing/cases/class_template_arg_count_fail/main.myll` | new | Negative integration case for class-template arity mismatch. |
| `testing/cases/function_template_arg_count_fail/main.myll` | new | Negative integration case for function-template arity mismatch on a call. |
| `testing/caseconfig.json` | `depth.generateFailing` | Add the two new failure cases. |
| `testing/ResolutionResultTests.cs` | new | Unit tests for invalid type and invalid call helpers. |

## 6. Implementation Order

1. Extend `ResolutionResult` with the new dictionaries, records, enums, and helpers. Keep `Apply` unchanged.
2. In `Resolver.cs`, delete the three HashSet fields.
3. Update `ResolveTypes` to skip and mark invalid class/struct template types.
4. Update `ResolveSingleCall` with the `IsInvalidCall` short-circuit.
5. Update `RecordOverloadFailure` to use `result.MarkInvalidCall` and preserve messages.
6. Update both `ReportTemplateArityMismatch` overloads similarly.
7. Update `ReportUnresolved` to use `IsResolvedOrInvalid` everywhere.
8. Build and run existing unit tests to catch type-resolution regressions.
9. Add the two negative integration cases and register them in `caseconfig.json`.
10. Add `testing/ResolutionResultTests.cs` covering all new helpers and `Apply`.
11. Run the full test suite.

## 7. Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Reference equality for `Expr` / `TypespecNested` keys does not match across passes. | The new dictionaries use the same object-reference scheme that the existing valid dictionaries already use. No change in identity semantics. |
| `Apply()` accidentally writes an invalid entry to `resolvedDecl`. | Invalid state lives in separate dictionaries; `Apply()` is left untouched by the refactor. |
| A duplicate diagnostic is emitted for a failed call. | `RecordOverloadFailure` and both `ReportTemplateArityMismatch` overloads guard with `result.IsInvalidCall` before storing or reporting. |
| `ReportUnresolved` starts emitting a fallback diagnostic for previously suppressed call failures. | `IsResolvedOrInvalid` for `IdExpr`, `ScopedExpr`, and the generic `Expr` overload consult `invalidCalls`, so failed call callees are skipped. |
| `ResolveSingleCall` short-circuits a call that could later succeed. | Only terminal failures (ambiguity, no match, template arity mismatch) are stored. Calls deferred because argument types are still unknown are not marked invalid. |
| Multi-module resolution shares one `ResolutionResult`, causing cross-module key collisions. | AST node instances are module-specific; a `BinOp` from module A cannot equal a `BinOp` from module B. |
| The fixed-point loop keeps running because invalid marking counts as progress. | Invalid marking does not set `progress = true` in `ResolveTypes` and `ResolveSingleCall` returns `false`. |
| Existing `invalidIds` / `invalidScopeds` behavior changes. | The new helpers keep the old behavior and only add new overloads and the extra call-failure check inside `IsResolvedOrInvalid`. |

## 8. Future Dependency: Context-Aware Template Inference

The resolver currently runs name resolution and template argument deduction before `TypeChecker.CheckAssignment` sees the target type of an assignment or initialization. This means target-type-driven inference, such as `var f32 f = std::numbers::pi_v;`, cannot use the declared `f32` type during `TemplateInference.TryDeduceTemplateArgs`. That known design tension is unchanged by this refactor.

Recording arity-mismatch failures in `ResolutionResult` is terminal because a wrong argument count can never be fixed by a target type. However, richer inference that is added later will need either:

- propagation of the expected type into the resolver so deduction can use it, or
- a second inference pass inside type checking that can update or override `ResolutionResult` entries.

## 9. Spinoff Idea: Rich Overload Diagnostics

Once overload failures live in `ResolutionResult`, `InvalidCallInfo` can be extended with a list of candidate signatures without touching the rest of the pipeline. A future diagnostic enhancement could list the candidates for ambiguous calls and the overload set for no-matching calls, producing messages like:

```
Ambiguous call to 'pick': candidate pick(i32), candidate pick(i64)
No matching overload for 'dispatch'; candidates: dispatch(int), dispatch(f32)
```

This is out of scope for the current refactor and is recorded here as a follow-up.

## 10. Testing Plan

### 10.1 Unit tests for `ResolutionResult` helpers

Create `testing/ResolutionResultTests.cs` and add independent tests for:

- `MarkInvalid(TypespecNested)` / `IsInvalid(TypespecNested)` / `IsResolvedOrInvalid(TypespecNested)`.
- `MarkInvalidCall` / `IsInvalidCall` / `TryGetInvalidCall` for `IdExpr`, `ScopedExpr`, `BinOp`, and unknown `Expr` subtypes.
- `IsResolvedOrInvalid` for an `IdExpr` that is only in `invalidCalls`.
- That `Apply()` does not set `resolvedDecl` on invalid types or expressions.
- That a type or expression marked invalid still returns `false` from the matching `TryGetResolved` / `Ids.TryGetValue` / `Scopeds.TryGetValue` lookups.

Use hand-built `IdExpr`, `ScopedExpr`, `TypespecNested`, and `BinOp` instances with distinct names. Hand-built AST objects are fine because the helpers only require object identity and the specific fields are not read during these tests.

### 10.2 New negative integration cases

Create `testing/cases/class_template_arg_count_fail/main.myll`:

```myll
module class_template_arg_count_fail;

class Wrapper<T> { }

func main() -> int
{
    var Wrapper<int, float> w;
    return 0;
}
```

Create `testing/cases/function_template_arg_count_fail/main.myll`:

```myll
module function_template_arg_count_fail;

func identity<T>(T x) -> T { return x; }

func main() -> int
{
    identity<int, float>( 1 );
    return 0;
}
```

Add both directory names to `testing/caseconfig.json` under `depth.generateFailing`.

### 10.3 Manual verification

For each new failure case, run:

```bash
dotnet test testing/ --filter "DisplayName~class_template_arg_count_fail"
dotnet test testing/ --filter "DisplayName~function_template_arg_count_fail"
```

Inspect the test output and confirm:

1. Myll exits with a non-zero status.
2. The diagnostic is emitted exactly once.
3. There is no second `"Unresolved type"` or `"Unresolved identifier"` message for the same node.

### 10.4 Regression checks

Run the full suite and the existing template and overload cases:

```bash
dotnet test testing/
dotnet test testing/ --filter "DisplayName~template_arg_count_fail"
dotnet test testing/ --filter "DisplayName~overload_fail"
```

Confirm that existing diagnostics and exit codes remain unchanged.
