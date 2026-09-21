namespace Myll.Core
{
	/// <summary>
	/// Controls which source-map modes are active for a module compilation.
	/// A <c>#line</c> directive can be emitted at declaration boundaries,
	/// statement boundaries, or both.
	/// </summary>
	public sealed record SourceMapOptions( bool Decl = false, bool Stmt = false )
	{
		public bool Any => Decl || Stmt;
	}
}
