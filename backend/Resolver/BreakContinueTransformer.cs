using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Lowers multi-level <c>break N;</c> and <c>continue N;</c> to hidden flags.
	///
	/// Only loops are counted as levels. <c>break</c> inside a <c>switch</c> keeps
	/// its normal C-style meaning and does not interact with this transform.
	///</summary>
	public sealed class BreakContinueTransformer : ITransformer
	{
		private sealed class LoopContext
		{
			public string BreakFlag    { get; set; } = null!;
			public string ContinueFlag { get; set; } = null!;
			public bool   BreakUsed    { get; set; }
			public bool   ContinueUsed { get; set; }
		}

		public void Transform(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules,
			List<Diagnostic> diagnostics )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules )
				TransformDecl( module, context, diagnostics );
		}

		private static void TransformDecl(
			Decl decl,
			CompilationContext context,
			List<Diagnostic> diagnostics )
		{
			switch( decl ) {
				case Func func:
					if( func.body != null )
						func.body = TransformBlock( func.body, context, new List<LoopContext>(), diagnostics );
					break;

				case Structor stc:
					if( stc.body != null )
						stc.body = TransformBlock( stc.body, context, new List<LoopContext>(), diagnostics );
					break;

				case Hierarchical h:
					foreach( Decl child in h.children )
						TransformDecl( child, context, diagnostics );
					break;
			}
		}

		private static MultiStmt TransformBlock(
			MultiStmt block,
			CompilationContext context,
			List<LoopContext> loops,
			List<Diagnostic> diagnostics )
		{
			var newStmts = new List<Stmt>();
			List<Stmt>? prevGuards = null;

			foreach( Stmt stmt in block.stmts ) {
				newStmts.Add( TransformStmt( stmt, context, loops, diagnostics ) );

				List<Stmt> guards = MakeFlagGuards( loops ).ToList();
				if( !GuardListEquals( guards, prevGuards ) ) {
					newStmts.AddRange( guards );
					prevGuards = guards;
				}
			}

			block.stmts = newStmts;
			return block;
		}

		private static bool GuardListEquals( List<Stmt> a, List<Stmt>? b )
		{
			if( b == null || a.Count != b.Count )
				return false;

			for( int i = 0; i < a.Count; i++ ) {
				if( !GuardEquals( a[i], b[i] ) )
					return false;
			}
			return true;
		}

		private static bool GuardEquals( Stmt a, Stmt b )
		{
			if( a is not IfStmt ia || b is not IfStmt ib )
				return false;

			return String.Equals( GuardFlag( ia ), GuardFlag( ib ), StringComparison.Ordinal )
			    && GuardActionType( ia ) == GuardActionType( ib );
		}

		private static Type GuardActionType( IfStmt guard )
			=> guard.ifThens[0].then.GetType();

		private static string GuardFlag( IfStmt guard )
			=> guard.ifThens[0].cond is IdExpr id
				? id.idTplArgs.id
				: "";

		private static IEnumerable<Stmt> MakeFlagGuards( List<LoopContext> loops )
		{
			if( loops.Count == 0 )
				yield break;

			int currentIndex = loops.Count - 1;

			for( int i = 0; i < loops.Count; i++ ) {
				LoopContext ctx = loops[i];
				bool isCurrent = i == currentIndex;

				if( ctx.BreakUsed )
					yield return MakeGuard( ctx.BreakFlag, new BreakStmt { depth = 1 } );

				if( ctx.ContinueUsed ) {
					Stmt action = isCurrent
						? new ContinueStmt { depth = 1 }
						: new BreakStmt    { depth = 1 };
					yield return MakeGuard( ctx.ContinueFlag, action );
				}
			}
		}

		private static Stmt TransformStmt(
			Stmt stmt,
			CompilationContext context,
			List<LoopContext> loops,
			List<Diagnostic> diagnostics )
		{
			switch( stmt ) {
				case ForStmt fs:
					return TransformLoop( fs, context, loops, diagnostics );

				case WhileStmt ws:
					return TransformLoop( ws, context, loops, diagnostics );

				case DoWhileStmt dws:
					return TransformLoop( dws, context, loops, diagnostics );

				case LoopStmt ls:
					return TransformLoop( ls, context, loops, diagnostics );

				case TimesStmt ts:
					return TransformLoop( ts, context, loops, diagnostics );

				case MultiStmt ms:
					return TransformBlock( ms, context, loops, diagnostics );

				case IfStmt ifs:
					foreach( IfStmt.CondThen ct in ifs.ifThens )
						ct.then = TransformStmt( ct.then, context, loops, diagnostics );
					ifs.els = ifs.els != null
						? TransformStmt( ifs.els, context, loops, diagnostics )
						: null;
					return ifs;

				case TryCatchStmt tcs:
					tcs.tryBody = TransformStmt( tcs.tryBody, context, loops, diagnostics );
					foreach( CatchClause cc in tcs.catches )
						cc.body = TransformStmt( cc.body, context, loops, diagnostics );
					return tcs;

				case SwitchStmt sw:
					foreach( SwitchStmt.CaseBlock cb in sw.cases )
						cb.then = (MultiStmt)TransformStmt( cb.then, context, loops, diagnostics );
					sw.els = sw.els != null
						? (MultiStmt)TransformStmt( sw.els, context, loops, diagnostics )
						: null;
					return sw;

				case BreakStmt bs:
					return TransformBreak( bs, loops, diagnostics );

				case ContinueStmt cs:
					return TransformContinue( cs, loops, diagnostics );

				default:
					return stmt;
			}
		}

		private static Stmt TransformLoop(
			Stmt loop,
			CompilationContext context,
			List<LoopContext> loops,
			List<Diagnostic> diagnostics )
		{
			var loopContext = new LoopContext {
				BreakFlag    = context.NextTempName(),
				ContinueFlag = context.NextTempName(),
			};
			loops.Add( loopContext );

			Stmt body = GetLoopBody( loop ) ?? new EmptyStmt();
			SetLoopBody( loop, TransformBody( body, context, loops, diagnostics ) );

			if( loop is ForStmt fs && fs.els != null )
				fs.els = TransformStmt( fs.els, context, loops, diagnostics );
			else if( loop is WhileStmt ws && ws.els != null )
				ws.els = TransformStmt( ws.els, context, loops, diagnostics );

			loops.RemoveAt( loops.Count - 1 );

			return WrapLoopIfNeeded( loop, loopContext );
		}

		private static Stmt TransformBody(
			Stmt body,
			CompilationContext context,
			List<LoopContext> loops,
			List<Diagnostic> diagnostics )
		{
			MultiStmt block = body is MultiStmt ms
				? ms
				: new MultiStmt( new List<Stmt> { body }, false );
			return TransformBlock( block, context, loops, diagnostics );
		}

		private static Stmt? GetLoopBody( Stmt loop )
			=> loop switch {
				ForStmt fs      => fs.body,
				WhileStmt ws    => ws.body,
				DoWhileStmt dws => dws.body,
				LoopStmt ls     => ls.body,
				TimesStmt ts    => ts.body,
				_               => throw new InvalidOperationException( "unexpected loop kind" ),
			};

		private static void SetLoopBody( Stmt loop, Stmt body )
		{
			switch( loop ) {
				case ForStmt fs:      fs.body = body; break;
				case WhileStmt ws:    ws.body = body; break;
				case DoWhileStmt dws: dws.body = body; break;
				case LoopStmt ls:     ls.body = body; break;
				case TimesStmt ts:    ts.body = body; break;
			}
		}

		private static Stmt WrapLoopIfNeeded( Stmt loop, LoopContext loopContext )
		{
			if( !loopContext.BreakUsed && !loopContext.ContinueUsed )
				return loop;

			var decls = new List<Stmt>();

			if( loopContext.BreakUsed ) {
				decls.Add( new VarStmt {
					srcPos = loop.srcPos,
					kind   = VarDecl.Kind.Var,
					name   = loopContext.BreakFlag,
					type   = new TypespecBasic {
						kind   = TypespecBasic.Kind.Bool,
						size   = 1,
						srcPos = loop.srcPos,
					},
					init = new Literal { op = Operand.Literal, text = "false" },
				} );
			}

			if( loopContext.ContinueUsed ) {
				decls.Add( new VarStmt {
					srcPos = loop.srcPos,
					kind   = VarDecl.Kind.Var,
					name   = loopContext.ContinueFlag,
					type   = new TypespecBasic {
						kind   = TypespecBasic.Kind.Bool,
						size   = 1,
						srcPos = loop.srcPos,
					},
					init = new Literal { op = Operand.Literal, text = "false" },
				} );
			}

			decls.Add( loop );
			return new MultiStmt( decls, true );
		}

		private static IfStmt MakeGuard( string flagName, Stmt action )
			=> new() {
				ifThens = new List<IfStmt.CondThen> {
					new(
						IdExpr( flagName ),
						action ),
				},
			};

		private static Stmt TransformBreak(
			BreakStmt bs,
			List<LoopContext> loops,
			List<Diagnostic> diagnostics )
		{
			if( bs.depth <= 1 )
				return bs;

			if( loops.Count < bs.depth ) {
				diagnostics.Add( new Diagnostic(
					bs.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"break {0} is not inside {0} enclosing loops",
						bs.depth ) ) );
				return bs;
			}

			var stmts = new List<Stmt>();
			int targetIndex = loops.Count - bs.depth;

			for( int i = targetIndex; i < loops.Count - 1; i++ ) {
				LoopContext ctx = loops[i];
				ctx.BreakUsed = true;
				stmts.Add( AssignFlag( ctx.BreakFlag, "true" ) );
			}

			stmts.Add( new BreakStmt { depth = 1, srcPos = bs.srcPos } );
			return stmts.Count == 1
				? stmts[0]
				: new MultiStmt( stmts, false );
		}

		private static Stmt TransformContinue(
			ContinueStmt cs,
			List<LoopContext> loops,
			List<Diagnostic> diagnostics )
		{
			if( cs.depth <= 1 )
				return cs;

			if( loops.Count < cs.depth ) {
				diagnostics.Add( new Diagnostic(
					cs.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"continue {0} is not inside {0} enclosing loops",
						cs.depth ) ) );
				return cs;
			}

			var stmts = new List<Stmt>();
			int targetIndex = loops.Count - cs.depth;

			for( int i = targetIndex + 1; i < loops.Count - 1; i++ ) {
				LoopContext ctx = loops[i];
				ctx.BreakUsed = true;
				stmts.Add( AssignFlag( ctx.BreakFlag, "true" ) );
			}

			LoopContext target = loops[targetIndex];
			target.ContinueUsed = true;
			stmts.Add( AssignFlag( target.ContinueFlag, "true" ) );

			stmts.Add( new BreakStmt { depth = 1, srcPos = cs.srcPos } );
			return stmts.Count == 1
				? stmts[0]
				: new MultiStmt( stmts, false );
		}

		private static MultiAssign AssignFlag( string flagName, string value )
			=> new() {
				exprs = new List<Expr> {
					IdExpr( flagName ),
					new Literal { op = Operand.Literal, text = value },
				},
			};

		private static IdExpr IdExpr( string name )
			=> new() { op = Operand.Id, idTplArgs = new() { id = name } };
	}
}
