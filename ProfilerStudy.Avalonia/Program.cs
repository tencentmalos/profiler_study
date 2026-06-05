using Avalonia;
using System;

namespace ProfilerStudy.Avalonia;

internal static class Program
{
	public static void Main(string[] args)
	{
		if (args != null && args.Length == 1 && args[0] == "--smoke-test")
		{
			Environment.Exit(AvaloniaSmokeTest.Run());
		}

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
