# Expression Features Plan

## Goal
Finish the expression-level features that are already parsed but not yet
lowered to C++, and add the missing `discard` support.

## Milestones

### M1: No type information needed
These can be implemented with the current AST only.

#### Discard assignment
- Syntax: `_ = <expr>;`
- C++ output: `(void)(<expr>);`
- Implementation: special-case `leftExpr is Discard` in `AggrAssign.Gen()`
  for `Operand.Assign`.
- Files: `backend/Core/Stmt.cs`

#### Empty statement
- Syntax: `;`
- C++ output: `;`
- Implementation: make `EmptyStmt.Gen()` return the same as
  `GenWithoutCurly()`.
- Files: `backend/Core/Stmt.cs`

### M2: Type information needed
The following features need the resolved type of an expression or the
expected type from the surrounding context.

#### Discard as a value
- Syntax: `inOutFunc(_);` or `return _;`
- Requirements:
  - Know the expected parameter / return type.
  - Generate a temporary of that type so C++ can bind it.
- Examples:
  - `inOutFunc(_)` with parameter `int&` should become something like
    `int __discard_tmp; inOutFunc(__discard_tmp);`.
  - `return _` should likely be a semantic error until the language defines
    what it means; or it needs the function return type.
- Files: `backend/Core/Expr.cs`, `backend/Core/Mixed.cs`,
  `backend/Core/Stmt.cs`

#### Copy-cast `(copy)(expr)`
- Syntax: `(copy)(expr)`
- Requirements:
  - Resolve the value type of `expr` (strip references).
  - Emit a copy construction or functional cast: `T(expr);`.
- Note: grammar and visitor already have scaffolding (`COPY` token,
  `Operand.CopyCast`), but `VExpr.cs` currently throws.
- Files: `backend/Visitor/VExpr.cs`, `backend/Core/Expr.cs`

#### Move-cast `(move)(expr)` and forward-cast `(forward)(expr)`
- Syntax: `(move)(expr)`, `(forward)(expr)`
- Status: partially scaffolded; verify whether current output compiles.
- Requirements:
  - For `forward`, know the template parameter type context.
- Files: `backend/Visitor/VExpr.cs`, `backend/Core/Expr.cs`

### M3: Function-call features
These need function signature information.

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
Small syntax extensions that need a little transformation.

#### Else on loops
- Syntax: `while(cond) { ... } else { ... }`, `for(...;...;...) { ... } else { ... }`
- Implementation: desugar to a flag that tracks whether the loop was exited
  with `break`, then emit the `else` block only when it was not.
- Files: `backend/Core/Stmt.cs`

#### Multi-statement for init
- Syntax: `for(a; b; c, d) { ... }`
- Implementation: allow `MultiStmt` in the init position and emit the
  statements separated by commas.
- Files: `backend/Core/Stmt.cs`

## Open questions
- Should `return _` be a semantic error, or should it mean "return default"?
- Do we want a separate `discard` type in the type system, or treat `_`
  purely as a parser-level marker?
- For `(copy)(expr)`, should it be allowed to return a prvalue copy even
  when the source is already a prvalue?
