using ScottPlot;
using System.Collections.Generic;
using System.Reflection;

namespace ProfilerStudy.Avalonia.ProfilerStats.Timeline;

public sealed class CurveFieldMetadata
{
	public string PropertyName { get; set; } = string.Empty;

	public string CurveLabel { get; set; } = string.Empty;

	public string CurveUnit { get; set; } = string.Empty;

	public Color LineColor { get; set; }

	public PropertyInfo? FieldProperty { get; set; }
}

public sealed class CurvePlotMetadata
{
	public string PropertyName { get; set; } = string.Empty;

	public string PlotTitle { get; set; } = string.Empty;

	public bool IsMainPlot { get; set; }

	public PropertyInfo? ContainerProperty { get; set; }

	public List<CurveFieldMetadata> SubFields { get; private set; } = new List<CurveFieldMetadata>();

	public Dictionary<string, CurveFieldMetadata> FieldDictionary { get; private set; } = new Dictionary<string, CurveFieldMetadata>();
}

public sealed class DiagramMetadata
{
	public Dictionary<string, CurvePlotMetadata> PlotDictionary { get; private set; } = new Dictionary<string, CurvePlotMetadata>();

	public List<CurvePlotMetadata> PlotList { get; private set; } = new List<CurvePlotMetadata>();
}
