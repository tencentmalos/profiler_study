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
		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	private static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
	}
}
