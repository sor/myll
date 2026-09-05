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
	public sealed class TemplateParamTransformerTests
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

		private static Structural FindClass( GlobalNamespace module, string name )
			=> module.children.OfType<Structural>().First( s => s.name == name );

		[Fact]
		public void FunctionTemplateParameter_ResolvesInBody()
		{
			var (module, context) = CompileModule( @"
module test;
func identity<T>( T x ) -> T { return x; }
" );

			var (result, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Empty( diagnostics );

			Func func = FindFunc( module, "identity" );
			Assert.Single( func.TplParams );
			Assert.True( func.funcScope!.children.ContainsKey( "T" ) );

			var xId = context.UnresolvedIds.Single( u => u.Node.idTplArgs.id == "x" );
			Assert.True( result.Ids.TryGetValue( xId.Node, out Decl? decl ) );
			Assert.IsType<VarDecl>( decl );
		}

		[Fact]
		public void ClassTemplateParameter_ResolvesInMethod()
		{
			var (module, context) = CompileModule( @"
module test;
class Box<T> {
field { T _value; }
[pub]:
	method get() -> T { return _value; }
}
" );

			var (result, diagnostics) = NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Assert.Empty( diagnostics );

			Structural box = FindClass( module, "Box" );
			Assert.Single( box.TplParams );
			Assert.True( box.scope.children.ContainsKey( "T" ) );
		}

		[Fact]
		public void TemplateParameter_NotInjectedTwice()
		{
			var (module, context) = CompileModule( @"
module test;
func f<T>( T x ) -> T { return x; }
" );

			NameResolver.Resolve( new CompiledModuleResult[] { (module, context) } );

			Func func = FindFunc( module, "f" );
			Assert.Single( func.funcScope!.children["T"] );
		}
	}
}
