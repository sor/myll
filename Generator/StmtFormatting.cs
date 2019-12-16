using System.Collections.Generic;
using System.Collections.ObjectModel;
using Myll.Core;

namespace Myll.Generator
{
	public static class StmtFormatting
	{
		public static readonly string Indent = "\t";
		public static readonly string[] ReturnFormat = {
			"{0}return;",
			"{0}return {1};",
		};
		public static readonly string ThrowFormat = "{0}throw {1};";
		public static readonly string BreakFormat = "{0}break;";
		public static readonly string[] IfFormat = {
			"{0}if( {1} )",
			"{0}else if( {1} )",
			"{0}else",
		};

		public static readonly string[] VarFormat = {
			"{0}{1}{2}{3};", // 0 indent, 1 typename, 2 type & name, 3 init
			"typename ",
			" = ",
		};

		public static readonly IReadOnlyDictionary<Core.TypespecBasic.Kind, Dictionary<int, string>>
			BasicFormat = new Dictionary<TypespecBasic.Kind, Dictionary<int, string>> {
				{
					TypespecBasic.Kind.Auto, new Dictionary<int, string> {
						{ TypespecBasic.SizeUndetermined, "auto" }
					}
				}, {
					TypespecBasic.Kind.Void, new Dictionary<int, string> {
						{ TypespecBasic.SizeInvalid, "void" }
					}
				}, {
					TypespecBasic.Kind.Bool, new Dictionary<int, string> {
						{ 1, "bool" }
					}
				}, {
					TypespecBasic.Kind.Char, new Dictionary<int, string> {
						{ 1, "char" }, // TODO char8_t
						{ 4, "char32_t" },
					}
				}, {
					TypespecBasic.Kind.String, new Dictionary<int, string> {
						{ TypespecBasic.SizeUndetermined, "std::string" }
					}
				}, {
					TypespecBasic.Kind.Float, new Dictionary<int, string> {
						{ 2, "half" },
						{ 4, "float" },
						{ 8, "double" },
						{ 16, "long double" },
					}
				}, {
					TypespecBasic.Kind.Binary, new Dictionary<int, string> {
						{ 1, "std::byte" },
						{ 2, "std::uint16_t" },
						{ 4, "std::uint32_t" },
						{ 8, "std::uint64_t" },
					}
				}, {
					TypespecBasic.Kind.Integer, new Dictionary<int, string> {
						{ 1, "std::int8_t" },
						{ 2, "std::int16_t" },
						{ 4, "int" },
						{ 8, "std::int64_t" },
					}
				}, {
					TypespecBasic.Kind.Unsigned, new Dictionary<int, string> {
						{ 1, "std::uint8_t" },
						{ 2, "std::uint16_t" },
						{ 4, "unsigned int" },
						{ 8, "std::uint64_t" },
					}
				},
				/* TODO {
					TypespecBasic.Kind.Size, new Dictionary<int, string> {
						{ 1, "std::intptr_t" },
						{ 2, "std::uintptr_t" },
					}
				},*/
			};
	}
}
