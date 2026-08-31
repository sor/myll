using System.Collections.Generic;
using Myll.Core;

namespace Myll.Resolver
{
	public interface ITransformer
	{
		void Transform(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules,
			List<Diagnostic> diagnostics );
	}
}
