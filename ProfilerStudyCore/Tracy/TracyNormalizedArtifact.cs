using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProfilerStudy.Trace;

namespace ProfilerStudy.Tracy;

public static class TracyNormalizedArtifact
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = false };

	public static void Write(string normalizedDirectory, TraceDocument document, TracyTraceQuerySession querySession, ArrayList diagnostics)
	{
		Directory.CreateDirectory(normalizedDirectory);
		TracyEventStream eventStream = querySession.EventStream;
		List<TracyFrameSummary> frames = querySession.GetVisibleFrames().ToList();
		long startTime = ResolveStartTime(eventStream, frames);
		long endTime = ResolveEndTime(eventStream, frames);

		WriteJson(Path.Combine(normalizedDirectory, "manifest.json"), BuildManifest(document, eventStream, frames, startTime, endTime));
		WriteLines(Path.Combine(normalizedDirectory, "threads.ndjson"), BuildThreads(eventStream));
		WriteLines(Path.Combine(normalizedDirectory, "frames.ndjson"), frames.Select(FrameToRecord));
		WriteLines(Path.Combine(normalizedDirectory, "cpu_zones.ndjson"), eventStream.CpuZones.Select(ZoneToRecord));
		WriteLines(Path.Combine(normalizedDirectory, "plots.ndjson"), BuildPlotRecords(eventStream));
		WriteJson(Path.Combine(normalizedDirectory, "diagnostics.json"), diagnostics ?? new ArrayList());
	}

	public static TraceDocument Load(string artifactDirectory, TraceArtifactManifest artifactManifest)
	{
		string normalizedDirectory = ResolveNormalizedDirectory(artifactDirectory, artifactManifest.NormalizedPath ?? "normalized");
		Dictionary<string, object> manifest = ReadDictionary(Path.Combine(normalizedDirectory, "manifest.json"));
		ArrayList diagnostics = ReadArrayList(Path.Combine(normalizedDirectory, "diagnostics.json"));
		List<TracyFrameSummary> frames = ReadFrames(Path.Combine(normalizedDirectory, "frames.ndjson"));
		List<TracyCpuZoneSummary> zones = ReadZones(Path.Combine(normalizedDirectory, "cpu_zones.ndjson"));
		List<TracyPlotSummary> plots = ReadPlots(Path.Combine(normalizedDirectory, "plots.ndjson"));
		List<TracyThreadSummary> threads = ReadThreads(Path.Combine(normalizedDirectory, "threads.ndjson"), zones);
		TracyTraceMetadata metadata = BuildMetadata(manifest, frames);
		TracyFileHeader header = new TracyFileHeader(GetString(manifest, "tracyVersion", artifactManifest.TracyVersion), GetString(manifest, "compression", "lz4"));
		TracyEventStream eventStream = new TracyEventStream(
			header,
			GetInt(manifest, "compressedBlockCount", 0),
			GetLong(manifest, "compressedByteCount", 0L),
			GetLong(manifest, "decodedByteCount", 0L),
			GetLong(manifest, "payloadByteCount", 0L),
			metadata,
			zones,
			plots,
			threads,
			GetInt(manifest, "threadCount", threads.Count),
			diagnostics);
		TracyTraceQuerySession querySession = new TracyTraceQuerySession(artifactManifest.SourcePath ?? artifactManifest.ArtifactId, eventStream, diagnostics);
		return new TraceDocument(
			Guid.NewGuid().ToString("N"),
			artifactManifest.SourcePath ?? artifactManifest.ArtifactId,
			"tracy",
			artifactManifest.ArtifactId,
			artifactManifest.GetCreatedUtc() == DateTime.MinValue ? DateTime.UtcNow : artifactManifest.GetCreatedUtc(),
			querySession,
			diagnostics,
			artifactManifest.ArtifactId);
	}

	private static string ResolveNormalizedDirectory(string artifactDirectory, string relativePath)
	{
		string fullArtifactDirectory = Path.GetFullPath(artifactDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string fullPath = Path.GetFullPath(Path.Combine(fullArtifactDirectory, relativePath ?? string.Empty));
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		if (!fullPath.Equals(fullArtifactDirectory, comparison) && !fullPath.StartsWith(fullArtifactDirectory + Path.DirectorySeparatorChar, comparison))
		{
			throw new InvalidOperationException("Trace artifact normalized path escapes the artifact directory.");
		}
		return fullPath;
	}

	private static Dictionary<string, object> BuildManifest(TraceDocument document, TracyEventStream eventStream, List<TracyFrameSummary> frames, long startTime, long endTime)
	{
		TracyTraceMetadata metadata = eventStream.Metadata;
		return new Dictionary<string, object>
		{
			["schemaVersion"] = 1,
			["sourceFormat"] = "tracy",
			["sourcePath"] = document.SourcePath,
			["implementation"] = "ProfilerStudyCore.Tracy",
			["readerVersion"] = "0.1.0",
			["tracyVersion"] = eventStream.Version,
			["compression"] = eventStream.Compression,
			["startTimeNs"] = startTime,
			["endTimeNs"] = endTime,
			["timeBase"] = "trace-relative-ns",
			["frameSource"] = frames.Count > 0 ? "tracy-frame-set-metadata" : "none",
			["framesAreVisible"] = true,
			["threadCount"] = eventStream.ThreadCount,
			["frameCount"] = frames.Count,
			["zoneCount"] = eventStream.CpuZones.Count,
			["plotCount"] = eventStream.Plots.Count,
			["compressedBlockCount"] = eventStream.CompressedBlockCount,
			["compressedByteCount"] = eventStream.CompressedByteCount,
			["decodedByteCount"] = eventStream.DecodedByteCount,
			["payloadByteCount"] = eventStream.PayloadByteCount,
			["metadataDecoded"] = eventStream.HasMetadata,
			["delay"] = metadata == null ? 0L : metadata.Delay,
			["resolution"] = metadata == null ? 0L : metadata.Resolution,
			["timerMultiplier"] = metadata == null ? 0.0 : metadata.TimerMultiplier,
			["lastTime"] = metadata == null ? 0L : metadata.LastTime,
			["frameOffset"] = metadata == null ? 0L : metadata.FrameOffset,
			["processId"] = metadata == null ? 0UL : metadata.ProcessId,
			["samplingPeriod"] = metadata == null ? 0L : metadata.SamplingPeriod,
			["cpuArchitecture"] = metadata == null ? 0 : metadata.CpuArchitecture,
			["cpuId"] = metadata == null ? 0U : metadata.CpuId,
			["cpuManufacturer"] = metadata == null ? string.Empty : metadata.CpuManufacturer,
			["onDemand"] = metadata != null && metadata.OnDemand,
			["captureName"] = metadata == null ? string.Empty : metadata.CaptureName,
			["captureProgram"] = metadata == null ? string.Empty : metadata.CaptureProgram,
			["captureTime"] = metadata == null ? 0L : metadata.CaptureTime,
			["executableTime"] = metadata == null ? 0L : metadata.ExecutableTime,
			["hostInfo"] = metadata == null ? string.Empty : metadata.HostInfo,
			["stringCount"] = metadata == null ? 0 : metadata.StringCount,
			["threadNameCount"] = metadata == null ? 0 : metadata.ThreadNameCount
		};
	}

	private static IEnumerable<Dictionary<string, object>> BuildThreads(TracyEventStream eventStream)
	{
		foreach (TracyThreadSummary thread in eventStream.Threads.OrderBy(value => value.ThreadId))
		{
			yield return new Dictionary<string, object>
			{
				["threadId"] = thread.ThreadId,
				["name"] = thread.Name,
				["processId"] = 0
			};
		}
	}

	private static IEnumerable<Dictionary<string, object>> BuildPlotRecords(TracyEventStream eventStream)
	{
		foreach (TracyPlotSummary plot in eventStream.Plots)
		{
			foreach (TracyPlotSample sample in plot.Samples)
			{
				yield return new Dictionary<string, object>
				{
					["counterName"] = plot.Name,
					["timeNs"] = sample.Time,
					["value"] = sample.Value,
					["type"] = plot.Type,
					["format"] = plot.Format,
					["min"] = plot.Min,
					["max"] = plot.Max,
					["sum"] = plot.Sum
				};
			}
		}
	}

	private static Dictionary<string, object> FrameToRecord(TracyFrameSummary frame)
	{
		return new Dictionary<string, object>
		{
			["frameIndex"] = frame.FrameIndex,
			["name"] = "Frame",
			["startNs"] = frame.Start,
			["endNs"] = frame.End,
			["durationNs"] = frame.Duration
		};
	}

	private static Dictionary<string, object> ZoneToRecord(TracyCpuZoneSummary zone)
	{
		return new Dictionary<string, object>
		{
			["threadId"] = zone.ThreadId,
			["sourceLocation"] = zone.SourceLocation,
			["name"] = zone.Name,
			["startNs"] = zone.Start,
			["endNs"] = zone.End,
			["durationNs"] = zone.Duration,
			["depth"] = 0
		};
	}

	private static long ResolveStartTime(TracyEventStream eventStream, List<TracyFrameSummary> frames)
	{
		List<long> values = new List<long>();
		values.AddRange(frames.Select(frame => frame.Start));
		values.AddRange(eventStream.CpuZones.Select(zone => zone.Start));
		foreach (TracyPlotSummary plot in eventStream.Plots)
		{
			values.AddRange(plot.Samples.Select(sample => sample.Time));
		}
		return values.Count == 0 ? 0L : values.Min();
	}

	private static long ResolveEndTime(TracyEventStream eventStream, List<TracyFrameSummary> frames)
	{
		List<long> values = new List<long>();
		values.AddRange(frames.Select(frame => frame.End));
		values.AddRange(eventStream.CpuZones.Select(zone => zone.End));
		foreach (TracyPlotSummary plot in eventStream.Plots)
		{
			values.AddRange(plot.Samples.Select(sample => sample.Time));
		}
		return values.Count == 0 ? 0L : values.Max();
	}

	private static TracyTraceMetadata BuildMetadata(Dictionary<string, object> manifest, List<TracyFrameSummary> frames)
	{
		bool metadataDecoded = GetBool(manifest, "metadataDecoded", false);
		if (!metadataDecoded && frames.Count == 0)
		{
			return null;
		}

		List<TracyFrameSetSummary> frameSets = new List<TracyFrameSetSummary>();
		if (frames.Count > 0)
		{
			ulong frameSetName = GetBool(manifest, "framesAreVisible", false) ? ulong.MaxValue : 0UL;
			frameSets.Add(new TracyFrameSetSummary(frameSetName, false, frames));
		}
		return new TracyTraceMetadata(
			GetLong(manifest, "delay", 0L),
			GetLong(manifest, "resolution", 0L),
			GetDouble(manifest, "timerMultiplier", 0.0),
			GetLong(manifest, "lastTime", 0L),
			GetLong(manifest, "frameOffset", 0L),
			GetULong(manifest, "processId", 0UL),
			GetLong(manifest, "samplingPeriod", 0L),
			(byte)GetInt(manifest, "cpuArchitecture", 0),
			(uint)GetInt(manifest, "cpuId", 0),
			GetString(manifest, "cpuManufacturer", string.Empty),
			GetBool(manifest, "onDemand", false),
			GetString(manifest, "captureName", string.Empty),
			GetString(manifest, "captureProgram", string.Empty),
			GetLong(manifest, "captureTime", 0L),
			GetLong(manifest, "executableTime", 0L),
			GetString(manifest, "hostInfo", string.Empty),
			frameSets,
			GetInt(manifest, "stringCount", 0),
			GetInt(manifest, "threadNameCount", 0));
	}

	private static List<TracyFrameSummary> ReadFrames(string path)
	{
		List<TracyFrameSummary> frames = new List<TracyFrameSummary>();
		foreach (Dictionary<string, object> record in ReadLineDictionaries(path))
		{
			frames.Add(new TracyFrameSummary(
				GetInt(record, "frameIndex", frames.Count),
				GetLong(record, "startNs", 0L),
				GetLong(record, "endNs", 0L)));
		}
		return frames;
	}

	private static List<TracyCpuZoneSummary> ReadZones(string path)
	{
		List<TracyCpuZoneSummary> zones = new List<TracyCpuZoneSummary>();
		foreach (Dictionary<string, object> record in ReadLineDictionaries(path))
		{
			zones.Add(new TracyCpuZoneSummary(
				GetULong(record, "threadId", 0UL),
				(short)GetInt(record, "sourceLocation", -1),
				GetString(record, "name", string.Empty),
				GetLong(record, "startNs", 0L),
				GetLong(record, "endNs", 0L)));
		}
		return zones;
	}

	private static List<TracyThreadSummary> ReadThreads(string path, IReadOnlyList<TracyCpuZoneSummary> zones)
	{
		Dictionary<ulong, string> names = new Dictionary<ulong, string>();
		foreach (Dictionary<string, object> record in ReadLineDictionaries(path))
		{
			ulong threadId = GetULong(record, "threadId", 0UL);
			names[threadId] = GetString(record, "name", string.Empty);
		}

		foreach (TracyCpuZoneSummary zone in zones)
		{
			if (!names.ContainsKey(zone.ThreadId))
			{
				names[zone.ThreadId] = string.Empty;
			}
		}

		return names
			.OrderBy(pair => pair.Key)
			.Select(pair => new TracyThreadSummary(pair.Key, pair.Value))
			.ToList();
	}

	private static List<TracyPlotSummary> ReadPlots(string path)
	{
		List<TracyPlotSummary> plots = new List<TracyPlotSummary>();
		foreach (IGrouping<string, Dictionary<string, object>> group in ReadLineDictionaries(path).GroupBy(record => GetString(record, "counterName", string.Empty)))
		{
			List<TracyPlotSample> samples = group
				.Select(record => new TracyPlotSample(GetLong(record, "timeNs", 0L), GetDouble(record, "value", 0.0)))
				.ToList();
			Dictionary<string, object> first = group.First();
			double min = samples.Count == 0 ? 0.0 : samples.Min(sample => sample.Value);
			double max = samples.Count == 0 ? 0.0 : samples.Max(sample => sample.Value);
			double sum = samples.Sum(sample => sample.Value);
			plots.Add(new TracyPlotSummary(
				group.Key,
				(byte)GetInt(first, "type", 0),
				(byte)GetInt(first, "format", 0),
				GetDouble(first, "min", min),
				GetDouble(first, "max", max),
				GetDouble(first, "sum", sum),
				samples));
		}
		return plots;
	}

	private static IEnumerable<Dictionary<string, object>> ReadLineDictionaries(string path)
	{
		if (!File.Exists(path))
		{
			yield break;
		}

		foreach (string line in File.ReadLines(path))
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}
			yield return ReadDictionaryFromJson(line);
		}
	}

	private static Dictionary<string, object> ReadDictionary(string path)
	{
		return ReadDictionaryFromJson(File.ReadAllText(path));
	}

	private static Dictionary<string, object> ReadDictionaryFromJson(string json)
	{
		using JsonDocument document = JsonDocument.Parse(json);
		return ToDictionary(document.RootElement);
	}

	private static ArrayList ReadArrayList(string path)
	{
		if (!File.Exists(path))
		{
			return new ArrayList();
		}
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
		return ToArrayList(document.RootElement);
	}

	private static Dictionary<string, object> ToDictionary(JsonElement element)
	{
		Dictionary<string, object> values = new Dictionary<string, object>();
		foreach (JsonProperty property in element.EnumerateObject())
		{
			values[property.Name] = ToObject(property.Value);
		}
		return values;
	}

	private static ArrayList ToArrayList(JsonElement element)
	{
		ArrayList values = new ArrayList();
		if (element.ValueKind != JsonValueKind.Array)
		{
			return values;
		}
		foreach (JsonElement item in element.EnumerateArray())
		{
			values.Add(ToObject(item));
		}
		return values;
	}

	private static object ToObject(JsonElement element)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				return ToDictionary(element);
			case JsonValueKind.Array:
				return ToArrayList(element);
			case JsonValueKind.String:
				return element.GetString();
			case JsonValueKind.Number:
				if (element.TryGetInt64(out long longValue))
				{
					return longValue;
				}
				return element.GetDouble();
			case JsonValueKind.True:
				return true;
			case JsonValueKind.False:
				return false;
			default:
				return null;
		}
	}

	private static void WriteJson(string path, object value)
	{
		File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions));
	}

	private static void WriteLines(string path, IEnumerable<Dictionary<string, object>> records)
	{
		using StreamWriter writer = new StreamWriter(path);
		foreach (Dictionary<string, object> record in records)
		{
			writer.WriteLine(JsonSerializer.Serialize(record, JsonOptions));
		}
	}

	private static string GetString(Dictionary<string, object> values, string key, string defaultValue)
	{
		return values.TryGetValue(key, out object value) && value != null ? Convert.ToString(value) : defaultValue;
	}

	private static int GetInt(Dictionary<string, object> values, string key, int defaultValue)
	{
		return values.TryGetValue(key, out object value) && value != null ? Convert.ToInt32(value) : defaultValue;
	}

	private static long GetLong(Dictionary<string, object> values, string key, long defaultValue)
	{
		return values.TryGetValue(key, out object value) && value != null ? Convert.ToInt64(value) : defaultValue;
	}

	private static ulong GetULong(Dictionary<string, object> values, string key, ulong defaultValue)
	{
		return values.TryGetValue(key, out object value) && value != null ? Convert.ToUInt64(value) : defaultValue;
	}

	private static double GetDouble(Dictionary<string, object> values, string key, double defaultValue)
	{
		return values.TryGetValue(key, out object value) && value != null ? Convert.ToDouble(value) : defaultValue;
	}

	private static bool GetBool(Dictionary<string, object> values, string key, bool defaultValue)
	{
		return values.TryGetValue(key, out object value) && value != null ? Convert.ToBoolean(value) : defaultValue;
	}
}
