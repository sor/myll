using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Myll.Core;

namespace Myll
{
	public class Options
	{
		[Option( 'i', "in", HelpText = "List of input files, searches *.myll deeply by default")]
		public IEnumerable<string> InFiles { get; set; } = Enumerable.Empty<string>();

		[Option( 'o', "out", HelpText = "Output path, current directory by default" )]
		public string OutPath { get; init; } = Directory.GetCurrentDirectory();

		[Option( 's', "stdout", HelpText = "Output to std out", Default = false )]
		public bool IsStdOut { get; init; } = false;

		[Option( 'd', "deep", HelpText = "Search sub-directories", Default = false )]
		public bool IsDeep { get; init; } = false;

		[Option( 'k', "keep", HelpText = "Keep going when errors are encountered", Default = false )]
		public bool IsKeepGoing { get; init; } = false;

		[Option( 'n', "nofile", HelpText = "Do not generate files", Default = false )]
		public bool IsNoFile { get; init; } = false;
		public bool IsFileOut => !IsNoFile;

		[Option( 'c', "compile", HelpText = "Pass the generated .cpp files to a C++ compiler", Default = false )]
		public bool IsCompile { get; init; } = false;

		[Option( 'C', "clear", HelpText = "Clear the target directory of old *.cpp, *.h, and a.out/a.exe files", Default = false )]
		public bool IsClear { get; init; } = false;

		[Option( 'g', "debug", HelpText = "Let the C++ compiler generate a debug executable", Default = false )]
		public bool IsDebug { get; init; } = false;

		[Option( 'O', "optimize", HelpText = "Set C++ compiler optimization level", Default = 0 )]
		public int OptimizationLevel { get; init; } = 0;

		[Option( 'r', "run", HelpText = "Run the generated binary", Default = false )]
		public bool IsRun { get; init; } = false;

		[Option( 'M', "main", HelpText = "Set from which module the main func to generate", Default = "" )]
		public string MainModule { get; init; } = "";
	}

	static partial class Program
	{
		static Options ParseCommandLine( string[] args )
		{
			ParserResult<Options> result = Parser.Default.ParseArguments<Options>( args );
			// if( ret is NotParsed<Options> )
			// 	throw new Exception(
			// 		ret.Errors
			// 			.Select(
			// 				e => e switch {
			// 					TokenError te => te.Tag + " - " + te.Token,
			// 					NamedError ne => ne.Tag + " - " + ne.NameInfo,
			// 					_             => e.Tag  + " no further info",
			// 				} )
			// 			.Join( ", " ) );

			if( result.Tag == ParserResultType.NotParsed )
				Environment.Exit( -7 );

			Options opt = result.Value;

			if( opt.IsFileOut ) {
				if( opt.InFiles.IsEmpty() )
					opt.InFiles = Directory
						.GetFiles(
							Directory.GetCurrentDirectory(),
							"*.myll",
							SearchOption.AllDirectories );
				else
					opt.InFiles = opt.InFiles
						.SelectMany(
							f => Directory
								.GetFiles(
									Directory.GetCurrentDirectory(),
									f,
									opt.IsDeep
										? SearchOption.AllDirectories
										: SearchOption.TopDirectoryOnly ) )
						.Distinct();
				opt.InFiles = opt.InFiles.OrderBy( n => n );
			}
			return opt;
		}
	}
}
