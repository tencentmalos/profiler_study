using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ProfilerStudy.Avalonia;

public sealed class SessionScrollbarControl : Control
{
	private bool m_IsDragging;
	private double m_DragStartX;
	private int m_DragStartFrame;
	private static readonly IBrush FrameBrush = new SolidColorBrush(Color.FromRgb(0, 128, 0));
	private static readonly IBrush WarningFrameBrush = new SolidColorBrush(Color.FromRgb(210, 126, 0));
	private static readonly IBrush AlertFrameBrush = new SolidColorBrush(Color.FromRgb(200, 35, 35));

	public static readonly StyledProperty<TimelineViewport> ViewportProperty =
		AvaloniaProperty.Register<SessionScrollbarControl, TimelineViewport>(nameof(Viewport));

	public static readonly StyledProperty<IReadOnlyList<FrameSample>> SamplesProperty =
		AvaloniaProperty.Register<SessionScrollbarControl, IReadOnlyList<FrameSample>>(nameof(Samples));

	public static readonly StyledProperty<double> TargetFrameMsProperty =
		AvaloniaProperty.Register<SessionScrollbarControl, double>(nameof(TargetFrameMs), 33.333);

	static SessionScrollbarControl()
	{
		AffectsRender<SessionScrollbarControl>(ViewportProperty, SamplesProperty, TargetFrameMsProperty);
	}

	public TimelineViewport Viewport
	{
		get => GetValue(ViewportProperty);
		set => SetValue(ViewportProperty, value);
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

	protected override Size MeasureOverride(Size availableSize)
	{
		return new Size(availableSize.Width, 20.0);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == ViewportProperty)
		{
			if (change.OldValue is TimelineViewport oldViewport)
			{
				oldViewport.PropertyChanged -= ViewportPropertyChanged;
			}
			if (change.NewValue is TimelineViewport newViewport)
			{
				newViewport.PropertyChanged += ViewportPropertyChanged;
			}
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		if (Viewport == null || Viewport.FrameCount <= 0)
		{
			return;
		}

		Point point = e.GetPosition(this);
		Rect windowRect = GetWindowRect();
		if (windowRect.Contains(point))
		{
			m_IsDragging = true;
			m_DragStartX = point.X;
			m_DragStartFrame = Viewport.StartFrame;
			e.Handled = true;
			return;
		}

		int targetStart = FrameFromPoint(point.X) - (Viewport.VisibleFrameCount / 2);
		Viewport.SetRange(targetStart, targetStart + Viewport.VisibleFrameCount - 1);
		e.Handled = true;
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		if (!m_IsDragging || Viewport == null || Viewport.FrameCount <= 0)
		{
			return;
		}

		double trackWidth = Math.Max(1.0, Bounds.Width - 16.0);
		double framesPerPixel = Viewport.FrameCount / trackWidth;
		int deltaFrames = (int)Math.Round((e.GetPosition(this).X - m_DragStartX) * framesPerPixel);
		int startFrame = m_DragStartFrame + deltaFrames;
		Viewport.SetRange(startFrame, startFrame + Viewport.VisibleFrameCount - 1);
		e.Handled = true;
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		if (m_IsDragging)
		{
			m_IsDragging = false;
			e.Handled = true;
		}
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		base.OnPointerExited(e);
		m_IsDragging = false;
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		TimelineDrawingTheme theme = TimelineDrawingTheme.Current();
		Rect bounds = Bounds;
		context.FillRectangle(theme.BackgroundBrush, bounds);
		context.DrawRectangle(null, theme.BorderPen, bounds.Deflate(0.5));

		Rect trackRect = GetTrackRect();
		context.FillRectangle(theme.TrackBrush, trackRect);
		DrawFrameStrip(context, trackRect);
		context.DrawRectangle(null, theme.BorderPen, trackRect);

		Rect windowRect = GetWindowRect();
		context.FillRectangle(theme.WindowBrush, windowRect);
		context.FillRectangle(theme.WindowHighlightBrush, new Rect(windowRect.X + 1.0, windowRect.Y + 1.0, Math.Max(0.0, windowRect.Width - 2.0), 3.0));
		context.DrawRectangle(null, theme.WindowPen, windowRect);
	}

	private void ViewportPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(TimelineViewport.FrameCount) ||
			e.PropertyName == nameof(TimelineViewport.StartFrame) ||
			e.PropertyName == nameof(TimelineViewport.EndFrame))
		{
			InvalidateVisual();
		}
	}

	private void DrawFrameStrip(DrawingContext context, Rect trackRect)
	{
		SessionScrollbarFrameStripLayout layout = SessionScrollbarFrameStripLayout.Create(Samples, Viewport, TargetFrameMs, trackRect);
		foreach (SessionScrollbarFrameStripBar bar in layout.Bars)
		{
			context.FillRectangle(GetFrameBrush(bar.Category), bar.Rect);
		}
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

	private Rect GetTrackRect()
	{
		return new Rect(8.0, Math.Max(2.0, (Bounds.Height - 8.0) / 2.0), Math.Max(1.0, Bounds.Width - 16.0), 8.0);
	}

	private Rect GetWindowRect()
	{
		Rect trackRect = GetTrackRect();
		if (Viewport == null || Viewport.FrameCount <= 0)
		{
			return new Rect(trackRect.X, trackRect.Y - 3.0, trackRect.Width, 14.0);
		}

		double frameCount = Math.Max(1.0, Viewport.FrameCount);
		double startRatio = Viewport.StartFrame / frameCount;
		double widthRatio = Math.Max(1.0 / frameCount, Viewport.VisibleFrameCount / frameCount);
		double x = trackRect.X + (trackRect.Width * startRatio);
		double width = Math.Max(12.0, trackRect.Width * widthRatio);
		if (x + width > trackRect.Right)
		{
			x = Math.Max(trackRect.X, trackRect.Right - width);
		}
		return new Rect(x, trackRect.Y - 3.0, Math.Min(width, trackRect.Width), 14.0);
	}

	private int FrameFromPoint(double x)
	{
		Rect trackRect = GetTrackRect();
		double ratio = Math.Clamp((x - trackRect.X) / Math.Max(1.0, trackRect.Width), 0.0, 1.0);
		return (int)Math.Round(ratio * Math.Max(0, (Viewport?.FrameCount ?? 0) - 1));
	}
}

internal readonly struct SessionScrollbarFrameStripBar
{
	public SessionScrollbarFrameStripBar(CoreUtils.FrameTimeCategory category, Rect rect)
	{
		Category = category;
		Rect = rect;
	}

	public CoreUtils.FrameTimeCategory Category { get; }

	public Rect Rect { get; }
}

internal sealed class SessionScrollbarFrameStripLayout
{
	public static readonly SessionScrollbarFrameStripLayout Empty = new SessionScrollbarFrameStripLayout(Array.Empty<SessionScrollbarFrameStripBar>());

	private SessionScrollbarFrameStripLayout(IReadOnlyList<SessionScrollbarFrameStripBar> bars)
	{
		Bars = bars;
	}

	public IReadOnlyList<SessionScrollbarFrameStripBar> Bars { get; }

	public static SessionScrollbarFrameStripLayout Create(
		IReadOnlyList<FrameSample> samples,
		TimelineViewport viewport,
		double targetFrameMs,
		Rect trackRect)
	{
		if (samples == null || samples.Count == 0 || trackRect.Width <= 0.0 || trackRect.Height <= 0.0)
		{
			return Empty;
		}

		int frameCount = viewport?.FrameCount > 0 ? viewport.FrameCount : Math.Max(1, samples[samples.Count - 1].Index + 1);
		int columnCount = Math.Max(1, (int)Math.Ceiling(trackRect.Width));
		CoreUtils.FrameTimeCategory[] columnCategories = new CoreUtils.FrameTimeCategory[columnCount];
		bool[] hasColumn = new bool[columnCount];
		foreach (FrameSample sample in samples)
		{
			int column = (int)Math.Floor(sample.Index * trackRect.Width / Math.Max(1.0, frameCount));
			column = Math.Max(0, Math.Min(columnCount - 1, column));
			CoreUtils.FrameTimeCategory category = FrameTimelineFrameClassifier.GetFrameTimeCategory(sample.DurationMs, targetFrameMs);
			if (hasColumn[column] is false || category > columnCategories[column])
			{
				columnCategories[column] = category;
				hasColumn[column] = true;
			}
		}

		List<SessionScrollbarFrameStripBar> bars = new List<SessionScrollbarFrameStripBar>(columnCount);
		for (int i = 0; i < columnCategories.Length; i++)
		{
			if (hasColumn[i] is false)
			{
				continue;
			}

			double height = columnCategories[i] == CoreUtils.FrameTimeCategory.Alert ? 4.0 : 2.0;
			height = Math.Min(trackRect.Height, height);
			bars.Add(new SessionScrollbarFrameStripBar(
				columnCategories[i],
				new Rect(trackRect.X + i, trackRect.Bottom - height, 1.0, height)));
		}

		return new SessionScrollbarFrameStripLayout(bars);
	}
}
