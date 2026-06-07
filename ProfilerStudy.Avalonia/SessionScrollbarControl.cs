using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ProfilerStudy.Avalonia;

public sealed class SessionScrollbarControl : Control
{
	private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(207, 207, 207));
	private static readonly IBrush TrackBrush = new SolidColorBrush(Color.FromRgb(175, 175, 175));
	private static readonly IBrush WindowBrush = new SolidColorBrush(Color.FromRgb(99, 130, 207));
	private static readonly IBrush WindowHighlightBrush = new SolidColorBrush(Color.FromRgb(127, 157, 230));
	private static readonly Pen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(128, 128, 128)), 1.0);
	private static readonly Pen WindowPen = new Pen(new SolidColorBrush(Color.FromRgb(48, 76, 149)), 1.0);

	private bool m_IsDragging;
	private double m_DragStartX;
	private int m_DragStartFrame;

	public static readonly StyledProperty<TimelineViewport> ViewportProperty =
		AvaloniaProperty.Register<SessionScrollbarControl, TimelineViewport>(nameof(Viewport));

	static SessionScrollbarControl()
	{
		AffectsRender<SessionScrollbarControl>(ViewportProperty);
	}

	public TimelineViewport Viewport
	{
		get => GetValue(ViewportProperty);
		set => SetValue(ViewportProperty, value);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		return new Size(availableSize.Width, 20.0);
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
		Rect bounds = Bounds;
		context.FillRectangle(BackgroundBrush, bounds);
		context.DrawRectangle(null, BorderPen, bounds.Deflate(0.5));

		Rect trackRect = GetTrackRect();
		context.FillRectangle(TrackBrush, trackRect);
		context.DrawRectangle(null, BorderPen, trackRect);

		Rect windowRect = GetWindowRect();
		context.FillRectangle(WindowBrush, windowRect);
		context.FillRectangle(WindowHighlightBrush, new Rect(windowRect.X + 1.0, windowRect.Y + 1.0, Math.Max(0.0, windowRect.Width - 2.0), 3.0));
		context.DrawRectangle(null, WindowPen, windowRect);
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
