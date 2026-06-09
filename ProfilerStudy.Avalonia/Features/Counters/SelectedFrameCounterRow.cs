using System;

namespace ProfilerStudy.Avalonia;

internal sealed class SelectedFrameCounterRow
{
	public SelectedFrameCounterRow(string graphName, string name, double value, double count, string unit)
	{
		GraphName = string.IsNullOrWhiteSpace(graphName) ? "default" : graphName;
		Name = string.IsNullOrWhiteSpace(name) ? "(unnamed counter)" : name;
		Value = value;
		Count = count;
		Unit = unit ?? string.Empty;
	}

	public string GraphName { get; }

	public string Name { get; }

	public double Value { get; }

	public double Count { get; }

	public string Unit { get; }

	public string ValueText => FormatNumber(Value);

	public string CountText => Count <= 0.0 ? "-" : FormatNumber(Count);

	public string UnitText => string.IsNullOrWhiteSpace(Unit) ? "-" : Unit;

	private static string FormatNumber(double value)
	{
		return Math.Abs(value) >= 1000.0 ? value.ToString("N0") : Math.Round(value, 3).ToString("0.###");
	}
}
