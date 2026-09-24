# Contained global `using NS`

Status: **implemented**.

The implementation ended up different from the original plan: instead of emitting `using namespace NS;` anywhere, Myll now emits **fully qualified names** everywhere a contained `using` would have supplied an unqualified name. This is simpler and avoids leaking names in headers.

## Scope rules

| Scope | Behavior |
|---|---|
| **Module / global scope** | Controlled by `Dialect.GlobalUsingNS`. `Leaky` emits `using namespace NS;` in the header; `Contained` injects it into every affected function body; `Disabled` rejects it. |
| **Class / struct / union scope** | Always contained, because C++ does not allow `using namespace` inside a class definition. The directive is injected into each member function/constructor/destructor body. |
| **Namespace scope** | Unchanged. `using namespace NS;` is emitted inside the namespace in the header. |

## Implementation notes

- The resolver records contained namespace usings per scope in `ModuleContext.ContainedNamespaceUsingsByScope`.
- `backend/Transform/ContainedNamespaceUsingInjector.cs` walks all function-like bodies after resolution and stores the effective list of namespace names on `IBodyOwner.InjectedNamespaceUsings`.
- The generator (`HierachicalGen`) prepends `using namespace NS;` statements at the start of each body and suppresses header emission for contained module/class usings.
- `TypespecNested.GenType()` was tightened so that unqualified templated names resolved through a namespace `using` are emitted fully qualified, including explicit template arguments (e.g. `std::vector<int>`).
- The CLI gained `--global-using-mode <leaky|contained|disabled>` to select the dialect per invocation.
- Regression test: `testing/cases/contained_using_ns/`.

## Long-term considerations

- Once the implementation has settled, the default `Dialect.GlobalUsingNS` should switch from `Leaky` to `Contained`.
