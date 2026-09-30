# Open TODOs

This file consolidates the remaining work items that came out of the visitor audit, the Unix-app porting work, and the `AGENTS.md` planned list.
The endboss architecture is described in `plan/semantic-analysis.md`.

## Semantic analysis / ScopeStack — DONE

The scope-stack based semantic-analysis pass is implemented. Name resolution, type checking, and overload resolution now run before code generation.

- `backend/Core/Scope.cs` holds the tree (`Scope`, `ScopeLeaf`, `children`, `importedScopes`).
- `backend/Visitor/VMain.cs` pushes/pops scopes during visiting and records unresolved identifiers, types, and member accesses.
- `backend/Resolver/Resolver.cs` runs a fixed-point resolver across imported modules and produces diagnostics for unresolved or ambiguous names.
- The C++ generator consumes `ResolutionResult` for `TypespecNested`, `ScopedExpr`, member access, and unqualified `IdExpr`.
- `backend/Core/Symbol.cs` is an unused future stub; `backend/Core/Attribute.cs` enums are also unused and marked obsolete.

What finishing this would unlock:
- Type checking and overload resolution (implemented for core cases).
- Clean semantic errors with source locations.
- Type-driven disambiguation in the generator (e.g. `enum` inheritance, operator synthesis, `convert`).

Completed endboss pieces:
- **Expression type model**: `Expr.Type`, `TypeResolver`, and `ConversionRules`.
- **Overload resolution**: ranked selection (exact + promotion), arity filtering, and diagnostics for ambiguous and no-matching calls. End-to-end test: `testing/cases/overload/`.
- **Core type checking**: assignments, variable/field initializers, return statements, and single-candidate argument compatibility. Negative tests: `testing/cases/typecheck_fail/`.
- **Declaration validation**: attribute combination and duplicate-name checks moved from `backend/Generator/HierachicalGen.cs` into `backend/Resolver/TypeChecker.cs`, producing source-location diagnostics. Negative test: `testing/cases/decl_fail/`.
- **Built-in operator checks**: hard-coded rule-based validation and result-type computation for arithmetic, bitwise, shift, comparison, and unary operators on scalar built-in types. Pointer arithmetic is rejected by default. Negative test: `testing/cases/operator_fail/`.
- **Spaceship operator**: the generator now emits C++ `<=>` instead of the old `cmp(...)` placeholder.
- **Broader conversion ranks**: safe integer widening, bool/integer conversion, and a configurable mixed-signedness dialect. Tests: `testing/cases/conversion/`, `testing/cases/conversion_fail/`.
- **Literal typing**: integer literals are untyped until bound to a target; valid targets are fitting integer types and floats. Float literals are untyped and only bind to floats.

See `docs/analysis/01-architecture.md` (static mutable state, `Decl`→`Stmt` inheritance) and `docs/analysis/07-future-stubs.md` for related context.

## Critical / next

1. **Softcoded dialect configuration** — DONE. Dialects are selected per module with a
   `dialect <name>;` statement and resolved from JSONC files in `dialect/`. `dialect/myll.jsonc`
   ships as the default and as a copy-paste base. Missing/unknown keys produce one warning per
   dialect. All files of a module must agree on the effective dialect. Files without a dialect
   statement default to `myll`. See `docs/design/softcoded-dialect.md`.
   - Future: inheritance (`"basedOn": "myll"`) and project-local `dialect/` directories.

1. **Overload resolution maturity** — fill the remaining gaps in a deliberate order, adding regression tests at each step:
   1. **Simple types**: exact match, promotions, safe conversions — regression locked in `testing/cases/overload_non_template/`.
   2. **Pointers and references**: `T*`/`T&` conversions, derived-to-base pointer/reference
      (including when only a base overload exists), `null` to pointer, and CV-qualified
      pointer/reference overload ranking — regression locked in
      `testing/cases/overload_non_template/`, `testing/cases/overload_non_template/`,
      `testing/cases/overload_non_template/`, `testing/cases/overload_non_template/`,
      `testing/cases/overload_non_template/`, and `testing/cases/overload_non_template/`.
      Also fixed `CloneTypespecBase` losing `isDefaultSized` on pointer-wrapped basic types.
   3. **Smart pointers**: `T*!`/`T*+`/`T*?` conversions and overload preference — regression locked in
      `testing/cases/overload_non_template/` and `testing/cases/overload_non_template/`.
      (Value parameters of unique pointer require `(move)`; this matches C++ ownership transfer.)
   4. **Templates**: deduction for `T`, `T*`, `T&` and overload ranking between template and non-template
      candidates — regression locked in `testing/cases/overload_template/` and
      `testing/cases/overload_template_ranking/`.
      Remaining: pointer wrappers, initializer lists, non-type template parameters, explicit template arguments.
    5. **Edge cases**: member-access overloads, constructor overloads, forwarding/reference collapsing,
        overload sets with mixed value/pointer/smart-pointer parameters.

6. **Reference-category ranking** — non-template overloads now distinguish `T&` / `const T&` /
   `T&&` binding according to the argument's lvalue/rvalue category. This removes ambiguity
   for `f(int&)` vs `f(const int&)` and `f(const int&)` vs `f(int&&)`. Implementation:
   - `ConversionRules.GetRank` accepts an optional `sourceIsLValue` flag and applies reference
     binding rules before falling back to inheritance conversion.
   - `Resolver.TryResolveOverload` classifies each argument expression with a lightweight
     `IsLValue` heuristic.
   Files: `backend/Resolver/ConversionRules.cs`, `backend/Resolver/Resolver.cs`.
   Test: `testing/cases/overload_non_template/`.
   Done: owner template parameters are now substituted before ranking for direct member access,
   scoped calls, inherited methods, and methods with their own template parameters.
   Regression test: `testing/cases/class_templates/`.
   Remaining: class-template method overload ranking for aliased class types is in place once
   Myll aliases support member access on the underlying class.

7. **Pointer/smart-pointer equality with `null`** — `p != null`, `null == p`, and the reverse
   now compile. This required a small special case in `ValidateComparisonOperator` because
   `null` is represented as `ExplicitAuto` and was not accepted by the scalar-comparison checks.
   Also fixes the diagnostic formatter to include pointer decorations so `int*` is shown as `int*`
   instead of `int`. Files: `backend/Resolver/TypeChecker.cs`. Test:
   `testing/cases/null_pointer_compare/`.

## Short-term consistency fixes (next few sessions)

1. **Cross-module declaration collisions** — DONE. `backend/Resolver/Resolver.cs`
   (`ValidateModuleDecls`, called after `ValidateGlobalUsings`) detects, per
   (namespace-FQN, name), when declarations that merge into one translation unit
   collide, mirroring what C++ would reject: redefined class/struct/union/enum/alias,
   duplicate non-link-safe variables, and same-signature function definitions (ODR
   violations), both across real modules and within one module. Functions that differ
   only in return type are rejected too, because C++ cannot overload on return type.
   Cross-kind name sharing (a type and a function/variable sharing a name) is rejected
   in strict Myll: Myll has no elaborated type specifiers; an `AllowCpp` dialect is
   planned (`plan/cross-kind-name-sharing.md`). Decl+def splits and different-signature
   overloads stay legal. The decl+def split is a supported pattern, not
   an accident: Myll needs forward declarations *only* for external libraries (the
   `.decl.myll` bindings in `std/`/`lib/`), and a signature-only module plus a
   definition module models shipping a library's signatures separately from its
   implementation; using both in one translation must compile and act as one function.
   Prototype (`.decl.myll`) modules are skipped because
   they are replaced by their raw C++ includes. A matching change in `TryResolveOverload`
   merges same-signature candidates (declaration + definition) so they are not read as
   ambiguous. Diagnostics anchor the error on the later declaration and a note on the
   earlier one, naming the other module. Tests: `testing/cases/cross_module_clash_fail/`
   and `testing/cases/same_module_redefinition_fail/` (negative, registered in
   `generateFailing`), `testing/cases/cross_module_overload/`,
   `testing/cases/cross_module_decldef/` and `testing/cases/same_module_decldef/`
   (positive, with goldens). The pre-existing
   `ResolverTests.AmbiguousImport_ProducesDiagnostic` was retargeted to the new
   redefinition diagnostic.


2. **Contained global namespace `using` mode** — DONE. Module-scope `using NS;` now emits
    fully qualified names in both `.hpp` and `.cpp`; no `using namespace NS;` directive is emitted.
    Class/struct-scope namespace usings are always contained because C++ does not allow
    `using namespace` inside a class definition.
    - *Known output change*: `TypespecNested.GenType()` now also fully qualifies unqualified
      templated class names when they resolve to a declaration, even if no `using` is involved.
      This is consistent with `IdExpr` behavior and only required updating the golden files for
      `template_basics`, `class_templates`, `alias_template`, and `synthetic_sugar`.

3. **Self-contained generated headers** — a generated `.hpp` must not rely on the order in which
   includes appear or on includes pulled in by other headers. Audit the generator for any
   remaining cases where an include is omitted because another header happens to provide it.

4. **Untyped integer compound constants** — DONE. Untyped integer constants carry their exact
   value in `TypespecBasic.literalValue` (`BigInteger`): literals are parsed at type resolution,
   and compound expressions are constant-folded by `FoldUntypedIntegerResult` in
   `backend/Resolver/TypeResolver.cs` for `+ - * % & | ^ << >>` (a fresh typespec is returned;
   operand types are never mutated). `ConversionRules.GetUntypedLiteralRank`/`IntegerFits` now
   check the value against the target's exact min/max, so `var u64 x = 1024 * 1024 * 1024;` and
   `var u64 x = 18446744073709551615;` work and non-fitting values are rejected (also for plain
   literals above `long.MaxValue`, which the old `long`-based parse could not see). The int target
   no longer short-circuits to Exact without a fit check. Tests:
   `testing/cases/new_in_myll/compound_constants.myll` (positive) and
   `testing/cases/typecheck_fail/main.myll` (`test_compound_constant_overflow`).
   Remaining follow-ups done 2026-09-30 (`plan/untyped-constant-followups.md`): `/` folds
   (division by zero left to C++); unary minus folds to the negated constant; ternaries of
   untyped constants carry all possible values (`TypespecBasic.literalValues`) so a target
   must hold every branch; and folded constants whose plain C++ emission would be undefined
   (shift count out of range, operand-type overflow) are emitted as the exact folded
   constant by `BinOp.Gen()`.

5. **`char` compared with integer literals** — DONE. `char` is not a number in Myll:
   `ValidateComparisonOperator` (`backend/Resolver/TypeChecker.cs`) rejects a plain `char`
   compared with any other built-in scalar type; use character literals such as
   `type == '\0'`. Pointer comparisons (`char*` vs `char*`) are unaffected. This also closed a
   validation gap: `if`/`while`/`for` conditions now run the built-in operator checks
   (`CheckBooleanOperand` calls `ValidateExpression`), so comparisons in conditions are validated
   like comparisons in expression position. Tests: `testing/cases/typecheck_fail/main.myll`
   (`test_char_vs_int_literal_eq`, `test_char_vs_int_literal_order`, `test_char_vs_typed_int`).

6. **Standard-library declaration attributes** — finish populating `std/*.decl.myll` and `c_*.decl.myll`
   with `[pure]`, `[ct]`, `[nodiscard]`, and `[noexcept]` to match the installed C++23 standard-library
   headers. Use `/usr/include/c++/15/` (or the highest version under `/usr/include/c++/`) as the
   reference for `constexpr`, `const`, `noexcept`, and `[[nodiscard]]`. Keep the bindings updated when
   adding new standard-library wrappers.

8. **Target-type-driven template inference** — let the RHS of an assignment or initializer know the
   type of the LHS so that missing template arguments can be inferred. Primary example:
   `var f32 f = std::numbers::pi_v;` should expand to `std::numbers::pi_v<f32>`.
   This requires propagating an expected type through expression resolution or running an inference
   pass during `TypeChecker.CheckAssignment`. Dependency: resolver currently resolves names before
   target types are consumed, so the design must decide between front-loading expected types into
   the resolver or re-resolving template arguments in the type checker.

## Blocked / needs dependency

(none at the moment)

## Recently completed

1. **Class-hierarchy resolution for overload ranking** — resolved declarations are now committed to
   the AST (`result.Apply()`) before overload resolution, and `ConversionRules.IsDerivedFrom` falls
   back to the `ResolutionResult` map for base-class `TypespecNested`s. This makes `Derived*` ->
   `Base*` and `Derived&` -> `Base&` conversions visible to Myll. Files:
   `backend/Resolver/Resolver.cs`, `backend/Resolver/ConversionRules.cs`,
   `backend/Resolver/TypeChecker.cs`. Tests: `testing/cases/overload_non_template/`,
   adjusted `testing/cases/overload_non_template/`.

2. **CV-qualified overload ranking** — exact matching now includes type and pointer-block
    qualifiers; qualifier conversions are allowed for value types (top-level qualifiers can
    be added/removed) and for adding pointee qualifiers on pointers/references
    (e.g., `int* -> const int*`). Reference-to-value binding now also permits qualifier
    changes (e.g., `const std::filesystem::path& -> std::filesystem::path`).
    Files: `backend/Resolver/ConversionRules.cs`.
    Tests: `testing/cases/overload_non_template/`, `testing/cases/overload_non_template/`.

3. **Unix app cleanup** — removed the hand-parsed non-cxxopts variants of `basename`, `cat`,
    `dirname`, `echo`, `grep`, `head`, `ls`, `wc`, and `yes`; renamed the cxxopts variants to the
    canonical module/file names (`cat.myll`, module `cat`, binary `cat`, etc.). The remaining
    utilities are the cxxopts versions plus the trivial builtins `true`/`false`/`pwd` and `find`
    (which already uses cxxopts). This eliminates the duplicate helper names that were being
    compiled into one Myll invocation.

4. **Lone-candidate overload shortcut removed** — the resolver no longer skips conversion checks
    for single-candidate calls. The previous failures were caused by:
    - premature "no match" diagnostics emitted before type identities converged, now deferred
      with `TypeMightResolveLater`;
    - `auto` loop variables whose type was inferred too late, fixed by running
      `RangeForAutoTypeTransformer` inside the fixed-point resolver loop;
    - `auto` loop variables over initializer-list literals, fixed by teaching
      `RangeForAutoTypeTransformer.InferRangeElementType` to read the element type from
      synthesized `std::initializer_list<T>` types that do not have `resolvedDecl` set yet.
    The flag `UseLoneCandidateShortcut` in `backend/Resolver/Resolver.cs` remains as an escape
    hatch. See `plan/lone-candidate-investigation.md` for full analysis and reproducers.

2. **Function-scope `using` resolution** — `using Namespace;` inside a function body now resolves names from that namespace for the rest of the block. Implemented by registering `UsingStmt`s as `UnresolvedUsingStmt`, resolving them alongside `UnresolvedUsing`, and generating the appropriate `using namespace` / `using Name;` C++ form. Added regression test `testing/cases/using_function_scope/` and moved the workaround in `frontend/coreutil/find.myll` into `main()`.

## High priority

1. **Per-declaration access modifiers** — done
   - Implemented `[pub]`, `[priv]`, `[prot]` on individual class/struct/union members; the attribute overrides the current section default from `[pub]:` / `[priv]:` / `[prot]:`.
   - Field order is preserved; access labels change inline.
   - End-to-end test: `testing/cases/access_mods/`.
   - Enforced: access attributes are only valid inside class/struct/union declarations.

2. **Repair the `try/catch` grammar** — done
   - Changed `catchClause` from `CATCH funcTypeDef? stmt` to `CATCH LPAREN param? RPAREN stmt`.
   - Updated `VStmt.VisitStmtTryCatch` to read the optional `param`.
   - Added regression coverage in `testing/cases/control_flow/try_catch.myll`.

3. **Implement reachable missing expression/statement features**
   - Named function arguments (`Arg.Gen` in `backend/Core/Mixed.cs`).
   - Null-coalescing function calls (`FuncCall.Gen` in `backend/Core/Mixed.cs`).
   - Copy-cast (`VisitPreExpr` in `backend/Visitor/VExpr.cs`).
   - Discard expression (`Discard.Gen` in `backend/Core/Expr.cs`) — DONE.

4. **Improve compiler diagnostics**
   - Replace empty `throw new NotImplementedException()` calls with messages that include the construct and source location.

5. **Direct constructor syntax and initializer-list literals** — done
   - Variables can be declared with `var T name(args);` (and `const`/`let`/`field` variants) instead of `var T name = T(args);`.
   - Grammar: `idAccessor` accepts an optional `funcCall` in addition to `= expr`; `expr` accepts `LBRACK args? RBRACK` as `InitListExpr`; `typespec` accepts `initlistType`; `kindOfPassing` accepts `INITLIST`.
   - AST: `VarDecl`/`VarStmt` carry an explicit `isDirectConstruct` flag and store the constructor call in `init` as a `FuncCallExpr`. A new `InitListExpr` node stores the brace-init-list elements. `TypespecNested.isInitList` identifies the `initlist`/`ilist` keyword type.
   - Generator emits `type name(args);` for direct construction and `{ e1, e2 }` for initializer lists. `initlist<T>`/`ilist<T>` emit `std::initializer_list<T>`.
   - Type inference: an empty `[ ]` with `auto`/bare `initlist`/`ilist` is rejected; otherwise the element common type is computed. `var auto a = [1,2,3];`, `var initlist<int> a = [1,2,3];`, and `var ilist a = [1,2,3];` all infer `std::initializer_list<int>`.
   - Auto-inferred `initlist` types are registered for name resolution, so member access such as `var auto a = [1,2,3]; a.size()` works.
   - Added special members `ctor initlist` and `operator initlist =`. They generate a `const std::initializer_list<E>&` parameter, default parameter name `init`, and are implicitly `[implicit]`. The element type `E` defaults to the enclosing class's first template parameter or can be written explicitly (`ctor initlist<E>`).
   - Added `std/std_initializer_list.decl.myll` and updated `myll/dyn_array.myll` to use `ctor initlist`.
    - Added `backend/Core/StdBuiltins.cs` containing `ImplicitStdImports`, a central list of standard modules (e.g. `c_math`, `c_stdint`, `std_memory`, `std_initializer_list`, etc.) that are imported implicitly into every compiled module. The resolver treats them as imported for name lookup, the generator emits the matching `#include <...>` lines, and a diagnostic is produced if any listed module is missing from the compilation.

   - Removed `StmtFormatting.DefaultIncludes`; C++ header emission is now driven entirely by imports.
   - Regression cases: `testing/cases/init_syntax/`, `testing/cases/init_syntax/`.

## Medium priority

4. **Language syntax gaps**
   - Range-based `for` loops (`for( a : b )`).
   - Namespace aliases via `alias` — DONE. `alias fs = std::filesystem;` at module or namespace scope resolves to the target namespace and generates `namespace fs = std::filesystem;`. Used in `frontend/coreutil/find.myll` and tested in `testing/cases/synthetic_sugar/alias.myll`.
   - Auto-property / combined private-field + public-getter syntax.

5. **Cleanup**
    - Remove `oldParser/` and `oldCodeGen/` once nothing references them.
    - Triage the TODO/FIXME/HACK markers across backend/frontend/testing.
    - Clean up the `= null!` initializers by introducing real constructors or nullable annotations.
    - Rename `backend/Core/Decl.cs` `Structor` to something less ambiguous (e.g. `ConDestructor` or split into `CtorDecl`/`DtorDecl`).

6. **Partial/broken generators**
   - Complete `VisitScopedExpr` (`backend/Visitor/VExpr.cs`).
   - Review/replace the lambda hack in `VisitLambdaExpr`.
   - Validate the switch/case generator paths (`caseBlock`, `defaultBlock`, `StmtSwitch`).

7. **Other generators / language features**
   - Accessor/property generation (currently parsed but skipped).
   - Subclass/inheritance generation (partially implemented).

## From SDL sample work

- **Resolve member access on array-of-struct elements.** Indexing into a fixed array of a user-defined struct (`body[idx].x`) now resolves the element's members. The resolver strips the outermost subscriptable pointer/array and looks up members in the resulting type. Regression test: `testing/cases/array_features/`.
- **SDL Snake now uses a single struct array.** Instead of parallel `int[...] sx` / `int[...] sy` arrays, the snake body is stored as `SDL_Rect[ MAX_LEN ] body` (and previously `Pos[ MAX_LEN ] body`). This exercises the new array-of-struct member resolution.
- **Support braced aggregate initialization.** Myll does not parse `var T a = { 1, 2, 3 };` or `var T a { 1, 2, 3 };`. Direct construction (`var T a( 1, 2, 3 );`) works for types with a suitable constructor/aggregate call.
- **Shared external-library declarations.** Created `lib/` at repo root, auto-discovered by `frontend/Program.cs` alongside `std/` and `myll/`. The SDL3 binding lives at `lib/sdl3.decl.myll` and is consumed by both `frontend/sdl-pong/` and `frontend/sdl-snake/`.

## Pointer-to-array (`[*]`) — superficial plan

- **Syntax**: `T[*]` is a pointer to an array of `T`, distinct from `T[]` (array of `T`) and `T*` (pointer to a single `T`).
  - `T*[]` remains "array of pointers to `T`" (e.g. `const char *[] argv`).
- **Parser**: `[*]` already tokenized as `PTR_TO_ARY` and mapped to `Pointer.Kind.PtrToAry`.
- **Type system**:
  - Allow implicit conversion from `T[N]` to `T[*]` (array lvalue decays to pointer-to-array).
  - Reject scalar / `T*` to `T[*]` conversions.
  - Subscript `p[i]` on `T[*]` returns `T`.
- **C++ emission**: `PtrToAry` currently emits `T*`; change to emit the parenthesized form `T (*name)[]` / `T (*name)[N]`.
- **Regression tests**: revive the old thesis cases (`frontend/tests/thesis/main.myll:263-264`):
  - `var int[*] ptr_to_ary1 = ary;` should work.
  - `var int[*] ptr_to_ary2 = &scl;` should fail with a clear diagnostic.
- **Docs**: update places that describe `[*]` as `std::vector<T>`; vector should be imported as a normal external type, not `[*]`.

## Lower priority / design decisions

8. **Imports**
   - Support subdirectory paths in import names or add a string-form import (`import "some/lib/header.hpp";`).
   - File-wide imports are the end goal; module-wide imports may be used as a first step.

9. **CI/CD**
   - Set up a CI workflow for automated builds and tests.
   - Generate ANTLR output during build; do not commit the generated files.

10. **Open language design questions**
    - Should `return _` mean "return default"?
    - Should `switch` support `else:` as an alias for `default:`?
    - Should `(copy)(expr)` be allowed on prvalues?
    - **Switch-case fallthrough dialect**: allow the user/program to select whether cases implicitly `break`, implicitly fall through, or require an explicit `break`/`fallthrough` annotation per case. The mode is modeled in `backend/Core/Dialect.cs` (`SwitchFallthroughMode`) but is not wired through the parser/generator yet.
    - **Linkage model**: work in progress on another machine. Decide which declarations get internal/external/module linkage and how it interacts with cross-module imports.
    - **Export control**: is `[hide]`/`[hidden]` the only module-export mechanism, or will there be explicit `export`?
    - **Class/struct defaults**: should classes get a default `[rule_of_n=...]` or other default attributes, and should structs get an attribute that requires/encourages POD-like behavior? This needs a decision before wiring into `Dialect` defaults.

## Recently completed

- Visitor audit (`plan/visitor-audit.md`) and fail-loudly overrides.
- `continue`, dotted imports, `try/catch` codegen, `\0` char literal fix.
- `var` parameter error message.
- ProcessRunner thread-safety fix.
- `ls` vertical slice with classes, inheritance, smart-pointer shorthand, sorting, and hidden files.
- Function/scope-level namespace aliases using an interim `KnownNamespaces` table, with a new integration test case.
- Generator changes to support aliases in namespaced modules: trailing return types for out-of-line function definitions and namespace wrapping for constructors/destructors; golden files regenerated.
- Per-base inheritance access specifiers (`[pub]`, `[priv]`, `[prot]`) and `[virtual]` bases; new `bases` integration test case.
- Value-initialization by default for `var`/`field`/`const`/`let` declarations without an initializer; `[noinit]`/`[uninit]` opt-out; new `default_init` integration test case.
- Fixed `EnumerateDF` for `ForStmt`, `WhileStmt`, `DoWhileStmt`, and `TimesStmt` and added unit tests in `testing/LoopEnumerationTests.cs`.
- Instance-based `ModuleContext` per module; removed static visitor state.
- Name resolver skeleton with single-segment, cross-module, cyclic-import, and qualified-path resolution.
- Semantic name resolution is now mandatory; removed the `-R` / `--resolve` opt-in flag.
- Function parameters, local `var`s, lambdas, catch variables, and `times` indices are now added to the scope tree.
- Forward declarations via optional bodies in normal and prototype files: `class A;`, `enum E;`, `func f();`, `namespace Ns;`.
- Prototype/extern declaration files (`std/*.decl.myll`) and inline `[extern]` forwarding for classes/namespaces.
- `using namespace` / `using Name` resolution and C++ emission.
- Cross-module namespace merging and ambiguity diagnostics.
- Resolver-to-generator wiring for `TypespecNested` and `ScopedExpr` (resolved `FullyQualifiedName`).
- End-to-end integration test `testing/cases/using_ns/` for merged namespaces and `using`.
- Bit-container types (`bint`, `b8`, `b16`, `b32`, `b64`) backed by unsigned integers; the internal AST kind is now `TypespecBasic.Kind.Bitwise`. `byte` is a separate `std::byte`-backed `TypespecBasic.Kind.Byte`. Bit operators are aliased so `+` is OR, `*` is AND, `-` is AND-NOT, `/` is implication, and `^` is XOR. `byte` and the `b##` family may not mix and only support `==`/`!=`. End-to-end tests: `testing/cases/bint/`, `testing/cases/byte/`.
- Default-sized type dialect model with `DefaultTypeMode` flags (`SizeIndeterminate`, `Forbidden`, `ForbiddenInStruct`, `Size8`...`Size64`, `SizeFast`) and per-type `Dialect.Default.DefaultInt`, `Dialect.Default.DefaultUInt`, `Dialect.Default.DefaultFloat`, and `Dialect.Default.DefaultBint`. The old on/off `AllowFloatKeyword` switch is replaced by `DefaultFloat & Forbidden`. The `ForbiddenInStruct` mode is checked by the type checker for struct/class fields.
- Generator fixes for bit/byte expressions: untyped integer literals are cast to the bit/byte type in comparisons and operator aliases; `std::byte` casts are emitted as `std::byte{ n }`; `0x...E` literals are no longer mis-resolved as floats.
- Rule-of-N class enforcement via `[rule_of_n=0|3|5]`, `[rule_of_0]`, `[rule_of_3]`, and `[rule_of_5]`. The active rule set is checked after name resolution; classes must satisfy at least one allowed rule. Copy/move constructors are recognized and are no longer emitted with `explicit`, enabling copy/move initialization. Cases: `testing/cases/special_members/`, `testing/cases/rule_of_n_fail/`.
- `ctor copy`/`ctor move` shorthand generates the `const C&` / `C&&` parameter automatically, supports optional parameter names, and works with `[default]`/`[delete]`. Case: `testing/cases/special_members/`.
- Per-kind default attributes (`Dialect.Default.DefaultAttributesClass`/`Struct`/`Union`/`Enum`) are parsed and merged before other attribute-consuming transforms.
- Built-in `(move)` and `(forward)` casts return the operand's own type and emit `std::move(...)` / `std::forward(...)`.
- `null` is allowed as an implicit null-pointer constant for raw pointers and smart pointers (`T*`, `T*!`, `T*+`, `T*?`, `T[]!`, `T[]+`, `T[]?`, etc.). It is rejected for value types and references. Added move ctor/move assignment to `Myll::DynArray<T>` that nulls the moved-from `_data`.
- Migrated `AutoReturnTransformer`, `TemplateParamTransformer`, and `ChainTransformer` from `frontend/Program.cs` into `backend/Resolver/Resolver.Resolve` as pre-resolution transforms. Tightened `[chain]` to reject explicit return types, warn on redundant trailing `return self;`, and cleaned up the corresponding boilerplate in `myll/dyn_array.myll`, `testing/cases/external_algorithms/main.myll`, and `frontend/tests/thesis/container.myll`. Added `ChainTransformerTests.cs` and `testing/cases/chain_explicit_return_fail/`.
- Migrated `ElseOnLoopTransformer`, `BreakContinueTransformer`, and `ConfiguredAliasShadowingTransformer` from `frontend/Program.cs` into `backend/Resolver/Resolver.Resolve` as post-resolution transforms. Rewrote `ConfiguredAliasShadowingTransformer` to walk the resolved scope tree and added direct unit tests for `AutoReturnTransformer`, `TemplateParamTransformer`, `ElseOnLoopTransformer`, `BreakContinueTransformer`, and `ConfiguredAliasShadowingTransformer`.
- Removed the broken `Stmt.EnumerateDF` virtual property and replaced it with a test-only `DescendantsAndSelf` extension in `testing/StmtTestExtensions.cs`.
- Aligned prototype-file skipping: `ElseOnLoopTransformer` and `BreakContinueTransformer` now skip `.decl.myll`/`.d.myll`/`.extern.myll` files like the other lowering transforms.
