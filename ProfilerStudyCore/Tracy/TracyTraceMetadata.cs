using System.Collections.Generic;
using System.Linq;

namespace ProfilerStudy.Tracy;

public sealed class TracyTraceMetadata
{
	public TracyTraceMetadata(
		long delay,
		long resolution,
		double timerMultiplier,
		long lastTime,
		long frameOffset,
		ulong processId,
		long samplingPeriod,
		byte cpuArchitecture,
		uint cpuId,
		string cpuManufacturer,
		bool onDemand,
		string captureName,
		string captureProgram,
		long captureTime,
		long executableTime,
		string hostInfo,
		IReadOnlyList<TracyFrameSetSummary> frameSets,
		int stringCount,
		int threadNameCount)
	{
		Delay = delay;
		Resolution = resolution;
		TimerMultiplier = timerMultiplier;
		LastTime = lastTime;
		FrameOffset = frameOffset;
		ProcessId = processId;
		SamplingPeriod = samplingPeriod;
		CpuArchitecture = cpuArchitecture;
		CpuId = cpuId;
		CpuManufacturer = cpuManufacturer;
		OnDemand = onDemand;
		CaptureName = captureName;
		CaptureProgram = captureProgram;
		CaptureTime = captureTime;
		ExecutableTime = executableTime;
		HostInfo = hostInfo;
		FrameSets = frameSets;
		StringCount = stringCount;
		ThreadNameCount = threadNameCount;
	}

	public long Delay { get; }

	public long Resolution { get; }

	public double TimerMultiplier { get; }

	public long LastTime { get; }

	public long FrameOffset { get; }

	public ulong ProcessId { get; }

	public long SamplingPeriod { get; }

	public byte CpuArchitecture { get; }

	public uint CpuId { get; }

	public string CpuManufacturer { get; }

	public bool OnDemand { get; }

	public string CaptureName { get; }

	public string CaptureProgram { get; }

	public long CaptureTime { get; }

	public long ExecutableTime { get; }

	public string HostInfo { get; }

	public IReadOnlyList<TracyFrameSetSummary> FrameSets { get; }

	public int FrameSetCount => FrameSets.Count;

	public int FrameCount => FrameSets.Sum(frameSet => frameSet.FrameCount);

	public int StringCount { get; }

	public int ThreadNameCount { get; }
}
