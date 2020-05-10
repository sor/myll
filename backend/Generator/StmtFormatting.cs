using System.Collections.Generic;
using Myll.Core;

namespace Myll.Generator
{
	using Strings     = List<string>;
	using IntToString = Dictionary<int, string>;

	internal static class StmtFormatting
	{
		internal static readonly Strings DefaultIncludes = new Strings {
			"#pragma once",
			"#include <memory>",      // smart pointer (expensive)
			"#include <utility>",     // move, pair, swap
			"#include <cmath>",       // math
			"#include <type_traits>", // underlying_type
			//	"#include <iostream>",  // in and output
			//	"#include <vector>",    // dynamically sized array
			//	"#include <map>",    	// hash map (expensive)
			//	"#include <algorithm>", // algorithms
		};

		//internal static readonly string IndentString = "\t";
		internal const string IndentString = "    ";
		internal const string ThrowFormat = "{0}throw {1};";
		internal const string BreakFormat = "{0}break;";
		internal const string CurlyOpen = "{0}{{";
		internal const string CurlyClose = "{0}}}";
		internal const string CurlyCloseSC = "{0}}};";

		public static readonly Dictionary<Access, string>
			AccessFormat = new Dictionary<Access, string> {
				{ Access.Private,   "{0}  private:" },
				{ Access.Protected, "{0}  protected:" },
				{ Access.Public,    "{0}  public:" },
			};

		public static readonly string[] ReturnFormat = {
			"{0}return;",
			"{0}return {1};",
		};

		public static readonly string[] IfFormat = {
			"{0}if( {1} )",
			"{0}else if( {1} )",
			"{0}else",
		};

		public static readonly string[] LoopFormat = {
			"{0}for( {1} {2}; {3} )", // first semicolon is part of the stmt
			"{0}while( {1} )",
			"{0}do",
			"{0}while( {1} );",
		};

		public static readonly string[] VarFormat = {
			"{0}{1}{2}{3}{4};", // 0 indent, 1 static , 2 typename, 3 type & name, 4 init
			"static ",
			"typename ",
			" = ",
			"{0}{1}{2},", // 0 indent, 1 name, 2 init
		};

		public static readonly string[] FuncFormat = {
			"{0}{1}{2} {3}({4}){5}", // function:  0 indent, 1 leading attributes, 2 return type, 3 name, 4 params, 5 trailing attributes
			"{0}{1}{2}({3}){4}",     // ctor/dtor: 0 indent, 1 leading attributes, 2 name, 3 params, 4 trailing attributes
		};

		public static readonly string[] StructFormat = {
			"{0}{1}{2} {3}{4}{5}", // 0 indent, 1 keyword, 2 attributes, 3 name, 4 final, 5 bases or semicolon
			" : {0}{1}",           // first base; 0 virtual and/or ppp, 1 name
			", {0}{1}",            // other bases; 0 virtual and/or ppp, 1 name
			"struct",
			"class",
			"union",
			"enum class",
			"namespace",
		};

		public static readonly IReadOnlyDictionary<TypespecBasic.Kind, IntToString>
			BasicFormat = new Dictionary<TypespecBasic.Kind, IntToString> {
				{
					TypespecBasic.Kind.Auto, new IntToString {
						{ TypespecBasic.SizeUndetermined, "auto" }
					}
				}, {
					TypespecBasic.Kind.Void, new IntToString {
						{ TypespecBasic.SizeInvalid, "void" }
					}
				}, {
					TypespecBasic.Kind.Bool, new IntToString {
						{ 1, "bool" }
					}
				}, {
					TypespecBasic.Kind.Char, new IntToString {
						{ 1, "char" }, // TODO char8_t
						{ 4, "char32_t" },
					}
				}, {
					TypespecBasic.Kind.String, new IntToString {
						{ TypespecBasic.SizeUndetermined, "std::string" }
					}
				}, {
					TypespecBasic.Kind.Float, new IntToString {
						{ 2, "half" },
						{ 4, "float" },
						{ 8, "double" },
						{ 16, "long double" },
					}
				}, {
					TypespecBasic.Kind.Binary, new IntToString {
						{ 1, "std::byte" },
						{ 2, "std::uint16_t" },
						{ 4, "std::uint32_t" },
						{ 8, "std::uint64_t" },
					}
				}, {
					TypespecBasic.Kind.Integer, new IntToString {
						{ 1, "std::int8_t" },
						{ 2, "std::int16_t" },
						{ 4, "int" },
						{ 8, "std::int64_t" },
					}
				}, {
					TypespecBasic.Kind.Unsigned, new IntToString {
						{ 1, "std::uint8_t" },
						{ 2, "std::uint16_t" },
						{ 4, "unsigned int" },
						{ 8, "std::uint64_t" },
					}
				},
				/* TODO {
					TypespecBasic.Kind.Size, new IntToString {
						{ 1, "std::intptr_t" },
						{ 2, "std::uintptr_t" },
					}
				},*/
			};
	}
}
