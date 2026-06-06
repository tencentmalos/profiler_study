using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ScottPlot;
using ScottPlot.Avalonia;
using System;

namespace ProfilerStudy.Avalonia.Timeline;

/// <summary>
/// Manages shared X-axis plot functionality
/// </summary>
public class ucChildSharedXAxis
{
    private AvaPlot _sharedXAxisPlot;
    
    // Constants
    public const int kLeftPaddingValue = 100;
    
    // Events
    public event Action<double, double>? AxisRangeChanged;

    // Events when scope range changed by ui
    public event Action? AxisRangeChangedByUi;

    public ucChildSharedXAxis(AvaPlot sharedXAxisPlot)
    {
        _sharedXAxisPlot = sharedXAxisPlot;
        SetupSharedXAxisPlot();
    }
    
    /// <summary>
    /// Setup the shared X-axis plot
    /// </summary>
    public void SetupSharedXAxisPlot()
    {
        _sharedXAxisPlot.Plot.Clear();
        
        // Hide everything except the bottom X-axis for SharedXAxisPlot
        _sharedXAxisPlot.Plot.Axes.Left.IsVisible = false;
        _sharedXAxisPlot.Plot.Axes.Right.IsVisible = false;
        _sharedXAxisPlot.Plot.Axes.Top.IsVisible = false;
        _sharedXAxisPlot.Plot.Grid.IsVisible = false;

        // Set X-axis label
        _sharedXAxisPlot.Plot.Axes.Bottom.Label.Text = "Time";

        // Set fixed padding for proper alignment
        PixelPadding fixedPadding = new(left: kLeftPaddingValue, right: 10, bottom: 30, top: 10);
        _sharedXAxisPlot.Plot.Layout.Fixed(fixedPadding);
        
        _sharedXAxisPlot.PointerWheelChanged += SharedXAxisPlotOnPointerWheelChanged;
        _sharedXAxisPlot.PointerMoved += SharedXAxisPlotOnPointerMoved;
    }
    
    /// <summary>
    /// Update shared X-axis range
    /// </summary>
    public void UpdateXRange(double xMin, double xMax)
    {
        _sharedXAxisPlot.Plot.Axes.SetLimitsX(xMin, xMax);
        _sharedXAxisPlot.Refresh();
    }
    
    private void SharedXAxisPlotOnPointerMoved(object? sender, PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(null).Properties;
        if (properties.IsLeftButtonPressed)
        {
            OnAxisDragged();
        }
    }

    private void SharedXAxisPlotOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        OnAxisDragged();
    }

    private void OnAxisDragged()
    {
        var scopeStart = _sharedXAxisPlot.Plot.Axes.Bottom.Min;
        var scopeEnd = _sharedXAxisPlot.Plot.Axes.Bottom.Max;
        
        // Trigger event to notify scope change
        AxisRangeChanged?.Invoke(scopeStart, scopeEnd);

        AxisRangeChangedByUi?.Invoke();
    }
}
