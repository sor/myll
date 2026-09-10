using System.Collections.Generic;
using Myll.Resolver;

namespace Myll.Transform
{
	/// <summary>
	/// Base class for AST transforms that run before and/or after name resolution.
	/// </summary>
	public abstract class Transformer
	{
		/// <summary>
		/// read/write: accumulator for one Do*Transform invocation.
		/// DoPreTransform/DoPostTransform clear it, the hook fills it,
		/// and the caller consumes it immediately.
		/// </summary>
		public List<Diagnostic> Diagnostics { get; } = new();

		public List<Diagnostic> DoPreTransform( TransformContext ctx )
		{
			Diagnostics.Clear();
			if( RunInPrototypeFiles || !ctx.IsPrototype )
				PreTransform( ctx );
			return Diagnostics;
		}

		public List<Diagnostic> DoPostTransform( TransformContext ctx )
		{
			Diagnostics.Clear();
			if( RunInPrototypeFiles || !ctx.IsPrototype )
				PostTransform( ctx );
			return Diagnostics;
		}

		protected virtual void PreTransform( TransformContext ctx ) { }
		protected virtual void PostTransform( TransformContext ctx ) { }

		protected virtual bool RunInPrototypeFiles => false;
	}
}
