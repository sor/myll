# Lone-candidate overload shortcut investigation

This file tracks the real reasons Myll previously needed the lone-candidate
shortcut in `backend/Resolver/Resolver.cs` (flag `UseLoneCandidateShortcut`).
The shortcut is still present and togglable; set `UseLoneCandidateShortcut` to
`true` to restore the original behaviour if a larger testset surfaces new
failures.

## Status

All known failure modes have been fixed; `UseLoneCandidateShortcut` is
currently set to `false`. The full test suite passes (128/128) and the Unix
apps build end-to-end with the shortcut disabled.

## Reproduction harness

All reproducers live under `tmp/lone_repros/`.

```bash
# Run with the shortcut disabled
MYLL_DEBUG_OVERLOAD=1 \
dotnet run --project frontend -- \
  -i tmp/lone_repros/repro_<name>.myll \
  -o tmp/lone_repros/out -CR
```

## Bucket A — premature diagnostics before type identity converges — FIXED

### Symptom

A single-candidate call was reported as "No matching overload" in an early
resolver pass, even though a later pass ranked the conversion successfully.

### Reproducer

`tmp/lone_repros/repro_local_stream.myll`

### Debug output

First pass:

```text
[Myll overload] ranking copyStream(std::istream &) -> REJECTED [0] istream->std::istream &=None
```

Later pass:

```text
[Myll overload] ranking copyStream(std::istream &) -> [0] istream->std::istream &=Conversion
```

### Root cause

The argument type (`std::cin`'s type) was initially represented as the bare
`istream` nested name, while the parameter was `std::istream`. Both should
resolve to the same declaration, but `TypespecNested` equality fell back to a
textual path comparison when one side had not been resolved yet, returning
`ConversionRank.None`. The resolver then recorded a hard "no match" diagnostic.
When the fixed-point loop finally resolved the type, the candidate succeeded,
but the diagnostic had already been emitted.

### Fix

`TryResolveOverload` now treats a `None` conversion rank as "not yet decidable"
when the source or target type still contains an unresolved `TypespecNested`
(or a class/struct with an unresolved base class). It returns
`noMatch = false` instead, allowing the fixed-point loop to retry. The
"no viable candidate" diagnostic is only emitted when the conversion is
definitively rejected after types have converged.

## Bucket B — class-template method parameters not substituted — FIXED AS SIDE EFFECT

### Symptom

Calling a method of an imported class template such as
`Myll::DynArray<int>::pushBack(10)` initially failed.

### Reproducer

Compile `myll/dyn_array.myll` + `testing/cases/dyn_array/main.myll` with the
shortcut disabled.

### Debug output

```text
[Myll overload] ranking pushBack(const T &) -> REJECTED [0] UntypedInteger->const T &=None
```

A local generic class (`tmp/lone_repros/repro_template_method.myll`) succeeded:

```text
[Myll overload] ranking put(const T &) -> [0] UntypedInteger->const T &=TemplateParameter
```

### Root cause

In early resolver passes the bare `T` in `pushBack(const T&)` was not yet
recognized as a template parameter, so the conversion ranked as `None`. Once
Bucket A's deferral gave the fixed-point loop more time, `T` was resolved to
its `TplParamDecl` and `ConversionRules.GetRank` returned `TemplateParameter`,
which is accepted as a viable conversion.

### Fix

No separate fix was needed; the Bucket A deferral allowed the resolver to
retry the call after the class template parameter became known.

## Bucket C — `auto` range-for variable has no concrete type — FIXED

### Symptom

After `for( auto i : v )`, calls such as `takesInt(i)` were rejected.

### Reproducer

`tmp/lone_repros/repro_auto_arg.myll`

### Debug output

```text
[Myll overload] ranking takesInt(Integer) -> REJECTED [0] ExplicitAuto->Integer=None
```

### Root cause

`RangeForAutoTypeTransformer` already inferred the element type for range-based
`for` loops, but it ran as a post-resolution transformer, **after** overload
resolution had already emitted diagnostics. The loop variable therefore stayed
as `TypespecBasic.ExplicitAuto` during call resolution.

### Fix

`RangeForAutoTypeTransformer` is now also invoked inside the fixed-point
resolver loop, immediately after the mid-loop `result.Apply()`, so the loop
variable gets a concrete type before `ResolveCalls` processes the body. The
post-resolution invocation is kept as a no-op safety net.

Regression test added: `testing/cases/range_for_auto/`.

## Additional complex scenarios checked after the fix

These reproducers live under `tmp/lone_repros/repro_complex_*.myll`.

| Scenario | Result |
|---|---|
| `std::vector<int>` method calls through a `T*!` smart pointer (`c->put(...)`) | passes |
| range-based `for( auto & i : v )` over `std::vector<int>` | passes |
| constructor overloads with direct construction `T name(args)` | passes |
| explicit class-template instantiation `Pair<int,string>(...)` | passes |
| external function template with explicit args `std::sort<int>(...)` | passes |
| smart-pointer derived-to-base conversion `unique_ptr<Derived>` -> `unique_ptr<Base>` | passes |
| `auto` variable initialized from `v[0]` with member access | passes |
| pointer/smart-pointer equality with `null` (`p != null`, `null == p`) | passes (fixed by allowing `ExplicitAuto`/`null` in `ValidateComparisonOperator`) |
| non-template overloaded functions/methods `f(int&)`, `f(const int&)`, `f(int&&)` | passes |
| overloaded class-template methods `assign(const T&)` / `assign(T&&)` inside `Box<T>` | **ambiguous for lvalue and rvalue arguments** (class template params are not substituted before overload ranking, so both overloads collapse to the same `TemplateParameter` rank) |
| unqualified base method call inside a derived method | **fails** (known limitation, see AGENTS.md) |
| range-based `for( auto i : [1,2,3] )` over an initializer list | passes (fixed by handling unresolved `std::initializer_list<T>` in `RangeForAutoTypeTransformer`) |

One remaining reference-category gap:

- **Class-template method overloads** where the references are around the class's own
  template parameter (`const T&` / `T&&` in `Box<T>`) remain ambiguous because the
  enclosing `T` is not replaced with the deduced concrete type during overload ranking.
  Fixing this requires plumbing the owning class template parameters through
  `TemplateInference.TryDeduceTemplateArgs` / `SubstituteTemplateParams`.

Another failure shown by the repros is the already-known limitation that
unqualified base method calls from inside a derived method are not supported
(`baseValue(v)` must be written as `Base<T>::baseValue(v)` or similar).

Regression tests added: `testing/cases/range_for_auto/` and
`testing/cases/range_for_initlist_auto/`.
