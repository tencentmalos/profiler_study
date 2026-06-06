using System.Collections.Generic;

namespace ProfilerStudy.Avalonia;

internal sealed class AppSettings
{
	public List<string> RecentFiles { get; set; } = new List<string>();
}
