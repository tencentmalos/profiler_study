using System;

namespace ProfilerStudy.Avalonia;

public sealed class TimelineViewport : ObservableObject
{
	private int m_FrameCount;
	private int m_StartFrame;
	private int m_EndFrame;

	public int FrameCount
	{
		get => m_FrameCount;
		private set => SetProperty(ref m_FrameCount, value);
	}

	public int StartFrame
	{
		get => m_StartFrame;
		private set
		{
			if (SetProperty(ref m_StartFrame, value))
			{
				RaisePropertyChanged(nameof(VisibleFrameCount));
				RaisePropertyChanged(nameof(RangeText));
			}
		}
	}

	public int EndFrame
	{
		get => m_EndFrame;
		private set
		{
			if (SetProperty(ref m_EndFrame, value))
			{
				RaisePropertyChanged(nameof(VisibleFrameCount));
				RaisePropertyChanged(nameof(RangeText));
			}
		}
	}

	public int VisibleFrameCount => FrameCount == 0 ? 0 : EndFrame - StartFrame + 1;

	public string RangeText => FrameCount == 0 ? string.Empty : $"visible {StartFrame} - {EndFrame}";

	public static TimelineViewport CreateForFrames(int frameCount)
	{
		TimelineViewport viewport = new TimelineViewport();
		viewport.Reset(frameCount);
		return viewport;
	}

	public void Reset(int frameCount)
	{
		FrameCount = Math.Max(0, frameCount);
		StartFrame = 0;
		EndFrame = Math.Max(0, FrameCount - 1);
	}

	public void ScrollFrames(int deltaFrames)
	{
		if (FrameCount == 0 || deltaFrames == 0)
		{
			return;
		}

		int visibleCount = VisibleFrameCount;
		int maxStart = Math.Max(0, FrameCount - visibleCount);
		int newStart = Math.Max(0, Math.Min(maxStart, StartFrame + deltaFrames));
		StartFrame = newStart;
		EndFrame = Math.Min(FrameCount - 1, newStart + visibleCount - 1);
	}

	public void Zoom(double factor, double anchorRatio)
	{
		if (FrameCount == 0 || factor <= 0.0)
		{
			return;
		}

		anchorRatio = Math.Max(0.0, Math.Min(1.0, anchorRatio));
		int oldVisibleCount = Math.Max(1, VisibleFrameCount);
		int newVisibleCount = Math.Max(8, Math.Min(FrameCount, (int)Math.Round(oldVisibleCount / factor)));
		int anchorFrame = StartFrame + (int)Math.Round((oldVisibleCount - 1) * anchorRatio);
		int newStart = anchorFrame - (int)Math.Round((newVisibleCount - 1) * anchorRatio);
		newStart = Math.Max(0, Math.Min(Math.Max(0, FrameCount - newVisibleCount), newStart));

		StartFrame = newStart;
		EndFrame = Math.Min(FrameCount - 1, newStart + newVisibleCount - 1);
	}

	public bool Contains(int frameIndex)
	{
		return FrameCount != 0 && frameIndex >= StartFrame && frameIndex <= EndFrame;
	}
}
