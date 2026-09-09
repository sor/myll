# Unix apps follow-up todos — DONE

This plan is complete.

1. **Make `string` a real class reference**  
   Stop emitting `TypespecBasic.Kind.String`. Treat `string` as a `TypespecNested` that resolves to the dialect-configured backing class (e.g. `std::string`). This removes the special-case operator and member-access code for strings.

2. **Move `ModuleContext` out of `GlobalNamespace`** — DONE  
   `GlobalNamespace.Context` was removed. `ModuleContext` now owns the module AST via `ModuleContext.Module`. `CompiledModuleResult` wraps the context, and `NameResolver` keeps a dictionary from `GlobalNamespace` to `ModuleContext` for resolver-internal lookups.

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

8. **Flesh out the `cxxopts` prototype** — DONE / not needed  
   The existing `cxxopts.decl.myll` covers the patterns used by the sample apps, so no further work is required for now.

9. **Build all unix utilities as a CI gate** — DONE  
   The default `dotnet test testing/` harness includes `UnixAppsTests.GenerateUnixApps`, which generates C++ for all plain and cxxopts variants. The full C++ build (`bash frontend/apps/make_unix.sh`, ~30s clean) now runs in the container build as well.

## Current status
All items are done. The unix utilities build end-to-end with the resolver enabled, their generation is exercised by the default test harness, and the full C++ build is part of the container image build.

The open design concern about default-sized vs fixed-width overloads has been moved to `plan/default-sized-overloads.md`.
