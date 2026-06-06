using System;

namespace ProfilerStudy.Avalonia;

internal sealed class ScopeHotspotRow
{
	public ScopeHotspotRow(string name, double totalTimeMs, long totalCount, double maxTimePerFrameMs, long maxCountPerFrame)
	{
		Name = string.IsNullOrWhiteSpace(name) ? "(unnamed scope)" : name;
		TotalTimeMs = totalTimeMs;
		TotalCount = totalCount;
		MaxTimePerFrameMs = maxTimePerFrameMs;
		MaxCountPerFrame = maxCountPerFrame;
		AverageTimeMs = totalCount <= 0 ? 0.0 : totalTimeMs / totalCount;
	}

	public string Name { get; }

	public double TotalTimeMs { get; }

	public long TotalCount { get; }

	public double AverageTimeMs { get; }

	public double MaxTimePerFrameMs { get; }

	public long MaxCountPerFrame { get; }

	public string TotalTimeText => FormatMs(TotalTimeMs);

	public string AverageTimeText => FormatMs(AverageTimeMs);

	public string MaxTimePerFrameText => FormatMs(MaxTimePerFrameMs);

	public string TotalCountText => TotalCount.ToString("N0");

	public string MaxCountPerFrameText => MaxCountPerFrame.ToString("N0");

	private static string FormatMs(double value)
	{
		return value <= 0.0 ? "-" : Math.Round(value, 3).ToString("0.###");
	}
}
