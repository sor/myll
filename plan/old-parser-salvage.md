# Salvage from oldParser

`oldParser/` is the first ANTLR-based compiler frontend for the language that became Myll.
It contains a full grammar, AST, visitor layer, and an early symbol-resolution sketch.
The compiler was abandoned because the old model could not properly track the stack, namespaces, and lexical scopes.
The new backend is the right foundation; this file only records isolated ideas and code-generation patterns that can be borrowed.

## What oldParser contains

- `oldParser/MyLang.g4` — single-file lexer/parser grammar.
- `oldParser/MyLangVisitor.cs` / `MyLangVisitorExpressions.cs` — AST-building visitors.
- `oldParser/Core/` — AST nodes, literal support, hierarchy/symbol model, and resolution sketches.
- `oldParser/Type.cs` — Myll-to-C++ type and pointer mapping.
- `oldParser/Generator.cs` — ad-hoc C++ generator for classes.
- `oldParser/Extension.cs` — helper extension methods for C++ keyword generation.

## What is NOT worth reusing whole

- **Symbol / scope model.** `oldParser/Core/Library.cs` and `Scope.cs` are early sketches that could not handle stack, namespace, and lexical scope correctly. The current backend still has stubs, but its AST, visitor, and generator architecture is the better place to build a real symbol layer.
- **The visitor layer.** It is tightly coupled to the old AST and grammar.
- **The exact grammar.** `MyLang.g4` is a single monolithic file and should not replace the split lexer/parser in `backend/Grammar/`.
- **The AST class hierarchy.** Names like `MyReturnStmt` do not match the current `backend/Core/` naming; wholesale copying would create more work than porting concepts.

## Salvage: ideas from the symbol sketch

The current backend has only disconnected stubs in `backend/Core/Symbol.cs` and `backend/Core/Attribute.cs`.
The old symbol model is not a drop-in replacement, but it does contain ideas that can inform a new design built on the current architecture:

- `Fragment` — a unified hierarchical symbol with kind, children, base classes, parameters, template parameters, and using namespaces.
- `Search` / `SearchUp` — names for hierarchical lookup.
- `Resolve` / `Unresolve` — tracking unresolved symbols.
- `GenerateUsingNamespaces` — collecting `using` namespaces for generated C++.
- `AddMethod` indexed by parameter list — a pattern for overload sets.

## Salvage: type and pointer mapping

`oldParser/Type.cs` contains a `Pointer` model that is richer than the current `backend/Core/Typespec.Pointer`.

- `Pointer.Type` enum already covers raw `*`, `&`, `&&`, `[*]`, plus `unique/shared/weak` and array/vector/set/multiset variants.
- `Pointer.ToString()` uses a template dictionary to map each pointer kind to C++ syntax.
- The current backend leaves `Map` unmapped and has no set/multiset support, so this mapping is a direct source.

`oldParser/Extension.cs` also has basic-type formatting helpers:

- `MyBasicType.Gen()` — maps signedness and width to C++ keywords.
- `MyBasicType.Signedness.Gen()` — emits `signed`/`unsigned`.

## Salvage: class generation

`oldParser/Generator.cs` is a rough but functional class emitter.

- Splits output into declaration and definition views, similar to the current `.hpp`/`.cpp` split.
- Mines field initializers from assignment statements to build constructor initializer lists.
- Provides a pattern for emitting `ctor` bodies and member access sections.

## Salvage: language features modeled more completely

Several constructs are parsed and represented in `oldParser` but are stubbed or missing in the current backend.

### Properties and access grouping

- `prop` declarations: oldParser has `prop_expr` and `MyProperty`. The current grammar only has accessors attached to `var`/`field`/`const` through `accessorDef`.
- `static { ... }` blocks inside classes.
- `fields { ... }` grouping blocks.
- Current gap: `backend/Generator/HierachicalGen.cs` has no `AddAccessor` method, so accessors are parsed but omitted from output.

### Loops and iteration

- Range/each statement: `expr .. expr stmt` was a first-class `EachStmt` in oldParser. The current grammar has `RangeExpr` and the `..` token, but the only loop statement is C-style `for( init; cond; iter )`.
- Current gap: `backend/Grammar/MyllParser.g4` marks range-based `for` as TODO.

### Constructors and initializer lists

- oldParser parses `initializationList: COLON ID argumentList (COMMA ID argumentList)*`.
- Current grammar has an `initList` rule, but it is narrower and code generation does not use it yet.

### Function and operator declarations

- oldParser uses a top-level `func` keyword and allows operator overloads declared with string literals.
- Current grammar uses `func`/`proc`/`method` plus an `operator` keyword. The old syntax may not be desirable anymore, but the operator string-literal approach is a reference for how a richer operator set could be parsed.

### Expressions

- `sizeof`, `new`, `delete`, `delete[]` are modeled in oldParser. The current backend has grammar support and generators for these, but `NewExpr.Gen()` mutates the AST and related handling is rough.
- C-style cast `(type) expr` is present.

### Smart pointers and container syntax

- oldParser syntax: `@!` unique, `@+` shared, `@?` weak. The current syntax uses `*`/`[*]` with `!`/`+`/`?` suffixes.
- oldParser supports set/multiset variants.
- The template mapping in `oldParser/Type.cs` is the most concrete thing to reuse here.

### Attributes

- oldParser recognized `[[poly]]` and `[[pod(force|permit)]]`.
- Current attributes are parsed generically but the semantics are not implemented.

## Recommended first steps

1. Use `oldParser/Type.cs` mappings to fill in `Map`, set, and multiset support in `backend/Core/Typespec.cs`.
2. Use the old `prop` model when wiring property accessors into `backend/Generator/HierachicalGen.cs`.
3. Use the range/each loop, constructor initializer list, and attribute syntax as references when extending the current grammar and visitor.
4. Treat `oldParser/Core/Library.cs` and `Scope.cs` as idea references only when designing a real symbol layer on top of the current `backend/Core/` AST.
