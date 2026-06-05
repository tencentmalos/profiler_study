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
	private const float RulerHeight = 30.0f;
	private const float FrameStripHeight = 20.0f;
	private const float HeaderHeight = RulerHeight + FrameStripHeight;
	private const float AxisWidth = 54.0f;
	private const float RightPadding = 12.0f;
	private const float GraphTopGap = 12.0f;
	private const float BottomPadding = 24.0f;
	private const float MinFrameLineWidth = 5.0f;

	private static readonly SKColor TimelineBackground = new SKColor(200, 200, 200);
	private static readonly SKColor TimelineFrameFill = new SKColor(99, 99, 99);
	private static readonly SKColor TimelineFrameText = new SKColor(245, 245, 245);
	private static readonly SKColor FrameLine = SKColors.White;
	private static readonly SKColor GraphBackground = SKColors.LightGray;
	private static readonly SKColor FrameBar = SKColors.Green;
	private static readonly SKColor FrameBarWarning = SKColors.Orange;
	private static readonly SKColor FrameBarAlert = SKColors.Red;
	private static readonly SKColor TargetLine = new SKColor(182, 82, 0, 128);
	private static readonly SKColor SelectionFill = new SKColor(0, 128, 255, 32);
	private static readonly SKColor SelectionLine = new SKColor(64, 170, 255, 160);
	private static readonly SKColor HoverLine = new SKColor(255, 255, 255, 180);

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
		canvas.Clear(TimelineBackground);

		IReadOnlyList<FrameSample> samples = Samples;
		TimelineViewport viewport = Viewport;
		if (samples == null || samples.Count == 0 || viewport == null || viewport.FrameCount == 0)
		{
			DrawEmptyState(canvas, bounds);
			canvas.Restore();
			return;
		}

		float stripLeft = bounds.Left;
		float plotLeft = bounds.Left + AxisWidth;
		float right = bounds.Right - RightPadding;
		float stripRight = bounds.Right;
		float graphTop = bounds.Top + HeaderHeight + GraphTopGap;
		float bottom = bounds.Bottom - BottomPadding;
		float graphHeight = Math.Max(1, bottom - graphTop);
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

		using SKFont labelFont = new SKFont { Size = 11 };
		using SKFont frameFont = new SKFont { Size = 10 };
		using SKPaint blackTextPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
		using SKPaint whiteTextPaint = new SKPaint { Color = TimelineFrameText, IsAntialias = true };
		using SKPaint tickPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1, IsAntialias = false };
		using SKPaint minorTickPaint = new SKPaint { Color = new SKColor(90, 90, 90), StrokeWidth = 1, IsAntialias = false };
		using SKPaint stripFillPaint = new SKPaint { Color = TimelineFrameFill, IsAntialias = false };
		using SKPaint stripLinePaint = new SKPaint { Color = FrameLine, StrokeWidth = 1, IsAntialias = false };
		using SKPaint graphBackPaint = new SKPaint { Color = GraphBackground, IsAntialias = false };
		using SKPaint gridPaint = new SKPaint { Color = new SKColor(160, 160, 160), StrokeWidth = 1, IsAntialias = false };
		using SKPaint framePaint = new SKPaint { Color = FrameBar, IsAntialias = false };
		using SKPaint warningFramePaint = new SKPaint { Color = FrameBarWarning, IsAntialias = false };
		using SKPaint alertFramePaint = new SKPaint { Color = FrameBarAlert, IsAntialias = false };
		using SKPaint targetPaint = new SKPaint { Color = TargetLine, StrokeWidth = 1, IsAntialias = false };

		canvas.DrawRect(new SKRect(bounds.Left, graphTop, bounds.Right, bottom), graphBackPaint);

		float chartWidth = Math.Max(1, right - plotLeft);
		int visibleFrameCount = Math.Max(1, endFrame - startFrame + 1);
		float barStep = chartWidth / visibleFrameCount;
		float barWidth = Math.Max(1, Math.Min(8, barStep - 1));

		DrawRuler(canvas, samples, startFrame, endFrame, stripLeft, stripRight, labelFont, blackTextPaint, tickPaint, minorTickPaint);
		DrawFrameStrip(canvas, startFrame, endFrame, stripLeft, stripRight, frameFont, stripFillPaint, stripLinePaint, whiteTextPaint);

		for (int i = 0; i <= 4; i++)
		{
			float y = graphTop + graphHeight * i / 4f;
			canvas.DrawLine(plotLeft, y, right, y, gridPaint);
			double labelValue = maxMs * (4 - i) / 4.0;
			canvas.DrawText(labelValue.ToString("0.#") + "ms", plotLeft - 6, y + 4, SKTextAlign.Right, labelFont, blackTextPaint);
		}

		float targetY = bottom - (float)(Math.Min(TargetFrameMs, maxMs) / maxMs) * graphHeight;
		canvas.DrawLine(plotLeft, targetY, right, targetY, targetPaint);
		canvas.DrawText(TargetFrameMs.ToString("0.###") + "ms", right, targetY - 4, SKTextAlign.Right, labelFont, blackTextPaint);

		for (int frameIndex = startFrame; frameIndex <= endFrame; frameIndex++)
		{
			if (!TryGetSample(samples, frameIndex, out FrameSample sample))
			{
				continue;
			}

			float x = plotLeft + (frameIndex - startFrame) * barStep;
			float barHeight = (float)(Math.Min(sample.DurationMs, maxMs) / maxMs) * graphHeight;
			SKPaint paint = sample.DurationMs >= TargetFrameMs * 1.5 ? alertFramePaint : (sample.DurationMs > TargetFrameMs ? warningFramePaint : framePaint);
			canvas.DrawRect(x, bottom - barHeight, barWidth, Math.Max(1, barHeight), paint);
		}

		DrawSelectionOverlay(canvas, plotLeft, graphTop, bottom, chartWidth, startFrame, visibleFrameCount);
		canvas.DrawText("Frame " + startFrame, plotLeft, bottom + 17, SKTextAlign.Left, labelFont, blackTextPaint);
		canvas.DrawText("Frame " + endFrame, right, bottom + 17, SKTextAlign.Right, labelFont, blackTextPaint);
		canvas.Restore();
	}

	private static void DrawRuler(SKCanvas canvas, IReadOnlyList<FrameSample> samples, int startFrame, int endFrame, float left, float right, SKFont font, SKPaint textPaint, SKPaint tickPaint, SKPaint minorTickPaint)
	{
		float width = Math.Max(1, right - left);
		int visibleFrameCount = Math.Max(1, endFrame - startFrame + 1);
		double visibleMs = 0.0;
		for (int i = startFrame; i <= endFrame; i++)
		{
			if (TryGetSample(samples, i, out FrameSample sample))
			{
				visibleMs += sample.DurationMs;
			}
		}

		double majorStepMs = ChooseTimeStep(Math.Max(1.0, visibleMs), Math.Max(1.0, width));
		double nextMajorMs = 0.0;
		double elapsedMs = 0.0;
		for (int frameIndex = startFrame; frameIndex <= endFrame; frameIndex++)
		{
			if (!TryGetSample(samples, frameIndex, out FrameSample sample))
			{
				continue;
			}

			float frameStartX = left + (float)((frameIndex - startFrame) * width / visibleFrameCount);
			float frameEndX = left + (float)((frameIndex - startFrame + 1) * width / visibleFrameCount);
			while (nextMajorMs <= elapsedMs + sample.DurationMs)
			{
				double ratioInsideFrame = sample.DurationMs <= 0.0 ? 0.0 : (nextMajorMs - elapsedMs) / sample.DurationMs;
				float x = frameStartX + (frameEndX - frameStartX) * (float)Math.Clamp(ratioInsideFrame, 0.0, 1.0);
				canvas.DrawLine(x, 0, x, 10, tickPaint);
				canvas.DrawText(FormatMs(nextMajorMs), x, 22, SKTextAlign.Center, font, textPaint);
				for (int i = 1; i < 5; i++)
				{
					float minorX = x + (float)(majorStepMs * i / 5.0 / Math.Max(visibleMs, 1.0) * width);
					if (minorX < right)
					{
						canvas.DrawLine(minorX, 0, minorX, 5, minorTickPaint);
					}
				}
				nextMajorMs += majorStepMs;
			}
			elapsedMs += sample.DurationMs;
		}
	}

	private static void DrawFrameStrip(SKCanvas canvas, int startFrame, int endFrame, float left, float right, SKFont font, SKPaint fillPaint, SKPaint linePaint, SKPaint textPaint)
	{
		float top = RulerHeight;
		float bottom = RulerHeight + FrameStripHeight;
		int visibleFrameCount = Math.Max(1, endFrame - startFrame + 1);
		float width = Math.Max(1, right - left);
		float frameStep = width / visibleFrameCount;
		for (int frameIndex = startFrame; frameIndex <= endFrame; frameIndex++)
		{
			float x = left + (frameIndex - startFrame) * frameStep;
			float nextX = left + (frameIndex - startFrame + 1) * frameStep;
			float frameWidth = nextX - x;
			if (frameWidth < MinFrameLineWidth)
			{
				continue;
			}

			SKRect rect = new SKRect(x, top, nextX, bottom);
			canvas.DrawRect(rect, fillPaint);
			canvas.DrawLine(rect.Left, rect.Top, rect.Left, rect.Bottom, linePaint);
			canvas.DrawLine(rect.Right, rect.Top, rect.Right, rect.Bottom, linePaint);
			if (frameWidth > 52.0f)
			{
				canvas.DrawText("Frame: " + frameIndex, rect.MidX, rect.MidY + 4, SKTextAlign.Center, font, textPaint);
			}
			else if (frameWidth > 24.0f)
			{
				canvas.DrawText(frameIndex.ToString(), rect.MidX, rect.MidY + 4, SKTextAlign.Center, font, textPaint);
			}
		}
	}

	private static double ChooseTimeStep(double visibleMs, double width)
	{
		double[] steps = { 0.01, 0.05, 0.1, 0.5, 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000 };
		double idealMs = visibleMs * 100.0 / width;
		double best = steps[0];
		double bestDiff = double.MaxValue;
		for (int i = 0; i < steps.Length; i++)
		{
			double diff = Math.Abs(steps[i] - idealMs);
			if (diff < bestDiff)
			{
				best = steps[i];
				bestDiff = diff;
			}
		}
		return best;
	}

	private static string FormatMs(double ms)
	{
		if (ms >= 1000.0)
		{
			return (ms / 1000.0).ToString("0.#") + "s";
		}
		if (ms < 1.0)
		{
			return (ms * 1000.0).ToString("0.#") + "us";
		}
		return ms.ToString("0.#") + "ms";
	}

	private void DrawSelectionOverlay(SKCanvas canvas, float left, float top, float bottom, float chartWidth, int startFrame, int visibleFrameCount)
	{
		TimelineSelection selection = Selection;
		if (selection == null)
		{
			return;
		}

		using SKPaint selectedFillPaint = new SKPaint { Color = SelectionFill, IsAntialias = false };
		using SKPaint selectedPaint = new SKPaint { Color = SelectionLine, StrokeWidth = 2, IsAntialias = false };
		using SKPaint hoverPaint = new SKPaint { Color = HoverLine, StrokeWidth = 1, IsAntialias = false };
		FillFrameMarker(canvas, selection.SelectedFrameIndex, selectedFillPaint, left, top, bottom, chartWidth, startFrame, visibleFrameCount);
		DrawFrameMarker(canvas, selection.SelectedFrameIndex, selectedPaint, left, top, bottom, chartWidth, startFrame, visibleFrameCount);
		DrawFrameMarker(canvas, selection.HoveredFrameIndex, hoverPaint, left, top, bottom, chartWidth, startFrame, visibleFrameCount);
	}

	private static void FillFrameMarker(SKCanvas canvas, int frameIndex, SKPaint paint, float left, float top, float bottom, float chartWidth, int startFrame, int visibleFrameCount)
	{
		if (frameIndex < startFrame || frameIndex >= startFrame + visibleFrameCount)
		{
			return;
		}

		float x0 = left + (frameIndex - startFrame) * chartWidth / visibleFrameCount;
		float x1 = left + (frameIndex - startFrame + 1) * chartWidth / visibleFrameCount;
		canvas.DrawRect(new SKRect(x0, top, x1, bottom), paint);
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
			Color = SKColors.Black,
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

		double left = AxisWidth;
		double right = Math.Max(left + 1.0, Bounds.Width - RightPadding);
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
