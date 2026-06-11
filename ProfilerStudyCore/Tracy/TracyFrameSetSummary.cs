using System.Collections.Generic;

namespace ProfilerStudy.Tracy;

public sealed class TracyFrameSetSummary
{
	public TracyFrameSetSummary(ulong name, bool continuous, IReadOnlyList<TracyFrameSummary> frames)
	{
		Name = name;
		Continuous = continuous;
		Frames = frames;
	}

	public ulong Name { get; }

	public bool Continuous { get; }

	public IReadOnlyList<TracyFrameSummary> Frames { get; }

	public int FrameCount => Frames.Count;

	public long FirstFrameStart => Frames.Count == 0 ? 0L : Frames[0].Start;

	public long LastFrameEnd => Frames.Count == 0 ? 0L : Frames[Frames.Count - 1].End;
}
