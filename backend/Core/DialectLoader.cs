using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Myll.Core
{
	/// <summary>
	/// Loads named dialects from a set of dialect directories. Each dialect is stored as a
	/// JSONC file and applied on top of the internal <see cref="Dialect.Default"/> values.
	/// Dialects are cached for the lifetime of the loader.
	/// </summary>
	public sealed class DialectLoader
	{
		private readonly IReadOnlyList<string> searchDirectories;
		private readonly object                          loadLock    = new();
		private readonly Dictionary<string, Dialect>     cache      = new();
		private readonly Dictionary<string, Diagnostics> diagnostics = new();

		private sealed class Diagnostics
		{
			public List<Diagnostic> Items { get; init; } = new();
		}

		public DialectLoader( IEnumerable<string> searchDirectories )
		{
			this.searchDirectories = searchDirectories
				.Select( d => d.TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ) )
				.ToList();
		}

		/// <summary>
		/// Loads the named dialect. Returns a cached result on subsequent calls.
		/// Diagnostics are returned once per dialect.
		/// </summary>
		public Dialect Load( string name, out IReadOnlyList<Diagnostic> outDiagnostics )
		{
			lock( loadLock ) {
				if( cache.TryGetValue( name, out Dialect? cached ) ) {
					outDiagnostics = diagnostics[name].Items;
					return cached;
				}

				Dialect result = LoadUncached( name, out List<Diagnostic> loadedDiagnostics );
				cache.Add( name, result );
				diagnostics.Add( name, new Diagnostics { Items = loadedDiagnostics } );
				outDiagnostics = loadedDiagnostics;
				return result;
			}
		}

		private Dialect LoadUncached( string name, out List<Diagnostic> loadedDiagnostics )
		{
			loadedDiagnostics = new List<Diagnostic>();
			string? path = FindDialectFile( name );

			if( path == null ) {
				loadedDiagnostics.Add( new Diagnostic(
					null,
					DiagnosticKind.Error,
					String.Format( "Dialect '{0}' was not found in any dialect search directory.", name ) ) );
				return Dialect.Default;
			}

			Dialect result = new();
			IReadOnlyDictionary<string, PropertyInfo> knownProperties = GetKnownProperties();

			try {
				JsonDocumentOptions options = new() {
					AllowTrailingCommas = true,
					CommentHandling     = JsonCommentHandling.Skip,
				};

				using JsonDocument document = JsonDocument.Parse( File.ReadAllText( path ), options );
				JsonElement root = document.RootElement;

				if( root.ValueKind != JsonValueKind.Object ) {
					loadedDiagnostics.Add( new Diagnostic(
						null,
						DiagnosticKind.Error,
						String.Format( "Dialect file '{0}' must contain a JSON object.", path ) ) );
					return Dialect.Default;
				}

				HashSet<string> seenKeys = new();
				List<string> unknownKeys = new();

				foreach( JsonProperty property in root.EnumerateObject() ) {
					string key = property.Name;

					if( knownProperties.TryGetValue( key, out PropertyInfo? prop ) ) {
						seenKeys.Add( key );
						object? value = ParseValue( prop.PropertyType, property.Value );
						prop.SetValue( result, value );
					}
					else {
						unknownKeys.Add( key );
					}
				}

				if( unknownKeys.Count > 0 ) {
					loadedDiagnostics.Add( new Diagnostic(
						null,
						DiagnosticKind.Warning,
						String.Format(
							"Dialect '{0}' contains unknown keys: {1}. They will be ignored.",
							name,
							String.Join( ", ", unknownKeys.OrderBy( k => k ) ) ) ) );
				}

				List<string> missingKeys = knownProperties.Keys
					.Except( seenKeys )
					.OrderBy( k => k )
					.ToList();

				if( missingKeys.Count > 0 ) {
					loadedDiagnostics.Add( new Diagnostic(
						null,
						DiagnosticKind.Warning,
						String.Format(
							"Dialect '{0}' does not specify {1}: {2}. Internal defaults will be used.",
							name,
							missingKeys.Count == 1 ? "key" : "keys",
							String.Join( ", ", missingKeys ) ) ) );
				}
			}
			catch( JsonException ex ) {
				loadedDiagnostics.Add( new Diagnostic(
					null,
					DiagnosticKind.Error,
					String.Format( "Failed to parse dialect file '{0}': {1}", path, ex.Message ) ) );
				return Dialect.Default;
			}
			catch( Exception ex ) {
				loadedDiagnostics.Add( new Diagnostic(
					null,
					DiagnosticKind.Error,
					String.Format( "Failed to load dialect '{0}': {1}", name, ex.Message ) ) );
				return Dialect.Default;
			}

			return result;
		}

		private string? FindDialectFile( string name )
		{
			foreach( string dir in searchDirectories ) {
				if( !Directory.Exists( dir ) )
					continue;

				string path = Path.Combine( dir, name + ".jsonc" );
				if( File.Exists( path ) )
					return path;
			}

			return null;
		}

		private static IReadOnlyDictionary<string, PropertyInfo> GetKnownProperties()
		{
			PropertyInfo[] properties = typeof( Dialect )
				.GetProperties( BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly )
				.Where( p => p.CanRead && p.GetSetMethod()?.IsPublic == true )
				.Where( p => p.Name != "EqualityContract" )
				.ToArray();

			return properties.ToDictionary( p => ToCamelCase( p.Name ), p => p );
		}

		private static string ToCamelCase( string name )
		{
			if( String.IsNullOrEmpty( name ) )
				return name;

			return Char.ToLowerInvariant( name[0] ) + name.Substring( 1 );
		}

		private static object? ParseValue( Type targetType, JsonElement element )
		{
			if( targetType == typeof( string ) )
				return element.GetString();

			if( targetType == typeof( bool ) )
				return element.GetBoolean();

			if( targetType.IsEnum )
				return ParseEnum( targetType, element.GetString()! );

			if( IsDictionary( targetType, out Type? keyType, out Type? valueType ) )
				return ParseDictionary( keyType!, valueType!, element );

			throw new NotSupportedException(
				String.Format( "Dialect property type '{0}' is not supported.", targetType.Name ) );
		}

		private static object ParseEnum( Type enumType, string text )
		{
			string trimmed = text.Trim();

			if( enumType.IsDefined( typeof( FlagsAttribute ), inherit: false ) ) {
				long combined = 0;
				foreach( string part in trimmed.Split( ',' ) ) {
					string name = part.Trim();
					if( String.IsNullOrEmpty( name ) )
						continue;

					object value = Enum.Parse( enumType, name, ignoreCase: false );
					combined |= Convert.ToInt64( value );
				}

				return Enum.ToObject( enumType, combined );
			}

			return Enum.Parse( enumType, trimmed, ignoreCase: false );
		}

		private static bool IsDictionary( Type type, out Type? keyType, out Type? valueType )
		{
			keyType   = null;
			valueType = null;

			if( !type.IsGenericType )
				return false;

			Type generic = type.GetGenericTypeDefinition();
			if( generic != typeof( IReadOnlyDictionary<,> ) && generic != typeof( Dictionary<,> ) )
				return false;

			Type[] args = type.GetGenericArguments();
			keyType   = args[0];
			valueType = args[1];
			return true;
		}

		private static object ParseDictionary( Type keyType, Type valueType, JsonElement element )
		{
			if( element.ValueKind != JsonValueKind.Object )
				throw new JsonException( "Expected a JSON object for dictionary property." );

			Type dictType = typeof( Dictionary<,> ).MakeGenericType( keyType, valueType );
			object dict = Activator.CreateInstance( dictType )!;
			MethodInfo addMethod = dictType.GetMethod( "Add" )!;

			foreach( JsonProperty property in element.EnumerateObject() ) {
				object key   = keyType.IsEnum
					? ParseEnum( keyType, property.Name )
					: Convert.ChangeType( property.Name, keyType )!;
				object value = ParseValue( valueType, property.Value )!;

				addMethod.Invoke( dict, new[] { key, value } );
			}

			return dict;
		}
	}
}
