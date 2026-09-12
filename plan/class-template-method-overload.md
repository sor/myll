# Class-template method overload ranking

Substitute the owning class/struct template parameters when ranking methods and
constructors of class templates, so overloads such as
`Box<T>::assign(const T&)` vs `Box<T>::assign(T&&)` resolve correctly.

## Context

`TemplateInference.GetTemplateParamCount` and `Resolver.TryResolveOverload`
currently use only the candidate function's own template parameters. For a method
inside a class template, the enclosing class parameters are not available, so the
method's parameter types keep the opaque template parameter `T`. Both overloads
collapse to the same `ConversionRank.TemplateParameter` rank and the resolver
reports ambiguity.

## Approach

1. Refactor `TemplateInference` to expose combined "owner + own" template
   parameters and substitution helpers.
2. In `Resolver.TryResolveOverload`, extract owner template arguments from the
   receiver for member-access and scoped calls, substitute them into each
   parameter type, and rank the concretized overloads.
3. For methods with their own template parameters, pre-substitute the owner
   parameters before running deduction.
4. Extend the receiver extraction to inherited methods and alias receivers.
5. Add regression tests under `testing/cases/class_template_ref_category/`.

## Phases

1. **TemplateInference refactor** — no behavior change.
2. **Owner substitution for direct member access + scoped calls** — fix the
   common `Box<int>::assign(...)` case.
3. **Method-own template param deduction** — support methods that also introduce
   their own template parameters.
4. **Inherited class-template methods** — receiver is `Derived<int>`, method
   lives in a base class template.
5. **Alias receivers + final tests** — unwrap alias declarations and add
   regression coverage.

## Verification

After each phase:
- `dotnet build myll.sln`
- `dotnet test testing/`
- `bash frontend/apps/make_unix.sh release`
