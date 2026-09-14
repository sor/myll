using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Myll.Core;
using Myll.Generator;

namespace Myll
{
	public static class CompilerPipeline
	{
		public static string ClassifyModule( MyllParser.ProgContext c )
		{
			FileInfo fi = new( c.Start.InputStream.SourceName );
			return c.module()?.id().GetText()
			    ?? Path.GetFileNameWithoutExtension( fi.Name );
		}

		public static CompiledModuleResult CompileModule(
			IGrouping<string, MyllParser.ProgContext> progContext,
			bool                                      isPrototypeFile )
		{
			ModuleContext context = new() {
				IsPrototypeFile = isPrototypeFile,
				Dialect         = Dialect.Default,
			};

			GlobalNamespace module = context.DeclVisitor.VisitProgs( progContext );
			context.Module = module;
			return new( context );
		}

		public static List<CompiledModuleResult> CompileModules( IEnumerable<ParseResult> parseResults )
		{
			return parseResults
				.Select( r => r.Prog )
				.GroupBy( ClassifyModule )
				.AsParallel()
				.Select( g => CompileModule(
					g,
					g.All( p => IsPrototypeFile( p.Start.InputStream.SourceName ) ) ) )
				.ToList();
		}

		public static bool IsPrototypeFile( string path )
			=> path.EndsWith( ".d.myll", StringComparison.OrdinalIgnoreCase )
			|| path.EndsWith( ".decl.myll", StringComparison.OrdinalIgnoreCase )
			|| path.EndsWith( ".extern.myll", StringComparison.OrdinalIgnoreCase );

		public static List<GeneratedModuleFilesResult> GenerateFiles(
			List<CompiledModuleResult>                              modules,
			IReadOnlyDictionary<string, IReadOnlyList<RawImport>> moduleRawImports,
			IReadOnlySet<string>                                    prototypeModules )
		{
			return modules
				.Where( m => !m.Context.IsPrototypeFile )
				.AsParallel()
				.Select( m => ModuleGenerator.GenerateForModule(
					m.Context, moduleRawImports, prototypeModules ) )
				.ToList();
		}
	}
}
