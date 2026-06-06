using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ProfilerStudy.Avalonia.ProfilerStats;

namespace ProfilerStudy.Avalonia;

internal static class AvaloniaSmokeTest
{
	public static int Run(string profilerPath = null)
	{
		try
		{
			SessionLoader loader = new SessionLoader();
			SessionDocument document = LoadDocument(loader, profilerPath);
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
			document.Viewport.SetRange(10, 20);
			Assert(document.Viewport.StartFrame == 10 && document.Viewport.EndFrame == 20, "explicit viewport range");

			FrameSample sample = document.FrameSamples[document.Viewport.StartFrame];
			document.Selection.SelectedFrameIndex = sample.Index;
			document.Selection.SelectedFrameTimeMs = sample.DurationMs;
			Assert(document.Selection.SelectedFrameIndex == sample.Index, "selection index");
			Assert(document.Selection.SelectedFrameTimeMs > 0.0, "selection duration");
			FrameSample slowestFrame = FindSlowestFrame(document.FrameSamples);
			document.Viewport.SetRange(Math.Max(0, slowestFrame.Index - 5), Math.Min(document.Summary.FrameCount - 1, slowestFrame.Index + 5));
			document.Selection.SelectedFrameIndex = slowestFrame.Index;
			document.Selection.SelectedFrameTimeMs = slowestFrame.DurationMs;
			Assert(document.Viewport.Contains(slowestFrame.Index), "slowest frame viewport");
			Assert(document.Selection.SelectedFrameIndex == slowestFrame.Index, "slowest frame selection");

			ProfilerStatsDocumentSummary profilerStatsSummary = ProfilerStatsDocumentAnalyzer.Analyze(document);
			Assert(profilerStatsSummary.FrameSeriesPointCount == document.FrameSamples.Length, "profiler stats frame series");
			IReadOnlyList<ScopeHotspotRow> scopeHotspots = ScopeHotspotAnalyzer.Build(document);
			IReadOnlyList<ScopeFrameDetailRow> selectedFrameScopes = ScopeFrameDetailAnalyzer.Build(document, document.Viewport.StartFrame);
			IReadOnlyList<SelectedFrameCounterRow> selectedFrameCounters = SelectedFrameCounterAnalyzer.Build(document, document.Viewport.StartFrame);
			if (document.Session == null)
			{
				Assert(profilerStatsSummary.CustomStatPlotCount == 0, "sample custom stat plots");
				Assert(profilerStatsSummary.CustomStatCurveCount == 0, "sample custom stat curves");
				Assert(scopeHotspots.Count == 0, "sample scope hotspots");
				Assert(selectedFrameScopes.Count == 0, "sample frame scope details");
				Assert(selectedFrameCounters.Count == 0, "sample frame counters");
			}
			else if (document.Session.GetCustomStats().Count > 0)
			{
				Assert(profilerStatsSummary.CustomStatPlotCount > 0, "file custom stat plots");
				Assert(profilerStatsSummary.CustomStatCurveCount > 0, "file custom stat curves");
			}
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

	private static FrameSample FindSlowestFrame(FrameSample[] samples)
	{
		FrameSample slowestFrame = samples[0];
		for (int i = 1; i < samples.Length; i++)
		{
			if (samples[i].DurationMs > slowestFrame.DurationMs)
			{
				slowestFrame = samples[i];
			}
		}
		return slowestFrame;
	}

	private static SessionDocument LoadDocument(SessionLoader loader, string profilerPath)
	{
		if (string.IsNullOrWhiteSpace(profilerPath))
		{
			return loader.LoadSampleAsync(CancellationToken.None).GetAwaiter().GetResult();
		}

		if (!File.Exists(profilerPath))
		{
			throw new FileNotFoundException("Smoke test profiler file not found.", profilerPath);
		}

		return loader.LoadFileAsync(profilerPath, CancellationToken.None).GetAwaiter().GetResult();
	}
}
