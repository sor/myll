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
	public sealed class ShadowingTests
	{
		private static (GlobalNamespace Module, CompilationContext Context) CompileModule(
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
			return (module, context);
		}

		private static List<Diagnostic> RunTypeChecker(
			ShadowingMode mode,
			params (GlobalNamespace Module, CompilationContext Context)[] modules )
		{
			ShadowingMode previous = Dialect.Shadowing;
			try {
				Dialect.Shadowing = mode;

				var (result, resolveDiagnostics) = NameResolver.Resolve( modules );
				List<Diagnostic> diagnostics = new( resolveDiagnostics );

				TypeChecker checker = new( result, diagnostics );
				checker.Validate( modules.ToList() );

				return diagnostics;
			}
			finally {
				Dialect.Shadowing = previous;
			}
		}

		[Fact]
		public void LocalShadowsOuterLocal_Warns()
		{
			var (module, context) = CompileModule( @"
module test;
func main() -> int
{
	var int x = 1;
	{
		var int x = 2;
	}
	return 0;
}
" );

			List<Diagnostic> diagnostics = RunTypeChecker(
				ShadowingMode.WarnLocalShadowing,
				(module, context) );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "shadows" )
				 && d.Message.Contains( "x" ) );
		}

		[Fact]
		public void LocalShadowsOuterLocal_ErrorsWhenConfigured()
		{
			var (module, context) = CompileModule( @"
module test;
func main() -> int
{
	var int x = 1;
	{
		var int x = 2;
	}
	return 0;
}
" );

			List<Diagnostic> diagnostics = RunTypeChecker(
				ShadowingMode.ErrorLocalShadowing,
				(module, context) );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Error
				 && d.Message.Contains( "shadows" )
				 && d.Message.Contains( "x" ) );
		}

		[Fact]
		public void LocalCollidesWithInstanceMember_Warns()
		{
			var (module, context) = CompileModule( @"
module test;
class Foo
{
	field int value;

	func test() -> int
	{
		var int value = 42;
		return value;
	}
}
" );

			List<Diagnostic> diagnostics = RunTypeChecker(
				ShadowingMode.WarnLocalMemberCollision,
				(module, context) );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "collides with instance field" )
				 && d.Message.Contains( "value" ) );
		}

		[Fact]
		public void ParameterCollidesWithInstanceMethod_Warns()
		{
			var (module, context) = CompileModule( @"
module test;
class Foo
{
	func helper() -> int { return 0; }

	func test( int helper ) -> int
	{
		return helper;
	}
}
" );

			List<Diagnostic> diagnostics = RunTypeChecker(
				ShadowingMode.WarnLocalMemberCollision,
				(module, context) );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "collides with instance method" )
				 && d.Message.Contains( "helper" ) );
		}

		[Fact]
		public void NoCollision_Allows()
		{
			var (module, context) = CompileModule( @"
module test;
func main() -> int
{
	var int x = 1;
	var int y = 2;
	return x + y;
}
" );

			List<Diagnostic> diagnostics = RunTypeChecker(
				ShadowingMode.WarnLocalShadowing | ShadowingMode.WarnLocalMemberCollision,
				(module, context) );

			Assert.DoesNotContain( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "shadows" ) );
			Assert.DoesNotContain( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "collides" ) );
		}
	}
}
