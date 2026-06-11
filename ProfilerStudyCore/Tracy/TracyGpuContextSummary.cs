namespace ProfilerStudy.Tracy;

public sealed class TracyGpuContextSummary
{
	public TracyGpuContextSummary(byte context, string name, uint threadId, float period, byte type, byte flags, long cpuTime, long gpuTime)
	{
		Context = context;
		Name = name ?? string.Empty;
		ThreadId = threadId;
		Period = period;
		Type = type;
		Flags = flags;
		CpuTime = cpuTime;
		GpuTime = gpuTime;
	}

	public byte Context { get; }

	public string Name { get; }

	public uint ThreadId { get; }

	public float Period { get; }

	public byte Type { get; }

	public byte Flags { get; }

	public long CpuTime { get; }

	public long GpuTime { get; }
}
