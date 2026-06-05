using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace ProfilerStudy.Avalonia;

public sealed class FrameTimelineControl : Control
{
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
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		Point position = e.GetPosition(this);
		if (m_IsPanning && Viewport != null)
		{
			int visibleCount = Math.Max(1, Viewport.VisibleFrameCount);
			double plotWidth = Math.Max(1.0, Bounds.Width - 32.0);
			int deltaFrames = (int)Math.Round(-(position.X - m_LastPanX) * visibleCount / plotWidth);
			Viewport.ScrollFrames(m_LastPanStartFrame + deltaFrames - Viewport.StartFrame);
			return;
		}

		UpdateHover(position);
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		base.OnPointerExited(e);
		if (Selection != null)
		{
			Selection.HoveredFrameIndex = -1;
			Selection.HoveredFrameTimeMs = 0.0;
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		Point position = e.GetPosition(this);
		PointerPointProperties properties = e.GetCurrentPoint(this).Properties;
		if (properties.IsLeftButtonPressed)
		{
			SelectFrameAt(position);
		}
		else if (properties.IsMiddleButtonPressed || properties.IsRightButtonPressed)
		{
			StartPan(position, e.Pointer);
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		if (m_IsPanning)
		{
			m_IsPanning = false;
			e.Pointer.Capture(null);
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
			double anchorRatio = Bounds.Width <= 0.0 ? 0.5 : e.GetPosition(this).X / Bounds.Width;
			Viewport.Zoom(e.Delta.Y > 0 ? 1.25 : 0.8, anchorRatio);
		}
		e.Handled = true;
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);

		int width = Math.Max(1, (int)Math.Ceiling(Bounds.Width));
		int height = Math.Max(1, (int)Math.Ceiling(Bounds.Height));
		using SKSurface surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
		if (surface == null)
		{
			return;
		}

		DrawTimeline(surface.Canvas, new SKRect(0, 0, width, height));
		using SKImage image = surface.Snapshot();
		using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
		using Stream stream = data.AsStream();
		using Bitmap bitmap = new Bitmap(stream);
		context.DrawImage(bitmap, new Rect(0, 0, Bounds.Width, Bounds.Height));
	}

	private void DrawTimeline(SKCanvas canvas, SKRect bounds)
	{
		canvas.Save();
		canvas.ClipRect(bounds);
		canvas.Clear(new SKColor(21, 25, 31));

		IReadOnlyList<FrameSample> samples = Samples;
		TimelineViewport viewport = Viewport;
		if (samples == null || samples.Count == 0 || viewport == null || viewport.FrameCount == 0)
		{
			DrawEmptyState(canvas, bounds);
			canvas.Restore();
			return;
		}

		float left = bounds.Left + 16;
		float right = bounds.Right - 16;
		float top = bounds.Top + 18;
		float bottom = bounds.Bottom - 28;
		float height = Math.Max(1, bottom - top);
		double maxMs = TargetFrameMs * 2.0;
		int startFrame = Math.Max(0, viewport.StartFrame);
		int endFrame = Math.Min(viewport.EndFrame, samples[samples.Count - 1].Index);
		for (int frameIndex = startFrame; frameIndex <= endFrame; frameIndex++)
		{
			if (TryGetSample(samples, frameIndex, out FrameSample sample))
			{
				maxMs = Math.Max(maxMs, sample.DurationMs);
			}
		}

		using SKPaint gridPaint = new SKPaint { Color = new SKColor(58, 66, 77), StrokeWidth = 1, IsAntialias = true };
		using SKFont labelFont = new SKFont { Size = 12 };
		using SKPaint textPaint = new SKPaint { Color = new SKColor(154, 164, 178), IsAntialias = true };
		using SKPaint framePaint = new SKPaint { Color = new SKColor(118, 184, 159), IsAntialias = false };
		using SKPaint slowFramePaint = new SKPaint { Color = new SKColor(224, 112, 96), IsAntialias = false };
		using SKPaint targetPaint = new SKPaint { Color = new SKColor(236, 196, 108), StrokeWidth = 1, IsAntialias = true };

		for (int i = 0; i <= 4; i++)
		{
			float y = top + height * i / 4f;
			canvas.DrawLine(left, y, right, y, gridPaint);
		}

		float targetY = bottom - (float)(Math.Min(TargetFrameMs, maxMs) / maxMs) * height;
		canvas.DrawLine(left, targetY, right, targetY, targetPaint);
		canvas.DrawText("target " + TargetFrameMs.ToString("0.###") + " ms", left, targetY - 5, SKTextAlign.Left, labelFont, textPaint);

		float chartWidth = Math.Max(1, right - left);
		int visibleFrameCount = Math.Max(1, endFrame - startFrame + 1);
		float barStep = chartWidth / visibleFrameCount;
		float barWidth = Math.Max(1, Math.Min(8, barStep - 1));
		for (int frameIndex = startFrame; frameIndex <= endFrame; frameIndex++)
		{
			if (!TryGetSample(samples, frameIndex, out FrameSample sample))
			{
				continue;
			}

			float x = left + (frameIndex - startFrame) * barStep;
			float barHeight = (float)(Math.Min(sample.DurationMs, maxMs) / maxMs) * height;
			SKPaint paint = sample.DurationMs > TargetFrameMs ? slowFramePaint : framePaint;
			canvas.DrawRect(x, bottom - barHeight, barWidth, Math.Max(1, barHeight), paint);
		}

		DrawSelectionOverlay(canvas, left, top, bottom, chartWidth, startFrame, visibleFrameCount);
		canvas.DrawText(startFrame.ToString(), left, bottom + 18, SKTextAlign.Left, labelFont, textPaint);
		canvas.DrawText(endFrame.ToString(), right, bottom + 18, SKTextAlign.Right, labelFont, textPaint);
		canvas.DrawText(maxMs.ToString("0.#") + " ms", left, top - 4, SKTextAlign.Left, labelFont, textPaint);
		canvas.Restore();
	}

	private void DrawSelectionOverlay(SKCanvas canvas, float left, float top, float bottom, float chartWidth, int startFrame, int visibleFrameCount)
	{
		TimelineSelection selection = Selection;
		if (selection == null)
		{
			return;
		}

		using SKPaint selectedPaint = new SKPaint { Color = new SKColor(121, 162, 255, 180), StrokeWidth = 2, IsAntialias = false };
		using SKPaint hoverPaint = new SKPaint { Color = new SKColor(242, 244, 248, 120), StrokeWidth = 1, IsAntialias = false };
		DrawFrameMarker(canvas, selection.SelectedFrameIndex, selectedPaint, left, top, bottom, chartWidth, startFrame, visibleFrameCount);
		DrawFrameMarker(canvas, selection.HoveredFrameIndex, hoverPaint, left, top, bottom, chartWidth, startFrame, visibleFrameCount);
	}

	private static void DrawFrameMarker(SKCanvas canvas, int frameIndex, SKPaint paint, float left, float top, float bottom, float chartWidth, int startFrame, int visibleFrameCount)
	{
		if (frameIndex < startFrame || frameIndex >= startFrame + visibleFrameCount)
		{
			return;
		}

		float x = left + (frameIndex - startFrame + 0.5f) * chartWidth / visibleFrameCount;
		canvas.DrawLine(x, top, x, bottom, paint);
	}

	private static void DrawEmptyState(SKCanvas canvas, SKRect bounds)
	{
		using SKFont font = new SKFont { Size = 15 };
		using SKPaint textPaint = new SKPaint
		{
			Color = new SKColor(154, 164, 178),
			IsAntialias = true
		};
		canvas.DrawText("No frame samples", bounds.MidX, bounds.MidY, SKTextAlign.Center, font, textPaint);
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
		if (Selection == null)
		{
			return;
		}

		if (TryGetFrameAt(position, out FrameSample sample))
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
		if (Selection == null || !TryGetFrameAt(position, out FrameSample sample))
		{
			return;
		}

		Selection.SelectedFrameIndex = sample.Index;
		Selection.SelectedFrameTimeMs = sample.DurationMs;
	}

	private bool TryGetFrameAt(Point position, out FrameSample sample)
	{
		sample = default;
		IReadOnlyList<FrameSample> samples = Samples;
		TimelineViewport viewport = Viewport;
		if (samples == null || samples.Count == 0 || viewport == null || viewport.FrameCount == 0)
		{
			return false;
		}

		double left = 16.0;
		double right = Math.Max(left + 1.0, Bounds.Width - 16.0);
		if (position.X < left || position.X > right)
		{
			return false;
		}

		int visibleFrameCount = Math.Max(1, viewport.VisibleFrameCount);
		double ratio = (position.X - left) / (right - left);
		int frameIndex = viewport.StartFrame + (int)Math.Floor(ratio * visibleFrameCount);
		frameIndex = Math.Min(viewport.EndFrame, Math.Max(viewport.StartFrame, frameIndex));
		return TryGetSample(samples, frameIndex, out sample);
	}

	private static bool TryGetSample(IReadOnlyList<FrameSample> samples, int frameIndex, out FrameSample sample)
	{
		if (frameIndex >= 0 && frameIndex < samples.Count && samples[frameIndex].Index == frameIndex)
		{
			sample = samples[frameIndex];
			return true;
		}

		for (int i = 0; i < samples.Count; i++)
		{
			if (samples[i].Index == frameIndex)
			{
				sample = samples[i];
				return true;
			}
		}

		sample = default;
		return false;
	}

	private void TimelineStatePropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		InvalidateVisual();
	}
}
