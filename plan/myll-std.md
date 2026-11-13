# Myll Standard Library — v1

Status: **design settled 2026-11-14** (big-bang pass, decisions below); implementation in progress.

## Goals

- Give Myll programs a native container/utility layer they reach for before `std::` bindings.
- Dogfood the language on itself: every type is pure Myll in `myll/`, no generated binding code.
- Grow the compiler: gaps found while building go to bug docs and get fixed, not worked around silently.

## Posture

- Pure Myll implementations in `myll/`, one module per family, `namespace Myll`. `std::` bindings stay the escape hatch (the compiler keeps C++ interop untouched).
- Semantics mirror C++ value types: copyable and movable classes with explicit ctors; no reference-semantics surprises.
- No exceptions in v1: `Result` carries errors as values. When the `except` axis lands (`plan/except-noexcept.md`), `Result` coexists with it — `Result` for recoverable, `throw` for the exceptional path.

## Layout and naming

- One module per type family: `myll_option`, `myll_result`, `myll_rng`, `myll_hash`, `myll_string`, `myll_map`, `myll_set`, `myll_dyn_array` (existing, extended).
- Types are PascalCase (`Map`, `StringView`); methods are PascalCase (`pushBack`); private fields are lowerCamel with a `_` prefix (`_size`), in a `field { ... }` block. This follows the existing `DynArray` conventions.
- Sizes and indices are `usize`. Hash results are `u64`.
- Smart arrays (`T[]!`) back all storage; raw `T*` only where C++ ABI demands it.

## Policies

- **Value semantics**: every container is deep-copy on copy, move on move, with the matching assignment operators. Classes declare the `[rule_of_3]` / `[rule_of_5]` attributes where the checker allows, so the compiler enforces the rule on each class.
- **Bounds**: no checked bounds in v1 (matching C++ `std::` semantics); unchecked access stays `operator[]`, `at()` matches `std::` `at` spelling (also unchecked for now).
- **Iteration**: explicit `Iterator` classes following the `DynArray` model (`begin()`/`end()`, `++`, `*`, `==`, `!=`). There are no concepts/aspects (`REASONS.md`), so generic algorithms over containers are not possible in v1; iteration plus raw-pointer spans cover the common cases. `std::` algorithm bindings keep working on raw data.
- **Missing-key reads** return raw nullable pointers (`get(k) -> V*`, null when absent) plus `getOr` fallbacks. `Option<T>` exists for value-shaped results (see below); the raw-pointer form exists because `Option<T&>`/`Option<V&>` (references as template arguments) is not settled.

## Hash protocol (myll_hash)

- `func hash( K key ) -> u64` is a free function, overloaded per key type.
- `myll_hash` ships overloads for the built-in scalars (identity for integers) and `StringView` (FNV-1a over the bytes).
- `Map`/`Set` bodies call `hash(key)` unqualified; resolution is finished by C++ at instantiation (same mechanism as the external templated algorithms, `testing/cases/external_algorithms/`).
- User key types: define `func hash( MyType x ) -> u64` next to the type (same namespace, found by ADL). Documented requirement for now.

## Modules

### myll_option — `Option<T>`

- Storage: `T _value; bool _has;` (class, not union — tagged unions are not expressible yet; see Deferred).
- `ctor()` is none; `ctor(T)` is some; copy and move ctors; `operator move =`.
- `has() -> bool`, `get() -> T&`, `getOr( T fallback ) -> T`, `set(T)` and `reset()` (both `[chain]`).
- `[static]` factories `some(T)` / `none()` when static methods are available.

### myll_result — `Result<T,E>`

- Storage: `T _value; E _error; bool _ok;`.
- Factories `ok(T)` and `fail(E)` (static), copy/move ctors, `operator move =`.
- `isOk()`, `get() -> T&`, `error() -> E&`, `getOr(T)`, `errorOr(E)`.
- No exceptions thrown by the library in v1.

### myll_rng — `Rng`

- xorshift64, `u64 _state`, `ctor(u64 seed)`.
- `next() -> u64`, `nextU32() -> u32`, `nextF32() -> f32` (normalized), `range(min, max) -> usize` inclusive on both ends.
- No seeding from the clock in v1 (callers seed via `std::chrono`); distributions beyond `range` are deferred.

### myll_dyn_array — `DynArray<T>` (extend existing)

- Additions: `insert(usize index, const T&)` and `erase(usize)` (both `[chain]`, shifting), `remove(const T&) -> bool` (first match), `indexOf(const T&) -> Option<usize>` (first match, none when absent), `contains(const T&) -> bool`.
- `myll_dyn_array` imports `myll_option` for `indexOf`; the dependency is one-way.

### myll_string — `String` + `StringView`

- `String`: `char[]! _data; usize _len; usize _capacity;`, growth by doubling.
- Constructors: `ctor()` empty, `ctor(StringView)`, `ctor move`, copy ctor (deep); `operator move =`, `operator == / != / <` against `StringView`.
- Content: `len()`, `isEmpty()`, `at(usize) -> char&`, `operator[](usize) -> char&`, `pushBack(char)`, `append(StringView)` / `append(String)`, `fill(char, usize)`.
- Search: `find(char/StringView) -> Option<usize>`, `contains(...) -> bool`, `startsWith(StringView) -> bool`, `endsWith(StringView) -> bool`.
- Slices: `view() -> StringView`, `substring(usize start, usize end) -> String` (end exclusive).
- Iteration: a `char` Iterator over `begin()`/`end()`.
- `StringView`: struct over `const char[*] _data; usize _len;` with `len()`, `at()`, `operator[]`, `equals(StringView)`, `startsWith`, `contains`, plus a ctor from a null-terminated `const char*` (length via `strlen`, see below).
- Null-terminated literals: add `std/c_string.decl.myll` binding `strlen`; the view ctor uses it. No other C-string functions in v1.

### myll_map — `Map<K,V>` (hash table)

- Open addressing with linear probing; parallel arrays `K[]! _keys`, `V[]! _values`, `u8[]! _state` (empty/live/tombstone), power-of-two capacity, rehash at load factor 0.75.
- `put(K,V)` `[chain]` (insert or overwrite), `get(K) -> V*` (null when absent), `getOr(K, V)`, `has(K) -> bool`, `remove(K) -> bool`, `size()`, `isEmpty()`, `clear()` `[chain]`, `keys() -> DynArray<K>`, `values() -> DynArray<V>`.
- Key requirements (documented): K is default-constructible, has `operator ==`, and a visible `hash` (see the hash protocol).
- Copy semantics like the other containers; `operator move =` plus copy.

### myll_set — `Set<K>`

- Same hash core without the values array: `add(K) -> bool` (false when present), `has(K)`, `remove(K) -> bool`, `size()`, `isEmpty()`, `clear()` `[chain]`, `values() -> DynArray<K>`.

## Testing

- `testing/cases/myll_std/`: one case, `main.my` plus per-module check functions (`runOption`, `runResult`, `runRng`, `runString`, `runMap`, `runSet`, `runDynArray`), run depth, golden baseline.
- Every fix to the compiler that a module needs lands with a regression check inside the same case where possible.
- Bugs found but not fixed during the milestone get `plan/bug-*.md` entries.

## Consumer milestone

- Port one coreutil onto the library: `wc` (chars/words/lines over `String` reads) or `grep` (lines, `contains`, `Map` of matches). Pick for string coverage at execution time — leaning `grep`.
- The port must keep the same CLI behavior (golden output parity with the current implementation).

## Deferred (v2+)

- Tagged-union storage for `Option` (needs language support), `Option<T&>`.
- `Result` monadic combinators and the interaction with the `except` axis.
- Hash specialization for `String` keys via hashing hooks other than ADL.
- Ordered map/set, iteration order guarantees, `Map`/`Set` iterators.
- `StringBuilder`, UTF-8 views, `Rng` distributions, algorithm family over iterators.

## Risks

- Static factory methods (`[static]` method returning the enclosing type) are unproven; if unsupported, factories become plain ctors (`ctor okFrom(T)`-style) or free functions.
- Unqualified `hash(k)` inside template bodies rides the external-algorithm precedent; if resolution proves flaky, `Map` falls back to a `ctor( hasherFunc )` injection for v1 and the protocol is revisited.
- Reference-typed template arguments (`Option<T&>`) and `const` member reads of smart arrays may surface compiler gaps; file bugs, keep the library moving.