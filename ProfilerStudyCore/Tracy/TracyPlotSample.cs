namespace ProfilerStudy.Tracy;

public sealed class TracyPlotSample
{
	public TracyPlotSample(long time, double value)
	{
		Time = time;
		Value = value;
	}

	public long Time { get; }

	public double Value { get; }
}
