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
		private static CompiledModuleResult CompileModule(
			string source,
			string moduleName = "test",
			Dialect? dialect = null )
		{
			AntlrInputStream inputStream = new( source ) { name = moduleName + ".myll" };
			MyllLexer        lexer       = new( inputStream );
			CommonTokenStream tokenStream = new( lexer );
			MyllParser        parser      = new( tokenStream );
			MyllParser.ProgContext prog = parser.prog();

			CompilationContext context = new() { Dialect = dialect ?? Myll.Core.Dialect.Default };
			IGrouping<string, MyllParser.ProgContext> group = new[] { prog }
				.GroupBy( _ => moduleName )
				.First();

			GlobalNamespace module = context.DeclVisitor.VisitProgs( group );
			return new( module, context );
		}

		private static List<Diagnostic> RunTypeChecker(
			params CompiledModuleResult[] modules )
		{
			var (result, resolveDiagnostics) = NameResolver.Resolve( modules );
			List<Diagnostic> diagnostics = new( resolveDiagnostics );

			TypeChecker checker = new( result, diagnostics );
			checker.Validate( modules.ToList() );

			return diagnostics;
		}

		[Fact]
		public void LocalShadowsOuterLocal_Warns()
		{
			var (module, context) = CompileModule(
				@"
module test;
func main() -> int
{
	var int x = 1;
	{
		var int x = 2;
	}
	return 0;
}
",
				dialect: new Dialect { Shadowing = ShadowingMode.WarnLocalShadowing } );

			List<Diagnostic> diagnostics = RunTypeChecker( (module, context) );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "shadows" )
				 && d.Message.Contains( "x" ) );
		}

		[Fact]
		public void LocalShadowsOuterLocal_ErrorsWhenConfigured()
		{
			var (module, context) = CompileModule(
				@"
module test;
func main() -> int
{
	var int x = 1;
	{
		var int x = 2;
	}
	return 0;
}
",
				dialect: new Dialect { Shadowing = ShadowingMode.ErrorLocalShadowing } );

			List<Diagnostic> diagnostics = RunTypeChecker( (module, context) );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Error
				 && d.Message.Contains( "shadows" )
				 && d.Message.Contains( "x" ) );
		}

		[Fact]
		public void LocalCollidesWithInstanceMember_Warns()
		{
			var (module, context) = CompileModule(
				@"
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
",
				dialect: new Dialect { Shadowing = ShadowingMode.WarnLocalMemberCollision } );

			List<Diagnostic> diagnostics = RunTypeChecker( (module, context) );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "collides with instance field" )
				 && d.Message.Contains( "value" ) );
		}

		[Fact]
		public void ParameterCollidesWithInstanceMethod_Warns()
		{
			var (module, context) = CompileModule(
				@"
module test;
class Foo
{
	func helper() -> int { return 0; }

	func test( int helper ) -> int
	{
		return helper;
	}
}
",
				dialect: new Dialect { Shadowing = ShadowingMode.WarnLocalMemberCollision } );

			List<Diagnostic> diagnostics = RunTypeChecker( (module, context) );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "collides with instance method" )
				 && d.Message.Contains( "helper" ) );
		}

		[Fact]
		public void NoCollision_Allows()
		{
			var (module, context) = CompileModule(
				@"
module test;
func main() -> int
{
	var int x = 1;
	var int y = 2;
	return x + y;
}
",
				dialect: new Dialect { Shadowing = ShadowingMode.WarnLocalShadowing | ShadowingMode.WarnLocalMemberCollision } );

			List<Diagnostic> diagnostics = RunTypeChecker( (module, context) );

			Assert.DoesNotContain( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "shadows" ) );
			Assert.DoesNotContain( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "collides" ) );
		}
	}
}
