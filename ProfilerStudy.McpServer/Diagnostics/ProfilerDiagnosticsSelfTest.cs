using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ProfilerStudy;
using ProfilerStudy.Tracy;

namespace ProfilerStudy.McpServer;

internal static class ProfilerDiagnosticsSelfTest
{
	public static int Run()
	{
		try
		{
			List<ProfilerDiagnostics.FrameSample> samples = new List<ProfilerDiagnostics.FrameSample>
			{
				new ProfilerDiagnostics.FrameSample(10, 20.0, 100, 1000),
				new ProfilerDiagnostics.FrameSample(15, 96.0, 100, 1000),
				new ProfilerDiagnostics.FrameSample(20, 132.0, 100, 1000),
				new ProfilerDiagnostics.FrameSample(25, 133.0, 100, 1000),
				new ProfilerDiagnostics.FrameSample(30, 132.5, 100, 1000)
			};

			Dictionary<string, object> pattern = ProfilerDiagnostics.AnalyzeSlowFramePattern(samples, 50.0);
			AssertEqual(true, pattern["hasPeriodicSlowFrames"], "periodic slow frames");
			AssertEqual(5, pattern["dominantIndexDelta"], "dominant delta");
			RunCaptureTargetTests();
			RunTracyStatusTests();
			RunAnalysisServiceTests();

			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.GetType().FullName);
			Console.Error.WriteLine(ex.Message);
			Console.Error.WriteLine(ex.StackTrace ?? string.Empty);
			return 1;
		}
	}

	private static void RunCaptureTargetTests()
	{
		ProfilerCaptureTarget android = ProfilerCaptureTarget.Parse("android:///data/local/tmp/framepro");
		AssertEqual(ProfilerCaptureTargetKind.AndroidForward, android.Kind, "android target kind");
		AssertEqual("localfilesystem:/data/local/tmp/framepro", android.Endpoint, "android endpoint");
		AssertEqual("127.0.0.1", android.ConnectHost, "android connect host");
		AssertEqual(0, android.ConnectPort, "android connect port");

		ProfilerCaptureTarget namedAndroid = ProfilerCaptureTarget.Parse("android://azahar.debug/framepro");
		AssertEqual("localfilesystem:azahar.debug/framepro", namedAndroid.Endpoint, "named android endpoint");

		ProfilerCaptureTarget localAbstractAndroid = ProfilerCaptureTarget.Parse("android://localabstract:azahar-framepro");
		AssertEqual("localabstract:azahar-framepro", localAbstractAndroid.Endpoint, "localabstract android endpoint");
		AssertEqual(true, AdbSocketDiscovery.IsAdbSocketEndpoint(localAbstractAndroid.Endpoint), "localabstract endpoint accepted");

		ProfilerCaptureTarget tcpAndroid = ProfilerCaptureTarget.Parse("android://tcp:8428");
		AssertEqual("tcp:8428", tcpAndroid.Endpoint, "tcp android endpoint");
		AssertEqual(true, AdbSocketDiscovery.IsAdbSocketEndpoint(tcpAndroid.Endpoint), "tcp endpoint accepted");

		ProfilerCaptureTarget portAndroid = ProfilerCaptureTarget.Parse("android://8429");
		AssertEqual("tcp:8429", portAndroid.Endpoint, "port android endpoint");

		ProfilerCaptureTarget pc = ProfilerCaptureTarget.Parse("pc://192.168.1.20:8428");
		AssertEqual(ProfilerCaptureTargetKind.Tcp, pc.Kind, "pc target kind");
		AssertEqual("192.168.1.20", pc.ConnectHost, "pc host");
		AssertEqual(8428, pc.ConnectPort, "pc port");
		AssertEqual("192.168.1.20:8428", pc.Endpoint, "pc endpoint");

		AssertThrows(() => ProfilerCaptureTarget.Parse("android://tcp:not-a-port"), "invalid tcp android port");
		AssertThrows(() => ProfilerCaptureTarget.Parse("http://127.0.0.1:8428"), "unsupported scheme");
		AssertCaptureProfileTool();
	}

	private static void AssertCaptureProfileTool()
	{
		ProfilerMcpTools tools = new ProfilerMcpTools();
		foreach (object toolObject in tools.ListTools())
		{
			IDictionary tool = toolObject as IDictionary;
			if (tool != null && Convert.ToString(tool["name"]) == "capture_profile")
			{
				IDictionary inputSchema = tool["inputSchema"] as IDictionary;
				IList required = inputSchema["required"] as IList;
				if (required == null || !required.Contains("url"))
				{
					throw new InvalidOperationException("capture_profile must require url.");
				}
				IDictionary properties = inputSchema["properties"] as IDictionary;
				IDictionary protocol = properties["protocol"] as IDictionary;
				AssertEqual("study", protocol["default"], "capture_profile protocol default");
				IList protocolValues = protocol["enum"] as IList;
				if (protocolValues == null || !protocolValues.Contains("study") || !protocolValues.Contains("tracy") || !protocolValues.Contains("perfetto"))
				{
					throw new InvalidOperationException("capture_profile protocol enum must include study, tracy, and perfetto.");
				}
				return;
			}
		}
		throw new InvalidOperationException("capture_profile tool is missing.");
	}

	private static void RunTracyStatusTests()
	{
		TracyStatus status = TracyVersionRegistry.GetStatus();
		AssertEqual("0.10.0", status.LockedVersion, "tracy locked version");
		AssertEqual(false, string.IsNullOrWhiteSpace(status.SourceReferencePath), "tracy source reference path");
		AssertHasItems(status.SupportedVersions, "tracy supported versions");
		AssertTracyFileHeaderReader();
		AssertTracyStatusTool();
	}

	private static void AssertTracyFileHeaderReader()
	{
		string path = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test.tracy");
		string unsupportedPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-unsupported.tracy");
		string metadataPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-metadata.tracy");
		string zonePath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-zone.tracy");
		try
		{
			WriteMinimalTracyDump(path, 0, 10, 0);
			TracyFileHeader header = Tracy010FileReader.ReadHeader(path);
			AssertEqual("0.10.0", header.Version, "tracy file header version");
			AssertEqual("lz4", header.Compression, "tracy file compression");
			TracyEventStream eventStream = Tracy010FileReader.Read(path);
			AssertEqual("0.10.0", eventStream.Version, "tracy event stream version");
			AssertEqual("lz4", eventStream.Compression, "tracy event stream compression");
			AssertEqual(1, eventStream.CompressedBlockCount, "tracy compressed block count");
			AssertEqual(8L, eventStream.DecodedByteCount, "tracy decoded byte count");
			AssertEqual(0L, eventStream.PayloadByteCount, "tracy payload byte count");
			AssertLoadTraceFileTool(path);

			WriteTracyDumpWithMetadata(metadataPath);
			TracyEventStream metadataStream = Tracy010FileReader.Read(metadataPath);
			AssertEqual(true, metadataStream.HasMetadata, "tracy metadata present");
			AssertEqual("SelfTestCapture", metadataStream.Metadata.CaptureName, "tracy metadata capture name");
			AssertEqual("SelfTestProgram", metadataStream.Metadata.CaptureProgram, "tracy metadata capture program");
			AssertEqual("SelfTestHost", metadataStream.Metadata.HostInfo, "tracy metadata host info");
			AssertEqual(4242UL, metadataStream.Metadata.ProcessId, "tracy metadata pid");
			AssertEqual(16_666_667L, metadataStream.Metadata.LastTime, "tracy metadata last time");
			AssertEqual(1, metadataStream.Metadata.FrameSetCount, "tracy metadata frame set count");
			AssertEqual(2, metadataStream.Metadata.FrameCount, "tracy metadata frame count");
			AssertTracyMetadataQueryTools(metadataPath);

			WriteTracyDumpWithCpuZone(zonePath);
			TracyEventStream zoneStream = Tracy010FileReader.Read(zonePath);
			AssertEqual(1, zoneStream.ThreadCount, "tracy zone thread count");
			AssertEqual(1, zoneStream.CpuZones.Count, "tracy cpu zone count");
			AssertEqual("SelfTestZone", zoneStream.CpuZones[0].Name, "tracy cpu zone name");
			AssertEqual(4_000_000L, zoneStream.CpuZones[0].Duration, "tracy cpu zone duration");
			AssertTracyZoneQueryTools(zonePath);

			WriteMinimalTracyDump(unsupportedPath, 0, 11, 0);
			AssertThrows(() => Tracy010FileReader.ReadHeader(unsupportedPath), "unsupported tracy file version");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			if (File.Exists(unsupportedPath))
			{
				File.Delete(unsupportedPath);
			}
			if (File.Exists(metadataPath))
			{
				File.Delete(metadataPath);
			}
			if (File.Exists(zonePath))
			{
				File.Delete(zonePath);
			}
		}
	}

	private static void AssertLoadTraceFileTool(string path)
	{
		ProfilerMcpTools tools = new ProfilerMcpTools();
		bool found = false;
		foreach (object toolObject in tools.ListTools())
		{
			IDictionary tool = toolObject as IDictionary;
			if (tool != null && Convert.ToString(tool["name"]) == "load_trace_file")
			{
				IDictionary inputSchema = tool["inputSchema"] as IDictionary;
				IDictionary properties = inputSchema["properties"] as IDictionary;
				IDictionary keepSession = properties["keep_session"] as IDictionary;
				AssertEqual(false, keepSession["default"], "load_trace_file keep_session default");
				found = true;
				break;
			}
		}
		if (!found)
		{
			throw new InvalidOperationException("load_trace_file tool is missing.");
		}

		Dictionary<string, object> result = tools.CallTool("load_trace_file", new Dictionary<string, object>
		{
			["path"] = path,
			["format"] = "auto"
		});
		AssertEqual(false, result["isError"], "load_trace_file isError");
		IDictionary structured = result["structuredContent"] as IDictionary;
		AssertEqual("tracy", structured["sourceFormat"], "load trace source format");
		AssertEqual("0.10.0", structured["tracyVersion"], "load trace tracy version");

		Dictionary<string, object> keptResult = tools.CallTool("load_trace_file", new Dictionary<string, object>
		{
			["path"] = path,
			["format"] = "auto",
			["keep_session"] = true
		});
		AssertEqual(false, keptResult["isError"], "load_trace_file keep_session isError");
		IDictionary kept = keptResult["structuredContent"] as IDictionary;
		string sessionId = Convert.ToString(kept["sessionId"]);
		if (string.IsNullOrWhiteSpace(sessionId))
		{
			throw new InvalidOperationException("load_trace_file keep_session=true must return sessionId.");
		}
		AssertEqual("tracy", kept["sourceFormat"], "kept load trace source format");

		Dictionary<string, object> summaryResult = tools.CallTool("get_session_summary", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
		AssertEqual(false, summaryResult["isError"], "tracy get_session_summary isError");
		IDictionary summary = summaryResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", summary["sourceFormat"], "tracy summary source format");
		AssertEqual(sessionId, summary["sessionId"], "tracy summary session id");
		IDictionary summaryBlock = summary["summary"] as IDictionary;
		AssertEqual("tracy", summaryBlock["sourceFormat"], "tracy summary block source format");
		AssertEqual(true, summaryBlock["framesUnavailable"], "tracy summary frames unavailable");
		AssertEqual(false, summaryBlock["eventsDecoded"], "tracy summary events decoded");
		AssertTracyQueryToolContracts(tools, sessionId);
		tools.CallTool("close_session", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
	}

	private static void AssertTracyQueryToolContracts(ProfilerMcpTools tools, string sessionId)
	{
		Dictionary<string, object> slowFramesResult = tools.CallTool("find_slow_frames", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
		AssertEqual(false, slowFramesResult["isError"], "tracy find_slow_frames isError");
		IDictionary slowFrames = slowFramesResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", slowFrames["sourceFormat"], "tracy slow frames source format");
		AssertEqual(true, slowFrames["framesUnavailable"], "tracy slow frames unavailable");

		Dictionary<string, object> hotspotsResult = tools.CallTool("find_scope_hotspots", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
		AssertEqual(false, hotspotsResult["isError"], "tracy find_scope_hotspots isError");
		IDictionary hotspots = hotspotsResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", hotspots["sourceFormat"], "tracy hotspots source format");
		AssertEqual(false, hotspots["eventsDecoded"], "tracy hotspots events decoded");

		Dictionary<string, object> countersResult = tools.CallTool("list_counters", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
		AssertEqual(false, countersResult["isError"], "tracy list_counters isError");
		IDictionary counters = countersResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", counters["sourceFormat"], "tracy counters source format");
		AssertEqual(false, counters["eventsDecoded"], "tracy counters events decoded");

		Dictionary<string, object> counterResult = tools.CallTool("query_counter", new Dictionary<string, object>
		{
			["session_id"] = sessionId,
			["counter_name"] = "FrameTime"
		});
		AssertEqual(false, counterResult["isError"], "tracy query_counter isError");
		IDictionary counter = counterResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", counter["sourceFormat"], "tracy counter source format");
		AssertEqual("FrameTime", counter["counterName"], "tracy counter name");

		Dictionary<string, object> rangeResult = tools.CallTool("analyze_time_range", new Dictionary<string, object>
		{
			["session_id"] = sessionId,
			["start_frame"] = 0,
			["end_frame"] = 0
		});
		AssertEqual(false, rangeResult["isError"], "tracy analyze_time_range isError");
		IDictionary range = rangeResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", range["sourceFormat"], "tracy range source format");
		AssertEqual(true, range["framesUnavailable"], "tracy range frames unavailable");

		Dictionary<string, object> overheadResult = tools.CallTool("get_profiler_overhead", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
		AssertEqual(false, overheadResult["isError"], "tracy get_profiler_overhead isError");
		IDictionary overhead = overheadResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", overhead["sourceFormat"], "tracy overhead source format");
		AssertEqual(false, overhead["supported"], "tracy overhead supported");
	}

	private static void AssertTracyMetadataQueryTools(string path)
	{
		ProfilerMcpTools tools = new ProfilerMcpTools();
		Dictionary<string, object> keptResult = tools.CallTool("load_trace_file", new Dictionary<string, object>
		{
			["path"] = path,
			["format"] = "auto",
			["keep_session"] = true
		});
		AssertEqual(false, keptResult["isError"], "metadata load_trace_file isError");
		IDictionary kept = keptResult["structuredContent"] as IDictionary;
		string sessionId = Convert.ToString(kept["sessionId"]);
		IDictionary summary = kept["summary"] as IDictionary;
		AssertEqual(true, summary["metadataDecoded"], "metadata summary decoded");
		AssertEqual(2, summary["frameCount"], "metadata summary frame count");
		AssertEqual(false, summary["framesUnavailable"], "metadata summary frames unavailable");

		Dictionary<string, object> slowFramesResult = tools.CallTool("find_slow_frames", new Dictionary<string, object>
		{
			["session_id"] = sessionId,
			["top"] = 5
		});
		AssertEqual(false, slowFramesResult["isError"], "metadata tracy find_slow_frames isError");
		IDictionary slowFrames = slowFramesResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", slowFrames["sourceFormat"], "metadata slow frames source format");
		AssertEqual(false, slowFrames["framesUnavailable"], "metadata slow frames unavailable");
		AssertHasItems(slowFrames["slowFrames"], "metadata slow frames");
		IDictionary firstFrame = ((IList)slowFrames["slowFrames"])[0] as IDictionary;
		AssertEqual(1, firstFrame["frameIndex"], "metadata slow frame index");
		AssertEqual(9.0, firstFrame["durationMs"], "metadata slow frame duration");

		tools.CallTool("close_session", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
	}

	private static void AssertTracyZoneQueryTools(string path)
	{
		ProfilerMcpTools tools = new ProfilerMcpTools();
		Dictionary<string, object> keptResult = tools.CallTool("load_trace_file", new Dictionary<string, object>
		{
			["path"] = path,
			["format"] = "auto",
			["keep_session"] = true
		});
		AssertEqual(false, keptResult["isError"], "zone load_trace_file isError");
		IDictionary kept = keptResult["structuredContent"] as IDictionary;
		string sessionId = Convert.ToString(kept["sessionId"]);
		IDictionary summary = kept["summary"] as IDictionary;
		AssertEqual(1, summary["threadCount"], "zone summary thread count");
		AssertEqual(1, summary["zoneCount"], "zone summary zone count");

		Dictionary<string, object> hotspotsResult = tools.CallTool("find_scope_hotspots", new Dictionary<string, object>
		{
			["session_id"] = sessionId,
			["top"] = 5
		});
		AssertEqual(false, hotspotsResult["isError"], "zone find_scope_hotspots isError");
		IDictionary hotspots = hotspotsResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", hotspots["sourceFormat"], "zone hotspots source format");
		AssertEqual(true, hotspots["eventsDecoded"], "zone hotspots events decoded");
		AssertHasItems(hotspots["scopeHotspots"], "zone scope hotspots");
		IDictionary firstHotspot = ((IList)hotspots["scopeHotspots"])[0] as IDictionary;
		AssertEqual("SelfTestZone", firstHotspot["name"], "zone hotspot name");
		AssertEqual(4.0, firstHotspot["totalMs"], "zone hotspot total ms");
		AssertEqual(1, firstHotspot["totalCount"], "zone hotspot count");

		Dictionary<string, object> rangeResult = tools.CallTool("analyze_time_range", new Dictionary<string, object>
		{
			["session_id"] = sessionId,
			["start_frame"] = 0,
			["end_frame"] = 1,
			["top"] = 5
		});
		AssertEqual(false, rangeResult["isError"], "zone analyze_time_range isError");
		IDictionary range = rangeResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", range["sourceFormat"], "zone range source format");
		AssertHasItems(range["scopeHotspots"], "zone range hotspots");

		tools.CallTool("close_session", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
	}

	private static void WriteMinimalTracyDump(string path, byte major, byte minor, byte patch)
	{
		byte[] innerHeader = new byte[] { (byte)'t', (byte)'r', (byte)'a', (byte)'c', (byte)'y', major, minor, patch };
		WriteTracyDump(path, innerHeader);
	}

	private static void WriteTracyDumpWithMetadata(string path)
	{
		using MemoryStream inner = new MemoryStream();
		WriteTracyMetadataPrefix(inner, includeZoneString: false);
		WriteTracyDump(path, inner.ToArray());
	}

	private static void WriteTracyDumpWithCpuZone(string path)
	{
		using MemoryStream inner = new MemoryStream();
		WriteTracyMetadataPrefix(inner, includeZoneString: true);
		WriteUInt64(inner, 1);                      // localThreadCompress size
		WriteUInt64(inner, 123);                    // thread id
		WriteUInt64(inner, 0);                      // externalThreadCompress size
		WriteUInt64(inner, 0);                      // sourceLocation map count
		WriteUInt64(inner, 0);                      // sourceLocationExpand count
		WriteUInt64(inner, 1);                      // sourceLocationPayload count
		WriteSourceLocationBase(inner, nameIndex: 0, line: 77);
		WriteUInt64(inner, 1);                      // sourceLocationZones count
		WriteInt16(inner, 0);                       // source location id
		WriteUInt64(inner, 1);                      // zone count for source location
		WriteUInt64(inner, 0);                      // gpuSourceLocationZones count
		WriteUInt64(inner, 0);                      // lockMap count
		WriteUInt64(inner, 0);                      // messages count
		WriteUInt64(inner, 1);                      // zoneExtra count
		inner.Write(new byte[12], 0, 12);           // ZoneExtra
		WriteUInt64(inner, 1);                      // total zone count
		WriteUInt64(inner, 0);                      // zoneChildren count
		WriteUInt64(inner, 1);                      // thread count
		WriteUInt64(inner, 123);                    // thread id
		WriteUInt64(inner, 1);                      // thread zone count
		WriteUInt64(inner, 0);                      // kernelSampleCnt
		inner.WriteByte(0);                         // isFiber
		WriteUInt32(inner, 1);                      // timeline size
		WriteInt16(inner, 0);                       // srcloc
		WriteInt64(inner, 1_000_000);               // start offset
		WriteUInt32(inner, 0);                      // extra
		WriteUInt32(inner, 0);                      // child size
		WriteInt64(inner, 4_000_000);               // end offset
		WriteUInt64(inner, 0);                      // thread messages
		WriteUInt64(inner, 0);                      // ctxSwitchSamples
		WriteUInt64(inner, 0);                      // samples
		WriteTracyDump(path, inner.ToArray());
	}

	private static void WriteTracyMetadataPrefix(MemoryStream inner, bool includeZoneString)
	{
		inner.Write(new byte[] { (byte)'t', (byte)'r', (byte)'a', (byte)'c', (byte)'y', 0, 10, 0 });
		WriteInt64(inner, 0);                       // m_delay
		WriteInt64(inner, 1_000_000_000);           // m_resolution
		WriteDouble(inner, 1.0);                    // m_timerMul
		WriteInt64(inner, 16_666_667);              // lastTime
		WriteInt64(inner, 0);                       // frameOffset
		WriteUInt64(inner, 4242);                   // pid
		WriteInt64(inner, 0);                       // samplingPeriod
		inner.WriteByte(2);                         // CpuArchX64
		WriteUInt32(inner, 0x12345678);             // cpuId
		WriteFixedAscii(inner, "SelfTestCpu", 12);  // cpuManufacturer
		inner.WriteByte(0);                         // onDemand
		WriteSizedString(inner, "SelfTestCapture");
		WriteSizedString(inner, "SelfTestProgram");
		WriteInt64(inner, 1_700_000_000);           // captureTime
		WriteInt64(inner, 1_699_999_000);           // executableTime
		WriteSizedString(inner, "SelfTestHost");
		WriteUInt64(inner, 0);                      // cpuTopology count
		WriteUInt64(inner, 0);                      // crashEvent.thread
		WriteInt64(inner, 0);                       // crashEvent.time
		WriteUInt64(inner, 0);                      // crashEvent.message
		WriteUInt32(inner, 0);                      // crashEvent.callstack
		WriteUInt64(inner, 1);                      // frame set count
		WriteUInt64(inner, 0);                      // frame name
		inner.WriteByte(0);                         // non-continuous frame set
		WriteUInt64(inner, 2);                      // frame count
		WriteInt64(inner, 0);                       // frame 0 start offset
		WriteInt64(inner, 8_333_333);               // frame 0 end offset
		WriteInt32(inner, -1);                      // frame 0 image
		WriteInt64(inner, 1);                       // frame 1 start offset
		WriteInt64(inner, 9_000_000);               // frame 1 end offset
		WriteInt32(inner, -1);                      // frame 1 image
		WriteUInt64(inner, includeZoneString ? 1UL : 0UL); // stringData count
		if (includeZoneString)
		{
			WriteUInt64(inner, 0x1000);
			WriteSizedString(inner, "SelfTestZone");
		}
		WriteUInt64(inner, 0);                      // strings count
		WriteUInt64(inner, 0);                      // threadNames count
		WriteUInt64(inner, 0);                      // externalNames count
	}

	private static void WriteSourceLocationBase(Stream stream, uint nameIndex, uint line)
	{
		WriteStringRefIndex(stream, nameIndex);
		WriteInactiveStringRef(stream);
		WriteInactiveStringRef(stream);
		WriteUInt32(stream, line);
		WriteUInt32(stream, 0);
	}

	private static void WriteStringRefIndex(Stream stream, uint index)
	{
		WriteUInt64(stream, index);
		stream.WriteByte(3);
	}

	private static void WriteInactiveStringRef(Stream stream)
	{
		WriteUInt64(stream, 0);
		stream.WriteByte(0);
	}

	private static void WriteTracyDump(string path, byte[] inner)
	{
		byte[] compressed = EncodeLz4LiteralBlock(inner);

		using FileStream stream = File.Create(path);
		byte[] outerHeader = Encoding.ASCII.GetBytes("tlZ4");
		stream.Write(outerHeader, 0, outerHeader.Length);
		byte[] blockSize = BitConverter.GetBytes((uint)compressed.Length);
		stream.Write(blockSize, 0, blockSize.Length);
		stream.Write(compressed, 0, compressed.Length);
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

	private static void WriteSizedString(Stream stream, string value)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(value);
		WriteUInt64(stream, (ulong)bytes.Length);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteFixedAscii(Stream stream, string value, int size)
	{
		byte[] bytes = new byte[size];
		byte[] text = Encoding.ASCII.GetBytes(value);
		Buffer.BlockCopy(text, 0, bytes, 0, Math.Min(text.Length, bytes.Length));
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteInt32(Stream stream, int value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteInt16(Stream stream, short value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteUInt32(Stream stream, uint value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteInt64(Stream stream, long value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteUInt64(Stream stream, ulong value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteDouble(Stream stream, double value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void AssertTracyStatusTool()
	{
		ProfilerMcpTools tools = new ProfilerMcpTools();
		foreach (object toolObject in tools.ListTools())
		{
			IDictionary tool = toolObject as IDictionary;
			if (tool != null && Convert.ToString(tool["name"]) == "get_tracy_status")
			{
				IDictionary inputSchema = tool["inputSchema"] as IDictionary;
				IDictionary properties = inputSchema["properties"] as IDictionary;
				if (properties == null)
				{
					throw new InvalidOperationException("get_tracy_status must accept an empty object.");
				}
				Dictionary<string, object> result = tools.CallTool("get_tracy_status", new Dictionary<string, object>());
				IDictionary structured = result["structuredContent"] as IDictionary;
				AssertEqual("0.10.0", structured["lockedVersion"], "tracy status tool locked version");
				AssertHasItems(structured["supportedVersions"], "tracy status tool supported versions");
				return;
			}
		}
		throw new InvalidOperationException("get_tracy_status tool is missing.");
	}

	private static void RunAnalysisServiceTests()
	{
		ProfilerAnalysisService service = new ProfilerAnalysisService();
		Session session = new Session(new CoreSettings(), new CapturingLog());
		session.FillWithRandomData();
		InvokePrivate(session, "AssignUnassignedTimeSpans");
		PopulateTimerNamesFromStats(session);
		PopulateCounter(session);
		PopulateProfilerOverhead(session);
		string sessionId = AddSession(service, session);
		AssertProfilerOverheadContract(service, sessionId);
		Dictionary<string, object> hotspots = service.FindScopeHotspots(sessionId, 10, -1, -1);
		AssertHasItems(hotspots["scopeHotspots"], "scope hotspots");

		Dictionary<string, object> frame = service.AnalyzeFrame(sessionId, 20, 10, 3);
		AssertHasItems(frame["scopeHotspots"], "frame scope hotspots");
		AssertHasItems(frame["neighborSlowFrames"], "neighbor slow frames");

		Dictionary<string, object> frameDetail = service.AnalyzeFrameDetail(sessionId, 20, 20, 4, 0.0);
		AssertHasItems(frameDetail["threadFlameGraphs"], "thread flame graphs");
		AssertHasItems(frameDetail["topSpans"], "top frame detail spans");
		AssertHasItems(frameDetail["frameCounters"], "frame detail counters");
		IDictionary frameDetailCounter = ((IList)frameDetail["frameCounters"])[0] as IDictionary;
		AssertEqual("SelfTestCounter", frameDetailCounter["name"], "frame detail counter name");
		AssertEqual(12.0, frameDetailCounter["value"], "frame detail counter value");

		AssertFrameDetailDefaults();
		AssertProfilerOverheadTool();

		Dictionary<string, object> range = service.AnalyzeTimeRange(sessionId, 18, 24, 10, 0.0);
		AssertHasItems(range["scopeHotspots"], "range scope hotspots");

		Dictionary<string, object> counters = service.ListCounters(sessionId, 10, string.Empty);
		AssertHasItems(counters["counters"], "counters");
		IDictionary firstCounter = ((IList)counters["counters"])[0] as IDictionary;
		string counterName = Convert.ToString(firstCounter["name"]);
		Dictionary<string, object> counterSamples = service.QueryCounterSamples(sessionId, counterName, 18, 24, false, 10);
		AssertEqual(counterName, counterSamples["counterName"], "counter name");
		AssertHasItems(counterSamples["samples"], "counter samples");
		service.CloseSession(sessionId);
	}

	private static void AssertProfilerOverheadContract(ProfilerAnalysisService service, string sessionId)
	{
		Dictionary<string, object> summary = service.GetSessionSummary(sessionId);
		IDictionary summaryOverhead = summary["profilerOverhead"] as IDictionary;
		AssertProfilerOverhead(summaryOverhead, "summary profiler overhead");

		Dictionary<string, object> rangeOverhead = service.GetProfilerOverhead(sessionId, 18, 20, 5);
		AssertEqual(sessionId, rangeOverhead["sessionId"], "profiler overhead session id");
		AssertProfilerOverhead(rangeOverhead["profilerOverhead"] as IDictionary, "range profiler overhead");
	}

	private static void AssertFrameDetailDefaults()
	{
		ProfilerMcpTools tools = new ProfilerMcpTools();
		IDictionary frameDetailTool = null;
		foreach (object toolObject in tools.ListTools())
		{
			IDictionary tool = toolObject as IDictionary;
			if (tool != null && Convert.ToString(tool["name"]) == "analyze_frame_detail")
			{
				frameDetailTool = tool;
				break;
			}
		}
		if (frameDetailTool == null)
		{
			throw new InvalidOperationException("analyze_frame_detail tool is missing.");
		}
		IDictionary inputSchema = frameDetailTool["inputSchema"] as IDictionary;
		IDictionary properties = inputSchema["properties"] as IDictionary;
		AssertEqual(300, ((IDictionary)properties["max_nodes"])["default"], "max_nodes default");
		AssertEqual(12, ((IDictionary)properties["max_depth"])["default"], "max_depth default");
		AssertEqual(0.01, ((IDictionary)properties["min_duration_ms"])["default"], "min_duration_ms default");
	}

	private static void AssertProfilerOverheadTool()
	{
		ProfilerMcpTools tools = new ProfilerMcpTools();
		foreach (object toolObject in tools.ListTools())
		{
			IDictionary tool = toolObject as IDictionary;
			if (tool != null && Convert.ToString(tool["name"]) == "get_profiler_overhead")
			{
				IDictionary inputSchema = tool["inputSchema"] as IDictionary;
				IList required = inputSchema["required"] as IList;
				if (required == null || !required.Contains("session_id"))
				{
					throw new InvalidOperationException("get_profiler_overhead must require session_id.");
				}
				return;
			}
		}
		throw new InvalidOperationException("get_profiler_overhead tool is missing.");
	}

	private static void AssertProfilerOverhead(IDictionary overhead, string name)
	{
		if (overhead == null)
		{
			throw new InvalidOperationException(name + " expected profilerOverhead object.");
		}
		IDictionary memory = overhead["memoryOverhead"] as IDictionary;
		IDictionary frame = overhead["frameOverhead"] as IDictionary;
		AssertEqual(448L, memory["totalBytes"], name + " memory total bytes");
		AssertEqual(1800L, frame["totalBytesSent"], name + " total bytes sent");
		AssertEqual(6.0, frame["totalWaitForSendCompleteMs"], name + " total wait ms");
		AssertHasItems(overhead["topWaitFrames"], name + " top wait frames");
		AssertHasItems(overhead["topBytesFrames"], name + " top bytes frames");
	}

	private static string AddSession(ProfilerAnalysisService service, Session session)
	{
		MethodInfo method = typeof(ProfilerAnalysisService).GetMethod("AddSession", BindingFlags.Instance | BindingFlags.NonPublic);
		return (string)method.Invoke(service, new object[] { session, "self-test", new CapturingLog() });
	}

	private static void InvokePrivate(object target, string name)
	{
		MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
		method.Invoke(target, new object[0]);
	}

	private static void PopulateTimerNamesFromStats(Session session)
	{
		FieldInfo statsField = typeof(Session).GetField("m_TimeSpanFrameStats", BindingFlags.Instance | BindingFlags.NonPublic);
		FieldInfo namesField = typeof(Session).GetField("m_TimeSpanNames", BindingFlags.Instance | BindingFlags.NonPublic);
		IDictionary stats = (IDictionary)statsField.GetValue(session);
		object names = namesField.GetValue(session);
		MethodInfo add = names.GetType().GetMethod("Add");
		foreach (object key in stats.Keys)
		{
			add.Invoke(names, new object[] { key });
		}
	}

	private static void PopulateCounter(Session session)
	{
		const long counterId = 9001;
		const string counterName = "SelfTestCounter";
		IDictionary strings = (IDictionary)GetPrivateField(session, "m_Strings");
		strings[counterId] = counterName;
		IDictionary stringIds = (IDictionary)GetPrivateField(session, "m_StringIds");
		stringIds[counterName] = counterId;
		IDictionary valueTypes = (IDictionary)GetPrivateField(session, "m_CustomStatValueTypes");
		valueTypes[counterId] = CustomStatValueType.Double;
		IDictionary sessionInfo = (IDictionary)GetPrivateField(session, "m_CustomStatSessionInfo");
		sessionInfo[counterId] = new CustomStatSessionData(counterId, CustomStatValueType.Double, 18)
		{
			m_TotalValueDouble = 196.0,
			m_TotalCount = 7,
			m_MinValuePerFrameDouble = 10.0,
			m_MaxValuePerFrameDouble = 46.0,
			m_MinCountPerFrame = 1,
			m_MaxCountPerFrame = 1,
			m_AccTotalValueDouble = 196.0,
			m_AccMinValuePerFrameDouble = 10.0,
			m_AccMaxValuePerFrameDouble = 196.0
		};
		for (int frameIndex = 18; frameIndex <= 24; frameIndex++)
		{
			Frame frame = session.GetFrame(frameIndex);
			frame.AddCustomStat(counterId, 0, 10.0 + frameIndex - 18, 1);
		}
	}

	private static void PopulateProfilerOverhead(Session session)
	{
		SetPrivateField(session, "m_SendBufferSize", 128L);
		SetPrivateField(session, "m_StringMemorySize", 64L);
		SetPrivateField(session, "m_MiscMemorySize", 256L);
		SetPrivateField(session, "m_RecordingFileSize", 4096L);
		for (int frameIndex = 18; frameIndex <= 20; frameIndex++)
		{
			Frame frame = session.GetFrame(frameIndex);
			long waitTicks = (frameIndex - 17) * session.TimerFrequency / 1000;
			long sendTicks = (frameIndex - 17) * session.TimerFrequency / 500;
			frame.Finalise(frame.EndTime, frame.TimeSpanCount, (frameIndex - 17) * 300, waitTicks, sendTicks);
		}
	}

	private static object GetPrivateField(object target, string name)
	{
		FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		return field.GetValue(target);
	}

	private static void SetPrivateField(object target, string name, object value)
	{
		FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		field.SetValue(target, value);
	}

	private static void AssertHasItems(object actual, string name)
	{
		ICollection collection = actual as ICollection;
		if (collection == null || collection.Count == 0)
		{
			throw new InvalidOperationException(name + " expected at least one item.");
		}
	}

	private static void AssertThrows(Action action, string name)
	{
		try
		{
			action();
		}
		catch
		{
			return;
		}
		throw new InvalidOperationException(name + " expected an exception.");
	}

	private static void AssertEqual<T>(T expected, object actual, string name)
	{
		if (!object.Equals(expected, actual))
		{
			throw new InvalidOperationException(name + " expected " + expected + " but got " + actual);
		}
	}
}
