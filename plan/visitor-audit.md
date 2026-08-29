# Visitor override audit

Date: 2026-08-17.

## Summary

This audit compares the generated `IMyllParserVisitor<Result>` interface (`backend/obj/Debug/net6.0/MyllParserVisitor.cs`) with all visitor code in `backend/Visitor/*.cs`. The goal is to find grammar constructs that parse but do not reach code generation, or that reach generation in a broken/partial state.

A number of constructs are accepted by the parser and silently dropped, which is worse than an explicit error. The most dangerous silent holes are `defer`, `do return if`, `expr...expr` ranges, and expression-level `throw`. These should either be implemented or made to throw a clear `NotSupportedException`/`NotImplementedException` until they are implemented.

## Immediate fixes applied

During the audit the following safe "fail loudly" overrides were added:

- `VisitStmtDefer` -> `NotImplementedException`
- `VisitStmtReturnIf` -> `NotImplementedException`
- `VisitRangeExpr` -> `NotImplementedException`
- `VisitThrowExpr` -> `NotImplementedException`
- `VisitThreeWayConditionalExpr` -> `NotImplementedException`
- `VisitDefAspect` / `VisitDefConcept` / `VisitDefConvert` -> `NotSupportedException` with reference to `REASONS.md`

These changes turn silent mis-generation into explicit errors. `dotnet test testing/` still passes 10/10.

## Methodology

- Generated visitor interface methods extracted from `MyllParserVisitor.cs`.
- `public override` methods from `VDecl.cs`, `VExpr.cs`, `VStmt.cs` extracted.
- `new` helper methods from `ExtendedVisitor<Result>` and extension methods from `VisitorExtensions` treated as coverage.
- Reachable missing rules were tested with minimal `.myll` files to see whether they crash or silently mis-generate.

## Coverage numbers

| Category | Count |
|----------|------:|
| Visitor interface methods | 150 |
| `public override` implementations | 65 |
| `new` helper methods in `ExtendedVisitor<Result>` | 11 |
| Extension helper methods in `VisitorExtensions` | 15 |
| Truly missing overrides | 68 |

Of those 68 missing methods many are internal token wrappers, helper rules visited privately, or top-level wrappers that fall through to implemented child rules. The ones that matter are listed below.

## Findings

### 1. Silent codegen holes

These constructs parse without complaint and the generated C++ is wrong because the visitor override is missing.

| Construct | Grammar rule | Observed behavior | Status |
|-----------|--------------|-------------------|--------|
| range expression `a...b` | `RangeExpr` | left operand dropped, emits `5` instead of a range | fixed: now throws `NotImplementedException` |
| expression-level `throw e` | `ThrowExpr` | `throw` keyword dropped, emits operand only | fixed: now throws `NotImplementedException` |
| `defer stmt;` | `stmtDefer` | entire statement omitted | fixed: now throws `NotImplementedException` |
| `do return x if(cond);` | `stmtReturnIf` | NRE crash in `VisitStmt` | fixed: now throws `NotImplementedException` |
| `continue case` / `continue default` / `continue else` | `stmtContinue2` | throws `NotImplementedException` (acceptable) | keep or implement |

### 2. Construct stubs that call base and return `default`

These overrides exist but do nothing useful, so code that uses them silently vanishes.

| Method | File | Construct | Status |
|--------|------|-----------|--------|
| `VisitDefAspect` | `backend/Visitor/VDecl.cs:273` | aspect definition | fixed: now throws `NotSupportedException` |
| `VisitDefConcept` | `backend/Visitor/VDecl.cs:276` | concept definition | fixed: now throws `NotSupportedException` |
| `VisitDefConvert` | `backend/Visitor/VDecl.cs:329` | convert definition | fixed: now throws `NotSupportedException` |

These ruled-out/deferred features are documented in `REASONS.md`.

### 3. Runtime `NotImplementedException` / `NotSupportedException` throws

The following paths throw at compile/codegen time. They are honest errors, but several messages are unhelpful.

| Location | Rule/Method | Message or trigger |
|----------|-------------|--------------------|
| `backend/Core/Mixed.cs:62` | named argument in function call | "named function arguments needs to be implemented" |
| `backend/Core/Mixed.cs:80` | null-coalescing function call (`foo?(...)`) | "null coalescing for function calls needs to be implemented" |
| `backend/Core/Stmt.cs:150` | `BreakStmt.Gen` with `depth != 1` | "no depth except 1 supported directly" |
| `backend/Core/Stmt.cs:164` | `ContinueStmt.Gen` with `depth != 1` | "no depth except 1 supported directly" |
| `backend/Core/Stmt.cs:424` | `ForStmt.Gen` with `else` | "Else for for-loop not implemented yet" |
| `backend/Core/Stmt.cs:427` | `ForStmt.Gen` with `MultiStmt` init | "A MultiStmt can not be used in for-loop as init" |
| `backend/Core/Stmt.cs:431` | `ForStmt.Gen` with multiple initializers | "for statement does not support more than one initializer yet" |
| `backend/Core/Stmt.cs:467` | `WhileStmt.Gen` with `else` | "implement else for while-loop" |
| `backend/Core/Expr.cs:208` | `Discard.Gen` | empty `NotImplementedException()` |
| `backend/Core/Decl.cs:93` | `Decl.Gen` base | "plx implement in missing class: {TypeName}" |
| `backend/Visitor/VExpr.cs:103` | copy-cast `((copy)T)x` inside `VisitPreExpr` | "copy-cast might need to introduce a new local to work" |
| `backend/Visitor/VStmt.cs:241` | `VisitStmtContinue2` | "continue case/default/else is not implemented yet" |
| `backend/Visitor/VStmt.cs:259` | `VisitStmtTryCatch` catch with wrong arity | "catch clause must have exactly one parameter or none" |
| `backend/Visitor/VDecl.cs:163` | `VisitAttrColon` unknown access attribute | "Got unsupported attribute in AttribState: ..." |
| `backend/Visitor/VDecl.cs:385` | `VisitDefOp` non-copy/move op= | "only copy and move special assignment ops are supported" |
| `backend/Generator/HierarchicalGen.cs:270-287` | attribute validation | moved to `backend/Resolver/TypeChecker.cs` as source-location diagnostics |

### 4. Partial / broken generation

| Area | Problem | Notes |
|------|---------|-------|
| `for`/`while`/`do while`/`times` | `EnumerateDF` skips loop bodies | known issue from `docs/analysis/01-architecture.md` and `03-ast-core.md` |
| `try`/`catch` | catch clause uses `funcTypeDef` as a stand-in for `catch(type name)` | works for easy cases but is semantically wrong; catch-all `catch()` is special-cased |
| `VisitScopedExpr` (`VExpr.cs:30`) | does not visit the scoped expression body | `expr` field left commented out |
| `VisitLambdaExpr` (`VExpr.cs:353`) | annotated as "probably one big hack" | scope push/pop may be wrong, return-type inference is ad-hoc |
| `Discard` expression | `Gen` is not implemented | `_` is currently a parser-level marker only |

### 5. Categories of missing overrides

#### Covered by private helpers (not currently a bug)

These rules are not overridden, but a helper method handles them when reached from a parent context.

- `arg`, `args`, `funcCall`, `indexCall` via `VisitArgs`, `VisitFuncCall`, `VisitIndexCall` in `VExt.cs`.
- `catchClause` handled inline in `VisitStmtTryCatch`.
- `caseBlock`, `defaultBlock` via private `VisitCaseBlock` / `VisitDefaultBlock` in `VStmt.cs`.
- `condThen` via `VisitCondThen` in `VStmt.cs`.
- `idAccessor`, `idAccessors`, `idExpr`, `idExprs` inline in `VisitDefVar` and other variable construction paths.
- `tplArgs`, `tplParams` via `VisitTplArgs` / `VisitTplParams` in `VTpl.cs`.
- `typespecsNested` and the `Typespec*` family via `new` helpers in `VTypes.cs`.

#### Top-level wrappers that fall through to child rules

These are missing an override, but the generated base visitor will walk children and hit an implemented rule (for example `declNamespace -> defNamespace`). They are fragile but not currently dropping code.

`declAlias`, `declAspect`, `declConcept`, `declConvert`, `declCtor`, `declDtor`, `declEnum`, `declNamespace`, `declOp`, `declUsing`, `defDecl`, `defStmt`, `module`, `imports`, `importName`, `prog`.

#### Leaf / operator token wrappers (safe to leave missing)

These rules only return the token type/text and are read by their parent contexts.

`addOP`, `aggrAssignOP`, `andOP`, `assignOP`, `cmpOp`, `equalOP`, `memAccOP`, `memAccPtrOP`, `multOP`, `nulCoalOP`, `orOP`, `postOP`, `powOP`, `preOP`, `relOP`, `shiftOP`, `binaryType`, `charType`, `floatingType`, `signedIntType`, `specialType`, `unsignIntType`, `kindOfFunc`, `kindOfPassing`, `kindOfStruct`, `kindOfVar`, `qual`, `comment`, `attrib`, `attribId`.

## Recommended action items

The following items can be done one at a time, mostly localized to the visitor layer with corresponding generator entries when needed.

1. **Fail loudly on deferred features.**
   - Add `throw new NotSupportedException("aspect is not supported; see REASONS.md")` to `VisitDefAspect`, `VisitDefConcept`, `VisitDefConvert`.

2. **Plug the silent statement holes.**
   - Add overrides or dispatch rules for `stmtDefer`, `stmtReturnIf` that throw `NotImplementedException` or generate correct C++.

3. **Plug the silent expression holes.**
   - Add overrides for `RangeExpr`, `ThrowExpr`, and `ThreeWayConditionalExpr` that throw `NotImplementedException` until they are designed.

4. **Improve unhelpful exception messages.**
   - `Core/Expr.cs:208` `Discard.Gen` should say what node failed.
   - `VStmt.cs:259` should mention the exact number of catch parameters found.
   - All empty `throw new NotImplementedException()` should get a message.

5. **Fix the `try`/`catch` grammar.**
   - Change `catchClause` from `CATCH funcTypeDef? stmt` to `CATCH LPAREN param? RPAREN stmt`.
   - Use `VisitParam` or equivalent instead of `VisitFuncTypeDef` in `VStmt.VisitStmtTryCatch`.

6. **Implement or guard reachable missing features.**
   - named arguments (`Arg.Gen`)
   - null-coalescing function calls (`FuncCall.Gen`)
   - copy-cast (`VisitPreExpr`)
   - discard expression (`Discard.Gen`)

7. **Fix broken AST traversal.**
   - Repair `ForStmt`, `WhileStmt`, `DoWhileStmt`, `TimesStmt.EnumerateDF` so loop bodies are included (see `docs/analysis/03-ast-core.md`).

8. **Complete `VisitScopedExpr`.**
   - Decide whether a scoped expression should carry an expression child or be represented differently, then restore the commented-out `expr` visitor call.

9. **Audit helper coverage once the above is done.**
   - After the missing overrides above are added or made to throw, re-run the comparison script and ensure no statement/expression construct is still silently dropped.

## Appendix: comparison script

The commands used to build this list can be rerun after changes:

```bash
# generated visitor interface methods
grep -oP "Result Visit\K[A-Za-z0-9_]+(?=\()" \
  backend/obj/Debug/net6.0/MyllParserVisitor.cs | sort -u > interface.txt

# public override implementations
grep -rohP "public override .*? Visit\K[A-Za-z0-9_]+" \
  backend/Visitor/*.cs | sort -u > overrides.txt

# new helper methods
grep -rohP "public new [^\(]+Visit\K[A-Za-z0-9_]+" \
  backend/Visitor/*.cs | sort -u > newhelpers.txt

# extension helpers (manual PascalCase conversion needed)
# combined lowercase set, then compare
```
