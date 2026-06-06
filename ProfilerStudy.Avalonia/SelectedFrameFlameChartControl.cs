using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ProfilerStudy.Avalonia;

public sealed class SelectedFrameFlameChartControl : Control
{
	private const double LeftPadding = 8.0;
	private const double RightPadding = 8.0;
	private const double TopPadding = 8.0;
	private const double BottomPadding = 8.0;
	private const double LaneHeight = 18.0;
	private const double LaneGap = 3.0;
	private const double MinBarWidth = 2.0;

	private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(207, 207, 207));
	private static readonly IBrush EmptyTextBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
	private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(16, 16, 16));
	private static readonly Pen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(124, 124, 124)), 1.0);

	public static readonly StyledProperty<IReadOnlyList<ScopeFrameDetailRow>> RowsProperty =
		AvaloniaProperty.Register<SelectedFrameFlameChartControl, IReadOnlyList<ScopeFrameDetailRow>>(nameof(Rows));

	static SelectedFrameFlameChartControl()
	{
		AffectsRender<SelectedFrameFlameChartControl>(RowsProperty);
	}

	public IReadOnlyList<ScopeFrameDetailRow> Rows
	{
		get => GetValue(RowsProperty);
		set => SetValue(RowsProperty, value);
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
			DrawText(context, "Select a frame with scope data to show flame chart.", EmptyTextBrush, 12.0, LeftPadding, TopPadding);
			return;
		}

		double maxEndMs = rows.Max(item => item.StartOffsetMs + Math.Max(0.0, item.DurationMs));
		if (maxEndMs <= 0.0)
		{
			DrawText(context, "Selected frame has no measurable scope duration.", EmptyTextBrush, 12.0, LeftPadding, TopPadding);
			return;
		}

		double plotWidth = Math.Max(1.0, bounds.Width - LeftPadding - RightPadding);
		double y = TopPadding;
		foreach (ScopeFrameDetailRow row in rows.OrderBy(item => item.StartOffsetMs).ThenBy(item => item.Depth).Take(GetVisibleRowCount(bounds.Height)))
		{
			double x = LeftPadding + (row.StartOffsetMs / maxEndMs * plotWidth);
			double width = Math.Max(MinBarWidth, row.DurationMs / maxEndMs * plotWidth);
			Rect rect = new Rect(x, y, Math.Min(width, bounds.Width - RightPadding - x), LaneHeight);
			if (rect.Width <= 0.0)
			{
				continue;
			}

			IBrush fill = new SolidColorBrush(GetDepthColor(row.Depth));
			context.FillRectangle(fill, rect);
			context.DrawRectangle(null, BorderPen, rect);
			if (rect.Width > 42.0)
			{
				DrawText(context, row.Name, TextBrush, 10.0, rect.X + 4.0, rect.Y + 2.0);
			}

			y += LaneHeight + LaneGap;
			if (y + LaneHeight > bounds.Height - BottomPadding)
			{
				break;
			}
		}
	}

	private static int GetVisibleRowCount(double height)
	{
		return Math.Max(1, (int)Math.Floor((height - TopPadding - BottomPadding) / (LaneHeight + LaneGap)));
	}

	private static Color GetDepthColor(int depth)
	{
		Color[] palette =
		{
			Color.FromRgb(115, 181, 255),
			Color.FromRgb(99, 215, 155),
			Color.FromRgb(255, 180, 79),
			Color.FromRgb(255, 126, 139),
			Color.FromRgb(192, 162, 255),
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
