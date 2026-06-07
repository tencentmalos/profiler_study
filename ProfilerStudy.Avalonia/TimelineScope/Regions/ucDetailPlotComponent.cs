using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using ProfilerStudy.Avalonia.Timeline;

namespace ProfilerStudy.Avalonia.Timeline;

/// <summary>
/// Common functionality for detail views - extracted from DetailsRegion and MainStatsRegion
/// </summary>
public class ucDetailPlotComponent
{
    public enum DetailPlotType
    {
        //Value1D,
        Value2D,
        FlameGraph
    }
    
    public DetailPlotType Type { get; set; }
    public DetailPlotConfig Config { get; set; }
    public AvaPlot Plot { get; set; }
    public Border? Border { get; set; }
    public IPlotGenerator DataGenerator { get; set; } // Using interface instead of object

    public double DefaultXMaxValue { get; set; } = double.MinValue;

    public double DefaultXMinValue { get; set; } = double.MaxValue;

    public bool IsFixedYRange { get; set; } = false;

    public double FixedYMin { get; set; } = 0.0;

    public double FixedYMax { get; set; } = 1.0;

    public bool IsXAxisLimitOperate { get; set; } = true;


    //public ICurvePlotGen? AsCurve => ViewInstance as ICurvePlotGen;
    public FlameGenerator? FlameGenerator => DataGenerator as FlameGenerator;

    public Curve2DGenerator? Curve2DGenerator => DataGenerator as Curve2DGenerator;

    //public Curve1DGenerator? Curve1DGenerator => DataGenerator as Curve1DGenerator;

    public double CurrentXStart => Plot.Plot.Axes.Bottom.Min;
    public double CurrentXEnd => Plot.Plot.Axes.Bottom.Max;
    
    public ucDetailPlotComponent(DetailPlotConfig config, AvaPlot plot, DetailPlotType plotType, Border? border = null)
    {
        Config = config;
        Plot = plot;
        Border = border;

        Type = plotType;
        DataGenerator = CreatePlotGenerate(Type, plot, config);
    }
    
    /// <summary>
    /// Create appropriate view instance based on type
    /// </summary>
    public static IPlotGenerator CreatePlotGenerate(DetailPlotType viewType, AvaPlot plot, DetailPlotConfig config)
    {
        switch (viewType)
        {
            case DetailPlotType.Value2D:
                return new Curve2DGenerator(plot, config);
            case DetailPlotType.FlameGraph:
                return new FlameGenerator(plot);
            default:
                return new Curve2DGenerator(plot, config);
        }
    }
    
    /// <summary>
    /// Update plot for this detail view
    /// </summary>
    public void UpdatePlot()
    {
        DataGenerator.UpdatePlot();
    }
    
    /// <summary>
    /// Update X-axis range for this detail view
    /// </summary>
    public void UpdateXRange(double xMin, double xMax, bool isAutoScaleY)
    {
        DataGenerator.UpdateXRange(xMin, xMax, isAutoScaleY, IsXAxisLimitOperate);
    }
    
    /// <summary>
    /// Set fixed layout padding for the plot
    /// </summary>
    public void SetFixedLayout(PixelPadding padding)
    {
        Plot.Plot.Layout.Fixed(padding);
    }
    
    /// <summary>
    /// Get standard fixed padding for timeline plots
    /// </summary>
    public static PixelPadding GetStandardFixedPadding()
    {
        return new PixelPadding(left: ucTimelineScope.kLeftPaddingValue, right: 10, bottom: 30, top: 10);
    }
    
    /// <summary>
    /// Configure user input for detail views (locked horizontal)
    /// </summary>
    public void UpdateXLimits()
    {
        AxisLimits currentLimits = Plot.Plot.Axes.GetLimits();
        
        var lockedHorizontalRule = new ScottPlot.AxisRules.LockedHorizontal(
            Plot.Plot.Axes.Bottom, 
            currentLimits.Left, 
            currentLimits.Right);
        
        Plot.Plot.Axes.Rules.Add(lockedHorizontalRule);
    }
    
    /// <summary>
    /// Create UI border container for detail view
    /// </summary>
    public static Border CreateDetailPlotBorder(string title, AvaPlot plot, bool isFirstView = false)
    {
        var border = new Border
        {
            BorderBrush = TimelineScopeThemeHelper.GetBrush(TimelineScopeThemeHelper.PanelBorderBrushKey, "#FFD0D9E4"),
            BorderThickness = new Thickness(1, 0, 1, 1),
            Margin = new Thickness(0, 0, 0, isFirstView ? 8 : 0),
            Height = 250
        };

        var grid = new Grid();

        var textBlock = new TextBlock
        {
            Text = title,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(5),
            FontWeight = global::Avalonia.Media.FontWeight.Bold,
            Foreground = TimelineScopeThemeHelper.GetBrush(TimelineScopeThemeHelper.TextBrushKey, "#FF18212B"),
            Background = TimelineScopeThemeHelper.GetBrush(TimelineScopeThemeHelper.PlotTitleSurfaceBrushKey, "#FFF7FAFD"),
            Padding = new Thickness(2)
        };

        grid.Children.Add(plot);
        grid.Children.Add(textBlock);
        border.Child = grid;
        TimelineScopeThemeHelper.ApplyPlotTheme(plot);
        
        return border;
    }

    public void MoveXAxisByOffset(double offset)
    {
        Plot.Plot.Axes.Bottom.Range.Pan(offset);
    }

    public void ForceChangeXAxisRange(double xMin, double xMax)
    {
        Plot.Plot.Axes.Bottom.Range.Set(xMin, xMax);
        //Plot.Plot.Axes.Bottom.Min = 0.0;
        //Plot.Plot.Axes.Bottom.Max = xMax;
    }

    public (double, double) GetDataSourceXRange(bool takeLimit = true)
    {
        if (DataGenerator != null)
        {
            (double xMin, double xMax) = DataGenerator.GetDataSourceXRange();
            if (takeLimit)
            {
                return (Math.Min(xMin, DefaultXMinValue), Math.Max(xMax, DefaultXMaxValue));
            }
            else
            {
                return (xMin, xMax);
            }
        }
        
        return (0, 0);
    }

}
