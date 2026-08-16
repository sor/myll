# Expression Features Plan

## Goal
Complete the expression-level features that the parser already accepts, convert them to C++, and add `discard` support.

## Milestones

### M1: No type information needed
You can implement these features with only the current AST.

#### Discard assignment
- Syntax: `_ = <expr>;`
- C++ output: `(void)(<expr>);`
- Use cases: call something for side effects and ignore its return value, show that you do not need the result, or avoid `[[nodiscard]]` warnings without a dummy variable.
- Implementation: in `MultiAssign.Gen()`, check whether the first expression is a `Discard`. If it is, emit `(void)(<last expression>);`.
- Files: `backend/Core/Stmt.cs`

#### Empty statement
- Syntax: `;`
- C++ output: `;` on its own line, indented to the surrounding scope.
- Implementation:
  - `EmptyStmt.Gen()` and `GenWithoutCurly()` both return `";".IndentAll(level)`.
  - `MultiStmt` keeps an `EmptyStmt` when it is the only statement via `NonEmptyStmts()`. Other stray empty statements are still filtered out.
  - Loop bodies use `VisitBlockify()`. They stay as `MultiStmt` scopes. An empty body becomes `{ ; }`.
- Files: `backend/Core/Stmt.cs`, `backend/Visitor/VStmt.cs`
- Cleanup: later avoid generating `{}` around an empty loop body. Emit `for(...);`, `while(...);`, or `do; while(...);` instead.

### M2: Type information needed
The following features need the type of an expression or the type that the surrounding context expects.

#### Discard as a value
- Syntax:
  - `inOutFunc(_);`
  - `(_, y) = split();` (needs multi-value assignment / destructuring first)
- Not planned: `switch` wildcard case. `default:` already covers this, and we may alias it as `else:` for consistency with `if` and loops.
- Requirements:
  - Know the expected parameter / return type.
  - Generate a temporary of that type so C++ can bind it.
- Examples:
  - `inOutFunc(_)` with parameter `int&` becomes something like `int __discard_tmp; inOutFunc(__discard_tmp);`.
  - `return _` is probably a semantic error until the language defines what it means.
- Files: `backend/Core/Expr.cs`, `backend/Core/Mixed.cs`, `backend/Core/Stmt.cs`

#### Copy-cast `(copy)(expr)`
- Syntax: `(copy)(expr)`
- Requirements:
  - Resolve the value type of `expr` (strip references).
  - Emit a copy construction or functional cast: `T(expr);`.
- Note: the grammar and visitor already contain the needed parts (`COPY` token, `Operand.CopyCast`). `VExpr.cs` currently throws an exception.
- Files: `backend/Visitor/VExpr.cs`, `backend/Core/Expr.cs`

#### Move-cast `(move)(expr)` and forward-cast `(forward)(expr)`
- Syntax: `(move)(expr)`, `(forward)(expr)`
- Status: partially scaffolded; verify whether the current output compiles.
- Requirements:
  - For `forward`, know the template parameter type context.
- Files: `backend/Visitor/VExpr.cs`, `backend/Core/Expr.cs`

### M3: Function-call features
These features need function signature information.

#### Null-coalescing function call
- Syntax: `obj?.method(args)`
- Requirements:
  - Resolve that `obj` is a nullable / pointer-like type.
  - Emit `obj ? obj->method(args) : default` or similar.
- Files: `backend/Core/Mixed.cs`

#### Named function arguments
- Syntax: `func(a: 1, b: 2)`
- Requirements:
  - Function signature with parameter names and default values.
  - Reorder / fill arguments before generating the call.
- Files: `backend/Core/Mixed.cs`

### M4: Loop sugar
These are small syntax extensions that need a little transformation.

#### Else on loops
- Syntax: `while(cond) { ... } else { ... }`, `for(...;...;...) { ... } else { ... }`
- Implementation: add a flag that tracks whether the loop exits with `break`. Emit the `else` block only when it did not.
- Files: `backend/Core/Stmt.cs`

#### Multi-statement for init
- Syntax: `for(a; b; c, d) { ... }`
- Implementation: allow `MultiStmt` in the init position and emit the statements separated by commas.
- Files: `backend/Core/Stmt.cs`

### M5: Constructor shortcut

#### Motivation
Long type names make variable initialization noisy and error-prone, especially when the desired constructor is just the type itself. The C++ form `MyType name(args);` is also famously ambiguous with a function declaration, so Myll intentionally avoids it. A self-describing keyword avoids both of these problems while still being obvious to new users.

#### Proposed syntax
The first supported form should be the explicit-assignment form because it reuses existing expression machinery and is unambiguous:

- `var Type name = ctor(args);`
- Example: `var std::filesystem::path p = ctor(argv[1]);` emits `std::filesystem::path p(argv[1]);` or an equivalent construction.

Later forms, which need grammar work to accept braced lists in a definition statement:

- `var Type name = { args };`
- `var Type name { args };`

Rejected form:

- `Type name(args);` — looks like a function declaration, so it will not be valid Myll.

#### Why it only works in contexts that provide a type
`ctor()` is a shortcut that says "construct an instance of the type I already told you about." In a direct variable declaration that type is explicit on the left-hand side, so resolution is local and unambiguous. A `return` statement inside a function with a declared return type has the same property, because C++ already accepts `return {42, 13};` when the return type is known.

Contexts that provide a type for the initial implementation:

- `var Type name = ctor(args);` — type from the variable declaration.
- `return ctor(args);` — type from the enclosing function signature.

Contexts without enough type information, so they are out of scope at first:

- `auto x = ctor();` — no declared type to fill in, so the meaning is ambiguous.
- `f(ctor());` — the parameter type is not yet resolved in the current pipeline, even though C++ could infer it from the overload set.

#### Implementation sketch
- Add a new AST expression node that records the constructor arguments and a flag that marks it as a shortcut.
- In the variable-definition visitor, when the initializer is a `ctor(...)` shortcut, replace it with a normal constructor call whose type is the declared type from the definition.
- Optionally extend the grammar so `ctor` can appear as a generic callable expression, but type-check it later so it only passes where the expected type is available.
- Files: `backend/Grammar/MyllParser.g4`, `backend/Visitor/VStmt.cs`, `backend/Visitor/VExpr.cs`, `backend/Core/Expr.cs`, `backend/Generator/...`.

## Known parser issue: chained `[][]` with `&&`

Single chained indexing and simple comparisons parse correctly:

```myll
var char c = argv[1][0];
if( argv[i][0] == '-' ) { }
```

However, combining two chained-index comparisons with `&&` currently fails:

```myll
if( argv[i][0] == '-' && argv[i][1] == '\0' ) { }
// mismatched input '==' expecting ')'
```

This was discovered while porting Unix utilities. Using a single string comparison (`(string)argv[i] == "-"`) works around it for that use case; the underlying expression-grammar interaction still needs a real fix.

## Open questions
- Should `return _` be a semantic error, or should it mean "return default"?
- Do we want a separate `discard` type in the type system, or treat `_` purely as a parser-level marker?
- For `(copy)(expr)`, should it be allowed to return a prvalue copy even when the source is already a prvalue?
- Should `switch` support `else:` as an alias for `default:`? This would make the fallback keyword consistent with `if` and loops. Should we deprecate `default:`?
