using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
			document.Selection.SelectFrame(sample.Index, sample.DurationMs);
			Assert(document.Selection.SelectedFrameIndex == sample.Index, "selection index");
			Assert(document.Selection.SelectedFrameTimeMs > 0.0, "selection duration");
			document.Selection.SelectFrame(sample.Index + 1, 123.456);
			Assert(document.Selection.SelectedFrameIndex == sample.Index + 1 && document.Selection.SelectedFrameTimeMs == 123.456, "selection paired update");
			document.Selection.ClearSelectedFrame();
			Assert(document.Selection.SelectedFrameIndex == -1 && document.Selection.SelectedFrameTimeMs == 0.0, "selection paired clear");
			FrameSample slowestFrame = FindSlowestFrame(document.FrameSamples);
			document.Viewport.SetRange(Math.Max(0, slowestFrame.Index - 5), Math.Min(document.Summary.FrameCount - 1, slowestFrame.Index + 5));
			document.Selection.SelectFrame(slowestFrame.Index, slowestFrame.DurationMs);
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
			AssertTimelineSelectionContract();
			AssertTimelineSelectionStatusContract(document);
			AssertTimelineZoomCommands(document);
			AssertFrameTimelinePointerGestureContract();
			AssertFrameTimelineRenderModel(document);
			AssertFrameTimelineNavigationContract();
			AssertSessionScrollbarFrameStripContract();
			AssertTracyTraceLoad();
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

	private static void AssertTimelineZoomCommands(SessionDocument document)
	{
		document.Viewport.SetRange(10, 109);
		MainWindowViewModel viewModel = CreateSmokeViewModel(document);

		int visibleBeforeZoomIn = viewModel.Viewport.VisibleFrameCount;
		viewModel.ZoomTimelineInCommand.Execute(null);
		Assert(viewModel.Viewport.VisibleFrameCount < visibleBeforeZoomIn, "timeline zoom in command narrows visible range");

		int visibleBeforeZoomOut = viewModel.Viewport.VisibleFrameCount;
		viewModel.ZoomTimelineOutCommand.Execute(null);
		Assert(viewModel.Viewport.VisibleFrameCount > visibleBeforeZoomOut, "timeline zoom out command widens visible range");
	}

	private static void AssertTimelineSelectionContract()
	{
		Assert(typeof(TimelineSelection).GetProperty(nameof(TimelineSelection.SelectedFrameIndex))?.SetMethod?.IsPublic is false, "timeline selection selected index setter hidden");
		Assert(typeof(TimelineSelection).GetProperty(nameof(TimelineSelection.SelectedFrameTimeMs))?.SetMethod?.IsPublic is false, "timeline selection selected time setter hidden");
		Assert(typeof(TimelineSelection).GetProperty(nameof(TimelineSelection.HoveredFrameIndex))?.SetMethod?.IsPublic is false, "timeline selection hovered index setter hidden");
		Assert(typeof(TimelineSelection).GetProperty(nameof(TimelineSelection.HoveredFrameTimeMs))?.SetMethod?.IsPublic is false, "timeline selection hovered time setter hidden");

		TimelineSelection selection = new TimelineSelection();
		selection.SelectFrame(7, 18.25);
		Assert(selection.SelectedFrameIndex == 7 && selection.SelectedFrameTimeMs == 18.25, "timeline selection selected pair");
		selection.HoverFrame(9, 20.5);
		Assert(selection.HoveredFrameIndex == 9 && selection.HoveredFrameTimeMs == 20.5, "timeline selection hovered pair");
		selection.Clear();
		Assert(selection.SelectedFrameIndex == -1 && selection.SelectedFrameTimeMs == 0.0, "timeline selection selected pair clear");
		Assert(selection.HoveredFrameIndex == -1 && selection.HoveredFrameTimeMs == 0.0, "timeline selection hovered pair clear");
	}

	private static void AssertTimelineSelectionStatusContract(SessionDocument document)
	{
		document.Selection.Clear();
		MainWindowViewModel viewModel = CreateSmokeViewModel(document);
		viewModel.Selection.SelectFrame(17, 23.5);
		Assert(viewModel.SelectedFrameStatusText == "Frame: 17 (23.5 ms)", "timeline selected frame main status updates");
		Assert(viewModel.StatusBarSelectedFrameText == "17 (23.5 ms)", "timeline selected frame statusbar updates");
		Assert(viewModel.FooterText.Contains("selected frame 17 (23.5 ms)", StringComparison.Ordinal), "timeline selected frame footer updates");

		viewModel.Selection.HoverFrame(19, 24.75);
		Assert(viewModel.StatusBarHoveredFrameText == "19 (24.75 ms)", "timeline hovered frame statusbar updates");
		Assert(viewModel.FooterText.Contains("hover frame 19 (24.75 ms)", StringComparison.Ordinal), "timeline hovered frame footer updates");
	}

	private static void AssertFrameTimelinePointerGestureContract()
	{
		Type gestureType = typeof(FrameTimelineControl).Assembly.GetType("ProfilerStudy.Avalonia.FrameTimelinePointerGesture");
		Assert(gestureType != null, "frame timeline pointer gesture type");
		MethodInfo isDragDistanceExceeded = gestureType.GetMethod("IsDragDistanceExceeded", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		MethodInfo calculatePanDeltaFrames = gestureType.GetMethod("CalculatePanDeltaFrames", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		Assert(isDragDistanceExceeded != null, "frame timeline pointer drag threshold method");
		Assert(calculatePanDeltaFrames != null, "frame timeline pointer pan delta method");

		bool smallMoveIsDrag = (bool)isDragDistanceExceeded.Invoke(null, new object[] { 40.0, 42.0 });
		bool largeMoveIsDrag = (bool)isDragDistanceExceeded.Invoke(null, new object[] { 40.0, 48.0 });
		Assert(smallMoveIsDrag is false, "frame timeline click movement stays selection");
		Assert(largeMoveIsDrag, "frame timeline drag movement starts pan");

		int rightDragDelta = (int)calculatePanDeltaFrames.Invoke(null, new object[] { 80.0, 40.0, 100, 200.0 });
		int leftDragDelta = (int)calculatePanDeltaFrames.Invoke(null, new object[] { 40.0, 80.0, 100, 200.0 });
		Assert(rightDragDelta < 0, "frame timeline right drag pans earlier frames");
		Assert(leftDragDelta > 0, "frame timeline left drag pans later frames");
		Assert(Math.Abs(leftDragDelta) == Math.Abs(rightDragDelta), "frame timeline pan delta symmetric");
	}

	private static void AssertFrameTimelineRenderModel(SessionDocument document)
	{
		Assert(FrameTimelineFrameClassifier.GetFrameTimeCategory(16.0, 16.0) == CoreUtils.FrameTimeCategory.InBudget, "frame category target boundary");
		Assert(FrameTimelineFrameClassifier.GetFrameTimeCategory(16.001, 16.0) == CoreUtils.FrameTimeCategory.Warning, "frame category warning boundary");
		Assert(FrameTimelineFrameClassifier.GetFrameTimeCategory(32.0, 16.0) == CoreUtils.FrameTimeCategory.Warning, "frame category alert boundary");
		Assert(FrameTimelineFrameClassifier.GetFrameTimeCategory(32.001, 16.0) == CoreUtils.FrameTimeCategory.Alert, "frame category alert over boundary");

		document.Viewport.SetRange(10, 20);
		FrameTimelineRenderModel model = FrameTimelineRenderModel.Create(
			document.FrameSamples,
			document.Viewport,
			document.Summary.TargetFrameTimeMs);
		Assert(model.HasItems, "frame timeline render model items");
		Assert(model.StartFrame == 10 && model.EndFrame == 20, "frame timeline render model range");
		Assert(model.Items[0].Index >= 10, "frame timeline first item range");
		Assert(model.Items[model.Items.Count - 1].Index <= 20, "frame timeline last item range");
		FrameTimelineRenderItem middleItem = model.Items[model.Items.Count / 2];
		Assert(model.TryGetItem(middleItem.Index, out FrameTimelineRenderItem hitItem), "frame timeline hit item");
		Assert(hitItem.Index == middleItem.Index && hitItem.DurationMs == middleItem.DurationMs, "frame timeline hit item identity");

		FrameSample[] sparseSamples =
		{
			new FrameSample(0, 10.0),
			new FrameSample(5, 20.0),
			new FrameSample(10, 40.0)
		};
		TimelineViewport sparseViewport = TimelineViewport.CreateForFrames(11);
		FrameTimelineRenderModel sparseModel = FrameTimelineRenderModel.Create(sparseSamples, sparseViewport, 16.0);
		Assert(sparseModel.TryHitFrameCoordinate(5.99, out FrameTimelineRenderItem sparseHit), "frame timeline sparse band hit");
		Assert(sparseHit.Index == 5, "frame timeline sparse band hit identity");
		Assert(sparseModel.TryHitFrameCoordinate(6.0, out _) is false, "frame timeline sparse next gap miss");
		Assert(sparseModel.TryHitFrameCoordinate(2.5, out _) is false, "frame timeline sparse gap miss");

		FrameTimelinePixelLayout layout = FrameTimelinePixelLayout.Create(sparseModel, new global::Avalonia.Rect(0, 0, 110, 55));
		Assert(layout.TryHit(new global::Avalonia.Point(59.9, 20), out FrameTimelineRenderItem pixelHit), "frame timeline pixel hit");
		Assert(pixelHit.Index == 5, "frame timeline pixel hit identity");
		Assert(Math.Abs(layout.GetFrameCoordinate(new global::Avalonia.Point(55, 20)) - 5.5) < 0.0001, "frame timeline pixel coordinate");
		Assert(layout.TryHit(new global::Avalonia.Point(60, 20), out _) is false, "frame timeline sparse pixel gap miss");
		TimelineViewport sparseTailViewport = TimelineViewport.CreateForFrames(21);
		FrameTimelineRenderModel sparseTailModel = FrameTimelineRenderModel.Create(
			new[] { new FrameSample(0, 10.0), new FrameSample(10, 20.0) },
			sparseTailViewport,
			16.0);
		Assert(sparseTailModel.EndFrame == 20, "frame timeline sparse viewport end preserved");
		FrameTimelinePixelLayout sparseTailLayout = FrameTimelinePixelLayout.Create(sparseTailModel, new global::Avalonia.Rect(0, 0, 210, 55));
		Assert(Math.Abs(sparseTailLayout.GetFrameCoordinate(new global::Avalonia.Point(105, 20)) - 10.5) < 0.0001, "frame timeline sparse viewport coordinate");

		FrameSample[] denseSamples = Enumerable.Range(0, 20)
			.Select(index => new FrameSample(index, index == 7 ? 80.0 : 5.0))
			.ToArray();
		TimelineViewport denseViewport = TimelineViewport.CreateForFrames(denseSamples.Length);
		FrameTimelineRenderModel denseModel = FrameTimelineRenderModel.Create(denseSamples, denseViewport, 16.0);
		FrameTimelinePixelLayout denseLayout = FrameTimelinePixelLayout.Create(denseModel, new global::Avalonia.Rect(0, 0, 5, 50));
		Assert(denseLayout.Bars.Count <= 5, "frame timeline dense pixel columns");
		Assert(denseLayout.Bars.Any(bar => bar.Item.Index == 7 && bar.Item.Category == CoreUtils.FrameTimeCategory.Alert), "frame timeline dense keeps alert frame");
		Assert(denseLayout.TryHit(new global::Avalonia.Point(1.875, 20), out FrameTimelineRenderItem denseHit), "frame timeline dense exact hit");
		Assert(denseHit.Index == 7, "frame timeline dense exact hit identity");
		global::Avalonia.Rect denseRepresentativeRect = denseLayout.Bars.First(bar => bar.Item.Index == 7).Rect;
		AssertTryGetOverlayRect(
			denseLayout,
			7,
			new global::Avalonia.Rect(denseRepresentativeRect.X, denseLayout.Bounds.Y, denseRepresentativeRect.Width, denseLayout.Bounds.Height),
			"frame timeline dense overlay follows representative column");
		AssertTryGetOverlayRect(
			denseLayout,
			6,
			new global::Avalonia.Rect(denseRepresentativeRect.X, denseLayout.Bounds.Y, denseRepresentativeRect.Width, denseLayout.Bounds.Height),
			"frame timeline dense overlay keeps non representative frame visible");

		FrameSample[] stableHeightSamples =
		{
			new FrameSample(0, 5.0),
			new FrameSample(1, 10.0)
		};
		TimelineViewport stableHeightViewport = TimelineViewport.CreateForFrames(stableHeightSamples.Length);
		FrameTimelinePixelLayout lowTargetLayout = FrameTimelinePixelLayout.Create(
			FrameTimelineRenderModel.Create(stableHeightSamples, stableHeightViewport, 8.0),
			new global::Avalonia.Rect(0, 0, 20, 50));
		FrameTimelinePixelLayout highTargetLayout = FrameTimelinePixelLayout.Create(
			FrameTimelineRenderModel.Create(stableHeightSamples, stableHeightViewport, 60.0),
			new global::Avalonia.Rect(0, 0, 20, 50));
		Assert(Math.Abs(lowTargetLayout.Bars[1].Rect.Height - highTargetLayout.Bars[1].Rect.Height) < 0.0001, "frame timeline target independent bar height");
		Assert(lowTargetLayout.TargetLineY != highTargetLayout.TargetLineY, "frame timeline target line moves");

		CountingFrameSampleList manySamples = new CountingFrameSampleList(10000);
		TimelineViewport tailViewport = TimelineViewport.CreateForFrames(10000);
		tailViewport.SetRange(9900, 9910);
		FrameTimelineRenderModel tailModel = FrameTimelineRenderModel.Create(manySamples, tailViewport, 16.0);
		Assert(tailModel.HasItems && tailModel.Items[0].Index == 9900, "frame timeline tail model");
		Assert(manySamples.AccessCount < 100, "frame timeline tail model sample access count");
	}

	private static void AssertTryGetOverlayRect(FrameTimelinePixelLayout layout, int frameIndex, global::Avalonia.Rect expectedRect, string name)
	{
		Assert(layout.TryGetOverlayRect(frameIndex, out global::Avalonia.Rect rect), name + " result");
		Assert(rect == expectedRect, name + " rect");
	}

	private static void AssertFrameTimelineNavigationContract()
	{
		FrameTimelineRange range = FrameTimelineNavigation.CenterRange(500, 1000, 37);
		Assert(range.StartFrame <= 500 && range.EndFrame >= 500, "frame timeline navigation contains frame");
		Assert(range.EndFrame - range.StartFrame + 1 == 37, "frame timeline navigation preserves visible count");
		FrameTimelineRange tailRange = FrameTimelineNavigation.CenterRange(999, 1000, 37);
		Assert(tailRange.StartFrame == 963 && tailRange.EndFrame == 999, "frame timeline navigation clamps tail range");
	}

	private static void AssertSessionScrollbarFrameStripContract()
	{
		Assert(typeof(SessionScrollbarControl).GetProperty("Samples") != null, "session scrollbar samples property");
		Assert(typeof(SessionScrollbarControl).GetProperty("TargetFrameMs") != null, "session scrollbar target property");

		FrameSample[] samples = Enumerable.Range(0, 20)
			.Select(index => new FrameSample(index, index == 12 ? 40.0 : 5.0))
			.ToArray();
		TimelineViewport viewport = TimelineViewport.CreateForFrames(samples.Length);
		SessionScrollbarFrameStripLayout layout = SessionScrollbarFrameStripLayout.Create(samples, viewport, 16.0, new global::Avalonia.Rect(0, 0, 5, 8));
		Assert(layout.Bars.Count <= 5, "session scrollbar frame strip dense columns");
		Assert(layout.Bars.Any(bar => bar.Category == CoreUtils.FrameTimeCategory.Alert), "session scrollbar frame strip keeps alert");

		global::Avalonia.Rect fractionalTrack = new global::Avalonia.Rect(0, 0, 5.5, 8);
		SessionScrollbarFrameStripLayout fractionalLayout = SessionScrollbarFrameStripLayout.Create(samples, viewport, 16.0, fractionalTrack);
		Assert(fractionalLayout.Bars.All(bar => bar.Rect.Right <= fractionalTrack.Right), "session scrollbar frame strip clipped to track");
	}

	private static MainWindowViewModel CreateSmokeViewModel(SessionDocument document)
	{
		SmokeAppSettingsService appSettingsService = new SmokeAppSettingsService();
		AppThemeService appThemeService = new AppThemeService(appSettingsService.Load(), new SmokeThemeHost(), appSettingsService.Save);
		MainWindowViewModel viewModel = new MainWindowViewModel(new SmokeSourceViewerLauncher(), appSettingsService, appThemeService);
		MethodInfo applyDocument = typeof(MainWindowViewModel).GetMethod("ApplyDocument", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert(applyDocument != null, "timeline smoke apply document hook");
		applyDocument.Invoke(viewModel, new object[] { document });
		return viewModel;
	}

	private sealed class CountingFrameSampleList : IReadOnlyList<FrameSample>
	{
		private readonly int m_Count;

		public CountingFrameSampleList(int count)
		{
			m_Count = count;
		}

		public int AccessCount { get; private set; }

		public int Count => m_Count;

		public FrameSample this[int index]
		{
			get
			{
				AccessCount++;
				return new FrameSample(index, (index % 120) + 1.0);
			}
		}

		public IEnumerator<FrameSample> GetEnumerator()
		{
			for (int i = 0; i < m_Count; i++)
			{
				yield return this[i];
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
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

	private static void AssertTracyTraceLoad()
	{
		string path = Path.Combine(Path.GetTempPath(), "ProfilerStudy.Avalonia.Tracy." + Guid.NewGuid().ToString("N") + ".tracy");
		try
		{
			WriteTracyMetadataDump(path);
			SessionLoader loader = new SessionLoader();
			SessionDocument document = loader.LoadFileAsync(path, CancellationToken.None).GetAwaiter().GetResult();
			Assert(document.Session == null, "tracy document is not legacy session");
			Assert(document.Summary.SourceName == Path.GetFileName(path), "tracy source name");
			Assert(document.Summary.FrameCount == 2, "tracy frame count");
			Assert(document.FrameSamples.Length == 2, "tracy frame samples");
			Assert(Math.Abs(document.FrameSamples[1].DurationMs - 9.0) < 0.0001, "tracy frame duration");
			Assert(document.Viewport.FrameCount == 2, "tracy viewport frame count");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	private static void WriteTracyMetadataDump(string path)
	{
		using MemoryStream inner = new MemoryStream();
		inner.Write(new byte[] { (byte)'t', (byte)'r', (byte)'a', (byte)'c', (byte)'y', 0, 10, 0 });
		WriteInt64(inner, 0);
		WriteInt64(inner, 1_000_000_000);
		WriteDouble(inner, 1.0);
		WriteInt64(inner, 16_666_667);
		WriteInt64(inner, 0);
		WriteUInt64(inner, 4242);
		WriteInt64(inner, 0);
		inner.WriteByte(2);
		WriteUInt32(inner, 0x12345678);
		WriteFixedAscii(inner, "SelfTestCpu", 12);
		inner.WriteByte(0);
		WriteSizedString(inner, "AvaloniaTrace");
		WriteSizedString(inner, "AvaloniaSmoke");
		WriteInt64(inner, 1_700_000_000);
		WriteInt64(inner, 1_699_999_000);
		WriteSizedString(inner, "AvaloniaHost");
		WriteUInt64(inner, 0);
		WriteUInt64(inner, 0);
		WriteInt64(inner, 0);
		WriteUInt64(inner, 0);
		WriteUInt32(inner, 0);
		WriteUInt64(inner, 1);
		WriteUInt64(inner, 0);
		inner.WriteByte(0);
		WriteUInt64(inner, 2);
		WriteInt64(inner, 0);
		WriteInt64(inner, 8_333_333);
		WriteInt32(inner, -1);
		WriteInt64(inner, 1);
		WriteInt64(inner, 9_000_000);
		WriteInt32(inner, -1);
		WriteUInt64(inner, 0);
		WriteUInt64(inner, 0);
		WriteUInt64(inner, 0);
		WriteUInt64(inner, 0);
		WriteTracyDump(path, inner.ToArray());
	}

	private static void WriteTracyDump(string path, byte[] inner)
	{
		byte[] compressed = EncodeLz4LiteralBlock(inner);
		using FileStream stream = File.Create(path);
		byte[] outerHeader = System.Text.Encoding.ASCII.GetBytes("tlZ4");
		stream.Write(outerHeader, 0, outerHeader.Length);
		byte[] blockSize = BitConverter.GetBytes((uint)compressed.Length);
		stream.Write(blockSize, 0, blockSize.Length);
		stream.Write(compressed, 0, compressed.Length);
	}

	private static byte[] EncodeLz4LiteralBlock(byte[] bytes)
	{
		using MemoryStream stream = new MemoryStream();
		if (bytes.Length < 15)
		{
			stream.WriteByte((byte)(bytes.Length << 4));
		}
		else
		{
			stream.WriteByte(0xF0);
			int remaining = bytes.Length - 15;
			while (remaining >= 255)
			{
				stream.WriteByte(255);
				remaining -= 255;
			}
			stream.WriteByte((byte)remaining);
		}
		stream.Write(bytes, 0, bytes.Length);
		return stream.ToArray();
	}

	private static void WriteSizedString(Stream stream, string value)
	{
		byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
		WriteUInt64(stream, (ulong)bytes.Length);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteFixedAscii(Stream stream, string value, int size)
	{
		byte[] bytes = new byte[size];
		byte[] text = System.Text.Encoding.ASCII.GetBytes(value);
		Buffer.BlockCopy(text, 0, bytes, 0, Math.Min(text.Length, bytes.Length));
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteInt32(Stream stream, int value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteUInt32(Stream stream, uint value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteInt64(Stream stream, long value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteUInt64(Stream stream, ulong value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteDouble(Stream stream, double value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private sealed class SmokeSourceViewerLauncher : ISourceViewerLauncher
	{
		public bool TryLaunch(string sourceFile, int sourceLine, out string error)
		{
			error = string.Empty;
			return false;
		}
	}

	private sealed class SmokeAppSettingsService : IAppSettingsService
	{
		private AppSettings m_Settings = new AppSettings();

		public AppSettings Load()
		{
			return m_Settings;
		}

		public void Save(AppSettings settings)
		{
			m_Settings = settings ?? new AppSettings();
		}
	}

	private sealed class SmokeThemeHost : AppThemeService.IThemeHost
	{
		public IReadOnlyList<AppThemeColorOption> ColorThemes { get; } = new[] { new AppThemeColorOption("Blue") };

		public void ChangeBaseTheme(string baseThemeName)
		{
		}

		public void ChangeColorTheme(string colorThemeName)
		{
		}
	}
}
