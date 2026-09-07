# Unix apps follow-up todos

1. **Make `string` a real class reference**  
   Stop emitting `TypespecBasic.Kind.String`. Treat `string` as a `TypespecNested` that resolves to the dialect-configured backing class (e.g. `std::string`). This removes the special-case operator and member-access code for strings.

2. **Move `CompilationContext` out of `GlobalNamespace`**  
   Carrying the context on the AST node is a workaround. Pass `(GlobalNamespace module, CompilationContext context)` explicitly through resolver passes, or keep a `Dictionary<GlobalNamespace, CompilationContext>`. Keep AST nodes read-only so a parallel pipeline can share them safely.

3. **Support function-style constructor expressions**  
   `std::filesystem::path(input).filename()` currently fails because `path(input)` in an expression is treated as a cast, not direct construction. Allow `Class(args)` as a temporary expression so `basename`, `dirname`, `ls`, etc. compile without source rewrites.

4. **Resolve `operator*` / `operator->` on class types**  
   `(*it).path()` fails because `TypeResolver` only dereferences pointers, not class instances with `operator*()`. Make unary `*` and `->` on classes use the operator-overload return type.

5. **Fix primitive reference binding and increment**  
   `parseOptions(..., int & argi)` rejects the `int` argument, and `++argi` fails inside the function. Reference parameters and pre-increment should work for plain integers.

6. **Resolve member templates like `Value::as<T>()`**  
   `result["x"].as<bool>()` returns unresolved `T` even though the explicit template argument is `bool`. Wire member-template instantiation the same way free function templates are handled.

7. **Prefer exact matches in overload resolution**  
   `std::to_string(u64)` is reported ambiguous despite the exact `u64` overload existing. The resolver should prefer exact parameter types before promotion/conversion candidates for external functions too.

8. **Flesh out the `cxxopts` prototype**  
   The current `cxxopts.decl.myll` covers the common pattern. Extend it as needed for positional-argument helpers and the `count()` method.

9. **Build all unix utilities as a CI gate**  
   Once the resolver issues above are fixed, add `bash frontend/apps/make_unix.sh` to the test/CI steps so the sample apps do not regress again.
