# REASONS.md

Decision log for Myll. Captures the "why" behind choices so we don't repeat debates.

## 2026-06-08 — Default C++ Includes Fix

### Problem
Generated C++ headers failed to compile: `std::int8_t`, `std::uint64_t`, `std::byte` were all
undefined because the required `<cstdint>` and `<cstddef>` headers were never included.

### What Changed
Added to `DefaultIncludes` in `backend/Generator/StmtFormatting.cs`:
- `#include <cstddef>` — provides `std::byte`, `size_t`, `nullptr_t`
- `#include <cstdint>` — provides `std::int8_t`, `std::uint64_t`, etc.
- `#include <string>` — provides `std::string` (used for the `string` keyword)

All includes sorted alphabetically.

### What Was Deliberately Excluded
- `<iostream>` — being replaced by `std::print` (C++26). Not worth bundling.
- `<vector>` — user may replace with custom container. Opt-in via `import std_vector`.
- `<map>` — custom containers planned. Opt-in via `import std_map`.

### Possibly Added Later
- `<algorithm>` — reimplementing is a lot of work, may stick with std. Still undecided.

### Why Keep `std::` Prefix on Fixed-Width Ints
Types like `std::int8_t` in `StmtFormatting.cs` are left as-is. C++ guarantees that
`<cstdint>` provides these in `std::` namespace. Myll targets C++ only (no C mode),
so `int8_t` in global namespace is irrelevant.

## 2026-06-10 — Intentionally Unimplemented Features

### Problem
Several keywords exist in the grammar and parser but have no stable design yet.
Accidentally implementing them now would lock in bad decisions and create
migration pain when the real design emerges.

### What Is Intentionally Unimplemented
| Feature | Grammar Status | Why Not Implemented |
|---------|----------------|---------------------|
| `aspect` | Parsed, visitor stub | Concept not finalized |
| `concept` | Parsed, visitor stub | Concept not finalized |
| `defer` | Parsed, no visitor | Needs ownership/borrowing semantics first |
| `convert` | Parsed, visitor stub | Syntax direction (`->` vs `<-`) undecided |

### What Changed
No code changes. These remain as `throw new NotImplementedException` in the
visitor, but now with an explicit comment: `// RESERVED: do not implement`.

### Where to Track
See `AGENTS.md` → "Planned Work" for the active priority list.
This file (`REASONS.md`) remains the decision log.
