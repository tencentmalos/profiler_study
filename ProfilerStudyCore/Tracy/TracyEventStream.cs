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
		int threadCount,
		ArrayList diagnostics)
	{
		Header = header;
		CompressedBlockCount = compressedBlockCount;
		CompressedByteCount = compressedByteCount;
		DecodedByteCount = decodedByteCount;
		PayloadByteCount = payloadByteCount;
		Metadata = metadata;
		CpuZones = cpuZones;
		ThreadCount = threadCount;
		Diagnostics = diagnostics;
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

	public int ThreadCount { get; }

	public ArrayList Diagnostics { get; }
}
