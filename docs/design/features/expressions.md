# Expressions and Operators

Myll provides a rich expression language with C++ semantics but improved syntax.

## Operator Precedence

From highest to lowest:

1. Postfix: `expr++`, `expr--`, `.`, `->`, `()`, `[]`
2. Prefix: `++expr`, `--expr`, `+`, `-`, `~`, `!`, `*`, `&`
3. Power: `**`
4. Multiplicative: `*`, `/`, `%`, `·` (dot), `×` (cross), `÷`
5. Additive: `+`, `-`
6. Shift: `<<`, `>>`
7. Relational: `<`, `<=`, `>`, `>=`
8. Equality: `==`, `!=`
9. Bitwise AND: `&`
10. Bitwise XOR: `^`
11. Bitwise OR: `|`
12. Three-way: `??`
13. Logical AND: `&&`
14. Logical OR: `||`
15. Null-coalescing: `??` [at expression level]
16. Ternary: `?:`
17. Assignment: `=`, `+=`, `-=`, etc.

## Special Operators

### Power
```
result = base ** exponent;      // std::pow(base, exponent)
```

### Dot Product
```
result = a · b;                 // std::inner_product or manual loop
```

### Cross Product
```
result = a × b;                 // cross product (3D vectors)
```

### Division
```
result = a ÷ b;                 // floating-point division even for integrals
```

## Member Access

```
obj.field
obj.method(args)
```

## Null-Coalescing Access

```
obj?.field          // if obj != null, obj->field, else default
obj?[index]         // if obj != null, obj[index], else default
obj?(args)          // if obj != null, obj(args), else default
```

## Casts

```
(Type)expr          // static_cast
(?Type)expr         // dynamic_cast
(-Type)expr         // const_cast
(!Type)expr         // std::bit_cast
(!!Type)expr        // reinterpret_cast
(move)expr          // std::move
(forward)expr       // std::forward
```

Myll's parenthesized cast syntax revives the familiar C-style form but removes its dangerous multi-attempt behavior. Each prefix marker maps to exactly one C++ cast kind.
expr as T           // static_cast
expr dynamic T      // dynamic_cast
expr const T        // const_cast
expr bit T          // reinterpret_cast (bit-level)
expr reinterpret T  // reinterpret_cast (alias)
```

## Lambdas

```
var lambda = |x: int| -> int { return x * 2; };
var short = |x| { return x * 2; };  // types inferred
```

## Move and Forward

```
var moved = move expr;
var forwarded = forward expr;
```

## Sizeof

```
sizeof(Type)
sizeof expr
```

## Future Directions

- Range expressions: `1..10` or `1...10`.
- Array comprehensions.
- String interpolation.

## Implementation Notes

- `ExprFormatting.cs` maps `Operand` enum values to C++ format strings.
- `Precedence` tables handle operator precedence and parenthesization.
- `FlattenRelational` handles chained comparisons (`a < b < c`).
- `NewExpr.Gen()` mutates the AST — critical bug.
- Several null-coalescing expression variants exist in `Operand` enum but are not fully handled in `Gen()`.
