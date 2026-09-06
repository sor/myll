using System.Collections.Generic;

namespace Myll.Core
{
	/// <summary>
	/// Defines the standard C++ modules that Myll's built-in constructs lower to.
	/// These are imported implicitly by the compiler front-end so that Myll can
	/// resolve members and overloads for the types that back the language syntax.
	/// </summary>
	public static class StdBuiltins
	{
		/// <summary>
		/// Module names that are imported into every compiled module. Each name
		/// corresponds to a <c>std/*.decl.myll</c> prototype file and to a C++
		/// standard-library header that the generator emits as an include.
		/// </summary>
		public static readonly IReadOnlyList<string> ImplicitStdImports = new List<string> {
			"std_cmath",
			"std_cstddef",
			"std_cstdint",
			"std_initializer_list",
			"std_memory",
			"std_string",
			"std_type_traits",
			"std_utility",
		};
	}
}
