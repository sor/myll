using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Myll;
using Myll.Core;
using Myll.Resolver;
using Xunit;

namespace Myll.Tests
{
	public sealed class ElseOnLoopTransformerTests
	{
		private static CompiledModuleResult CompileModule(
			string source,
			string moduleName = "test" )
		{
			AntlrInputStream inputStream = new( source ) { name = moduleName + ".myll" };
			MyllLexer        lexer       = new( inputStream );
			CommonTokenStream tokenStream = new( lexer );
			MyllParser        parser      = new( tokenStream );
			MyllParser.ProgContext prog = parser.prog();

			CompilationContext context = new();
			IGrouping<string, MyllParser.ProgContext> group = new[] { prog }
				.GroupBy( _ => moduleName )
				.First();

			GlobalNamespace module = context.DeclVisitor.VisitProgs( group );
			return new( module, context );
		}

		private static Func FindFunc( GlobalNamespace module, string name )
			=> module.children.OfType<Func>().First( f => f.name == name );

		[Fact]
		public void WhileElse_IsLoweredToFlagAndGuard()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
	var int x = 0;
	while( true ) { x = 1; } else { return -1; }
	return 0;
}
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "f" );

			var loop = func.body!.DescendantsAndSelf().OfType<WhileStmt>().Single();
			Assert.Null( loop.els );

			Assert.Single( func.body.DescendantsAndSelf().OfType<IfStmt>() );
			Assert.True( func.body.DescendantsAndSelf().OfType<VarStmt>().Any() );
		}

		[Fact]
		public void LoopWithoutElse_PassesThrough()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
	while( true ) { }
	return 0;
}
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "f" );
			var loop = func.body!.DescendantsAndSelf().OfType<WhileStmt>().Single();
			Assert.Null( loop.els );
		}
	}
}
