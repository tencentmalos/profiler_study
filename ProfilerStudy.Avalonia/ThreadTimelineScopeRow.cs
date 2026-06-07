namespace ProfilerStudy.Avalonia;

public sealed class ThreadTimelineScopeRow
{
	public ThreadTimelineScopeRow(int frameIndex, string threadName, int depth, string name, double startFrame, double endFrame)
	{
		FrameIndex = frameIndex;
		ThreadName = string.IsNullOrWhiteSpace(threadName) ? "(unnamed thread)" : threadName;
		Depth = depth < 0 ? 0 : depth;
		Name = string.IsNullOrWhiteSpace(name) ? "(unnamed scope)" : name;
		StartFrame = startFrame;
		EndFrame = endFrame < startFrame ? startFrame : endFrame;
	}

	public int FrameIndex { get; }

	public string ThreadName { get; }

	public int Depth { get; }

	public string Name { get; }

	public double StartFrame { get; }

	public double EndFrame { get; }
}
