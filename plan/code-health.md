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
