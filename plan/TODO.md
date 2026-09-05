# Open TODOs

This file consolidates the remaining work items that came out of the visitor audit, the Unix-app porting work, and the `AGENTS.md` planned list.
The endboss architecture is described in `plan/semantic-analysis.md`.

## Endboss — semantic analysis / ScopeStack

This is the overarching long-term goal that blocks many smaller features. Finish the scope-stack based semantic-analysis pass so the compiler can resolve names to declarations, type-check expressions, and disambiguate language constructs that currently rely on string matching or syntactic guesswork.

- `backend/Core/Scope.cs` holds the tree (`Scope`, `ScopeLeaf`, `children`, `importedScopes`).
- `backend/Visitor/VMain.cs` pushes/pops scopes during visiting and records unresolved identifiers, types, and member accesses.
- `backend/Resolver/Resolver.cs` runs a fixed-point resolver across imported modules and produces diagnostics for unresolved or ambiguous names.
- The C++ generator consumes `ResolutionResult` for `TypespecNested`, `ScopedExpr`, and member access; `IdExpr` resolution is recorded but not yet used for unqualified identifiers.
- `backend/Core/Symbol.cs` and `backend/Core/Attribute.cs` remain disconnected stubs for a future stable design.

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

## High priority

1. **Per-declaration access modifiers** — done
   - Implemented `[pub]`, `[priv]`, `[prot]` on individual class/struct/union members; the attribute overrides the current section default from `[pub]:` / `[priv]:` / `[prot]:`.
   - Field order is preserved; access labels change inline.
   - End-to-end test: `testing/cases/access_mods/`.
   - Enforced: access attributes are only valid inside class/struct/union declarations.

2. **Repair the `try/catch` grammar**
   - Change `catchClause` from `CATCH funcTypeDef? stmt` to `CATCH LPAREN param? RPAREN stmt`.
   - Update `VStmt.VisitStmtTryCatch` and any generator formatting that depends on the catch clause.

3. **Implement reachable missing expression/statement features**
   - Named function arguments (`Arg.Gen` in `backend/Core/Mixed.cs`).
   - Null-coalescing function calls (`FuncCall.Gen` in `backend/Core/Mixed.cs`).
   - Copy-cast (`VisitPreExpr` in `backend/Visitor/VExpr.cs`).
   - Discard expression (`Discard.Gen` in `backend/Core/Expr.cs`).

4. **Improve compiler diagnostics**
   - Replace empty `throw new NotImplementedException()` calls with messages that include the construct and source location.
   - Emit a clear error when `alias` is used on a namespace until namespace aliases are supported.

5. **Prototype declaration files**
   - Adopt `.d.myll` and `.decl.myll` as the extensions for silent resolver-only prototypes.
   - Inline `[extern]` in normal `.myll` files should emit C++ forward declarations.
   - Extend `[extern]` propagation to classes/structs so members resolve but are not emitted.
   - Add grammar/visitor/generator support for forward declarations in prototype files.
   - See `plan/prototype-files.md`.

6. **Direct constructor syntax and initializer-list literals** — done
   - Variables can be declared with `var T name(args);` (and `const`/`let`/`field` variants) instead of `var T name = T(args);`.
   - Grammar: `idAccessor` accepts an optional `funcCall` in addition to `= expr`; `expr` accepts `LBRACK args? RBRACK` as `InitListExpr`; `typespec` accepts `initlistType`; `kindOfPassing` accepts `INITLIST`.
   - AST: `VarDecl`/`VarStmt` carry an explicit `isDirectConstruct` flag and store the constructor call in `init` as a `FuncCallExpr`. A new `InitListExpr` node stores the brace-init-list elements. `TypespecNested.isInitList` identifies the `initlist`/`ilist` keyword type.
   - Generator emits `type name(args);` for direct construction and `{ e1, e2 }` for initializer lists. `initlist<T>`/`ilist<T>` emit `std::initializer_list<T>`.
   - Type inference: an empty `[ ]` with `auto`/bare `initlist`/`ilist` is rejected; otherwise the element common type is computed. `var auto a = [1,2,3];`, `var initlist<int> a = [1,2,3];`, and `var ilist a = [1,2,3];` all infer `std::initializer_list<int>`.
   - Added special members `ctor initlist` and `operator initlist =`. They generate a `const std::initializer_list<E>&` parameter, default parameter name `init`, and are implicitly `[implicit]`. The element type `E` defaults to the enclosing class's first template parameter or can be written explicitly (`ctor initlist<E>`).
   - Added `std/std_initializer_list.decl.myll` and updated `myll/dyn_array.myll` to use `ctor initlist`. The `<initializer_list>` header is now included by default.
   - Regression cases: `testing/cases/direct_ctor/`, `testing/cases/initializer_list/`.

## Medium priority

4. **Language syntax gaps**
   - Range-based `for` loops (`for( a : b )`).
   - Namespace aliases via `alias` (`alias fs = std::filesystem;` should generate `namespace fs = std::filesystem;`).
   - Auto-property / combined private-field + public-getter syntax.

5. **Cleanup**
   - Remove `oldParser/` and `oldCodeGen/` once nothing references them.
   - Triage the TODO/FIXME/HACK markers across backend/frontend/testing.
   - Clean up the `= null!` initializers by introducing real constructors or nullable annotations.

6. **Partial/broken generators**
   - Complete `VisitScopedExpr` (`backend/Visitor/VExpr.cs`).
   - Review/replace the lambda hack in `VisitLambdaExpr`.
   - Validate the switch/case generator paths (`caseBlock`, `defaultBlock`, `StmtSwitch`).

7. **Other generators / language features**
   - Accessor/property generation (currently parsed but skipped).
   - Subclass/inheritance generation (partially implemented).

## Lower priority / design decisions

8. **Imports**
   - Support subdirectory paths in import names or add a string-form import (`import "some/lib/header.hpp";`).
   - File-wide imports are the end goal; module-wide imports may be used as a first step.

9. **CI/CD**
   - Set up a CI workflow for automated builds and tests.
   - Decide whether to commit the generated ANTLR files or always regenerate during build.

10. **Open language design questions**
   - Should `return _` mean "return default"?
   - Should `switch` support `else:` as an alias for `default:`?
   - Should `(copy)(expr)` be allowed on prvalues?
   - **Switch-case fallthrough dialect**: allow the user/program to select whether cases implicitly `break`, implicitly fall through, or require an explicit `break`/`fallthrough` annotation per case. The mode is modeled in `backend/Core/Dialect.cs` (`SwitchFallthroughMode`) but is not wired through the parser/generator yet.
    - **Linkage model**: work in progress on another machine. Decide which declarations get internal/external/module linkage and how it interacts with cross-module imports.
   - **Export control**: is `[hide]`/`[hidden]` the only module-export mechanism, or will there be explicit `export`?

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
- Instance-based `CompilationContext` per module; removed static visitor state.
- Name resolver skeleton with single-segment, cross-module, cyclic-import, and qualified-path resolution.
- `-R` / `--resolve` flag and `--extern-dir` for opt-in semantic checks.
- Function parameters, local `var`s, lambdas, catch variables, and `times` indices are now added to the scope tree.
- Forward declarations via optional bodies in normal and prototype files: `class A;`, `enum E;`, `func f();`, `namespace Ns;`.
- Prototype/extern declaration files (`std/*.decl.myll`) and inline `[extern]` forwarding for classes/namespaces.
- `using namespace` / `using Name` resolution and C++ emission.
- Cross-module namespace merging and ambiguity diagnostics.
- Resolver-to-generator wiring for `TypespecNested` and `ScopedExpr` (resolved `FullyQualifiedName`).
- End-to-end integration test `testing/cases/using_ns/` for merged namespaces and `using`.
- Bit-container types (`bint`, `b8`, `b16`, `b32`, `b64`) backed by unsigned integers; the internal AST kind is now `TypespecBasic.Kind.Bitwise`. `byte` is a separate `std::byte`-backed `TypespecBasic.Kind.Byte`. Bit operators are aliased so `+` is OR, `*` is AND, `-` is AND-NOT, `/` is implication, and `^` is XOR. `byte` and the `b##` family may not mix and only support `==`/`!=`. End-to-end tests: `testing/cases/bint/`, `testing/cases/byte/`.
- Default-sized type dialect model with `DefaultTypeMode` flags (`SizeIndeterminate`, `Forbidden`, `ForbiddenInStruct`, `Size8`...`Size64`, `SizeFast`) and per-type `Dialect.DefaultInt`, `DefaultUInt`, `Dialect.DefaultFloat`, and `Dialect.DefaultBint`. The old on/off `AllowFloatKeyword` switch is replaced by `DefaultFloat & Forbidden`. The `ForbiddenInStruct` mode is checked by the type checker for struct/class fields.
- Generator fixes for bit/byte expressions: untyped integer literals are cast to the bit/byte type in comparisons and operator aliases; `std::byte` casts are emitted as `std::byte{ n }`; `0x...E` literals are no longer mis-resolved as floats.
- Rule-of-N class enforcement via `[rule_of_n=0|3|5]`, `[rule_of_0]`, `[rule_of_3]`, and `[rule_of_5]`. The active rule set is checked after name resolution; classes must satisfy at least one allowed rule. Copy/move constructors are recognized and are no longer emitted with `explicit`, enabling copy/move initialization. Cases: `testing/cases/rule_of_n/`, `testing/cases/rule_of_n_fail/`.
- `ctor copy`/`ctor move` shorthand generates the `const C&` / `C&&` parameter automatically, supports optional parameter names, and works with `[default]`/`[delete]`. Case: `testing/cases/special_ctors/`.
- Per-kind default attributes (`Dialect.DefaultAttributesClass`/`Struct`/`Union`/`Enum`) are parsed and merged before other attribute-consuming transforms.
- Built-in `(move)` and `(forward)` casts return the operand's own type and emit `std::move(...)` / `std::forward(...)`.
- `null` is allowed as an implicit null-pointer constant for raw pointers and smart pointers (`T*`, `T*!`, `T*+`, `T*?`, `T[]!`, `T[]+`, `T[]?`, etc.). It is rejected for value types and references. Added move ctor/move assignment to `Myll::DynArray<T>` that nulls the moved-from `_data`.
- Migrated `AutoReturnTransformer`, `TemplateParamTransformer`, and `ChainTransformer` from `frontend/Program.cs` into `backend/Resolver/Resolver.Resolve` as pre-resolution transforms. Tightened `[chain]` to reject explicit return types, warn on redundant trailing `return self;`, and cleaned up the corresponding boilerplate in `myll/dyn_array.myll`, `testing/cases/external_algorithms/main.myll`, and `frontend/tests/thesis/container.myll`. Added `ChainTransformerTests.cs` and `testing/cases/chain_explicit_return_fail/`.
- Migrated `ElseOnLoopTransformer`, `BreakContinueTransformer`, and `ConfiguredAliasShadowingTransformer` from `frontend/Program.cs` into `backend/Resolver/Resolver.Resolve` as post-resolution transforms. Rewrote `ConfiguredAliasShadowingTransformer` to walk the resolved scope tree and added direct unit tests for `AutoReturnTransformer`, `TemplateParamTransformer`, `ElseOnLoopTransformer`, `BreakContinueTransformer`, and `ConfiguredAliasShadowingTransformer`.
- Removed the broken `Stmt.EnumerateDF` virtual property and replaced it with a test-only `DescendantsAndSelf` extension in `testing/StmtTestExtensions.cs`.
