# Rebase notes: local commits vs. origin/master

We created a branch `upstream-ideas` from `origin/master` and inspected the local commits one by one.
The upstream branch has full name resolution, a TypeChecker, and several refactors, so not every local change is still needed.
This file records what should be carried forward, what is already covered, and what should be dropped.

## Commit `b066d7a`: namespace wrapping, base-clause formatting and alias support

### Already covered by origin/master

- **Class-based `ls.myll`**: upstream already rewrote `frontend/apps/unix/ls.myll` to use a `DirEntry` class.
- **Base-clause formatting tokens**: upstream `StmtFormatting.StructFormat` already has slots 1 and 2 for the first and subsequent bases, with placeholders for access and `virtual`.
- **Type aliases via `alias`**: upstream supports `alias MyInt = int;` and emits `using MyInt = int;`.
- **Namespace `using` distinction**: upstream changed `UsingDecl` to include `IsNamespaceUsing`, and `HierarchicalGen.AddUsing` emits either a type alias or a namespace using from the same `UsingFormat` array.
- **Manual AddVar validation**: upstream removed the ad-hoc duplicate-name and attribute-combination checks from `HierarchicalGen` and moved that responsibility into the resolver / TypeChecker.

### Not needed anymore

- **Namespace wrapping for ctors/dtors**: upstream uses `FullyQualifiedName` for out-of-line definitions, so the explicit `namespace X { ... }` block around structor implementations is unnecessary.
- **`KnownNamespaces` table in `Symbol.cs`**: the hardcoded table itself is no longer needed because upstream now has name resolution. The namespace-alias feature has been reimplemented on `upstream-ideas` using `UsingDecl.IsAlias`, resolver-side `importedNames`, and a dedicated `namespace X = Y;` generator format.
- **Trailing return types in `FuncFormat`**: upstream reverted to the classic return-type-and-name form (`retType.Gen(name)`) and moved away from the trailing-arrow style. We can revisit this later if dependent return types need it, but it is not required for the current feature set.

### Still worth carrying forward

- **`MYLL_CXXFLAGS` / `CXXFLAGS` forwarding** in `frontend/Program.cs`: upstream still builds `cxxFlags` only from debug/optimization options. Forwarding environment flags is a small, low-risk addition that has no upstream equivalent.
- **Namespace aliases via `alias`**: implemented on `upstream-ideas`. Type aliases (`alias MyInt = int;`) continue to emit `using MyInt = int;`; namespace aliases (`alias s = std;`) now emit `namespace s = std;`. The `testing/cases/alias` integration test has been added.

### Could be added but is optional

- **`testing/cases/alias` integration test**: upstream has the `alias` grammar and visitor, but no alias test case. The local test could be ported as-is if we want coverage for it.

## Commit `0a22c2f`: per-base inheritance access specifiers and virtual bases

This is **not** in upstream.
The feature is still valuable, but the implementation must be adapted:

- Upstream `Structural.basetypes` is `List<TypespecNested>`.
Our local version changed it to `List<BaseType>` so each base can carry an access specifier and a virtual flag.
That change needs to be reapplied, and any resolver code that reads `basetypes` must be updated to go through `BaseType.type`.
- Upstream already has the right `StructFormat` slots for prefix/name, so the generator-side change is small.
- The `bases` integration test is not upstream and should be kept.

## Commit `d73f14d`: vars/fields now init unless told to not do

This is **not** in upstream either.
The behavior is still desired, but it must be reimplemented on top of the refactored `AddVar`:

- Upstream `AddVar` no longer contains the manual validation block, so the `noinit` / `uninit` const/compile-time checks should live in the TypeChecker rather than the generator.
- The core idea stays the same: `var T name;` → `T name{};`, with `[noinit]` / `[uninit]` opting out.
- The `default_init` test case is not upstream and should be kept.

## Recommended path

1. Stay on a fresh branch from `origin/master`.
2. Cherry-pick or reapply only the environment-flags change from `frontend/Program.cs`.
3. Reimplement per-base access/virtual bases and default value-init on top of upstream.
4. Port the `bases` and `default_init` test cases. The `alias` test has already been ported on `upstream-ideas` and covers both type and namespace aliases at module scope.
