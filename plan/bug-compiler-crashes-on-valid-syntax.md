# Bug: valid syntax crashes the compiler or emits TODO text

Status: **fixed 2026-09-30** (all table rows; the `[flags]` row earlier). Found in the 2026-09-29 review. One repro verified.

## Principle

The compiler must never print a stack trace for user input, and it must never emit placeholder text
into generated C++. Parsed-but-unimplemented constructs need a clean diagnostic
(`not implemented yet: <feature>` with file:line:col), not a crash.

## How it was fixed (no new machinery)

The existing channels were reused — no new helper was introduced:

- **Visitor-stage constructs** (`(copy)`, ranges, throw expressions, three-way conditionals, defer,
  aspect, concept) throw a `DiagnosticException` (the existing mechanism); `VDecl.VisitProgs` catches
  it, drains the scope stack, and records the diagnostic in `Context.Diagnostics` — the frontend
  already bails on those before resolution (and keeps going under `-k` with the partial AST).
- **Generator-stage constructs** (null-coalescing `?:`, `?.` member access, `?.`-calls/indexing,
  pointer-to-member access, multi-initializer `for` headers, named call arguments) throw
  `DiagnosticException` from `BinOp.Gen`/`ForStmt.Gen`/`Arg.Gen`/`FuncCall.Gen`; `ModuleGenerator`
  already catches it. `Arg`/`FuncCall` gained a `srcPos` field (wired in `VExt.VisitArgs`/
  `VisitFuncCall`/`VisitIndexCall`) so these diagnostics carry a real location instead of
  `<unknown>`.
- The `"TODO MAP .*"` / `"TODO MPAP ->*"` format entries were deleted outright.

Tests: `testing/cases/not_implemented_fail/` (visitor-stage, one construct per module) and
`testing/cases/not_implemented_gen_fail/` (generation-stage), both registered in `generateFailing`.

## Findings

### Verified: `[flags]` enum with a non-decimal value crashes

**FIXED 2026-09-30** (see `plan/untyped-constant-followups.md`): `EnumTransformer.NumberFlags`
uses `ConversionRules.TryParseInteger` (which now handles leading signs and all radixes)
and rejects negative / out-of-u32-range literal values with a diagnostic instead of
crashing. Computed values (e.g. `Yellow = Red | Green`) are left untouched and emitted
as-is instead of being silently replaced by the auto-number. The `[flags]` row in the
table below is historical.

`tmp/review/flags_hex.myll`:

```myll
[flags] enum E { A = 0x4, B = 0x8 }
```

`EnumTransformer.PreTransform` calls `uint.Parse( lit.text )`
(`backend/Transform/EnumTransformer.cs:64`, stack trace at `:31`) and throws `FormatException` for
hex, negative, or suffixed values. Fix: use `ConversionRules.TryParseInteger`
(`backend/Resolver/ConversionRules.cs:609`) and emit a diagnostic on failure.

### Parsed constructs with no lowering (crash or nonsense output)

| Construct | Site | Behavior today |
|---|---|---|
| `x ?: y` (null-coalescing binary) | `Operand.NullCoalesce` has no `ToFormat` entry (`backend/Generator/ExprFormatting.cs`) | `KeyNotFoundException` in `GetFormat()` |
| `a?.b` / `a?.(x)` / `a?[i]` | `NCMemberAccess`/`NCMemberAccessPtr` family | `KeyNotFoundException` / `NotImplementedException` in `FuncCall.Gen` (`backend/Core/Mixed.cs:62,80`) |
| `a.*b` / `a->*b` | `Operand.MemberAccessPtr`/`MemberPtrAccessPtr` | mapped to the literal strings `"TODO MAP .*"` / `"TODO MPAP ->*"` (`ExprFormatting.cs:16-17`) and **emitted verbatim into the C++** |
| `(copy)` cast | `backend/Visitor/VExpr.cs:127` | `NotImplementedException` |
| `a...b` range expression | `backend/Visitor/VExpr.cs:550` | `NotImplementedException` |
| `throw e` expression | `backend/Visitor/VExpr.cs:553` | `NotImplementedException` |
| three-way `?? :` | `backend/Visitor/VExpr.cs:556` | `NotImplementedException` |
| multi-statement / multi-init `for` header | `backend/Core/Stmt.cs:369-373` | `NotImplementedException` |
| named call arguments | `backend/Core/Mixed.cs:62` | `NotImplementedException` |
| aspect / concept / defer | `backend/Visitor/VDecl.cs:519,523`, `backend/Visitor/VStmt.cs:319` | raw `NotSupportedException` (REJECTED in `REASONS.md` — a clean diagnostic is still better than a stack trace) |

Named arguments, null-coalescing call, and copy-cast are already tracked as low-priority known work
in `AGENTS.md`; this task only changes *how* they fail.

## Fix plan

1. Replace the `"TODO MAP .*"` / `"TODO MPAP ->*"` format strings with a `DiagnosticException`
   (never emit TODO text as code).
2. Introduce a helper (e.g. `throw NotSupported("not implemented yet: null-coalescing operator")`)
   that produces a `DiagnosticKind.Error` diagnostic with the node's `srcPos` instead of a raw
   `NotImplementedException`, and convert every user-reachable throw site in the table above.
   Keep the exception mechanism for internal invariants.
3. `EnumTransformer`: `TryParseInteger` + diagnostic (see above).

## Verification

- Each table construct produces a single clean diagnostic with `[file:line:col]` and a non-zero exit
  code, no stack trace.
- Add negative cases under `testing/cases/` (extend `typecheck_fail` or add `not_implemented_fail/`)
  covering at least: `?:`, `?.b`, `.*`, `(copy)`, named args, `[flags]` hex enum.
- `dotnet test testing/` green.

## Risk

Low. All changes convert crash paths into diagnostic paths; no successful compilation changes
behavior.
