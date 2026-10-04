# Enhancement: consolidate operator classification into OperatorRules

Status: **done 2026-10-04** (pending review). Behavior-preserving; the golden suite stayed
byte-identical throughout.

## What was done

1. `Operand` cleanup: the `_Begin`/`_End` range stubs are removed (their only live consumers were
   two dead `isPreOp`/`isPostOp` locals in `UnOp.Gen`, deleted with them; `PrecedenceLevel` anchors
   re-anchored on the first real member of each precedence level, keeping every auto-filled value
   identical). The blank lines keep the precedence-level grouping visible in the enum. `Operand.Assign`
   was added after `Conditional` (assignment below conditional; auto-fills 160 like the Parens/Ids/
   Literal leaves) and `Parser.ASSIGN` now maps to it, so `=` can never silently validate as `==`.
2. Classification consolidated into `OperatorRules` (one canonical place): `IsMemberAccess`,
   `IsArithmetic`, `IsBitOperator`, `IsBitwise`, `IsShift`, `IsComparison`, `IsLogical`, and
   `SameBitFamily` (the pointer-checking implementation from the resolver; the checker's
   pointer-blind twin is gone). TypeChecker, TypeResolver, and Resolver call the shared helpers.
   All helpers use the `or` pattern throughout.
3. Dead assignment branches removed: `IsAssignmentOperation` plus its `ResolveBinOp` arm, and the
   `ExprStmt` BinOp-Equal arm in the type checker. Statements are always `MultiAssign`, so a BinOp
   assignment cannot occur today; if the grammar ever yields assignment expressions, `Operand.Assign`
   now makes that a loud unknown-operator failure instead of silent `==` validation.

## Follow-up commit: `%` on bit types is equivalence (2026-10-04)

Bit types are bags of bits with set semantics, forming a coherent set algebra: `+` union (`a | b`),
`-` difference (`a & ~b`), `*` intersection (`a & b`), `^` symmetric difference, `/` implication /
subset (`~a | b`, tentative), and now `%` equivalence (`~(a ^ b)`, set equality) — so `/` reads
`a ≤ b`, `%` reads `a == b`, `^` reads `a != b`. `IsBitOperator` includes `Modulo`; the generator
emits `static_cast<T>( ~(a ^ b) )` (the negation promotes to int, hence the cast, like the
Subtract/Divide negations). Non-bit types keep plain integer modulo.

This also fixed the bit-operator validation position: the checker's `IsBitOperand` guard now
resolves through the type resolver instead of reading possibly-unmemoized `expr.Type`, so bit
operators validate in initializer positions too (`var b8 eq = a % b;`), not only where a prior
resolution populated the operand types. The bint case covers both positions.

## Reference: C++ `??` null coalescing

Myll's `NullCoalesce` sits at 155, above `Or` (150) and below `Conditional` (160) — the C# `??`
placement (between the logical operators and the conditional). For the C++ side, searching "??" is
not reliable, so: as of 2026-10, no WG21 paper with "coalescing" in its title appears in the paper
indices for 2024, 2025, or 2026 (checked via https://www.open-std.org/jtc1/sc22/wg21/docs/papers/
— follow the year). If a concrete proposal number turns up, note it here.

## Problem

Operator classification logic is duplicated across three files:

- `IsBitOperator` is textually identical in `backend/Resolver/TypeResolver.cs:807-811` and
  `backend/Resolver/TypeChecker.cs:810-814`.
- `SameBitFamily` exists twice with **subtly different implementations**:
  `TypeResolver.cs:782-795` (goes through `OperatorRules`) vs `TypeChecker.cs:940-951` (inline
  pattern matching).
- The six-member "is a member-access operator" list is written out four times
  (`TypeResolver.cs:288-289, 833-834`, `TypeChecker.cs:200-203`, + two more).

Dead branches from a shared token: `EQ` and `ASSIGN` both map to `Operand.Equal`
(`backend/Visitor/VExt.cs:157,161`). Statements are always `MultiAssign`
(`backend/Visitor/VStmt.cs:618-624`), so the `IsAssignmentOperation` branch of `ResolveBinOp`
(`TypeResolver.cs:288-289, 833-834`) and the `ExprStmt` BinOp-Equal branch of the type checker
(`TypeChecker.cs:200-203`) are unreachable. The shared token is a live trap: if the grammar's
`idExpr : id (ASSIGN expr)?` (`backend/Grammar/MyllParser.g4:420`) ever yields assignment
expressions, `=` would be silently validated as `==` and typed as `bool`.

## Fix plan

1. Move `IsBitOperator`, `SameBitFamily` (one implementation), and the member-access-op list into
   `backend/Resolver/OperatorRules.cs` — the file that exists for exactly this.
2. Delete the duplicates and the dead assignment branches.
3. Give `ASSIGN` its own `Operand` value so `=` can never mean `==` again.

## Verification

- `dotnet test testing/` green (behavior-preserving).
- Grep check: `IsBitOperator` / `SameBitFamily` / the member-access list each appear in exactly one
  file.
- The assignment-statement path (`a = b;`) still type-checks — covered by existing cases.

## Risk

Low. Behavior-preserving consolidation.

Effort: S-M.
