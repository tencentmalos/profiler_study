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

	private sealed class FrameDetailStackItem
	{
		public FrameDetailStackItem(int depth, long end, Dictionary<string, object> node)
		{
			Depth = depth;
			End = end;
			Node = node;
		}

		public int Depth { get; }

		public long End { get; }

		public Dictionary<string, object> Node { get; }
	}

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

	public IReadOnlyList<TracyFrameSummary> GetVisibleFrames()
	{
		return GetMetadataFrames().ToArray();
	}

	public Dictionary<string, object> GetSummary(int top)
	{
		int resolvedTop = Math.Max(1, top);
		ArrayList threads = ToArrayList(m_EventStream.Threads
			.OrderBy(thread => thread.ThreadId)
			.Take(resolvedTop)
			.Select(ThreadToDictionary));
		Dictionary<string, object> slowFrameResult = FindSlowFrames(resolvedTop, 0.0);
		Dictionary<string, object> hotspotResult = FindScopeHotspots(resolvedTop, -1, -1);
		Dictionary<string, object> counterResult = ListCounters(resolvedTop, string.Empty);
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
				["gpuContextCount"] = m_EventStream.GpuContexts.Count,
				["gpuZoneCount"] = m_EventStream.GpuZones.Count,
				["gpuTimeZoneCount"] = m_EventStream.GpuZones.Count(zone => string.Equals(zone.TimeSource, "gpu-time", StringComparison.OrdinalIgnoreCase)),
				["cpuSubmitGpuZoneCount"] = m_EventStream.GpuZones.Count(zone => string.Equals(zone.TimeSource, "cpu-submit-time", StringComparison.OrdinalIgnoreCase)),
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
				["eventsDecoded"] = HasDecodedEventData(),
				["framesUnavailable"] = !HasMetadataFrames(),
				["frameSource"] = HasMetadataFrames() ? "tracy-frame-set-metadata" : "none",
				["status"] = "header-loaded"
			},
			["threads"] = threads,
			["profilerOverhead"] = GetProfilerOverhead(-1, -1, resolvedTop),
			["slowFrames"] = GetArrayList(slowFrameResult, "slowFrames"),
			["scopeHotspots"] = GetArrayList(hotspotResult, "scopeHotspots"),
			["customStats"] = GetArrayList(counterResult, "counters"),
			["slowFramePattern"] = slowFrameResult.TryGetValue("slowFramePattern", out object slowFramePattern) ? slowFramePattern : new Dictionary<string, object>(),
			["diagnostics"] = m_Diagnostics,
			["capabilities"] = new Dictionary<string, object>
			{
				["summary"] = true,
				["threads"] = m_EventStream.Threads.Count > 0,
				["frames"] = HasMetadataFrames(),
				["scopeHotspots"] = m_EventStream.CpuZones.Count > 0,
				["gpu"] = m_EventStream.GpuZones.Count > 0 || m_EventStream.GpuContexts.Count > 0,
				["counters"] = m_EventStream.Plots.Count > 0,
				["timeRange"] = HasMetadataFrames() || m_EventStream.CpuZones.Count > 0,
				["profilerOverhead"] = false
			},
			["top"] = resolvedTop
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
			List<TracyPlotSample> filteredSamples = FilterPlotSamplesByFrameRange(plot, startFrame, endFrame).ToList();
			ArrayList samples = ToArrayList(filteredSamples
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
				["availableSampleCount"] = filteredSamples.Count,
				["truncated"] = filteredSamples.Count > samples.Count,
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
			["availableCounters"] = ToArrayList(m_EventStream.Plots.Select(PlotToCounterDictionary)),
			["range"] = BuildFrameRange(startFrame, endFrame),
			["accumulated"] = accumulated,
			["sampleCount"] = 0,
			["availableSampleCount"] = 0,
			["truncated"] = false,
			["samples"] = new ArrayList(),
			["eventsDecoded"] = m_EventStream.Plots.Count > 0,
			["diagnostics"] = Diagnostics(
				m_EventStream.Plots.Count > 0 ? "TracyCounterNotFound" : "TracyPlotsPending",
				m_EventStream.Plots.Count > 0 ? "Requested Tracy counter was not found in decoded plot data." : "Plot decoding is required before Tracy counter samples can return data."),
			["maxSamples"] = Math.Max(1, maxSamples)
		};
	}

	public Dictionary<string, object> ListGpuZones(int top, int startFrame, int endFrame, long startTimeNs, long endTimeNs, string timeSource)
	{
		int resolvedTop = Math.Max(1, top);
		string resolvedTimeSource = NormalizeGpuTimeSource(timeSource);
		List<TracyGpuZoneSummary> rangeZones = FilterGpuZones(startFrame, endFrame, startTimeNs, endTimeNs).ToList();
		List<TracyGpuZoneSummary> zones = rangeZones
			.Where(zone => resolvedTimeSource == "any" || string.Equals(zone.TimeSource, resolvedTimeSource, StringComparison.OrdinalIgnoreCase))
			.ToList();
		int gpuTimeZoneCount = rangeZones.Count(zone => string.Equals(zone.TimeSource, "gpu-time", StringComparison.OrdinalIgnoreCase));
		int cpuSubmitZoneCount = rangeZones.Count(zone => string.Equals(zone.TimeSource, "cpu-submit-time", StringComparison.OrdinalIgnoreCase));
		string summaryTimeSource = gpuTimeZoneCount > 0 && cpuSubmitZoneCount > 0
			? "mixed"
			: (gpuTimeZoneCount > 0 ? "gpu-time" : (cpuSubmitZoneCount > 0 ? "cpu-submit-time" : resolvedTimeSource));
		ArrayList contexts = ToArrayList(m_EventStream.GpuContexts
			.OrderBy(context => context.Context)
			.Select(GpuContextToDictionary));
		ArrayList zoneItems = ToArrayList(zones
			.OrderByDescending(zone => zone.Duration)
			.ThenBy(zone => zone.Start)
			.Take(resolvedTop)
			.Select(GpuZoneToDictionary));
		ArrayList hotspots = ToArrayList(zones
			.GroupBy(zone => zone.Name)
			.Select(group => new Dictionary<string, object>
			{
				["name"] = group.Key,
				["totalMs"] = Round(group.Sum(zone => zone.Duration) / 1_000_000.0),
				["totalCount"] = group.Count(),
				["maxMs"] = Round(group.Max(zone => zone.Duration) / 1_000_000.0),
				["contextCount"] = group.Select(zone => zone.Context).Distinct().Count()
			})
			.OrderByDescending(hotspot => Convert.ToDouble(hotspot["totalMs"]))
			.ThenBy(hotspot => Convert.ToString(hotspot["name"]))
			.Take(resolvedTop));
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["eventsDecoded"] = m_EventStream.GpuZones.Count > 0 || m_EventStream.GpuContexts.Count > 0,
			["timeSource"] = resolvedTimeSource,
			["summary"] = new Dictionary<string, object>
			{
				["contextCount"] = m_EventStream.GpuContexts.Count,
				["zoneCount"] = zones.Count,
				["rangeZoneCount"] = rangeZones.Count,
				["totalZoneCount"] = m_EventStream.GpuZones.Count,
				["gpuTimeZoneCount"] = gpuTimeZoneCount,
				["cpuSubmitZoneCount"] = cpuSubmitZoneCount,
				["timeSource"] = summaryTimeSource,
				["returnedTimeSource"] = resolvedTimeSource
			},
			["range"] = BuildGpuRange(startFrame, endFrame, startTimeNs, endTimeNs),
			["contexts"] = contexts,
			["zones"] = zoneItems,
			["hotspots"] = hotspots,
			["diagnostics"] = Diagnostics(
				m_EventStream.GpuZones.Count > 0 ? "TracyGpuZonesDecoded" : "TracyGpuZonesPending",
				m_EventStream.GpuZones.Count > 0 ? "GPU zones are decoded from Tracy live queue events. time_source=gpu-time returns zones with resolved Tracy GpuTime samples; cpu-submit-time returns submit fallback zones." : "GPU zone decoding requires Tracy live GPU queue events."),
			["top"] = resolvedTop
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

		int resolvedMaxNodes = Math.Max(1, maxNodes);
		int resolvedMaxDepth = Math.Max(1, maxDepth);
		ArrayList threadFlameGraphs = BuildFrameDetailThreadFlameGraphs(
			frame,
			resolvedMaxNodes,
			resolvedMaxDepth,
			minDurationMs,
			out List<Dictionary<string, object>> flatSpans,
			out int includedNodeCount,
			out int omittedNodeCount,
			out int omittedByDepthCount,
			out int omittedByDurationCount,
			out bool truncated);
		List<Dictionary<string, object>> topSpans = flatSpans
			.OrderByDescending(span => Convert.ToDouble(span["durationMs"]))
			.ThenBy(span => Convert.ToString(span["name"]))
			.ThenBy(span => Convert.ToUInt64(span["threadId"]))
			.Take(resolvedMaxNodes)
			.ToList();
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
				["maxDepth"] = resolvedMaxDepth,
				["minDurationMs"] = Round(Math.Max(0.0, minDurationMs))
			},
			["threadFlameGraphs"] = threadFlameGraphs,
			["topSpans"] = ToArrayList(topSpans),
			["frameCounters"] = BuildFrameCounters(frame, frameIndex, 200),
			["nodeStats"] = new Dictionary<string, object>
			{
				["includedNodeCount"] = includedNodeCount,
				["omittedNodeCount"] = omittedNodeCount,
				["omittedByDepthCount"] = omittedByDepthCount,
				["omittedByDurationCount"] = omittedByDurationCount,
				["truncated"] = truncated
			},
			["diagnostics"] = Diagnostics("TracyFrameDetailHierarchyDecoded", "Frame detail uses decoded Tracy CPU zone hierarchy and frame set metadata; callstack symbols are limited to decoded zone names.")
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
					["gpuZoneCount"] = FilterGpuZones(startFrame, endFrame, 0L, 0L).Count(),
					["plotCount"] = m_EventStream.Plots.Count
				},
				["profilerOverhead"] = GetProfilerOverhead(startFrame, endFrame, top),
				["slowFrames"] = FindSlowFrames(top, thresholdMs)["slowFrames"],
				["scopeHotspots"] = hotspots,
				["gpu"] = ListGpuZones(top, startFrame, endFrame, 0L, 0L, "any"),
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

	public Dictionary<string, object> AnalyzeTimeRange(long startTimeNs, long endTimeNs, int top, double thresholdMs)
	{
		long resolvedStart = Math.Min(startTimeNs, endTimeNs);
		long resolvedEnd = Math.Max(startTimeNs, endTimeNs);
		List<TracyCpuZoneSummary> zones = FilterZonesByTimeRange(resolvedStart, resolvedEnd).ToList();
		ArrayList hotspots = BuildScopeHotspots(zones, top);
		return new Dictionary<string, object>
		{
			["sourceFormat"] = SourceFormat,
			["startTimeNs"] = resolvedStart,
			["endTimeNs"] = resolvedEnd,
			["range"] = new Dictionary<string, object>
			{
				["startTimeNs"] = resolvedStart,
				["endTimeNs"] = resolvedEnd,
				["framesUnavailable"] = !HasMetadataFrames(),
				["frameSource"] = HasMetadataFrames() ? "tracy-frame-set-metadata" : "none"
			},
			["framesUnavailable"] = !HasMetadataFrames(),
			["frameSource"] = HasMetadataFrames() ? "tracy-frame-set-metadata" : "none",
			["eventsDecoded"] = zones.Count > 0,
			["summary"] = new Dictionary<string, object>
			{
				["sourceFormat"] = SourceFormat,
				["frameCount"] = GetMetadataFrameCount(),
				["threadCount"] = m_EventStream.ThreadCount,
				["zoneCount"] = zones.Count,
				["gpuZoneCount"] = FilterGpuZones(-1, -1, resolvedStart, resolvedEnd).Count(),
				["plotCount"] = m_EventStream.Plots.Count
			},
			["profilerOverhead"] = GetProfilerOverhead(-1, -1, top),
			["slowFrames"] = new ArrayList(),
			["scopeHotspots"] = hotspots,
			["gpu"] = ListGpuZones(top, -1, -1, resolvedStart, resolvedEnd, "any"),
			["slowFramePattern"] = new Dictionary<string, object>
			{
				["hasPeriodicSlowFrames"] = false,
				["reason"] = "Time-range analysis was requested in trace time; slow frame cadence requires frame metadata."
			},
			["diagnostics"] = Diagnostics(
				zones.Count > 0 ? "TracyCpuZonesDecoded" : "TracyZonesPending",
				zones.Count > 0 ? "Time range analysis is based on decoded Tracy CPU zones." : "CPU zone decoding is required before time range analysis can return hotspot data."),
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

	private bool HasDecodedEventData()
	{
		return m_EventStream.CpuZones.Count > 0 || m_EventStream.Plots.Count > 0 || m_EventStream.GpuZones.Count > 0;
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
		return GetMetadataFrames().Count();
	}

	private IEnumerable<TracyCpuZoneSummary> FilterZonesByFrameRange(int startFrame, int endFrame)
	{
		if (m_EventStream.CpuZones.Count == 0)
		{
			yield break;
		}

		if (!HasExplicitFrameRange(startFrame, endFrame) ||
			!TryResolveMetadataFrameRange(startFrame, endFrame, out List<TracyFrameSummary> frames, out int first, out int last))
		{
			foreach (TracyCpuZoneSummary zone in m_EventStream.CpuZones)
			{
				yield return zone;
			}
			yield break;
		}

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

	private IEnumerable<TracyCpuZoneSummary> FilterZonesByTimeRange(long startTimeNs, long endTimeNs)
	{
		foreach (TracyCpuZoneSummary zone in m_EventStream.CpuZones)
		{
			long clippedStart = Math.Max(zone.Start, startTimeNs);
			long clippedEnd = Math.Min(zone.End, endTimeNs);
			if (clippedEnd > clippedStart)
			{
				yield return new TracyCpuZoneSummary(zone.ThreadId, zone.SourceLocation, zone.Name, clippedStart, clippedEnd, zone.Depth, Math.Min(zone.SelfDuration, clippedEnd - clippedStart));
			}
		}
	}

	private IEnumerable<TracyPlotSample> FilterPlotSamplesByFrameRange(TracyPlotSummary plot, int startFrame, int endFrame)
	{
		if (plot == null || plot.Samples.Count == 0)
		{
			yield break;
		}

		if (!HasExplicitFrameRange(startFrame, endFrame) ||
			!TryResolveMetadataFrameRange(startFrame, endFrame, out List<TracyFrameSummary> frames, out int first, out int last))
		{
			foreach (TracyPlotSample sample in plot.Samples)
			{
				yield return sample;
			}
			yield break;
		}

		long rangeStart = frames[first].Start;
		long rangeEnd = frames[last].End;
		foreach (TracyPlotSample sample in plot.Samples)
		{
			if (sample.Time >= rangeStart && sample.Time <= rangeEnd)
			{
				yield return sample;
			}
		}
	}

	private IEnumerable<TracyGpuZoneSummary> FilterGpuZones(int startFrame, int endFrame, long startTimeNs, long endTimeNs)
	{
		if (startTimeNs > 0L || endTimeNs > 0L)
		{
			long resolvedStart = Math.Min(startTimeNs, endTimeNs);
			long resolvedEnd = Math.Max(startTimeNs, endTimeNs);
			foreach (TracyGpuZoneSummary zone in m_EventStream.GpuZones)
			{
				long clippedStart = Math.Max(zone.Start, resolvedStart);
				long clippedEnd = Math.Min(zone.End, resolvedEnd);
				if (clippedEnd > clippedStart)
				{
					yield return new TracyGpuZoneSummary(zone.Context, zone.QueryId, zone.ThreadId, zone.SourceLocation, zone.Name, clippedStart, clippedEnd, zone.TimeSource);
				}
			}
			yield break;
		}

		if (!HasExplicitFrameRange(startFrame, endFrame) ||
			!TryResolveMetadataFrameRange(startFrame, endFrame, out List<TracyFrameSummary> frames, out int first, out int last))
		{
			foreach (TracyGpuZoneSummary zone in m_EventStream.GpuZones)
			{
				yield return zone;
			}
			yield break;
		}

		long rangeStart = frames[first].Start;
		long rangeEnd = frames[last].End;
		foreach (TracyGpuZoneSummary zone in m_EventStream.GpuZones)
		{
			if (zone.End >= rangeStart && zone.Start <= rangeEnd)
			{
				yield return zone;
			}
		}
	}

	private static string NormalizeGpuTimeSource(string timeSource)
	{
		if (string.IsNullOrWhiteSpace(timeSource))
		{
			return "any";
		}
		string normalized = timeSource.Trim().ToLowerInvariant();
		if (normalized == "gpu" || normalized == "gpu_time" || normalized == "gputime")
		{
			return "gpu-time";
		}
		if (normalized == "cpu" || normalized == "submit" || normalized == "cpu_submit" || normalized == "cpu-submit")
		{
			return "cpu-submit-time";
		}
		if (normalized == "gpu-time" || normalized == "cpu-submit-time")
		{
			return normalized;
		}
		return "any";
	}

	private ArrayList BuildFrameDetailThreadFlameGraphs(
		TracyFrameSummary frame,
		int maxNodes,
		int maxDepth,
		double minDurationMs,
		out List<Dictionary<string, object>> flatSpans,
		out int includedNodeCount,
		out int omittedNodeCount,
		out int omittedByDepthCount,
		out int omittedByDurationCount,
		out bool truncated)
	{
		long minDurationNs = (long)Math.Ceiling(Math.Max(0.0, minDurationMs) * 1_000_000.0);
		Dictionary<ulong, string> threadNames = m_EventStream.Threads
			.GroupBy(thread => thread.ThreadId)
			.ToDictionary(group => group.Key, group => group.Select(thread => thread.Name).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty);
		ArrayList threadGraphs = new ArrayList();
		flatSpans = new List<Dictionary<string, object>>();
		includedNodeCount = 0;
		omittedNodeCount = 0;
		omittedByDepthCount = 0;
		omittedByDurationCount = 0;
		truncated = false;

		IEnumerable<IGrouping<ulong, TracyCpuZoneSummary>> zonesByThread = m_EventStream.CpuZones
			.Where(zone => zone.End > frame.Start && zone.Start < frame.End)
			.GroupBy(zone => zone.ThreadId)
			.OrderBy(group => group.Key);
		foreach (IGrouping<ulong, TracyCpuZoneSummary> threadGroup in zonesByThread)
		{
			string threadName = GetThreadName(threadNames, threadGroup.Key);
			ArrayList roots = new ArrayList();
			List<FrameDetailStackItem> stack = new List<FrameDetailStackItem>();
			foreach (TracyCpuZoneSummary zone in threadGroup
				.OrderBy(zone => zone.Start)
				.ThenByDescending(zone => zone.End)
				.ThenBy(zone => zone.Depth))
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
					omittedNodeCount++;
					omittedByDurationCount++;
					continue;
				}

				while (stack.Count > 0)
				{
					FrameDetailStackItem top = stack[stack.Count - 1];
					if (top.Depth < zone.Depth && top.End > clippedStart)
					{
						break;
					}
					stack.RemoveAt(stack.Count - 1);
				}

				int displayDepth = stack.Count;
				if (displayDepth >= maxDepth)
				{
					omittedNodeCount++;
					omittedByDepthCount++;
					continue;
				}

				if (includedNodeCount >= maxNodes)
				{
					omittedNodeCount++;
					truncated = true;
					continue;
				}

				Dictionary<string, object> node = BuildFrameDetailNode(zone, threadName, frame, clippedStart, clippedEnd, displayDepth);
				if (stack.Count == 0)
				{
					roots.Add(node);
				}
				else
				{
					((ArrayList)stack[stack.Count - 1].Node["children"]).Add(node);
				}
				stack.Add(new FrameDetailStackItem(zone.Depth, clippedEnd, node));
				flatSpans.Add(BuildFrameDetailSpan(zone, threadName, frame, clippedStart, clippedEnd, displayDepth));
				includedNodeCount++;
			}

			if (roots.Count > 0)
			{
				threadGraphs.Add(new Dictionary<string, object>
				{
					["threadId"] = threadGroup.Key,
					["threadName"] = threadName,
					["roots"] = roots
				});
			}
		}

		return threadGraphs;
	}

	private ArrayList BuildFrameCounters(TracyFrameSummary frame, int frameIndex, int maxCounters)
	{
		List<Dictionary<string, object>> counters = new List<Dictionary<string, object>>();
		foreach (TracyPlotSummary plot in m_EventStream.Plots)
		{
			List<TracyPlotSample> samples = plot.Samples
				.Where(sample => sample.Time >= frame.Start && sample.Time <= frame.End)
				.ToList();
			if (samples.Count == 0)
			{
				continue;
			}

			double lastValue = samples[samples.Count - 1].Value;
			counters.Add(new Dictionary<string, object>
			{
				["name"] = plot.Name,
				["valueType"] = "Double",
				["format"] = plot.Format,
				["type"] = plot.Type,
				["frameIndex"] = frameIndex,
				["frameStartTime"] = frame.Start,
				["frameEndTime"] = frame.End,
				["value"] = Round(lastValue),
				["count"] = samples.Count,
				["minValue"] = Round(samples.Min(sample => sample.Value)),
				["maxValue"] = Round(samples.Max(sample => sample.Value)),
				["sumValue"] = Round(samples.Sum(sample => sample.Value))
			});
		}

		return ToArrayList(counters
			.OrderByDescending(counter => Convert.ToInt32(counter["count"]))
			.ThenByDescending(counter => Math.Abs(Convert.ToDouble(counter["value"])))
			.ThenBy(counter => Convert.ToString(counter["name"]))
			.Take(Math.Max(1, maxCounters)));
	}

	private static Dictionary<string, object> BuildFrameDetailNode(TracyCpuZoneSummary zone, string threadName, TracyFrameSummary frame, long clippedStart, long clippedEnd, int depth)
	{
		Dictionary<string, object> node = BuildFrameDetailSpan(zone, threadName, frame, clippedStart, clippedEnd, depth);
		node["children"] = new ArrayList();
		return node;
	}

	private static Dictionary<string, object> BuildFrameDetailSpan(TracyCpuZoneSummary zone, string threadName, TracyFrameSummary frame, long clippedStart, long clippedEnd, int depth)
	{
		long duration = clippedEnd - clippedStart;
		long selfDuration = Math.Min(zone.SelfDuration, duration);
		return new Dictionary<string, object>
		{
			["threadId"] = zone.ThreadId,
			["threadName"] = threadName,
			["name"] = zone.Name,
			["sourceLocation"] = zone.SourceLocation,
			["start"] = clippedStart,
			["end"] = clippedEnd,
			["startTime"] = zone.Start,
			["endTime"] = zone.End,
			["clippedStartTime"] = clippedStart,
			["clippedEndTime"] = clippedEnd,
			["startOffsetMs"] = Round((clippedStart - frame.Start) / 1_000_000.0),
			["relativeStartMs"] = Round((clippedStart - frame.Start) / 1_000_000.0),
			["relativeEndMs"] = Round((clippedEnd - frame.Start) / 1_000_000.0),
			["durationMs"] = Round(duration / 1_000_000.0),
			["selfMs"] = Round(selfDuration / 1_000_000.0),
			["depth"] = depth
		};
	}

	private static string GetThreadName(Dictionary<ulong, string> threadNames, ulong threadId)
	{
		return threadNames.TryGetValue(threadId, out string name) && !string.IsNullOrWhiteSpace(name)
			? name
			: threadId.ToString();
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
			int skipFrames = GetSystemFrameSkipCount(frameSet);
			foreach (TracyFrameSummary frame in frameSet.Frames.Skip(skipFrames))
			{
				yield return new TracyFrameSummary(globalFrameIndex++, frame.Start, frame.End);
			}
		}
	}

	private int GetSystemFrameSkipCount(TracyFrameSetSummary frameSet)
	{
		if (frameSet == null || frameSet.Name != 0 || m_EventStream.Metadata == null || !m_EventStream.Metadata.OnDemand)
		{
			return 0;
		}
		return Math.Min(2, frameSet.Frames.Count);
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

	private Dictionary<string, object> GpuZoneToDictionary(TracyGpuZoneSummary zone)
	{
		TracyGpuContextSummary context = m_EventStream.GpuContexts.FirstOrDefault(value => value.Context == zone.Context);
		return new Dictionary<string, object>
		{
			["context"] = zone.Context,
			["contextName"] = context == null ? "GPU Context " + zone.Context : context.Name,
			["queryId"] = zone.QueryId,
			["threadId"] = zone.ThreadId,
			["name"] = zone.Name,
			["sourceLocation"] = zone.SourceLocation,
			["start"] = zone.Start,
			["end"] = zone.End,
			["durationMs"] = Round(zone.Duration / 1_000_000.0),
			["timeSource"] = zone.TimeSource
		};
	}

	private static Dictionary<string, object> GpuContextToDictionary(TracyGpuContextSummary context)
	{
		return new Dictionary<string, object>
		{
			["context"] = context.Context,
			["name"] = context.Name,
			["threadId"] = context.ThreadId,
			["period"] = context.Period,
			["type"] = context.Type,
			["flags"] = context.Flags,
			["cpuTime"] = context.CpuTime,
			["gpuTime"] = context.GpuTime
		};
	}

	private Dictionary<string, object> BuildGpuRange(int startFrame, int endFrame, long startTimeNs, long endTimeNs)
	{
		if (startTimeNs > 0L || endTimeNs > 0L)
		{
			return new Dictionary<string, object>
			{
				["startTimeNs"] = Math.Min(startTimeNs, endTimeNs),
				["endTimeNs"] = Math.Max(startTimeNs, endTimeNs),
				["timeSource"] = "trace-relative-ns"
			};
		}
		Dictionary<string, object> range = BuildFrameRange(startFrame, endFrame);
		range["timeSource"] = "frame-range";
		return range;
	}

	private static Dictionary<string, object> ThreadToDictionary(TracyThreadSummary thread)
	{
		return new Dictionary<string, object>
		{
			["threadId"] = thread.ThreadId,
			["name"] = thread.Name,
			["processId"] = 0
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

	private static ArrayList GetArrayList(Dictionary<string, object> values, string key)
	{
		return values.TryGetValue(key, out object value) && value is ArrayList list ? list : new ArrayList();
	}

	private Dictionary<string, object> BuildFrameRange(int startFrame, int endFrame)
	{
		if (TryResolveMetadataFrameRange(startFrame, endFrame, out _, out int first, out int last))
		{
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

	private bool TryResolveMetadataFrameRange(int startFrame, int endFrame, out List<TracyFrameSummary> frames, out int first, out int last)
	{
		frames = GetMetadataFrames().ToList();
		if (frames.Count == 0)
		{
			first = 0;
			last = 0;
			return false;
		}

		first = startFrame < 0 ? 0 : Math.Max(0, Math.Min(startFrame, frames.Count - 1));
		last = endFrame < 0 ? frames.Count - 1 : Math.Max(first, Math.Min(endFrame, frames.Count - 1));
		return true;
	}

	private static bool HasExplicitFrameRange(int startFrame, int endFrame)
	{
		return startFrame >= 0 || endFrame >= 0;
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
