using Avalonia;
using System;

namespace ProfilerStudy.Avalonia;

internal static class Program
{
	public static void Main(string[] args)
	{
		if (args != null && args.Length >= 1 && args[0] == "--smoke-test")
		{
			string profilerPath = args.Length >= 2 ? args[1] : null;
			Environment.Exit(AvaloniaSmokeTest.Run(profilerPath));
		}

		App.LoadSampleOnStartup = args != null && Array.IndexOf(args, "--load-sample") >= 0;
		App.StartupProfilerPath = GetStartupProfilerPath(args);
		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	private static string GetStartupProfilerPath(string[] args)
	{
		if (args == null)
		{
			return null;
		}

		foreach (string arg in args)
		{
			if (string.IsNullOrWhiteSpace(arg) || arg.StartsWith("--", StringComparison.Ordinal))
			{
				continue;
			}

			return arg;
		}

		return null;
	}

	private static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
	}
}
