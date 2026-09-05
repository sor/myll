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
	public sealed class ChainTransformerTests
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

		private static Func FindMethod( GlobalNamespace module, string className, string methodName )
		{
			Structural cls = module.children
				.OfType<Structural>()
				.Single( s => s.name == className );
			return cls.children
				.OfType<Func>()
				.Single( f => f.name == methodName );
		}

		[Fact]
		public void ChainMethod_WithoutReturnType_ReturnsClassReference()
		{
			var (module, context) = CompileModule( @"
module test;
class C {
field { int _x; }
[pub]:
	[chain]
			method inc() { _x = 1; }
}
" );

			var (result, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Empty( diagnostics );

			Func func = FindMethod( module, "C", "inc" );
			Assert.IsType<TypespecNested>( func.retType );
			var ret = (TypespecNested)func.retType;
			Assert.NotNull( ret.ptrs );
			Assert.Single( ret.ptrs );
			Assert.Equal( Pointer.Kind.LVRef, ret.ptrs[0].kind );
			Assert.Equal( module.scope.children["C"].Single().decl, ret.resolvedDecl );
		}

		[Fact]
		public void ChainPureMethod_WithoutReturnType_ReturnsConstClassReference()
		{
			var (module, context) = CompileModule( @"
module test;
class C {
field { int _x; }
[pub]:
	[chain, pure]
	method peek() { }
}
" );

			var (result, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Empty( diagnostics );

			Func func = FindMethod( module, "C", "peek" );
			Assert.IsType<TypespecNested>( func.retType );
			var ret = (TypespecNested)func.retType;
			Assert.NotNull( ret.ptrs );
			Assert.Single( ret.ptrs );
			Assert.Equal( Pointer.Kind.LVRef, ret.ptrs[0].kind );
			Assert.Equal( Qualifier.Const, ret.qual );
		}

		[Fact]
		public void ChainMethod_ExplicitReturnType_ProducesError()
		{
			var (module, context) = CompileModule( @"
module test;
class C {
[pub]:
	[chain]
	method inc() -> C& { return self; }
}
" );

			var (result, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Contains( diagnostics, d
				=> d.Kind == DiagnosticKind.Error
				 && d.Message.Contains( "must not declare a return type" ) );
		}

		[Fact]
		public void ChainMethod_TrailingReturnSelf_ProducesWarning()
		{
			var (module, context) = CompileModule( @"
module test;
class C {
field { int _x; }
[pub]:
	[chain]
	method inc() { _x = 1; return self; }
}
" );

			var (result, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Single( diagnostics );
			Assert.Equal( DiagnosticKind.Warning, diagnostics[0].Kind );
			Assert.Contains( "redundant trailing 'return self;'", diagnostics[0].Message );

			Func func = FindMethod( module, "C", "inc" );
			Assert.Single( func.body!.stmts.OfType<ReturnStmt>() );
		}

		[Fact]
		public void ChainMethod_EarlyReturnSelf_AppendsFinalReturn()
		{
			var (module, context) = CompileModule( @"
module test;
class C {
field { int _x; }
[pub]:
	[chain]
	method inc() { if( _x == 0 ) return self; _x = 1; }
}
" );

			var (result, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Empty( diagnostics );

			Func func = FindMethod( module, "C", "inc" );
			Assert.IsType<ReturnStmt>( func.body!.stmts.Last() );
			Assert.Equal( 2, func.body.EnumerateDF.OfType<ReturnStmt>().Count() );
		}
	}
}
