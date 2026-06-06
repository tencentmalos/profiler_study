using System;
using System.Threading;
using ProfilerStudy.Avalonia.ProfilerStats;

namespace ProfilerStudy.Avalonia;

internal static class AvaloniaSmokeTest
{
	public static int Run()
	{
		try
		{
			SessionLoader loader = new SessionLoader();
			SessionDocument document = loader.LoadSampleAsync(CancellationToken.None).GetAwaiter().GetResult();
			Assert(document != null, "document");
			Assert(document.Summary.FrameCount > 0, "frame count");
			Assert(document.FrameSamples.Length == document.Summary.FrameCount, "frame samples");
			Assert(document.Viewport.FrameCount == document.Summary.FrameCount, "viewport frame count");
			Assert(document.Viewport.VisibleFrameCount == document.Summary.FrameCount, "initial visible range");

			int initialVisibleCount = document.Viewport.VisibleFrameCount;
			document.Viewport.Zoom(2.0, 0.5);
			Assert(document.Viewport.VisibleFrameCount < initialVisibleCount, "zoom in");
			document.Viewport.ScrollFrames(Math.Max(1, document.Viewport.VisibleFrameCount / 2));
			Assert(document.Viewport.StartFrame > 0, "scroll");

			FrameSample sample = document.FrameSamples[document.Viewport.StartFrame];
			document.Selection.SelectedFrameIndex = sample.Index;
			document.Selection.SelectedFrameTimeMs = sample.DurationMs;
			Assert(document.Selection.SelectedFrameIndex == sample.Index, "selection index");
			Assert(document.Selection.SelectedFrameTimeMs > 0.0, "selection duration");

			ProfilerStatsDocumentSummary profilerStatsSummary = ProfilerStatsDocumentAnalyzer.Analyze(document);
			Assert(profilerStatsSummary.FrameSeriesPointCount == document.FrameSamples.Length, "profiler stats frame series");
			Assert(profilerStatsSummary.CustomStatPlotCount == 0, "sample custom stat plots");
			Assert(profilerStatsSummary.CustomStatCurveCount == 0, "sample custom stat curves");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.GetType().FullName);
			Console.Error.WriteLine(ex.Message);
			Console.Error.WriteLine(ex.StackTrace);
			return 1;
		}
	}

	private static void Assert(bool condition, string name)
	{
		if (!condition)
		{
			throw new InvalidOperationException("Smoke test failed: " + name);
		}
	}
}
