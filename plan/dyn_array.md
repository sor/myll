# DynArray<T> implementation plan

## Goal

Build a realistic Myll standard library container `Myll::DynArray<T>` (similar to `std::vector`) incrementally, adding features and tests phase by phase. Each phase runs through `dotnet test testing/` so we discover what works and what needs workarounds.

## Location

- Development/test directory: `testing/cases/dyn_array/`
- Final home: `myll/dyn_array.myll` under the `Myll` namespace.
- Class name: `DynArray`
- Module name: `dyn_array`
- Method casing: camelCase (`pushBack`, `isEmpty`, `at`, etc.)
- Data member: `T[]!` (`std::unique_ptr<T[]>`)
- Naming rule: boolean `[pure]` methods start with `is` or `has` (e.g. `isEmpty`, not `empty`).
- Iterator: inner class `Iterator` inside `DynArray<T>`.

## Phases

### Phase 1 — Minimal container

Files: `myll/dyn_array.myll` for the library, `testing/cases/dyn_array/main.myll` for tests.

Features:
- `class DynArray<T>` with `_data: T[]!`, `_size: usize`, `_capacity: usize`.
- Default constructor delegating to a capacity constructor.
- Destructor (automatic via `T[]!`).
- `pushBack(const T& value)` and internal `grow()`.
- `reserve(usize capacity)`.
- `[pure] method size() -> usize`.
- `[pure] method isEmpty() -> bool`.
- `method at(usize index) -> T&`.
- `operator [](usize index) -> T&`.
- `method front() -> T&`, `method back() -> T&`, `method top() -> T&` (acts as a stack).

Tests:
- Push integers, verify `size`.
- Read and modify through `at()` and `operator []`.
- Trigger growth by pushing many elements.
- Test with a simple non-POD `struct`.

### Phase 2 — Chaining and helpers

Features:
- `[chain]` on `pushBack`, `reserve`, `clear`, `popBack`, `shrinkToFit`.
- `popBack()`.
- `back()` / `front()` returning `T&`.
- `clear()`.
- `shrinkToFit()`.

Tests:
- Chain pushes.
- Pop, clear, front/back/top.
- Verify no leaks/memory corruption (valgrind later, if available).

### Phase 3/5 — Inner iterator and operators

Features:
- Inner `class Iterator` storing `T*`.
- `begin()` and `end()` returning `Iterator` (pointer one-past-last).
- Iterator named methods:
  - `next() -> Iterator&`
  - `value() -> T&`
  - `equals(Iterator other) -> bool`
- Iterator operators:
  - `operator ++ ()` (prefix)
  - `operator * ()`
  - `operator == (Iterator other)`
  - `operator != (Iterator other)`

Tests:
- Manual iteration with `while` and named methods.
- Sum elements via operator-based iteration.
- Compare `begin`/`end`.

### Phase 4 — Move to `myll/` namespace

- Create `myll/dyn_array.myll`.
- Wrap `DynArray` in `namespace Myll { ... }`.
- Add auto-discovery of `myll/*.myll` in `frontend/Program.cs` so `import dyn_array` works from any case.
- Update tests to `import dyn_array` and use `Myll::DynArray<T>`.
- Ensure the module compiles both standalone and as part of an app.

## Known blockers and workarounds

- Copy/move semantics and assignment shorthand are unreliable inside templates; defer copy constructor / copy assignment to a later phase and test carefully.
- Move/forward support is shaky; keep `pushBack` taking `const T&` until value categories are better supported.
- `const` overloads (e.g. `at() const`) deferred until Myll const-correctness is better understood.
- Bounds checking / `assert` deferred; precondition attributes may not generate yet.

## String-literal operator bug

Resolved. Bare operator syntax (`operator +`, `operator []`, `operator ++`, etc.) is now supported; string-literal operator definitions are no longer accepted.
