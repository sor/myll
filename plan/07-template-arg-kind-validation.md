# Plan: template argument kind validation

> Status: **Implemented**. This plan is kept for reference; see commit `c62beb8` and earlier for the actual changes.

This plan describes how to extend Myll's template-argument checking so that it rejects obvious mismatches between what a template parameter expects (a type or a non-type value) and what the call site provided.
The immediate goal is to catch mistakes such as `Box<42>` for a type parameter `T`, not to implement full C++ template semantics.

The related variables and entry points are in `/home/sor/myll/backend/Resolver/TemplateInference.cs` (`ValidateTemplateArgs`, `TplArg`, `TplParam`).
The main call sites are in `/home/sor/myll/backend/Resolver/Resolver.cs`, with follow-up touches in the generator, visitor, and `std/` bindings.

## 1. Current state (implemented)

- `TplArg` (`/home/sor/myll/backend/Core/Mixed.cs`) carries either a `Typespec` (`typespec`) or an expression literal (`lit`).
- `TplParam` now carries a `kind` (`Type`, `NonType`, or `Unknown`) and, for non-type parameters, a `type`.
- `TemplateInference.ValidateTemplateArgs` checks both arity and argument kind. `HasTemplateArityMismatch` is a thin wrapper that returns `ValidateTemplateArgs(...).IsMismatch`.
- The validation is invoked for:
  - variable templates (`Resolver.TryResolveVariableTemplate`);
  - type names (`Resolver.ResolveTypes`);
  - function/structor overloads with explicit template arguments (`Resolver.TryResolveOverload`).
- `TemplateInference.TryDeduceTemplateArgs` only binds `Type`/`Unknown` parameters; candidates containing `NonType` parameters must have those supplied explicitly.
- The C++ generator uses a shared `GenTemplateParameter` helper that emits `typename T` for type parameters and `int N` for non-type parameters.

The grammar now supports both type parameters (`T`) and non-type parameters (`int N`) via the `tplParam` rule, and the standard library binding `std/std_array.decl.myll` declares `std::array<T, int N>`.

## 2. Data-model changes

Add a `Kind` field to `TplParam` in `/home/sor/myll/backend/Core/Mixed.cs`.
Use an enum with at least these values so the implementation can stay conservative:

- `Type` — the parameter stands for a type (the current default for every Myll-defined template).
- `NonType` — the parameter stands for a value (for example an integer constant).
- `Unknown` — the compiler has no kind information; validation falls back to arity-only checks.

```csharp
public enum TplParamKind
{
    Type,
    NonType,
    Unknown,
}

public class TplParam
{
    public string       name = null!;
    public TplParamKind kind = TplParamKind.Type; // safe existing default
    // For NonType parameters, filled in once non-type syntax is added.
    public Typespec?    type = null;
}
```

Add a small helper on `TplArg` to classify the argument:

```csharp
public bool IsTypeArgument  => typespec != null;
public bool IsValueArgument => lit != null;
```

Also update the temporary `TplParamDecl` placeholder (`/home/sor/myll/backend/Core/Decl.cs`) to mirror the kind of the source `TplParam` once kinds are introduced.

## 3. Validation API (replace the pure arity check)

Introduce a richer result object in `/home/sor/myll/backend/Resolver/TemplateInference.cs` and a new entry point.
Keep `HasTemplateArityMismatch` as a thin wrapper initially so callers can migrate one by one.

```csharp
public enum TemplateArgMismatchKind
{
    None,
    Arity,
    ArgumentKind,
}

public readonly record struct TemplateArgValidation(
    bool                  IsMismatch,
    TemplateArgMismatchKind MismatchKind,
    int?                  MismatchIndex,    // 0-based index into the argument list
    string?               ParameterName,
    string?               Expected,         // "type" or "non-type value"
    string?               Actual );         // the offending argument text

public static TemplateArgValidation ValidateTemplateArgs(
    Decl                   candidate,
    IReadOnlyList<TplArg>? explicitArgs )
{
    // 1. Compare counts exactly as HasTemplateArityMismatch does today.
    // 2. If counts match, walk arguments and parameters in parallel.
    //    For each parameter that is Type, require arg.IsTypeArgument.
    //    For each parameter that is NonType, require arg.IsValueArgument.
    //    For Unknown, skip the kind check.
}
```

Rules to preserve:

- A non-template (`expected == 0`) with explicit arguments is still a mismatch.
- A template function with `actual == 0` is still allowed (deduction may fill the arguments).
- A non-function template with `actual == 0` is still a mismatch.

The helper must not crash when `explicitArgs` is `null`; treat it as an empty list.

## 4. Files and methods that change

### 4.1 `/home/sor/myll/backend/Core/Mixed.cs`

- Add `TplParamKind` and update `TplParam` as described in section 2.
- Add `TplArg` classification helpers.

### 4.2 `/home/sor/myll/backend/Resolver/TemplateInference.cs`

- Add `TemplateArgValidation` and `ValidateTemplateArgs`.
- Keep `HasTemplateArityMismatch` as `ValidateTemplateArgs(...).IsMismatch` until all callers are migrated.
- Harden `SubstituteTemplateParams` for literal arguments:
  - In the substitution dictionary construction, only add entries when `args[i].typespec != null`.
  - In the nested-type recursion, preserve literal arguments unchanged (the check `a.typespec == null ? null : Substitute(...)` already does this; make sure it stays that way).
- Update `TryDeduceTemplateArgs` so it only expects to bind parameters whose kind is `Type` or `Unknown`.
  Non-type parameters are not deduced by Myll.
  This also lets mixed parameter lists such as `<int N, T>` work when `T` can be deduced from a function argument.

### 4.3 `/home/sor/myll/backend/Resolver/Resolver.cs`

- `TryResolveVariableTemplate` (`Resolver.cs:322`):
  - Call `ValidateTemplateArgs(varDecl, idTpl.tplArgs)` before substitution.
  - On mismatch, add a diagnostic and return `false`.
- `ResolveTypes` (`Resolver.cs:381`):
  - Replace the `HasTemplateArityMismatch` call with `ValidateTemplateArgs`.
  - On mismatch, emit a diagnostic and `continue`.
- `TryResolveOverload` (`Resolver.cs:834`):
  - In the explicit-template-arguments branch, use `ValidateTemplateArgs` per candidate.
  - Treat a kind mismatch the same way an arity mismatch is treated today (skip the candidate, set `templateArityMismatch`, let the existing reporting path handle a final failure).
- Rename the `templateArityMismatchCalls` hash set to `templateMismatchCalls` and rename `ReportTemplateArityMismatch` to `ReportTemplateMismatch`.
  The new helper should produce either the existing count message or a new kind-specific message.

### 4.4 `/home/sor/myll/backend/Generator/HierachicalGen.cs`

- Replace every hard-coded `"typename " + p.name` with a call to a new generator helper, for example `p.GenTemplateParameter()`.
- The helper emits:
  - `Type`: `typename {name}`.
  - `NonType`: the parameter's declared type followed by the name, for example `int N`.
    This requires the `TplParam.type` field from section 2.
  - `Unknown`: `typename {name}` as the conservative fallback.
- Locations that need the update are the ones that currently construct template headers:
  - variable templates (`AddVar`, ~ line 571);
  - functions/methods (`AddFunc`, ~ line 620);
  - structs/classes (`AddHierarchicalGen`, ~ line 980 and ~ line 1299).

### 4.5 (Optional later) Grammar and visitor for non-type parameters

If Myll is to declare its own non-type parameters, extend the grammar:

```antlr
tplParam
    : id                # TypeTplParam
    | typespec id       # NonTypeTplParam
    ;
tplParams : LT tplParam (COMMA tplParam)* COMMA? GT;
```

Then update `/home/sor/myll/backend/Visitor/VTpl.cs`:

```csharp
public new TplParam VisitTplParam( TplParamContext c )
{
    if( c.typespec() != null )
        return new TplParam {
            name = c.id().Visit(),
            kind = TplParamKind.NonType,
            type = VisitTypespec( c.typespec() ),
        };

    return new TplParam { name = c.id().Visit(), kind = TplParamKind.Type };
}
```

This can be deferred; the validation changes in sections 4.1-4.4 are useful even without it because they already reject `Box<42>`.

### 4.6 `/home/sor/myll/backend/Resolver/OperatorResolver.cs`

- No behavioral change is required, but verify that `SubstituteMethodReturnType` does not dereference `arg.typespec` when a literal argument is present.
  The existing guard (`if( arg.typespec is TypespecNested ... )`) is safe; document the assumption.

## 5. Diagnostic messages

Keep the style of the existing messages.
Use the callee source position (`GetCalleeSrcPos`).

- Arity mismatch (single known candidate):
  - same as today: `Template argument count mismatch for '{name}': expected {expected}, got {actual}`.
  - or for a non-template: `'{name}' is not a template; 0 template arguments expected, {actual} provided`.
- Kind mismatch (single known candidate):
  - `Template argument kind mismatch for '{name}': parameter {index} ('{paramName}') expects a {expected}, but argument '{arg}' is a {actual}`.
  - Examples:
    - `... expects a type, but argument '42' is a value`.
    - `... expects a non-type value, but argument 'int' is a type`.
- Multiple candidates, at least one rejected:
  - Keep the current generic message: `No matching template overload for '{name}' with {count} template argument(s)`.
  - Optionally append the first concrete reason, for example `(parameter 'T' expects a type, got value '42')`.

For variable templates, the message can be more specific because there is exactly one candidate:

- `Template argument kind mismatch for variable '{name}': parameter {index} ('{paramName}') expects a type, but argument '42' is a value`.

## 6. Interaction with deduction

- `TryDeduceTemplateArgs` must only bind `Type` (and `Unknown`, treated optimistically as type) parameters.
  After deduction, if any type parameter is still unbound, the candidate is rejected.
  Non-type parameters are considered the caller's responsibility and are ignored during deduction.
- When explicit template arguments are supplied, no deduction is performed.
  The new kind check runs instead and must pass before overload ranking starts.
- Deduction produces new `TplArg` objects whose `typespec` is set; therefore every deduced argument is a valid type argument.

## 7. Interaction with external C++ templates

External and forward declarations are already handled specially in `TryResolveOverload` (`Resolver.cs:854`):

- When there are no explicit template arguments and the external template is the only same-arity candidate, it is accepted without Myll-side deduction.
- When it competes with internal candidates, Myll tries to deduce its type arguments.

The same principle applies to kind validation:

- If an external declaration uses the new non-type syntax, its `TplParam` records `NonType` and normal validation runs.
- If an external declaration only has a name (`Kind == Unknown`), validation falls back to the current arity-only behavior.
  This avoids rejecting valid C++ templates that Myll does not fully understand.
- For external variable templates, the resolver must not call `SubstituteTemplateParams` with literal arguments unless the parameter kind is known to be `NonType`.
  In the first milestone, variable templates are assumed to have type parameters (`Kind == Type`), so a literal argument is a kind mismatch and is rejected before substitution.

## 8. Risks and how to mitigate them

| Risk | Mitigation |
|------|------------|
| Rejecting a valid external C++ template because Myll mis-classifies a parameter. | Use `Unknown` as the default kind for parameters parsed from legacy declarations. Only enforce kind checks when the kind is explicitly known. |
| Breaking overload resolution for templates that mix type and non-type parameters. | Update `TryDeduceTemplateArgs` to count only type parameters. Require non-type parameters to be supplied explicitly until deduction is extended. |
| `SubstituteTemplateParams` throwing on `args[i].typespec!` when a non-type argument passes through. | Guard the dictionary insertion with `args[i].typespec != null`. Report kind mismatches before substitution at every call site. |
| Parameter packs (`template<typename... Ts>`) or template-template parameters. | Out of scope for this pass. If they later appear, mark them `Unknown` or add dedicated `Kind` values and skip the simple kind check. |
| Existing generated code changes for every template header because of a new helper. | Keep the default `Kind == Type`, so every currently valid Myll program still emits `template <typename T>`. |

## 9. Testing steps

1. Build the project: `dotnet build myll.sln`.
2. Run the existing test suite: `dotnet test testing/`.
   All current template cases, especially `/home/sor/myll/testing/cases/var_templates/`, `/home/sor/myll/testing/cases/template_basics/`, `/home/sor/myll/testing/cases/class_templates/`, and `/home/sor/myll/testing/cases/overload_template_ranking/`, must still pass.
3. Build the coreutil sample apps: `bash frontend/coreutil/make_coreutil.sh`.
4. Add new regression cases:
   - `/home/sor/myll/testing/cases/template_arg_kind_fail/`:
     - class template with a literal argument: `Box<42>` should fail with a kind mismatch.
     - function template with a literal argument: `identity<42>(1)` should fail.
     - variable template with a literal argument: `answer<42>` should fail.
   - (After grammar support) `/home/sor/myll/testing/cases/non_type_template/`:
     - `func f<int N>() -> int { return N; }` with `f<5>()` succeeds.
     - `f<int>()` fails because a type is passed where a value is expected.
   - `/home/sor/myll/testing/cases/external_non_type_template/` (optional):
     - A `.decl.myll` binding with `[extern] template<typename T, int N> void use();` used correctly should still compile.
5. Verify that diagnostics point at the template argument, not at the declaration, by checking `GetCalleeSrcPos` behavior.
6. Run `dotnet test testing/ --filter "DisplayName~template"` to isolate template-related tests.

## 10. Migration order (all completed)

1. ✅ Add `TplParamKind` and default all existing parameters to `Type`.
2. ✅ Implement `ValidateTemplateArgs` and make `HasTemplateArityMismatch` delegate to it.
3. ✅ Migrate the three resolver call sites (`TryResolveVariableTemplate`, `ResolveTypes`, `TryResolveOverload`) to emit kind-specific diagnostics.
4. ✅ Harden `SubstituteTemplateParams` and update `TryDeduceTemplateArgs` for mixed parameter lists.
5. ✅ Replace the generator's hard-coded `typename` with a `TplParam`-aware helper.
6. ✅ Add regression tests (`non_type_template`, `non_type_template_fail`, `local_var_template_fail`).
7. ✅ Implement the grammar/visitor changes for explicit non-type parameters.
