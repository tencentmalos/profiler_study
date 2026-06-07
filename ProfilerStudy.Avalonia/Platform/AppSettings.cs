using System.Collections.Generic;

namespace ProfilerStudy.Avalonia;

internal sealed class AppSettings
{
	public List<string> RecentFiles { get; set; } = new List<string>();

	public string CapturedSourceRoot { get; set; } = string.Empty;

	public string LocalSourceRoot { get; set; } = string.Empty;

	public string BaseTheme { get; set; } = "Light";

	public string ColorTheme { get; set; } = "Blue";
}
