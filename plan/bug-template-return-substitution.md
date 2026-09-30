# Bug: template call return type is never substituted

Status: **open**. Found in the 2026-09-29 review. Reproduced.

## Symptom

A call to a templated function keeps the raw template parameter in the call's type. Myll never applies
the resolved template arguments to the callee's return type. Whenever the call type must be spelled out
(for example `var auto`), the generated C++ contains a raw `T` and fails to compile.

## Repro

`tmp/review/tpl_ret.myll`:

```myll
func max<T>(T a, T b) -> T
{
	return a > b ? a : b;
}

func main() -> int
{
	var auto m = max( 1, 2 );
	return 0;
}
```

Generated C++ contains `T m = max<int>( 1, 2 );`. clang++ rejects it: `unknown type name 'T'`.

Pointer return and explicit arguments behave the same (`tmp/review/const_deduce4.myll`):

```myll
func f<T>(T* p) -> T* { ret = p; }

var int * r1 = f<int>( c );   // fine: int* r1 = f<int>( c );
var auto r2  = f( c );        // broken: T* r2  = f<int>( c );
```

Deduction of the argument works (`f<int>` is emitted). Only the return type stays unsubstituted.

Existing template tests pass because they always give the call result an explicit target type
(`var int a = max( 1, 2 );` in `testing/cases/template_basics/deduction.myll`). The user's type then
hides the broken call type.

## Root cause

After deduction or explicit argument application, the callee's `retType` is not substituted with the
resolved arguments. The call expression's type stays the return typespec from the original definition,
still containing the template parameter reference.

Related symptom (const): `f(c)` with `c : const int*` fails with "No matching overload" instead of
deducing `T = const int` (`tmp/review/const_deduce.myll`). `TemplateInference.CloneTypespecBase`
(`backend/Resolver/TemplateInference.cs:455`) drops `qual` when stripping the `T*` wrapper, so the
binding check compares unqualified `int` against `const int`. This is a clone-drift symptom; the fix
belongs with the shared cloner (`enhancement-typespec-cloner.md`).

## Fix plan

1. In the call resolution path (`ResolveSingleCall` / `ApplyTemplateArgs`,
   `backend/Resolver/TemplateInference.cs:192-207`), substitute the callee's return type with the
   resolved template arguments and store the result as the call expression's type.
2. Cover both the explicit-argument path and the deduction path.
3. Use the shared Typespec cloner for the substitution so `qual`, `srcPos`, and nested flags survive.

## Verification

- `tmp/review/tpl_ret.myll` and `tmp/review/const_deduce4.myll` generate C++ with no raw `T` in any
  type position; clang++ `-std=c++23 -Wall -Wextra` is clean.
- `var auto m = max( 1, 2 );` infers `int`.
- The const repro deduces `T = const int` and emits `f<const int>( c );`.
- Add a regression case (extend `testing/cases/template_basics/` or add `testing/cases/template_return/`)
  and update goldens.
- `dotnet test testing/` green; `bash frontend/coreutil/make_coreutil.sh` clean.

## Risk

Medium. Call types feed overload ranking, and the fixed-point loop in `Resolver.cs` re-ranks calls.
The substituted type must be stable across passes.
