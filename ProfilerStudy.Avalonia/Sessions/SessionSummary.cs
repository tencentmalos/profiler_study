namespace ProfilerStudy.Avalonia;

internal sealed class SessionSummary
{
	public int FrameCount { get; set; }

	public double AverageFrameTimeMs { get; set; }

	public double MaxFrameTimeMs { get; set; }

	public double TargetFrameTimeMs { get; set; }

	public int FirstFrameIndex { get; set; }

	public int LastFrameIndex { get; set; }

	public int ThreadCount { get; set; }

	public string SourceName { get; set; }
}
