using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ProfilerStudy.Tracy;

public sealed class Tracy010FileWriteSummary
{
	public Tracy010FileWriteSummary(
		long byteCount,
		int frameCount,
		int cpuZoneCount,
		int plotCount,
		int threadCount,
		int gpuZoneCount,
		int gpuContextCount,
		string writerCompatibility,
		Dictionary<string, object> writerCoverage)
	{
		ByteCount = byteCount;
		FrameCount = frameCount;
		CpuZoneCount = cpuZoneCount;
		PlotCount = plotCount;
		ThreadCount = threadCount;
		GpuZoneCount = gpuZoneCount;
		GpuContextCount = gpuContextCount;
		WriterCompatibility = writerCompatibility;
		WriterCoverage = writerCoverage;
	}

	public long ByteCount { get; }

	public int FrameCount { get; }

	public int CpuZoneCount { get; }

	public int PlotCount { get; }

	public int ThreadCount { get; }

	public int GpuZoneCount { get; }

	public int GpuContextCount { get; }

	public string WriterCompatibility { get; }

	public Dictionary<string, object> WriterCoverage { get; }
}

public static class Tracy010FileWriter
{
	private const int MaxDecodedBlockSize = 60 * 1024;
	private const ulong StringPointerBase = 0x1000UL;
	private const ulong SourceLocationPointerBase = 0x4000UL;

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct MetadataFixedPrefix
	{
		public long Delay;
		public long Resolution;
		public double TimerMultiplier;
		public long LastTime;
		public long FrameOffset;
		public ulong ProcessId;
		public long SamplingPeriod;
		public byte CpuArchitecture;
		public uint CpuId;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct CrashEvent
	{
		public ulong Thread;
		public long Time;
		public ulong Message;
		public uint Callstack;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct TracyStringRef
	{
		public ulong Value;
		public byte Flags;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct SourceLocationBase
	{
		public TracyStringRef Name;
		public TracyStringRef Function;
		public TracyStringRef File;
		public uint Line;
		public uint Color;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct SourceLocationZoneReservation
	{
		public short SourceLocation;
		public ulong ZoneCount;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct ZoneExtra
	{
		public ulong Text;
		public uint Color;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct CpuThreadHeader
	{
		public ulong ThreadId;
		public ulong ZoneCount;
		public ulong KernelSampleCount;
		public byte IsFiber;
		public uint TimelineSize;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct CpuZoneHeader
	{
		public short SourceLocation;
		public long StartDelta;
		public uint Extra;
		public uint ChildSize;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct GpuEmptySection
	{
		public ulong TotalZoneCount;
		public ulong GpuChildrenCount;
		public ulong GpuDataCount;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct GpuContextHeader
	{
		public ulong ThreadId;
		public byte HasCalibration;
		public ulong ZoneCount;
		public float Period;
		public byte Type;
		public uint NameIndex;
		public ulong Overflow;
		public ulong ThreadDataCount;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct GpuThreadHeader
	{
		public ulong ThreadId;
		public ulong TimelineSize;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct GpuZoneHeader
	{
		public long CpuStartDelta;
		public long GpuStartDelta;
		public short SourceLocation;
		public byte Callstack0;
		public byte Callstack1;
		public byte Callstack2;
		public short Thread;
		public ulong ChildSize;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct GpuZoneEnd
	{
		public long CpuEndDelta;
		public long GpuEndDelta;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct PlotHeader
	{
		public byte Type;
		public byte Format;
		public byte ShowSteps;
		public byte Fill;
		public uint Color;
		public ulong NamePointer;
		public double Min;
		public double Max;
		public double Sum;
		public ulong SampleCount;
	}

	public static Tracy010FileWriteSummary Write(string path, TracyTraceQuerySession querySession)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Tracy output path is required.", nameof(path));
		}
		if (querySession == null)
		{
			throw new ArgumentNullException(nameof(querySession));
		}
		if (!BitConverter.IsLittleEndian)
		{
			throw new PlatformNotSupportedException("Tracy 0.10.0 writer requires a little-endian runtime.");
		}

		TracyEventStream eventStream = querySession.EventStream;
		List<TracyFrameSummary> frames = querySession.GetVisibleFrames().ToList();
		List<TracyCpuZoneSummary> cpuZones = eventStream.CpuZones.OrderBy(zone => zone.ThreadId).ThenBy(zone => zone.Start).ThenByDescending(zone => zone.End).ToList();
		List<TracyGpuZoneSummary> gpuZones = eventStream.GpuZones.OrderBy(zone => zone.Context).ThenBy(zone => zone.ThreadId).ThenBy(zone => zone.Start).ToList();
		List<WriterGpuContext> gpuContexts = BuildGpuContexts(eventStream.GpuContexts, gpuZones);
		List<TracyPlotSummary> plots = eventStream.Plots.ToList();
		List<TracyThreadSummary> threads = BuildThreadList(eventStream, cpuZones);
		StringTable strings = BuildStringTable(cpuZones, gpuZones, gpuContexts, plots, threads, eventStream.SourceLocations);
		strings.SetThreads(threads);
		List<WriterSourceLocation> sourceLocations = BuildSourceLocations(cpuZones, gpuZones, eventStream.SourceLocations, strings, out Dictionary<string, short> sourceLocationIds);
		Dictionary<ulong, ushort> compressedThreads = BuildCompressedThreadMap(cpuZones.Select(zone => zone.ThreadId).Concat(gpuZones.Select(zone => (ulong)zone.ThreadId)));

		using MemoryStream inner = new MemoryStream();
		WriteHeaderAndMetadata(inner, eventStream, frames, strings);
		WriteThreadCompress(inner, compressedThreads.Keys);
		WriteUInt64(inner, 0); // externalThreadCompress size
		WriteSourceLocations(inner, sourceLocations);
		WriteUInt64(inner, 0); // lockMap count
		WriteUInt64(inner, 0); // messages count
		WriteUInt64(inner, 1); // zoneExtra count
		WriteStruct(inner, new ZoneExtra()); // zoneExtra section keeps the layout explicit even when no zone has extra text/color.
		WriteCpuZones(inner, cpuZones, sourceLocationIds);
		WriteGpuZones(inner, gpuContexts, gpuZones, sourceLocationIds, compressedThreads, strings);
		WritePlots(inner, plots, strings);
		WriteTracyDump(path, inner.ToArray());

		long byteCount = new FileInfo(path).Length;
		Dictionary<string, object> coverage = new Dictionary<string, object>
		{
			["metadataPreserved"] = eventStream.HasMetadata,
			["frameCount"] = frames.Count,
			["cpuZoneCount"] = cpuZones.Count,
			["cpuHierarchyPreserved"] = false,
			["plotCount"] = plots.Count,
			["threadCount"] = threads.Count,
			["sourceLocationCount"] = sourceLocations.Count,
			["gpuContextCount"] = eventStream.GpuContexts.Count,
			["gpuZoneCount"] = eventStream.GpuZones.Count,
			["gpuZonesPreserved"] = eventStream.GpuZones.Count == gpuZones.Count,
			["hardwareSamplesPreserved"] = false,
			["sourceByteExact"] = false
		};
		return new Tracy010FileWriteSummary(
			byteCount,
			frames.Count,
			cpuZones.Count,
			plots.Count,
			threads.Count,
			eventStream.GpuZones.Count,
			eventStream.GpuContexts.Count,
			"mcp-readable-tracy-0.10.0",
			coverage);
	}

	private static List<TracyThreadSummary> BuildThreadList(TracyEventStream eventStream, IReadOnlyList<TracyCpuZoneSummary> cpuZones)
	{
		Dictionary<ulong, string> names = new Dictionary<ulong, string>();
		foreach (TracyThreadSummary thread in eventStream.Threads)
		{
			names[thread.ThreadId] = thread.Name;
		}
		foreach (TracyCpuZoneSummary zone in cpuZones)
		{
			if (!names.ContainsKey(zone.ThreadId))
			{
				names[zone.ThreadId] = string.Empty;
			}
		}
		foreach (TracyGpuZoneSummary zone in eventStream.GpuZones)
		{
			ulong threadId = zone.ThreadId;
			if (threadId != 0 && !names.ContainsKey(threadId))
			{
				names[threadId] = string.Empty;
			}
		}
		return names
			.OrderBy(pair => pair.Key)
			.Select(pair => new TracyThreadSummary(pair.Key, string.IsNullOrWhiteSpace(pair.Value) ? "Thread " + pair.Key : pair.Value))
			.ToList();
	}

	private static StringTable BuildStringTable(
		IEnumerable<TracyCpuZoneSummary> cpuZones,
		IEnumerable<TracyGpuZoneSummary> gpuZones,
		IEnumerable<WriterGpuContext> gpuContexts,
		IEnumerable<TracyPlotSummary> plots,
		IEnumerable<TracyThreadSummary> threads,
		IEnumerable<TracySourceLocationSummary> sourceLocations)
	{
		StringTable strings = new StringTable();
		foreach (TracyCpuZoneSummary zone in cpuZones)
		{
			strings.Add(NormalizeText(zone.Name, "Zone " + zone.SourceLocation));
		}
		foreach (TracyGpuZoneSummary zone in gpuZones)
		{
			strings.Add(NormalizeText(zone.Name, "GPU Zone " + zone.SourceLocation));
		}
		foreach (WriterGpuContext context in gpuContexts)
		{
			strings.Add(NormalizeText(context.Name, "GPU Context " + context.Context));
		}
		foreach (TracyPlotSummary plot in plots)
		{
			strings.Add(NormalizeText(plot.Name, "Plot"));
		}
		foreach (TracyThreadSummary thread in threads)
		{
			strings.Add(NormalizeText(thread.Name, "Thread " + thread.ThreadId));
		}
		foreach (TracySourceLocationSummary sourceLocation in sourceLocations)
		{
			strings.Add(NormalizeText(sourceLocation.Name, "SourceLocation " + sourceLocation.Id));
			strings.Add(NormalizeText(sourceLocation.Function, sourceLocation.Name));
			strings.Add(NormalizeText(sourceLocation.File, string.Empty));
		}
		return strings;
	}

	private static List<WriterGpuContext> BuildGpuContexts(IReadOnlyList<TracyGpuContextSummary> decodedContexts, IReadOnlyList<TracyGpuZoneSummary> gpuZones)
	{
		Dictionary<byte, WriterGpuContext> contexts = new Dictionary<byte, WriterGpuContext>();
		foreach (TracyGpuContextSummary context in decodedContexts)
		{
			contexts[context.Context] = new WriterGpuContext(
				context.Context,
				NormalizeText(context.Name, "GPU Context " + context.Context),
				context.ThreadId,
				context.Period <= 0.0f ? 1.0f : context.Period,
				context.Type,
				context.Flags);
		}
		foreach (TracyGpuZoneSummary zone in gpuZones)
		{
			if (!contexts.ContainsKey(zone.Context))
			{
				contexts[zone.Context] = new WriterGpuContext(
					zone.Context,
					"GPU Context " + zone.Context,
					zone.ThreadId,
					1.0f,
					0,
					0);
			}
		}
		return contexts.Values.OrderBy(context => context.Context).ToList();
	}

	private static Dictionary<ulong, ushort> BuildCompressedThreadMap(IEnumerable<ulong> threadIds)
	{
		List<ulong> ids = threadIds
			.Where(id => id != 0)
			.Distinct()
			.OrderBy(id => id)
			.ToList();
		if (ids.Count > short.MaxValue)
		{
			throw new InvalidOperationException("Tracy writer cannot encode more than " + short.MaxValue + " compressed thread ids in one file.");
		}
		Dictionary<ulong, ushort> map = new Dictionary<ulong, ushort>();
		for (int index = 0; index < ids.Count; index++)
		{
			map[ids[index]] = checked((ushort)index);
		}
		return map;
	}

	private static List<WriterSourceLocation> BuildSourceLocations(
		IReadOnlyList<TracyCpuZoneSummary> cpuZones,
		IReadOnlyList<TracyGpuZoneSummary> gpuZones,
		IReadOnlyList<TracySourceLocationSummary> decodedSourceLocations,
		StringTable strings,
		out Dictionary<string, short> zoneSourceLocationIds)
	{
		Dictionary<short, TracySourceLocationSummary> decodedById = decodedSourceLocations.ToDictionary(sourceLocation => sourceLocation.Id, sourceLocation => sourceLocation);
		Dictionary<string, WriterSourceLocation> locationsByKey = new Dictionary<string, WriterSourceLocation>();
		zoneSourceLocationIds = new Dictionary<string, short>();
		foreach (TracyCpuZoneSummary zone in cpuZones)
		{
			string key = SourceLocationKey(zone);
			if (!locationsByKey.TryGetValue(key, out WriterSourceLocation writerLocation))
			{
				short id = checked((short)locationsByKey.Count);
				TracySourceLocationSummary decoded = decodedById.TryGetValue(zone.SourceLocation, out TracySourceLocationSummary value) ? value : null;
				string name = NormalizeText(decoded == null ? zone.Name : decoded.Name, NormalizeText(zone.Name, "Zone " + zone.SourceLocation));
				string function = NormalizeText(decoded == null ? name : decoded.Function, name);
				string file = decoded == null ? string.Empty : NormalizeText(decoded.File, string.Empty);
				uint line = decoded == null ? 0U : decoded.Line;
				writerLocation = new WriterSourceLocation(
					id,
					SourceLocationPointerBase + (ulong)id * 0x100UL,
					strings.IndexOf(name),
					strings.IndexOf(function),
					string.IsNullOrEmpty(file) ? -1 : strings.IndexOf(file),
					line);
				locationsByKey[key] = writerLocation;
			}
			zoneSourceLocationIds[key] = writerLocation.Id;
			writerLocation.ZoneCount++;
		}
		foreach (TracyGpuZoneSummary zone in gpuZones)
		{
			string key = SourceLocationKey(zone);
			if (!locationsByKey.TryGetValue(key, out WriterSourceLocation writerLocation))
			{
				short id = checked((short)locationsByKey.Count);
				TracySourceLocationSummary decoded = decodedById.TryGetValue(zone.SourceLocation, out TracySourceLocationSummary value) ? value : null;
				string name = NormalizeText(decoded == null ? zone.Name : decoded.Name, NormalizeText(zone.Name, "GPU Zone " + zone.SourceLocation));
				string function = NormalizeText(decoded == null ? name : decoded.Function, name);
				string file = decoded == null ? string.Empty : NormalizeText(decoded.File, string.Empty);
				uint line = decoded == null ? 0U : decoded.Line;
				writerLocation = new WriterSourceLocation(
					id,
					SourceLocationPointerBase + (ulong)id * 0x100UL,
					strings.IndexOf(name),
					strings.IndexOf(function),
					string.IsNullOrEmpty(file) ? -1 : strings.IndexOf(file),
					line);
				locationsByKey[key] = writerLocation;
			}
			zoneSourceLocationIds[key] = writerLocation.Id;
		}
		return locationsByKey.Values.OrderBy(location => location.Id).ToList();
	}

	private static void WriteHeaderAndMetadata(Stream stream, TracyEventStream eventStream, IReadOnlyList<TracyFrameSummary> frames, StringTable strings)
	{
		TracyTraceMetadata metadata = eventStream.Metadata;
		stream.Write(new byte[] { (byte)'t', (byte)'r', (byte)'a', (byte)'c', (byte)'y', 0, 10, 0 }, 0, 8);
		long lastTime = metadata == null ? ResolveLastTime(frames, eventStream) : metadata.LastTime;
		if (lastTime <= 0)
		{
			lastTime = ResolveLastTime(frames, eventStream);
		}
		WriteStruct(stream, new MetadataFixedPrefix
		{
			Delay = metadata == null ? 0L : metadata.Delay,
			Resolution = metadata == null || metadata.Resolution <= 0 ? 1_000_000_000L : metadata.Resolution,
			TimerMultiplier = metadata == null || metadata.TimerMultiplier <= 0.0 ? 1.0 : metadata.TimerMultiplier,
			LastTime = lastTime,
			FrameOffset = metadata == null ? 0L : metadata.FrameOffset,
			ProcessId = metadata == null ? 0UL : metadata.ProcessId,
			SamplingPeriod = metadata == null ? 0L : metadata.SamplingPeriod,
			CpuArchitecture = metadata == null ? (byte)0 : metadata.CpuArchitecture,
			CpuId = metadata == null ? 0U : metadata.CpuId
		});
		WriteFixedAscii(stream, metadata == null ? string.Empty : metadata.CpuManufacturer, 12);
		stream.WriteByte(metadata != null && metadata.OnDemand ? (byte)1 : (byte)0);
		WriteSizedString(stream, metadata == null ? string.Empty : metadata.CaptureName);
		WriteSizedString(stream, metadata == null ? string.Empty : metadata.CaptureProgram);
		WriteInt64(stream, metadata == null ? 0L : metadata.CaptureTime);
		WriteInt64(stream, metadata == null ? 0L : metadata.ExecutableTime);
		WriteSizedString(stream, metadata == null ? string.Empty : metadata.HostInfo);
		WriteUInt64(stream, 0); // cpuTopology package count
		WriteStruct(stream, new CrashEvent());
		WriteFrameSets(stream, frames);
		strings.Write(stream);
		WriteUInt64(stream, 0); // strings map count
		WriteThreadNames(stream, strings);
		WriteUInt64(stream, 0); // externalNames count
	}

	private static void WriteFrameSets(Stream stream, IReadOnlyList<TracyFrameSummary> frames)
	{
		if (frames.Count == 0)
		{
			WriteUInt64(stream, 0);
			return;
		}
		WriteUInt64(stream, 1);
		WriteUInt64(stream, 0); // default frame name
		stream.WriteByte(0);    // separated frame set; keeps every frame end explicit.
		WriteUInt64(stream, (ulong)frames.Count);
		long refTime = 0L;
		foreach (TracyFrameSummary frame in frames)
		{
			WriteTimeOffset(stream, frame.Start, ref refTime);
			WriteTimeOffset(stream, frame.End, ref refTime);
			WriteInt32(stream, -1);
		}
	}

	private static void WriteThreadNames(Stream stream, StringTable strings)
	{
		IReadOnlyList<TracyThreadSummary> threads = strings.Threads;
		WriteUInt64(stream, (ulong)threads.Count);
		foreach (TracyThreadSummary thread in threads)
		{
			WriteUInt64(stream, thread.ThreadId);
			WriteUInt64(stream, strings.PointerOf(NormalizeText(thread.Name, "Thread " + thread.ThreadId)));
		}
	}

	private static void WriteThreadCompress(Stream stream, IEnumerable<ulong> threadIds)
	{
		List<ulong> ids = threadIds.Distinct().OrderBy(id => id).ToList();
		WriteUInt64(stream, (ulong)ids.Count);
		foreach (ulong threadId in ids)
		{
			WriteUInt64(stream, threadId);
		}
	}

	private static void WriteSourceLocations(Stream stream, IReadOnlyList<WriterSourceLocation> sourceLocations)
	{
		WriteUInt64(stream, (ulong)sourceLocations.Count);
		foreach (WriterSourceLocation sourceLocation in sourceLocations)
		{
			WriteUInt64(stream, sourceLocation.Pointer);
			WriteStruct(stream, new SourceLocationBase
			{
				Name = StringRefIndex(sourceLocation.NameIndex),
				Function = StringRefIndex(sourceLocation.FunctionIndex),
				File = sourceLocation.FileIndex >= 0 ? StringRefIndex(sourceLocation.FileIndex) : InactiveStringRef(),
				Line = sourceLocation.Line,
				Color = 0
			});
		}
		WriteUInt64(stream, (ulong)sourceLocations.Count);
		foreach (WriterSourceLocation sourceLocation in sourceLocations)
		{
			WriteUInt64(stream, sourceLocation.Pointer);
		}
		WriteUInt64(stream, 0); // sourceLocationPayload count
		WriteUInt64(stream, (ulong)sourceLocations.Count);
		foreach (WriterSourceLocation sourceLocation in sourceLocations)
		{
			WriteStruct(stream, new SourceLocationZoneReservation
			{
				SourceLocation = sourceLocation.Id,
				ZoneCount = sourceLocation.ZoneCount
			});
		}
		WriteUInt64(stream, 0); // gpuSourceLocationZones count
	}

	private static void WriteCpuZones(Stream stream, IReadOnlyList<TracyCpuZoneSummary> cpuZones, Dictionary<string, short> sourceLocationIds)
	{
		WriteUInt64(stream, (ulong)cpuZones.Count);
		WriteUInt64(stream, 0); // zoneChildren count; this writer flattens the normalized zone list.
		List<IGrouping<ulong, TracyCpuZoneSummary>> zonesByThread = cpuZones
			.GroupBy(zone => zone.ThreadId)
			.OrderBy(group => group.Key)
			.ToList();
		WriteUInt64(stream, (ulong)zonesByThread.Count);
		foreach (IGrouping<ulong, TracyCpuZoneSummary> threadZones in zonesByThread)
		{
			List<TracyCpuZoneSummary> orderedZones = threadZones
				.OrderBy(zone => zone.Start)
				.ThenByDescending(zone => zone.End)
				.ToList();
			WriteStruct(stream, new CpuThreadHeader
			{
				ThreadId = threadZones.Key,
				ZoneCount = (ulong)orderedZones.Count,
				KernelSampleCount = 0,
				IsFiber = 0,
				TimelineSize = checked((uint)orderedZones.Count)
			});
			long refTime = 0L;
			foreach (TracyCpuZoneSummary zone in orderedZones)
			{
				long startDelta = zone.Start - refTime;
				refTime = zone.Start;
				WriteStruct(stream, new CpuZoneHeader
				{
					SourceLocation = sourceLocationIds[SourceLocationKey(zone)],
					StartDelta = startDelta,
					Extra = 0,
					ChildSize = 0
				});
				WriteTimeOffset(stream, zone.End, ref refTime);
			}
			WriteUInt64(stream, 0); // thread messages
			WriteUInt64(stream, 0); // ctxSwitchSamples
			WriteUInt64(stream, 0); // samples
		}
	}

	private static void WriteGpuZones(
		Stream stream,
		IReadOnlyList<WriterGpuContext> gpuContexts,
		IReadOnlyList<TracyGpuZoneSummary> gpuZones,
		Dictionary<string, short> sourceLocationIds,
		Dictionary<ulong, ushort> compressedThreads,
		StringTable strings)
	{
		if (gpuZones.Count == 0 && gpuContexts.Count == 0)
		{
			WriteStruct(stream, new GpuEmptySection());
			return;
		}

		WriteUInt64(stream, (ulong)gpuZones.Count);
		WriteUInt64(stream, 0); // gpuChildren count; this writer flattens the normalized GPU zone list.
		WriteUInt64(stream, (ulong)gpuContexts.Count);
		foreach (WriterGpuContext context in gpuContexts)
		{
			List<TracyGpuZoneSummary> contextZones = gpuZones
				.Where(zone => zone.Context == context.Context)
				.OrderBy(zone => zone.ThreadId)
				.ThenBy(zone => zone.Start)
				.ToList();
			List<IGrouping<uint, TracyGpuZoneSummary>> zonesByThread = contextZones
				.GroupBy(zone => zone.ThreadId)
				.OrderBy(group => group.Key)
				.ToList();
			WriteStruct(stream, new GpuContextHeader
			{
				ThreadId = context.ThreadId,
				HasCalibration = 0,
				ZoneCount = (ulong)contextZones.Count,
				Period = context.Period,
				Type = context.Type,
				NameIndex = checked((uint)strings.IndexOf(NormalizeText(context.Name, "GPU Context " + context.Context))),
				Overflow = 0,
				ThreadDataCount = (ulong)zonesByThread.Count
			});
			foreach (IGrouping<uint, TracyGpuZoneSummary> threadZones in zonesByThread)
			{
				List<TracyGpuZoneSummary> orderedZones = threadZones
					.OrderBy(zone => zone.Start)
					.ThenByDescending(zone => zone.End)
					.ToList();
				WriteStruct(stream, new GpuThreadHeader
				{
					ThreadId = threadZones.Key,
					TimelineSize = (ulong)orderedZones.Count
				});
				long refCpuTime = 0L;
				long refGpuTime = 0L;
				foreach (TracyGpuZoneSummary zone in orderedZones)
				{
					long cpuStart = zone.CpuStart;
					long cpuEnd = zone.CpuEnd >= zone.CpuStart ? zone.CpuEnd : zone.CpuStart;
					long gpuStart = zone.Start;
					long gpuEnd = zone.End >= zone.Start ? zone.End : zone.Start;
					ulong threadId = zone.ThreadId;
					if (!compressedThreads.TryGetValue(threadId, out ushort compressedThread))
					{
						compressedThread = 0;
					}
					WriteStruct(stream, new GpuZoneHeader
					{
						CpuStartDelta = cpuStart - refCpuTime,
						GpuStartDelta = gpuStart - refGpuTime,
						SourceLocation = sourceLocationIds[SourceLocationKey(zone)],
						Callstack0 = 0,
						Callstack1 = 0,
						Callstack2 = 0,
						Thread = checked((short)compressedThread),
						ChildSize = 0
					});
					refCpuTime = cpuStart;
					refGpuTime = gpuStart;
					WriteStruct(stream, new GpuZoneEnd
					{
						CpuEndDelta = cpuEnd - refCpuTime,
						GpuEndDelta = gpuEnd - refGpuTime
					});
					refCpuTime = cpuEnd;
					refGpuTime = gpuEnd;
				}
			}
		}
	}

	private static void WritePlots(Stream stream, IReadOnlyList<TracyPlotSummary> plots, StringTable strings)
	{
		WriteUInt64(stream, (ulong)plots.Count);
		foreach (TracyPlotSummary plot in plots)
		{
			WriteStruct(stream, new PlotHeader
			{
				Type = plot.Type,
				Format = plot.Format,
				ShowSteps = 0,
				Fill = 1,
				Color = 0,
				NamePointer = strings.PointerOf(NormalizeText(plot.Name, "Plot")),
				Min = plot.Min,
				Max = plot.Max,
				Sum = plot.Sum,
				SampleCount = (ulong)plot.Samples.Count
			});
			long refTime = 0L;
			foreach (TracyPlotSample sample in plot.Samples.OrderBy(sample => sample.Time))
			{
				WriteTimeOffset(stream, sample.Time, ref refTime);
				WriteDouble(stream, sample.Value);
			}
		}
	}

	private static long ResolveLastTime(IReadOnlyList<TracyFrameSummary> frames, TracyEventStream eventStream)
	{
		List<long> times = new List<long>();
		times.AddRange(frames.Select(frame => frame.End));
		times.AddRange(eventStream.CpuZones.Select(zone => zone.End));
		times.AddRange(eventStream.GpuZones.Select(zone => zone.End));
		foreach (TracyPlotSummary plot in eventStream.Plots)
		{
			times.AddRange(plot.Samples.Select(sample => sample.Time));
		}
		return times.Count == 0 ? 0L : times.Max();
	}

	private static void WriteTracyDump(string path, byte[] inner)
	{
		using FileStream stream = File.Create(path);
		stream.Write(new byte[] { (byte)'t', (byte)'l', (byte)'Z', 4 }, 0, 4);
		for (int offset = 0; offset < inner.Length; offset += MaxDecodedBlockSize)
		{
			int length = Math.Min(MaxDecodedBlockSize, inner.Length - offset);
			byte[] block = new byte[length];
			Buffer.BlockCopy(inner, offset, block, 0, length);
			byte[] compressed = EncodeLz4LiteralBlock(block);
			WriteUInt32(stream, (uint)compressed.Length);
			stream.Write(compressed, 0, compressed.Length);
		}
	}

	private static byte[] EncodeLz4LiteralBlock(byte[] bytes)
	{
		using MemoryStream stream = new MemoryStream();
		if (bytes.Length < 15)
		{
			stream.WriteByte((byte)(bytes.Length << 4));
		}
		else
		{
			stream.WriteByte(0xF0);
			int remaining = bytes.Length - 15;
			while (remaining >= 255)
			{
				stream.WriteByte(255);
				remaining -= 255;
			}
			stream.WriteByte((byte)remaining);
		}
		stream.Write(bytes, 0, bytes.Length);
		return stream.ToArray();
	}

	private static void WriteTimeOffset(Stream stream, long value, ref long refTime)
	{
		WriteInt64(stream, value - refTime);
		refTime = value;
	}

	private static TracyStringRef StringRefIndex(int index)
	{
		return new TracyStringRef { Value = (ulong)index, Flags = 3 };
	}

	private static TracyStringRef InactiveStringRef()
	{
		return new TracyStringRef();
	}

	private static string SourceLocationKey(TracyCpuZoneSummary zone)
	{
		return zone.SourceLocation + "\0" + NormalizeText(zone.Name, "Zone " + zone.SourceLocation);
	}

	private static string SourceLocationKey(TracyGpuZoneSummary zone)
	{
		return zone.SourceLocation + "\0" + NormalizeText(zone.Name, "GPU Zone " + zone.SourceLocation);
	}

	private static string NormalizeText(string value, string fallback)
	{
		return string.IsNullOrWhiteSpace(value) ? (fallback ?? string.Empty) : value;
	}

	private static void WriteSizedString(Stream stream, string value)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
		WriteUInt64(stream, (ulong)bytes.Length);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteFixedAscii(Stream stream, string value, int size)
	{
		byte[] bytes = new byte[size];
		byte[] text = Encoding.ASCII.GetBytes(value ?? string.Empty);
		Buffer.BlockCopy(text, 0, bytes, 0, Math.Min(text.Length, bytes.Length));
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteStruct<T>(Stream stream, T value) where T : unmanaged
	{
		int size = Marshal.SizeOf<T>();
		Span<byte> bytes = stackalloc byte[size];
		MemoryMarshal.Write(bytes, in value);
		stream.Write(bytes);
	}

	private static void WriteInt32(Stream stream, int value)
	{
		WriteStruct(stream, value);
	}

	private static void WriteUInt32(Stream stream, uint value)
	{
		WriteStruct(stream, value);
	}

	private static void WriteInt64(Stream stream, long value)
	{
		WriteStruct(stream, value);
	}

	private static void WriteUInt64(Stream stream, ulong value)
	{
		WriteStruct(stream, value);
	}

	private static void WriteDouble(Stream stream, double value)
	{
		WriteStruct(stream, value);
	}

	private sealed class WriterSourceLocation
	{
		public WriterSourceLocation(short id, ulong pointer, int nameIndex, int functionIndex, int fileIndex, uint line)
		{
			Id = id;
			Pointer = pointer;
			NameIndex = nameIndex;
			FunctionIndex = functionIndex;
			FileIndex = fileIndex;
			Line = line;
		}

		public short Id { get; }

		public ulong Pointer { get; }

		public int NameIndex { get; }

		public int FunctionIndex { get; }

		public int FileIndex { get; }

		public uint Line { get; }

		public ulong ZoneCount { get; set; }
	}

	private sealed class WriterGpuContext
	{
		public WriterGpuContext(byte context, string name, uint threadId, float period, byte type, byte flags)
		{
			Context = context;
			Name = name ?? string.Empty;
			ThreadId = threadId;
			Period = period;
			Type = type;
			Flags = flags;
		}

		public byte Context { get; }

		public string Name { get; }

		public uint ThreadId { get; }

		public float Period { get; }

		public byte Type { get; }

		public byte Flags { get; }
	}

	private sealed class StringTable
	{
		private readonly Dictionary<string, int> m_IndexByText = new Dictionary<string, int>(StringComparer.Ordinal);
		private readonly List<string> m_Texts = new List<string>();
		private readonly List<TracyThreadSummary> m_Threads = new List<TracyThreadSummary>();

		public IReadOnlyList<TracyThreadSummary> Threads => m_Threads;

		public void Add(string value)
		{
			string text = value ?? string.Empty;
			if (!m_IndexByText.ContainsKey(text))
			{
				m_IndexByText[text] = m_Texts.Count;
				m_Texts.Add(text);
			}
		}

		public int IndexOf(string value)
		{
			string text = value ?? string.Empty;
			if (!m_IndexByText.TryGetValue(text, out int index))
			{
				index = m_Texts.Count;
				m_IndexByText[text] = index;
				m_Texts.Add(text);
			}
			return index;
		}

		public ulong PointerOf(string value)
		{
			return StringPointerBase + (ulong)IndexOf(value) * 0x100UL;
		}

		public void Write(Stream stream)
		{
			WriteUInt64(stream, (ulong)m_Texts.Count);
			for (int index = 0; index < m_Texts.Count; index++)
			{
				WriteUInt64(stream, StringPointerBase + (ulong)index * 0x100UL);
				WriteSizedString(stream, m_Texts[index]);
			}
		}

		public void SetThreads(IEnumerable<TracyThreadSummary> threads)
		{
			m_Threads.Clear();
			m_Threads.AddRange(threads);
		}
	}
}
