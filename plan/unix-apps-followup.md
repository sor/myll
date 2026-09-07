# Unix apps follow-up todos

1. **Make `string` a real class reference**  
   Stop emitting `TypespecBasic.Kind.String`. Treat `string` as a `TypespecNested` that resolves to the dialect-configured backing class (e.g. `std::string`). This removes the special-case operator and member-access code for strings.

2. **Move `CompilationContext` out of `GlobalNamespace`**  
   Carrying the context on the AST node is a workaround. Pass `(GlobalNamespace module, CompilationContext context)` explicitly through resolver passes, or keep a `Dictionary<GlobalNamespace, CompilationContext>`. Keep AST nodes read-only so a parallel pipeline can share them safely.

3. **Support function-style constructor expressions** — DONE  
   `std::filesystem::path(input).filename()` now works. A call whose callee resolves to a class is treated as direct construction of a temporary of that class, so chained member access works.

4. **Resolve `operator*` / `operator->` on class types** — DONE  
   `(*it).path()` and `b->value()` now use the return types of the class's `operator*` and `operator->` overloads for member lookup. Only one level of `operator->` is supported; recursion is intentionally left for later.

5. **Fix primitive reference binding and increment** — DONE  
   `parseOptions(..., int & argi)` now accepts the `int` argument, and `++argi` works inside the function. Fixed `ConversionRules.CloneTypespecBase` to preserve `isDefaultSized` and pointer structure so reference/value conversions rank correctly; updated unary `++`/`--` validation to look through scalar references.

6. **Resolve member templates like `Value::as<T>()`** — DONE / not reached  
   The `cxxopts` sample compiles and runs now; the member-template problem expected here did not surface. Keep an eye on explicit-template member calls, but they appear to resolve correctly through the existing path.

7. **Prefer exact matches in overload resolution** — DONE  
   `std::to_string(u64)` no longer reports ambiguous. Unresolved identifiers that are used as call callees and resolve only to callable overloads are now left to call-level overload resolution, which ranks conversions and picks the exact match.

8. **Flesh out the `cxxopts` prototype**  
   The current `cxxopts.decl.myll` covers the common pattern. Extend it as needed for positional-argument helpers and the `count()` method.

9. **Build all unix utilities as a CI gate**  
   Once the resolver issues above are fixed, add `bash frontend/apps/make_unix.sh` to the test/CI steps so the sample apps do not regress again.

## Current status
The unix utilities now build end-to-end with the resolver enabled. The remaining work is item 9 (build them as a CI gate) plus the open design concern about default-sized vs fixed-width overloads.

## Open design concern: default-sized vs fixed-width overloads
Myll treats `int` (default-sized) and `i32` (explicitly 32-bit) as distinct types, with a family conversion between them. This lets overload resolution prefer `func f(int)` for an `int` argument and `func f(i32)` for an `i32` argument. In generated C++, however, `int` is typically emitted as `int` while `i32` is emitted as `std::int32_t`, which on most platforms are the same C++ type. Declaring both overloads in the same C++ scope can therefore produce a redefinition error. Possible directions: conditionally outlaw default-sized integer types as members of `class`/`struct` declarations, or conditionally outlaw default-sized parameter types in overload sets, or keep them distinct on the Myll side and leave the collision to the C++ compiler. This needs a dedicated design decision before it becomes a user-facing problem.
