using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProfilerStudy.Trace;

namespace ProfilerStudy.Tracy;

public sealed class TracyTraceQuerySession : ITraceQuerySession
{
	private readonly TracyFileHeader m_Header;
	private readonly TracyEventStream m_EventStream;
	private readonly ArrayList m_Diagnostics;

	public TracyTraceQuerySession(string sourcePath, TracyEventStream eventStream, ArrayList diagnostics)
	{
		SourcePath = sourcePath;
		DisplayName = Path.GetFileName(sourcePath);
		m_EventStream = eventStream;
		m_Header = eventStream.Header;
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
				["compressedBlockCount"] = m_EventStream.CompressedBlockCount,
				["compressedByteCount"] = m_EventStream.CompressedByteCount,
				["decodedByteCount"] = m_EventStream.DecodedByteCount,
				["payloadByteCount"] = m_EventStream.PayloadByteCount,
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

	public Dictionary<string, object> FindSlowFrames(int top, double thresholdMs)
	{
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["thresholdMs"] = Round(Math.Max(0.0, thresholdMs)),
			["framesUnavailable"] = true,
			["frameSource"] = "none",
			["eventsDecoded"] = false,
			["slowFrames"] = new ArrayList(),
			["slowFramePattern"] = new Dictionary<string, object>
			{
				["hasPeriodicSlowFrames"] = false,
				["reason"] = "Tracy frame marks have not been decoded yet."
			},
			["diagnostics"] = Diagnostics("TracyFrameMarksPending", "Frame mark decoding is required before slow frame analysis can return data."),
			["top"] = Math.Max(1, top)
		};
	}

	public Dictionary<string, object> FindScopeHotspots(int top, int startFrame, int endFrame)
	{
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["eventsDecoded"] = false,
			["scopeHotspots"] = new ArrayList(),
			["range"] = BuildFrameRange(startFrame, endFrame),
			["diagnostics"] = Diagnostics("TracyZonesPending", "CPU zone decoding is required before scope hotspots can return data."),
			["top"] = Math.Max(1, top)
		};
	}

	public Dictionary<string, object> GetProfilerOverhead(int startFrame, int endFrame, int top)
	{
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["supported"] = false,
			["capability"] = "profilerOverhead",
			["range"] = BuildFrameRange(startFrame, endFrame),
			["diagnostics"] = Diagnostics("TracyProfilerOverheadUnsupported", "Profiler overhead is specific to ProfilerStudy target-side instrumentation and is not reported by Tracy files."),
			["top"] = Math.Max(1, top)
		};
	}

	public Dictionary<string, object> ListCounters(int top, string filter)
	{
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["filter"] = filter ?? string.Empty,
			["eventsDecoded"] = false,
			["counters"] = new ArrayList(),
			["diagnostics"] = Diagnostics("TracyPlotsPending", "Plot decoding is required before Tracy counters can return data."),
			["top"] = Math.Max(1, top)
		};
	}

	public Dictionary<string, object> QueryCounterSamples(string counterName, int startFrame, int endFrame, bool accumulated, int maxSamples)
	{
		if (string.IsNullOrWhiteSpace(counterName))
		{
			throw new ArgumentException("counter_name is required.", nameof(counterName));
		}

		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["counterName"] = counterName,
			["counter"] = new Dictionary<string, object>(),
			["range"] = BuildFrameRange(startFrame, endFrame),
			["accumulated"] = accumulated,
			["sampleCount"] = 0,
			["availableSampleCount"] = 0,
			["truncated"] = false,
			["samples"] = new ArrayList(),
			["eventsDecoded"] = false,
			["diagnostics"] = Diagnostics("TracyPlotsPending", "Plot decoding is required before Tracy counter samples can return data."),
			["maxSamples"] = Math.Max(1, maxSamples)
		};
	}

	public Dictionary<string, object> AnalyzeTimeRange(int startFrame, int endFrame, int top, double thresholdMs)
	{
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["range"] = BuildFrameRange(startFrame, endFrame),
			["framesUnavailable"] = true,
			["frameSource"] = "none",
			["eventsDecoded"] = false,
			["summary"] = new Dictionary<string, object>
			{
				["sourceFormat"] = SourceFormat,
				["frameCount"] = 0,
				["threadCount"] = 0,
				["zoneCount"] = 0,
				["plotCount"] = 0
			},
			["profilerOverhead"] = GetProfilerOverhead(startFrame, endFrame, top),
			["slowFrames"] = new ArrayList(),
			["scopeHotspots"] = new ArrayList(),
			["slowFramePattern"] = new Dictionary<string, object>
			{
				["hasPeriodicSlowFrames"] = false,
				["reason"] = "Tracy frame marks have not been decoded yet."
			},
			["diagnostics"] = Diagnostics("TracyEventsPending", "Tracy event decoding is required before time range analysis can return data."),
			["thresholdMs"] = Round(Math.Max(0.0, thresholdMs)),
			["top"] = Math.Max(1, top)
		};
	}

	private ArrayList Diagnostics(string code, string message)
	{
		ArrayList diagnostics = new ArrayList();
		foreach (object diagnostic in m_Diagnostics)
		{
			diagnostics.Add(diagnostic);
		}
		diagnostics.Add(new Dictionary<string, object>
		{
			["severity"] = "warning",
			["code"] = code,
			["message"] = message
		});
		return diagnostics;
	}

	private static Dictionary<string, object> BuildFrameRange(int startFrame, int endFrame)
	{
		return new Dictionary<string, object>
		{
			["startFrame"] = startFrame,
			["endFrame"] = endFrame,
			["framesUnavailable"] = true
		};
	}

	private static double Round(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			return 0.0;
		}
		return Math.Round(value, 3);
	}
}
