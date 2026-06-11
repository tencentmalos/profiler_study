using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

	public TracyEventStream EventStream => m_EventStream;

	public ArrayList ImportDiagnostics => m_Diagnostics;

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
				["threadCount"] = m_EventStream.ThreadCount,
				["frameCount"] = GetMetadataFrameCount(),
				["zoneCount"] = m_EventStream.CpuZones.Count,
				["plotCount"] = m_EventStream.Plots.Count,
				["compressedBlockCount"] = m_EventStream.CompressedBlockCount,
				["compressedByteCount"] = m_EventStream.CompressedByteCount,
				["decodedByteCount"] = m_EventStream.DecodedByteCount,
				["payloadByteCount"] = m_EventStream.PayloadByteCount,
				["metadataDecoded"] = m_EventStream.HasMetadata,
				["captureName"] = m_EventStream.Metadata == null ? string.Empty : m_EventStream.Metadata.CaptureName,
				["captureProgram"] = m_EventStream.Metadata == null ? string.Empty : m_EventStream.Metadata.CaptureProgram,
				["hostInfo"] = m_EventStream.Metadata == null ? string.Empty : m_EventStream.Metadata.HostInfo,
				["processId"] = m_EventStream.Metadata == null ? 0UL : m_EventStream.Metadata.ProcessId,
				["resolution"] = m_EventStream.Metadata == null ? 0L : m_EventStream.Metadata.Resolution,
				["lastTime"] = m_EventStream.Metadata == null ? 0L : m_EventStream.Metadata.LastTime,
				["frameSetCount"] = m_EventStream.Metadata == null ? 0 : m_EventStream.Metadata.FrameSetCount,
				["eventsDecoded"] = m_EventStream.CpuZones.Count > 0,
				["framesUnavailable"] = !HasMetadataFrames(),
				["frameSource"] = HasMetadataFrames() ? "tracy-frame-set-metadata" : "none",
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
		if (HasMetadataFrames())
		{
			double resolvedThresholdMs = Math.Max(0.0, thresholdMs);
			List<Dictionary<string, object>> frames = GetMetadataFrames()
				.Select(frame => FrameToDictionary(frame))
				.Where(frame => resolvedThresholdMs <= 0.0 || Convert.ToDouble(frame["durationMs"]) >= resolvedThresholdMs)
				.OrderByDescending(frame => Convert.ToDouble(frame["durationMs"]))
				.ThenBy(frame => Convert.ToInt32(frame["frameIndex"]))
				.Take(Math.Max(1, top))
				.ToList();
			return new Dictionary<string, object>
			{
				["sourceFormat"] = SourceFormat,
				["thresholdMs"] = Round(resolvedThresholdMs),
				["framesUnavailable"] = false,
				["frameSource"] = "tracy-frame-set-metadata",
				["eventsDecoded"] = false,
				["slowFrames"] = ToArrayList(frames),
				["slowFramePattern"] = new Dictionary<string, object>
				{
					["hasPeriodicSlowFrames"] = false,
					["reason"] = "Tracy event timeline decoding is pending; cadence analysis uses frame set metadata only."
				},
				["diagnostics"] = Diagnostics("TracyFrameSetMetadata", "Slow frame results are based on saved Tracy frame set metadata; full frame mark decoding is pending."),
				["top"] = Math.Max(1, top)
			};
		}

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
		List<TracyCpuZoneSummary> zones = FilterZonesByFrameRange(startFrame, endFrame).ToList();
		if (zones.Count > 0)
		{
			ArrayList hotspots = BuildScopeHotspots(zones, top);
			return new Dictionary<string, object>
			{
				["sourceFormat"] = SourceFormat,
				["eventsDecoded"] = true,
				["scopeHotspots"] = hotspots,
				["range"] = BuildFrameRange(startFrame, endFrame),
				["diagnostics"] = Diagnostics("TracyCpuZonesDecoded", "Scope hotspots are aggregated from decoded Tracy CPU zones."),
				["top"] = Math.Max(1, top)
			};
		}

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
		List<Dictionary<string, object>> counters = m_EventStream.Plots
			.Where(plot => string.IsNullOrWhiteSpace(filter) || plot.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
			.Select(PlotToCounterDictionary)
			.OrderByDescending(counter => Convert.ToInt32(counter["sampleCount"]))
			.ThenBy(counter => Convert.ToString(counter["name"]))
			.Take(Math.Max(1, top))
			.ToList();
		if (counters.Count > 0)
		{
			return new Dictionary<string, object>
			{
				["sourceFormat"] = SourceFormat,
				["filter"] = filter ?? string.Empty,
				["eventsDecoded"] = true,
				["counters"] = ToArrayList(counters),
				["diagnostics"] = Diagnostics("TracyPlotsDecoded", "Counters are read from decoded Tracy plot data."),
				["top"] = Math.Max(1, top)
			};
		}

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

		TracyPlotSummary plot = m_EventStream.Plots.FirstOrDefault(value => string.Equals(value.Name, counterName, StringComparison.OrdinalIgnoreCase));
		if (plot != null)
		{
			ArrayList samples = ToArrayList(plot.Samples
				.Take(Math.Max(1, maxSamples))
				.Select(sample => new Dictionary<string, object>
				{
					["time"] = sample.Time,
					["value"] = Round(sample.Value)
				}));
			return new Dictionary<string, object>
			{
				["sourceFormat"] = SourceFormat,
				["counterName"] = plot.Name,
				["counter"] = PlotToCounterDictionary(plot),
				["range"] = BuildFrameRange(startFrame, endFrame),
				["accumulated"] = accumulated,
				["sampleCount"] = samples.Count,
				["availableSampleCount"] = plot.Samples.Count,
				["truncated"] = plot.Samples.Count > samples.Count,
				["samples"] = samples,
				["eventsDecoded"] = true,
				["diagnostics"] = Diagnostics("TracyPlotsDecoded", "Counter samples are read from decoded Tracy plot data."),
				["maxSamples"] = Math.Max(1, maxSamples)
			};
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

	public Dictionary<string, object> AnalyzeFrame(int frameIndex, int top, int neighborCount)
	{
		if (!TryGetMetadataFrame(frameIndex, out TracyFrameSummary frame))
		{
			return BuildUnsupportedFrameResult(
				"analyzeFrame",
				frameIndex,
				"TracyFrameAnalysisUnavailable",
				HasMetadataFrames() ? "Requested Tracy frame index is outside the decoded frame metadata range." : "Tracy frame metadata is required before frame analysis can return data.");
		}

		int resolvedNeighborCount = Math.Max(0, neighborCount);
		int startFrame = Math.Max(0, frameIndex - resolvedNeighborCount);
		int endFrame = Math.Min(GetMetadataFrameCount() - 1, frameIndex + resolvedNeighborCount);
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["capability"] = "analyzeFrame",
			["supported"] = true,
			["framesUnavailable"] = false,
			["frameSource"] = "tracy-frame-set-metadata",
			["frame"] = FrameToDictionary(frame),
			["range"] = BuildFrameRange(frameIndex, frameIndex),
			["scopeHotspots"] = BuildScopeHotspots(FilterZonesByFrameRange(frameIndex, frameIndex), top),
			["neighborRange"] = BuildFrameRange(startFrame, endFrame),
			["neighborSlowFrames"] = FindSlowFrames(Math.Min(Math.Max(1, top), endFrame + 1 - startFrame), 0.0)["slowFrames"],
			["diagnostics"] = Diagnostics("TracyFrameSetMetadata", "Frame analysis is based on decoded Tracy frame set metadata; full frame mark and stack hierarchy decoding is pending.")
		};
	}

	public Dictionary<string, object> AnalyzeFrameDetail(int frameIndex, int maxNodes, int maxDepth, double minDurationMs)
	{
		if (!TryGetMetadataFrame(frameIndex, out TracyFrameSummary frame))
		{
			return BuildUnsupportedFrameResult(
				"analyzeFrameDetail",
				frameIndex,
				"TracyFrameDetailUnavailable",
				HasMetadataFrames() ? "Requested Tracy frame index is outside the decoded frame metadata range." : "Tracy frame metadata is required before frame detail analysis can return data.");
		}

		List<Dictionary<string, object>> allSpans = BuildFrameDetailSpans(frameIndex, frame, minDurationMs, out int omittedByDurationCount);
		int resolvedMaxNodes = Math.Max(1, maxNodes);
		List<Dictionary<string, object>> includedSpans = allSpans
			.Take(resolvedMaxNodes)
			.ToList();
		int omittedNodeCount = Math.Max(0, allSpans.Count - includedSpans.Count) + omittedByDurationCount;
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["capability"] = "analyzeFrameDetail",
			["supported"] = true,
			["framesUnavailable"] = false,
			["frameSource"] = "tracy-frame-set-metadata",
			["frame"] = FrameToDictionary(frame),
			["range"] = BuildFrameRange(frameIndex, frameIndex),
			["options"] = new Dictionary<string, object>
			{
				["maxNodes"] = resolvedMaxNodes,
				["maxDepth"] = Math.Max(1, maxDepth),
				["minDurationMs"] = Round(Math.Max(0.0, minDurationMs))
			},
			["threadFlameGraphs"] = new ArrayList(),
			["topSpans"] = ToArrayList(includedSpans),
			["frameCounters"] = new ArrayList(),
			["nodeStats"] = new Dictionary<string, object>
			{
				["includedNodeCount"] = includedSpans.Count,
				["omittedNodeCount"] = omittedNodeCount,
				["omittedByDepthCount"] = 0,
				["omittedByDurationCount"] = omittedByDurationCount,
				["truncated"] = allSpans.Count > includedSpans.Count
			},
			["diagnostics"] = Diagnostics("TracyFrameDetailPartial", "Frame detail uses decoded Tracy frame set metadata and flat CPU zones; full Tracy callstack hierarchy decoding is pending.")
		};
	}

	public Dictionary<string, object> AnalyzeTimeRange(int startFrame, int endFrame, int top, double thresholdMs)
	{
		List<TracyCpuZoneSummary> zones = FilterZonesByFrameRange(startFrame, endFrame).ToList();
		if (zones.Count > 0 || HasMetadataFrames())
		{
			ArrayList hotspots = BuildScopeHotspots(zones, top);
			return new Dictionary<string, object>
			{
				["sourceFormat"] = SourceFormat,
				["range"] = BuildFrameRange(startFrame, endFrame),
				["framesUnavailable"] = !HasMetadataFrames(),
				["frameSource"] = HasMetadataFrames() ? "tracy-frame-set-metadata" : "none",
				["eventsDecoded"] = zones.Count > 0,
				["summary"] = new Dictionary<string, object>
				{
					["sourceFormat"] = SourceFormat,
					["frameCount"] = GetMetadataFrameCount(),
					["threadCount"] = m_EventStream.ThreadCount,
					["zoneCount"] = zones.Count,
					["plotCount"] = m_EventStream.Plots.Count
				},
				["profilerOverhead"] = GetProfilerOverhead(startFrame, endFrame, top),
				["slowFrames"] = FindSlowFrames(top, thresholdMs)["slowFrames"],
				["scopeHotspots"] = hotspots,
				["slowFramePattern"] = new Dictionary<string, object>
				{
					["hasPeriodicSlowFrames"] = false,
					["reason"] = "Tracy event timeline decoding is partial; cadence analysis uses frame set metadata only."
				},
				["diagnostics"] = Diagnostics("TracyCpuZonesDecoded", "Time range analysis is based on decoded Tracy CPU zones and frame set metadata."),
				["thresholdMs"] = Round(Math.Max(0.0, thresholdMs)),
				["top"] = Math.Max(1, top)
			};
		}

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

	private bool HasMetadataFrames()
	{
		return GetMetadataFrameCount() > 0;
	}

	private bool TryGetMetadataFrame(int frameIndex, out TracyFrameSummary frame)
	{
		if (frameIndex >= 0)
		{
			foreach (TracyFrameSummary candidate in GetMetadataFrames())
			{
				if (candidate.FrameIndex == frameIndex)
				{
					frame = candidate;
					return true;
				}
			}
		}
		frame = null;
		return false;
	}

	private int GetMetadataFrameCount()
	{
		return m_EventStream.Metadata == null ? 0 : m_EventStream.Metadata.FrameCount;
	}

	private IEnumerable<TracyCpuZoneSummary> FilterZonesByFrameRange(int startFrame, int endFrame)
	{
		if (m_EventStream.CpuZones.Count == 0)
		{
			yield break;
		}

		if (!HasMetadataFrames() || startFrame < 0 || endFrame < 0)
		{
			foreach (TracyCpuZoneSummary zone in m_EventStream.CpuZones)
			{
				yield return zone;
			}
			yield break;
		}

		List<TracyFrameSummary> frames = GetMetadataFrames().ToList();
		if (frames.Count == 0)
		{
			yield break;
		}
		int first = Math.Max(0, Math.Min(startFrame, frames.Count - 1));
		int last = Math.Max(first, Math.Min(endFrame, frames.Count - 1));
		long rangeStart = frames[first].Start;
		long rangeEnd = frames[last].End;
		foreach (TracyCpuZoneSummary zone in m_EventStream.CpuZones)
		{
			if (zone.End >= rangeStart && zone.Start <= rangeEnd)
			{
				yield return zone;
			}
		}
	}

	private List<Dictionary<string, object>> BuildFrameDetailSpans(int frameIndex, TracyFrameSummary frame, double minDurationMs, out int omittedByDurationCount)
	{
		long minDurationNs = (long)Math.Ceiling(Math.Max(0.0, minDurationMs) * 1_000_000.0);
		omittedByDurationCount = 0;
		List<Dictionary<string, object>> spans = new List<Dictionary<string, object>>();
		foreach (TracyCpuZoneSummary zone in FilterZonesByFrameRange(frameIndex, frameIndex))
		{
			long clippedStart = Math.Max(zone.Start, frame.Start);
			long clippedEnd = Math.Min(zone.End, frame.End);
			if (clippedEnd <= clippedStart)
			{
				continue;
			}

			long duration = clippedEnd - clippedStart;
			if (duration < minDurationNs)
			{
				omittedByDurationCount++;
				continue;
			}

			spans.Add(new Dictionary<string, object>
			{
				["threadId"] = zone.ThreadId,
				["name"] = zone.Name,
				["sourceLocation"] = zone.SourceLocation,
				["start"] = clippedStart,
				["end"] = clippedEnd,
				["startOffsetMs"] = Round((clippedStart - frame.Start) / 1_000_000.0),
				["durationMs"] = Round(duration / 1_000_000.0),
				["depth"] = 0
			});
		}

		return spans
			.OrderByDescending(span => Convert.ToDouble(span["durationMs"]))
			.ThenBy(span => Convert.ToString(span["name"]))
			.ThenBy(span => Convert.ToUInt64(span["threadId"]))
			.ToList();
	}

	private IEnumerable<TracyFrameSummary> GetMetadataFrames()
	{
		if (m_EventStream.Metadata == null)
		{
			yield break;
		}
		int globalFrameIndex = 0;
		foreach (TracyFrameSetSummary frameSet in m_EventStream.Metadata.FrameSets)
		{
			foreach (TracyFrameSummary frame in frameSet.Frames)
			{
				yield return new TracyFrameSummary(globalFrameIndex++, frame.Start, frame.End);
			}
		}
	}

	private static Dictionary<string, object> FrameToDictionary(TracyFrameSummary frame)
	{
		return new Dictionary<string, object>
		{
			["frameIndex"] = frame.FrameIndex,
			["startTime"] = frame.Start,
			["endTime"] = frame.End,
			["durationNs"] = frame.Duration,
			["durationMs"] = Round(frame.Duration / 1_000_000.0)
		};
	}

	private static ArrayList BuildScopeHotspots(IEnumerable<TracyCpuZoneSummary> zones, int top)
	{
		IEnumerable<Dictionary<string, object>> hotspots = zones
			.GroupBy(zone => zone.Name)
			.Select(group => new Dictionary<string, object>
			{
				["name"] = group.Key,
				["totalMs"] = Round(group.Sum(zone => zone.Duration) / 1_000_000.0),
				["totalCount"] = group.Count(),
				["maxMs"] = Round(group.Max(zone => zone.Duration) / 1_000_000.0),
				["threadCount"] = group.Select(zone => zone.ThreadId).Distinct().Count()
			})
			.OrderByDescending(hotspot => Convert.ToDouble(hotspot["totalMs"]))
			.ThenBy(hotspot => Convert.ToString(hotspot["name"]))
			.Take(Math.Max(1, top));
		return ToArrayList(hotspots);
	}

	private Dictionary<string, object> BuildUnsupportedFrameResult(string capability, int frameIndex, string code, string message)
	{
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["capability"] = capability,
			["supported"] = false,
			["framesUnavailable"] = !HasMetadataFrames(),
			["frameSource"] = HasMetadataFrames() ? "tracy-frame-set-metadata" : "none",
			["frameIndex"] = frameIndex,
			["diagnostics"] = Diagnostics(code, message)
		};
	}

	private static Dictionary<string, object> ZoneToSpanDictionary(TracyCpuZoneSummary zone)
	{
		return new Dictionary<string, object>
		{
			["threadId"] = zone.ThreadId,
			["name"] = zone.Name,
			["sourceLocation"] = zone.SourceLocation,
			["start"] = zone.Start,
			["end"] = zone.End,
			["durationMs"] = Round(zone.Duration / 1_000_000.0)
		};
	}

	private static Dictionary<string, object> PlotToCounterDictionary(TracyPlotSummary plot)
	{
		return new Dictionary<string, object>
		{
			["name"] = plot.Name,
			["valueType"] = "Double",
			["format"] = plot.Format,
			["type"] = plot.Type,
			["sampleCount"] = plot.Samples.Count,
			["minValue"] = Round(plot.Min),
			["maxValue"] = Round(plot.Max),
			["totalValueDouble"] = Round(plot.Sum)
		};
	}

	private static ArrayList ToArrayList(IEnumerable<Dictionary<string, object>> values)
	{
		ArrayList list = new ArrayList();
		foreach (Dictionary<string, object> value in values)
		{
			list.Add(value);
		}
		return list;
	}

	private Dictionary<string, object> BuildFrameRange(int startFrame, int endFrame)
	{
		if (HasMetadataFrames())
		{
			int frameCount = GetMetadataFrameCount();
			int first = Math.Max(0, Math.Min(startFrame, frameCount - 1));
			int last = Math.Max(first, Math.Min(endFrame, frameCount - 1));
			return new Dictionary<string, object>
			{
				["startFrame"] = first,
				["endFrame"] = last,
				["framesUnavailable"] = false,
				["frameSource"] = "tracy-frame-set-metadata"
			};
		}

		return new Dictionary<string, object>
		{
			["startFrame"] = startFrame,
			["endFrame"] = endFrame,
			["framesUnavailable"] = true,
			["frameSource"] = "none"
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
