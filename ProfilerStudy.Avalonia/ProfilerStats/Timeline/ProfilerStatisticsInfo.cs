namespace ProfilerStudy.Avalonia.ProfilerStats.Timeline;

public sealed class ProfilerStatisticsInfo
{
	[PlotContainer("Frame", true)]
	public ProfilerFrameStatistics MainStats { get; private set; } = new ProfilerFrameStatistics();
}

public sealed class ProfilerFrameStatistics
{
	[CurveField("Frame Duration", "ms", 0)]
	public double DurationMs { get; set; }
}
