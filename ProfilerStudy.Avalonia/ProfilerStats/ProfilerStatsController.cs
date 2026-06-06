using FramePro;
using ProfilerStudy;
using ProfilerStudy.Avalonia.ProfilerStats.Timeline;
using ProfilerStudy.Avalonia.Timeline;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace ProfilerStudy.Avalonia.ProfilerStats;

internal sealed class ProfilerStatsController : ObservableObject
{
	private sealed record CustomStatPlotInfo(string PlotKey, string CurveKey, long StatId, string Name, string Unit, bool ConvertCyclesToMilliseconds);

	private const double kDefaultSelectWindow = 30.0;

	private readonly ucProfilerStats m_View;
	private readonly List<DetailPlotControlItem> m_DetailPlotControls = new();
	private readonly List<CustomStatPlotInfo> m_CustomStatInfos = new();
	private readonly Dictionary<string, CurveUiPlotBridgeItem> m_PlotBridges = new();
	private readonly List<CurveUiPlotBridgeItem> m_AllBridges = new();

	private DiagramMetadata m_StatisticsDiagramMetadata = ProfilerStatisticsProcessor.GetMetadataFromProfilerStatisticsInfo();
	private CurveUiPlotBridgeItem m_MainStatsBridge = null!;
	private Session m_CurrentSession;
	private FrameSample[] m_CurrentFrameSamples = Array.Empty<FrameSample>();
	private bool m_IsAutoFollow = true;
	private bool m_IsAutoScaleY = true;
	private bool m_ShowDetailPlots = true;
	private PlotHeight m_SelectedPlotHeight = PlotHeight.Medium;
	private bool m_IsInitialized;
	private bool m_IsAutoFollowByUi;

	internal ProfilerStatsController(ucProfilerStats view)
	{
		m_View = view;
	}

	public ucTimelineScope Timeline => m_View.TimelineScope;

	public bool IsAutoFollow
	{
		get => m_IsAutoFollow;
	}

	public bool IsAutoScaleY
	{
		get => m_IsAutoScaleY;
	}

	public bool ShowDetailPlots
	{
		get => m_ShowDetailPlots;
	}

	public PlotHeight SelectedPlotHeight
	{
		get => m_SelectedPlotHeight;
	}

	public void Initialize()
	{
		if (m_IsInitialized)
		{
			return;
		}

		Timeline.OnScopeRangeChangedByUi += Timeline_OnScopeRangeChangedByUi;
		m_IsInitialized = true;

		ApplyEmptyState();
		ApplyPlotHeight(m_SelectedPlotHeight);
		Timeline.ApplyTheme();
	}

	public void ApplyDocument(SessionDocument document)
	{
		if (Dispatcher.UIThread.CheckAccess() is false)
		{
			Dispatcher.UIThread.Post(() => ApplyDocument(document));
			return;
		}

		m_CurrentSession = document?.Session;
		m_CurrentFrameSamples = document?.FrameSamples ?? Array.Empty<FrameSample>();

		BuildPlots();

		if (m_CurrentFrameSamples.Length == 0)
		{
			ApplyEmptyState();
			return;
		}

		FillFrameSeries();
		FillCustomStatsSeries();

		Timeline.UpdateMainStatsAndAllDetailPlots();
		ApplyRangeAndViewport();
		ApplyDetailPlotVisibility();
		RefreshDetailPlotControlPanel();
		ApplyPlotHeight(m_SelectedPlotHeight);
		Timeline.ApplyTheme();
	}

	public void SetAutoFollow(bool isChecked)
	{
		if (m_IsAutoFollow == isChecked)
		{
			return;
		}

		m_IsAutoFollow = isChecked;
		if (m_IsAutoFollow)
		{
			m_IsAutoFollowByUi = false;
			ApplyRangeAndViewport();
		}
	}

	public void SetAutoScaleY(bool isChecked)
	{
		if (m_IsAutoScaleY == isChecked)
		{
			return;
		}

		m_IsAutoScaleY = isChecked;
		Timeline.EnableAutoScaleY = m_IsAutoScaleY;
		ApplyRangeAndViewport();
	}

	public void SetShowDetails(bool isChecked)
	{
		if (m_ShowDetailPlots == isChecked)
		{
			return;
		}

		m_ShowDetailPlots = isChecked;
		ApplyDetailPlotVisibility();
	}

	public void ShowAllDetailPlots()
	{
		SetAllDetailPlotsVisible(true);
	}

	public void HideAllDetailPlots()
	{
		SetAllDetailPlotsVisible(false);
	}

	public void SetPlotHeight(PlotHeight plotHeight)
	{
		if (m_SelectedPlotHeight == plotHeight)
		{
			return;
		}

		m_SelectedPlotHeight = plotHeight;
		ApplyPlotHeight(plotHeight);
	}

	private void BuildPlots()
	{
		Timeline.ChildDetails.ClearAll();
		UnsubscribePlotControls();
		m_DetailPlotControls.Clear();
		m_CustomStatInfos.Clear();
		m_PlotBridges.Clear();
		m_AllBridges.Clear();

		m_StatisticsDiagramMetadata = ProfilerStatisticsProcessor.GetMetadataFromProfilerStatisticsInfo();

		if (m_CurrentSession != null)
		{
			AppendCustomStatMetadata(m_CurrentSession);
		}

		foreach (var plotMeta in m_StatisticsDiagramMetadata.PlotList)
		{
			var bridge = CreateBridgeFromPlotMetadata(plotMeta);
			m_AllBridges.Add(bridge);
			m_PlotBridges[plotMeta.PropertyName] = bridge;
			if (plotMeta.IsMainPlot)
			{
				m_MainStatsBridge = bridge;
			}
		}

		Timeline.ResetAllPlots();
		m_MainStatsBridge = m_AllBridges.FirstOrDefault(item => item.Metadata.IsMainPlot) ?? m_MainStatsBridge;

		if (m_MainStatsBridge?.UiComponent == null)
		{
			ApplyEmptyState();
			return;
		}

		Timeline.ChildSelectScope.UpdateXRange(0, Math.Max(kDefaultSelectWindow, 1.0));
		Timeline.ChildMainStats.UpdateXRange(0, Math.Max(kDefaultSelectWindow, 1.0), m_IsAutoScaleY);
	}

	private void ApplyEmptyState()
	{
		Timeline.ChildDetails.ClearAll();
		m_PlotBridges.Clear();
		m_AllBridges.Clear();
		m_CustomStatInfos.Clear();

		// Keep a clean main plot even in empty states
		Timeline.ChangeTimelineConfig(CreateDefaultMainPlotConfig());
		Timeline.ResetAllPlots();

		m_MainStatsBridge = new CurveUiPlotBridgeItem
		{
			Metadata = new CurvePlotMetadata
			{
				PropertyName = nameof(ProfilerStatisticsInfo.MainStats),
				PlotTitle = "Frame Duration",
				IsMainPlot = true,
			},
			UiComponent = Timeline.ChildMainStats.LogicPlot
		};

		InitializeBridgeCurves(m_MainStatsBridge);
		ApplyDetailPlotVisibility();
		RefreshDetailPlotControlPanel();
		ApplyPlotHeight(m_SelectedPlotHeight);
	}

	private void ApplyRangeAndViewport()
	{
		if (Timeline == null || m_CurrentFrameSamples == null || m_CurrentFrameSamples.Length == 0)
		{
			Timeline.ChildSelectScope.UpdateXRange(0, Math.Max(kDefaultSelectWindow, 1.0), false);
			Timeline.ChildMainStats.UpdateXRange(0, Math.Max(kDefaultSelectWindow, 1.0), m_IsAutoScaleY);
			Timeline.ChildDetails.UpdateXRange(0, Math.Max(kDefaultSelectWindow, 1.0), m_IsAutoScaleY);
			return;
		}

		double minX = m_CurrentFrameSamples[0].Index;
		double maxX = m_CurrentFrameSamples[m_CurrentFrameSamples.Length - 1].Index;
		if (maxX <= minX)
		{
			maxX = minX + 1.0;
		}

		double xStart;
		double xEnd;

		if (m_IsAutoFollow && !m_IsAutoFollowByUi)
		{
			xEnd = maxX;
			xStart = Math.Max(minX, xEnd - kDefaultSelectWindow);
		}
		else
		{
			xStart = minX;
			xEnd = maxX;
		}

		Timeline.ChildSelectScope.UpdateXRange(xStart, xEnd, false);
		Timeline.ChildMainStats.UpdateXRange(xStart, xEnd, m_IsAutoScaleY);
		Timeline.ChildDetails.UpdateXRange(xStart, xEnd, m_IsAutoScaleY);
	}

	private void ApplyDetailPlotVisibility()
	{
		foreach (var bridge in m_AllBridges)
		{
			if (bridge.Metadata.IsMainPlot)
			{
				continue;
			}

			if (bridge.UiComponent?.Border != null)
			{
				bridge.UiComponent.Border.IsVisible = m_ShowDetailPlots;
			}
		}
	}

	private void ApplyPlotHeight(PlotHeight height)
	{
		double heightPx = height switch
		{
			PlotHeight.Small => 120.0,
			PlotHeight.Medium => 180.0,
			PlotHeight.Large => 240.0,
			PlotHeight.ExtraLarge => 300.0,
			_ => 180.0
		};

		foreach (var bridge in m_AllBridges)
		{
			if (bridge.Metadata.IsMainPlot)
			{
				continue;
			}

			if (bridge.UiComponent?.Border != null)
			{
				bridge.UiComponent.Border.Height = heightPx;
			}
		}
	}

	private void FillFrameSeries()
	{
		if (m_CurrentFrameSamples == null || m_CurrentFrameSamples.Length == 0 || m_MainStatsBridge == null)
		{
			return;
		}

		var mainCurve = m_MainStatsBridge.CurveList.FirstOrDefault();
		if (mainCurve == null)
		{
			return;
		}

		foreach (FrameSample frame in m_CurrentFrameSamples)
		{
			mainCurve.AppendData(frame.Index, frame.DurationMs);
		}
	}

	private void FillCustomStatsSeries()
	{
		if (m_CurrentSession == null || m_CustomStatInfos.Count == 0)
		{
			return;
		}

		for (int customIndex = 0; customIndex < m_CustomStatInfos.Count; customIndex++)
		{
			CustomStatPlotInfo statInfo = m_CustomStatInfos[customIndex];
			var points = new List<FrameValue>();
			int startFrameIndex = 0;
			int endFrameIndex = m_CurrentSession.FrameCount > 0 ? m_CurrentSession.FrameCount - 1 : 0;
			m_CurrentSession.GetCustomStats(startFrameIndex, endFrameIndex, statInfo.StatId, false, points);
			if (points.Count == 0 ||
				!m_PlotBridges.TryGetValue(statInfo.PlotKey, out var bridge) ||
				!bridge.CurveDictionary.TryGetValue(statInfo.CurveKey, out var curve))
			{
				continue;
			}

			if (m_CurrentFrameSamples.Length == 0)
			{
				continue;
			}

			foreach (FrameValue point in points)
			{
				double value = point.m_Value;
				if (statInfo.ConvertCyclesToMilliseconds && m_CurrentSession.TimerFrequency > 0)
				{
					value = value * 1000.0 / m_CurrentSession.TimerFrequency;
				}

				int sourceFrameIndex = m_CurrentSession.GetFrameIndex(point.m_FrameEndTime);
				if (sourceFrameIndex < 0)
				{
					continue;
				}

				int displayIndex = GetDisplaySampleIndex(sourceFrameIndex);
				if (displayIndex < 0)
				{
					continue;
				}

				curve.AppendData(m_CurrentFrameSamples[displayIndex].Index, value);
			}
		}
	}

	private void RefreshDetailPlotControlPanel()
	{
		StackPanel controlsPanel = m_View.DetailPlotControlsHost;
		controlsPanel.Children.Clear();
		m_DetailPlotControls.Clear();

		foreach (CurveUiPlotBridgeItem bridge in m_AllBridges)
		{
			if (bridge.Metadata.IsMainPlot || bridge.UiComponent?.Border == null)
			{
				continue;
			}

			var control = new DetailPlotControlItem(bridge.Metadata.PlotTitle, bridge.UiComponent);
			var toggle = new ToggleButton
			{
				Content = control.Title,
				IsChecked = m_ShowDetailPlots,
				Margin = new global::Avalonia.Thickness(2, 0),
				FontSize = 11,
				Height = 28,
			};

			var localControl = control;
			toggle.IsCheckedChanged += (_, _) => localControl.ChangePlotVisible(toggle.IsChecked ?? false);
			control.UiItem = toggle;
			control.ChangePlotVisible(m_ShowDetailPlots);
			m_DetailPlotControls.Add(control);
			controlsPanel.Children.Add(toggle);
		}
	}

	private void UnsubscribePlotControls()
	{
		m_View.DetailPlotControlsHost.Children.Clear();
	}

	private void AppendCustomStatMetadata(Session session)
	{
		var customStats = session.GetCustomStats();
		if (customStats == null || customStats.Count == 0)
		{
			return;
		}

		var orderedStats = ProfilerStatsDocumentAnalyzer.GetCustomStatDescriptors(session);

		foreach (var graphGroup in orderedStats.GroupBy(item => item.GraphName))
		{
			string plotProperty = $"CustomStatGraph_{m_StatisticsDiagramMetadata.PlotList.Count}";
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
				m_CustomStatInfos.Add(new CustomStatPlotInfo(plotMeta.PropertyName, curveMeta.PropertyName, item.StatId, item.Name, item.DisplayUnit, item.ConvertCyclesToMilliseconds));
				curveIndex++;
			}

			m_StatisticsDiagramMetadata.PlotList.Add(plotMeta);
			m_StatisticsDiagramMetadata.PlotDictionary.Add(plotMeta.PropertyName, plotMeta);
		}
	}

	private static Color ConvertDrawingColor(System.Drawing.Color color)
	{
		return new Color(color.R, color.G, color.B, color.A);
	}

	private CurveUiPlotBridgeItem CreateBridgeFromPlotMetadata(CurvePlotMetadata plotMeta)
	{
		var plotConfig = new DetailPlotConfig
		{
			Title = plotMeta.PlotTitle
		};
		foreach (CurveFieldMetadata curveMeta in plotMeta.SubFields)
		{
			plotConfig.AllCurves.Add(new CurveConfig
			{
				Label = curveMeta.CurveLabel,
				LineColor = curveMeta.LineColor,
				UnitName = curveMeta.CurveUnit,
			});
		}

		ucDetailPlotComponent plot;
		if (plotMeta.IsMainPlot)
		{
			Timeline.ChangeTimelineConfig(plotConfig);
			plot = Timeline.ChildMainStats.LogicPlot;
		}
		else
		{
			plot = Timeline.AddDetailPlot(plotConfig);
		}

		var bridge = new CurveUiPlotBridgeItem
		{
			Metadata = plotMeta,
			UiComponent = plot,
		};
		InitializeBridgeCurves(bridge);

		return bridge;
	}

	private void InitializeBridgeCurves(CurveUiPlotBridgeItem bridge)
	{
		bridge.CurveList.Clear();
		bridge.CurveDictionary.Clear();
		if (bridge.UiComponent?.Curve2DGenerator == null)
		{
			return;
		}

		int fieldIndex = 0;
		foreach (CurveFieldMetadata field in bridge.Metadata.SubFields)
		{
			if (fieldIndex >= bridge.UiComponent.Curve2DGenerator!.CurveCount)
			{
				break;
			}

			Curve2DGenerator.OneCurve curve = bridge.UiComponent.Curve2DGenerator[fieldIndex];
			bridge.CurveList.Add(curve);
			bridge.CurveDictionary[field.PropertyName] = curve;
			fieldIndex++;
		}
	}

	private void SetAllDetailPlotsVisible(bool isVisible)
	{
		foreach (DetailPlotControlItem control in m_DetailPlotControls)
		{
			control.ChangePlotVisible(isVisible);
		}
		m_ShowDetailPlots = isVisible;
		ApplyDetailPlotVisibility();
	}

	private static DetailPlotConfig CreateDefaultMainPlotConfig()
	{
		return new DetailPlotConfig
		{
			Title = "Frame Duration",
			AllCurves =
			{
				new CurveConfig
				{
					Label = "Frame Duration",
					LineColor = ScottPlotColorUtil.GetColor(0),
					UnitName = "ms"
				}
			}
		};
	}

	private void Timeline_OnScopeRangeChangedByUi()
	{
		m_IsAutoFollow = false;
		m_IsAutoFollowByUi = true;
	}

	private int GetDisplaySampleIndex(int sourceFrameIndex)
	{
		if (m_CurrentFrameSamples.Length == 0)
		{
			return -1;
		}

		int firstSampleFrameIndex = m_CurrentFrameSamples[0].Index;
		int lastSampleFrameIndex = m_CurrentFrameSamples[^1].Index;

		if (sourceFrameIndex <= firstSampleFrameIndex)
		{
			return 0;
		}

		if (sourceFrameIndex >= lastSampleFrameIndex)
		{
			return m_CurrentFrameSamples.Length - 1;
		}

		int left = 0;
		int right = m_CurrentFrameSamples.Length - 1;
		while (left <= right)
		{
			int mid = left + ((right - left) / 2);
			int sampleFrameIndex = m_CurrentFrameSamples[mid].Index;
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

		// `left` is first index with frame index greater than sourceFrameIndex
		int previousIndex = Math.Max(0, right);
		int nextIndex = Math.Min(m_CurrentFrameSamples.Length - 1, left);
		int previousDistance = sourceFrameIndex - m_CurrentFrameSamples[previousIndex].Index;
		int nextDistance = m_CurrentFrameSamples[nextIndex].Index - sourceFrameIndex;
		return (previousDistance <= nextDistance) ? previousIndex : nextIndex;
	}
}
