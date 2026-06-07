using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ProfilerStudy.Avalonia;

public sealed class ThreadTimelineLaneControl : Control
{
	private const double LeftPadding = 8.0;
	private const double RightPadding = 8.0;
	private const double TopPadding = 8.0;
	private const double BottomPadding = 8.0;
	private const double ThreadLabelWidth = 160.0;
	private const double AxisHeight = 18.0;
	private const double LaneHeight = 16.0;
	private const double LaneGap = 6.0;
	private const double MinBarWidth = 1.5;

	private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(207, 207, 207));
	private static readonly IBrush LabelBackgroundBrush = new SolidColorBrush(Color.FromRgb(190, 190, 190));
	private static readonly IBrush EmptyTextBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
	private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(16, 16, 16));
	private static readonly IBrush MutedTextBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
	private static readonly Pen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(124, 124, 124)), 1.0);
	private static readonly Pen LanePen = new Pen(new SolidColorBrush(Color.FromRgb(150, 150, 150)), 1.0);
	private static readonly Pen AxisPen = new Pen(new SolidColorBrush(Color.FromRgb(112, 112, 112)), 1.0);

	public static readonly StyledProperty<IReadOnlyList<ThreadTimelineScopeRow>> RowsProperty =
		AvaloniaProperty.Register<ThreadTimelineLaneControl, IReadOnlyList<ThreadTimelineScopeRow>>(nameof(Rows));

	public static readonly StyledProperty<TimelineViewport> ViewportProperty =
		AvaloniaProperty.Register<ThreadTimelineLaneControl, TimelineViewport>(nameof(Viewport));

	public static readonly StyledProperty<TimelineSelection> SelectionProperty =
		AvaloniaProperty.Register<ThreadTimelineLaneControl, TimelineSelection>(nameof(Selection));

	static ThreadTimelineLaneControl()
	{
		AffectsRender<ThreadTimelineLaneControl>(RowsProperty, ViewportProperty, SelectionProperty);
	}

	public IReadOnlyList<ThreadTimelineScopeRow> Rows
	{
		get => GetValue(RowsProperty);
		set => SetValue(RowsProperty, value);
	}

	public TimelineViewport Viewport
	{
		get => GetValue(ViewportProperty);
		set => SetValue(ViewportProperty, value);
	}

	public TimelineSelection Selection
	{
		get => GetValue(SelectionProperty);
		set => SetValue(SelectionProperty, value);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		IReadOnlyList<ThreadTimelineScopeRow> rows = Rows ?? Array.Empty<ThreadTimelineScopeRow>();
		int threadCount = Math.Max(1, rows.Select(item => item.ThreadName).Distinct(StringComparer.Ordinal).Count());
		double desiredHeight = TopPadding + AxisHeight + BottomPadding + (threadCount * LaneHeight) + ((threadCount - 1) * LaneGap);
		return new Size(availableSize.Width, Math.Max(118.0, desiredHeight));
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		Rect bounds = Bounds;
		context.FillRectangle(BackgroundBrush, bounds);
		context.DrawRectangle(null, BorderPen, bounds.Deflate(0.5));

		int startFrame = Viewport?.StartFrame ?? 0;
		int endFrame = Math.Max(startFrame + 1, Viewport?.EndFrame ?? startFrame + 1);
		double axisLeft = LeftPadding + ThreadLabelWidth;
		double axisRight = Math.Max(axisLeft + 1.0, bounds.Width - RightPadding);
		double plotWidth = Math.Max(1.0, axisRight - axisLeft);
		DrawAxis(context, axisLeft, axisRight, TopPadding, startFrame, endFrame);
		DrawSelection(context, axisLeft, axisRight, TopPadding + AxisHeight, bounds.Height - BottomPadding, startFrame, endFrame);

		IReadOnlyList<ThreadTimelineScopeRow> rows = Rows ?? Array.Empty<ThreadTimelineScopeRow>();
		if (rows.Count == 0)
		{
			DrawText(context, "No scope timeline data in the visible frame range.", EmptyTextBrush, 12.0, LeftPadding, TopPadding + AxisHeight + 8.0);
			return;
		}

		double y = TopPadding + AxisHeight;
		foreach (IGrouping<string, ThreadTimelineScopeRow> threadRows in rows
			.GroupBy(item => item.ThreadName, StringComparer.Ordinal)
			.OrderBy(group => group.Min(item => item.StartFrame)))
		{
			if (y + LaneHeight > bounds.Height - BottomPadding)
			{
				break;
			}

			var labelRect = new Rect(LeftPadding, y, ThreadLabelWidth - 4.0, LaneHeight);
			context.FillRectangle(LabelBackgroundBrush, labelRect);
			context.DrawRectangle(null, LanePen, labelRect);
			DrawText(context, threadRows.Key, TextBrush, 10.0, labelRect.X + 4.0, labelRect.Y + 1.0);

			var laneRect = new Rect(axisLeft, y, plotWidth, LaneHeight);
			context.DrawRectangle(null, LanePen, laneRect);

			foreach (ThreadTimelineScopeRow row in threadRows.OrderBy(item => item.StartFrame).ThenBy(item => item.Depth))
			{
				double x = axisLeft + ((row.StartFrame - startFrame) / Math.Max(1.0, endFrame - startFrame + 1.0) * plotWidth);
				double width = Math.Max(MinBarWidth, (row.EndFrame - row.StartFrame) / Math.Max(1.0, endFrame - startFrame + 1.0) * plotWidth);
				double depthInset = Math.Min(7.0, row.Depth * 1.1);
				Rect barRect = new Rect(
					x,
					y + 2.0 + depthInset,
					Math.Min(width, axisRight - x),
					Math.Max(3.0, LaneHeight - 4.0 - depthInset));
				if (barRect.Width <= 0.0 || barRect.Height <= 0.0)
				{
					continue;
				}

				IBrush fill = new SolidColorBrush(GetScopeColor(row.Depth));
				context.FillRectangle(fill, barRect);
				context.DrawRectangle(null, BorderPen, barRect);
				if (barRect.Width > 58.0)
				{
					DrawText(context, row.Name, TextBrush, 9.0, barRect.X + 3.0, barRect.Y);
				}
			}

			y += LaneHeight + LaneGap;
		}
	}

	private void DrawSelection(DrawingContext context, double left, double right, double top, double bottom, int startFrame, int endFrame)
	{
		int selectedFrame = Selection?.SelectedFrameIndex ?? -1;
		if (selectedFrame < startFrame || selectedFrame > endFrame)
		{
			return;
		}

		double x = left + ((selectedFrame - startFrame) / Math.Max(1.0, endFrame - startFrame + 1.0) * (right - left));
		var pen = new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 60)), 1.0);
		context.DrawLine(pen, new Point(x, top), new Point(x, bottom));
	}

	private static void DrawAxis(DrawingContext context, double left, double right, double top, int startFrame, int endFrame)
	{
		double axisY = top + AxisHeight - 4.0;
		context.DrawLine(AxisPen, new Point(left, axisY), new Point(right, axisY));

		for (int i = 0; i <= 4; ++i)
		{
			double ratio = i / 4.0;
			double x = left + ((right - left) * ratio);
			int frame = (int)Math.Round(startFrame + ((endFrame - startFrame) * ratio));
			context.DrawLine(AxisPen, new Point(x, axisY), new Point(x, axisY - 5.0));
			DrawText(context, frame.ToString(CultureInfo.InvariantCulture), MutedTextBrush, 9.0, x + 2.0, top);
		}
	}

	private static Color GetScopeColor(int depth)
	{
		Color[] palette =
		{
			Color.FromRgb(115, 181, 255),
			Color.FromRgb(99, 215, 155),
			Color.FromRgb(255, 180, 79),
			Color.FromRgb(255, 126, 139),
			Color.FromRgb(192, 162, 255),
			Color.FromRgb(122, 218, 218),
		};
		return palette[Math.Abs(depth) % palette.Length];
	}

	private static void DrawText(DrawingContext context, string text, IBrush brush, double fontSize, double x, double y)
	{
		var formattedText = new FormattedText(
			text ?? string.Empty,
			CultureInfo.CurrentCulture,
			FlowDirection.LeftToRight,
			Typeface.Default,
			fontSize,
			brush);
		context.DrawText(formattedText, new Point(x, y));
	}
}
