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
		TracyFileHeader header = Tracy010FileReader.ReadHeader(fullPath);
		ArrayList diagnostics = new ArrayList
		{
			new Dictionary<string, object>
			{
				["severity"] = "info",
				["code"] = "TracyHeaderLoaded",
				["message"] = "Tracy 0.10.0 LZ4 container header was validated."
			},
			new Dictionary<string, object>
			{
				["severity"] = "warning",
				["code"] = "TracyEventDecodingPending",
				["message"] = "CPU zones, frame marks, plots, and metadata decoding are not enabled in this implementation slice."
			}
		};
		TracyTraceQuerySession querySession = new TracyTraceQuerySession(fullPath, header, diagnostics);
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
