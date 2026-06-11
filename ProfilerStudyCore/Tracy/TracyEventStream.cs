using System.Collections;
using System.Collections.Generic;

namespace ProfilerStudy.Tracy;

public sealed class TracyEventStream
{
	public TracyEventStream(
		TracyFileHeader header,
		int compressedBlockCount,
		long compressedByteCount,
		long decodedByteCount,
		long payloadByteCount,
		TracyTraceMetadata metadata,
		IReadOnlyList<TracyCpuZoneSummary> cpuZones,
		IReadOnlyList<TracyPlotSummary> plots,
		IReadOnlyList<TracyThreadSummary> threads,
		int threadCount,
		ArrayList diagnostics,
		IReadOnlyList<TracyGpuContextSummary> gpuContexts = null,
		IReadOnlyList<TracyGpuZoneSummary> gpuZones = null,
		IReadOnlyList<TracySourceLocationSummary> sourceLocations = null)
	{
		Header = header;
		CompressedBlockCount = compressedBlockCount;
		CompressedByteCount = compressedByteCount;
		DecodedByteCount = decodedByteCount;
		PayloadByteCount = payloadByteCount;
		Metadata = metadata;
		CpuZones = cpuZones;
		Plots = plots;
		Threads = threads;
		ThreadCount = threadCount;
		Diagnostics = diagnostics;
		GpuContexts = gpuContexts ?? new List<TracyGpuContextSummary>();
		GpuZones = gpuZones ?? new List<TracyGpuZoneSummary>();
		SourceLocations = sourceLocations ?? new List<TracySourceLocationSummary>();
	}

	public TracyFileHeader Header { get; }

	public string Version => Header.Version;

	public string Compression => Header.Compression;

	public int CompressedBlockCount { get; }

	public long CompressedByteCount { get; }

	public long DecodedByteCount { get; }

	public long PayloadByteCount { get; }

	public TracyTraceMetadata Metadata { get; }

	public bool HasMetadata => Metadata != null;

	public IReadOnlyList<TracyCpuZoneSummary> CpuZones { get; }

	public IReadOnlyList<TracyPlotSummary> Plots { get; }

	public IReadOnlyList<TracyThreadSummary> Threads { get; }

	public int ThreadCount { get; }

	public ArrayList Diagnostics { get; }

	public IReadOnlyList<TracyGpuContextSummary> GpuContexts { get; }

	public IReadOnlyList<TracyGpuZoneSummary> GpuZones { get; }

	public IReadOnlyList<TracySourceLocationSummary> SourceLocations { get; }
}
