using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
	private static readonly ScottPlotColor TargetLineColor = new(182, 82, 0, 150);
	private static readonly ScottPlotColor SelectionLineColor = new(64, 130, 210, 220);
	private static readonly ScottPlotColor HoverLineColor = new(255, 255, 255, 190);
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
			e.PropertyName == nameof(TimelineViewport.EndFrame) ||
			e.PropertyName == nameof(TimelineSelection.SelectedFrameIndex) ||
			e.PropertyName == nameof(TimelineSelection.SelectedFrameTimeMs))
		{
			RefreshPlot();
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

			IReadOnlyList<FrameSample> samples = Samples;
			TimelineViewport viewport = Viewport;
			if (samples == null || samples.Count == 0 || viewport == null || viewport.FrameCount == 0)
			{
				m_Plot.Plot.Add.Text("No frame samples", 0, 0);
				m_Plot.Plot.Axes.SetLimits(-1, 1, -1, 1);
				m_Plot.Refresh();
				return;
			}

			int startFrame = Math.Max(0, viewport.StartFrame);
			int endFrame = Math.Min(viewport.EndFrame, samples[samples.Count - 1].Index);
			List<FrameSample> visibleSamples = samples
				.Where(item => item.Index >= startFrame && item.Index <= endFrame)
				.OrderBy(item => item.Index)
				.ToList();

			if (visibleSamples.Count == 0)
			{
				m_Plot.Plot.Add.Text("No frame samples in visible range", startFrame, 0);
				m_Plot.Plot.Axes.SetLimits(startFrame, Math.Max(startFrame + 1, endFrame), -1, 1);
				m_Plot.Refresh();
				return;
			}

			double[] xs = visibleSamples.Select(item => (double)item.Index).ToArray();
			double[] ys = visibleSamples.Select(item => item.DurationMs).ToArray();
			double maxMs = Math.Max(TargetFrameMs * 2.0, ys.Max());

			var frameLine = m_Plot.Plot.Add.Scatter(xs, ys);
			frameLine.LineStyle.Color = FrameLineColor;
			frameLine.MarkerSize = visibleSamples.Count <= 160 ? 3 : 0;
			frameLine.LegendText = "Frame ms";

			double[] warningXs = visibleSamples.Where(item => item.DurationMs >= TargetFrameMs).Select(item => (double)item.Index).ToArray();
			double[] warningYs = visibleSamples.Where(item => item.DurationMs >= TargetFrameMs).Select(item => item.DurationMs).ToArray();
			if (warningXs.Length > 0)
			{
				var warningMarkers = m_Plot.Plot.Add.Scatter(warningXs, warningYs);
				warningMarkers.LineStyle.Color = WarningFrameLineColor;
				warningMarkers.MarkerSize = 4;
			}

			AddHorizontalGuide(TargetFrameMs, startFrame, endFrame, TargetLineColor);
			if (Selection != null && Selection.SelectedFrameIndex >= startFrame && Selection.SelectedFrameIndex <= endFrame)
			{
				AddVerticalGuide(Selection.SelectedFrameIndex, maxMs, SelectionLineColor);
			}
			if (Selection != null && Selection.HoveredFrameIndex >= startFrame && Selection.HoveredFrameIndex <= endFrame)
			{
				AddVerticalGuide(Selection.HoveredFrameIndex, maxMs, HoverLineColor);
			}

			m_Plot.Plot.Axes.SetLimits(startFrame, Math.Max(startFrame + 1, endFrame), 0, Math.Max(1.0, maxMs * 1.1));
			m_Plot.Refresh();
		}
		finally
		{
			m_IsRefreshing = false;
		}
	}

	private void AddHorizontalGuide(double y, double xStart, double xEnd, ScottPlotColor color)
	{
		var guide = m_Plot.Plot.Add.Scatter(new[] { xStart, xEnd }, new[] { y, y });
		guide.LineStyle.Color = color;
		guide.MarkerSize = 0;
	}

	private void AddVerticalGuide(double frameIndex, double maxMs, ScottPlotColor color)
	{
		var guide = m_Plot.Plot.Add.Scatter(new[] { frameIndex, frameIndex }, new[] { 0.0, maxMs * 1.1 });
		guide.LineStyle.Color = color;
		guide.MarkerSize = 0;
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
			Selection.HoveredFrameIndex = -1;
			Selection.HoveredFrameTimeMs = 0.0;
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

		int frameIndex = PointToFrameIndex(position);
		if (TryGetSample(Samples, frameIndex, out FrameSample sample))
		{
			Selection.HoveredFrameIndex = sample.Index;
			Selection.HoveredFrameTimeMs = sample.DurationMs;
		}
		else
		{
			Selection.HoveredFrameIndex = -1;
			Selection.HoveredFrameTimeMs = 0.0;
		}
	}

	private void SelectFrameAt(Point position)
	{
		if (Selection == null || Samples == null || Samples.Count == 0)
		{
			return;
		}

		int frameIndex = PointToFrameIndex(position);
		if (TryGetSample(Samples, frameIndex, out FrameSample sample))
		{
			Selection.SelectedFrameIndex = sample.Index;
			Selection.SelectedFrameTimeMs = sample.DurationMs;
			Selection.HoveredFrameIndex = sample.Index;
			Selection.HoveredFrameTimeMs = sample.DurationMs;
		}
	}

	private int PointToFrameIndex(Point position)
	{
		if (Viewport == null)
		{
			return 0;
		}

		return (int)Math.Round(PointToFrameCoordinate(position));
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

	private static bool TryGetSample(IReadOnlyList<FrameSample> samples, int frameIndex, out FrameSample sample)
	{
		foreach (FrameSample candidate in samples)
		{
			if (candidate.Index == frameIndex)
			{
				sample = candidate;
				return true;
			}
		}
		sample = default;
		return false;
	}
}
