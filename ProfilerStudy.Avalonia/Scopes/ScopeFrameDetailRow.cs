using System;

namespace ProfilerStudy.Avalonia;

public sealed class ScopeFrameDetailRow
{
	public ScopeFrameDetailRow(string threadName, int depth, string name, double startOffsetMs, double durationMs, string sourceText, string sourceFile, int sourceLine)
	{
		ThreadName = string.IsNullOrWhiteSpace(threadName) ? "(unnamed thread)" : threadName;
		Depth = Math.Max(0, depth);
		Name = string.IsNullOrWhiteSpace(name) ? "(unnamed scope)" : name;
		StartOffsetMs = startOffsetMs;
		DurationMs = durationMs;
		SourceText = sourceText ?? string.Empty;
		SourceFile = sourceFile ?? string.Empty;
		SourceLine = sourceLine;
	}

	public string ThreadName { get; }

	public int Depth { get; }

	public string Name { get; }

	public double StartOffsetMs { get; }

	public double DurationMs { get; }

	public string SourceText { get; }

	public string SourceFile { get; }

	public int SourceLine { get; }

	public bool CanOpenSource => !string.IsNullOrWhiteSpace(SourceFile);

	public string IndentedName => new string(' ', Depth * 2) + Name;

	public string StartOffsetText => FormatMs(StartOffsetMs);

	public string DurationText => FormatMs(DurationMs);

	private static string FormatMs(double value)
	{
		return value <= 0.0 ? "0" : Math.Round(value, 3).ToString("0.###");
	}
}
