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

			ModuleContext context = new() { Dialect = dialect ?? Myll.Core.Dialect.Default };
			IGrouping<string, MyllParser.ProgContext> group = new[] { prog }
				.GroupBy( _ => moduleName )
				.First();

			GlobalNamespace module = context.DeclVisitor.VisitProgs( group );
			context.Module = module;
			return new( context );
		}

		private static ResolveResult Resolve( params CompiledModuleResult[] modules )
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
func use( B b ) -> B { var B x; return x; }
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
		public void ImportedRedefinition_ProducesDiagnostic()
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

			// Two same-signature definitions from two imported modules are a C++ ODR
			// violation, not an overload: report the later definition as the error and
			// the earlier one as the note.
			Assert.Single( diagnostics, d => d.Kind == DiagnosticKind.Error
			                               && d.Message.Contains( "Redefinition of 'clash'" )
			                               && d.Message.Contains( "module 'B'" ) );
			Assert.Contains( diagnostics, d =>
				d.Kind == DiagnosticKind.Note
				&& d.Message.Contains( "clash" ) );
		}

		[Fact]
		public void InvalidVarAttributes_ProduceDiagnostics()
		{
			var (module, context) = CompileModule( @"
module test;
[static] var int staticGlobal = 1;
[inline, hide] var int inlineHiddenGlobal = 2;
[extern] var const int externConstGlobal = 3;
struct S {
	[hide] var int hiddenField;
	[inline] var int inlineNonStaticField;
}
func main() -> int { return 0; }
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Equal( 6, diagnostics.Count );
			Assert.All( diagnostics, d => Assert.Equal( DiagnosticKind.Error, d.Kind ) );
		}

		[Fact]
		public void DuplicateField_ProducesDiagnostic()
		{
			var (module, context) = CompileModule( @"
module test;
struct S {
	var int dupField;
	var int dupField;
}
func main() -> int { return 0; }
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Single( diagnostics );
			Assert.Equal( DiagnosticKind.Error, diagnostics[0].Kind );
			Assert.Contains( "dupField", diagnostics[0].Message );
		}

		[Fact]
		public void StrictNew_BareNew_ProducesDiagnostic()
		{
			var (module, context) = CompileModule(
				@"
module test;
func main() -> int {
	var int * p = new int;
	return 0;
}
",
				dialect: new Dialect { StrictNew = true } );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Single( diagnostics );
			Assert.Equal( DiagnosticKind.Error, diagnostics[0].Kind );
			Assert.Contains( "StrictNew", diagnostics[0].Message );
		}

		[Fact]
		public void ClassTemplateArityMismatch_MarksTypeInvalid()
		{
			var (module, context) = CompileModule( @"
module test;
class Box<T> {
method get() -> T;
}
func main() -> int {
	var Box<int, float> b = Box<int, float>();
	return 0;
}
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Single( diagnostics );
			Assert.Contains( "Template argument count mismatch for type 'Box'", diagnostics[0].Message );

			var boxType = context.UnresolvedTypes.Single( ut => ut.Node.idTpls.Last().id == "Box" ).Node;
			Assert.False( result.Types.ContainsKey( boxType ) );
			Assert.True( result.InvalidTypes.ContainsKey( boxType ) );
		}

		[Fact]
		public void FunctionTemplateArityMismatch_MarksCallInvalid()
		{
			var (module, context) = CompileModule( @"
module test;
func identity<T>(T value) -> T { return value; }
func main() -> int {
	identity<int, float>( 1 );
	return 0;
}
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Single( diagnostics );
			Assert.Contains( "Template argument count mismatch for 'identity'", diagnostics[0].Message );

			var callee = context.UnresolvedIds.Single( u => u.Node.idTplArgs.id == "identity" ).Node;
			Assert.True( result.InvalidCalls.ContainsKey( callee ) );
			Assert.Equal( InvalidCallKind.TemplateArityMismatchSingle, result.InvalidCalls[callee].Kind );
		}

		[Fact]
		public void NoMatchingOverload_MarksCallInvalid()
		{
			var (module, context) = CompileModule( @"
module test;
func bar(int x) -> int { return x; }
func main() -> int {
	bar( 1, 2 );
	return 0;
}
" );

			var (result, diagnostics) = Resolve( (module, context) );

			Assert.Single( diagnostics );
			Assert.Contains( "No matching overload for 'bar'", diagnostics[0].Message );

			var callee = context.UnresolvedIds.Single( u => u.Node.idTplArgs.id == "bar" ).Node;
			Assert.True( result.InvalidCalls.ContainsKey( callee ) );
			Assert.Equal( InvalidCallKind.NoMatchingOverload, result.InvalidCalls[callee].Kind );
		}
	}
}
