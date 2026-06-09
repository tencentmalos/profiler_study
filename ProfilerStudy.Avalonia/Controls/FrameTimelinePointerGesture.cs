using System;

namespace ProfilerStudy.Avalonia;

internal static class FrameTimelinePointerGesture
{
	private const double DragThresholdPixels = 4.0;

	public static bool IsDragDistanceExceeded(double pressX, double currentX)
	{
		return Math.Abs(currentX - pressX) >= DragThresholdPixels;
	}

	public static int CalculatePanDeltaFrames(double currentX, double pressX, int visibleFrameCount, double graphWidth)
	{
		visibleFrameCount = Math.Max(1, visibleFrameCount);
		graphWidth = Math.Max(1.0, graphWidth);
		return (int)Math.Round(-(currentX - pressX) * visibleFrameCount / graphWidth);
	}
}
