using Avalonia;

namespace ProfilerStudy.Avalonia;

internal static class Program
{
	public static void Main(string[] args)
	{
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
