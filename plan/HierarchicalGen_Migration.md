# Plan: `HierarchicalGen` Migration — Move generation out of Core AST

## Goal

Move all C++ emission code out of the Core AST classes and into a single logical `Generator` class split across focused files.

Source files to clean:

- `backend/Core/Expr.cs` — move all `Gen` overrides.
- `backend/Core/Stmt.cs` — move all `Gen` / `GenWithoutCurly` overrides.
- `backend/Core/Decl.cs` — move all `AddToGen` overrides.
- `backend/Core/Typespec.cs` — move all type-formatting code.
- `backend/Core/Mixed.cs` — move `Gen` methods on `Param`, `Arg`, `FuncCall`, `IdTplArgs`, `TplArg`.

After the migration, `Core` contains only AST data (plus one tiny dispatch bridge). The `Generator` owns `ModuleContext`, the active `Dialect`, output buffers, and all formatting logic.

## Core layout rule

Only *direct descendants* of a node kind live in that file:

- descendants of `Expr` → `Expr.cs`
- descendants of `Stmt` → `Stmt.cs`
- descendants of `Decl` → `Decl.cs`

Everything else — `BaseType`, `CatchClause`, `Param`, `Arg`, `FuncCall`, `IdTplArgs`, `TplArg`, pointer/type helper data — lives in `Mixed.cs`.

## Target file layout

```text
backend/Generator/
  Generator.cs                  # shared state, entry point, output buffers, helpers
  Generator.Hierarchical.cs     # module / namespace / struct / function layout
  Generator.Decl.cs             # Func, VarDecl, Structor, Enum, Alias, etc.
  Generator.Stmt.cs             # statements + statement-only format tables
  Generator.Expr.cs             # expressions + expression-only format tables
  Generator.Type.cs             # Typespec formatting
  Generator.Mixed.cs            # Param, Arg, FuncCall, IdTplArgs, TplArg formatting
```

All files declare `public sealed partial class Generator`. They share state directly; no context is passed between methods.

## Class shape

```csharp
// Generator.cs
public sealed partial class Generator
{
    private readonly ModuleContext context;
    private readonly Dialect dialect;

    private readonly AccessStrings staticFieldDecl = new();
    // ... other output collections from current HierarchicalGen

    public Generator(ModuleContext context)
    {
        this.context = context;
        this.dialect = context.Dialect;
    }

    public GeneratedModuleFilesResult GenerateModule()
    {
        GlobalNamespace module = context.Module!;
        // drive generation through decl dispatch
        module.Accept(this);
        return new(files, diagnostics);
    }
}
```

## Formatting organization

Format strings stay close to their consumer.

- `Generator.Stmt.cs` contains statement format tables.
- `Generator.Expr.cs` contains expression format tables and operand format helpers.
- `Generator.Hierarchical.cs` / `Generator.Decl.cs` contain declaration format tables.
- `Generator.Type.cs` contains type-formatting tables.
- `Generator.Mixed.cs` contains mixed-helper format tables.
- Shared helpers (indent strings, curly helpers used by multiple files) live in `Generator.cs`.

The rule is the same as before: **no inline C++ keywords inside `Generate(...)` methods**. Every format fragment is a named field or helper.

## Dispatch mechanism

### Option A — virtual `Accept` in Core

Each AST node keeps one tiny virtual method:

```csharp
// Core/Expr.cs
public abstract partial class Expr
{
    public abstract string Accept(Generator g);
}

public class BinOp : Expr
{
    public override string Accept(Generator g) => g.Generate(this);
}
```

`Generator` implements typed overloads:

```csharp
// Generator.Expr.cs
public partial class Generator
{
    public string Generate(BinOp e) { ... }
    public string Generate(IdExpr e) { ... }
}
```

Same pattern for `Stmt`, `Decl`, `Typespec`, and `Mixed` helpers.

### Option B — closed switch in `Generator`

Core stays completely free of generator code. `Generator` does runtime matching:

```csharp
public string Generate(Expr e) => e switch {
    BinOp    b => Generate(b),
    IdExpr   i => Generate(i),
    ...
};
```

### Plan

Start with **Option A** for everything. It is the easiest migration and keeps the build green through each phase.

Then implement **Option B for `Expr` only** in a spike branch, run the golden tests, and compare readability and maintainability. Record the decision and apply the winner consistently.

## Step-by-step migration

### Phase 0 — Rename skeleton

1. Rename class `HierarchicalGen` → `Generator`.
2. Rename `backend/Generator/HierachicalGen.cs` → `backend/Generator/Generator.Hierarchical.cs`.
3. Update `frontend/Program.cs` to use `new Generator(context).GenerateModule()`.
4. Update any remaining `HierarchicalGen` references.
5. Build + test. No output changes. Commit.

### Phase 1 — Declarations

1. Create `backend/Generator/Generator.Decl.cs`.
2. Move declaration emission methods from `Generator.Hierarchical.cs`:
   - `AddFunc` → `Generate(Func)`
   - `AddAccessor` → `Generate(Accessor)`
   - `AddVar` → `Generate(VarDecl)`
   - `AddStructor` → `Generate(Structor)`
   - `AddHierarchical` → `Generate(Hierarchical)`
   - `AddEnum` → `Generate(Enumeration)`
   - etc.
3. Replace `Decl.AddToGen(Generator gen)` with `Decl.Accept(Generator gen)`:
   - Remove `AddToGen` from `Core/Decl.cs`.
   - Add `public abstract void Accept(Generator g);` to `Decl`.
   - Subclasses implement `Accept(g) => g.Generate(this);`.
4. Build + test. No output changes. Commit.

### Phase 2 — Types

1. Create `backend/Generator/Generator.Type.cs`.
2. Move type formatting out of `Core/Typespec.cs`:
   - `Typespec.Gen` → `Generator.Generate(Typespec)`
   - `TypespecBasic.GenType` → `Generator.Generate(TypespecBasic)`
   - `Pointer.GetTemplate` → `Generator.Generate(Pointer)`
   - Replace any `Dialect.Default` reads with the `dialect` field.
3. Update callers in `Core/Mixed.cs`, `Core/Expr.cs`, `Core/Stmt.cs` from `type.Gen()` to `type.Accept(this)` when a generator is available.
4. Build + test. No output changes. Commit.

### Phase 3 — Expressions

1. Create `backend/Generator/Generator.Expr.cs`.
2. Remove `Expr.Gen(bool doBrace)` from `Core/Expr.cs`.
3. Add `Expr.Accept(Generator g)` (or use switch in `Generator` if Option B wins).
4. Move each `Gen` override into `Generator.Expr.cs` as `Generate(SomeExpr)`.
5. Convert recursive `child.Gen()` calls to `child.Accept(this)`.
6. Move bit-operator helper methods near the expression generator.
7. Fix `NewExpr` pointer-list mutation: create a shallow copy of the type, strip the outer pointer, generate, discard the copy.
8. Move inline C++ strings into private static format fields in `Generator.Expr.cs`.
9. Build + test. Golden files should still match. Commit.

### Phase 4 — Statements

1. Create `backend/Generator/Generator.Stmt.cs`.
2. Remove `Stmt.Gen(int level)` / `Stmt.GenWithoutCurly(int level)` from `Core/Stmt.cs`.
3. Add `Stmt.Accept(Generator g, int level)`.
4. Move each statement `Gen` override into `Generator.Stmt.cs`.
5. Convert recursive calls to `Accept`.
6. Move inline C++ strings into private static format fields in `Generator.Stmt.cs`.
7. Build + test. Commit.

### Phase 5 — Mixed helpers

1. Create `backend/Generator/Generator.Mixed.cs`.
2. Remove `Gen` methods from `Param`, `Arg`, `FuncCall`, `IdTplArgs`, `TplArg` in `Core/Mixed.cs`.
3. Add `Accept` methods or switch-based generation in `Generator.Mixed.cs`.
4. Update call sites.
5. Build + test. Commit.

### Phase 6 — Mixed data cleanup

1. Audit `Core` for data classes that are not `Expr`/`Stmt`/`Decl` descendants:
   - `BaseType`
   - `CatchClause`
   - `Param`, `Arg`, `FuncCall`, `IdTplArgs`, `TplArg` (already in `Mixed.cs`)
2. Move `BaseType` and `CatchClause` to `Core/Mixed.cs` if they are not already there.
3. Keep `Core/Decl.cs`, `Core/Expr.cs`, `Core/Stmt.cs` strictly limited to descendants of their base type.
4. Build + test. Commit.

### Phase 7 — Formatting consolidation

1. Move format strings from the old `StmtFormatting` / `ExprFormatting` classes into the appropriate `Generator.*.cs` file.
2. Put cross-cutting helpers in `Generator.cs`.
3. Delete or empty `StmtFormatting.cs` / `ExprFormatting.cs` if nothing remains.
4. Build + test. Commit.

### Phase 8 — Final cleanup

1. Remove `using Myll.Generator;` and `using static StmtFormatting;` from Core files if no longer needed.
2. Audit `backend/Core` for any remaining `Dialect.Default` references.
3. Clean up method names in `Generator.Hierarchical.cs` now that the class is called `Generator`:
   - `GenerateModule`
   - `GenerateNamespace`
   - `GenerateClass` / `GenerateStruct`
   - `GenerateFunction`
4. Clean Unix apps build:
   ```bash
   bash frontend/apps/make_unix.sh clean
   bash frontend/apps/make_unix.sh
   ```
5. Final commit.

## Out of scope

- Converting AST nodes to `record` with `init` properties.
- Parallel module generation.
- Full per-module dialect plumbing in `TypeResolver` / `TypeChecker` / `ConversionRules`.

## Must not break

- Golden-file output must remain byte-identical.
- `dotnet test testing/` must pass (106/106).
- `bash frontend/apps/make_unix.sh` must build all utilities.

## Commit strategy

Commit after each phase when the tree is green. Phases 0–6 are self-contained and safe to land incrementally. Phases 7 and 8 are cleanup.
