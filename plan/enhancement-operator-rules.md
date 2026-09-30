# Enhancement: consolidate operator classification into OperatorRules

Status: **open**. Found in the 2026-09-29 review.

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
