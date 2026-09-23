# Shared Template-Prefix Helper Plan

## Context

Variable templates were added recently. `backend/Generator/HierachicalGen.cs` now builds the `template <typename T>` prefix in at least five places:

- `AddVar` (lines 567-578) for variable-template declarations and out-of-line definitions.
- `AddFunc` (lines 702-721) for function-template prototypes/declarations and implementations.
- `AddConversionOperator` (lines 866-878) for conversion-operator templates.
- `AddHierarchical` (lines 980-991) for templated classes/structs/namespaces.
- `AddForwardDecl` (lines 1299-1306) for templated forward declarations.

The immediate problem is `AddVar` and `AddFunc`, where the same `Select(p => "typename " + p.name).Join(", ")` + `Format("{0}template <{1}>", indent, ...)` logic is duplicated. This is inconsistent with the formatting-convention rule that C++ keyword fragments live in `*Format` arrays.

## Proposed Helper

Introduce a small private helper in `HierarchicalGen`:

```csharp
private static string BuildTemplatePrefix( string indent, IReadOnlyList<TplParam> tplParams )
    => tplParams.Count == 0
        ? string.Empty
        : Format(
            TemplatePrefixFormat[0],
            indent,
            tplParams.Select( p => TemplateTypenamePrefix + p.name ).Join( ", " ) );
```

Add the formatting fragments to `backend/Generator/StmtFormatting.cs`:

```csharp
internal const string TemplateTypenamePrefix = "typename ";

public static readonly string[] TemplatePrefixFormat = {
    "{0}template <{1}>", // 0 indent, 1 comma-separated "typename name" list
};
```

### Why this location?

`HierarchicalGen` owns the code that repeatedly builds the prefix, so the helper belongs there. The actual string format lives in `StmtFormatting.cs` alongside `VarFormat`, `FuncFormat`, etc., keeping C++ keywords out of generator logic and making future changes to template syntax single-source.

## Usage in `AddVar` and `AddFunc`

### `AddVar`

Replace the current `tplDeclLine` / `tplImplLine` blocks:

```csharp
string tplDeclLine = BuildTemplatePrefix( IndentDecl, obj.TplParams );
string tplImplLine = BuildTemplatePrefix( IndentImpl, obj.TplParams );
```

Then keep the existing emission pattern:

```csharp
if( (emitAt & GenerateAt.Decl) != 0 ) {
    SourceLines retDecl = new();
    if( !IsNullOrEmpty( tplDeclLine ) )
        retDecl.Add( tplDeclLine );
    retDecl.Add( Format( VarFormat[0], ... ) );
    PrependDeclSourceMap( retDecl, obj );
    targetDecl.Add( (obj.access, retDecl) );
}

if( (emitAt & GenerateAt.Impl) != 0 ) {
    SourceLines retImpl = new();
    if( !IsNullOrEmpty( tplImplLine ) )
        retImpl.Add( tplImplLine );
    retImpl.Add( Format( VarFormat[0], ... ) );
    PrependDeclSourceMap( retImpl, obj );
    targetImpl.Add( (obj.access, retImpl) );
}
```

### `AddFunc`

Replace the local `tplDecl` / `tplImpl` construction with:

```csharp
string tplDecl = BuildTemplatePrefix( indentDecl, tplParams );
string tplImpl = BuildTemplatePrefix( indentImpl, tplParams );
```

All existing emission branches continue to add `tplDecl`/`tplImpl` conditionally before the declaration/implementation lines.

### `AddConversionOperator` (opportunistic)

Same replacement as `AddFunc`:

```csharp
string tplDecl = BuildTemplatePrefix( indentDecl, obj.TplParams );
string tplImpl = BuildTemplatePrefix( indentImpl, obj.TplParams );
```

### `AddHierarchical` and `AddForwardDecl` (optional follow-up)

Both already build an identical prefix string. They can also call `BuildTemplatePrefix`:

```csharp
string tpl = BuildTemplatePrefix( indent, hierWithTpl.TplParams );
```

This is a one-line change in each method and removes the last `template <...>` inline format strings from `HierarchicalGen`. It is safe to include in the same PR because the helper is general over `IReadOnlyList<TplParam>`.

## Formatting Edge Cases to Preserve

- **Indentation**: The helper uses the caller's indent string (`IndentDecl`, `IndentImpl`, `DeIndentDecl`, etc.) exactly, so class/namespace/function levels stay aligned.
- **Spacing**: The generated line must remain `    template <typename T>` — one space between `template` and `<`, and `typename ` immediately before each parameter name. The `TemplatePrefixFormat` and `TemplateTypenamePrefix` constants encode this explicitly.
- **Empty parameter list**: When `tplParams.Count == 0` the helper returns `string.Empty`. Callers must keep the conditional `if( !IsNullOrEmpty( tplDecl ) )` guards so no blank `template <>` line is emitted.
- **Parameter separator**: The joined string uses `", "`, consistent with the current output. Do not change to `,` without checking generated C++ diffs.
- **Source-map ordering**:
  - In `AddVar`, `PrependDeclSourceMap` is called **after** the template line and declaration are added to the local `SourceLines`; since it prepends, the final order is `#line`, `template`, declaration. Keep this call after emission.
  - In `AddFunc` and `AddConversionOperator`, `PrependDeclSourceMap` is called **before** the template line. Keep that order.
  - The helper itself must be a pure string factory; it must not modify any `SourceLines` collection or call `PrependDeclSourceMap`.
- **No effect on non-template output**: The helper returns an empty string for non-template symbols, leaving existing non-template formatting unchanged.
- **`typename` slot in `VarFormat`**: `AddVar` intentionally leaves `VarFormat[5]` empty because dependent nested types already emit `typename ` inside `TypespecNested.GenType()`. Do not start inserting `needsTypename` into that slot as part of this refactor.

## Step-by-Step Implementation Plan

1. **Add format constants** in `backend/Generator/StmtFormatting.cs`:
   - `internal const string TemplateTypenamePrefix = "typename ";`
   - `public static readonly string[] TemplatePrefixFormat` with one slot `"{0}template <{1}>"`.
2. **Add the helper** near the top of `HierarchicalGen` (after the `DeIndentImpl` property or in a private helper region).
3. **Refactor `AddVar`** to build `tplDeclLine` / `tplImplLine` through `BuildTemplatePrefix`.
4. **Refactor `AddFunc`** to build `tplDecl` / `tplImpl` through `BuildTemplatePrefix`.
5. **Refactor `AddConversionOperator`** to use the same helper.
6. **Opportunistically refactor `AddHierarchical` and `AddForwardDecl`** to use the helper.
7. **Build**: `dotnet build myll.sln`.
8. **Test**: `dotnet test testing/` and inspect any diffs.
9. **Golden diff check**: For template test cases, compare generated `.hpp` / `.cpp` against previous output to confirm byte-identical template prefixes.
10. **Add a variable-template regression case** if none exists, or manually verify with a small `.myll` file.

## Risks and Mitigations

| Risk | Mitigation |
|------|-----------|
| Source-map directive moves relative to the `template` line because of changed insertion order. | Keep `PrependDeclSourceMap` calls in the exact same order as today; the helper only produces strings. |
| Whitespace changes in generated C++ (e.g., `template < typename T>`). | Use constants + format array; review diff before committing. |
| `TplParams` null reference. | The helper accepts `IReadOnlyList<TplParam>`; callers pass `obj.TplParams`, which is initialized to a non-null list in all current `Decl` subclasses. Add a defensive `?? new List<TplParam>()` if desired. |
| Build breaks because `TemplatePrefixFormat` or `TemplateTypenamePrefix` naming conflicts with existing symbols. | Search the solution for `TemplatePrefix` / `TemplateTypename` before naming; choose unique names. |
| Compile failure due to `IReadOnlyList<TplParam>` not matching `List<TplParam>`. | `List<TplParam>` implements `IReadOnlyList<TplParam>`. If older C# language version is used, verify or use `IList<TplParam>` / `List<TplParam>` instead. The project targets C# 9.0, which supports `IReadOnlyList<T>`. |
| Helper is later called for a template parameter kind that is not `typename` (e.g., a value/template-template parameter). | Currently only type parameters exist. If new kinds appear, extend the helper to accept a parameter-generator delegate or switch; document that limitation in XML comments. |

## Testing Steps

1. `dotnet build myll.sln`
2. `dotnet test testing/`
3. If variable templates are not covered by an existing case, create `testing/cases/var_template/` with a module containing:
   ```myll
   module var_template;
   namespace vt {
       var T value<T> = T(0);
       func run() -> int { return value<int> == 0 ? 0 : 1; }
   }
   ```
   Compile and run it via the CLI to confirm both the `.hpp` and `.cpp` emit exactly one `template <typename T>` line each.
4. Run the template-specific cases:
   ```bash
   dotnet test testing/ --filter "DisplayName~template"
   ```
5. Inspect generated output for `testing/cases/template_basics/`, `testing/cases/class_templates/`, and any other template golden files. The template prefix lines must match the previous output character-for-character.

## Definition of Done

- `AddVar`, `AddFunc`, and `AddConversionOperator` no longer inline-build `template <...>` strings.
- `BuildTemplatePrefix` is the single place in `HierarchicalGen` that produces the prefix.
- All template-related format fragments live in `StmtFormatting.cs`.
- `dotnet build myll.sln` succeeds.
- `dotnet test testing/` passes with no unexpected golden diffs.
