using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Windows.Input;

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
	private static readonly Pen HoverPen = new Pen(new SolidColorBrush(Color.FromRgb(32, 32, 32)), 2.0);

	private ThreadTimelineScopeRow m_HoveredScope;
	private bool m_IsPanning;
	private double m_PanStartX;
	private int m_PanStartFrame;

	public static readonly StyledProperty<IReadOnlyList<ThreadTimelineScopeRow>> RowsProperty =
		AvaloniaProperty.Register<ThreadTimelineLaneControl, IReadOnlyList<ThreadTimelineScopeRow>>(nameof(Rows));

	public static readonly StyledProperty<IReadOnlyList<FrameSample>> SamplesProperty =
		AvaloniaProperty.Register<ThreadTimelineLaneControl, IReadOnlyList<FrameSample>>(nameof(Samples));

	public static readonly StyledProperty<TimelineViewport> ViewportProperty =
		AvaloniaProperty.Register<ThreadTimelineLaneControl, TimelineViewport>(nameof(Viewport));

	public static readonly StyledProperty<TimelineSelection> SelectionProperty =
		AvaloniaProperty.Register<ThreadTimelineLaneControl, TimelineSelection>(nameof(Selection));

	public static readonly StyledProperty<ICommand> OpenScopeSourceCommandProperty =
		AvaloniaProperty.Register<ThreadTimelineLaneControl, ICommand>(nameof(OpenScopeSourceCommand));

	public static readonly StyledProperty<ICommand> FocusThreadCommandProperty =
		AvaloniaProperty.Register<ThreadTimelineLaneControl, ICommand>(nameof(FocusThreadCommand));

	static ThreadTimelineLaneControl()
	{
		AffectsRender<ThreadTimelineLaneControl>(RowsProperty, SamplesProperty, ViewportProperty, SelectionProperty);
	}

	public IReadOnlyList<ThreadTimelineScopeRow> Rows
	{
		get => GetValue(RowsProperty);
		set => SetValue(RowsProperty, value);
	}

	public IReadOnlyList<FrameSample> Samples
	{
		get => GetValue(SamplesProperty);
		set => SetValue(SamplesProperty, value);
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

	public ICommand OpenScopeSourceCommand
	{
		get => GetValue(OpenScopeSourceCommandProperty);
		set => SetValue(OpenScopeSourceCommandProperty, value);
	}

	public ICommand FocusThreadCommand
	{
		get => GetValue(FocusThreadCommandProperty);
		set => SetValue(FocusThreadCommandProperty, value);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		Point point = e.GetPosition(this);
		PointerPointProperties properties = e.GetCurrentPoint(this).Properties;
		if (properties.IsRightButtonPressed && TryGetThreadNameAtPoint(point, out string threadName))
		{
			if (FocusThreadCommand?.CanExecute(threadName) == true)
			{
				FocusThreadCommand.Execute(threadName);
			}
			return;
		}

		if (properties.IsRightButtonPressed && Viewport != null)
		{
			m_IsPanning = true;
			m_PanStartX = point.X;
			m_PanStartFrame = Viewport.StartFrame;
			e.Handled = true;
			return;
		}

		if (e.ClickCount >= 2)
		{
			ThreadTimelineScopeRow scope = TryGetScopeAtPoint(point);
			if (scope?.SourceScope != null && OpenScopeSourceCommand?.CanExecute(scope.SourceScope) == true)
			{
				OpenScopeSourceCommand.Execute(scope.SourceScope);
				e.Handled = true;
				return;
			}
		}

		if (Selection == null)
		{
			return;
		}

		if (TryGetFrameAtPoint(point, out FrameSample frame))
		{
			Selection.SelectedFrameIndex = frame.Index;
			Selection.SelectedFrameTimeMs = frame.DurationMs;
		}
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		Point point = e.GetPosition(this);
		if (m_IsPanning && Viewport != null)
		{
			double axisLeft = LeftPadding + ThreadLabelWidth;
			double axisRight = Math.Max(axisLeft + 1.0, Bounds.Width - RightPadding);
			double framesPerPixel = Viewport.VisibleFrameCount / Math.Max(1.0, axisRight - axisLeft);
			int deltaFrames = (int)Math.Round((m_PanStartX - point.X) * framesPerPixel);
			Viewport.ScrollFrames(m_PanStartFrame + deltaFrames - Viewport.StartFrame);
			e.Handled = true;
			return;
		}

		if (Selection == null)
		{
			return;
		}

		if (TryGetFrameAtPoint(point, out FrameSample frame))
		{
			Selection.HoveredFrameIndex = frame.Index;
			Selection.HoveredFrameTimeMs = frame.DurationMs;
		}
		else
		{
			Selection.HoveredFrameIndex = -1;
			Selection.HoveredFrameTimeMs = 0.0;
		}

		ThreadTimelineScopeRow hoveredScope = TryGetScopeAtPoint(point);
		if (!ReferenceEquals(m_HoveredScope, hoveredScope))
		{
			m_HoveredScope = hoveredScope;
			ToolTip.SetTip(this, hoveredScope == null ? null : FormatScopeTip(hoveredScope));
			InvalidateVisual();
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		if (m_IsPanning)
		{
			m_IsPanning = false;
			e.Handled = true;
		}
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		base.OnPointerExited(e);
		if (Selection != null)
		{
			Selection.HoveredFrameIndex = -1;
			Selection.HoveredFrameTimeMs = 0.0;
		}
		m_IsPanning = false;
		m_HoveredScope = null;
		ToolTip.SetTip(this, null);
		InvalidateVisual();
	}

	protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
	{
		base.OnPointerWheelChanged(e);
		if (Viewport == null || Viewport.FrameCount <= 0 || e.Delta.Y == 0.0)
		{
			return;
		}

		if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
		{
			int deltaFrames = Math.Max(1, Viewport.VisibleFrameCount / 12);
			Viewport.ScrollFrames(e.Delta.Y > 0.0 ? -deltaFrames : deltaFrames);
			e.Handled = true;
			return;
		}

		double anchorRatio = GetTimelineAnchorRatio(e.GetPosition(this));
		double factor = e.Delta.Y > 0.0 ? 1.25 : 0.8;
		Viewport.Zoom(factor, anchorRatio);
		e.Handled = true;
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
				context.DrawRectangle(null, ReferenceEquals(row, m_HoveredScope) ? HoverPen : BorderPen, barRect);
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

	private bool TryGetFrameAtPoint(Point point, out FrameSample frame)
	{
		frame = default;
		IReadOnlyList<FrameSample> samples = Samples ?? Array.Empty<FrameSample>();
		if (samples.Count == 0 || Viewport == null)
		{
			return false;
		}

		double axisLeft = LeftPadding + ThreadLabelWidth;
		double axisRight = Math.Max(axisLeft + 1.0, Bounds.Width - RightPadding);
		if (point.X < axisLeft || point.X > axisRight)
		{
			return false;
		}

		int startFrame = Viewport.StartFrame;
		int endFrame = Math.Max(startFrame, Viewport.EndFrame);
		double ratio = Math.Clamp((point.X - axisLeft) / Math.Max(1.0, axisRight - axisLeft), 0.0, 1.0);
		double targetFrame = startFrame + ((endFrame - startFrame + 1.0) * ratio);
		bool found = false;
		double bestDistance = double.MaxValue;
		foreach (FrameSample sample in samples)
		{
			if (sample.Index < startFrame || sample.Index > endFrame)
			{
				continue;
			}

			double distance = Math.Abs(sample.Index - targetFrame);
			if (distance < bestDistance)
			{
				frame = sample;
				bestDistance = distance;
				found = true;
			}
		}

		return found;
	}

	private double GetTimelineAnchorRatio(Point point)
	{
		double axisLeft = LeftPadding + ThreadLabelWidth;
		double axisRight = Math.Max(axisLeft + 1.0, Bounds.Width - RightPadding);
		return Math.Clamp((point.X - axisLeft) / Math.Max(1.0, axisRight - axisLeft), 0.0, 1.0);
	}

	private ThreadTimelineScopeRow TryGetScopeAtPoint(Point point)
	{
		IReadOnlyList<ThreadTimelineScopeRow> rows = Rows ?? Array.Empty<ThreadTimelineScopeRow>();
		if (rows.Count == 0 || Viewport == null)
		{
			return null;
		}

		int startFrame = Viewport.StartFrame;
		int endFrame = Math.Max(startFrame + 1, Viewport.EndFrame);
		double axisLeft = LeftPadding + ThreadLabelWidth;
		double axisRight = Math.Max(axisLeft + 1.0, Bounds.Width - RightPadding);
		double plotWidth = Math.Max(1.0, axisRight - axisLeft);
		double y = TopPadding + AxisHeight;

		foreach (IGrouping<string, ThreadTimelineScopeRow> threadRows in rows
			.GroupBy(item => item.ThreadName, StringComparer.Ordinal)
			.OrderBy(group => group.Min(item => item.StartFrame)))
		{
			if (y + LaneHeight > Bounds.Height - BottomPadding)
			{
				break;
			}

			foreach (ThreadTimelineScopeRow row in threadRows.OrderBy(item => item.StartFrame).ThenByDescending(item => item.Depth))
			{
				double x = axisLeft + ((row.StartFrame - startFrame) / Math.Max(1.0, endFrame - startFrame + 1.0) * plotWidth);
				double width = Math.Max(MinBarWidth, (row.EndFrame - row.StartFrame) / Math.Max(1.0, endFrame - startFrame + 1.0) * plotWidth);
				double depthInset = Math.Min(7.0, row.Depth * 1.1);
				Rect barRect = new Rect(
					x,
					y + 2.0 + depthInset,
					Math.Min(width, axisRight - x),
					Math.Max(3.0, LaneHeight - 4.0 - depthInset));
				if (barRect.Contains(point))
				{
					return row;
				}
			}

			y += LaneHeight + LaneGap;
		}

		return null;
	}

	private bool TryGetThreadNameAtPoint(Point point, out string threadName)
	{
		threadName = null;
		IReadOnlyList<ThreadTimelineScopeRow> rows = Rows ?? Array.Empty<ThreadTimelineScopeRow>();
		if (rows.Count == 0)
		{
			return false;
		}

		double y = TopPadding + AxisHeight;
		foreach (IGrouping<string, ThreadTimelineScopeRow> threadRows in rows
			.GroupBy(item => item.ThreadName, StringComparer.Ordinal)
			.OrderBy(group => group.Min(item => item.StartFrame)))
		{
			if (y + LaneHeight > Bounds.Height - BottomPadding)
			{
				break;
			}

			Rect rowRect = new Rect(0.0, y, Bounds.Width, LaneHeight);
			if (rowRect.Contains(point))
			{
				threadName = threadRows.Key;
				return true;
			}

			y += LaneHeight + LaneGap;
		}

		return false;
	}

	private static string FormatScopeTip(ThreadTimelineScopeRow row)
	{
		string sourceText = string.IsNullOrWhiteSpace(row.SourceText) ? string.Empty : "\n" + row.SourceText;
		string openText = row.CanOpenSource ? "\nDouble-click to open source" : string.Empty;
		return row.ThreadName + "\n" +
			"Frame " + row.FrameIndex.ToString(CultureInfo.InvariantCulture) + "\n" +
			row.Name + "\n" +
			"Depth " + row.Depth.ToString(CultureInfo.InvariantCulture) + ", " +
			row.StartFrame.ToString("0.###", CultureInfo.InvariantCulture) + "-" +
			row.EndFrame.ToString("0.###", CultureInfo.InvariantCulture) +
			sourceText +
			openText;
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
