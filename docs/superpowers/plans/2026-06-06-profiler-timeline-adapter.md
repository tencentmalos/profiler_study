# Profiler Timeline Adapter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move ProfilerStudy.Avalonia profiler data mapping onto the DeviceInfo-derived ScottPlot `TimelineScope` control model through a focused adapter layer.

**Architecture:** Keep `ProfilerStatsController` responsible for UI state and event wiring only. Add profiler-specific adapter/model files that build `CurvePlotMetadata`, `DetailPlotConfig`, custom-stat descriptors, and frame/custom-stat series for the existing `TimelineScope`/`CurveUiPlotBridgeItem` structure. Do not modify DeviceInfo business behavior or introduce a second timeline control.

**Tech Stack:** C#/.NET 8, Avalonia, ScottPlot.Avalonia 5.0.55, existing `ProfilerStudy.Avalonia.Timeline` controls migrated from DeviceInfo.

---

## File structure

- Create: `ProfilerStudy.Avalonia/ProfilerStats/ProfilerTimelinePlotModel.cs`
  - Owns the immutable plot metadata and custom-stat mapping produced from a `Session`.
- Create: `ProfilerStudy.Avalonia/ProfilerStats/ProfilerTimelineAdapter.cs`
  - Converts `SessionDocument` and `FrameSample[]` into `ProfilerTimelinePlotModel`, fills `CurveUiPlotBridgeItem` curves, and maps frame indices to display samples.
- Modify: `ProfilerStudy.Avalonia/ProfilerStats/ProfilerStatsController.cs`
  - Removes profiler data-conversion details from the controller and delegates to `ProfilerTimelineAdapter`.
- Modify: `ProfilerStudy.Avalonia/AvaloniaSmokeTest.cs`
  - Adds a smoke assertion that the adapter can analyze the loaded sample document and produce a main frame-duration plot.

---

### Task 1: Add profiler timeline plot model

**Files:**
- Create: `ProfilerStudy.Avalonia/ProfilerStats/ProfilerTimelinePlotModel.cs`

- [ ] **Step 1: Create model file**

Create `ProfilerStudy.Avalonia/ProfilerStats/ProfilerTimelinePlotModel.cs` with this content:

```csharp
using ProfilerStudy.Avalonia.ProfilerStats.Timeline;
using System.Collections.Generic;

namespace ProfilerStudy.Avalonia.ProfilerStats;

internal sealed class ProfilerTimelinePlotModel
{
	public ProfilerTimelinePlotModel(DiagramMetadata metadata, IReadOnlyList<ProfilerCustomStatTimelineInfo> customStats)
	{
		Metadata = metadata;
		CustomStats = customStats;
	}

	public DiagramMetadata Metadata { get; }

	public IReadOnlyList<ProfilerCustomStatTimelineInfo> CustomStats { get; }
}

internal sealed class ProfilerCustomStatTimelineInfo
{
	public ProfilerCustomStatTimelineInfo(string plotKey, string curveKey, long statId, bool convertCyclesToMilliseconds)
	{
		PlotKey = plotKey;
		CurveKey = curveKey;
		StatId = statId;
		ConvertCyclesToMilliseconds = convertCyclesToMilliseconds;
	}

	public string PlotKey { get; }

	public string CurveKey { get; }

	public long StatId { get; }

	public bool ConvertCyclesToMilliseconds { get; }
}
```

- [ ] **Step 2: Build**

Run:

```bash
/Users/bytedance/.dotnet/dotnet build ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj -c Debug
```

Expected: build exits `0` with no new errors.

- [ ] **Step 3: Commit**

Run:

```bash
git add ProfilerStudy.Avalonia/ProfilerStats/ProfilerTimelinePlotModel.cs
git commit -m "refactor: add profiler timeline plot model"
```

---

### Task 2: Add adapter for metadata and series mapping

**Files:**
- Create: `ProfilerStudy.Avalonia/ProfilerStats/ProfilerTimelineAdapter.cs`

- [ ] **Step 1: Create adapter file**

Create `ProfilerStudy.Avalonia/ProfilerStats/ProfilerTimelineAdapter.cs` with this content:

```csharp
using FramePro;
using ProfilerStudy.Avalonia.ProfilerStats.Timeline;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProfilerStudy.Avalonia.ProfilerStats;

internal sealed class ProfilerTimelineAdapter
{
	private Session m_Session;
	private FrameSample[] m_FrameSamples = Array.Empty<FrameSample>();

	public FrameSample[] FrameSamples => m_FrameSamples;

	public ProfilerTimelinePlotModel CreatePlotModel(SessionDocument document)
	{
		m_Session = document?.Session;
		m_FrameSamples = document?.FrameSamples ?? Array.Empty<FrameSample>();

		DiagramMetadata metadata = ProfilerStatisticsProcessor.GetMetadataFromProfilerStatisticsInfo();
		var customStats = new List<ProfilerCustomStatTimelineInfo>();

		if (m_Session != null)
		{
			AppendCustomStatMetadata(m_Session, metadata, customStats);
		}

		return new ProfilerTimelinePlotModel(metadata, customStats);
	}

	public void FillFrameSeries(CurveUiPlotBridgeItem mainStatsBridge)
	{
		if (m_FrameSamples.Length == 0 || mainStatsBridge == null)
		{
			return;
		}

		var mainCurve = mainStatsBridge.CurveList.FirstOrDefault();
		if (mainCurve == null)
		{
			return;
		}

		foreach (FrameSample frame in m_FrameSamples)
		{
			mainCurve.AppendData(frame.Index, frame.DurationMs);
		}
	}

	public void FillCustomStatSeries(IReadOnlyList<ProfilerCustomStatTimelineInfo> customStats, IDictionary<string, CurveUiPlotBridgeItem> plotBridges)
	{
		if (m_Session == null || customStats.Count == 0 || m_FrameSamples.Length == 0)
		{
			return;
		}

		int startFrameIndex = 0;
		int endFrameIndex = m_Session.FrameCount > 0 ? m_Session.FrameCount - 1 : 0;
		foreach (ProfilerCustomStatTimelineInfo statInfo in customStats)
		{
			var points = new List<FrameValue>();
			m_Session.GetCustomStats(startFrameIndex, endFrameIndex, statInfo.StatId, false, points);
			if (points.Count == 0 ||
				plotBridges.TryGetValue(statInfo.PlotKey, out CurveUiPlotBridgeItem bridge) is false ||
				bridge.CurveDictionary.TryGetValue(statInfo.CurveKey, out Curve2DGenerator.OneCurve curve) is false)
			{
				continue;
			}

			foreach (FrameValue point in points)
			{
				double value = point.m_Value;
				if (statInfo.ConvertCyclesToMilliseconds && m_Session.TimerFrequency > 0)
				{
					value = value * 1000.0 / m_Session.TimerFrequency;
				}

				int sourceFrameIndex = m_Session.GetFrameIndex(point.m_FrameEndTime);
				int displayIndex = GetDisplaySampleIndex(sourceFrameIndex);
				if (displayIndex < 0)
				{
					continue;
				}

				curve.AppendData(m_FrameSamples[displayIndex].Index, value);
			}
		}
	}

	public int GetNearestFrameIndex(double targetX)
	{
		if (m_FrameSamples.Length == 0)
		{
			return -1;
		}

		int left = 0;
		int right = m_FrameSamples.Length - 1;
		while (left <= right)
		{
			int mid = left + ((right - left) / 2);
			double sampleFrameIndex = m_FrameSamples[mid].Index;
			if (sampleFrameIndex == targetX)
			{
				return m_FrameSamples[mid].Index;
			}

			if (sampleFrameIndex < targetX)
			{
				left = mid + 1;
			}
			else
			{
				right = mid - 1;
			}
		}

		if (left >= m_FrameSamples.Length)
		{
			return m_FrameSamples[^1].Index;
		}

		if (right < 0)
		{
			return m_FrameSamples[0].Index;
		}

		double leftDistance = Math.Abs(m_FrameSamples[left].Index - targetX);
		double rightDistance = Math.Abs(m_FrameSamples[right].Index - targetX);
		return leftDistance < rightDistance ? m_FrameSamples[left].Index : m_FrameSamples[right].Index;
	}

	public int GetDisplaySampleIndex(int sourceFrameIndex)
	{
		if (m_FrameSamples.Length == 0)
		{
			return -1;
		}

		int firstSampleFrameIndex = m_FrameSamples[0].Index;
		int lastSampleFrameIndex = m_FrameSamples[^1].Index;
		if (sourceFrameIndex <= firstSampleFrameIndex)
		{
			return 0;
		}

		if (sourceFrameIndex >= lastSampleFrameIndex)
		{
			return m_FrameSamples.Length - 1;
		}

		int left = 0;
		int right = m_FrameSamples.Length - 1;
		while (left <= right)
		{
			int mid = left + ((right - left) / 2);
			int sampleFrameIndex = m_FrameSamples[mid].Index;
			if (sampleFrameIndex == sourceFrameIndex)
			{
				return mid;
			}

			if (sampleFrameIndex < sourceFrameIndex)
			{
				left = mid + 1;
			}
			else
			{
				right = mid - 1;
			}
		}

		int previousIndex = Math.Max(0, right);
		int nextIndex = Math.Min(m_FrameSamples.Length - 1, left);
		int previousDistance = sourceFrameIndex - m_FrameSamples[previousIndex].Index;
		int nextDistance = m_FrameSamples[nextIndex].Index - sourceFrameIndex;
		return previousDistance <= nextDistance ? previousIndex : nextIndex;
	}

	private static void AppendCustomStatMetadata(Session session, DiagramMetadata metadata, List<ProfilerCustomStatTimelineInfo> customStats)
	{
		var orderedStats = ProfilerStatsDocumentAnalyzer.GetCustomStatDescriptors(session);
		foreach (var graphGroup in orderedStats.GroupBy(item => item.GraphName))
		{
			string plotProperty = $"CustomStatGraph_{metadata.PlotList.Count}";
			var plotMeta = new CurvePlotMetadata
			{
				PropertyName = plotProperty,
				PlotTitle = string.Equals(graphGroup.Key, "default", StringComparison.OrdinalIgnoreCase) ? "Custom Stats" : graphGroup.Key,
				IsMainPlot = false
			};

			int curveIndex = 0;
			foreach (var item in graphGroup)
			{
				string curveProperty = $"Value_{curveIndex}";
				var curveMeta = new CurveFieldMetadata
				{
					PropertyName = curveProperty,
					CurveLabel = item.Name,
					CurveUnit = item.DisplayUnit,
					LineColor = ConvertDrawingColor(item.Color),
				};
				plotMeta.SubFields.Add(curveMeta);
				plotMeta.FieldDictionary.Add(curveMeta.PropertyName, curveMeta);
				customStats.Add(new ProfilerCustomStatTimelineInfo(plotMeta.PropertyName, curveMeta.PropertyName, item.StatId, item.ConvertCyclesToMilliseconds));
				curveIndex++;
			}

			metadata.PlotList.Add(plotMeta);
			metadata.PlotDictionary.Add(plotMeta.PropertyName, plotMeta);
		}
	}

	private static Color ConvertDrawingColor(System.Drawing.Color color)
	{
		return new Color(color.R, color.G, color.B, color.A);
	}
}
```

- [ ] **Step 2: Build**

Run:

```bash
/Users/bytedance/.dotnet/dotnet build ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj -c Debug
```

Expected: build exits `0`. If `Curve2DGenerator.OneCurve` cannot be resolved, add `using ProfilerStudy.Avalonia.Timeline;` to the adapter file.

- [ ] **Step 3: Commit**

Run:

```bash
git add ProfilerStudy.Avalonia/ProfilerStats/ProfilerTimelineAdapter.cs
git commit -m "refactor: add profiler timeline adapter"
```

---

### Task 3: Refactor controller to use adapter

**Files:**
- Modify: `ProfilerStudy.Avalonia/ProfilerStats/ProfilerStatsController.cs`

- [ ] **Step 1: Replace controller data fields**

In `ProfilerStatsController`, remove this record and fields:

```csharp
private sealed record CustomStatPlotInfo(string PlotKey, string CurveKey, long StatId, string Name, string Unit, bool ConvertCyclesToMilliseconds);
private readonly List<CustomStatPlotInfo> m_CustomStatInfos = new();
private DiagramMetadata m_StatisticsDiagramMetadata = ProfilerStatisticsProcessor.GetMetadataFromProfilerStatisticsInfo();
private Session m_CurrentSession;
private FrameSample[] m_CurrentFrameSamples = Array.Empty<FrameSample>();
```

Add these fields:

```csharp
private readonly ProfilerTimelineAdapter m_TimelineAdapter = new();
private ProfilerTimelinePlotModel m_PlotModel;
```

- [ ] **Step 2: Update document application**

Replace the top of `ApplyDocument(SessionDocument document)` with:

```csharp
m_PlotModel = m_TimelineAdapter.CreatePlotModel(document);

BuildPlots();

if (m_TimelineAdapter.FrameSamples.Length == 0)
{
	ApplyEmptyState();
	return;
}

m_TimelineAdapter.FillFrameSeries(m_MainStatsBridge);
m_TimelineAdapter.FillCustomStatSeries(m_PlotModel.CustomStats, m_PlotBridges);
```

Remove the old assignments to `m_CurrentSession` and `m_CurrentFrameSamples`, and remove calls to `FillFrameSeries()` and `FillCustomStatsSeries()`.

- [ ] **Step 3: Update plot building**

In `BuildPlots()`, replace metadata setup and iteration with:

```csharp
if (m_PlotModel == null)
{
	m_PlotModel = m_TimelineAdapter.CreatePlotModel(null);
}

foreach (var plotMeta in m_PlotModel.Metadata.PlotList)
{
	var bridge = CreateBridgeFromPlotMetadata(plotMeta);
	m_AllBridges.Add(bridge);
	m_PlotBridges[plotMeta.PropertyName] = bridge;
	if (plotMeta.IsMainPlot)
	{
		m_MainStatsBridge = bridge;
	}
}
```

Remove `m_StatisticsDiagramMetadata = ...` and `AppendCustomStatMetadata(m_CurrentSession)` from `BuildPlots()`.

- [ ] **Step 4: Update empty-state cleanup**

In `ApplyEmptyState()`, remove:

```csharp
m_CustomStatInfos.Clear();
```

Add:

```csharp
m_PlotModel = m_TimelineAdapter.CreatePlotModel(null);
```

before `Timeline.ChangeTimelineConfig(CreateDefaultMainPlotConfig());`.

- [ ] **Step 5: Replace frame-sample references**

Replace every `m_CurrentFrameSamples` reference with `m_TimelineAdapter.FrameSamples` in range and viewport methods.

Replace:

```csharp
int startSampleIndex = GetDisplaySampleIndex(viewport.StartFrame);
int endSampleIndex = GetDisplaySampleIndex(viewport.EndFrame);
```

with:

```csharp
int startSampleIndex = m_TimelineAdapter.GetDisplaySampleIndex(viewport.StartFrame);
int endSampleIndex = m_TimelineAdapter.GetDisplaySampleIndex(viewport.EndFrame);
```

Replace:

```csharp
int startFrame = GetNearestFrameIndex(Timeline.ChildSelectScope.ScopeStart);
int endFrame = GetNearestFrameIndex(Timeline.ChildSelectScope.ScopeEnd);
```

with:

```csharp
int startFrame = m_TimelineAdapter.GetNearestFrameIndex(Timeline.ChildSelectScope.ScopeStart);
int endFrame = m_TimelineAdapter.GetNearestFrameIndex(Timeline.ChildSelectScope.ScopeEnd);
```

- [ ] **Step 6: Delete moved methods**

Delete these methods from `ProfilerStatsController`:

```csharp
private void FillFrameSeries()
private void FillCustomStatsSeries()
private void AppendCustomStatMetadata(Session session)
private static Color ConvertDrawingColor(System.Drawing.Color color)
private int GetNearestFrameIndex(double targetX)
private int GetDisplaySampleIndex(int sourceFrameIndex)
```

Also remove unused `using FramePro;`, `using ScottPlot;`, and `using System.Collections.Generic;` if the build reports them as unnecessary.

- [ ] **Step 7: Build**

Run:

```bash
/Users/bytedance/.dotnet/dotnet build ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj -c Debug
```

Expected: build exits `0` with no behavior changes to viewport sync, auto-scale, detail plot visibility, or user scope selection.

- [ ] **Step 8: Smoke test**

Run:

```bash
ProfilerStudy.Avalonia/bin/Debug/net8.0/ProfilerStudy.Avalonia --smoke-test
```

Expected: smoke test exits `0`.

- [ ] **Step 9: Commit**

Run:

```bash
git add ProfilerStudy.Avalonia/ProfilerStats/ProfilerStatsController.cs
git commit -m "refactor: adapt profiler stats through timeline adapter"
```

---

### Task 4: Add smoke coverage for adapter output

**Files:**
- Modify: `ProfilerStudy.Avalonia/AvaloniaSmokeTest.cs`

- [ ] **Step 1: Add adapter assertion**

After the existing profiler stats summary analysis in `AvaloniaSmokeTest`, add:

```csharp
var adapter = new ProfilerTimelineAdapter();
ProfilerTimelinePlotModel plotModel = adapter.CreatePlotModel(document);
if (plotModel.Metadata.PlotList.Count == 0 || plotModel.Metadata.PlotList.Any(item => item.IsMainPlot) is false)
{
	throw new InvalidOperationException("Profiler timeline adapter did not produce a main timeline plot.");
}
```

If `System.Linq` is not already imported, add:

```csharp
using System.Linq;
```

- [ ] **Step 2: Build**

Run:

```bash
/Users/bytedance/.dotnet/dotnet build ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj -c Debug
```

Expected: build exits `0`.

- [ ] **Step 3: Smoke test**

Run:

```bash
ProfilerStudy.Avalonia/bin/Debug/net8.0/ProfilerStudy.Avalonia --smoke-test
```

Expected: smoke test exits `0`.

- [ ] **Step 4: Commit**

Run:

```bash
git add ProfilerStudy.Avalonia/AvaloniaSmokeTest.cs
git commit -m "test: cover profiler timeline adapter smoke output"
```

---

## Self-review

- Spec coverage: The plan keeps `TimelineScope` as the single ScottPlot timeline/stats control, moves profiler data mapping into an adapter, and avoids DeviceInfo business implementation.
- Placeholder scan: No `TBD`, `TODO`, or unspecified implementation steps remain.
- Type consistency: `ProfilerTimelinePlotModel`, `ProfilerCustomStatTimelineInfo`, and `ProfilerTimelineAdapter` signatures match all controller and smoke-test usage in this plan.
