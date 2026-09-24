# Plan: alias templates

This plan describes how to add alias templates to Myll. The grammar already accepts `tplParams` on `defAlias`, but the AST currently parses and discards them.

## Goal

Allow declarations such as:

```myll
alias Vec<T> = std::vector<T>;
alias Pair<T, U> = std::pair<T, U>;
alias FixedArray<T, int N> = std::array<T, N>;
```

These should lower to C++ alias templates:

```cpp
template <typename T>
using Vec = std::vector<T>;

template <typename T, typename U>
using Pair = std::pair<T, U>;

template <typename T, int N>
using FixedArray = std::array<T, N>;
```

## Out of scope

- Alias templates inside function bodies. C++ does not support block-scope alias templates, so Myll should reject them with a diagnostic, just like local variable templates.
- Alias templates to namespaces. Existing namespace aliases (`namespace Foo = Bar;`) stay unchanged and remain non-template.
- Template-template parameters. Those are handled separately if they are added later.

## 1. Data model

Add a `TplParams` list to `AliasDecl` and `AliasStmt` in `backend/Core/Decl.cs` and `backend/Core/Stmt.cs`. Make `AliasDecl` implement `ITemplatedDecl` so that the existing template-parameter plumbing can be reused.

```csharp
public class AliasDecl : Decl, ITemplatedDecl
{
    public Typespec       type     = null!;
    public List<TplParam> TplParams { get; init; } = new();
    public Scope?         TemplateParamScope => scope;

    public bool IsNamespaceAlias { get; set; }
    public override void AddToGen( HierarchicalGen gen ) { gen.AddAlias( this ); }
}
```

`AliasStmt` only needs the list for diagnostic purposes, because statements do not participate in resolution the same way declarations do.

## 2. Visitor changes

`backend/Visitor/VDecl.cs` (`VisitDefAlias`) and `backend/Visitor/VStmt.cs` (`VisitDefAlias`) currently parse `tplParams` into a local `useMe` variable and then drop it. Store the list on the alias node instead.

For `VStmt.VisitDefAlias`, reject alias templates at function scope:

```csharp
if( localTplParams.Count > 0 ) {
    Context.Diagnostics.Add( new Diagnostic(
        srcPos,
        DiagnosticKind.Error,
        "Local alias templates are not supported" ) );
}
```

`VDecl.VisitDefAlias` should allow module, namespace, class, and struct scope alias templates.

## 3. Scope and parameter injection

`TemplateParamTransformer` should inject hidden `TplParamDecl` placeholders for `AliasDecl.TplParams`, just as it does for `Func`, `Structural`, and `VarDecl`. This lets the aliased type reference the template parameters by name.

Because `AliasDecl` implements `ITemplatedDecl`, the existing `TransformDecl` recursion handles it automatically once it is added to the transformer logic.

## 4. Resolution

A use like `Vec<int>` is a `TypespecNested` whose final segment resolves to an `AliasDecl`. When the alias has template parameters, the resolver must:

1. Validate that the provided template arguments match in arity and kind, using `TemplateInference.ValidateTemplateArgs`.
2. Substitute the arguments into `alias.type` with `TemplateInference.SubstituteTemplateParams`.
3. Return the substituted type as the resolved type of the `TypespecNested`.

If no template arguments are supplied in a dependent context, leave the alias unresolved for C++ to instantiate later.

The existing `UnwrapAlias` helper in `ConversionRules` must continue to work after substitution. Because the substituted type is stored directly on the use site, `UnwrapAlias` can recurse through it as it does today.

## 5. Generator changes

`backend/Generator/HierachicalGen.AddAlias` currently emits:

```csharp
string ret = Format(
    AliasFormat[obj.IsNamespaceAlias ? 1 : 0],
    indent,
    obj.name,
    targetName );
```

For alias templates, emit a `template<...>` header before the `using`:

```csharp
if( obj.TplParams.Count > 0 ) {
    target.Add( Format(
        "{0}template <{1}>",
        indent,
        GenTemplateParameterList( obj.TplParams ) ) );
}

target.Add( Format(
    AliasFormat[0],
    indent,
    obj.name,
    targetName ) );
```

Non-template aliases keep the current behavior. Namespace aliases keep the namespace-alias format and reject template parameters in the visitor.

## 6. Interaction with the rest of the compiler

- **Member access on aliases.** If `Vec<T>` aliases `std::vector<T>`, calling `Vec<int>::push_back` must resolve to `std::vector<int>::push_back`. This should fall out naturally if the alias is unwrapped before member lookup.
- **Dependent contexts.** An alias template used inside another template (e.g. `Vec<T>` inside a class template with type parameter `T`) must stay dependent. The substituted type will contain a `TplParamDecl`, so `IsDependentType` will already return true.
- **Conversions.** `ConversionRules.UnwrapAlias` and the conversion rank code already follow `AliasDecl.type`. After substitution, the inner type is the real C++ type, so conversion ranking works without further changes.
- **External aliases.** A `.decl.myll` file can declare an alias template with `[extern] template<typename T> using ...`. Myll should parse the parameters and trust the binding, falling back to arity-only validation if the parameter kind is `Unknown`.

## 7. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Aliases to class templates with explicit template arguments, e.g. `alias IntVec = std::vector<int>;` do not need parameters and must keep working. | Keep the non-template path unchanged and only substitute parameters when `AliasDecl.TplParams.Count > 0`. |
| Cyclic alias templates such as `alias A<T> = B<T>; alias B<T> = A<T>;` could loop during unwrapping. | `UnwrapAlias` already tracks visited aliases with a `HashSet`; extend the same guard to alias-template substitution. |
| Alias templates inside classes/structs need to be emitted inside the class body. | `AddAlias` is already called while the class is being generated, so adding the template header there is enough. |
| Mixed type/non-type alias templates use the same validation as class and function templates. | Reuse `ValidateTemplateArgs` and `GenTemplateParameterList`. |

## 8. Testing

Add `testing/cases/alias_template/main.myll`:

```myll
module alias_template;
import std_vector;
import std_array;

alias Vec<T> = std::vector<T>;
alias Pair<T, U> = std::pair<T, U>;
alias FixedArray<T, int N> = std::array<T, N>;

func main() -> int {
    var Vec<int> v;
    v.push_back( 42 );
    if( v.size() != 1 || v[0] != 42 )
        return 1;

    var FixedArray<int, 4> arr;
    if( arr.size() != 4 )
        return 2;

    return 0;
}
```

Add `testing/cases/alias_template_fail/main.myll` for error cases:

- Passing a value where a type is expected: `alias Bad<T> = std::vector<int>; var Bad<42> x;`.
- Passing a type where a value is expected: `alias Bad<int N> = std::array<int, N>; var Bad<float> x;`.
- Local alias template inside a function.

## 9. Migration steps

1. Add `TplParams` to `AliasDecl` and make it implement `ITemplatedDecl`.
2. Store parsed `tplParams` in `VDecl.VisitDefAlias` and `VStmt.VisitDefAlias`; reject alias templates in `VStmt`.
3. Update `TemplateParamTransformer` to inject placeholders for alias template parameters.
4. Update type resolution to substitute template arguments into alias templates.
5. Update `AddAlias` to emit `template<...>` headers.
6. Add regression cases.
