using System;

namespace ProfilerStudy.Tracy;

public sealed class TracyGpuZoneSummary
{
	public TracyGpuZoneSummary(byte context, ushort queryId, uint threadId, short sourceLocation, string name, long start, long end, string timeSource)
		: this(-1, context, queryId, threadId, sourceLocation, name, start, end, timeSource, 0, -1, string.Empty)
	{
	}

	public TracyGpuZoneSummary(int id, byte context, ushort queryId, uint threadId, short sourceLocation, string name, long start, long end, string timeSource, int depth, int parentId, string zoneKind)
	{
		Id = id;
		Context = context;
		QueryId = queryId;
		ThreadId = threadId;
		SourceLocation = sourceLocation;
		Name = string.IsNullOrWhiteSpace(name) ? "gpu-query:" + queryId : name;
		Start = start;
		End = end;
		TimeSource = string.IsNullOrWhiteSpace(timeSource) ? "cpu-submit-time" : timeSource;
		Depth = Math.Max(0, depth);
		ParentId = parentId;
		ZoneKind = string.IsNullOrWhiteSpace(zoneKind) ? ClassifyZoneKind(Name) : zoneKind;
	}

	public int Id { get; }

	public byte Context { get; }

	public ushort QueryId { get; }

	public uint ThreadId { get; }

	public short SourceLocation { get; }

	public string Name { get; }

	public long Start { get; }

	public long End { get; }

	public string TimeSource { get; }

	public int Depth { get; }

	public int ParentId { get; }

	public string ZoneKind { get; }

	public long Duration => Math.Max(0L, End - Start);

	private static string ClassifyZoneKind(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return "unknown";
		}
		string normalized = name.ToLowerInvariant();
		if (normalized.Contains("blit"))
		{
			return "blit";
		}
		if (normalized.Contains("draw"))
		{
			return "draw";
		}
		if (normalized.Contains("pass"))
		{
			return "pass";
		}
		return "marker";
	}
}
