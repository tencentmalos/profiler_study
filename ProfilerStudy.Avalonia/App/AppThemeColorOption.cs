using Avalonia.Media;

namespace ProfilerStudy.Avalonia;

internal sealed class AppThemeColorOption
{
	public AppThemeColorOption(string name, IBrush primaryBrush = null)
	{
		Name = string.IsNullOrWhiteSpace(name) ? "Blue" : name;
		PrimaryBrush = primaryBrush;
	}

	public string Name { get; }

	public IBrush PrimaryBrush { get; }
}
