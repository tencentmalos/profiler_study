using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ProfilerStudy.Avalonia;

public sealed class SelectedFrameThreadLaneControl : Control
{
	private const double LeftPadding = 8.0;
	private const double RightPadding = 8.0;
	private const double TopPadding = 8.0;
	private const double BottomPadding = 8.0;
	private const double ThreadLabelWidth = 150.0;
	private const double LaneHeight = 18.0;
	private const double ThreadGap = 7.0;
	private const double MinBarWidth = 2.0;
	private const double TimeAxisHeight = 18.0;

	private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(207, 207, 207));
	private static readonly IBrush LabelBackgroundBrush = new SolidColorBrush(Color.FromRgb(190, 190, 190));
	private static readonly IBrush EmptyTextBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
	private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(16, 16, 16));
	private static readonly IBrush MutedTextBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
	private static readonly Pen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(124, 124, 124)), 1.0);
	private static readonly Pen LanePen = new Pen(new SolidColorBrush(Color.FromRgb(150, 150, 150)), 1.0);
	private static readonly Pen AxisPen = new Pen(new SolidColorBrush(Color.FromRgb(112, 112, 112)), 1.0);

	public static readonly StyledProperty<IReadOnlyList<ScopeFrameDetailRow>> RowsProperty =
		AvaloniaProperty.Register<SelectedFrameThreadLaneControl, IReadOnlyList<ScopeFrameDetailRow>>(nameof(Rows));

	static SelectedFrameThreadLaneControl()
	{
		AffectsRender<SelectedFrameThreadLaneControl>(RowsProperty);
	}

	public IReadOnlyList<ScopeFrameDetailRow> Rows
	{
		get => GetValue(RowsProperty);
		set => SetValue(RowsProperty, value);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		IReadOnlyList<ScopeFrameDetailRow> rows = Rows ?? Array.Empty<ScopeFrameDetailRow>();
		int threadCount = Math.Max(1, rows.Select(item => item.ThreadName).Distinct(StringComparer.Ordinal).Count());
		double desiredHeight = TopPadding + TimeAxisHeight + BottomPadding + (threadCount * LaneHeight) + ((threadCount - 1) * ThreadGap);
		return new Size(availableSize.Width, Math.Max(96.0, desiredHeight));
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		Rect bounds = Bounds;
		context.FillRectangle(BackgroundBrush, bounds);
		context.DrawRectangle(null, BorderPen, bounds.Deflate(0.5));

		IReadOnlyList<ScopeFrameDetailRow> rows = Rows ?? Array.Empty<ScopeFrameDetailRow>();
		if (rows.Count == 0)
		{
			DrawText(context, "Select a frame with scope data to show thread lanes.", EmptyTextBrush, 12.0, LeftPadding, TopPadding);
			return;
		}

		double maxEndMs = rows.Max(item => item.StartOffsetMs + Math.Max(0.0, item.DurationMs));
		if (maxEndMs <= 0.0)
		{
			DrawText(context, "Selected frame has no measurable scope duration.", EmptyTextBrush, 12.0, LeftPadding, TopPadding);
			return;
		}

		double axisLeft = LeftPadding + ThreadLabelWidth;
		double axisRight = Math.Max(axisLeft + 1.0, bounds.Width - RightPadding);
		double plotWidth = Math.Max(1.0, axisRight - axisLeft);
		DrawAxis(context, axisLeft, axisRight, TopPadding, maxEndMs);

		double y = TopPadding + TimeAxisHeight;
		foreach (IGrouping<string, ScopeFrameDetailRow> threadRows in rows
			.Where(item => item.DurationMs > 0.0)
			.GroupBy(item => item.ThreadName, StringComparer.Ordinal)
			.OrderBy(group => group.Min(item => item.StartOffsetMs)))
		{
			if (y + LaneHeight > bounds.Height - BottomPadding)
			{
				break;
			}

			var labelRect = new Rect(LeftPadding, y, ThreadLabelWidth - 4.0, LaneHeight);
			context.FillRectangle(LabelBackgroundBrush, labelRect);
			context.DrawRectangle(null, LanePen, labelRect);
			DrawText(context, threadRows.Key, TextBrush, 10.0, labelRect.X + 4.0, labelRect.Y + 2.0);

			var laneRect = new Rect(axisLeft, y, plotWidth, LaneHeight);
			context.DrawRectangle(null, LanePen, laneRect);

			foreach (ScopeFrameDetailRow row in threadRows.OrderBy(item => item.StartOffsetMs).ThenBy(item => item.Depth))
			{
				double x = axisLeft + (Math.Max(0.0, row.StartOffsetMs) / maxEndMs * plotWidth);
				double width = Math.Max(MinBarWidth, row.DurationMs / maxEndMs * plotWidth);
				double depthInset = Math.Min(7.0, row.Depth * 1.2);
				Rect barRect = new Rect(
					x,
					y + 2.0 + depthInset,
					Math.Min(width, axisRight - x),
					Math.Max(4.0, LaneHeight - 4.0 - depthInset));
				if (barRect.Width <= 0.0 || barRect.Height <= 0.0)
				{
					continue;
				}

				IBrush fill = new SolidColorBrush(GetScopeColor(row.Depth));
				context.FillRectangle(fill, barRect);
				context.DrawRectangle(null, BorderPen, barRect);
				if (barRect.Width > 54.0)
				{
					DrawText(context, row.Name, TextBrush, 9.0, barRect.X + 4.0, barRect.Y + 1.0);
				}
			}

			y += LaneHeight + ThreadGap;
		}
	}

	private static void DrawAxis(DrawingContext context, double left, double right, double top, double maxEndMs)
	{
		double axisY = top + TimeAxisHeight - 4.0;
		context.DrawLine(AxisPen, new Point(left, axisY), new Point(right, axisY));

		for (int i = 0; i <= 4; ++i)
		{
			double ratio = i / 4.0;
			double x = left + ((right - left) * ratio);
			context.DrawLine(AxisPen, new Point(x, axisY), new Point(x, axisY - 5.0));
			DrawText(context, FormatMs(maxEndMs * ratio), MutedTextBrush, 9.0, x + 2.0, top);
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

	private static string FormatMs(double value)
	{
		return value <= 0.0 ? "0 ms" : Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture) + " ms";
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
