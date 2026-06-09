using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ProfilerStudy.Avalonia.Timeline;

namespace ProfilerStudy.Avalonia.ProfilerStats;

internal sealed class DetailPlotControlItem
{
	public string Title { get; set; } = string.Empty;

	public ucDetailPlotComponent PlotComponent { get; }

	public ToggleButton? UiItem { get; set; }

	public DetailPlotControlItem(string title, ucDetailPlotComponent plotComponent)
	{
		Title = title;
		PlotComponent = plotComponent;
	}

	public void ChangePlotVisible(bool value)
	{
		if (PlotComponent?.Border != null)
		{
			PlotComponent.Border.IsVisible = value;
		}
		if (UiItem != null)
		{
			UiItem.IsChecked = value;
		}
	}
}
