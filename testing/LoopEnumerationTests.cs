using System.Linq;
using Myll.Core;
using Xunit;

namespace Myll.Tests
{
	public sealed class LoopEnumerationTests
	{
		[Fact]
		public void ForStmt_EnumerateDF_IncludesBody()
		{
			var body = new ExprStmt { expr = new Literal { text = "42" } };
			var forStmt = new ForStmt {
				init = new EmptyStmt(),
				cond = new Literal { text = "true" },
				iter = new Literal { text = "++i" },
				body = body,
			};

			var enumerated = forStmt.DescendantsAndSelf().ToList();

			Assert.Contains( body, enumerated );
			Assert.Contains( forStmt, enumerated );
		}

		[Fact]
		public void WhileStmt_EnumerateDF_IncludesBody()
		{
			var body = new ExprStmt { expr = new Literal { text = "42" } };
			var whileStmt = new WhileStmt {
				cond = new Literal { text = "true" },
				body = body,
			};

			var enumerated = whileStmt.DescendantsAndSelf().ToList();

			Assert.Contains( body, enumerated );
			Assert.Contains( whileStmt, enumerated );
		}

		[Fact]
		public void DoWhileStmt_EnumerateDF_IncludesBody()
		{
			var body = new ExprStmt { expr = new Literal { text = "42" } };
			var doWhileStmt = new DoWhileStmt {
				cond = new Literal { text = "true" },
				body = body,
			};

			var enumerated = doWhileStmt.DescendantsAndSelf().ToList();

			Assert.Contains( body, enumerated );
			Assert.Contains( doWhileStmt, enumerated );
		}

		[Fact]
		public void TimesStmt_EnumerateDF_IncludesBody()
		{
			var body = new ExprStmt { expr = new Literal { text = "42" } };
			var timesStmt = new TimesStmt {
				count = new Literal { text = "3" },
				body  = body,
			};

			var enumerated = timesStmt.DescendantsAndSelf().ToList();

			Assert.Contains( body, enumerated );
			Assert.Contains( timesStmt, enumerated );
		}
	}
}
