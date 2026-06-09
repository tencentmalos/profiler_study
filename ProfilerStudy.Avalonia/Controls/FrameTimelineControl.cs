using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ProfilerStudy.Avalonia;

public sealed class FrameTimelineControl : Control
{
	private const double LeftPadding = 34.0;
	private const double RightPadding = 8.0;
	private const double TopPadding = 8.0;
	private const double BottomPadding = 18.0;

	private static readonly IBrush BackgroundBrush = Brush(200, 200, 200);
	private static readonly IBrush DataBackgroundBrush = Brush(211, 211, 211);
	private static readonly IBrush FrameBrush = Brush(0, 128, 0);
	private static readonly IBrush WarningFrameBrush = Brush(210, 126, 0);
	private static readonly IBrush AlertFrameBrush = Brush(200, 35, 35);
	private static readonly IBrush SelectionFillBrush = Brush(0, 128, 255, 35);
	private static readonly IBrush HoverFillBrush = Brush(255, 255, 255, 45);
	private static readonly IBrush TextBrush = Brush(30, 30, 30);
	private static readonly Pen BorderPen = new Pen(Brush(90, 90, 90), 1.0);
	private static readonly Pen AxisPen = new Pen(Brush(30, 30, 30), 1.0);
	private static readonly Pen GridPen = new Pen(Brush(155, 155, 155, 120), 1.0);
	private static readonly Pen TargetLinePen = new Pen(Brush(182, 82, 0, 150), 1.0);
	private static readonly Pen SelectionLinePen = new Pen(Brush(64, 130, 210, 180), 1.0);
	private static readonly Pen HoverLinePen = new Pen(Brush(255, 255, 255, 190), 1.0);

	public static readonly StyledProperty<IReadOnlyList<FrameSample>> SamplesProperty =
		AvaloniaProperty.Register<FrameTimelineControl, IReadOnlyList<FrameSample>>(nameof(Samples));

	public static readonly StyledProperty<double> TargetFrameMsProperty =
		AvaloniaProperty.Register<FrameTimelineControl, double>(nameof(TargetFrameMs), 33.333);

	public static readonly StyledProperty<TimelineViewport> ViewportProperty =
		AvaloniaProperty.Register<FrameTimelineControl, TimelineViewport>(nameof(Viewport));

	public static readonly StyledProperty<TimelineSelection> SelectionProperty =
		AvaloniaProperty.Register<FrameTimelineControl, TimelineSelection>(nameof(Selection));

	private bool m_IsPanning;
	private double m_LastPanX;
	private int m_LastPanStartFrame;
	private FrameTimelineRenderModel m_RenderModel = FrameTimelineRenderModel.Empty;
	private FrameTimelinePixelLayout m_PixelLayout = FrameTimelinePixelLayout.Empty;
	private bool m_IsLayoutDirty = true;

	static FrameTimelineControl()
	{
		AffectsRender<FrameTimelineControl>(SamplesProperty, TargetFrameMsProperty, ViewportProperty, SelectionProperty);
	}

	public IReadOnlyList<FrameSample> Samples
	{
		get => GetValue(SamplesProperty);
		set => SetValue(SamplesProperty, value);
	}

	public double TargetFrameMs
	{
		get => GetValue(TargetFrameMsProperty);
		set => SetValue(TargetFrameMsProperty, value);
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
		return new Size(availableSize.Width, 64.0);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == ViewportProperty)
		{
			if (change.OldValue is TimelineViewport oldViewport)
			{
				oldViewport.PropertyChanged -= TimelineStatePropertyChanged;
			}
			if (change.NewValue is TimelineViewport newViewport)
			{
				newViewport.PropertyChanged += TimelineStatePropertyChanged;
			}
		}
		else if (change.Property == SelectionProperty)
		{
			if (change.OldValue is TimelineSelection oldSelection)
			{
				oldSelection.PropertyChanged -= TimelineStatePropertyChanged;
			}
			if (change.NewValue is TimelineSelection newSelection)
			{
				newSelection.PropertyChanged += TimelineStatePropertyChanged;
			}
		}

		if (change.Property == SamplesProperty ||
			change.Property == TargetFrameMsProperty ||
			change.Property == ViewportProperty)
		{
			m_IsLayoutDirty = true;
			InvalidateVisual();
		}
		else if (change.Property == SelectionProperty)
		{
			InvalidateVisual();
		}
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		EnsureLayout();
		Rect bounds = Bounds;
		context.FillRectangle(BackgroundBrush, bounds);
		context.DrawRectangle(null, BorderPen, bounds.Deflate(0.5));

		Rect graphBounds = GetGraphBounds();
		context.FillRectangle(DataBackgroundBrush, graphBounds);
		context.DrawRectangle(null, BorderPen, graphBounds);

		if (Samples == null || Samples.Count == 0 || Viewport == null || Viewport.FrameCount == 0)
		{
			DrawText(context, "No frame samples", graphBounds.X + 8.0, graphBounds.Y + 8.0);
			return;
		}

		if (m_PixelLayout.HasBars is false)
		{
			DrawText(context, "No frame samples in visible range", graphBounds.X + 8.0, graphBounds.Y + 8.0);
			return;
		}

		DrawGrid(context, graphBounds);
		foreach (FrameTimelinePixelBar bar in m_PixelLayout.Bars)
		{
			context.FillRectangle(GetFrameBrush(bar.Item.Category), AlignRect(bar.Rect));
		}

		DrawFrameOverlay(context, Selection?.SelectedFrameIndex ?? -1, SelectionFillBrush, SelectionLinePen);
		DrawFrameOverlay(context, Selection?.HoveredFrameIndex ?? -1, HoverFillBrush, HoverLinePen);
		context.DrawLine(TargetLinePen, new Point(graphBounds.X, m_PixelLayout.TargetLineY), new Point(graphBounds.Right, m_PixelLayout.TargetLineY));
		DrawAxis(context, graphBounds);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		Point position = e.GetPosition(this);
		if (m_IsPanning && Viewport != null)
		{
			int visibleCount = Math.Max(1, Viewport.VisibleFrameCount);
			double graphWidth = Math.Max(1.0, GetGraphBounds().Width);
			int deltaFrames = (int)Math.Round(-(position.X - m_LastPanX) * visibleCount / graphWidth);
			Viewport.ScrollFrames(m_LastPanStartFrame + deltaFrames - Viewport.StartFrame);
			e.Handled = true;
			return;
		}

		UpdateHover(position);
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		base.OnPointerExited(e);
		Selection?.ClearHoveredFrame();
		m_IsPanning = false;
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		Point position = e.GetPosition(this);
		PointerPointProperties properties = e.GetCurrentPoint(this).Properties;
		if (properties.IsLeftButtonPressed)
		{
			SelectFrameAt(position);
			e.Handled = true;
		}
		else if (properties.IsMiddleButtonPressed || properties.IsRightButtonPressed)
		{
			StartPan(position, e.Pointer);
			e.Handled = true;
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		if (m_IsPanning)
		{
			m_IsPanning = false;
			e.Pointer.Capture(null);
			e.Handled = true;
		}
	}

	protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
	{
		base.OnPointerWheelChanged(e);
		if (Viewport == null)
		{
			return;
		}

		if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
		{
			int scrollFrames = Math.Max(1, Viewport.VisibleFrameCount / 12);
			Viewport.ScrollFrames(e.Delta.Y < 0 ? scrollFrames : -scrollFrames);
		}
		else
		{
			double anchorFrame = PointToFrameCoordinate(e.GetPosition(this));
			double anchorRatio = Viewport.VisibleFrameCount <= 1
				? 0.5
				: (anchorFrame - Viewport.StartFrame) / Math.Max(1, Viewport.VisibleFrameCount);
			Viewport.Zoom(e.Delta.Y > 0 ? 1.25 : 0.8, anchorRatio);
		}
		e.Handled = true;
	}

	private void TimelineStatePropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(TimelineViewport.StartFrame) ||
			e.PropertyName == nameof(TimelineViewport.EndFrame))
		{
			m_IsLayoutDirty = true;
			InvalidateVisual();
		}
		else if (e.PropertyName == nameof(TimelineSelection.SelectedFrameIndex) ||
			e.PropertyName == nameof(TimelineSelection.HoveredFrameIndex))
		{
			InvalidateVisual();
		}
	}

	private void EnsureLayout()
	{
		Rect graphBounds = GetGraphBounds();
		if (m_IsLayoutDirty || graphBounds != m_PixelLayout.Bounds)
		{
			RebuildLayout();
		}
	}

	private void RebuildLayout()
	{
		m_RenderModel = FrameTimelineRenderModel.Create(Samples, Viewport, TargetFrameMs);
		m_PixelLayout = FrameTimelinePixelLayout.Create(m_RenderModel, GetGraphBounds());
		m_IsLayoutDirty = false;
	}

	private void DrawGrid(DrawingContext context, Rect graphBounds)
	{
		double midY = graphBounds.Y + (graphBounds.Height / 2.0);
		context.DrawLine(GridPen, new Point(graphBounds.X, midY), new Point(graphBounds.Right, midY));
	}

	private void DrawAxis(DrawingContext context, Rect graphBounds)
	{
		context.DrawLine(AxisPen, new Point(graphBounds.X, graphBounds.Bottom), new Point(graphBounds.Right, graphBounds.Bottom));
		context.DrawLine(AxisPen, new Point(graphBounds.X, graphBounds.Y), new Point(graphBounds.X, graphBounds.Bottom));
		DrawText(context, "ms", 6.0, graphBounds.Y + 2.0);
		DrawText(context, "Frames", graphBounds.Right - 44.0, graphBounds.Bottom + 2.0);
	}

	private void DrawFrameOverlay(DrawingContext context, int frameIndex, IBrush fillBrush, Pen linePen)
	{
		if (m_PixelLayout.HasBars is false ||
			frameIndex < m_RenderModel.StartFrame ||
			frameIndex > m_RenderModel.EndFrame)
		{
			return;
		}

		Rect graphBounds = m_PixelLayout.Bounds;
		double x = graphBounds.X + ((frameIndex - m_RenderModel.StartFrame) * m_PixelLayout.FrameWidth);
		Rect rect = new Rect(x, graphBounds.Y, Math.Max(1.0, m_PixelLayout.FrameWidth), graphBounds.Height);
		context.FillRectangle(fillBrush, rect);
		context.DrawLine(linePen, new Point(rect.X, rect.Y), new Point(rect.X, rect.Bottom));
		context.DrawLine(linePen, new Point(rect.Right, rect.Y), new Point(rect.Right, rect.Bottom));
	}

	private void StartPan(Point position, IPointer pointer)
	{
		if (Viewport == null)
		{
			return;
		}

		m_IsPanning = true;
		m_LastPanX = position.X;
		m_LastPanStartFrame = Viewport.StartFrame;
		pointer.Capture(this);
	}

	private void UpdateHover(Point position)
	{
		if (Selection == null || Samples == null || Viewport == null || Samples.Count == 0)
		{
			return;
		}

		EnsureLayout();
		if (m_PixelLayout.TryHit(position, out FrameTimelineRenderItem item))
		{
			Selection.HoverFrame(item.Index, item.DurationMs);
		}
		else
		{
			Selection.ClearHoveredFrame();
		}
	}

	private void SelectFrameAt(Point position)
	{
		if (Selection == null || Samples == null || Samples.Count == 0)
		{
			return;
		}

		EnsureLayout();
		if (m_PixelLayout.TryHit(position, out FrameTimelineRenderItem item))
		{
			Selection.SelectFrame(item.Index, item.DurationMs);
			Selection.HoverFrame(item.Index, item.DurationMs);
		}
	}

	private double PointToFrameCoordinate(Point position)
	{
		if (Viewport == null)
		{
			return 0.0;
		}

		EnsureLayout();
		double frameCoordinate = m_PixelLayout.GetFrameCoordinate(position);
		return Math.Clamp(frameCoordinate, Viewport.StartFrame, Viewport.EndFrame + 1.0);
	}

	private Rect GetGraphBounds()
	{
		double width = Math.Max(1.0, Bounds.Width - LeftPadding - RightPadding);
		double height = Math.Max(1.0, Bounds.Height - TopPadding - BottomPadding);
		return new Rect(LeftPadding, TopPadding, width, height);
	}

	private static IBrush GetFrameBrush(CoreUtils.FrameTimeCategory category)
	{
		return category switch
		{
			CoreUtils.FrameTimeCategory.InBudget => FrameBrush,
			CoreUtils.FrameTimeCategory.Warning => WarningFrameBrush,
			CoreUtils.FrameTimeCategory.Alert => AlertFrameBrush,
			_ => WarningFrameBrush
		};
	}

	private static Rect AlignRect(Rect rect)
	{
		double x = Math.Floor(rect.X);
		double right = Math.Ceiling(rect.Right);
		return new Rect(x, rect.Y, Math.Max(1.0, right - x), rect.Height);
	}

	private static void DrawText(DrawingContext context, string text, double x, double y)
	{
		FormattedText formattedText = new FormattedText(
			text,
			System.Globalization.CultureInfo.CurrentCulture,
			FlowDirection.LeftToRight,
			new Typeface("Monaco,Consolas"),
			10.0,
			TextBrush);
		context.DrawText(formattedText, new Point(x, y));
	}

	private static IBrush Brush(byte r, byte g, byte b, byte a = 255)
	{
		return new SolidColorBrush(Color.FromArgb(a, r, g, b));
	}
}
