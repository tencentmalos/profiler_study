using ProfilerStudy;
using ProfilerStudy.Avalonia.ProfilerStats.Timeline;
using ProfilerStudy.Avalonia.Timeline;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace ProfilerStudy.Avalonia.ProfilerStats;

internal sealed class ProfilerStatsController : ObservableObject
{
	private const double kDefaultSelectWindow = 30.0;

	private readonly ucProfilerStats m_View;
	private readonly ProfilerTimelineAdapter m_TimelineAdapter = new();
	private readonly List<DetailPlotControlItem> m_DetailPlotControls = new();
	private readonly Dictionary<string, CurveUiPlotBridgeItem> m_PlotBridges = new();
	private readonly List<CurveUiPlotBridgeItem> m_AllBridges = new();

	private ProfilerTimelinePlotModel m_PlotModel;
	private CurveUiPlotBridgeItem m_MainStatsBridge = null!;
	private ucDetailPlotComponent m_ScopeFlamePlot;
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

		m_PlotModel = m_TimelineAdapter.CreatePlotModel(document);

		BuildPlots();

		if (m_TimelineAdapter.FrameSamples.Length == 0)
		{
			ApplyEmptyState();
			return;
		}

		m_TimelineAdapter.FillFrameSeries(m_MainStatsBridge);
		m_TimelineAdapter.FillCustomStatSeries(m_PlotModel.CustomStats, m_PlotBridges);
		ApplyScopeFlamePlot(document);

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

	public void ApplyViewport(TimelineViewport viewport)
	{
		if (Dispatcher.UIThread.CheckAccess() is false)
		{
			Dispatcher.UIThread.Post(() => ApplyViewport(viewport));
			return;
		}

		if (viewport == null || m_TimelineAdapter.FrameSamples.Length == 0)
		{
			return;
		}

		int startSampleIndex = m_TimelineAdapter.GetDisplaySampleIndex(viewport.StartFrame);
		int endSampleIndex = m_TimelineAdapter.GetDisplaySampleIndex(viewport.EndFrame);
		if (startSampleIndex < 0 || endSampleIndex < 0)
		{
			return;
		}

		if (endSampleIndex < startSampleIndex)
		{
			(startSampleIndex, endSampleIndex) = (endSampleIndex, startSampleIndex);
		}

		double xStart = m_TimelineAdapter.FrameSamples[startSampleIndex].Index;
		double xEnd = m_TimelineAdapter.FrameSamples[endSampleIndex].Index;
		if (xEnd <= xStart)
		{
			xEnd = xStart + 1.0;
		}

		m_IsAutoFollow = false;
		m_IsAutoFollowByUi = true;
		Timeline.ChildSelectScope.UpdateXRange(xStart, xEnd, false);
		ApplyExplicitRange(xStart, xEnd);
	}

	private void BuildPlots()
	{
		Timeline.ChildDetails.ClearAll();
		UnsubscribePlotControls();
		m_DetailPlotControls.Clear();
		m_PlotBridges.Clear();
		m_AllBridges.Clear();
		m_ScopeFlamePlot = null;

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
		m_ScopeFlamePlot = null;
		m_PlotModel = m_TimelineAdapter.CreatePlotModel(null);

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
		if (Timeline == null || m_TimelineAdapter.FrameSamples.Length == 0)
		{
			Timeline.ChildSelectScope.UpdateXRange(0, Math.Max(kDefaultSelectWindow, 1.0), false);
			Timeline.ChildMainStats.UpdateXRange(0, Math.Max(kDefaultSelectWindow, 1.0), m_IsAutoScaleY);
			Timeline.ChildDetails.UpdateXRange(0, Math.Max(kDefaultSelectWindow, 1.0), m_IsAutoScaleY);
			return;
		}

		double minX = m_TimelineAdapter.FrameSamples[0].Index;
		double maxX = m_TimelineAdapter.FrameSamples[m_TimelineAdapter.FrameSamples.Length - 1].Index;
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
		ApplyExplicitRange(xStart, xEnd);
	}

	private void ApplyExplicitRange(double xStart, double xEnd)
	{
		Timeline.ChildMainStats.UpdateXRange(xStart, xEnd, m_IsAutoScaleY);
		Timeline.ChildSharedXAxis.UpdateXRange(xStart, xEnd);
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

		if (m_ScopeFlamePlot?.Border != null)
		{
			m_ScopeFlamePlot.Border.IsVisible = m_ShowDetailPlots;
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

		if (m_ScopeFlamePlot?.Border != null)
		{
			m_ScopeFlamePlot.Border.Height = heightPx;
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

		if (m_ScopeFlamePlot?.Border != null)
		{
			var control = new DetailPlotControlItem("Profiler Scopes", m_ScopeFlamePlot);
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

	private void ApplyScopeFlamePlot(SessionDocument document)
	{
		FlameGenerator.FlameGraphConfig flameConfig = m_TimelineAdapter.CreateScopeFlameGraphConfig(document);
		if (flameConfig.StackFrames.Count == 0)
		{
			m_ScopeFlamePlot = null;
			return;
		}

		m_ScopeFlamePlot = Timeline.AddFlameGraphDetailView("Profiler Scopes");
		m_ScopeFlamePlot.FlameGenerator?.Configure(flameConfig);
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

		int startFrame = m_TimelineAdapter.GetNearestFrameIndex(Timeline.ChildSelectScope.ScopeStart);
		int endFrame = m_TimelineAdapter.GetNearestFrameIndex(Timeline.ChildSelectScope.ScopeEnd);
		if (startFrame < 0 || endFrame < 0)
		{
			return;
		}

		if (endFrame < startFrame)
		{
			(startFrame, endFrame) = (endFrame, startFrame);
		}

		m_View.NotifyViewportChangedByUser(startFrame, endFrame);
	}
}
