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
	public sealed class AliasShadowingTransformerTests
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

		[Fact]
		public void LocalInLoop_ShadowsBaseAlias()
		{
			var (module, context) = CompileModule( @"
module test;
class Base {}
class Derived : Base {
[pub]:
	method test() -> int {
		while( true ) { var int base = 0; }
		return 0;
	}
}
" );

			var (_, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "base" )
				 && d.Message.Contains( "shadows" ) );
		}

		[Fact]
		public void CatchParameter_ShadowsBaseAlias()
		{
			var (module, context) = CompileModule( @"
module test;
class Base {}
class Derived : Base {
[pub]:
	method test() -> int {
		try { return 0; }
		catch( int base ) { return 1; }
	}
}
" );

			var (_, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "base" )
				 && d.Message.Contains( "shadows" ) );
		}

		[Fact]
		public void Parameter_ShadowsBaseAlias()
		{
			var (module, context) = CompileModule( @"
module test;
class Base {}
class Derived : Base {
[pub]:
	method test( int base ) -> int { return 0; }
}
" );

			var (_, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "Parameter" )
				 && d.Message.Contains( "base" ) );
		}

		[Fact]
		public void Member_ShadowsBaseAlias()
		{
			var (module, context) = CompileModule( @"
module test;
class Base {}
class Derived : Base {
field { int base; }
[pub]:
	method test() -> int { return 0; }
}
" );

			var (_, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "Name 'base'" )
				 && d.Message.Contains( "shadows" ) );
		}

		[Fact]
		public void NoShadow_NoWarning()
		{
			var (module, context) = CompileModule( @"
module test;
class Base {}
class Derived : Base {
[pub]:
	method test() -> int { return 0; }
}
" );

			var (_, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.DoesNotContain( diagnostics, d => d.Message.Contains( "base" ) );
		}
	}
}
