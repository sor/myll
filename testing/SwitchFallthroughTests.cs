using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Myll;
using Myll.Core;
using Xunit;

namespace Myll.Tests
{
	public sealed class SwitchFallthroughTests
	{
		private static CompiledModuleResult CompileModule(
			string source,
			SwitchFallthroughMode mode,
			string moduleName = "test" )
		{
			AntlrInputStream inputStream = new( source ) { name = moduleName + ".myll" };
			MyllLexer        lexer       = new( inputStream );
			CommonTokenStream tokenStream = new( lexer );
			MyllParser        parser      = new( tokenStream );
			MyllParser.ProgContext prog = parser.prog();

			ModuleContext context = new() {
				Dialect = Dialect.Default with { SwitchFallthrough = mode },
			};

			IGrouping<string, MyllParser.ProgContext> group = new[] { prog }
				.GroupBy( _ => moduleName )
				.First();

			GlobalNamespace module = context.DeclVisitor.VisitProgs( group );
			context.Module = module;
			return new( context );
		}

		private static SwitchStmt FindSwitch( GlobalNamespace module )
			=> module.children.OfType<Func>().First().body!.DescendantsAndSelf().OfType<SwitchStmt>().Single();

		[Fact]
		public void ImplicitBreak_InsertsBreakInCaseAndDefault()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
    var int x = 0;
    switch(x) {
        case 1: x = 1;
        default: x = 2;
    }
    return 0;
}
", SwitchFallthroughMode.ImplicitBreak );

			SwitchStmt sw = FindSwitch( module );
			Assert.IsType<BreakStmt>( sw.cases[0].then.stmts.Last() );
			Assert.IsType<BreakStmt>( sw.els!.stmts.Last() );
			Assert.Empty( context.Diagnostics );
		}

		[Fact]
		public void ImplicitFallthrough_DoesNotInsertBreak()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
    var int x = 0;
    switch(x) {
        case 1: x = 1;
        default: x = 2;
    }
    return 0;
}
", SwitchFallthroughMode.ImplicitFallthrough );

			SwitchStmt sw = FindSwitch( module );
			Assert.Single( sw.cases[0].then.stmts );
			Assert.Single( sw.els!.stmts );
			Assert.Empty( context.Diagnostics );
		}

		[Fact]
		public void Explicit_MissingTerminatorProducesDiagnostic()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
    var int x = 0;
    switch(x) {
        case 1: x = 1;
        default: x = 2;
    }
    return 0;
}
", SwitchFallthroughMode.Explicit );

			SwitchStmt sw = FindSwitch( module );
			Assert.Single( sw.cases[0].then.stmts );
			Assert.Single( sw.els!.stmts );
			Assert.Equal( 2, context.Diagnostics.Count );
			Assert.All( context.Diagnostics, d => Assert.Equal( DiagnosticKind.Error, d.Kind ) );
		}

		[Fact]
		public void Fallthrough_StmtIsAddedForAllModes()
		{
			foreach( SwitchFallthroughMode mode in Enum.GetValues<SwitchFallthroughMode>() ) {
				var (module, _) = CompileModule( @"
module test;
func f() -> int {
    var int x = 0;
    switch(x) {
        case 1: x = 1; fall;
        default: x = 2;
    }
    return 0;
}
", mode );

				SwitchStmt sw = FindSwitch( module );
				Assert.Contains( sw.cases[0].then.stmts, s => s is FreetextStmt ft && ft.lines.Any( l => l.Contains( "fallthrough" ) ) );
			}
		}
	}
}
