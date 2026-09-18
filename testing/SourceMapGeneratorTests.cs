using System;
using System.Linq;
using Myll.Core;
using Myll.Generator;
using Xunit;

namespace Myll.Tests
{
	public sealed class SourceMapGeneratorTests
	{
		private static SrcPos Pos( string file, int line )
			=> new() {
				file = file,
				from = { line = line, col = 0 },
				to   = { line = line, col = 0 },
			};

		[Fact]
		public void EmitsFilenameOnlyOnFirstFileDirective()
		{
			SourceLines lines = new() {
				new SourceLine( "void f()", Pos( "/tmp/a.myll", 3 ) ),
				new SourceLine( "    x;", Pos( "/tmp/a.myll", 4 ) ),
			};

			string[] result = SourceMapGenerator.Apply( lines, "/tmp/gen" )
				.Select( l => l.Text )
				.ToArray();

			Assert.Equal(
				new[] {
					"#line 3 \"../a.myll\"",
					"void f()",
					"    x;",
				},
				result );
		}

		[Fact]
		public void RepeatsDirectiveWhenSameSourceLineGeneratedTwice()
		{
			SourceLines lines = new() {
				new SourceLine( "greet();", Pos( "/tmp/a.myll", 4 ) ),
				new SourceLine( "return 0;", Pos( "/tmp/a.myll", 4 ) ),
			};

			string[] result = SourceMapGenerator.Apply( lines, "/tmp/gen" )
				.Select( l => l.Text )
				.ToArray();

			Assert.Equal(
				new[] {
					"#line 4 \"../a.myll\"",
					"greet();",
					"#line 4",
					"return 0;",
				},
				result );
		}

		[Fact]
		public void NoRedundantDirectiveWhenCounterMatchesSourceLine()
		{
			SourceLines lines = new() {
				new SourceLine( "line3();", Pos( "/tmp/a.myll", 3 ) ),
				new SourceLine( "line4();", Pos( "/tmp/a.myll", 4 ) ),
			};

			string[] result = SourceMapGenerator.Apply( lines, "/tmp/gen" )
				.Select( l => l.Text )
				.ToArray();

			Assert.Equal(
				new[] {
					"#line 3 \"../a.myll\"",
					"line3();",
					"line4();",
				},
				result );
		}

		[Fact]
		public void ReEmitsAfterUnmappedLines()
		{
			SourceLines lines = new() {
				new SourceLine( "void f()", Pos( "/tmp/a.myll", 3 ) ),
				new SourceLine( "}" ),
				new SourceLine( "" ),
				new SourceLine( "x;", Pos( "/tmp/a.myll", 5 ) ),
			};

			string[] result = SourceMapGenerator.Apply( lines, "/tmp/gen" )
				.Select( l => l.Text )
				.ToArray();

			Assert.Equal(
				new[] {
					"#line 3 \"../a.myll\"",
					"void f()",
					"}",
					"",
					"#line 5",
					"x;",
				},
				result );
		}

		[Fact]
		public void EmitsFilenameWhenFileChanges()
		{
			SourceLines lines = new() {
				new SourceLine( "a();", Pos( "/tmp/a.myll", 1 ) ),
				new SourceLine( "b();", Pos( "/tmp/b.myll", 2 ) ),
				new SourceLine( "c();", Pos( "/tmp/a.myll", 3 ) ),
			};

			string[] result = SourceMapGenerator.Apply( lines, "/tmp/gen" )
				.Select( l => l.Text )
				.ToArray();

			Assert.Equal(
				new[] {
					"#line 1 \"../a.myll\"",
					"a();",
					"#line 2 \"../b.myll\"",
					"b();",
					"#line 3 \"../a.myll\"",
					"c();",
				},
				result );
		}
	}
}
