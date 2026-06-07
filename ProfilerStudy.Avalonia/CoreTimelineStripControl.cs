using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ProfilerStudy.Avalonia;

public sealed class CoreTimelineStripControl : Control
{
	private const double LeftPadding = 8.0;
	private const double RightPadding = 8.0;
	private const double TopPadding = 8.0;
	private const double BottomPadding = 8.0;
	private const double CoreLabelWidth = 92.0;
	private const double LaneHeight = 14.0;
	private const double LaneGap = 5.0;

	private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(207, 207, 207));
	private static readonly IBrush LabelBackgroundBrush = new SolidColorBrush(Color.FromRgb(190, 190, 190));
	private static readonly IBrush EmptyTextBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
	private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(16, 16, 16));
	private static readonly IBrush EmptyLaneBrush = new SolidColorBrush(Color.FromRgb(196, 196, 196));
	private static readonly IBrush SwitchBrush = new SolidColorBrush(Color.FromRgb(245, 245, 245));
	private static readonly Pen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(124, 124, 124)), 1.0);
	private static readonly Pen LanePen = new Pen(new SolidColorBrush(Color.FromRgb(150, 150, 150)), 1.0);
	private static readonly Pen SwitchPen = new Pen(SwitchBrush, 1.0);

	public static readonly StyledProperty<IReadOnlyList<CoreSummaryRow>> RowsProperty =
		AvaloniaProperty.Register<CoreTimelineStripControl, IReadOnlyList<CoreSummaryRow>>(nameof(Rows));

	public static readonly StyledProperty<string> SummaryTextProperty =
		AvaloniaProperty.Register<CoreTimelineStripControl, string>(nameof(SummaryText));

	static CoreTimelineStripControl()
	{
		AffectsRender<CoreTimelineStripControl>(RowsProperty, SummaryTextProperty);
	}

	public IReadOnlyList<CoreSummaryRow> Rows
	{
		get => GetValue(RowsProperty);
		set => SetValue(RowsProperty, value);
	}

	public string SummaryText
	{
		get => GetValue(SummaryTextProperty);
		set => SetValue(SummaryTextProperty, value);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		IReadOnlyList<CoreSummaryRow> rows = Rows ?? Array.Empty<CoreSummaryRow>();
		double desiredHeight = TopPadding + BottomPadding + Math.Max(1, rows.Count) * LaneHeight + Math.Max(0, rows.Count - 1) * LaneGap;
		return new Size(availableSize.Width, Math.Max(70.0, desiredHeight));
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		Rect bounds = Bounds;
		context.FillRectangle(BackgroundBrush, bounds);
		context.DrawRectangle(null, BorderPen, bounds.Deflate(0.5));

		IReadOnlyList<CoreSummaryRow> rows = Rows ?? Array.Empty<CoreSummaryRow>();
		if (rows.Count == 0)
		{
			DrawText(context, string.IsNullOrWhiteSpace(SummaryText) ? "No core data in visible range." : SummaryText, EmptyTextBrush, 12.0, LeftPadding, TopPadding);
			return;
		}

		double laneLeft = LeftPadding + CoreLabelWidth;
		double laneWidth = Math.Max(1.0, bounds.Width - laneLeft - RightPadding);
		double y = TopPadding;
		foreach (CoreSummaryRow row in rows)
		{
			if (y + LaneHeight > bounds.Height - BottomPadding)
			{
				break;
			}

			var labelRect = new Rect(LeftPadding, y, CoreLabelWidth - 4.0, LaneHeight);
			context.FillRectangle(LabelBackgroundBrush, labelRect);
			context.DrawRectangle(null, LanePen, labelRect);
			DrawText(context, row.CoreText, TextBrush, 10.0, labelRect.X + 4.0, labelRect.Y);

			var laneRect = new Rect(laneLeft, y, laneWidth, LaneHeight);
			context.FillRectangle(EmptyLaneBrush, laneRect);
			context.DrawRectangle(null, LanePen, laneRect);
			DrawContextSwitches(context, row, laneRect);
			if (row.ContextSwitchCount > 0)
			{
				DrawText(context, row.ContextSwitchCount.ToString(CultureInfo.InvariantCulture), TextBrush, 10.0, laneLeft + 4.0, y);
			}
			y += LaneHeight + LaneGap;
		}
	}

	private static void DrawContextSwitches(DrawingContext context, CoreSummaryRow row, Rect laneRect)
	{
		long duration = Math.Max(1L, row.VisibleEndTime - row.VisibleStartTime);
		foreach (long timestamp in row.ContextSwitchTimes)
		{
			double ratio = (timestamp - row.VisibleStartTime) / (double)duration;
			if (ratio < 0.0 || ratio > 1.0)
			{
				continue;
			}

			double x = laneRect.X + (laneRect.Width * ratio);
			context.DrawLine(SwitchPen, new Point(x, laneRect.Y + 2.0), new Point(x, laneRect.Bottom - 2.0));
		}
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
