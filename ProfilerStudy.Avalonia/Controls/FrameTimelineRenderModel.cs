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

		maxDurationMs = Math.Max(1.0, maxDurationMs);
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
		int frameIndex = (int)Math.Floor(frameCoordinate);
		return TryGetItem(frameIndex, out item);
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
		List<FrameTimelinePixelBar> bars = frameWidth < 1.0
			? CreateDenseBars(model, bounds, frameWidth, maxDurationMs)
			: CreateFrameBars(model.Items, model.StartFrame, bounds, frameWidth, maxDurationMs);

		double targetLineY = bounds.Bottom - Math.Min(bounds.Height, Math.Max(0.0, model.TargetFrameMs) * bounds.Height / maxDurationMs);
		return new FrameTimelinePixelLayout(model, bounds, bars, frameWidth, targetLineY);
	}

	private static List<FrameTimelinePixelBar> CreateFrameBars(
		IReadOnlyList<FrameTimelineRenderItem> items,
		int startFrame,
		Rect bounds,
		double frameWidth,
		double maxDurationMs)
	{
		List<FrameTimelinePixelBar> bars = new List<FrameTimelinePixelBar>(items.Count);
		foreach (FrameTimelineRenderItem item in items)
		{
			double x = bounds.X + ((item.Index - startFrame) * frameWidth);
			double height = Math.Max(1.0, Math.Min(bounds.Height, item.DurationMs * bounds.Height / maxDurationMs));
			double y = bounds.Bottom - height;
			double width = Math.Max(1.0, frameWidth);
			bars.Add(new FrameTimelinePixelBar(item, new Rect(x, y, width, height)));
		}

		return bars;
	}

	private static List<FrameTimelinePixelBar> CreateDenseBars(
		FrameTimelineRenderModel model,
		Rect bounds,
		double frameWidth,
		double maxDurationMs)
	{
		int columnCount = Math.Max(1, (int)Math.Ceiling(bounds.Width));
		FrameTimelineRenderItem[] columnItems = new FrameTimelineRenderItem[columnCount];
		bool[] hasColumnItem = new bool[columnCount];
		foreach (FrameTimelineRenderItem item in model.Items)
		{
			int column = (int)Math.Floor((item.Index - model.StartFrame) * frameWidth);
			column = Math.Max(0, Math.Min(columnCount - 1, column));
			if (hasColumnItem[column] is false || IsMoreImportantFrame(item, columnItems[column]))
			{
				columnItems[column] = item;
				hasColumnItem[column] = true;
			}
		}

		List<FrameTimelinePixelBar> bars = new List<FrameTimelinePixelBar>(columnCount);
		for (int i = 0; i < columnItems.Length; i++)
		{
			if (hasColumnItem[i] is false)
			{
				continue;
			}

			FrameTimelineRenderItem item = columnItems[i];
			double height = Math.Max(1.0, Math.Min(bounds.Height, item.DurationMs * bounds.Height / maxDurationMs));
			double y = bounds.Bottom - height;
			bars.Add(new FrameTimelinePixelBar(item, new Rect(bounds.X + i, y, 1.0, height)));
		}

		return bars;
	}

	private static bool IsMoreImportantFrame(FrameTimelineRenderItem candidate, FrameTimelineRenderItem current)
	{
		if (candidate.Category != current.Category)
		{
			return candidate.Category > current.Category;
		}

		if (candidate.DurationMs != current.DurationMs)
		{
			return candidate.DurationMs > current.DurationMs;
		}

		return candidate.Index < current.Index;
	}

	public bool TryHit(Point point, out FrameTimelineRenderItem item)
	{
		if (Bounds.Contains(point) is false)
		{
			item = default;
			return false;
		}

		if (FrameWidth < 1.0)
		{
			foreach (FrameTimelinePixelBar bar in Bars)
			{
				if (point.X >= bar.Rect.X && point.X < bar.Rect.Right)
				{
					item = bar.Item;
					return true;
				}
			}

			item = default;
			return false;
		}

		double frameCoordinate = GetFrameCoordinate(point);
		return Model.TryHitFrameCoordinate(frameCoordinate, out item);
	}

	public bool TryGetOverlayRect(int frameIndex, out Rect rect)
	{
		if (FrameWidth < 1.0)
		{
			foreach (FrameTimelinePixelBar bar in Bars)
			{
				if (bar.Item.Index == frameIndex)
				{
					rect = new Rect(bar.Rect.X, Bounds.Y, bar.Rect.Width, Bounds.Height);
					return true;
				}
			}

			rect = default;
			return false;
		}

		if (Model.TryGetItem(frameIndex, out _) is false)
		{
			rect = default;
			return false;
		}

		double x = Bounds.X + ((frameIndex - Model.StartFrame) * FrameWidth);
		rect = new Rect(x, Bounds.Y, Math.Max(1.0, FrameWidth), Bounds.Height);
		return true;
	}

	public double GetFrameCoordinate(Point point)
	{
		return Model.StartFrame + ((point.X - Bounds.X) / Math.Max(double.Epsilon, FrameWidth));
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
