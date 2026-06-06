namespace ProfilerStudy.Avalonia.DeviceInfo.Timeline;

public sealed class DeviceStatisticsInfo
{
	[PlotContainer("Frame", true)]
	public DeviceInfoFrameStatistics MainStats { get; private set; } = new DeviceInfoFrameStatistics();
}

public sealed class DeviceInfoFrameStatistics
{
	[CurveField("Frame Duration", "ms", 0)]
	public double DurationMs { get; set; }
}
