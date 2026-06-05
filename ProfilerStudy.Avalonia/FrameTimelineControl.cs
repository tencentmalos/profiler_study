using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
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

	static FrameTimelineControl()
	{
		AffectsRender<FrameTimelineControl>(SamplesProperty, TargetFrameMsProperty);
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
		if (samples == null || samples.Count == 0)
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
		for (int i = 0; i < samples.Count; i++)
		{
			maxMs = Math.Max(maxMs, samples[i].DurationMs);
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
		float barStep = chartWidth / samples.Count;
		float barWidth = Math.Max(1, Math.Min(8, barStep - 1));
		for (int i = 0; i < samples.Count; i++)
		{
			FrameSample sample = samples[i];
			float x = left + i * barStep;
			float barHeight = (float)(Math.Min(sample.DurationMs, maxMs) / maxMs) * height;
			SKPaint paint = sample.DurationMs > TargetFrameMs ? slowFramePaint : framePaint;
			canvas.DrawRect(x, bottom - barHeight, barWidth, Math.Max(1, barHeight), paint);
		}

		canvas.DrawText("0", left, bottom + 18, SKTextAlign.Left, labelFont, textPaint);
		canvas.DrawText(samples[samples.Count - 1].Index.ToString(), right, bottom + 18, SKTextAlign.Right, labelFont, textPaint);
		canvas.DrawText(maxMs.ToString("0.#") + " ms", left, top - 4, SKTextAlign.Left, labelFont, textPaint);
		canvas.Restore();
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
}
