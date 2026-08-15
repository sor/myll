namespace Myll.Core
{
	// Toggle flags here to select a dialect. These are compile-time switches for now;
	// in the future they may move to a config file or CLI options.
	public static class Dialect
	{
		// When true, `new T` is illegal; you must write `new T*` (or another explicit pointer type).
		// When false, `new T` gets an implicit raw pointer, matching the C++-style default.
		public static bool StrictNew = false;
	}
}
