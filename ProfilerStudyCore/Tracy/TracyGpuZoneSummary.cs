using System;

namespace ProfilerStudy.Tracy;

public sealed class TracyGpuZoneSummary
{
	public TracyGpuZoneSummary(byte context, ushort queryId, uint threadId, short sourceLocation, string name, long start, long end, string timeSource)
	{
		Context = context;
		QueryId = queryId;
		ThreadId = threadId;
		SourceLocation = sourceLocation;
		Name = string.IsNullOrWhiteSpace(name) ? "gpu-query:" + queryId : name;
		Start = start;
		End = end;
		TimeSource = string.IsNullOrWhiteSpace(timeSource) ? "cpu-submit-time" : timeSource;
	}

	public byte Context { get; }

	public ushort QueryId { get; }

	public uint ThreadId { get; }

	public short SourceLocation { get; }

	public string Name { get; }

	public long Start { get; }

	public long End { get; }

	public string TimeSource { get; }

	public long Duration => Math.Max(0L, End - Start);
}
