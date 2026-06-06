using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ProfilerStudy.Avalonia;

internal sealed class SourceViewerLauncher : ISourceViewerLauncher
{
	public bool TryLaunch(string sourceFile, int sourceLine, out string error)
	{
		error = string.Empty;
		if (string.IsNullOrWhiteSpace(sourceFile))
		{
			error = "Scope has no source file.";
			return false;
		}

		if (!File.Exists(sourceFile))
		{
			error = "Source file not found: " + sourceFile;
			return false;
		}

		string configuredViewer = Environment.GetEnvironmentVariable("PROFILER_STUDY_SOURCE_VIEWER");
		if (!string.IsNullOrWhiteSpace(configuredViewer))
		{
			return TryStart(configuredViewer, BuildConfiguredArguments(configuredViewer, sourceFile, sourceLine), false, out error);
		}

		if (TryStart("code", "-g " + Quote(sourceLine > 0 ? sourceFile + ":" + sourceLine : sourceFile), false, out error))
		{
			return true;
		}

		return TryOpenWithSystem(sourceFile, out error);
	}

	private static string BuildConfiguredArguments(string configuredViewer, string sourceFile, int sourceLine)
	{
		if (configuredViewer.Contains("{file}", StringComparison.Ordinal) ||
			configuredViewer.Contains("{line}", StringComparison.Ordinal))
		{
			return configuredViewer
				.Replace("{file}", Quote(sourceFile), StringComparison.Ordinal)
				.Replace("{line}", Math.Max(1, sourceLine).ToString(), StringComparison.Ordinal);
		}

		return Quote(sourceLine > 0 ? sourceFile + ":" + sourceLine : sourceFile);
	}

	private static bool TryOpenWithSystem(string sourceFile, out string error)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return TryStart("open", Quote(sourceFile), false, out error);
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return TryStart(sourceFile, string.Empty, true, out error);
		}

		return TryStart("xdg-open", Quote(sourceFile), false, out error);
	}

	private static bool TryStart(string fileName, string arguments, bool useShellExecute, out string error)
	{
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments ?? string.Empty,
				UseShellExecute = useShellExecute,
			};
			Process.Start(startInfo);
			error = string.Empty;
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static string Quote(string value)
	{
		return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
	}
}
