namespace ProfilerStudy.Avalonia;

public readonly struct FrameSample
{
	public FrameSample(int index, double durationMs)
	{
		Index = index;
		DurationMs = durationMs;
	}

	public int Index { get; }

	public double DurationMs { get; }
}
