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
/// Manages all detail views in a unified way
/// </summary>
public class ucChildDetails
{
    private List<ucDetailPlotComponent> _detailPlots = new List<ucDetailPlotComponent>();
    private StackPanel _container;
    private AvaPlot _sharedXAxisPlot;
    
    public List<ucDetailPlotComponent> AllDetailPlots => _detailPlots;

    public ucDetailPlotComponent this[int index]
    {
        get
        {
            if (index < 0 || index >= _detailPlots.Count) throw new ArgumentOutOfRangeException("index");

            return _detailPlots[index];
        }
    }

	public void ClearAll()
	{
		foreach (ucDetailPlotComponent plot in _detailPlots)
		{
			if (plot.Border != null)
			{
				_container.Children.Remove(plot.Border);
			}
		}

		_detailPlots.Clear();
	}

    public ucChildDetails(StackPanel container, AvaPlot sharedXAxisPlot)
    {
        _container = container;
        _sharedXAxisPlot = sharedXAxisPlot;
    }
    
    public ucDetailPlotComponent AddCurvePlot(DetailPlotConfig config)
    {
        var detailPlot = CreateDetailPlot(config, config.DetailPlotType);
        _detailPlots.Add(detailPlot);
        if (detailPlot.Border != null)
        {
            _container.Children.Add(detailPlot.Border);
        }

        //detailPlot.LinkToSharedAxis(_sharedXAxisPlot);
        return detailPlot;
    }
    
    public ucDetailPlotComponent AddFlamePlot(string title = "Flame Graph - Execution Stack")
    {
        var flameGraphConfig = new DetailPlotConfig
        {
            Title = title,
            AllCurves = new List<CurveConfig>()
        };
        
        var detailPlot = CreateDetailPlot(flameGraphConfig, ucDetailPlotComponent.DetailPlotType.FlameGraph);
        _detailPlots.Add(detailPlot);
        if (detailPlot.Border != null)
        {
            _container.Children.Add(detailPlot.Border);
        }

        //detailPlot.LinkToSharedAxis(_sharedXAxisPlot);

        return detailPlot;
    }
    
    public void RemovePlot(int index)
    {
        if (index < 0 || index >= _detailPlots.Count) return;
        
        var detailImage = _detailPlots[index];
        if (detailImage.Border != null)
        {
            _container.Children.Remove(detailImage.Border);
        }
        _detailPlots.RemoveAt(index);
    }
    
    public void UpdateXRange(double xMin, double xMax, bool isAutoScaleY)
    {
        foreach (var detailPlot in _detailPlots)
        {
            detailPlot.UpdateXRange(xMin, xMax, isAutoScaleY);
        }
    }
    
    private ucDetailPlotComponent CreateDetailPlot(DetailPlotConfig config, ucDetailPlotComponent.DetailPlotType plotType)
    {
        var plot = new AvaPlot();
        
        var border = ucDetailPlotComponent.CreateDetailPlotBorder(config.Title, plot, _detailPlots.Count == 0);
        
        // Create DetailImage instance
        var detailPlot = new ucDetailPlotComponent(config, plot, plotType, border);
        
        // Set fixed layout
        detailPlot.SetFixedLayout(ucDetailPlotComponent.GetStandardFixedPadding());
        
        // Configure user input
        detailPlot.UpdateXLimits();

        return detailPlot;
    }
    
}
