using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Transform
{
	/// <summary>
	/// Lowers Myll loop <c>else</c> clauses to plain C++.
	///
	/// Unlike Python, Myll loop <c>else</c> is the "was-NOT-entered" branch:
	/// the <c>else</c> body runs ONLY if the loop condition is false on the
	/// first check and the loop body never executes.
	/// </summary>
	public sealed class ElseOnLoopTransformer : Transformer
	{
		protected override void PostTransform( TransformContext ctx )
		{
			TransformDecl( ctx.Module.Module, ctx.Module.Context );
		}

		private static void TransformDecl( Decl decl, ModuleContext context )
		{
			switch( decl ) {
				case Func func:
					if( func.body != null )
						func.body = TransformBlock( func.body, context );
					break;

				case Structor stc:
					if( stc.body != null )
						stc.body = TransformBlock( stc.body, context );
					break;

				case Hierarchical h:
					foreach( Decl child in h.children )
						TransformDecl( child, context );
					break;
			}
		}

		private static MultiStmt TransformBlock( MultiStmt block, ModuleContext context )
		{
			block.stmts = block.stmts
				.Select( s => TransformStmt( s, context ) )
				.ToList();
			return block;
		}

		private static Stmt TransformStmt( Stmt stmt, ModuleContext context )
		{
			switch( stmt ) {
			case ForStmt fs:
				return fs.els != null
					? TransformForOrWhile( fs, context )
					: fs;

			case RangeForStmt rfs:
				return rfs.els != null
					? TransformForOrWhile( rfs, context )
					: rfs;

			case WhileStmt ws:
					return ws.els != null
						? TransformForOrWhile( ws, context )
						: ws;

				case MultiStmt ms:
					return TransformBlock( ms, context );

				case DoWhileStmt dws:
					dws.body = TransformStmt( dws.body, context );
					return dws;

				case LoopStmt ls:
					ls.body = TransformStmt( ls.body, context );
					return ls;

				case TimesStmt ts:
					ts.body = TransformStmt( ts.body, context );
					return ts;

				case IfStmt ifs:
					foreach( IfStmt.CondThen ct in ifs.ifThens )
						ct.then = TransformStmt( ct.then, context );
					ifs.els = ifs.els != null
						? TransformStmt( ifs.els, context )
						: null;
					return ifs;

				case TryCatchStmt tcs:
					tcs.tryBody = TransformStmt( tcs.tryBody, context );
					foreach( CatchClause cc in tcs.catches )
						cc.body = TransformStmt( cc.body, context );
					return tcs;

				case SwitchStmt sw:
					foreach( SwitchStmt.CaseBlock cb in sw.cases )
						cb.then = (MultiStmt)TransformStmt( cb.then, context );
					sw.els = sw.els != null
						? (MultiStmt)TransformStmt( sw.els, context )
						: null;
					return sw;

				default:
					return stmt;
			}
		}

		private static MultiStmt TransformForOrWhile( ForStmt loop, ModuleContext context )
		{
			string flagName = context.NextTempName();

			loop.body = loop.body != null
				? TransformStmt( loop.body, context )
				: null;

			Stmt elseBody = loop.els!;
			loop.els     = null;

			loop.body = WrapWithEntryFlag( loop.body, flagName );

			return BuildElseScope( loop, elseBody, flagName, context );
		}

		private static MultiStmt TransformForOrWhile( WhileStmt loop, ModuleContext context )
		{
			string flagName = context.NextTempName();

			loop.body = TransformStmt( loop.body, context );

			Stmt elseBody = loop.els!;
			loop.els     = null;

			loop.body = WrapWithEntryFlag( loop.body, flagName );

			return BuildElseScope( loop, elseBody, flagName, context );
		}

		private static MultiStmt TransformForOrWhile( RangeForStmt loop, ModuleContext context )
		{
			string flagName = context.NextTempName();

			loop.body = loop.body != null
				? TransformStmt( loop.body, context )
				: null;

			Stmt elseBody = loop.els!;
			loop.els     = null;

			loop.body = WrapWithEntryFlag( loop.body, flagName );

			return BuildElseScope( loop, elseBody, flagName, context );
		}

		private static MultiStmt WrapWithEntryFlag( Stmt? body, string flagName )
		{
			var stmts = new List<Stmt> { AssignFlag( flagName, "true" ) };
			if( body != null )
				stmts.Add( body );
			return new MultiStmt( stmts, false );
		}

		private static MultiStmt BuildElseScope(
			Stmt loop,
			Stmt elseBody,
			string flagName,
			ModuleContext context )
		{
			Stmt transformedElse = TransformStmt( elseBody, context );

			var flagDecl = new VarStmt {
				srcPos = loop.srcPos,
				kind   = VarDecl.Kind.Var,
				name   = flagName,
				type   = new TypespecBasic {
					kind   = TypespecBasic.Kind.Bool,
					size   = 1,
					srcPos = loop.srcPos,
				},
				init = new Literal {
					op   = Operand.Literal,
					text = "false",
				},
			};

			var guard = new IfStmt {
				srcPos = transformedElse.srcPos,
				ifThens = new List<IfStmt.CondThen> {
					new(
						new UnOp {
							op   = Operand.Negation,
							expr = FlagId( flagName ),
						},
						transformedElse ),
				},
			};

			return new MultiStmt(
				new List<Stmt> { flagDecl, loop, guard },
				true );
		}

		private static MultiAssign AssignFlag( string flagName, string value )
			=> new() {
				exprs = new List<Expr> {
					FlagId( flagName ),
					new Literal { op = Operand.Literal, text = value },
				},
			};

		private static IdExpr FlagId( string flagName )
			=> new() { op = Operand.Id, idTplArgs = new() { id = flagName } };
	}
}
