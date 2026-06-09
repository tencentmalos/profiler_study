using ScottPlot;
using System;
using System.Collections.Generic;

namespace ProfilerStudy.Avalonia.Timeline;

/// <summary>
/// Data series configuration for a single DetailView
/// </summary>
public class CurveConfig
{
    public string Label { get; set; } = "";
    public Color LineColor { get; set; } = ScottPlotColorUtil.GetColor(0);
    public string UnitName { get; set; } = "";
}

/// <summary>
/// DetailView configuration that can contain multiple data series
/// </summary>
public class DetailPlotConfig
{
    public string Title { get; set; } = "";
    public List<CurveConfig> AllCurves { get; set; } = new();
    public ucDetailPlotComponent.DetailPlotType DetailPlotType { get; set; } = ucDetailPlotComponent.DetailPlotType.Value2D;
}

public class CurvePointHint
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    
    public double Value { get; set; }
}

public class PlotHoverResult
{
    public List<CurvePointHint> HintItems { get; private set; } = new List<CurvePointHint> { };
    public CurvePointHint? NearestHintItem { get; internal set; }
}

