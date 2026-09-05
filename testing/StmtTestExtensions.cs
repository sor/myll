using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Tests
{
	/// <summary>
	/// Replacement for the removed <c>Stmt.EnumerateDF</c> traversal. Used only by tests.
	/// </summary>
	internal static class StmtTestExtensions
	{
		public static IEnumerable<Stmt> DescendantsAndSelf( this Stmt? stmt )
		{
			if( stmt == null )
				yield break;

			yield return stmt;

			foreach( Stmt child in stmt.GetChildStatements() )
				foreach( Stmt descendant in child.DescendantsAndSelf() )
					yield return descendant;
		}

		private static IEnumerable<Stmt> GetChildStatements( this Stmt stmt )
		{
			return stmt switch {
				IfStmt ifs
					=> ifs.ifThens.Select( ct => ct.then )
					   .Concat( ToEnumerable( ifs.els ) ),
				SwitchStmt sw
					=> sw.cases.SelectMany( cb => ToEnumerable( cb.then ) )
					   .Concat( ToEnumerable( sw.els ) ),
				LoopStmt ls
					=> ToEnumerable( ls.body ),
				ForStmt fs
					=> ToEnumerable( fs.init )
					   .Concat( ToEnumerable( fs.body ) )
					   .Concat( ToEnumerable( fs.els ) ),
				WhileStmt ws
					=> ToEnumerable( ws.body )
					   .Concat( ToEnumerable( ws.els ) ),
				DoWhileStmt dws
					=> ToEnumerable( dws.body ),
				TimesStmt ts
					=> ToEnumerable( ts.body ),
				TryCatchStmt tcs
					=> ToEnumerable( tcs.tryBody )
					   .Concat( tcs.catches.Select( cc => cc.body ) ),
				MultiStmt ms
					=> ms.stmts,
				_ => Enumerable.Empty<Stmt>(),
			};
		}

		private static IEnumerable<T> ToEnumerable<T>( this T? item ) where T : class
		{
			if( item == null )
				return Enumerable.Empty<T>();

			return new[] { item };
		}
	}
}
