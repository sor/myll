using System;
using System.Collections.Generic;
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
			IReadOnlySet<string>                                    prototypeModules )
		{
			GlobalNamespace global_ns = context.Module!;
			List<(string Path, IEnumerable<string> Lines)> ret = new();

			HierarchicalGen gen = new( global_ns, -1, 0, moduleRawImports, prototypeModules, context );
			try {
				global_ns.AddToGen( gen );

				IEnumerable<string> decl = gen.GenDeclGlobal();
				IEnumerable<string>? impl = gen.GenImplGlobal();
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
