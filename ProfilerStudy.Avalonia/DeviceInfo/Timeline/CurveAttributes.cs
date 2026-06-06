using System;

namespace ProfilerStudy.Avalonia.DeviceInfo.Timeline;

/// <summary>
/// Marks a property as a curve field in a timeline plot.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CurveFieldAttribute : Attribute
{
	public CurveFieldAttribute(string label, string unit, int colorIndex)
	{
		CurveLabel = label;
		CurveUnit = unit;
		LineColorIndex = colorIndex;
	}

	public string CurveLabel { get; set; }

	public string CurveUnit { get; set; }

	public int LineColorIndex { get; set; }
}

/// <summary>
/// Marks a property as a timeline plot container.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PlotContainerAttribute : Attribute
{
	public PlotContainerAttribute(string title, bool isMainPlot = false)
	{
		PlotTitle = title;
		IsMainPlot = isMainPlot;
	}

	public string PlotTitle { get; set; }

	public bool IsMainPlot { get; set; }
}

/// <summary>
/// Marks a property as ignored in timeline metadata generation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CurveIgnoreFieldAttribute : Attribute
{
}
