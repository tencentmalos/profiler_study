using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProfilerStudy.Avalonia.Timeline;

namespace ProfilerStudy.Avalonia.Timeline;

public partial class ucTimelineScope : UserControl
{
    // Main components using the new manager classes
    private ucChildSelectScope _scopeRegion = null!;
    private ucChildMainStats _mainStatsRegion = null!;
    private ucChildDetails _detailsRegion = null!;
    private ucChildSharedXAxis _sharedXAxisRegion = null!;

    // Tooltip related fields
    //private Border _tooltipPanel;
    private TextBlock _tooltipTitle = null!;
    private TextBlock _tooltipContent = null!;
    private ucDetailPlotComponent? _currentHoveredPlot;
    private Popup _popupTooltip = null!;
    private Border _tooltipBorder = null!;
    private StackPanel _tooltipContainer = null!;
    private readonly Dictionary<AvaPlot, (EventHandler<PointerEventArgs> Moved, EventHandler<PointerEventArgs> Exited)> _tooltipHandlers = new();

    public ucChildSelectScope ChildSelectScope => _scopeRegion;
    public ucChildMainStats ChildMainStats => _mainStatsRegion;
    public ucChildDetails ChildDetails => _detailsRegion;
    public ucChildSharedXAxis ChildSharedXAxis => _sharedXAxisRegion;

    #region "Events"
    public event Action? OnScopeRangeChangedByUi;

    //public event Action<bool, string, List<CurvePointHint>>? OnPlotTooltipShowNotify;
    //public event Action? OnPlotTooltipHideNotify;
    #endregion


    public bool EnableAutoScaleY { get; set; } = true;

    public void SetCompactTimelineMode(bool isCompact)
    {
        if (RootGrid == null)
        {
            return;
        }

        RootGrid.RowDefinitions = isCompact
            ? new RowDefinitions("*,0,0")
            : new RowDefinitions("200,60,*");
        TimelineBorder.Margin = isCompact ? new Thickness(0) : new Thickness(0, 0, 0, 8);
        TimelineTitleTextBlock.IsVisible = !isCompact;
        SharedXAxisBorder.IsVisible = !isCompact;
        DetailViewArea.IsVisible = !isCompact;
    }

    // DetailView configurations
    //private List<DetailViewConfig> _detailViewConfigs = new List<DetailViewConfig>();

    public const int kLeftPaddingValue = 100;



    public ucTimelineScope()
    {
        InitializeComponent();
        
        // Initialize tooltip controls
        InitializeTooltip();
        
        // Initialize managers
        InitializeManagers();
        
        // Setup tooltip for main stats
        //SetupMainStatsTooltip();
        
        ResetAllPlots();
        
        // Set initial scope range
        var initialScopeStart = _scopeRegion.ScopeStart;
        var initialScopeEnd = _scopeRegion.ScopeEnd;

        _sharedXAxisRegion.UpdateXRange(initialScopeStart, initialScopeEnd);
        _detailsRegion.UpdateXRange(initialScopeStart, initialScopeEnd, EnableAutoScaleY);
    }

    private void InitializeManagers()
    {
        // Initialize timeline manager
        _mainStatsRegion = new ucChildMainStats(TimelinePlot);
        _mainStatsRegion.AxisRangeChanged += XRangeChangedByMainStatsOperate;
        _mainStatsRegion.AxisRangeChangedByUi += ScopeRangedChangedByUi;
        
        // Initialize scope manager and connect events
        _scopeRegion = new ucChildSelectScope(TimelinePlot);
        _scopeRegion.ScopeChanged += XRangeChangedByScopeOperate;
        _scopeRegion.ScopeChangedByUi += ScopeRangedChangedByUi;
        
        // Initialize shared X-axis manager and connect events
        _sharedXAxisRegion = new ucChildSharedXAxis(SharedXAxisPlot);
        _sharedXAxisRegion.AxisRangeChanged += XRangeChangedBySharedAxisOperate;
        _sharedXAxisRegion.AxisRangeChangedByUi += ScopeRangedChangedByUi;

        // Initialize detail view manager
        _detailsRegion = new ucChildDetails(DetailViewContainer, SharedXAxisPlot);
    }

    private void ScopeRangedChangedByUi()
    {
        OnScopeRangeChangedByUi?.Invoke();
    }

    private void XRangeChangedByScopeOperate(double scopeStart, double scopeEnd)
    {
        // Constrain scope to data bounds
        (double xMin, double xMax) = _mainStatsRegion.GetDataSourceXRange();
        if (xMax > xMin)
        {
            _scopeRegion.ConstrainToDataBounds(xMin, xMax);
        }
        
        // Update detail views and shared axis
        NotifyXRangeChanged(_scopeRegion.ScopeStart, _scopeRegion.ScopeEnd);
    }

    private void XRangeChangedByMainStatsOperate(double scopeStart, double scopeEnd)
    {
        _scopeRegion.UpdateXRange(scopeStart, scopeEnd, false);
        _sharedXAxisRegion.UpdateXRange(scopeStart, scopeEnd);
        _detailsRegion.UpdateXRange(scopeStart, scopeEnd, EnableAutoScaleY);
    }

    private void XRangeChangedBySharedAxisOperate(double scopeStart, double scopeEnd)
    {
        // Update scope region when shared axis is dragged
        _scopeRegion.UpdateXRange(scopeStart, scopeEnd, false); // Don't trigger event to avoid recursion

        // Update detail views
        _detailsRegion.UpdateXRange(scopeStart, scopeEnd, EnableAutoScaleY);
    }

    public void ChangeTimelineConfig(DetailPlotConfig timelineConfig)
    {
        _mainStatsRegion.SetConfig(timelineConfig);
        ResetAllPlots();

        SetupPlotTooltip(ChildMainStats.LogicPlot);
    }

    public ucDetailPlotComponent AddDetailPlot(DetailPlotConfig config)
    {
        //_detailViewConfigs.Add(config);
        var plot = _detailsRegion.AddCurvePlot(config);
        
        // Setup tooltip for this plot
        SetupPlotTooltip(plot);
        
        // Update axis ranges
        var scopeStart = _scopeRegion.ScopeStart;
        var scopeEnd = _scopeRegion.ScopeEnd;
        _detailsRegion.UpdateXRange(scopeStart, scopeEnd, EnableAutoScaleY);

        return plot;
    }

    public ucDetailPlotComponent AddFlameGraphDetailView(string title = "Flame Graph - Execution Stack")
    {
        var plot = _detailsRegion.AddFlamePlot(title);

        // Update axis ranges
        var scopeStart = _scopeRegion.ScopeStart;
        var scopeEnd = _scopeRegion.ScopeEnd;
        _detailsRegion.UpdateXRange(scopeStart, scopeEnd, EnableAutoScaleY);

        return plot;
    }

    public void RemoveDetailView(int index)
    {
        // Remove from detail view manager
        _detailsRegion.RemovePlot(index);
  
    }


    public void ResetAllPlots()
    {
        // Setup timeline plot
        _mainStatsRegion.SetupPlot();
        
        // Setup tooltip for main stats after plot is initialized
        //SetupMainStatsTooltip();
        
        // Refresh plots
        _mainStatsRegion.Refresh();


        // Recreate scope span after plot clear
        _scopeRegion.RecreateSpan();

        // Re-setup SharedXAxisPlot
        _sharedXAxisRegion.SetupSharedXAxisPlot();

        // Regenerate data for all detail views
        //Timeline.ChildDetails.RegenerateAllData();

        // Reset view
        // Reset scope to default
        _scopeRegion.ResetScope();

        // Reset all plot views
        TimelinePlot.Plot.Axes.AutoScale();
        TimelinePlot.Plot.Axes.Bottom.Min = 0;

        TimelinePlot.Refresh();
        ApplyTheme();
    }

    public void UpdateMainStatsAndAllDetailPlots()
    {
        ChildMainStats.LogicPlot.UpdatePlot();
        foreach(var plot in ChildDetails.AllDetailPlots)
        {
            plot.UpdatePlot();
        }
    }

    private void NotifyXRangeChanged(double xMin, double xMax)
    {
        _sharedXAxisRegion.UpdateXRange(xMin, xMax);
        _detailsRegion.UpdateXRange(xMin, xMax, EnableAutoScaleY);
    }

    // Public methods for configuration management
    public void SetTimelineConfig(DetailPlotConfig config)
    {
        _mainStatsRegion.SetConfig(config);
        //GenerateData();
        _mainStatsRegion.SetupPlot();
        
        // Setup tooltip for main stats after plot is reconfigured
        //SetupMainStatsTooltip();
        
        _mainStatsRegion.Refresh();

        SetupPlotTooltip(ChildMainStats.LogicPlot);
    }

    public DetailPlotConfig? GetTimelineConfig()
    {
        return _mainStatsRegion?.Config;
    }

    #region Tooltip Implementation

    private void InitializeTooltip()
    {
        _tooltipContainer = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Vertical,
        };
        _tooltipTitle = new TextBlock
        {
            FontWeight = global::Avalonia.Media.FontWeight.Bold,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4),
        };
        _tooltipTitle.Classes.Add("timeline-popup-tooltip-title");
        _tooltipContent = new TextBlock
        {
            FontSize = 11,
            FontFamily = "Consolas",
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
        };
        _tooltipContent.Classes.Add("timeline-popup-tooltip-content");

        _tooltipContainer.Children.Add(_tooltipTitle);
        _tooltipContainer.Children.Add(_tooltipContent);

        _tooltipBorder = new Border
        {
            Child = _tooltipContainer
        };
        _tooltipBorder.Classes.Add("timeline-popup-tooltip");
        TimelineScopeThemeHelper.ApplyTooltipTheme(_tooltipBorder, _tooltipTitle, _tooltipContent);

        _popupTooltip = new Popup
        {
            Child = _tooltipBorder,
            Placement = PlacementMode.Pointer,
            IsLightDismissEnabled = false,
            IsOpen = false,
        };
    }

    private void OnMainStatsPointerExited(object sender, PointerEventArgs e)
    {
        HideTooltip();
    }

    public void SetupPlotTooltip(ucDetailPlotComponent plotComponent)
    {
        if (plotComponent?.Plot == null) return;

        if (_tooltipHandlers.TryGetValue(plotComponent.Plot, out var previousHandlers))
        {
            plotComponent.Plot.PointerMoved -= previousHandlers.Moved;
            plotComponent.Plot.PointerExited -= previousHandlers.Exited;
        }

        EventHandler<PointerEventArgs> movedHandler = (sender, e) => OnPlotPointerMoved(plotComponent, e);
        EventHandler<PointerEventArgs> exitedHandler = (sender, e) => OnPlotPointerExited(plotComponent, e);
        plotComponent.Plot.PointerMoved += movedHandler;
        plotComponent.Plot.PointerExited += exitedHandler;
        _tooltipHandlers[plotComponent.Plot] = (movedHandler, exitedHandler);
    }

    private void OnPlotPointerMoved(ucDetailPlotComponent plotComponent, PointerEventArgs e)
    {
        try
        {
            _currentHoveredPlot = plotComponent;
            var position = e.GetPosition(plotComponent.Plot);
            
            // Convert pixel coordinates to plot coordinates
            var plotCoordinates = plotComponent.Plot.Plot.GetCoordinates((float)position.X, (float)position.Y);
            
            // Get tooltip content based on plot type and data
            var tooltipInfo = GetTooltipContent(plotComponent, plotCoordinates.X, plotCoordinates.Y);
            
            if (!string.IsNullOrEmpty(tooltipInfo.Title) && !string.IsNullOrEmpty(tooltipInfo.Content))
            {
                UpdateTooltipContent(tooltipInfo.Title, tooltipInfo.Content);
                ShowTooltip(e);
            }
            else
            {
                HideTooltip();
            }
        }
        catch (Exception)
        {
            // Handle any exceptions gracefully
            HideTooltip();
        }
    }

    private void OnPlotPointerExited(ucDetailPlotComponent plotComponent, PointerEventArgs e)
    {
        if (_currentHoveredPlot == plotComponent)
        {
            _currentHoveredPlot = null;
            HideTooltip();
        }
    }

    private (string Title, string Content) GetTooltipContent(ucDetailPlotComponent plotComponent, double x, double y)
    {
        if (plotComponent?.Config == null) return ("", "");

        var title = plotComponent.Config.Title ?? "Plot Data";
        var content = new StringBuilder();

        // Add time information
        content.AppendLine($"Time: {x:F3} s");
        content.AppendLine("-------------------------------------");
        // Get curve data based on plot type
        if (plotComponent.Type == ucDetailPlotComponent.DetailPlotType.Value2D && plotComponent.Curve2DGenerator != null)
        {
            ////var curveData = GetNearestCurveData2D(plotComponent, x);
            var hoverInfo = plotComponent.Curve2DGenerator.MouseHoverResult;
            if(hoverInfo != null)
            {
                //var nearestItem = hoverInfo.NearestHintItem;
                //if (nearestItem != null)
                //{
                //    content.AppendLine($"{nearestItem.Title,-20}:{nearestItem.Value,-8:F3} {nearestItem.Unit}");
                //}
                //content.AppendLine("-------------------------------------");

                var hintInfoList = hoverInfo.HintItems; //GetNearestHintItemsFor2D(plotComponent, x, y);
                foreach (var hint in hintInfoList)
                {
                    string extraFlag = hint == hoverInfo.NearestHintItem ? "(*)" : "";
                    content.AppendLine($"{hint.Title + extraFlag,-20}:{hint.Value,-8:F3} {hint.Unit}");
                }
            }

        }
        else if (plotComponent.Type == ucDetailPlotComponent.DetailPlotType.FlameGraph && plotComponent.FlameGenerator != null)
        {
            content.AppendLine($"Y Position: {y:F3}");
            content.AppendLine("Flame Graph Data");
        }

        return (title, content.ToString().Trim());
    }


    //private List<CurvePointHint> GetNearestHintItemsFor2D(ucDetailPlotComponent plotComponent, double targetX, double targetY)
    //{
    //    List<CurvePointHint> result = new List<CurvePointHint>();

    //    try
    //    {
    //        // Access data through the generator's curve list
    //        if (plotComponent.Curve2DGenerator != null)
    //        {
    //            var generator = plotComponent.Curve2DGenerator;

    //            for (int curveIndex = 0; curveIndex < plotComponent.Config.AllCurves.Count; curveIndex++)
    //            {
    //                var curveCfg = plotComponent.Config.AllCurves[curveIndex];
    //                string curveName = curveCfg.Label ?? $"Curve {curveIndex + 1}";
    //                string unitName = curveCfg.UnitName ?? "";

    //                try
    //                {
    //                    var curve = generator[curveIndex];
    //                    if (curve != null && curve.XDatas.Count > 0)
    //                    {
    //                        // Find the nearest X value
    //                        int nearestIndex = 0;
    //                        double minDistance = double.MaxValue;

    //                        for (int i = 0; i < curve.XDatas.Count; i++)
    //                        {
    //                            double distance = Math.Abs(curve.XDatas[i] - targetX);
    //                            if (distance < minDistance)
    //                            {
    //                                minDistance = distance;
    //                                nearestIndex = i;
    //                            }
    //                        }

    //                        if (nearestIndex < curve.YDatas.Count)
    //                        {
    //                            double yVal = curve.YDatas[nearestIndex];
    //                            result.Add(new CurvePointHint { Index = curveIndex, Title = curveName, Unit = unitName, Value = yVal });
    //                            //result[curveName] = curve.YDatas[nearestIndex];
    //                        }
    //                    }
    //                }
    //                catch (Exception)
    //                {
    //                    // Skip this curve if there's an error
    //                    continue;
    //                }
    //            }
    //        }
    //    }
    //    catch (Exception)
    //    {
    //        // Handle exceptions gracefully
    //    }

    //    return result;
    //}

    private void UpdateTooltipContent(string title, string content)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_tooltipTitle != null)
                _tooltipTitle.Text = title;
            
            if (_tooltipContent != null)
                _tooltipContent.Text = content;
        });
    }

    private void UpdatePosition(PointerEventArgs e)
    {
        var position = e.GetPosition(this);

        _popupTooltip.PlacementTarget = this;
        _popupTooltip.PlacementRect = new Rect(position.X, position.Y,0, 0);
        _popupTooltip.PlacementAnchor = PopupAnchor.TopLeft;
        _popupTooltip.PlacementGravity = PopupGravity.BottomRight;
        _popupTooltip.Placement = PlacementMode.Pointer;
        //_popupTooltip.PlacementAnchor = PopupAnchor.TopLeft;
        //_popupTooltip.PlacementGravity = PopupGravity.TopLeft;

        _popupTooltip.HorizontalOffset = 15;
        _popupTooltip.VerticalOffset = 15;
        
    }

    private void ShowTooltip(PointerEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _popupTooltip.IsOpen = true;
            UpdatePosition(e);
            //if (_tooltipPanel != null)
            //    _tooltipPanel.IsVisible = true;
        });
    }

    private void HideTooltip()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _popupTooltip.IsOpen = false;
            //if (_tooltipPanel != null)
            //    _tooltipPanel.IsVisible = false;
        });
    }

    public void ApplyTheme()
    {
        TimelineScopeThemeHelper.ApplyTooltipTheme(_tooltipBorder, _tooltipTitle, _tooltipContent);
        TimelineScopeThemeHelper.ApplyPlotTheme(TimelinePlot);
        TimelineScopeThemeHelper.ApplyPlotTheme(SharedXAxisPlot);
        ChildSelectScope.ApplyTheme();

        foreach (var plot in ChildDetails.AllDetailPlots)
        {
            TimelineScopeThemeHelper.ApplyPlotTheme(plot.Plot);
        }
    }

    #endregion
}
