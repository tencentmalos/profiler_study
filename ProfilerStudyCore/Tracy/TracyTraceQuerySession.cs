using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProfilerStudy.Trace;

namespace ProfilerStudy.Tracy;

public sealed class TracyTraceQuerySession : ITraceQuerySession
{
	private readonly TracyFileHeader m_Header;
	private readonly ArrayList m_Diagnostics;

	public TracyTraceQuerySession(string sourcePath, TracyFileHeader header, ArrayList diagnostics)
	{
		SourcePath = sourcePath;
		DisplayName = Path.GetFileName(sourcePath);
		m_Header = header;
		m_Diagnostics = diagnostics;
	}

	public string SourceFormat => "tracy";

	public string SourcePath { get; }

	public string DisplayName { get; }

	public Dictionary<string, object> GetSummary(int top)
	{
		return new Dictionary<string, object>
		{
			["sourceFormat"] = "tracy",
			["sourceFile"] = SourcePath,
			["tracyVersion"] = m_Header.Version,
			["compression"] = m_Header.Compression,
			["summary"] = new Dictionary<string, object>
			{
				["sourceFormat"] = "tracy",
				["tracyVersion"] = m_Header.Version,
				["compression"] = m_Header.Compression,
				["threadCount"] = 0,
				["frameCount"] = 0,
				["zoneCount"] = 0,
				["plotCount"] = 0,
				["eventsDecoded"] = false,
				["framesUnavailable"] = true,
				["frameSource"] = "none",
				["status"] = "header-loaded"
			},
			["threads"] = new ArrayList(),
			["slowFrames"] = new ArrayList(),
			["scopeHotspots"] = new ArrayList(),
			["customStats"] = new ArrayList(),
			["diagnostics"] = m_Diagnostics,
			["capabilities"] = new Dictionary<string, object>
			{
				["summary"] = true,
				["threads"] = false,
				["frames"] = false,
				["scopeHotspots"] = false,
				["counters"] = false,
				["timeRange"] = false,
				["profilerOverhead"] = false
			},
			["top"] = Math.Max(1, top)
		};
	}
}
