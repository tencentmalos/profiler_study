using ProfilerStudy;
using ProfilerStudy.Avalonia.ProfilerStats.Timeline;
using ProfilerStudy.Avalonia.Timeline;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using ProfilerStudy.Tracy;

namespace ProfilerStudy.Avalonia.ProfilerStats;

internal sealed class ProfilerTimelineAdapter
{
	private Session m_Session;
	private TracyTraceQuerySession m_TracyQuerySession;
	private FrameSample[] m_FrameSamples = Array.Empty<FrameSample>();

	public FrameSample[] FrameSamples => m_FrameSamples;

	public ProfilerTimelinePlotModel CreatePlotModel(SessionDocument document)
	{
		m_Session = document?.Session;
		m_TracyQuerySession = document?.TraceDocument?.QuerySession as TracyTraceQuerySession;
		m_FrameSamples = document?.FrameSamples ?? Array.Empty<FrameSample>();

		DiagramMetadata metadata = ProfilerStatisticsProcessor.GetMetadataFromProfilerStatisticsInfo();
		var customStats = new List<ProfilerCustomStatTimelineInfo>();

		if (m_Session != null)
		{
			AppendCustomStatMetadata(m_Session, metadata, customStats);
		}
		else if (m_TracyQuerySession != null)
		{
			AppendTracyPlotMetadata(m_TracyQuerySession, metadata, customStats);
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
		if (customStats.Count == 0 || m_FrameSamples.Length == 0)
		{
			return;
		}

		if (m_Session == null)
		{
			FillTracyPlotSeries(customStats, plotBridges);
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

	public FlameGenerator.FlameGraphConfig CreateScopeFlameGraphConfig(SessionDocument document, int maxFrames = 300, int maxScopes = 5000)
	{
		var config = new FlameGenerator.FlameGraphConfig
		{
			Title = "Profiler Scopes",
			TimelineStart = m_FrameSamples.Length == 0 ? 0.0 : m_FrameSamples[0].Index,
			TimelineEnd = m_FrameSamples.Length == 0 ? 1.0 : m_FrameSamples[^1].Index + 1.0,
			MaxStackDepth = 1,
			FrameHeight = 18.0,
		};

		if (document?.Session == null || m_FrameSamples.Length == 0 || maxFrames <= 0 || maxScopes <= 0)
		{
			return config;
		}

		int stride = Math.Max(1, (int)Math.Ceiling(m_FrameSamples.Length / (double)maxFrames));
		var threadLaneIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
		int maxDepth = 0;

		for (int sampleIndex = 0; sampleIndex < m_FrameSamples.Length && config.StackFrames.Count < maxScopes; sampleIndex += stride)
		{
			FrameSample sample = m_FrameSamples[sampleIndex];
			if (sample.DurationMs <= 0.0)
			{
				continue;
			}

			IReadOnlyList<ScopeFrameDetailRow> rows = ScopeFrameDetailAnalyzer.Build(document, sample.Index, 120);
			foreach (ScopeFrameDetailRow row in rows)
			{
				if (config.StackFrames.Count >= maxScopes)
				{
					break;
				}

				if (row.DurationMs <= 0.0)
				{
					continue;
				}

				if (threadLaneIndexes.TryGetValue(row.ThreadName, out int threadLane) is false)
				{
					threadLane = threadLaneIndexes.Count;
					threadLaneIndexes[row.ThreadName] = threadLane;
				}

				double startRatio = Math.Clamp(row.StartOffsetMs / sample.DurationMs, 0.0, 1.0);
				double endRatio = Math.Clamp((row.StartOffsetMs + row.DurationMs) / sample.DurationMs, startRatio, 1.0);
				if (endRatio <= startRatio)
				{
					endRatio = Math.Min(1.0, startRatio + 0.002);
				}

				int stackLevel = (threadLane * 12) + Math.Min(row.Depth, 11);
				maxDepth = Math.Max(maxDepth, stackLevel + 1);
				Color color = ScottPlotColorUtil.GetMutedColor(row.Depth + (threadLane * 3));
				config.StackFrames.Add(new FlameGenerator.StackFrame
				{
					FunctionName = row.Name,
					StartTime = sample.Index + startRatio,
					EndTime = sample.Index + endRatio,
					StackLevel = stackLevel,
					Color = new Color(color.R, color.G, color.B, 210),
					Module = row.ThreadName,
					FrameIndex = sample.Index,
					SourceText = row.SourceText,
					SourceFile = row.SourceFile,
					SourceLine = row.SourceLine,
				});
			}
		}

		config.MaxStackDepth = Math.Max(1, maxDepth);
		return config;
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

	private static void AppendTracyPlotMetadata(TracyTraceQuerySession querySession, DiagramMetadata metadata, List<ProfilerCustomStatTimelineInfo> customStats)
	{
		if (querySession.EventStream.Plots == null || querySession.EventStream.Plots.Count == 0)
		{
			return;
		}

		string plotProperty = $"TracyPlot_{metadata.PlotList.Count}";
		var plotMeta = new CurvePlotMetadata
		{
			PropertyName = plotProperty,
			PlotTitle = "Tracy Plots",
			IsMainPlot = false
		};

		int curveIndex = 0;
		foreach (TracyPlotSummary plot in querySession.EventStream.Plots)
		{
			string curveProperty = $"Value_{curveIndex}";
			var curveMeta = new CurveFieldMetadata
			{
				PropertyName = curveProperty,
				CurveLabel = plot.Name,
				CurveUnit = string.Empty,
				LineColor = ScottPlotColorUtil.GetMutedColor(curveIndex + 1),
			};
			plotMeta.SubFields.Add(curveMeta);
			plotMeta.FieldDictionary.Add(curveMeta.PropertyName, curveMeta);
			customStats.Add(new ProfilerCustomStatTimelineInfo(plotMeta.PropertyName, curveMeta.PropertyName, plot.Name));
			curveIndex++;
		}

		metadata.PlotList.Add(plotMeta);
		metadata.PlotDictionary.Add(plotMeta.PropertyName, plotMeta);
	}

	private void FillTracyPlotSeries(IReadOnlyList<ProfilerCustomStatTimelineInfo> customStats, IDictionary<string, CurveUiPlotBridgeItem> plotBridges)
	{
		if (m_TracyQuerySession?.EventStream.Plots == null ||
			m_TracyQuerySession.EventStream.Metadata == null)
		{
			return;
		}

		foreach (ProfilerCustomStatTimelineInfo statInfo in customStats.Where(item => item.IsTracePlot))
		{
			TracyPlotSummary plot = m_TracyQuerySession.EventStream.Plots.FirstOrDefault(item => string.Equals(item.Name, statInfo.TracePlotName, StringComparison.Ordinal));
			if (plot == null ||
				plotBridges.TryGetValue(statInfo.PlotKey, out CurveUiPlotBridgeItem bridge) is false ||
				bridge.CurveDictionary.TryGetValue(statInfo.CurveKey, out Curve2DGenerator.OneCurve curve) is false)
			{
				continue;
			}

			foreach (TracyPlotSample sample in plot.Samples)
			{
				if (!TryGetTracyFrameIndex(sample.Time, out int frameIndex))
				{
					continue;
				}
				int displayIndex = GetDisplaySampleIndex(frameIndex);
				if (displayIndex < 0)
				{
					continue;
				}
				curve.AppendData(m_FrameSamples[displayIndex].Index, sample.Value);
			}
		}
	}

	private bool TryGetTracyFrameIndex(long time, out int frameIndex)
	{
		frameIndex = 0;
		foreach (TracyFrameSetSummary frameSet in m_TracyQuerySession.EventStream.Metadata.FrameSets)
		{
			foreach (TracyFrameSummary frame in frameSet.Frames)
			{
				if (time >= frame.Start && time <= frame.End)
				{
					return true;
				}
				frameIndex++;
			}
		}
		frameIndex = -1;
		return false;
	}

	private static Color ConvertDrawingColor(System.Drawing.Color color)
	{
		return new Color(color.R, color.G, color.B, color.A);
	}
}
