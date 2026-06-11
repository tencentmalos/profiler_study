using System.Collections.Generic;

namespace ProfilerStudy.Tracy;

public sealed class TracyPlotSummary
{
	public TracyPlotSummary(string name, byte type, byte format, double min, double max, double sum, IReadOnlyList<TracyPlotSample> samples)
	{
		Name = name;
		Type = type;
		Format = format;
		Min = min;
		Max = max;
		Sum = sum;
		Samples = samples;
	}

	public string Name { get; }

	public byte Type { get; }

	public byte Format { get; }

	public double Min { get; }

	public double Max { get; }

	public double Sum { get; }

	public IReadOnlyList<TracyPlotSample> Samples { get; }
}
