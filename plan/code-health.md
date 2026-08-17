# Code health work

This file tracks general cleanup work that does not require a design decision.
It is separate from `suspicious-warnings.md`, which is reserved for warning clusters that expose deeper behavioral problems.

## Remove null-forgiving initializers (`= null!`)

When migrating files to `#nullable enable`, late-bound fields and properties (typically filled in by the visitor after construction) were initialized with `= null!` to silence `CS8618` without immediately rewriting every call site.

This is a migration crutch, not the end state: it suppresses warnings but also removes compile-time null safety for those fields. After the nullable migration is complete, every `= null!` in the codebase should be revisited and removed. For each one, choose either:

- Introduce a real constructor that takes the value up front.
- Annotate the field/property as nullable and add proper null guards (or targeted `!`) at the exact points where non-null use is guaranteed.

The constructor option is the safest. The nullable-field option is smaller and fits the current visitor pattern, but it ripples into every consumer of the field.

Current `= null!` count: 43 occurrences, mostly in `backend/Core/Expr.cs`, `backend/Core/Stmt.cs`, `backend/Core/Decl.cs`, `backend/Core/Mixed.cs`, and `backend/Core/Typespec.cs`.

## Nullable-reference-type migration status

`backend.csproj` and `frontend.csproj` are now both on full `<Nullable>enable</Nullable>`.

All C# nullability warnings are fixed. The only remaining warning is the unrelated package compatibility warning for `System.Text.Encoding.CodePages 10.0.2` / `net6.0`.

Clean build results:

- Release: 0 C# warnings, 1 package warning
- Debug: 0 C# warnings, 1 package warning

Remaining work: clean up the 43 `= null!` initializers by introducing real constructors or nullable annotations where appropriate.

## Clarify `var` in function parameter lists

Writing a reference/out parameter with `var` currently produces a confusing parser error:

```myll
func f( var int & argi ) -> void  // error: cryptic message
func f( int & argi )    -> void  // ok
```

`var`/`let`/`const` make sense for variable declarations because they introduce a new name, but in a parameter list the type is required. The parser should either:

- Reject `var`/`let`/`const` in a parameter with a clear message such as "function parameters cannot use 'var'; write the type directly (e.g. 'int & argi')"; or
- Silently ignore the redundant keyword, since it carries no useful information in that position.

The latter is more forgiving; the former teaches the language model. Either way is better than the current diagnostic.

Files: `backend/Grammar/MyllParser.g4`, `backend/Visitor/VDecl.cs` or parser validation step.

## Import/include paths cannot contain slashes

Dotted import names map directly to a quoted C++ include:

```myll
import cxxopts.hpp;
// -> #include "cxxopts.hpp"
```

This works as long as the header lives directly on the include path (via `-I...`), but it cannot currently express a subdirectory:

```myll
import some.lib.header.hpp;  // -> #include "some.lib.header.hpp", not "some/lib/header.hpp"
```

Options to resolve this later:

- Allow `/` in import names and translate it to a path separator in the generator.
- Introduce an explicit string-form import for paths, e.g. `import "some/lib/header.hpp";`.
- Establish a convention that `.` in import names maps to `/` (breaks filenames that contain real dots).

No implementation yet — needs a design decision first.

## `try`/`catch` code generation

Implemented. `try`/`catch` blocks now lower to valid C++.

Supported forms:

```myll
try {
    risky();
}
catch( const std::exception & e ) {
    std::cerr << e.what() << "\n";
}
catch() {
    std::cerr << "unknown error\n";
}
```

Files touched: `backend/Core/Stmt.cs`, `backend/Visitor/VStmt.cs`, `backend/Generator/StmtFormatting.cs`.

## Audit visitor overrides for unimplemented grammar rules

The parser accepts more constructs than the C++ generator/visitor currently handles. Some rules throw `NotImplementedException` or `NotSupportedException` at runtime; others silently produce broken or missing code (see the `try`/`catch` case above).

We should systematically go through the generated visitor base class and every override in `backend/Visitor/` to identify:

- Grammar rules that have **no override** and fall back to the generated base (which may silently skip the node).
- Overrides that **throw** `NotImplementedException`/`NotSupportedException` and decide whether to implement, remove, or improve the error message.
- Overrides that **generate partial/broken C++** (e.g. `try`/`catch`).

Where a construct is accepted by the grammar but cannot yet be lowered, the backend should throw a clear `NotImplementedException` or `NotSupportedException` rather than silently emit partial or broken C++. Silent mis-generation is much harder to debug than an explicit runtime error.

Files: `backend/Grammar/Generated/` (generated visitor base), `backend/Visitor/*.cs`, `backend/Generator/*.cs`.
