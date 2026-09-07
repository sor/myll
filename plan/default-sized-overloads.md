# Default-sized vs fixed-width overloads

Myll treats `int` (default-sized) and `i32` (explicitly 32-bit) as distinct types, with a family conversion between them.
This lets overload resolution prefer `func f(int)` for an `int` argument and `func f(i32)` for an `i32` argument.

In generated C++, however, `int` is typically emitted as `int` while `i32` is emitted as `std::int32_t`, which on most platforms are the same C++ type.
Declaring both overloads in the same C++ scope can therefore produce a redefinition error.

## Possible directions

- Conditionally outlaw default-sized integer types as members of `class`/`struct` declarations.
- Conditionally outlaw default-sized parameter types in overload sets.
- Keep them distinct on the Myll side and leave the collision to the C++ compiler.

This needs a dedicated design decision before it becomes a user-facing problem.
