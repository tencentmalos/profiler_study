using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using AvaloniaColor = Avalonia.Media.Color;
using ScottPlotColor = ScottPlot.Color;

namespace ProfilerStudy.Avalonia.Timeline;

internal static class TimelineScopeThemeHelper
{
    public const string PanelBackgroundBrushKey = "TimelineScopePanelBackgroundBrush";
    public const string SubtleBackgroundBrushKey = "TimelineScopeSubtleBackgroundBrush";
    public const string CardBackgroundBrushKey = "TimelineScopeCardBackgroundBrush";
    public const string PanelBorderBrushKey = "TimelineScopePanelBorderBrush";
    public const string SeparatorBrushKey = "TimelineScopeSeparatorBrush";
    public const string TextBrushKey = "TimelineScopeTextBrush";
    public const string SubtleTextBrushKey = "TimelineScopeSubtleTextBrush";
    public const string PlotSurfaceBrushKey = "TimelineScopePlotSurfaceBrush";
    public const string PlotTitleSurfaceBrushKey = "TimelineScopePlotTitleSurfaceBrush";
    public const string PlotGridBrushKey = "TimelineScopePlotGridBrush";
    public const string TooltipBackgroundBrushKey = "TimelineScopeTooltipBackgroundBrush";
    public const string TooltipBorderBrushKey = "TimelineScopeTooltipBorderBrush";
    public const string TooltipTextBrushKey = "TimelineScopeTooltipTextBrush";
    public const string LegendBackgroundBrushKey = "TimelineScopeLegendBackgroundBrush";
    public const string LegendBorderBrushKey = "TimelineScopeLegendBorderBrush";
    public const string AddedBrushKey = "TimelineScopeAddedBrush";
    public const string RemovedBrushKey = "TimelineScopeRemovedBrush";
    public const string WarningBrushKey = "TimelineScopeWarningBrush";
    public const string InfoBrushKey = "TimelineScopeInfoBrush";
    public const string AccentBrushKey = "TimelineScopeAccentBrush";
    public const string AddedSurfaceBrushKey = "TimelineScopeAddedSurfaceBrush";
    public const string RemovedSurfaceBrushKey = "TimelineScopeRemovedSurfaceBrush";
    public const string WarningSurfaceBrushKey = "TimelineScopeWarningSurfaceBrush";
    public const string InfoSurfaceBrushKey = "TimelineScopeInfoSurfaceBrush";
    public const string AccentSurfaceBrushKey = "TimelineScopeAccentSurfaceBrush";
    public const string AddedBorderBrushKey = "TimelineScopeAddedBorderBrush";
    public const string RemovedBorderBrushKey = "TimelineScopeRemovedBorderBrush";
    public const string WarningBorderBrushKey = "TimelineScopeWarningBorderBrush";
    public const string InfoBorderBrushKey = "TimelineScopeInfoBorderBrush";
    public const string AccentBorderBrushKey = "TimelineScopeAccentBorderBrush";

    public static IBrush GetBrush(string key, string fallbackHex)
    {
        if (Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out var value) == true &&
            value is IBrush brush)
        {
            return brush;
        }

        return new SolidColorBrush(AvaloniaColor.Parse(fallbackHex));
    }

    public static AvaloniaColor GetAvaloniaColor(string key, string fallbackHex)
    {
        var brush = GetBrush(key, fallbackHex);
        return brush is ISolidColorBrush solidBrush ? solidBrush.Color : AvaloniaColor.Parse(fallbackHex);
    }

    public static ScottPlotColor GetPlotColor(string key, string fallbackHex)
    {
        var color = GetAvaloniaColor(key, fallbackHex);
        return new ScottPlotColor(color.R, color.G, color.B, color.A);
    }

    public static void ApplyPlotTheme(AvaPlot plot)
    {
        var textColor = GetPlotColor(TextBrushKey, "#FFF2F6FA");
        var plotSurface = GetPlotColor(PlotSurfaceBrushKey, "#FF11181F");
        var plotGrid = GetPlotColor(PlotGridBrushKey, "#263F5365");
        var legendBackground = GetPlotColor(LegendBackgroundBrushKey, "#E619232D");
        var legendBorder = GetPlotColor(LegendBorderBrushKey, "#FF3E5366");

        plot.Plot.FigureBackground.Color = plotSurface;
        plot.Plot.DataBackground.Color = plotSurface;
        plot.Plot.Axes.Color(textColor);
        plot.Plot.Grid.MajorLineColor = plotGrid;
        plot.Plot.Grid.MinorLineColor = plotGrid;
        plot.Plot.Legend.BackgroundColor = legendBackground;
        plot.Plot.Legend.OutlineColor = legendBorder;
        plot.Plot.Legend.FontColor = textColor;
        plot.Refresh();
    }

    public static void ApplyTooltipTheme(Border border, TextBlock title, TextBlock content)
    {
        border.Background = GetBrush(TooltipBackgroundBrushKey, "#FF0D1319");
        border.BorderBrush = GetBrush(TooltipBorderBrushKey, "#FF4E677C");
        title.Foreground = GetBrush(TooltipTextBrushKey, "#FFF5F8FB");
        content.Foreground = GetBrush(TooltipTextBrushKey, "#FFF5F8FB");
    }

    public static void ApplyLabelTheme(Text label)
    {
        label.LabelFontColor = GetPlotColor(TextBrushKey, "#FFF2F6FA");
    }

    public static void ApplyTooltipTheme(Tooltip tooltip)
    {
        tooltip.FillColor = GetPlotColor(TooltipBackgroundBrushKey, "#FF0D1319");
        tooltip.LineColor = GetPlotColor(TooltipBorderBrushKey, "#FF4E677C");
        tooltip.LabelFontColor = GetPlotColor(TooltipTextBrushKey, "#FFF5F8FB");
    }

    public static void ApplyCrosshairTheme(Crosshair crosshair, string key, string fallbackHex)
    {
        var color = GetPlotColor(key, fallbackHex);
        crosshair.LineColor = color;
        crosshair.MarkerColor = color;
    }

    public static IBrush GetDeltaBrush(double value)
    {
        if (value > 0)
        {
            return GetBrush(AddedBrushKey, "#FF63D79B");
        }

        if (value < 0)
        {
            return GetBrush(RemovedBrushKey, "#FFFF7E8B");
        }

        return GetBrush(TextBrushKey, "#FFF2F6FA");
    }

    public static IBrush GetChangeTypeBrush(string? changeType)
    {
        return changeType switch
        {
            "Added" => GetBrush(AddedBrushKey, "#FF63D79B"),
            "Removed" => GetBrush(RemovedBrushKey, "#FFFF7E8B"),
            "Changed" => GetBrush(WarningBrushKey, "#FFFFB44F"),
            _ => GetBrush(TextBrushKey, "#FFF2F6FA")
        };
    }
}
