using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProfilerStudy.Trace;

namespace ProfilerStudy.Tracy;

public static class TracyTraceImporter
{
	public static TraceDocument Load(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Tracy file path is required.", nameof(path));
		}

		string fullPath = Path.GetFullPath(path);
		TracyEventStream eventStream = Tracy010FileReader.Read(fullPath);
		ArrayList diagnostics = new ArrayList
		{
			new Dictionary<string, object>
			{
				["severity"] = "info",
				["code"] = "TracyHeaderLoaded",
				["message"] = "Tracy 0.10.0 LZ4 container header was validated.",
				["compressedBlockCount"] = eventStream.CompressedBlockCount,
				["compressedByteCount"] = eventStream.CompressedByteCount,
				["decodedByteCount"] = eventStream.DecodedByteCount,
				["payloadByteCount"] = eventStream.PayloadByteCount
			},
			new Dictionary<string, object>
			{
				["severity"] = "warning",
				["code"] = "TracyEventDecodingPending",
				["message"] = "CPU zones, frame marks, plots, and metadata decoding are not enabled in this implementation slice."
			}
		};
		TracyTraceQuerySession querySession = new TracyTraceQuerySession(fullPath, eventStream, diagnostics);
		return new TraceDocument(
			Guid.NewGuid().ToString("N"),
			fullPath,
			"tracy",
			Path.GetFileName(fullPath),
			DateTime.UtcNow,
			querySession,
			diagnostics);
	}
}
