# Grammar and Visitors

## Grammar Completeness: ~85%

### Lexer (`MyllLexer.g4`)

**Strengths:**
- Comprehensive token set
- Unicode operator support (`·`, `×`, `÷`)
- Custom channels for newlines and comments
- Shebang support
- Fragment rules for reusable patterns

**Gaps:**
- `RSHIFT` (`>>`) is handled as two separate `>` tokens to avoid template ambiguity
- `DBL_STAR` (`**`) is handled as two `STAR` tokens in parser
- Contextual parameter keywords (`look`, `edit`, `share`, `give`) are parsed but semantic handling is partial

### Parser (`MyllParser.g4`)

**Implemented:**
- Modules, namespaces, all declaration types
- Control flow (if/switch/loop/for/while/do-while/times)
- Expressions with full precedence climbing
- Types including smart pointers and arrays
- Templates (basic)
- Lambdas
- Casts

**Stubbed / TODO in Grammar:**
- `aspect` — parser rule exists, body is `// TODO`
- `concept` — parser rule exists, body is `// TODO`
- `convert` — parsed but visitor is incomplete
- `range-based-for` — not in grammar
- `continue` variants — partially parsed

**Known Issues:**
- Template arguments can behave greedily and consume too many tokens in some expressions
- `powOP` (`* *`) conflicts with pointer dereference in edge cases (ANTLR precedence handles most cases)

## Visitor Completeness: ~65%

### Implemented Visitors

| File | Coverage |
|------|----------|
| `VMain.cs` | Scope stack management, bodyless namespace cleanup |
| `VDecl.cs` | All major declarations except `aspect`, `concept`, `convert` |
| `VExpr.cs` | Full expression tree including casts, lambdas, member access |
| `VStmt.cs` | Most statements except `try/catch`, `defer`, `continue`, `return-if` |
| `VTpl.cs` | Template arguments and parameters |
| `VTypes.cs` | All type specifications |
| `VExt.cs` | Operator mappings, visitor extensions, glue code |

### Missing Visitor Overrides

| Parser Rule | Status | Impact |
|-------------|--------|--------|
| `stmtTryCatch` | No override | try/catch blocks not built into AST |
| `stmtDefer` | No override | defer not built into AST |
| `stmtContinue` / `stmtContinue2` | No override | continue not built into AST |
| `stmtReturnIf` | No override | conditional return not built into AST |
| `defAspect` | Calls base | aspect declarations ignored |
| `defConcept` | Calls base | concept declarations ignored |
| `defConvert` | Calls base / incomplete | conversion functions ignored |
| `RangeExpr` | No override | range expressions not built |
| `ThreeWayConditionalExpr` | No override | `??` operator not fully handled |
| `ThrowExpr` | No override | throw as expression not fully handled |

### Code Quality Issues in Visitors

- `curAccess` field in `VDecl.cs` is mutable state for access modifiers. The author acknowledges this "will be buggy" and "needs to move to ScopeStack."
- `VisitDefVar` logic is duplicated between `VDecl` and `VStmt`.
- `VisitLit` and `VisitLiteralExpr` in `VExpr.cs` are duplicate methods.
- `VisitLambdaExpr` is marked "TODO this is probably just one big hack."
- `VisitPreExpr` for `MOVE` and `FORWARD` creates fake `TypespecNested` nodes (works but is a hack).

## Future Outlook

### Short-Term (Quick Wins)
1. Wire `stmtTryCatch` visitor — grammar exists, just needs AST building.
2. Wire `stmtDefer` visitor.
3. Remove duplicate `VisitLit` / `VisitLiteralExpr`.
4. Consolidate `VisitDefVar` logic.

### Medium-Term
5. Implement `continue` and `return-if` visitors.
6. Implement `RangeExpr` visitor.
7. Implement `ThreeWayConditionalExpr` and `ThrowExpr` visitors.
8. Replace mutable `curAccess` with scope-stack tracking.

### Long-Term
9. Implement `aspect` and `concept` visitors when those features are designed.
10. Make visitors instance-based (eliminate static state).
