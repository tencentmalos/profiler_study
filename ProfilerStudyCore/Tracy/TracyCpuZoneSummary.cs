using System;

namespace ProfilerStudy.Tracy;

public sealed class TracyCpuZoneSummary
{
	public TracyCpuZoneSummary(ulong threadId, short sourceLocation, string name, long start, long end)
		: this(threadId, sourceLocation, name, start, end, 0, -1L)
	{
	}

	public TracyCpuZoneSummary(ulong threadId, short sourceLocation, string name, long start, long end, int depth, long selfDuration)
	{
		ThreadId = threadId;
		SourceLocation = sourceLocation;
		Name = name;
		Start = start;
		End = end;
		Depth = Math.Max(0, depth);
		long duration = end >= start ? end - start : 0L;
		SelfDuration = selfDuration >= 0L ? Math.Max(0L, Math.Min(selfDuration, duration)) : duration;
	}

	public ulong ThreadId { get; }

	public short SourceLocation { get; }

	public string Name { get; }

	public long Start { get; }

	public long End { get; }

	public int Depth { get; }

	public long SelfDuration { get; }

	public long Duration => End >= Start ? End - Start : 0L;
}
