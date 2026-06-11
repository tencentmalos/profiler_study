namespace ProfilerStudy.Tracy;

public sealed class TracyThreadSummary
{
	public TracyThreadSummary(ulong threadId, string name)
	{
		ThreadId = threadId;
		Name = name ?? string.Empty;
	}

	public ulong ThreadId { get; }

	public string Name { get; }
}
