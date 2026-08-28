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
	public sealed class ResolverTests
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

		private static (ResolutionResult Result, IReadOnlyList<Diagnostic> Diagnostics) Resolve(
			params (GlobalNamespace Module, CompilationContext Context)[] modules )
			=> NameResolver.Resolve( modules );

		[Fact]
		public void ForwardFunctionReference_Resolves()
		{
			var (module, context) = CompileModule( @"
module test;
func main() -> int { return square(9); }
func square(int x) -> int { return 81; }
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Empty( diagnostics );
			Assert.Single( context.UnresolvedIds );
			Assert.True(
				result.Ids.TryGetValue( context.UnresolvedIds[0].Node, out Decl? decl ),
				"square should resolve" );
			Assert.IsType<Func>( decl );
			Assert.Equal( "square", decl!.name );
		}

		[Fact]
		public void ForwardClassReference_Resolves()
		{
			var (module, context) = CompileModule( @"
module test;
func use( B b ) -> B { var B x; return; }
class B { }
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Empty( diagnostics );
			Assert.Equal( 3, context.UnresolvedTypes.Count );
			Assert.All( context.UnresolvedTypes, ut =>
				Assert.True(
					result.Types.TryGetValue( ut.Node, out Decl? decl ),
					"type reference should resolve" ) );
		}

		[Fact]
		public void CrossModuleImport_Resolves()
		{
			var (moduleB, contextB) = CompileModule( @"
module B;
func helper() -> int { return 42; }
", "B" );

			var (moduleA, contextA) = CompileModule( @"
module A;
import B;
func main() -> int { return helper(); }
", "A" );

			var (result, diagnostics) = Resolve(
				(moduleA, contextA),
				(moduleB, contextB) );

			Assert.Empty( diagnostics );
			Assert.Single( contextA.UnresolvedIds );
			Assert.True(
				result.Ids.TryGetValue( contextA.UnresolvedIds[0].Node, out Decl? decl ),
				"helper should resolve via import" );
			Assert.IsType<Func>( decl );
			Assert.Equal( "helper", decl!.name );
		}

		[Fact]
		public void CyclicImport_Resolves()
		{
			var (moduleB, contextB) = CompileModule( @"
module B;
import A;
func b() -> int { return a(); }
func helpB() -> int { return 2; }
", "B" );

			var (moduleA, contextA) = CompileModule( @"
module A;
import B;
func a() -> int { return helpB(); }
func helpA() -> int { return 1; }
", "A" );

			var (result, diagnostics) = Resolve(
				(moduleA, contextA),
				(moduleB, contextB) );

			Assert.Empty( diagnostics );
			Assert.All( contextA.UnresolvedIds, uid =>
				Assert.True(
					result.Ids.TryGetValue( uid.Node, out Decl? decl ),
					String.Format( "'{0}' in module A should resolve", uid.Node.idTplArgs.id ) ) );
			Assert.All( contextB.UnresolvedIds, uid =>
				Assert.True(
					result.Ids.TryGetValue( uid.Node, out Decl? decl ),
					String.Format( "'{0}' in module B should resolve", uid.Node.idTplArgs.id ) ) );
		}

		[Fact]
		public void HiddenDeclaration_IsNotExported()
		{
			var (moduleB, contextB) = CompileModule( @"
module B;
[hide] func secret() -> int { return 42; }
", "B" );

			var (moduleA, contextA) = CompileModule( @"
module A;
import B;
func main() -> int { return secret(); }
", "A" );

			var (result, diagnostics) = Resolve(
				(moduleA, contextA),
				(moduleB, contextB) );

			Assert.Single( diagnostics );
			Assert.Contains( "secret", diagnostics[0].Message );
			Assert.Empty( result.Ids );
		}

		[Fact]
		public void ExternNamespace_ChildrenAreExternal()
		{
			var (module, context) = CompileModule( @"
module test;
[extern] namespace std {
    func helper() -> int;
    class vector<T>;
}
func main() -> int { return helper(); }
" );

			var std = module.scope.children["std"]
				.Select( l => l.decl as Namespace )
				.OfType<Namespace>()
				.Single();

			Assert.True( std.IsExternal, "extern namespace should be external" );
			Assert.All( std.scope.children.Values.SelectMany( l => l ),
				leaf => Assert.True( leaf.decl!.IsExternal,
					"children of extern namespace should inherit external flag" ) );
		}

		[Fact]
		public void QualifiedTypeReference_Resolves()
		{
			var (moduleB, contextB) = CompileModule( @"
module B;
namespace Ns {
    namespace Inner {
        class Foo { }
    }
}
", "B" );

			var (moduleA, contextA) = CompileModule( @"
module A;
import B;
func use() -> void { var Ns::Inner::Foo f; }
", "A" );

			var (result, diagnostics) = Resolve( (moduleA, contextA), (moduleB, contextB) );

			Assert.Empty( diagnostics );
			Assert.Single( contextA.UnresolvedTypes );
			Assert.True(
				result.Types.TryGetValue( contextA.UnresolvedTypes[0].Node, out Decl? decl ),
				"qualified type should resolve" );
			Assert.IsType<Structural>( decl );
			Assert.Equal( "Foo", decl!.name );
		}

		[Fact]
		public void QualifiedExpressionReference_Resolves()
		{
			var (moduleB, contextB) = CompileModule( @"
module B;
[extern] namespace std {
    func helper() -> int;
}
", "B" );

			var (moduleA, contextA) = CompileModule( @"
module A;
import B;
func main() -> int { return std::helper(); }
", "A" );

			var (result, diagnostics) = Resolve( (moduleA, contextA), (moduleB, contextB) );

			Assert.Empty( diagnostics );
			Assert.Single( contextA.UnresolvedScopeds );
			Assert.True(
				result.Scopeds.TryGetValue( contextA.UnresolvedScopeds[0].Node, out Decl? decl ),
				"std::helper should resolve" );
			Assert.IsType<Func>( decl );
			Assert.Equal( "helper", decl!.name );
		}

		[Fact]
		public void QualifiedPath_UnresolvedMiddleSegment_Diagnostic()
		{
			var (module, context) = CompileModule( @"
module test;
namespace Ns {
    class A { }
}
func use() -> void { var Ns::Missing::B f; }
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Single( diagnostics );
			Assert.Contains( "Missing", diagnostics[0].Message );
			Assert.Contains( "Ns", diagnostics[0].Message );
		}

		[Fact]
		public void FunctionParameter_ResolvesInBody()
		{
			var (module, context) = CompileModule( @"
module test;
func square(int x) -> int { return x * x; }
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Empty( diagnostics );
			Assert.Equal( 2, context.UnresolvedIds.Count );
			Assert.All( context.UnresolvedIds, uid => {
				Assert.True( result.Ids.TryGetValue( uid.Node, out Decl? decl ) );
				Assert.IsType<VarDecl>( decl );
				Assert.Equal( "x", decl!.name );
			} );
		}

		[Fact]
		public void LocalVar_ResolvesAfterDeclaration()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int { var int x = 5; return x; }
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Empty( diagnostics );
			Assert.Single( context.UnresolvedIds );
			Assert.True( result.Ids.TryGetValue( context.UnresolvedIds[0].Node, out Decl? decl ) );
			Assert.IsType<VarDecl>( decl );
			Assert.Equal( "x", decl!.name );
		}

		[Fact]
		public void LocalVar_BlockScoped_DoesNotLeak()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int { { var int y = 1; } return y; }
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Single( diagnostics );
			Assert.Contains( "y", diagnostics[0].Message );
		}

		[Fact]
		public void LambdaParameter_ResolvesInBody()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
    var auto lambda = func(int a) { return a + 1; };
    return lambda(5);
}
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Empty( diagnostics );
			Assert.Contains( context.UnresolvedIds, uid => uid.Node.idTplArgs.id == "a" );
			var a = context.UnresolvedIds.Single( uid => uid.Node.idTplArgs.id == "a" );
			Assert.True( result.Ids.TryGetValue( a.Node, out Decl? decl ) );
			Assert.IsType<VarDecl>( decl );
			Assert.Equal( "a", decl!.name );
		}

		[Fact]
		public void TimesLoopIndex_ResolvesInBody()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
    3 times i { return i; }
    return 0;
}
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Empty( diagnostics );
			Assert.Contains( context.UnresolvedIds, uid => uid.Node.idTplArgs.id == "i" );
			var i = context.UnresolvedIds.Single( uid => uid.Node.idTplArgs.id == "i" );
			Assert.True( result.Ids.TryGetValue( i.Node, out Decl? decl ) );
			Assert.IsType<VarDecl>( decl );
			Assert.Equal( "i", decl!.name );
		}

		[Fact]
		public void CatchParameter_ResolvesInBody()
		{
			var (module, context) = CompileModule( @"
module test;
func f() -> int {
    try { return 0; }
    catch( int e ) { return e; }
}
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Empty( diagnostics );
			Assert.Contains( context.UnresolvedIds, uid => uid.Node.idTplArgs.id == "e" );
			var e = context.UnresolvedIds.Single( uid => uid.Node.idTplArgs.id == "e" );
			Assert.True( result.Ids.TryGetValue( e.Node, out Decl? decl ) );
			Assert.IsType<VarDecl>( decl );
			Assert.Equal( "e", decl!.name );
		}

		[Fact]
		public void ExternClass_ChildrenInheritExternalFlag()
		{
			var (module, context) = CompileModule( @"
module test;
[extern] class A {
    func method();
    class Nested { }
}
" );

			var a = module.scope.children["A"]
				.Select( l => l.decl as Structural )
				.OfType<Structural>()
				.Single();

			Assert.True( a.IsExternal, "extern class should be external" );
			Assert.All( a.scope.children.Values.SelectMany( l => l ),
				leaf => Assert.True( leaf.decl!.IsExternal,
					"children of extern class should inherit external flag" ) );
		}

		[Fact]
		public void ExternNamespace_InNormalFile_ChildrenInheritExternalFlag()
		{
			var (module, context) = CompileModule( @"
module test;
[extern] namespace Ns {
    func foo();
    class Bar;
}
" );

			var ns = module.scope.children["Ns"]
				.Select( l => l.decl as Namespace )
				.OfType<Namespace>()
				.Single();

			Assert.True( ns.IsExternal, "extern namespace should be external" );
			Assert.All( ns.scope.children.Values.SelectMany( l => l ),
				leaf => Assert.True( leaf.decl!.IsExternal,
					"children of inline extern namespace should inherit external flag" ) );
		}

		[Fact]
		public void UnresolvedName_ProducesDiagnostic()
		{
			var (module, context) = CompileModule( @"
module test;
func main() -> int { return doesNotExist(); }
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Single( diagnostics );
			Assert.Equal( DiagnosticKind.Error, diagnostics[0].Kind );
			Assert.Contains( "doesNotExist", diagnostics[0].Message );
			Assert.Empty( result.Ids );
		}

		[Fact]
		public void UsingNamespace_Resolves()
		{
			var (moduleB, contextB) = CompileModule( @"
module B;
namespace Ns {
    func hiddenInNs() -> int { return 42; }
}
", "B" );

			var (moduleA, contextA) = CompileModule( @"
module A;
import B;
using Ns;
func main() -> int { return hiddenInNs(); }
", "A" );

			var (result, diagnostics) = Resolve(
				(moduleA, contextA),
				(moduleB, contextB) );

			Assert.Empty( diagnostics );
			Assert.Single( contextA.UnresolvedIds );
			Assert.True(
				result.Ids.TryGetValue( contextA.UnresolvedIds[0].Node, out Decl? decl ),
				"hiddenInNs should resolve via using namespace" );
			Assert.IsType<Func>( decl );
			Assert.Equal( "hiddenInNs", decl!.name );
		}

		[Fact]
		public void UsingSingleName_Resolves()
		{
			var (moduleB, contextB) = CompileModule( @"
module B;
namespace Ns {
    func specific() -> int { return 7; }
}
", "B" );

			var (moduleA, contextA) = CompileModule( @"
module A;
import B;
using Ns::specific;
func main() -> int { return specific(); }
", "A" );

			var (result, diagnostics) = Resolve(
				(moduleA, contextA),
				(moduleB, contextB) );

			Assert.Empty( diagnostics );
			Assert.Single( contextA.UnresolvedIds );
			Assert.True(
				result.Ids.TryGetValue( contextA.UnresolvedIds[0].Node, out Decl? decl ),
				"specific should resolve via using declaration" );
			Assert.IsType<Func>( decl );
			Assert.Equal( "specific", decl!.name );
		}

		[Fact]
		public void CrossModuleNamespaceMerging_Resolves()
		{
			var (moduleB, contextB) = CompileModule( @"
module B;
namespace Ns {
    func fromB() -> int { return 1; }
}
", "B" );

			var (moduleC, contextC) = CompileModule( @"
module C;
namespace Ns {
    func fromC() -> int { return 2; }
}
", "C" );

			var (moduleA, contextA) = CompileModule( @"
module A;
import B;
import C;
using Ns;
func main() -> int { return fromB() + fromC(); }
", "A" );

			var (result, diagnostics) = Resolve(
				(moduleA, contextA),
				(moduleB, contextB),
				(moduleC, contextC) );

			Assert.Empty( diagnostics );
			Assert.Equal( 2, contextA.UnresolvedIds.Count );
			Assert.All( contextA.UnresolvedIds, u =>
				Assert.True(
					result.Ids.TryGetValue( u.Node, out Decl? decl ),
					u.Node.idTplArgs.id + " should resolve via merged namespace" ) );
		}

		[Fact]
		public void AmbiguousImport_ProducesDiagnostic()
		{
			var (moduleB, contextB) = CompileModule( @"
module B;
func clash() -> int { return 1; }
", "B" );

			var (moduleC, contextC) = CompileModule( @"
module C;
func clash() -> int { return 2; }
", "C" );

			var (moduleA, contextA) = CompileModule( @"
module A;
import B;
import C;
func main() -> int { return clash(); }
", "A" );

			var (result, diagnostics) = Resolve(
				(moduleA, contextA),
				(moduleB, contextB),
				(moduleC, contextC) );

			Assert.Single( diagnostics );
			Assert.Equal( DiagnosticKind.Error, diagnostics[0].Kind );
			Assert.Contains( "Ambiguous", diagnostics[0].Message );
			Assert.Contains( "clash", diagnostics[0].Message );
		}
	}
}
