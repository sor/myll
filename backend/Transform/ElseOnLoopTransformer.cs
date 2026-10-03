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
		private sealed class Lowerer : DeclWalker
		{
			private readonly ModuleContext context;

			public Lowerer( ModuleContext context ) => this.context = context;

			protected override Stmt TransformStmt( Stmt stmt )
			{
				switch( stmt ) {
					case ForStmt fs when fs.els != null:
						return TransformForOrWhile( fs );
					case RangeForStmt rfs when rfs.els != null:
						return TransformForOrWhile( rfs );
					case WhileStmt ws when ws.els != null:
						return TransformForOrWhile( ws );
					default:
						return base.TransformStmt( stmt );
				}
			}

			private MultiStmt TransformForOrWhile( ForStmt loop )
			{
				string flagName = context.NextTempName();

				loop.body = loop.body != null
					? WalkStmt( loop.body )
					: null;

				Stmt elseBody = loop.els!;
				loop.els     = null;

				loop.body = WrapWithEntryFlag( loop.body, flagName );

				return BuildElseScope( loop, elseBody, flagName );
			}

			private MultiStmt TransformForOrWhile( WhileStmt loop )
			{
				string flagName = context.NextTempName();

				loop.body = WalkStmt( loop.body );

				Stmt elseBody = loop.els!;
				loop.els     = null;

				loop.body = WrapWithEntryFlag( loop.body, flagName );

				return BuildElseScope( loop, elseBody, flagName );
			}

			private MultiStmt TransformForOrWhile( RangeForStmt loop )
			{
				string flagName = context.NextTempName();

				loop.body = loop.body != null
					? WalkStmt( loop.body )
					: null;

				Stmt elseBody = loop.els!;
				loop.els     = null;

				loop.body = WrapWithEntryFlag( loop.body, flagName );

				return BuildElseScope( loop, elseBody, flagName );
			}

			private static MultiStmt WrapWithEntryFlag( Stmt? body, string flagName )
			{
				var stmts = new List<Stmt> { AssignFlag( flagName, "true" ) };
				if( body != null )
					stmts.Add( body );
				return new MultiStmt( stmts, false );
			}

			private MultiStmt BuildElseScope( Stmt loop, Stmt elseBody, string flagName )
			{
				Stmt transformedElse = WalkStmt( elseBody );

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

		protected override void PostTransform( TransformContext ctx )
		{
			Lowerer walker = new( ctx.Module.Context );
			walker.WalkModule( ctx.Module.Module );
		}
	}
}