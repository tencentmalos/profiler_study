namespace ProfilerStudy.Tracy;

public sealed class TracyFrameSummary
{
	public TracyFrameSummary(int frameIndex, long start, long end)
	{
		FrameIndex = frameIndex;
		Start = start;
		End = end;
	}

	public int FrameIndex { get; }

	public long Start { get; }

	public long End { get; }

	public long Duration => End >= Start ? End - Start : 0L;
}
