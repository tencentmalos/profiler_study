using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProfilerStudy.Avalonia.Timeline;

/// <summary>
/// Manages timeline plot functionality using DetailImage architecture
/// </summary>
public class ucChildMainStats
{
    public class ucVerticalLine
    {
        public VerticalLine NativeLine { get; internal set; } = null!;
        public double       TagTime { get; internal set; }
        public int          TagIndex { get; internal set; }
        public string       TagName { get; internal set; } = string.Empty;
    }

    private ucDetailPlotComponent _mainStatsDetailPlot = null!;
    private AvaPlot _timelinePlot;
    
    public DetailPlotConfig? Config => _mainStatsDetailPlot?.Config;
    public ucDetailPlotComponent LogicPlot => _mainStatsDetailPlot;
    public AvaPlot Plot => _timelinePlot;

    public List<ucVerticalLine> VLineList { get; internal set; } = new List<ucVerticalLine>();
    
    public ucChildMainStats(AvaPlot timelinePlot)
    {
        _timelinePlot = timelinePlot;
        InitializeDetailPlot();
    }
    
    private void InitializeDetailPlot()
    {
        var defaultConfig = new DetailPlotConfig
        {
            Title = "Main Stats",
            AllCurves = new List<CurveConfig>
            {
                new CurveConfig
                {
                    Label = "Timeline Data",
                    LineColor = ScottPlotColorUtil.GetColor(0),
                }
            }
        };
        
        SetConfig(defaultConfig);
    }
    
    public void SetConfig(DetailPlotConfig config)
    {
        if (config == null) return;

        // Clear the plot
        _timelinePlot.Plot.Clear();

        // Recreate DetailImage with new config
        _mainStatsDetailPlot = new ucDetailPlotComponent(config, _timelinePlot, config.DetailPlotType);
        _mainStatsDetailPlot.IsXAxisLimitOperate = false;

        // Reapply layout settings
        _mainStatsDetailPlot.SetFixedLayout(ucDetailPlotComponent.GetStandardFixedPadding());
        _timelinePlot.Plot.Axes.Bottom.IsVisible = true;
    }
    
    public void SetupPlot()
    {
        if (_mainStatsDetailPlot == null) return;
       
        // Generate data and update plot
        _mainStatsDetailPlot.UpdatePlot();
        
        // Ensure X-axis is visible for main stats
        _timelinePlot.Plot.Axes.Bottom.IsVisible = true;
        
        _timelinePlot.Plot.Axes.AutoScale();
        _timelinePlot.Plot.Axes.Bottom.Min = 0;

        ConfigureUserInput();

        //_timelinePlot.Plot.Axes.SetLimitsY(0.0, 1.0);

        var lockedVerticalRule = new ScottPlot.AxisRules.LockedVertical(
            Plot.Plot.Axes.Left,
            0.0,
            1.0);
        Plot.Plot.Axes.Rules.Add(lockedVerticalRule);
    }
    
    private void ConfigureUserInput()
    {
        // Reset user input processor
        _timelinePlot.UserInputProcessor.Reset();
        _timelinePlot.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseDragPan>();
        
        // Add horizontal-only panning
        var panButton = ScottPlot.Interactivity.StandardMouseButtons.Left;
        var panResponse = new ScottPlot.Interactivity.UserActionResponses.MouseDragPan(panButton);
        panResponse.LockY = true;
        _timelinePlot.UserInputProcessor.UserActionResponses.Add(panResponse);
    }
    
    public void UpdateXRange(double xMin, double xMax, bool isAutoScaleY)
    {
        if (_mainStatsDetailPlot == null) return;
        
        _mainStatsDetailPlot.UpdateXRange(xMin, xMax, isAutoScaleY);
    }
    
    public void Refresh()
    {
        _timelinePlot.Refresh();
    }

    public (double, double) GetDataSourceXRange()
    {
        return LogicPlot.GetDataSourceXRange();
    }

    public ucVerticalLine AddVLine(double tagTime, int tagIndex, string tagName, Color tagColor)
    {
        var vl = _timelinePlot.Plot.Add.VerticalLine(tagTime, 1, tagColor, LinePattern.Dashed);
        vl.IsDraggable = false;
        vl.Text = tagName;

        var ret = new ucVerticalLine
        {
            NativeLine = vl,
            TagTime = tagTime,
            TagIndex = tagIndex,
            TagName = tagName,
        };
        VLineList.Add(ret);

        return ret;
    }
    
    public void ClearAllVLines()
    {
        _timelinePlot.Plot.Remove<VerticalLine>();
        VLineList.Clear();
    }
}
