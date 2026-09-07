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

6. **Resolve member templates like `Value::as<T>()`**  
   `result["x"].as<bool>()` returns unresolved `T` even though the explicit template argument is `bool`. Wire member-template instantiation the same way free function templates are handled.

7. **Prefer exact matches in overload resolution**  
   `std::to_string(u64)` is reported ambiguous despite the exact `u64` overload existing. The resolver should prefer exact parameter types before promotion/conversion candidates for external functions too.

8. **Flesh out the `cxxopts` prototype**  
   The current `cxxopts.decl.myll` covers the common pattern. Extend it as needed for positional-argument helpers and the `count()` method.

9. **Build all unix utilities as a CI gate**  
   Once the resolver issues above are fixed, add `bash frontend/apps/make_unix.sh` to the test/CI steps so the sample apps do not regress again.

## Current status
After items 3–5, the unix utilities' remaining blocker is item 7 (`std::to_string(u64)` ambiguous). Item 6 (`Value::as<T>()`) will likely surface once item 7 is resolved.
