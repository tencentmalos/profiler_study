using System;
using System.Collections.Generic;

namespace ProfilerStudy.Avalonia;

internal readonly struct FrameTimelineRenderItem
{
	public FrameTimelineRenderItem(FrameSample sample, CoreUtils.FrameTimeCategory category)
	{
		Sample = sample;
		Category = category;
	}

	public FrameSample Sample { get; }

	public CoreUtils.FrameTimeCategory Category { get; }

	public int Index => Sample.Index;

	public double DurationMs => Sample.DurationMs;
}

internal sealed class FrameTimelineRenderModel
{
	public static readonly FrameTimelineRenderModel Empty = new FrameTimelineRenderModel(
		Array.Empty<FrameTimelineRenderItem>(),
		0,
		0,
		0.0,
		0.0);

	private FrameTimelineRenderModel(
		IReadOnlyList<FrameTimelineRenderItem> items,
		int startFrame,
		int endFrame,
		double targetFrameMs,
		double maxDurationMs)
	{
		Items = items;
		StartFrame = startFrame;
		EndFrame = endFrame;
		TargetFrameMs = targetFrameMs;
		MaxDurationMs = maxDurationMs;
	}

	public IReadOnlyList<FrameTimelineRenderItem> Items { get; }

	public int StartFrame { get; }

	public int EndFrame { get; }

	public double TargetFrameMs { get; }

	public double MaxDurationMs { get; }

	public bool HasItems => Items.Count > 0;

	public double BarSize => Items.Count <= 180 ? 0.86 : 1.0;

	public static FrameTimelineRenderModel Create(IReadOnlyList<FrameSample> samples, TimelineViewport viewport, double targetFrameMs)
	{
		if (samples == null || samples.Count == 0 || viewport == null || viewport.FrameCount == 0)
		{
			return Empty;
		}

		int startFrame = Math.Max(0, viewport.StartFrame);
		int endFrame = Math.Min(viewport.EndFrame, samples[samples.Count - 1].Index);
		List<FrameTimelineRenderItem> items = new List<FrameTimelineRenderItem>();
		double maxDurationMs = 0.0;
		for (int i = 0; i < samples.Count; i++)
		{
			FrameSample sample = samples[i];
			if (sample.Index < startFrame)
			{
				continue;
			}
			if (sample.Index > endFrame)
			{
				break;
			}

			maxDurationMs = Math.Max(maxDurationMs, sample.DurationMs);
			items.Add(new FrameTimelineRenderItem(
				sample,
				FrameTimelineFrameClassifier.GetFrameTimeCategory(sample.DurationMs, targetFrameMs)));
		}

		if (items.Count == 0)
		{
			return new FrameTimelineRenderModel(
				Array.Empty<FrameTimelineRenderItem>(),
				startFrame,
				endFrame,
				targetFrameMs,
				0.0);
		}

		maxDurationMs = Math.Max(Math.Max(1.0, targetFrameMs * 2.0), maxDurationMs);
		return new FrameTimelineRenderModel(items, startFrame, endFrame, targetFrameMs, maxDurationMs);
	}

	public bool TryGetItem(int frameIndex, out FrameTimelineRenderItem item)
	{
		int left = 0;
		int right = Items.Count - 1;
		while (left <= right)
		{
			int mid = left + ((right - left) / 2);
			int midFrameIndex = Items[mid].Index;
			if (midFrameIndex == frameIndex)
			{
				item = Items[mid];
				return true;
			}
			if (midFrameIndex < frameIndex)
			{
				left = mid + 1;
			}
			else
			{
				right = mid - 1;
			}
		}

		item = default;
		return false;
	}
}

internal static class FrameTimelineFrameClassifier
{
	public static CoreUtils.FrameTimeCategory GetFrameTimeCategory(double durationMs, double targetFrameMs)
	{
		if (targetFrameMs <= 0.0)
		{
			return CoreUtils.FrameTimeCategory.InBudget;
		}

		double percentOverTarget = durationMs * 100.0 / targetFrameMs - 100.0;
		if (percentOverTarget > 100.0)
		{
			return CoreUtils.FrameTimeCategory.Alert;
		}
		if (percentOverTarget > 0.0)
		{
			return CoreUtils.FrameTimeCategory.Warning;
		}
		return CoreUtils.FrameTimeCategory.InBudget;
	}
}
