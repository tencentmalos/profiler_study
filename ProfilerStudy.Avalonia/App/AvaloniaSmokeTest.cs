using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			var adapter = new ProfilerTimelineAdapter();
			ProfilerTimelinePlotModel plotModel = adapter.CreatePlotModel(document);
			Assert(plotModel.Metadata.PlotList.Count > 0, "profiler timeline plot metadata");
			Assert(plotModel.Metadata.PlotList.Any(item => item.IsMainPlot), "profiler timeline main plot");
			AssertProfilerScopeFlameConfig(adapter, document);
			AssertSourcePathMapping();
			AssertThemeSettings();
			AssertTableSorting();
			AssertFlameChartControl();
			AssertFrameTimelineCategories();
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

	private static void AssertFrameTimelineCategories()
	{
		Assert(FrameTimelineControl.GetFrameTimeCategory(16.0, 16.0) == CoreUtils.FrameTimeCategory.InBudget, "frame category target boundary");
		Assert(FrameTimelineControl.GetFrameTimeCategory(16.001, 16.0) == CoreUtils.FrameTimeCategory.Warning, "frame category warning boundary");
		Assert(FrameTimelineControl.GetFrameTimeCategory(32.0, 16.0) == CoreUtils.FrameTimeCategory.Warning, "frame category alert boundary");
		Assert(FrameTimelineControl.GetFrameTimeCategory(32.001, 16.0) == CoreUtils.FrameTimeCategory.Alert, "frame category alert over boundary");
	}

	private static void AssertSourcePathMapping()
	{
		string tempRoot = Path.Combine(Path.GetTempPath(), "ProfilerStudy.Avalonia.SourceMap." + Guid.NewGuid().ToString("N"));
		try
		{
			string localRoot = Path.Combine(tempRoot, "local");
			string localFile = Path.Combine(localRoot, "src", "main.cpp");
			Directory.CreateDirectory(Path.GetDirectoryName(localFile));
			File.WriteAllText(localFile, "int main() { return 0; }");

			string mapped = SourcePathMapper.Resolve("/build/agent/project/src/main.cpp", "/build/agent/project", localRoot);
			Assert(string.Equals(mapped, localFile, StringComparison.Ordinal), "source path mapping");

			string unmapped = SourcePathMapper.Resolve("/other/project/src/main.cpp", "/build/agent/project", localRoot);
			Assert(string.Equals(unmapped, "/other/project/src/main.cpp", StringComparison.Ordinal), "source path mapping mismatch");
		}
		finally
		{
			if (Directory.Exists(tempRoot))
			{
				Directory.Delete(tempRoot, true);
			}
		}
	}

	private static void AssertThemeSettings()
	{
		var settings = new AppSettings();
		var themeHost = new AppThemeService.TestThemeHost();
		var themeService = new AppThemeService(settings, themeHost, _ => { });
		Assert(themeService.IsLightThemeActive, "default light theme");
		Assert(!themeService.IsDarkThemeActive, "default dark theme");
		Assert(themeService.ActiveColorThemeName == "Blue", "default color theme");

		themeService.ChangeBaseTheme("Dark");
		Assert(settings.BaseTheme == "Dark", "saved dark theme");
		Assert(themeHost.RequestedBaseTheme == "Dark", "applied dark theme");
		Assert(themeService.IsDarkThemeActive, "dark theme active");

		themeService.ChangeColorTheme("Green");
		Assert(settings.ColorTheme == "Green", "saved color theme");
		Assert(themeHost.RequestedColorTheme == "Green", "applied color theme");
		Assert(themeService.ActiveColorThemeName == "Green", "active color theme");
	}

	private static void AssertProfilerScopeFlameConfig(ProfilerTimelineAdapter adapter, SessionDocument document)
	{
		var config = adapter.CreateScopeFlameGraphConfig(document, 8, 64);
		Assert(config.TimelineEnd > config.TimelineStart, "scope flame timeline range");
		Assert(config.MaxStackDepth >= 1, "scope flame depth");
		if (document.Session == null)
		{
			Assert(config.StackFrames.Count == 0, "sample scope flame frames");
		}
		else
		{
			Assert(config.StackFrames.All(item => item.EndTime > item.StartTime), "scope flame frame duration");
			Assert(config.StackFrames.All(item => item.FrameIndex >= 0), "scope flame frame index");
		}
	}

	private static void AssertTableSorting()
	{
		IReadOnlyList<ScopeHotspotRow> hotspots = ProfilerTableSorter.SortHotspots(new[]
		{
			new ScopeHotspotRow("B", 10.0, 2, 7.0, 1),
			new ScopeHotspotRow("A", 20.0, 1, 20.0, 1),
		}, "TotalTime", true);
		Assert(hotspots[0].Name == "A", "scope hotspot sort");

		IReadOnlyList<ScopeFrameDetailRow> frameScopes = ProfilerTableSorter.SortFrameScopes(new[]
		{
			new ScopeFrameDetailRow("Render", 0, "Late", 4.0, 1.0, "late.cpp:4", "late.cpp", 4),
			new ScopeFrameDetailRow("Render", 0, "Early", 1.0, 2.0, "early.cpp:1", "early.cpp", 1),
		}, "Start", false);
		Assert(frameScopes[0].Name == "Early", "frame scope sort");

		IReadOnlyList<SelectedFrameCounterRow> counters = ProfilerTableSorter.SortCounters(new[]
		{
			new SelectedFrameCounterRow("GPU", "Small", 1.0, 1.0, "ms"),
			new SelectedFrameCounterRow("GPU", "Large", 8.0, 1.0, "ms"),
		}, "Value", true);
		Assert(counters[0].Name == "Large", "counter sort");
	}

	private static void AssertFlameChartControl()
	{
		var control = new SelectedFrameFlameChartControl
		{
			Rows = new[]
			{
				new ScopeFrameDetailRow("Render", 0, "Frame", 0.0, 16.0, string.Empty, string.Empty, -1),
				new ScopeFrameDetailRow("Render", 1, "Draw", 2.0, 6.0, string.Empty, string.Empty, -1),
			}
		};
		Assert(control.Rows.Count == 2, "flame chart rows");
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
