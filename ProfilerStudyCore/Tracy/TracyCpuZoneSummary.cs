namespace ProfilerStudy.Tracy;

public sealed class TracyCpuZoneSummary
{
	public TracyCpuZoneSummary(ulong threadId, short sourceLocation, string name, long start, long end)
	{
		ThreadId = threadId;
		SourceLocation = sourceLocation;
		Name = name;
		Start = start;
		End = end;
	}

	public ulong ThreadId { get; }

	public short SourceLocation { get; }

	public string Name { get; }

	public long Start { get; }

	public long End { get; }

	public long Duration => End >= Start ? End - Start : 0L;
}
