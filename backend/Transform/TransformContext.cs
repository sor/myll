using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
{
	/// <summary>
	/// Per-module input passed to a <see cref="Transformer"/> hook.
	/// </summary>
	public sealed class TransformContext
	{
		public CompiledModuleResult Module { get; }                    // read-only: per-module input
		public ResolutionResult? ResolutionResult { get; }              // read-only; null pre-resolution
		public bool IsPrototype => Module.Context.IsPrototypeFile;    // read-only convenience

		public TransformContext( CompiledModuleResult module, ResolutionResult? resolutionResult )
		{
			Module            = module;
			ResolutionResult  = resolutionResult;
		}
	}
}
