using System.Collections.Generic;
using ProfilerStudy.Avalonia.Timeline;

namespace ProfilerStudy.Avalonia.ProfilerStats.Timeline;

public sealed class CurveUiPlotBridgeItem
{
	public CurvePlotMetadata Metadata { get; set; } = null!;

	public ucDetailPlotComponent UiComponent { get; set; } = null!;

	public List<Curve2DGenerator.OneCurve> CurveList { get; private set; } = new List<Curve2DGenerator.OneCurve>();

	public Dictionary<string, Curve2DGenerator.OneCurve> CurveDictionary { get; private set; } = new Dictionary<string, Curve2DGenerator.OneCurve>();
}
