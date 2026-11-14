# Myll Standard Library — v1

Status: **implemented 2026-11-14** (design settled in the same pass); the
notes below describe the shipped v1. Deviations from the original design
are marked inline.

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
- `myll_hash` ships overloads for the built-in scalars (identity for
  integers) and `StringView`/`String` (FNV-1a over the bytes).
- `Map`/`Set` bodies call `hash(key)` unqualified; overload resolution with
  dependent arguments defers to C++ (`Resolver.TryResolveOverload` leaves
  the call unresolved, the emission writes the raw name), so instantiation
  picks the concrete overload through ordinary lookup plus ADL.
- User key types: define `func hash( MyType x ) -> u64` next to the type
  (its namespace; found by ADL) and an `operator ==` overload on the type.
  The built-in overloads are found through the definition context
  (`myll_map.hpp` includes `myll_hash.hpp`), so no `Myll::` qualification
  is needed from users.

## Modules

### myll_option — `Option<T>`

- Storage: `T _value; bool _has;` (class, not union — tagged unions are not expressible yet; see Deferred).
- `ctor()` is none; `ctor(T)` is some; copy and move ctors; `operator move =` and `operator =`.
- `has()`, `value() -> T&`, `valueOr(T)`, `put(T)` and `reset()` (both `[chain]`).
- `[static]` factories `some(T)` / `none()` (shipped; `[static] method` works).

### myll_result — `Result<T,E>`

- Storage: `T _value; E _error; bool _ok;`.
- Factories `ok(T)` and `fail(E)` (static), copy/move ctors, `operator move =`, `operator =`.
- `isOk()`, `isFail()`, `value() -> T&`, `error() -> E&`, `valueOr(T)`, `errorOr(E)`.
- No exceptions thrown by the library in v1. Same-type `Result<int, int>`
  works because only the static factories construct values (plain ctor
  overloads would be ambiguous there).

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
- `put(K,V)` `[chain]` (insert or overwrite), `at(K) -> V*` (null when absent; `get`/`set` are lexer keywords), `getOr(K, V)`, `has(K) -> bool`, `remove(K) -> bool`, `size()`, `isEmpty()`, `clear()` `[chain]`, `keys() -> DynArray<K>`, `values() -> DynArray<V>`.
- Key requirements (documented): K is default-constructible, has `operator ==`, and a visible `hash` (see the hash protocol).
- Copy semantics like the other containers; `operator move =` plus copy.

### myll_set — `Set<K>`

- Same hash core without the values array: `add(K) -> bool` (false when present), `has(K)`, `remove(K) -> bool`, `size()`, `isEmpty()`, `clear()` `[chain]`, `values() -> DynArray<K>`.

## Testing

- `testing/cases/myll_std/` (module `std_lib` — the `myll_` prefix is
  reserved for compiler-owned modules): per-module check functions
  (`runOption` … `runSet`), run depth, golden baseline.
- Bugs found but not fixed during the milestone get `plan/bug-*.md` entries.

## Consumer milestone

- **Done**: `grep` ported onto the library (`frontend/coreutil/coreutil_grep.myll`).
  The matching core runs over `Myll::StringView` (zero-copy line views via
  `std::string::c_str()` + length; the pattern is a `Myll::String` viewed
  once per stream). CLI behavior is unchanged (spot-checked against the
  previous implementation: `-i`, `-c`, `-x`, `-v`, `-n`).
- The coreutil `Makefile` links the generated `myll_*.cpp` files and
  regenerates when `myll/` or `std/` sources change.

## Compiler changes the library needed

These shipped with the library; they are the "grow the compiler" part of
the goal.

- `GetAllTemplateParams` reversed the per-class parameter list (only
  single-parameter classes hid it) — levels now reverse, per-level order is
  kept.
- `T*` now converts to `T[*]` (both lower to the same C++ type); the decay
  chain accepts both directions.
- `myll/` auto-discovery follows transitive imports (myll_dyn_array imports
  myll_option).
- Overload resolution with dependent arguments defers to C++: the ranking
  skips dependent positions, and hard rejections or ties on dependent calls
  leave the call unresolved so the emitted raw name re-runs overload
  resolution at instantiation (ADL included). This is the mechanism the
  hash protocol rides on.
- `std::string::c_str()` binding (`std/std_string.decl.myll`).

## Deferred (v2+)

- Tagged-union storage for `Option` (needs language support), `Option<T&>`.
- `Result` monadic combinators and the interaction with the `except` axis.
- Hash specialization hooks other than ADL.
- Ordered map/set, iteration order guarantees, `Map`/`Set` iterators,
  `[rule_of_3]`/`[rule_of_5]` attribute wiring on the containers.
- `StringBuilder`, UTF-8 view, `Rng` distributions, algorithm family over
  iterators, `String` interop with `std::unordered_map`.

## Risks (realized)

- `[static]` factories work; `get`/`set`/`copy` are lexer keywords, so
  `Option.get`/`Map.get` became `value()`/`at()`/`put()`.
- Unqualified `hash(k)` inside template bodies needed the dependent-call
  deferral (see above); the emission stays unqualified, so ADL applies.
- The `[ct]` free-function hash overloads emitted mismatched constexpr
  declarations across hpp/cpp; the scalar hashes are plain functions.