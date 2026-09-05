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
	public sealed class AutoReturnTransformerTests
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
		public void RetUse_InsertsVariableAndFinalReturn()
		{
			var (module, context) = CompileModule( @"
module test;
func foo() -> int { ret = 42; }
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "foo" );
			Assert.NotNull( func.body );
			Assert.IsType<VarStmt>( func.body!.stmts[0] );
			Assert.True( ((VarStmt)func.body.stmts[0]).IsAutoReturn );
			Assert.IsType<ReturnStmt>( func.body.stmts[func.body.stmts.Count - 1] );
		}

		[Fact]
		public void FinalReturnPresent_NoExtraReturnAppended()
		{
			var (module, context) = CompileModule( @"
module test;
func foo() -> int { ret = 1; return ret; }
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "foo" );
			Assert.Single( func.body!.stmts.OfType<ReturnStmt>() );
		}

		[Fact]
		public void NoRetUse_NoTransformation()
		{
			var (module, context) = CompileModule( @"
module test;
func foo() -> int { return 7; }
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "foo" );
			Assert.DoesNotContain( func.body!.stmts, s => s is VarStmt v && v.IsAutoReturn );
		}

		[Fact]
		public void LocalNamedRet_DisabledWithWarning()
		{
			var (module, context) = CompileModule( @"
module test;
func foo() -> int { var int ret = 3; return ret; }
" );

			var (_, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "auto-return generation is disabled" ) );
		}

		[Fact]
		public void ParameterNamedRet_DisabledWithWarning()
		{
			var (module, context) = CompileModule( @"
module test;
func foo(int ret) -> int { return ret + 1; }
" );

			var (_, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "auto-return generation is disabled" ) );
		}

		[Fact]
		public void VoidReturn_NoTransformation()
		{
			var (module, context) = CompileModule( @"
module test;
func foo() { ret; }
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "foo" );
			Assert.DoesNotContain( func.body!.stmts, s => s is VarStmt v && v.IsAutoReturn );
		}

		[Fact]
		public void ReferenceReturn_DisabledWithWarning()
		{
			var (module, context) = CompileModule( @"
module test;
var int globalValue = 5;
func foo() -> int& { ret = globalValue; }
" );

			var (_, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Warning
				 && d.Message.Contains( "reference return types" ) );
		}
	}
}
