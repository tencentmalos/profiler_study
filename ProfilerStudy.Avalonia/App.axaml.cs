using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ProfilerStudy.Avalonia;

public sealed class App : Application
{
	public static bool LoadSampleOnStartup { get; set; }

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainWindowViewModel(LoadSampleOnStartup)
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}
