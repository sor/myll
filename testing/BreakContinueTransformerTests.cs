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
	public sealed class BreakContinueTransformerTests
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
		public void PlainBreak_IsNotTransformed()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
	while( true ) { break; }
	return 0;
}
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "f" );
			Assert.Contains( func.body!.EnumerateDF.OfType<BreakStmt>(), b => b.depth == 1 );
		}

		[Fact]
		public void BreakDepth_CreatesFlagVariables()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
	while( true ) {
		while( true ) {
			break 2;
		}
	}
	return 0;
}
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "f" );
			Assert.NotEmpty( func.body!.EnumerateDF.OfType<VarStmt>() );
			Assert.DoesNotContain( func.body.EnumerateDF.OfType<BreakStmt>(), b => b.depth > 1 );
		}

		[Fact]
		public void ContinueDepth_CreatesFlagVariables()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
	while( true ) {
		while( true ) {
			continue 2;
		}
	}
	return 0;
}
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "f" );
			Assert.NotEmpty( func.body!.EnumerateDF.OfType<VarStmt>() );
			Assert.DoesNotContain( func.body.EnumerateDF.OfType<ContinueStmt>(), c => c.depth > 1 );
		}

		[Fact]
		public void BreakTooDeep_ProducesError()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
	while( true ) { break 3; }
	return 0;
}
" );

			var (_, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Error
				 && d.Message.Contains( "break 3" ) );
		}
	}
}
