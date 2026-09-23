# Local variable templates: handling in `VStmt.VisitDefVar`

## Context

Variable templates at module and class/struct scope were recently implemented.
The grammar now allows `tplParams?` on every `idAccessor`, which means the same syntax is also accepted for local variable declarations inside functions/blocks.

At module/class scope, `backend/Visitor/VDecl.cs` handles the template parameters correctly:

- It calls `VisitTplParams(q.tplParams())` for each declared name.
- It pushes a dedicated `varScope` so the template parameter names are visible in the type and initializer without leaking.
- It stores the parameters in `Core.VarDecl.TplParams`.
- `backend/Transform/TemplateParamTransformer.cs` then injects hidden `TplParamDecl` symbols into `varScope` for resolution.
- `backend/Generator/HierachicalGen.cs` emits `template <typename ...>` around the generated variable.

At local scope, `backend/Visitor/VStmt.cs::VisitDefVar` currently ignores `q.tplParams()` entirely.
This is a bug: code such as `var int x<T> = 42;` inside a function parses and silently drops the `<T>`, producing a plain local variable.

## Decision

Explicitly reject local variable templates with a clear diagnostic.
Do not extend `VarStmt` or local `VarDecl` to carry template parameters.

### Rationale

- C++ does not support template declarations at block/function scope.
  A C++ variable template can only live at namespace or class scope.
- Adding full local support would require either lowering to a fundamentally different C++ construct (for example, a generic lambda or a local class template) or inventing Myll-only semantics that have no clean C++23 equivalent.
- The practical need can already be met by lifting the variable template to module scope (using `static`/`internal` linkage where needed) or converting it into a function template.
- The current code already has a clear, working model for module/class variable templates; keeping local variables simple avoids duplicating that machinery inside statement generation.

## Proposed changes

### 1. Emit a diagnostic in `backend/Visitor/VStmt.cs`

Edit `backend/Visitor/VStmt.cs`, method `VisitDefVar` (`backend/Visitor/VStmt.cs:175-225`).

For every `IdAccessorContext q` produced by `c.typedIdAcors().idAccessors().idAccessor()`:

1. Compute `List<TplParam> tplParams = VisitTplParams(q.tplParams());`.
2. If `tplParams.Count >= 1`, add an error diagnostic and skip creation of the local `VarStmt` for this accessor.

A minimal implementation would look like this:

```csharp
List<TplParam> tplParams = VisitTplParams( q.tplParams() );
if( tplParams.Count >= 1 ) {
    Context.Diagnostics.Add( new Diagnostic(
        q.tplParams()?.ToSrcPos() ?? srcPos,
        DiagnosticKind.Error,
        "Local variable templates are not supported; template parameters are only allowed on variables at module or class/struct scope." ) );

    return null;
}
```

The surrounding `.Select(...)` must tolerate a `null` return, or the diagnostic branch can return a no-op placeholder statement that is not emitted.
The cleanest option is to collect into a list and skip the `null` entries, so that non-templated declarations in the same `defVar` are still accepted.

### 2. Keep existing module/class handling unchanged

`backend/Visitor/VDecl.cs::VisitDefVar` (`backend/Visitor/VDecl.cs:1043-1096`) already supports variable templates and must stay as-is.
No changes are required there.

### 3. No AST additions needed

For the rejection path, do not extend these types:

- `backend/Core/Stmt.cs::VarStmt` (`backend/Core/Stmt.cs:46-95`) should not gain a `TplParams` field.
- `backend/Core/Decl.cs::VarDecl` (`backend/Core/Decl.cs:247-269`) already has `TplParams` and `varScope` for the module/class path; nothing needs to be added for locals.
- `backend/Transform/TemplateParamTransformer.cs` (`backend/Transform/TemplateParamTransformer.cs:32-34`) already handles `VarDecl` templates; because locals will never carry template parameters, no new injection site is necessary.

### 4. Code generation impact

`backend/Generator/HierachicalGen.cs::AddVar` (`backend/Generator/HierachicalGen.cs:566-578`) only sees `VarDecl` nodes that are children of a `Hierarchical` declaration.
Local variables are emitted by `VarStmt.Gen()` (`backend/Core/Stmt.cs:56-94`), which never sees template parameters.
Therefore, no generator changes are required.

## Risks and interactions

| Risk | Mitigation |
|---|---|
| The diagnostic is emitted during AST construction, so type-checker errors for the undefined template parameter name are avoided by skipping the declaration. | Return `null`/skip the `VarStmt` when template parameters are present; do not fall through to the normal variable path. |
| Multiple declarators in one `defVar` (e.g. `var int a, b<T>, c;`) should report the error only for the templated one while still accepting `a` and `c`. | Check template parameters inside the per-accessor lambda, and filter out the failing accessor from the resulting statement list. |
| `for` loop initializers use the same `VisitDefVar` path, so `for( var int i<T> = 0; ... )` will also be rejected. | This is the desired behavior; the diagnostic message is generic enough to cover all block scopes. |
| Existing module/class variable templates such as `testing/cases/var_templates` must continue to compile. | Only `VStmt.VisitDefVar` is changed; `VDecl.VisitDefVar` is untouched. |
| Adding a `Diagnostic` instead of throwing keeps the compiler pipeline running and may produce other diagnostics for the same file. | Document that this is intentional; the first error is the user-facing rejection. |

## Testing plan

1. Add a failing integration case `testing/cases/local_var_template_fail/main.myll`:

```myll
module local_var_template_fail;

func main() -> int {
    var int x<T> = 42;
    return 0;
}
```

2. Register the case as `GenerateFailing` in `/home/sor/myll/testing/caseconfig.json` under `depth.generateFailing`.
3. Run the new case:

```bash
dotnet test testing/ --filter "DisplayName~local_var_template_fail"
```

4. Verify that the existing variable-template case still generates and runs:

```bash
dotnet test testing/ --filter "DisplayName~var_templates"
```

5. Run the full integration suite to ensure no regressions:

```bash
dotnet test testing/
```

## Future work

If the language design later decides to support local generic constants, revisit this by choosing a real lowering strategy.
Two obvious candidates are:

- A Myll-local rewriting that lifts a local variable template into an anonymous namespace-level template with internal linkage.
- A new expression form that lowers to a C++ generic lambda for local polymorphic values.

Until then, the explicit diagnostic is the correct, safe behavior.
