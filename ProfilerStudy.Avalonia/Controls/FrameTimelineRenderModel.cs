using System;
using System.Collections.Generic;
using Avalonia;

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
		for (int i = LowerBoundFrameIndex(samples, startFrame); i < samples.Count; i++)
		{
			FrameSample sample = samples[i];
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

	private static int LowerBoundFrameIndex(IReadOnlyList<FrameSample> samples, int frameIndex)
	{
		int left = 0;
		int right = samples.Count;
		while (left < right)
		{
			int mid = left + ((right - left) / 2);
			if (samples[mid].Index < frameIndex)
			{
				left = mid + 1;
			}
			else
			{
				right = mid;
			}
		}

		return left;
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

	public bool TryHitFrameCoordinate(double frameCoordinate, out FrameTimelineRenderItem item)
	{
		int frameIndex = (int)Math.Floor(frameCoordinate + 0.5);
		if (TryGetItem(frameIndex, out item) is false)
		{
			return false;
		}

		return frameCoordinate >= item.Index - 0.5 && frameCoordinate < item.Index + 0.5;
	}
}

internal readonly struct FrameTimelinePixelBar
{
	public FrameTimelinePixelBar(FrameTimelineRenderItem item, Rect rect)
	{
		Item = item;
		Rect = rect;
	}

	public FrameTimelineRenderItem Item { get; }

	public Rect Rect { get; }
}

internal sealed class FrameTimelinePixelLayout
{
	public static readonly FrameTimelinePixelLayout Empty = new FrameTimelinePixelLayout(
		FrameTimelineRenderModel.Empty,
		default,
		Array.Empty<FrameTimelinePixelBar>(),
		0.0,
		0.0);

	private FrameTimelinePixelLayout(
		FrameTimelineRenderModel model,
		Rect bounds,
		IReadOnlyList<FrameTimelinePixelBar> bars,
		double frameWidth,
		double targetLineY)
	{
		Model = model;
		Bounds = bounds;
		Bars = bars;
		FrameWidth = frameWidth;
		TargetLineY = targetLineY;
	}

	public FrameTimelineRenderModel Model { get; }

	public Rect Bounds { get; }

	public IReadOnlyList<FrameTimelinePixelBar> Bars { get; }

	public double FrameWidth { get; }

	public double TargetLineY { get; }

	public bool HasBars => Bars.Count > 0;

	public static FrameTimelinePixelLayout Create(FrameTimelineRenderModel model, Rect bounds)
	{
		if (model == null || model.HasItems is false || bounds.Width <= 0.0 || bounds.Height <= 0.0)
		{
			return Empty;
		}

		int visibleFrameCount = Math.Max(1, model.EndFrame - model.StartFrame + 1);
		double frameWidth = bounds.Width / visibleFrameCount;
		double maxDurationMs = Math.Max(1.0, model.MaxDurationMs);
		List<FrameTimelinePixelBar> bars = new List<FrameTimelinePixelBar>(model.Items.Count);
		foreach (FrameTimelineRenderItem item in model.Items)
		{
			double x = bounds.X + ((item.Index - model.StartFrame) * frameWidth);
			double height = Math.Max(1.0, Math.Min(bounds.Height, item.DurationMs * bounds.Height / maxDurationMs));
			double y = bounds.Bottom - height;
			double width = Math.Max(1.0, frameWidth);
			bars.Add(new FrameTimelinePixelBar(item, new Rect(x, y, width, height)));
		}

		double targetLineY = bounds.Bottom - Math.Min(bounds.Height, Math.Max(0.0, model.TargetFrameMs) * bounds.Height / maxDurationMs);
		return new FrameTimelinePixelLayout(model, bounds, bars, frameWidth, targetLineY);
	}

	public bool TryHit(Point point, out FrameTimelineRenderItem item)
	{
		if (Bounds.Contains(point) is false)
		{
			item = default;
			return false;
		}

		double frameCoordinate = GetFrameCoordinate(point);
		int frameIndex = (int)Math.Floor(frameCoordinate);
		return Model.TryGetItem(frameIndex, out item);
	}

	public double GetFrameCoordinate(Point point)
	{
		return Model.StartFrame + ((point.X - Bounds.X) / Math.Max(1.0, FrameWidth));
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
