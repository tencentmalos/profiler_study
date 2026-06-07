using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ProfilerStudy.Avalonia.Timeline;

namespace ProfilerStudy.Avalonia.Timeline;

/// <summary>
/// 2D Value Detail View - Handles scatter plotting with custom X,Y coordinates
/// </summary>
public class Curve2DGenerator : ICurveGenerator
{
    public class OneCurve
    {
        public List<double> XDatas = new List<double>();
        public List<double> YDatas = new List<double>();

        public Scatter?     Scatter;
        public Crosshair?   Crosshair;


        public OneCurve(Curve2DGenerator parentGenerate)
        {
        }

        public void AppendData(double x, double y)
        {
            XDatas.Add(x);
            YDatas.Add(y);
        }

        public void SetData(double[] xData, double[] yData)
        {
            XDatas.Clear();
            XDatas.AddRange(xData);

            YDatas.Clear();
            YDatas.AddRange(yData);
        }
    }


    private AvaPlot                 _plot;
    
    private DetailPlotConfig        _config;

    private List<OneCurve>          _curveList = new List<OneCurve>();
    private int                     _lastHoverSeriesIndex = -1;
    private double                  _lastHoverX = double.NaN;
    private double                  _lastHoverY = double.NaN;

    //Do not use the tooltip builtin(can clip by plot view)
    ////private Tooltip                 _tooltip;

    public DetailPlotConfig     Config => _config;
    public AvaPlot              Plot => _plot;
    public int                  CurveCount => _curveList.Count;

    public PlotHoverResult? MouseHoverResult { get; private set; }

    public OneCurve this[int index]
    {
        get
        {
            if(index < 0 || index >= _curveList.Count) throw new ArgumentOutOfRangeException("index");

            return _curveList[index];
        }
    }

    public Curve2DGenerator(AvaPlot targetPlot, DetailPlotConfig configuration)
    {
        _plot = targetPlot;
        _config = configuration;

        SetupCurves();
    }

    /// <summary>
    /// Setup the plot with scatters and interactive elements
    /// </summary>
    private void SetupCurves()
    {
        _plot.Plot.Clear();
        _curveList.Clear();

        // Create scatters for each data series
        for (int i = 0; i < _config.AllCurves.Count; i++)
        {
            var seriesConfig = _config.AllCurves[i];
            var curve = new OneCurve(this);
            
            // Create scatter (will be populated when GenerateData is called)
            var scatter = _plot.Plot.Add.Scatter(new double[0], new double[0]);
            scatter.Color = seriesConfig.LineColor;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 0; // No markers, just lines
            scatter.LegendText = seriesConfig.Label;
            curve.Scatter = scatter;

            // Create crosshair for mouse tracking
            var crosshair = _plot.Plot.Add.Crosshair(0, 0);
            crosshair.IsVisible = false;
            crosshair.MarkerShape = MarkerShape.OpenCircle;
            crosshair.MarkerSize = 8;
            crosshair.LineColor = seriesConfig.LineColor;
            crosshair.MarkerColor = seriesConfig.LineColor;
            crosshair.LineWidth = 1;
            crosshair.LinePattern = LinePattern.Dashed;
            curve.Crosshair = crosshair;

            _curveList.Add(curve);
        }

        ////// Create tooltip
        ////_tooltip = _plot.Plot.Add.Tooltip(new Coordinates(0, 0), "", new Coordinates(0, 0));
        ////_tooltip.IsVisible = false;
        ////_tooltip.FillColor = Color.FromHex("#ffffcc");
        ////_tooltip.LineColor = Colors.Black;
        ////_tooltip.LineWidth = 1;
        ////_tooltip.LabelFontSize = 10;
        ////_tooltip.LabelFontColor = Colors.Black;

        // Setup legend
        _plot.Plot.ShowLegend();
        _plot.Plot.Legend.Alignment = Alignment.UpperRight;
        TimelineScopeThemeHelper.ApplyPlotTheme(_plot);

        // Hide X-axis (controlled by shared axis)
        _plot.Plot.Axes.Bottom.IsVisible = false;

        // Setup mouse interaction
        _plot.PointerMoved += OnMouseMove;
        _plot.PointerExited += OnMouseExit;
    }

    /// <summary>
    /// Update the plot with new data
    /// </summary>
    public void UpdatePlot()
    {
        // Remove old scatters and create new ones with updated data
        foreach (var curve in _curveList)
        {
            if (curve.Scatter != null)
            {
                _plot.Plot.Remove(curve.Scatter);
                curve.Scatter = null;
            }
        }

        // Create new scatters with updated data
        for (int i = 0; i < Math.Min(_config.AllCurves.Count, _curveList.Count); i++)
        {
            var curve = _curveList[i];
            var seriesConfig = _config.AllCurves[i];
            
            
            var scatter = _plot.Plot.Add.Scatter(curve.XDatas, curve.YDatas);
            scatter.Color = seriesConfig.LineColor;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 0; // No markers, just lines
            scatter.LegendText = seriesConfig.Label;


            curve.Scatter = scatter;
        }
        
        _plot.Refresh();
    }

    /// <summary>
    /// Set fixed layout padding
    /// </summary>
    public void SetFixedLayout(PixelPadding padding)
    {
        _plot.Plot.Layout.Fixed(padding);
    }


    private void ResetXLimitRule(double xMin, double xMax)
    {
        // Find and update existing LockedHorizontal rule
        var existingRule = _plot.Plot.Axes.Rules.OfType<ScottPlot.AxisRules.LockedHorizontal>().FirstOrDefault();
        if (existingRule != null)
        {
            // Remove the old rule safely
            var rulesList = _plot.Plot.Axes.Rules.ToList();
            rulesList.Remove(existingRule);
            _plot.Plot.Axes.Rules.Clear();

            // Add updated rule
            var newRule = new ScottPlot.AxisRules.LockedHorizontal(
                _plot.Plot.Axes.Bottom,
                xMin,
                xMax);

            // Add all rules back
            foreach (var rule in rulesList)
            {
                _plot.Plot.Axes.Rules.Add(rule);
            }
            _plot.Plot.Axes.Rules.Add(newRule);
        }
        else
        {
            // Add new rule if none exists
            var lockedHorizontalRule = new ScottPlot.AxisRules.LockedHorizontal(
                _plot.Plot.Axes.Bottom,
                xMin,
                xMax);
            _plot.Plot.Axes.Rules.Add(lockedHorizontalRule);
        }
    }

    /// <summary>
    /// Update X-axis range
    /// </summary>
    public void UpdateXRange(double xMin, double xMax, bool isAutoScaleY, bool isXAsixLimit)
    {
        // Use a safer approach that doesn't modify rules during rendering
        _plot.Plot.Axes.SetLimitsX(xMin, xMax);
        //Update y range here
        if (isAutoScaleY)
        {
            _plot.Plot.Axes.AutoScaleY();
        }

        if (isXAsixLimit)
        {
            ResetXLimitRule(xMin, xMax);
        }

        _plot.Refresh();
    }

    /// <summary>
    /// Handle mouse move event for data point tracking
    /// </summary>
    internal void OnMouseMove(object? sender, PointerEventArgs e)
    {
        // Get mouse position and convert to coordinates
        var pos = e.GetPosition(_plot);
        Pixel mousePixel = new(pos.X, pos.Y);
        Coordinates mouseLocation = _plot.Plot.GetCoordinates(mousePixel);

        if (!mouseLocation.AreReal)
        {
            //Not valid value here, just return here 
            return;
        }

       

        // Find the nearest data points for all series
        var nearestPoints = new List<(DataPoint point, string seriesLabel, Color seriesColor, int seriesIndex)>();
        double maxDistance = 15; // Maximum distance to consider a point "near"

        for (int seriesIndex = 0; seriesIndex < _curveList.Count; seriesIndex++)
        {
            var scatter = _curveList[seriesIndex].Scatter;
            var seriesConfig = _config.AllCurves[seriesIndex];
            
            if (scatter != null)
            {
                var nearest = scatter.GetNearestX(mouseLocation, _plot.Plot.LastRender, (float)maxDistance);
                
                if (nearest.IsReal)
                {
                    nearestPoints.Add((nearest, seriesConfig.Label, seriesConfig.LineColor, seriesIndex));
                }
            }
        }

        var hoverResult = new PlotHoverResult { };

        if (nearestPoints.Count > 0)
        {
            double nearestCurveY = double.MaxValue;
            CurvePointHint? nearestHintItem = null;
            OneCurve? nearestCurve = null;
            int nearestSeriesIndex = -1;
            DataPoint minYNearestPoint = new DataPoint();
            Color nearestColor = new Color();

            // Show crosshairs for all nearby points
            for (int i = 0; i < _curveList.Count; i++)
            {
                var crosshair = _curveList[i].Crosshair;
                var nearestPoint = nearestPoints.FirstOrDefault(p => p.seriesIndex == i);
                var curveCfg = _config.AllCurves[i];

                if (crosshair != null)
                {
                    crosshair.IsVisible = false;
                }

                if (nearestPoint.point.IsReal && crosshair != null)
                {
                    var hintItem = new CurvePointHint { Index = i, Title = curveCfg.Label, Unit = curveCfg.UnitName, Value = nearestPoint.point.Y };
                    hoverResult.HintItems.Add(hintItem);

                    double currentYLen = Math.Abs(nearestPoint.point.Coordinates.Y - mouseLocation.Y);
                    if(currentYLen < nearestCurveY)
                    {
                        nearestCurveY = currentYLen;
                        nearestHintItem = hintItem;
                        nearestCurve = _curveList[i];
                        nearestSeriesIndex = i;
                        minYNearestPoint = nearestPoint.point;
                        nearestColor = nearestPoint.seriesColor;
                    }
                }
            }

            if (nearestSeriesIndex == _lastHoverSeriesIndex &&
                Math.Abs(minYNearestPoint.X - _lastHoverX) < double.Epsilon &&
                Math.Abs(minYNearestPoint.Y - _lastHoverY) < double.Epsilon)
            {
                return;
            }

            if (nearestCurve?.Crosshair != null)
            {
                var crosshair = nearestCurve.Crosshair;
                crosshair.Position = minYNearestPoint.Coordinates;
                crosshair.MarkerColor = nearestColor;
                crosshair.IsVisible = true;
            }
            

            ////// Find the closest point among all series for tooltip positioning
            ////var closestPoint = nearestPoints.OrderBy(p => Math.Abs(p.point.X - mouseLocation.X)).First();

            ////// Build tooltip text with all nearby points
            ////var tooltipText = new StringBuilder();
            ////tooltipText.AppendLine($"X: {closestPoint.point.X:F2}");
            ////tooltipText.AppendLine();

            ////foreach (var (point, label, color, _) in nearestPoints.OrderBy(p => p.seriesLabel))
            ////{
            ////    tooltipText.AppendLine($"{label}: {point.Y:F3}");
            ////}

            ////// Position tooltip near the mouse but offset to avoid covering data
            ////var tooltipPosition = new Coordinates(
            ////    closestPoint.point.X + (mouseLocation.X > closestPoint.point.X ? 5 : -5),
            ////    closestPoint.point.Y + 0.1 * (_plot.Plot.Axes.GetLimits().Top - _plot.Plot.Axes.GetLimits().Bottom)
            ////);

            ////_tooltip.LabelText = tooltipText.ToString().Trim();
            ////_tooltip.TipLocation = closestPoint.point.Coordinates;
            ////_tooltip.LabelLocation = tooltipPosition;
            ////_tooltip.IsVisible = true;

            _plot.Refresh();

            _lastHoverSeriesIndex = nearestSeriesIndex;
            _lastHoverX = minYNearestPoint.X;
            _lastHoverY = minYNearestPoint.Y;
            hoverResult.NearestHintItem = nearestHintItem;
            MouseHoverResult = hoverResult;
        }
        else
        {
            // Hide all crosshairs and tooltip when no point is near
            HideInteractiveElements();
        }
    }

    /// <summary>
    /// Handle mouse exit event
    /// </summary>
    private void OnMouseExit(object? sender, PointerEventArgs e)
    {
        HideInteractiveElements();
    }

    /// <summary>
    /// Hide all interactive elements (crosshairs and tooltip)
    /// </summary>
    public void HideInteractiveElements()
    {
        bool needsRefresh = false;
        
        foreach (var curve in _curveList)
        {
            var crosshair = curve.Crosshair;

            if (crosshair?.IsVisible == true)
            {
                crosshair.IsVisible = false;
                needsRefresh = true;
            }
        }
        
        ////if (_tooltip.IsVisible)
        ////{
        ////    _tooltip.IsVisible = false;
        ////    needsRefresh = true;
        ////}
        
        if (needsRefresh)
        {
            _plot.Refresh();
        }

        _lastHoverSeriesIndex = -1;
        _lastHoverX = double.NaN;
        _lastHoverY = double.NaN;
        MouseHoverResult = null;
    }

    public (double, double) GetDataSourceXRange()
    {
        if (_curveList.Count > 0 && _curveList[0].XDatas.Count > 0)
        {
            var curve = _curveList[0];
            return (curve.XDatas.FirstOrDefault(), curve.XDatas.LastOrDefault());
        }
        else
        {
            return (0, 0);
        }
    }
}
