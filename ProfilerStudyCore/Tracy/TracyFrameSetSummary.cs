namespace ProfilerStudy.Tracy;

public sealed class TracyFrameSetSummary
{
	public TracyFrameSetSummary(ulong name, bool continuous, int frameCount, long firstFrameStart, long lastFrameEnd)
	{
		Name = name;
		Continuous = continuous;
		FrameCount = frameCount;
		FirstFrameStart = firstFrameStart;
		LastFrameEnd = lastFrameEnd;
	}

	public ulong Name { get; }

	public bool Continuous { get; }

	public int FrameCount { get; }

	public long FirstFrameStart { get; }

	public long LastFrameEnd { get; }
}
