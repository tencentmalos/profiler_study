using System;

namespace ProfilerStudy.Avalonia;

internal readonly struct FrameTimelineRange
{
	public FrameTimelineRange(int startFrame, int endFrame)
	{
		StartFrame = startFrame;
		EndFrame = endFrame;
	}

	public int StartFrame { get; }

	public int EndFrame { get; }
}

internal static class FrameTimelineNavigation
{
	public static FrameTimelineRange CenterRange(int frameIndex, int frameCount, int visibleFrameCount)
	{
		frameCount = Math.Max(0, frameCount);
		if (frameCount == 0)
		{
			return new FrameTimelineRange(0, 0);
		}

		visibleFrameCount = Math.Max(1, Math.Min(frameCount, visibleFrameCount));
		frameIndex = Math.Clamp(frameIndex, 0, frameCount - 1);
		int startFrame = Math.Max(0, frameIndex - (visibleFrameCount / 2));
		int endFrame = Math.Min(frameCount - 1, startFrame + visibleFrameCount - 1);
		startFrame = Math.Max(0, endFrame - visibleFrameCount + 1);
		return new FrameTimelineRange(startFrame, endFrame);
	}
}
