using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Myll.Tests
{
	public sealed record ProcessResult(
		int ExitCode,
		string StdOut,
		string StdErr,
		TimeSpan Duration,
		bool TimedOut
	);

	public static class ProcessRunner
	{
		public static ProcessResult Run(
			string fileName,
			string arguments,
			string? workingDirectory = null,
			IReadOnlyDictionary<string, string>? environment = null,
			TimeSpan? timeout = null )
		{
			timeout ??= TimeSpan.FromSeconds( 30 );

			ProcessStartInfo psi = new() {
				FileName               = fileName,
				Arguments              = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				UseShellExecute        = false,
				CreateNoWindow         = true,
			};

			if( workingDirectory != null )
				psi.WorkingDirectory = workingDirectory;

			if( environment != null )
				foreach( var pair in environment )
					psi.Environment[pair.Key] = pair.Value;

			using var process = Process.Start( psi )
			             ?? throw new InvalidOperationException( "Failed to start process: " + fileName );

			StringBuilder output = new();
			StringBuilder error  = new();

			process.OutputDataReceived += ( _, e ) => { if( e.Data != null ) output.AppendLine( e.Data ); };
			process.ErrorDataReceived  += ( _, e ) => { if( e.Data != null ) error.AppendLine( e.Data ); };

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			var sw = Stopwatch.StartNew();
			bool timedOut = false;

			if( !process.WaitForExit( (int)timeout.Value.TotalMilliseconds ) )
			{
				try { process.Kill( true ); } catch { /* ignore */ }
				timedOut = true;
			}

			// Give a small grace period for async stream readers to finish
			if( !timedOut )
				process.WaitForExit( 5000 );

			sw.Stop();

			return new ProcessResult(
				ExitCode: timedOut ? -1 : process.ExitCode,
				StdOut: output.ToString().TrimEnd(),
				StdErr: error.ToString().TrimEnd(),
				Duration: sw.Elapsed,
				TimedOut: timedOut
			);
		}
	}
}
