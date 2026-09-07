namespace Myll.Core
{
	public enum RawImportKind
	{
		System,
		Local,
	}

	public class RawImport
	{
		public RawImportKind kind;
		public string        path   = null!;
		public SrcPos        srcPos = null!;
	}

	public abstract class ImportSpec
	{
		public SrcPos srcPos = null!;
	}

	public class NamedImportSpec : ImportSpec
	{
		public string name = null!;
	}

	public class RawImportSpec : ImportSpec
	{
		public RawImport raw = null!;
	}
}
