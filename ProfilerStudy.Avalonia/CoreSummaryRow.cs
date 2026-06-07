namespace ProfilerStudy.Avalonia;

internal sealed class CoreSummaryRow
{
	public CoreSummaryRow(int coreIndex, int contextSwitchCount)
	{
		CoreIndex = coreIndex;
		ContextSwitchCount = contextSwitchCount;
	}

	public int CoreIndex { get; }

	public int ContextSwitchCount { get; }

	public string CoreText => "Core " + CoreIndex;
}
