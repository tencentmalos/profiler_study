using System.Collections;

namespace ProfilerStudy.Tracy;

public sealed class TracyEventStream
{
	public TracyEventStream(
		TracyFileHeader header,
		int compressedBlockCount,
		long compressedByteCount,
		long decodedByteCount,
		long payloadByteCount,
		ArrayList diagnostics)
	{
		Header = header;
		CompressedBlockCount = compressedBlockCount;
		CompressedByteCount = compressedByteCount;
		DecodedByteCount = decodedByteCount;
		PayloadByteCount = payloadByteCount;
		Diagnostics = diagnostics;
	}

	public TracyFileHeader Header { get; }

	public string Version => Header.Version;

	public string Compression => Header.Compression;

	public int CompressedBlockCount { get; }

	public long CompressedByteCount { get; }

	public long DecodedByteCount { get; }

	public long PayloadByteCount { get; }

	public ArrayList Diagnostics { get; }
}
