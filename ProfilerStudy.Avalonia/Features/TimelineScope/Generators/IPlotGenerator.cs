using Avalonia.Controls;
using ScottPlot;
using ScottPlot.Avalonia;

namespace ProfilerStudy.Avalonia.Timeline;

/// <summary>
/// Base interface for all image generators (1D, 2D, Flame)
/// Provides common functionality for plot generation and management
/// </summary>
public interface IPlotGenerator
{
    /// <summary>
    /// Gets the associated plot control
    /// </summary>
    AvaPlot Plot { get; }

    /// <summary>
    /// Update the plot with current data
    /// </summary>
    void UpdatePlot();

    /// <summary>
    /// Update X-axis range
    /// </summary>
    /// <param name="xMin">Minimum X value</param>
    /// <param name="xMax">Maximum X value</param>
    void UpdateXRange(double xMin, double xMax, bool isAutoScaleY, bool isXAxisLimit);

    /// <summary>
    /// Set fixed layout padding
    /// </summary>
    /// <param name="padding">Pixel padding for the plot</param>
    void SetFixedLayout(PixelPadding padding);

    /// <summary>
    /// Hide all interactive elements (crosshairs, tooltips, etc.)
    /// </summary>
    void HideInteractiveElements();

    (double, double) GetDataSourceXRange();
}

/// <summary>
/// Extended interface for image generators that support configuration
/// </summary>
public interface ICurveGenerator : IPlotGenerator
{
    /// <summary>
    /// Gets the configuration for this image generator
    /// </summary>
    DetailPlotConfig Config { get; }
}

/// <summary>
/// Extended interface for flame graph generators
/// </summary>
public interface IFlameGenerator : IPlotGenerator
{
    /// <summary>
    /// Update time range for flame graph
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    void UpdateTimeRange(double startTime, double endTime);
}
