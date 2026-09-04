using System.Collections.Generic;
using Myll.Core;

namespace Myll.Resolver
{
	public interface ITransformer
	{
		void Transform(
			IReadOnlyList<CompiledModuleResult> modules,
			List<Diagnostic> diagnostics );
	}
}
