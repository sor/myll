using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Generator
{
	public static class RawImportCollector
	{
		public static IReadOnlyDictionary<string, IReadOnlyList<RawImport>> Collect(
			IReadOnlyList<CompiledModuleResult> modules )
		{
			Dictionary<string, List<RawImport>> rawByModule = modules.ToDictionary(
				m => m.Module.module,
				m => m.Module.imports.OfType<RawImportSpec>().Select( s => s.raw ).ToList() );

			Dictionary<string, CompiledModuleResult> moduleByName = modules.ToDictionary( m => m.Module.module );

			foreach( CompiledModuleResult module in modules.Where( m => m.Context.IsPrototypeFile ) ) {
				HashSet<string> visited = new();
				Queue<string>   queue   = new();

				foreach( NamedImportSpec named in module.Module.imports.OfType<NamedImportSpec>() )
					queue.Enqueue( named.name );

				while( queue.Count > 0 ) {
					string importedName = queue.Dequeue();
					if( !visited.Add( importedName ) )
						continue;

					if( rawByModule.TryGetValue( importedName, out List<RawImport>? nestedRaws ) )
						rawByModule[module.Module.module].AddRange( nestedRaws );

					if( moduleByName.TryGetValue( importedName, out CompiledModuleResult? importedModule )
					 && importedModule.Context.IsPrototypeFile ) {
						foreach( NamedImportSpec nested in importedModule.Module.imports.OfType<NamedImportSpec>() )
							queue.Enqueue( nested.name );
					}
				}
			}

			return rawByModule.ToDictionary(
				kv => kv.Key,
				kv => (IReadOnlyList<RawImport>)kv.Value );
		}
	}
}
