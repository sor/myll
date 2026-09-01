using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Lowers multi-level <c>break N;</c> and <c>continue N;</c> to hidden flags.
	///
	/// <c>break N</c> exits the <c>N</c>th enclosing breakable construct (loops and
	/// switches). <c>continue N</c> only targets loops; switches are not counted.
	///
	/// A plain <c>break</c> inside a switch still exits that switch. A plain
	/// <c>continue</c> inside a loop still continues that loop.
	/// </summary>
	public sealed class BreakContinueTransformer : ITransformer
	{
		private sealed class BreakableContext
		{
			public bool   IsLoop       { get; set; }
			public string BreakFlag    { get; set; } = null!;
			public string ContinueFlag { get; set; } = null!;
			public bool   BreakUsed    { get; set; }
			public bool   ContinueUsed { get; set; }
		}

		private static BreakableContext NewBreakableContext(
			CompilationContext context,
			bool               isLoop )
			=> new() {
				IsLoop       = isLoop,
				BreakFlag    = context.NextTempName(),
				ContinueFlag = context.NextTempName(),
			};

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
						func.body = TransformBlock( func.body, context, new List<BreakableContext>(), diagnostics );
					break;

				case Structor stc:
					if( stc.body != null )
						stc.body = TransformBlock( stc.body, context, new List<BreakableContext>(), diagnostics );
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
			List<BreakableContext> breakables,
			List<Diagnostic> diagnostics )
		{
			var newStmts = new List<Stmt>();
			List<Stmt>? prevGuards   = null;
			bool        stopGuards = false;

			foreach( Stmt stmt in block.stmts ) {
				Stmt transformed = TransformStmt( stmt, context, breakables, diagnostics );
				newStmts.Add( transformed );

				if( stopGuards )
					continue;

				if( EndsWithUnconditionalExit( transformed ) ) {
					stopGuards = true;
					continue;
				}

				List<Stmt> guards = MakeFlagGuards( breakables ).ToList();
				if( !GuardListEquals( guards, prevGuards ) ) {
					newStmts.AddRange( guards );
					prevGuards = guards;
				}
			}

			block.stmts = newStmts;
			return block;
		}

		private static bool EndsWithUnconditionalExit( Stmt stmt )
			=> stmt switch {
				BreakStmt or ContinueStmt or ReturnStmt or ThrowStmt => true,
				MultiStmt ms when ms.stmts.Count > 0                 => EndsWithUnconditionalExit( ms.stmts[ms.stmts.Count - 1] ),
				_                                                    => false,
			};

		private static Stmt PrependResets( Stmt body, List<BreakableContext> breakables )
		{
			List<Stmt> resets = MakeFlagResets( breakables );
			if( resets.Count == 0 )
				return body;

			MultiStmt block = body is MultiStmt ms
				? ms
				: new MultiStmt( new List<Stmt> { body }, false );

			block.stmts.InsertRange( 0, resets );
			return block;
		}

		private static List<Stmt> MakeFlagResets( List<BreakableContext> breakables )
		{
			var resets = new List<Stmt>();

			foreach( BreakableContext ctx in breakables ) {
				if( ctx.BreakUsed )
					resets.Add( AssignFlag( ctx.BreakFlag, "false" ) );
				if( ctx.ContinueUsed )
					resets.Add( AssignFlag( ctx.ContinueFlag, "false" ) );
			}

			return resets;
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

		private static string GuardFlag( IfStmt guard )
			=> guard.ifThens[0].cond is IdExpr id
				? id.idTplArgs.id
				: "";

		private static Type GuardActionType( IfStmt guard )
			=> guard.ifThens[0].then.GetType();

		private static IEnumerable<Stmt> MakeFlagGuards( List<BreakableContext> breakables )
		{
			if( breakables.Count == 0 )
				yield break;

			int currentIndex = breakables.Count - 1;

			for( int i = 0; i < breakables.Count; i++ ) {
				BreakableContext ctx = breakables[i];

				if( ctx.BreakUsed )
					yield return MakeGuard( ctx.BreakFlag, new BreakStmt { depth = 1 } );

				if( ctx.ContinueUsed ) {
					Stmt action = ctx.IsLoop && i == currentIndex
						? new ContinueStmt { depth = 1 }
						: new BreakStmt    { depth = 1 };
					yield return MakeGuard( ctx.ContinueFlag, action );
				}
			}
		}

		private static Stmt TransformStmt(
			Stmt stmt,
			CompilationContext context,
			List<BreakableContext> breakables,
			List<Diagnostic> diagnostics )
		{
			switch( stmt ) {
				case ForStmt fs:
				case WhileStmt ws:
				case DoWhileStmt dws:
				case LoopStmt ls:
				case TimesStmt ts:
					return TransformLoop( stmt, context, breakables, diagnostics );

				case SwitchStmt sw:
					return TransformSwitch( sw, context, breakables, diagnostics );

				case MultiStmt ms:
					return TransformBlock( ms, context, breakables, diagnostics );

				case IfStmt ifs:
					foreach( IfStmt.CondThen ct in ifs.ifThens )
						ct.then = TransformStmt( ct.then, context, breakables, diagnostics );
					ifs.els = ifs.els != null
						? TransformStmt( ifs.els, context, breakables, diagnostics )
						: null;
					return ifs;

				case TryCatchStmt tcs:
					tcs.tryBody = TransformStmt( tcs.tryBody, context, breakables, diagnostics );
					foreach( CatchClause cc in tcs.catches )
						cc.body = TransformStmt( cc.body, context, breakables, diagnostics );
					return tcs;

				case BreakStmt bs:
					return TransformBreak( bs, breakables, diagnostics );

				case ContinueStmt cs:
					return TransformContinue( cs, breakables, diagnostics );

				default:
					return stmt;
			}
		}

		private static Stmt TransformLoop(
			Stmt loop,
			CompilationContext context,
			List<BreakableContext> breakables,
			List<Diagnostic> diagnostics )
		{
			var loopContext = NewBreakableContext( context, true );
			breakables.Add( loopContext );

			Stmt body = GetLoopBody( loop ) ?? new EmptyStmt();
			Stmt transformedBody = TransformBody( body, context, breakables, diagnostics );
			transformedBody     = PrependResets( transformedBody, breakables );
			SetLoopBody( loop, transformedBody );

			if( loop is ForStmt fs && fs.els != null )
				fs.els = TransformStmt( fs.els, context, breakables, diagnostics );
			else if( loop is WhileStmt ws && ws.els != null )
				ws.els = TransformStmt( ws.els, context, breakables, diagnostics );

			breakables.RemoveAt( breakables.Count - 1 );

			return WrapBreakableIfNeeded( loop, loopContext );
		}

		private static Stmt TransformSwitch(
			SwitchStmt sw,
			CompilationContext context,
			List<BreakableContext> breakables,
			List<Diagnostic> diagnostics )
		{
			var switchContext = NewBreakableContext( context, false );
			breakables.Add( switchContext );

			foreach( SwitchStmt.CaseBlock cb in sw.cases )
				cb.then = (MultiStmt)TransformStmt( cb.then, context, breakables, diagnostics );

			if( sw.els != null )
				sw.els = (MultiStmt)TransformStmt( sw.els, context, breakables, diagnostics );

			breakables.RemoveAt( breakables.Count - 1 );

			return WrapBreakableIfNeeded( sw, switchContext );
		}

		private static Stmt TransformBody(
			Stmt body,
			CompilationContext context,
			List<BreakableContext> breakables,
			List<Diagnostic> diagnostics )
		{
			MultiStmt block = body is MultiStmt ms
				? ms
				: new MultiStmt( new List<Stmt> { body }, false );
			return TransformBlock( block, context, breakables, diagnostics );
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

		private static Stmt WrapBreakableIfNeeded(
			Stmt stmt,
			BreakableContext ctx )
		{
			if( !ctx.BreakUsed && !ctx.ContinueUsed )
				return stmt;

			var decls = new List<Stmt>();

			if( ctx.BreakUsed ) {
				decls.Add( new VarStmt {
					srcPos = stmt.srcPos,
					kind   = VarDecl.Kind.Var,
					name   = ctx.BreakFlag,
					type   = new TypespecBasic {
						kind   = TypespecBasic.Kind.Bool,
						size   = 1,
						srcPos = stmt.srcPos,
					},
					init = new Literal { op = Operand.Literal, text = "false" },
				} );
			}

			if( ctx.ContinueUsed ) {
				decls.Add( new VarStmt {
					srcPos = stmt.srcPos,
					kind   = VarDecl.Kind.Var,
					name   = ctx.ContinueFlag,
					type   = new TypespecBasic {
						kind   = TypespecBasic.Kind.Bool,
						size   = 1,
						srcPos = stmt.srcPos,
					},
					init = new Literal { op = Operand.Literal, text = "false" },
				} );
			}

			decls.Add( stmt );
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
			List<BreakableContext> breakables,
			List<Diagnostic> diagnostics )
		{
			if( bs.depth <= 1 )
				return bs;

			if( breakables.Count < bs.depth ) {
				diagnostics.Add( new Diagnostic(
					bs.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"break {0} is not inside {0} enclosing breakable constructs",
						bs.depth ) ) );
				return bs;
			}

			var stmts = new List<Stmt>();
			int targetIndex = breakables.Count - bs.depth;

			for( int i = targetIndex; i < breakables.Count - 1; i++ ) {
				BreakableContext ctx = breakables[i];
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
			List<BreakableContext> breakables,
			List<Diagnostic> diagnostics )
		{
			if( cs.depth <= 1 )
				return cs;

			int targetIndex = FindLoopTargetIndex( breakables, cs.depth, out int loopsFound );

			if( targetIndex < 0 ) {
				diagnostics.Add( new Diagnostic(
					cs.srcPos,
					DiagnosticKind.Error,
					String.Format(
						"continue {0} is not inside {0} enclosing loops",
						cs.depth ) ) );
				return cs;
			}

			var stmts = new List<Stmt>();

			for( int i = targetIndex + 1; i < breakables.Count - 1; i++ ) {
				BreakableContext ctx = breakables[i];
				ctx.BreakUsed = true;
				stmts.Add( AssignFlag( ctx.BreakFlag, "true" ) );
			}

			BreakableContext target = breakables[targetIndex];
			target.ContinueUsed = true;
			stmts.Add( AssignFlag( target.ContinueFlag, "true" ) );

			stmts.Add( new BreakStmt { depth = 1, srcPos = cs.srcPos } );
			return stmts.Count == 1
				? stmts[0]
				: new MultiStmt( stmts, false );
		}

		private static int FindLoopTargetIndex(
			List<BreakableContext> breakables,
			int depth,
			out int loopsFound )
		{
			loopsFound = 0;

			for( int i = breakables.Count - 1; i >= 0; i-- ) {
				if( breakables[i].IsLoop ) {
					loopsFound++;
					if( loopsFound == depth )
						return i;
				}
			}

			return -1;
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
