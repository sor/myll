# Shared Statement Transformer

## Problem

`DiscardTransformer` and `ElseOnLoopTransformer` both walk `Stmt` trees inside `Func`/`Structor` bodies.
Each transformer currently contains a big `switch` over every statement container.
That duplication is fragile: every new statement kind that can hold statements must be added to every transformer.

## Goal

Add one shared `StmtTransformer` base class that knows how to walk a `Stmt` tree.
Concrete transformers then only override the node types they care about.
The base class handles recursion into all container statements.

## Proposed Design

Create `backend/Resolver/StmtTransformer.cs`:

```csharp
public abstract class StmtTransformer
{
	public Stmt Transform( Stmt stmt )
		=> stmt switch {
			ForStmt fs        => TransformForStmt( fs ),
			WhileStmt ws      => TransformWhileStmt( ws ),
			DoWhileStmt dws   => TransformDoWhileStmt( dws ),
			LoopStmt ls       => TransformLoopStmt( ls ),
			TimesStmt ts      => TransformTimesStmt( ts ),
			IfStmt ifs        => TransformIfStmt( ifs ),
			SwitchStmt sw     => TransformSwitchStmt( sw ),
			TryCatchStmt tcs  => TransformTryCatchStmt( tcs ),
			MultiStmt ms      => TransformMultiStmt( ms ),
			_                 => stmt,
		};

	protected virtual Stmt TransformForStmt( ForStmt fs )
	{
		fs.body = fs.body != null ? Transform( fs.body ) : null;
		fs.els  = fs.els != null ? Transform( fs.els ) : null;
		return fs;
	}

	protected virtual Stmt TransformWhileStmt( WhileStmt ws )
	{
		ws.body = Transform( ws.body );
		ws.els  = ws.els != null ? Transform( ws.els ) : null;
		return ws;
	}

	// ... similar protected virtual defaults for DoWhileStmt, LoopStmt,
	// TimesStmt, IfStmt, SwitchStmt, TryCatchStmt, MultiStmt
}
```

## Refactoring Steps

1. Add `StmtTransformer` with default recursion for every container `Stmt`.
2. Rewrite `ElseOnLoopTransformer` to inherit from `StmtTransformer`.
   Override only `TransformForStmt` and `TransformWhileStmt`.
3. Rewrite `DiscardTransformer` to inherit from `StmtTransformer` if practical.
   `DiscardTransformer` needs to insert hidden statements before the current statement.
   That may need a different shape, for example an override of `TransformMultiStmt` that walks the statement list with insertion.
   If it does not fit the base class cleanly, keep its own traversal for now and add a comment explaining why.
4. In `Program.cs` keep the separate transform calls; do not merge passes unless the transforms can run without interfering.

## Future Use

Once the shared walker exists, other AST-only transforms can use it:

- labelled multi-level `break`/`continue` lowering
- `defer` lowering
- future loop optimisations

## Notes

- This is AST-based lowering, not generator-based lowering.
- Keep the walker in `backend/Resolver/` because it is a transform, not a generator concern.
