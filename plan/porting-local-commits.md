# Porting the local commits to origin/master

We are working on branch `upstream-ideas`, which is `origin/master` plus the work done in this session.
The goal is to keep the useful pieces from the three local commits on `master` and drop everything that upstream already covers.

## Current state

All tests pass on `upstream-ideas`:

```bash
dotnet test testing/ -v quiet
# 60 / 60 passed
```

### Already ported

- **Namespace aliases** (from `b066d7a`). ✅ Done
  - New `AliasDecl` and `AliasStmt` AST nodes.
  - Dedicated `HierarchicalGen.AddAlias`.
  - Resolver pass `ResolveAliases` marks namespace vs. type aliases and registers the alias name.
  - Added `testing/cases/alias/` with type and namespace alias coverage.
- **Base-clause formatting tokens** (from `b066d7a`). ✅ Done
  - Enum and structural base clauses now use `StructFormat` consistently.
  - The `using base = Type;` pseudo-alias now uses `AliasFormat[0]` after the alias/using split.

### Still to port

1. **Per-base inheritance access specifiers and virtual bases** (`0a22c2f`). ✅ Done
   - `BaseType` AST node with access and virtual flag.
   - Grammar `baseSpec`/`baseSpecs` with optional attribute block.
   - Generator composes base prefixes (`virtual public A`, `private B`, etc.).
   - Ported `testing/cases/bases/` integration test.

2. **Default value-init with `[noinit]` / `[uninit]` opt-out** (`d73f14d`). ✅ Done
   - `VarStmt.Gen` and `HierarchicalGen.AddVar` value-initialize variables and fields unless they have an explicit initializer or `[noinit]`/`[uninit]`.
   - Invalid combinations (`const`/`[ct]` + `[noinit]`) throw.
   - Regenerated golden files for affected tests: `class`, `class_ctor`, `access_mods`, `overload`, `gol`, `cpp_fail`.
   - Ported `testing/cases/default_init/` integration test and golden files.
   - Updated AGENTS.md, language spec, declarations doc, test coverage inventory, and TODO plan.
   - Core behavior: `var T name;` → `T name{};` unless `[noinit]` is set.
   - Upstream `AddVar` no longer contains manual validation, so the `[noinit]` const/ct checks should move to the TypeChecker.
   - Add `VarEmptyInitFormat` / `VarDirectInitFormat` handling to the generator.
   - Port `testing/cases/default_init/` integration test and refresh affected golden files.

3. **`MYLL_CXXFLAGS` / `CXXFLAGS` forwarding** (`b066d7a`). ✅ Done
   - `frontend/Program.cs` now reads `MYLL_CXXFLAGS` or `CXXFLAGS` and appends them to `cxxFlags`.

### Dropped from `b066d7a`

- `KnownNamespaces` hardcoded table — replaced by resolver-based namespace detection.
- Namespace wrapping for ctors/dtors — upstream uses `FullyQualifiedName`.
- Trailing return types — upstream reverted to classic return type; not needed.
- Class-based `ls.myll` — already present upstream.


## Files currently changed on `upstream-ideas`

Tracked modifications:

```
backend/CompilationContext.cs
backend/Core/Decl.cs
backend/Core/Stmt.cs
backend/Generator/HierachicalGen.cs
backend/Generator/StmtFormatting.cs
backend/Resolver/Resolver.cs
backend/Resolver/Unresolved.cs
backend/Visitor/VDecl.cs
backend/Visitor/VStmt.cs
```

New files:

```
plan/rebase-notes.md
plan/porting-local-commits.md
testing/cases/alias/main.myll
testing/golden/alias/main.cpp
testing/golden/alias/main.hpp
```

Untracked local file (Rider/.NET SDK related):

```
global.json
```

## How to submit

The local `master` branch is still 3 commits ahead of `origin/master`, so the plan is to eventually make `upstream-ideas` the new feature branch and push it.

1. Review the tracked changes:
   ```bash
   git diff --stat
   git diff backend/...
   ```

2. Decide whether `global.json` should be kept. It was created when Rider installed the .NET 10 SDK and tells the SDK to roll forward. It is probably worth keeping, but it is not strictly part of the port.

3. Commit the work so far as one focused commit:
   ```bash
   git add backend/ testing/cases/alias/ testing/golden/alias/ plan/rebase-notes.md plan/porting-local-commits.md
   git commit -m "feat: namespace aliases, base-clause formatting, and CXXFLAGS forwarding"
   ```

4. Ported all three features as separate commits on `upstream-ideas`.

5. Before submitting, run:
   ```bash
   dotnet build myll.sln
   dotnet test testing/
   ```

6. Do not push until explicit confirmation.
