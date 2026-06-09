using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlotColor = ScottPlot.Color;

namespace ProfilerStudy.Avalonia;

public sealed class FrameTimelineControl : UserControl
{
	private static readonly ScottPlotColor PlotBackground = new(200, 200, 200);
	private static readonly ScottPlotColor DataBackground = new(211, 211, 211);
	private static readonly ScottPlotColor FrameLineColor = new(0, 128, 0);
	private static readonly ScottPlotColor WarningFrameLineColor = new(210, 126, 0);
	private static readonly ScottPlotColor AlertFrameLineColor = new(200, 35, 35);
	private static readonly ScottPlotColor TargetLineColor = new(182, 82, 0, 150);
	private static readonly ScottPlotColor SelectionLineColor = new(64, 130, 210, 180);
	private static readonly ScottPlotColor SelectionFillColor = new(0, 128, 255, 35);
	private static readonly ScottPlotColor HoverLineColor = new(255, 255, 255, 190);
	private static readonly ScottPlotColor HoverFillColor = new(255, 255, 255, 45);
	private static readonly ScottPlotColor AxisColor = new(30, 30, 30);
	private static readonly ScottPlotColor GridColor = new(155, 155, 155, 120);

	public static readonly StyledProperty<IReadOnlyList<FrameSample>> SamplesProperty =
		AvaloniaProperty.Register<FrameTimelineControl, IReadOnlyList<FrameSample>>(nameof(Samples));

	public static readonly StyledProperty<double> TargetFrameMsProperty =
		AvaloniaProperty.Register<FrameTimelineControl, double>(nameof(TargetFrameMs), 33.333);

	public static readonly StyledProperty<TimelineViewport> ViewportProperty =
		AvaloniaProperty.Register<FrameTimelineControl, TimelineViewport>(nameof(Viewport));

	public static readonly StyledProperty<TimelineSelection> SelectionProperty =
		AvaloniaProperty.Register<FrameTimelineControl, TimelineSelection>(nameof(Selection));

	private readonly AvaPlot m_Plot;
	private bool m_IsPanning;
	private bool m_IsRefreshing;
	private double m_LastPanX;
	private int m_LastPanStartFrame;
	private FrameTimelineRenderModel m_RenderModel = FrameTimelineRenderModel.Empty;
	private readonly List<IPlottable> m_OverlayPlottables = new List<IPlottable>();

	public FrameTimelineControl()
	{
		m_Plot = new AvaPlot();
		Content = m_Plot;
		m_Plot.UserInputProcessor.Disable();
		m_Plot.PointerMoved += PlotPointerMoved;
		m_Plot.PointerExited += PlotPointerExited;
		m_Plot.PointerPressed += PlotPointerPressed;
		m_Plot.PointerReleased += PlotPointerReleased;
		m_Plot.PointerWheelChanged += PlotPointerWheelChanged;
		ConfigurePlotChrome();
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
			change.Property == ViewportProperty ||
			change.Property == SelectionProperty)
		{
			RefreshPlot();
		}
	}

	private void TimelineStatePropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(TimelineViewport.StartFrame) ||
			e.PropertyName == nameof(TimelineViewport.EndFrame))
		{
			RefreshPlot();
		}
		else if (e.PropertyName == nameof(TimelineSelection.SelectedFrameIndex) ||
			e.PropertyName == nameof(TimelineSelection.HoveredFrameIndex))
		{
			RefreshSelectionOverlay();
		}
	}

	private void ConfigurePlotChrome()
	{
		m_Plot.Plot.FigureBackground.Color = PlotBackground;
		m_Plot.Plot.DataBackground.Color = DataBackground;
		m_Plot.Plot.Axes.Color(AxisColor);
		m_Plot.Plot.Grid.MajorLineColor = GridColor;
		m_Plot.Plot.Grid.MinorLineColor = GridColor;
		m_Plot.Plot.Legend.IsVisible = false;
		m_Plot.Plot.Title(string.Empty);
		m_Plot.Plot.Axes.Bottom.Label.Text = "Frames";
		m_Plot.Plot.Axes.Left.Label.Text = "ms";
	}

	private void RefreshPlot()
	{
		if (m_IsRefreshing)
		{
			return;
		}

		m_IsRefreshing = true;
		try
		{
			ConfigurePlotChrome();
			m_Plot.Plot.Clear();
			m_OverlayPlottables.Clear();

			m_RenderModel = FrameTimelineRenderModel.Create(Samples, Viewport, TargetFrameMs);
			if (Samples == null || Samples.Count == 0 || Viewport == null || Viewport.FrameCount == 0)
			{
				m_Plot.Plot.Add.Text("No frame samples", 0, 0);
				m_Plot.Plot.Axes.SetLimits(-1, 1, -1, 1);
				m_Plot.Refresh();
				return;
			}

			if (m_RenderModel.HasItems is false)
			{
				m_Plot.Plot.Add.Text("No frame samples in visible range", m_RenderModel.StartFrame, 0);
				m_Plot.Plot.Axes.SetLimits(m_RenderModel.StartFrame, Math.Max(m_RenderModel.StartFrame + 1, m_RenderModel.EndFrame), -1, 1);
				m_Plot.Refresh();
				return;
			}

			List<ScottPlot.Bar> bars = new List<ScottPlot.Bar>(m_RenderModel.Items.Count);
			foreach (FrameTimelineRenderItem item in m_RenderModel.Items)
			{
				ScottPlotColor color = GetFrameColor(item.Category);
				bars.Add(new ScottPlot.Bar
				{
					Position = item.Index,
					Value = item.DurationMs,
					ValueBase = 0.0,
					Size = m_RenderModel.BarSize,
					FillColor = color,
					LineColor = color,
					LineWidth = 0
				});
			}
			m_Plot.Plot.Add.Bars(bars);

			AddHorizontalGuide(TargetFrameMs, m_RenderModel.StartFrame, m_RenderModel.EndFrame, TargetLineColor);
			AddSelectionOverlay();
			ApplyFrameAxisLimits();
			m_Plot.Refresh();
		}
		finally
		{
			m_IsRefreshing = false;
		}
	}

	private void AddHorizontalGuide(double y, double xStart, double xEnd, ScottPlotColor color)
	{
		var guide = m_Plot.Plot.Add.HorizontalLine(y, 1, color);
		guide.Minimum = xStart;
		guide.Maximum = xEnd;
		guide.EnableAutoscale = false;
	}

	private void RefreshSelectionOverlay()
	{
		if (m_IsRefreshing)
		{
			return;
		}

		RemoveSelectionOverlay();
		AddSelectionOverlay();
		ApplyFrameAxisLimits();
		m_Plot.Refresh();
	}

	private void RemoveSelectionOverlay()
	{
		foreach (IPlottable plottable in m_OverlayPlottables)
		{
			m_Plot.Plot.Remove(plottable);
		}
		m_OverlayPlottables.Clear();
	}

	private void AddSelectionOverlay()
	{
		if (Selection == null || m_RenderModel.HasItems is false)
		{
			return;
		}

		if (Selection.SelectedFrameIndex >= m_RenderModel.StartFrame && Selection.SelectedFrameIndex <= m_RenderModel.EndFrame)
		{
			AddFrameOverlay(Selection.SelectedFrameIndex, SelectionFillColor, SelectionLineColor);
		}
		if (Selection.HoveredFrameIndex >= m_RenderModel.StartFrame && Selection.HoveredFrameIndex <= m_RenderModel.EndFrame)
		{
			AddFrameOverlay(Selection.HoveredFrameIndex, HoverFillColor, HoverLineColor);
		}
	}

	private void AddFrameOverlay(double frameIndex, ScottPlotColor fillColor, ScottPlotColor lineColor)
	{
		var span = m_Plot.Plot.Add.HorizontalSpan(frameIndex - 0.5, frameIndex + 0.5, fillColor);
		span.LineStyle.Color = lineColor;
		span.LineStyle.Width = 1;
		span.EnableAutoscale = false;
		m_OverlayPlottables.Add(span);
		AddVerticalGuide(frameIndex - 0.5, lineColor);
		AddVerticalGuide(frameIndex + 0.5, lineColor);
	}

	private void AddVerticalGuide(double frameIndex, ScottPlotColor color)
	{
		var guide = m_Plot.Plot.Add.VerticalLine(frameIndex, 1, color);
		guide.Minimum = 0.0;
		guide.Maximum = Math.Max(1.0, m_RenderModel.MaxDurationMs * 1.1);
		guide.EnableAutoscale = false;
		m_OverlayPlottables.Add(guide);
	}

	private void ApplyFrameAxisLimits()
	{
		if (m_RenderModel.HasItems is false)
		{
			return;
		}

		m_Plot.Plot.Axes.SetLimits(
			m_RenderModel.StartFrame,
			Math.Max(m_RenderModel.StartFrame + 1, m_RenderModel.EndFrame),
			0,
			Math.Max(1.0, m_RenderModel.MaxDurationMs * 1.1));
	}

	private void PlotPointerMoved(object sender, PointerEventArgs e)
	{
		Point position = e.GetPosition(m_Plot);
		if (m_IsPanning && Viewport != null)
		{
			int visibleCount = Math.Max(1, Viewport.VisibleFrameCount);
			double plotWidth = Math.Max(1.0, m_Plot.Bounds.Width);
			int deltaFrames = (int)Math.Round(-(position.X - m_LastPanX) * visibleCount / plotWidth);
			Viewport.ScrollFrames(m_LastPanStartFrame + deltaFrames - Viewport.StartFrame);
			return;
		}

		UpdateHover(position);
	}

	private void PlotPointerExited(object sender, PointerEventArgs e)
	{
		if (Selection != null)
		{
			Selection.ClearHoveredFrame();
		}
	}

	private void PlotPointerPressed(object sender, PointerPressedEventArgs e)
	{
		Point position = e.GetPosition(m_Plot);
		PointerPointProperties properties = e.GetCurrentPoint(m_Plot).Properties;
		if (properties.IsLeftButtonPressed)
		{
			SelectFrameAt(position);
		}
		else if (properties.IsMiddleButtonPressed || properties.IsRightButtonPressed)
		{
			StartPan(position, e.Pointer);
		}
	}

	private void PlotPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		if (m_IsPanning)
		{
			m_IsPanning = false;
			e.Pointer.Capture(null);
		}
	}

	private void PlotPointerWheelChanged(object sender, PointerWheelEventArgs e)
	{
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
			double anchorFrame = PointToFrameCoordinate(e.GetPosition(m_Plot));
			double anchorRatio = Viewport.VisibleFrameCount <= 1
				? 0.5
				: (anchorFrame - Viewport.StartFrame) / Math.Max(1, Viewport.VisibleFrameCount - 1);
			Viewport.Zoom(e.Delta.Y > 0 ? 1.25 : 0.8, anchorRatio);
		}
		e.Handled = true;
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
		pointer.Capture(m_Plot);
	}

	private void UpdateHover(Point position)
	{
		if (Selection == null || Samples == null || Viewport == null || Samples.Count == 0)
		{
			return;
		}

		double frameCoordinate = PointToFrameCoordinate(position);
		if (m_RenderModel.TryHitFrameCoordinate(frameCoordinate, out FrameTimelineRenderItem item))
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

		double frameCoordinate = PointToFrameCoordinate(position);
		if (m_RenderModel.TryHitFrameCoordinate(frameCoordinate, out FrameTimelineRenderItem item))
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

		Coordinates coordinates = m_Plot.Plot.GetCoordinates((float)position.X, (float)position.Y);
		return Math.Clamp(coordinates.X, Viewport.StartFrame, Viewport.EndFrame);
	}

	private static ScottPlotColor GetFrameColor(CoreUtils.FrameTimeCategory category)
	{
		return category switch
		{
			CoreUtils.FrameTimeCategory.InBudget => FrameLineColor,
			CoreUtils.FrameTimeCategory.Warning => WarningFrameLineColor,
			CoreUtils.FrameTimeCategory.Alert => AlertFrameLineColor,
			_ => WarningFrameLineColor
		};
	}

}
