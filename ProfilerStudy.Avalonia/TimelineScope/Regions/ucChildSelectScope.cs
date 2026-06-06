using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ProfilerStudy.Avalonia.Timeline;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProfilerStudy.Avalonia.Timeline;

/// <summary>
/// Manages scope selection functionality - handles the draggable span on timeline
/// </summary>
public class ucChildSelectScope
{
    private AvaPlot _timelinePlot;
    private HorizontalSpan _scopeSpan = null!;
    private AxisSpanUnderMouse? _spanBeingDragged = null;
    
    // Scope selection variables
    private double _scopeStart = 0;
    private double _scopeEnd = 30;
    
    // Events
    public event Action<double, double>? ScopeChanged;

    // Events when scope range changed by ui
    public event Action? ScopeChangedByUi;
    
    public double ScopeStart => _scopeStart;
    public double ScopeEnd => _scopeEnd;
    
    public ucChildSelectScope(AvaPlot timelinePlot)
    {
        _timelinePlot = timelinePlot;
        SetupScopeSpan();
        SetupMouseInteraction();
    }
    
    /// <summary>
    /// Setup the draggable scope span
    /// </summary>
    private void SetupScopeSpan()
    {
        _scopeSpan = _timelinePlot.Plot.Add.HorizontalSpan(_scopeStart, _scopeEnd);
        ApplyTheme();
        _scopeSpan.LineWidth = 2;
        _scopeSpan.IsDraggable = true;
        _scopeSpan.IsResizable = true;
    }

    public void ApplyTheme()
    {
        if (_scopeSpan == null)
        {
            return;
        }

        _scopeSpan.FillColor = TimelineScopeThemeHelper.GetPlotColor(TimelineScopeThemeHelper.InfoSurfaceBrushKey, "#1F2B5F97");
        _scopeSpan.LineColor = TimelineScopeThemeHelper.GetPlotColor(TimelineScopeThemeHelper.InfoBorderBrushKey, "#FF5F9CE0");
    }
    
    /// <summary>
    /// Setup mouse interaction for scope dragging
    /// </summary>
    private void SetupMouseInteraction()
    {
        _timelinePlot.PointerPressed += OnTimelineMouseDown;
        _timelinePlot.PointerReleased += OnTimelineMouseUp;
        _timelinePlot.PointerMoved += OnTimelineMouseMove;
    }
    
    /// <summary>
    /// Update scope range programmatically
    /// </summary>
    public void UpdateXRange(double start, double end, bool triggerEvent = true)
    {
        _scopeStart = Math.Max(0, start);
        _scopeEnd = end;
        
        _scopeSpan.X1 = _scopeStart;
        _scopeSpan.X2 = _scopeEnd;
        
        if (triggerEvent)
        {
            ScopeChanged?.Invoke(_scopeStart, _scopeEnd);
        }
        _timelinePlot.Refresh();
    }

    public void MoveToNewEnd(double newEnd)
    {
        var scopeLength = _scopeEnd - _scopeStart;
        var newStart = newEnd - scopeLength;

        UpdateXRange(newStart, newEnd, true);
    }
    
    /// <summary>
    /// Reset scope to default range
    /// </summary>
    public void ResetScope()
    {
        UpdateXRange(0, 30);
    }
    
    /// <summary>
    /// Constrain scope to data bounds
    /// </summary>
    public void ConstrainToDataBounds(double xMin, double xMax)
    {
        bool changed = false;
        
        if (_scopeEnd > xMax)
        {
            _scopeEnd = xMax;
            changed = true;
        }
        if (_scopeStart < xMin)
        {
            _scopeStart = xMin;
            changed = true;
        }
        
        if (changed)
        {
            _scopeSpan.X1 = _scopeStart;
            _scopeSpan.X2 = _scopeEnd;
            
            _timelinePlot.Refresh();
        }
    }
    
    private void OnTimelineMouseDown(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(_timelinePlot);
        var spanUnderMouse = GetSpanUnderMouse((float)pos.X, (float)pos.Y);
        if (spanUnderMouse is not null)
        {
            _spanBeingDragged = spanUnderMouse;
            _timelinePlot.UserInputProcessor.Disable(); // disable panning while dragging
        }
    }
    
    private void OnTimelineMouseUp(object? sender, PointerEventArgs e)
    {
        if (_spanBeingDragged is not null)
        {
            // Update scope values from the span
            _scopeStart = Math.Max(0, _scopeSpan.X1);
            _scopeEnd = _scopeSpan.X2;
            
            ScopeChanged?.Invoke(_scopeStart, _scopeEnd);
            ScopeChangedByUi?.Invoke();
        }
        
        _spanBeingDragged = null;
        _timelinePlot.UserInputProcessor.Enable(); // enable panning
        _timelinePlot.Refresh();
    }
    
    private void OnTimelineMouseMove(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(_timelinePlot);
        if (_spanBeingDragged is not null)
        {
            // currently dragging something so update it
            Coordinates mouseNow = _timelinePlot.Plot.GetCoordinates(new Pixel(pos.X, pos.Y));
            _spanBeingDragged.DragTo(mouseNow);
            
            // Update scope values in real-time
            _scopeStart = Math.Max(0, _scopeSpan.X1);
            _scopeEnd = _scopeSpan.X2;
            
            ScopeChanged?.Invoke(_scopeStart, _scopeEnd);
            ScopeChangedByUi?.Invoke();

            _timelinePlot.Refresh();
        }
        else
        {
            // not dragging anything so just set the cursor based on what's under the mouse
            var spanUnderMouse = GetSpanUnderMouse((float)pos.X, (float)pos.Y);
            if (spanUnderMouse is null) 
                _timelinePlot.Cursor = new(StandardCursorType.Arrow);
            else if (spanUnderMouse.IsResizingHorizontally) 
                _timelinePlot.Cursor = new(StandardCursorType.SizeWestEast);
            else if (spanUnderMouse.IsResizingVertically) 
                _timelinePlot.Cursor = new(StandardCursorType.SizeNorthSouth);
            else if (spanUnderMouse.IsMoving) 
                _timelinePlot.Cursor = new(StandardCursorType.SizeAll);
        }
    }
    
    private AxisSpanUnderMouse? GetSpanUnderMouse(float x, float y)
    {
        CoordinateRect rect = _timelinePlot.Plot.GetCoordinateRect(x, y, radius: 10);

        foreach (AxisSpan span in _timelinePlot.Plot.GetPlottables<AxisSpan>().Reverse())
        {
            AxisSpanUnderMouse? spanUnderMouse = span.UnderMouse(rect);
            if (spanUnderMouse is not null)
                return spanUnderMouse;
        }

        return null;
    }
    
    /// <summary>
    /// Remove scope span from plot
    /// </summary>
    public void RemoveFromPlot()
    {
        if (_scopeSpan != null)
        {
            _timelinePlot.Plot.Remove(_scopeSpan);
        }
    }
    
    /// <summary>
    /// Recreate scope span (useful after plot clear)
    /// </summary>
    public void RecreateSpan()
    {
        RemoveFromPlot();
        SetupScopeSpan();
    }
}
