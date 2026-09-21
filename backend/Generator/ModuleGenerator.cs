using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Generator
{
	public static class ModuleGenerator
	{
		public static GeneratedModuleFilesResult GenerateForModule(
			ModuleContext                                           context,
			IReadOnlyDictionary<string, IReadOnlyList<RawImport>> moduleRawImports,
			IReadOnlySet<string>                                    prototypeModules,
			string                                                  outputDirectory = "" )
		{
			GlobalNamespace global_ns = context.Module!;
			List<(string Path, IEnumerable<SourceLine> Lines)> ret = new();

			HierarchicalGen gen = new( global_ns, -1, 0, moduleRawImports, prototypeModules, context );
			try {
				if( context.SourceMap.Any ) {
					// Source-mapped output is checked against golden files, so make the
					// ordering of top-level declarations deterministic.
					global_ns.children.Sort( ( a, b ) => {
						string fileA = a.srcPos?.file ?? "";
						string fileB = b.srcPos?.file ?? "";
						int cmp = StringComparer.Ordinal.Compare( fileA, fileB );
						if( cmp != 0 )
							return cmp;
						return ( a.srcPos?.from.line ?? 0 )
							.CompareTo( b.srcPos?.from.line ?? 0 );
					} );
				}

				global_ns.AddToGen( gen );

				IEnumerable<SourceLine>? decl = gen.GenDeclGlobal();
				IEnumerable<SourceLine>? impl = gen.GenImplGlobal();

				if( context.SourceMap.Stmt ) {
					string basePath = string.IsNullOrEmpty( outputDirectory )
						? Directory.GetCurrentDirectory()
						: outputDirectory;
					decl = decl != null ? SourceMapGenerator.Apply( decl, basePath ) : decl;
					impl = impl != null ? SourceMapGenerator.Apply( impl, basePath ) : impl;
				}

				if( context.SourceMap.Decl ) {
					decl = decl != null ? SourceMapGenerator.NormalizeFileDirectives( decl ) : decl;
					impl = impl != null ? SourceMapGenerator.NormalizeFileDirectives( impl ) : impl;
				}

				if( decl != null )
					ret.Add( (String.Format( "{0}.hpp", global_ns.module ), decl) );
				if( impl != null )
					ret.Add( (String.Format( "{0}.cpp", global_ns.module ), impl) );
			}
			catch( DiagnosticException ex ) {
				gen.Diagnostics.Add( ex.Diagnostic );
			}

			return new( ret, gen.Diagnostics );
		}
	}
}
