using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Myll.Tests
{
	public enum TestDepth
	{
		Run,
		Generate,
		GenerateFailing,
		Compile,
		CompileFailing,
		Link,
		LinkFailing,
		RunFailing,
	}

	public static class CaseConfig
	{
		public const int DefaultMyllSeconds    = 10;
		public const int DefaultCompileSeconds = 10;
		public const int DefaultRunSeconds     = 5;
		public const int DefaultGlobalFactor   = 1;

		private const string DefaultKey = "default";

		private static readonly ConfigData Config;

		static CaseConfig()
		{
			string configPath = Path.Combine(
				Path.GetFullPath( Path.Combine( AppContext.BaseDirectory, "..", "..", "..", ".." ) ),
				"testing",
				"caseconfig.json" );

			if( File.Exists( configPath ) )
			{
				try
				{
					string json = File.ReadAllText( configPath );
					JsonSerializerOptions options = new()
					{
						PropertyNameCaseInsensitive = true,
					};
					Config = JsonSerializer.Deserialize<ConfigData>( json, options ) ?? new();
				}
				catch
				{
					Config = new();
				}
			}
			else
			{
				Config = new();
			}
		}

		public static TimeSpan MyllTimeout( string caseName )
			=> ResolveTimeout( Config.Timeouts.Myll, caseName, DefaultMyllSeconds );

		public static TimeSpan CompileTimeout( string caseName )
			=> ResolveTimeout( Config.Timeouts.Compile, caseName, DefaultCompileSeconds );

		public static TimeSpan RunTimeout( string caseName )
			=> ResolveTimeout( Config.Timeouts.Run, caseName, DefaultRunSeconds );

		public static bool UseMyllCompileRun( string caseName )
			=> Config.MyllCompileRun != null && Config.MyllCompileRun.Contains( caseName );

		public static TestDepth GetDepth( string caseName )
		{
			DepthConfig d = Config.Depth;

			if( d.Generate.Contains( caseName ) )
				return TestDepth.Generate;
			if( d.GenerateFailing.Contains( caseName ) )
				return TestDepth.GenerateFailing;
			if( d.Compile.Contains( caseName ) )
				return TestDepth.Compile;
			if( d.CompileFailing.Contains( caseName ) )
				return TestDepth.CompileFailing;
			if( d.Link.Contains( caseName ) )
				return TestDepth.Link;
			if( d.LinkFailing.Contains( caseName ) )
				return TestDepth.LinkFailing;
			if( d.RunFailing.Contains( caseName ) )
				return TestDepth.RunFailing;

			return TestDepth.Run;
		}

		private static TimeSpan ResolveTimeout( Dictionary<string, int> overrides, string caseName, int fallbackSeconds )
		{
			int value = fallbackSeconds;

			if( overrides != null )
			{
				if( overrides.TryGetValue( DefaultKey, out int defaultValue ) )
					value = defaultValue;

				if( overrides.TryGetValue( caseName, out int overrideValue ) )
					value = overrideValue;
			}

			int factor = Config.Timeouts.GlobalFactor > 0 ? Config.Timeouts.GlobalFactor : DefaultGlobalFactor;
			return TimeSpan.FromSeconds( value * factor );
		}
	}

	internal class ConfigData
	{
		public TimeoutSection Timeouts { get; set; } = new();
		public HashSet<string> MyllCompileRun { get; set; } = new();
		public DepthConfig Depth { get; set; } = new();
	}

	internal class DepthConfig
	{
		public HashSet<string> Generate { get; set; } = new();
		public HashSet<string> GenerateFailing { get; set; } = new();
		public HashSet<string> Compile { get; set; } = new();
		public HashSet<string> CompileFailing { get; set; } = new();
		public HashSet<string> Link { get; set; } = new();
		public HashSet<string> LinkFailing { get; set; } = new();
		public HashSet<string> RunFailing { get; set; } = new();
	}

	internal class TimeoutSection
	{
		public int GlobalFactor { get; set; } = CaseConfig.DefaultGlobalFactor;
		public Dictionary<string, int> Myll    { get; set; } = new();
		public Dictionary<string, int> Compile { get; set; } = new();
		public Dictionary<string, int> Run     { get; set; } = new();
	}
}
