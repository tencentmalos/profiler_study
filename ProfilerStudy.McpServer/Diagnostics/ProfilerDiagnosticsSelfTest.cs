using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
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
		ITracyVersionAdapter fileAdapter = TracyVersionRegistry.ResolveFileAdapter("0.10.0");
		AssertEqual("0.10.0", fileAdapter.Version, "tracy file adapter version");
		ITracyVersionAdapter liveAdapter = TracyVersionRegistry.ResolveLiveAdapter(Tracy010LiveCaptureClient.ProtocolVersion);
		AssertEqual("0.10.0", liveAdapter.Version, "tracy live adapter version");
		AssertThrows(
			() => TracyVersionRegistry.ResolveFileAdapter("0.11.0"),
			"unsupported tracy file adapter");
		AssertThrows(
			() => TracyVersionRegistry.ResolveLiveAdapter(0),
			"unsupported tracy live adapter");
		AssertTracyFileHeaderReader();
		AssertTracyLiveCaptureTool();
		AssertTracyStatusTool();
	}

	private static void AssertTracyFileHeaderReader()
	{
		string path = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test.tracy");
		string unsupportedPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-unsupported.tracy");
		string metadataPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-metadata.tracy");
		string zonePath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-zone.tracy");
		string plotPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-plot.tracy");
		string plotRangePath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-plot-range.tracy");
		string gpuPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-gpu.tracy");
		string unsupportedEventsPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-unsupported-events.tracy");
		string oversizedLockPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-oversized-lock.tracy");
		string oversizedFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-oversized-file.tracy");
		string dictionaryPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-lz4-dictionary.tracy");
		string onDemandPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-on-demand.tracy");
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
			using CancellationTokenSource canceledRead = new CancellationTokenSource();
			canceledRead.Cancel();
			AssertThrows(() => Tracy010FileReader.Read(path, canceledRead.Token), "tracy file reader cancellation");
			AssertThrows(() => TracyTraceImporter.Load(path, canceledRead.Token), "tracy trace importer cancellation");
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
			AssertEqual(1, zoneStream.Threads.Count, "tracy zone thread summary count");
			AssertEqual(123UL, zoneStream.Threads[0].ThreadId, "tracy zone thread id");
			AssertEqual("RenderThread", zoneStream.Threads[0].Name, "tracy zone thread name");
			AssertEqual(1, zoneStream.CpuZones.Count, "tracy cpu zone count");
			AssertEqual("SelfTestZone", zoneStream.CpuZones[0].Name, "tracy cpu zone name");
			AssertEqual(4_000_000L, zoneStream.CpuZones[0].Duration, "tracy cpu zone duration");
			AssertTracyZoneQueryTools(zonePath);

			WriteTracyDumpWithPlot(plotPath);
			TracyEventStream plotStream = Tracy010FileReader.Read(plotPath);
			AssertEqual(1, plotStream.Plots.Count, "tracy plot count");
			AssertEqual("FrameTime", plotStream.Plots[0].Name, "tracy plot name");
			AssertEqual(2, plotStream.Plots[0].Samples.Count, "tracy plot sample count");
			AssertTracyPlotQueryTools(plotPath);

			WriteTracyDumpWithPlotSeparatedFrames(plotRangePath);
			AssertTracyPlotFrameRangeQueryTools(plotRangePath);

			WriteTracyDumpWithGpuContext(gpuPath);
			TracyEventStream gpuStream = Tracy010FileReader.Read(gpuPath);
			AssertEqual(true, gpuStream.HasMetadata, "tracy gpu metadata present");
			AssertEqual(0, gpuStream.CpuZones.Count, "tracy gpu cpu zone count");
			AssertDiagnosticCode(gpuStream.Diagnostics, "TracyGpuZonesUnsupported", "tracy gpu unsupported diagnostics");
			AssertLoadTraceFileDiagnostics(gpuPath, "TracyGpuZonesUnsupported");

			WriteTracyDumpWithUnsupportedEvents(unsupportedEventsPath);
			TracyEventStream unsupportedEventsStream = Tracy010FileReader.Read(unsupportedEventsPath);
			AssertEqual(true, unsupportedEventsStream.HasMetadata, "tracy unsupported events metadata present");
			AssertDiagnosticCode(unsupportedEventsStream.Diagnostics, "TracyLocksUnsupported", "tracy locks unsupported diagnostics");
			AssertDiagnosticCode(unsupportedEventsStream.Diagnostics, "TracyMessagesUnsupported", "tracy messages unsupported diagnostics");
			AssertDiagnosticCode(unsupportedEventsStream.Diagnostics, "TracyAllocationsUnsupported", "tracy allocations unsupported diagnostics");
			AssertDiagnosticCode(unsupportedEventsStream.Diagnostics, "TracyCallstacksUnsupported", "tracy callstacks unsupported diagnostics");
			AssertLoadTraceFileDiagnostics(unsupportedEventsPath, "TracyLocksUnsupported");
			AssertLoadTraceFileDiagnostics(unsupportedEventsPath, "TracyMessagesUnsupported");
			AssertLoadTraceFileDiagnostics(unsupportedEventsPath, "TracyAllocationsUnsupported");
			AssertLoadTraceFileDiagnostics(unsupportedEventsPath, "TracyCallstacksUnsupported");

			WriteTracyDumpWithOversizedLockThreadList(oversizedLockPath);
			AssertInvalidTracyFileTool(oversizedLockPath);

			WriteOversizedTracyDump(oversizedFilePath);
			AssertTracyFileSizeLimitTool(oversizedFilePath);

			WriteTracyDumpWithDictionaryBackReference(dictionaryPath);
			TracyFileHeader dictionaryHeader = Tracy010FileReader.ReadHeader(dictionaryPath);
			AssertEqual("0.10.0", dictionaryHeader.Version, "dictionary tracy header version");

			WriteTracyDumpWithOnDemandFrames(onDemandPath);
			AssertOnDemandSystemFramesSkipped(onDemandPath);

			WriteMinimalTracyDump(unsupportedPath, 0, 11, 0);
			AssertThrows(() => Tracy010FileReader.ReadHeader(unsupportedPath), "unsupported tracy file version");
			AssertUnsupportedTracyFileVersionTool(unsupportedPath);
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
			if (File.Exists(plotPath))
			{
				File.Delete(plotPath);
			}
			if (File.Exists(plotRangePath))
			{
				File.Delete(plotRangePath);
			}
			if (File.Exists(gpuPath))
			{
				File.Delete(gpuPath);
			}
			if (File.Exists(unsupportedEventsPath))
			{
				File.Delete(unsupportedEventsPath);
			}
			if (File.Exists(oversizedLockPath))
			{
				File.Delete(oversizedLockPath);
			}
			if (File.Exists(oversizedFilePath))
			{
				File.Delete(oversizedFilePath);
			}
			if (File.Exists(dictionaryPath))
			{
				File.Delete(dictionaryPath);
			}
			if (File.Exists(onDemandPath))
			{
				File.Delete(onDemandPath);
			}
		}
	}

	private static void AssertLoadTraceFileTool(string path)
	{
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
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
			string artifactId = Convert.ToString(structured["artifactId"]);
			if (string.IsNullOrWhiteSpace(artifactId))
			{
				throw new InvalidOperationException("load_trace_file must return artifactId.");
			}
			AssertTraceArtifactListed(tools, artifactRoot, artifactId, "file-import");
			AssertTraceArtifactDiagnostics(tools, artifactId);
			AssertTraceArtifactDiagnosticsPathContained(tools, artifactRoot);
			AssertTraceArtifactNormalizedPathContained(tools, artifactRoot);

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
			AssertOpenFileToolWithTracy(tools, path);
			AssertTracySaveSessionFileTool(tools, path, sessionId);
			tools.CallTool("close_session", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertTraceArtifactDiagnostics(ProfilerMcpTools tools, string artifactId)
	{
		bool found = false;
		foreach (object toolObject in tools.ListTools())
		{
			IDictionary tool = toolObject as IDictionary;
			if (tool != null && Convert.ToString(tool["name"]) == "get_import_diagnostics")
			{
				IDictionary inputSchema = tool["inputSchema"] as IDictionary;
				IDictionary properties = inputSchema["properties"] as IDictionary;
				if (!properties.Contains("session_id") || !properties.Contains("artifact_id"))
				{
					throw new InvalidOperationException("get_import_diagnostics must accept session_id and artifact_id.");
				}
				found = true;
				break;
			}
		}
		if (!found)
		{
			throw new InvalidOperationException("get_import_diagnostics tool is missing.");
		}

		Dictionary<string, object> diagnosticsResult = tools.CallTool("get_import_diagnostics", new Dictionary<string, object>
		{
			["artifact_id"] = artifactId
		});
		AssertEqual(false, diagnosticsResult["isError"], "artifact get_import_diagnostics isError");
		IDictionary diagnostics = diagnosticsResult["structuredContent"] as IDictionary;
		AssertEqual(artifactId, diagnostics["artifactId"], "artifact diagnostics id");
		AssertEqual("tracy", diagnostics["sourceFormat"], "artifact diagnostics source format");
		AssertHasItems(diagnostics["diagnostics"], "artifact diagnostics");
	}

	private static void AssertTraceArtifactDiagnosticsPathContained(ProfilerMcpTools tools, string artifactRoot)
	{
		string artifactId = "escaped-diagnostics";
		string artifactDirectory = Path.Combine(artifactRoot, artifactId);
		Directory.CreateDirectory(artifactDirectory);
		File.WriteAllText(
			Path.Combine(artifactRoot, "escaped-diagnostics.json"),
			"[{\"severity\":\"error\",\"code\":\"EscapedDiagnostics\",\"message\":\"This file is outside the artifact directory.\"}]");
		File.WriteAllText(
			Path.Combine(artifactDirectory, "manifest.json"),
			"{\"ArtifactId\":\"" + artifactId + "\",\"SourceFormat\":\"tracy\",\"SourceKind\":\"file-import\",\"SourcePath\":\"capture.tracy\",\"NormalizedPath\":\"normalized\",\"DiagnosticsPath\":\"../escaped-diagnostics.json\",\"CreatedUtc\":\"2026-06-11T00:00:00.0000000Z\",\"Implementation\":\"self-test\",\"ReaderVersion\":\"0.0.0\",\"TracyVersion\":\"0.10.0\"}");

		Dictionary<string, object> diagnosticsResult = tools.CallTool("get_import_diagnostics", new Dictionary<string, object>
		{
			["artifact_id"] = artifactId
		});
		AssertEqual(true, diagnosticsResult["isError"], "escaped artifact diagnostics isError");
	}

	private static void AssertTraceArtifactNormalizedPathContained(ProfilerMcpTools tools, string artifactRoot)
	{
		string artifactId = "escaped-normalized";
		string artifactDirectory = Path.Combine(artifactRoot, artifactId);
		string escapedNormalizedDirectory = Path.Combine(artifactRoot, "escaped-normalized-cache");
		Directory.CreateDirectory(artifactDirectory);
		Directory.CreateDirectory(escapedNormalizedDirectory);
		File.WriteAllText(
			Path.Combine(artifactDirectory, "manifest.json"),
			"{\"ArtifactId\":\"" + artifactId + "\",\"SourceFormat\":\"tracy\",\"SourceKind\":\"file-import\",\"SourcePath\":\"capture.tracy\",\"NormalizedPath\":\"../escaped-normalized-cache\",\"DiagnosticsPath\":\"import-diagnostics.json\",\"CreatedUtc\":\"2026-06-11T00:00:00.0000000Z\",\"Implementation\":\"self-test\",\"ReaderVersion\":\"0.0.0\",\"TracyVersion\":\"0.10.0\"}");
		File.WriteAllText(
			Path.Combine(artifactDirectory, "import-diagnostics.json"),
			"[]");
		File.WriteAllText(
			Path.Combine(escapedNormalizedDirectory, "manifest.json"),
			"{\"schemaVersion\":1,\"sourceFormat\":\"tracy\",\"sourcePath\":\"capture.tracy\",\"implementation\":\"self-test\",\"readerVersion\":\"0.0.0\",\"tracyVersion\":\"0.10.0\",\"compression\":\"lz4\",\"startTimeNs\":0,\"endTimeNs\":0,\"timeBase\":\"trace-relative-ns\",\"frameSource\":\"none\",\"threadCount\":0,\"frameCount\":0,\"zoneCount\":0,\"plotCount\":0,\"compressedBlockCount\":0,\"compressedByteCount\":0,\"decodedByteCount\":0,\"payloadByteCount\":0,\"metadataDecoded\":false}");
		File.WriteAllText(Path.Combine(escapedNormalizedDirectory, "threads.ndjson"), string.Empty);
		File.WriteAllText(Path.Combine(escapedNormalizedDirectory, "frames.ndjson"), string.Empty);
		File.WriteAllText(Path.Combine(escapedNormalizedDirectory, "cpu_zones.ndjson"), string.Empty);
		File.WriteAllText(Path.Combine(escapedNormalizedDirectory, "plots.ndjson"), string.Empty);
		File.WriteAllText(Path.Combine(escapedNormalizedDirectory, "diagnostics.json"), "[]");

		Dictionary<string, object> loadResult = tools.CallTool("load_trace_artifact", new Dictionary<string, object>
		{
			["artifact_id"] = artifactId
		});
		AssertEqual(true, loadResult["isError"], "escaped artifact normalized path isError");
	}

	private static void AssertUnsupportedTracyFileVersionTool(string path)
	{
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
		{
			ProfilerMcpTools tools = new ProfilerMcpTools();
			Dictionary<string, object> result = tools.CallTool("load_trace_file", new Dictionary<string, object>
			{
				["path"] = path,
				["format"] = "tracy"
			});
			AssertEqual(true, result["isError"], "unsupported tracy load_trace_file isError");
			IDictionary structured = result["structuredContent"] as IDictionary;
			AssertEqual("TracyUnsupportedFileVersion", structured["errorCode"], "unsupported tracy error code");
			AssertEqual("0.11.0", structured["detectedVersion"], "unsupported tracy detected version");
			AssertEqual("0.10.0", structured["lockedVersion"], "unsupported tracy locked version");
			AssertHasItems(structured["supportedVersions"], "unsupported tracy supported versions");
			AssertDiagnosticCode(structured["diagnostics"], "TracyUnsupportedFileVersion", "unsupported tracy diagnostics");
			IDictionary tracyStatus = structured["tracyStatus"] as IDictionary;
			AssertEqual("0.10.0", tracyStatus["lockedVersion"], "unsupported tracy status locked version");
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertInvalidTracyFileTool(string path)
	{
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
		{
			ProfilerMcpTools tools = new ProfilerMcpTools();
			Dictionary<string, object> result = tools.CallTool("load_trace_file", new Dictionary<string, object>
			{
				["path"] = path,
				["format"] = "tracy"
			});
			AssertEqual(true, result["isError"], "invalid tracy load_trace_file isError");
			IDictionary structured = result["structuredContent"] as IDictionary;
			AssertEqual("TracyFileFormatInvalid", structured["errorCode"], "invalid tracy error code");
			AssertDiagnosticCode(structured["diagnostics"], "TracyFileFormatInvalid", "invalid tracy diagnostics");
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertTracyFileSizeLimitTool(string path)
	{
		AssertThrows(() => Tracy010FileReader.ReadHeader(path), "oversized tracy file reader");
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
		{
			ProfilerMcpTools tools = new ProfilerMcpTools();
			Dictionary<string, object> result = tools.CallTool("load_trace_file", new Dictionary<string, object>
			{
				["path"] = path,
				["format"] = "tracy"
			});
			AssertEqual(true, result["isError"], "oversized tracy load_trace_file isError");
			IDictionary structured = result["structuredContent"] as IDictionary;
			AssertEqual("TracyFileSizeLimitExceeded", structured["errorCode"], "oversized tracy error code");
			AssertDiagnosticCode(structured["diagnostics"], "TracyFileSizeLimitExceeded", "oversized tracy diagnostics");
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertOnDemandSystemFramesSkipped(string path)
	{
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
		{
			ProfilerMcpTools tools = new ProfilerMcpTools();
			Dictionary<string, object> result = tools.CallTool("load_trace_file", new Dictionary<string, object>
			{
				["path"] = path,
				["format"] = "tracy",
				["keep_session"] = true
			});
			AssertEqual(false, result["isError"], "on-demand tracy load_trace_file isError");
			IDictionary structured = result["structuredContent"] as IDictionary;
			string artifactId = Convert.ToString(structured["artifactId"]);
			AssertOnDemandNormalizedFrames(root: artifactRoot, artifactId: artifactId);
			AssertOnDemandArtifactReloadFrames(tools, artifactId);
			string sessionId = Convert.ToString(structured["sessionId"]);
			Dictionary<string, object> slowFramesResult = tools.CallTool("find_slow_frames", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["top"] = 1
			});
			AssertEqual(false, slowFramesResult["isError"], "on-demand tracy find_slow_frames isError");
			IDictionary slowFrames = slowFramesResult["structuredContent"] as IDictionary;
			IList frames = slowFrames["slowFrames"] as IList;
			AssertHasItems(frames, "on-demand slow frames");
			IDictionary firstFrame = frames[0] as IDictionary;
			AssertEqual(0, firstFrame["frameIndex"], "on-demand visible frame index");
			AssertEqual(16.0, firstFrame["durationMs"], "on-demand visible frame duration");
			tools.CallTool("close_session", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertOnDemandNormalizedFrames(string root, string artifactId)
	{
		string normalizedRoot = Path.Combine(root, artifactId, "normalized");
		string manifestPath = Path.Combine(normalizedRoot, "manifest.json");
		if (!File.Exists(manifestPath))
		{
			throw new InvalidOperationException("on-demand normalized manifest is missing.");
		}
		using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
		int frameCount = manifest.RootElement.GetProperty("frameCount").GetInt32();
		AssertEqual(2, frameCount, "on-demand normalized manifest frame count");

		string framesPath = Path.Combine(normalizedRoot, "frames.ndjson");
		if (!File.Exists(framesPath))
		{
			throw new InvalidOperationException("on-demand normalized frames file is missing.");
		}
		int visibleFrameLines = File.ReadLines(framesPath).Count(line => !string.IsNullOrWhiteSpace(line));
		AssertEqual(2, visibleFrameLines, "on-demand normalized frame line count");
	}

	private static void AssertOnDemandArtifactReloadFrames(ProfilerMcpTools tools, string artifactId)
	{
		Dictionary<string, object> reloadResult = tools.CallTool("load_trace_artifact", new Dictionary<string, object>
		{
			["artifact_id"] = artifactId,
			["keep_session"] = true
		});
		AssertEqual(false, reloadResult["isError"], "on-demand artifact reload isError");
		IDictionary reload = reloadResult["structuredContent"] as IDictionary;
		IDictionary summary = reload["summary"] as IDictionary;
		AssertEqual(2, summary["frameCount"], "on-demand artifact summary frame count");
		string reloadSessionId = Convert.ToString(reload["sessionId"]);
		Dictionary<string, object> frameResult = tools.CallTool("analyze_frame", new Dictionary<string, object>
		{
			["session_id"] = reloadSessionId,
			["frame_index"] = 0,
			["neighbor_count"] = 0
		});
		AssertEqual(false, frameResult["isError"], "on-demand artifact analyze_frame isError");
		IDictionary frameAnalysis = frameResult["structuredContent"] as IDictionary;
		IDictionary frame = frameAnalysis["frame"] as IDictionary;
		AssertEqual(16.0, frame["durationMs"], "on-demand artifact visible frame duration");
		tools.CallTool("close_session", new Dictionary<string, object>
		{
			["session_id"] = reloadSessionId
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

		Dictionary<string, object> frameResult = tools.CallTool("analyze_frame", new Dictionary<string, object>
		{
			["session_id"] = sessionId,
			["frame_index"] = 0
		});
		AssertEqual(false, frameResult["isError"], "tracy analyze_frame isError");
		IDictionary frame = frameResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", frame["sourceFormat"], "tracy frame source format");
		AssertEqual(false, frame["supported"], "tracy frame supported");
		AssertHasItems(frame["diagnostics"], "tracy frame diagnostics");

		Dictionary<string, object> frameDetailResult = tools.CallTool("analyze_frame_detail", new Dictionary<string, object>
		{
			["session_id"] = sessionId,
			["frame_index"] = 0
		});
		AssertEqual(false, frameDetailResult["isError"], "tracy analyze_frame_detail isError");
		IDictionary frameDetail = frameDetailResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", frameDetail["sourceFormat"], "tracy frame detail source format");
		AssertEqual(false, frameDetail["supported"], "tracy frame detail supported");
		AssertHasItems(frameDetail["diagnostics"], "tracy frame detail diagnostics");

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
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
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
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertTracyZoneQueryTools(string path)
	{
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
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
			string artifactId = Convert.ToString(kept["artifactId"]);
			if (string.IsNullOrWhiteSpace(artifactId))
			{
				throw new InvalidOperationException("zone load_trace_file must return artifactId.");
			}
			AssertTraceArtifactListed(tools, artifactRoot, artifactId, "file-import");
			AssertTraceArtifactNormalizedFiles(artifactRoot, artifactId, expectZone: true, expectPlot: false);
			IDictionary summary = kept["summary"] as IDictionary;
			AssertEqual(1, summary["threadCount"], "zone summary thread count");
			AssertEqual(1, summary["zoneCount"], "zone summary zone count");
			AssertHasItems(kept["threads"], "zone load trace threads");
			IDictionary loadedThread = ((IList)kept["threads"])[0] as IDictionary;
			AssertEqual(123UL, loadedThread["threadId"], "zone loaded thread id");
			AssertEqual("RenderThread", loadedThread["name"], "zone loaded thread name");
			AssertMarkdownContains(keptResult, "| 123 | RenderThread |", "zone markdown thread row");
			AssertTraceLoadedSessionListed(tools, sessionId, "tracy", 2, 1);

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
			AssertEqual(false, range["framesUnavailable"], "zone range frames unavailable");
			AssertEqual(false, ((IDictionary)range["range"])["framesUnavailable"], "zone nested range frames unavailable");
			AssertHasItems(range["scopeHotspots"], "zone range hotspots");

			Dictionary<string, object> timeRangeResult = tools.CallTool("analyze_time_range", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["start_time_ns"] = 1_000_000L,
				["end_time_ns"] = 6_000_000L,
				["top"] = 5
			});
			AssertEqual(false, timeRangeResult["isError"], "zone analyze_time_range by time isError");
			IDictionary timeRange = timeRangeResult["structuredContent"] as IDictionary;
			AssertEqual("tracy", timeRange["sourceFormat"], "zone time range source format");
			AssertEqual(1_000_000L, timeRange["startTimeNs"], "zone time range start ns");
			AssertEqual(6_000_000L, timeRange["endTimeNs"], "zone time range end ns");
			AssertEqual(false, timeRange["framesUnavailable"], "zone time range frames unavailable");
			AssertHasItems(timeRange["scopeHotspots"], "zone time range hotspots");

			Dictionary<string, object> clippedTimeRangeResult = tools.CallTool("analyze_time_range", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["start_time_ns"] = 2_000_000L,
				["end_time_ns"] = 3_000_000L,
				["top"] = 5
			});
			AssertEqual(false, clippedTimeRangeResult["isError"], "zone clipped time range isError");
			IDictionary clippedTimeRange = clippedTimeRangeResult["structuredContent"] as IDictionary;
			AssertHasItems(clippedTimeRange["scopeHotspots"], "zone clipped time range hotspots");
			IDictionary clippedHotspot = ((IList)clippedTimeRange["scopeHotspots"])[0] as IDictionary;
			AssertEqual(1.0, clippedHotspot["totalMs"], "zone clipped time range total ms");

			Dictionary<string, object> frameDetailResult = tools.CallTool("analyze_frame_detail", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["frame_index"] = 0,
				["max_nodes"] = 1,
				["min_duration_ms"] = 1.0
			});
			AssertEqual(false, frameDetailResult["isError"], "zone analyze_frame_detail isError");
			IDictionary frameDetail = frameDetailResult["structuredContent"] as IDictionary;
			AssertEqual("tracy", frameDetail["sourceFormat"], "zone frame detail source format");
			AssertEqual(true, frameDetail["supported"], "zone frame detail supported");
			AssertEqual(false, ((IDictionary)frameDetail["range"])["framesUnavailable"], "zone frame detail range frames unavailable");
			AssertHasItems(frameDetail["topSpans"], "zone frame detail top spans");
			IDictionary topSpan = ((IList)frameDetail["topSpans"])[0] as IDictionary;
			AssertEqual("SelfTestZone", topSpan["name"], "zone frame detail top span name");
			AssertEqual(4.0, topSpan["durationMs"], "zone frame detail top span duration");
			IDictionary nodeStats = frameDetail["nodeStats"] as IDictionary;
			AssertEqual(1, nodeStats["includedNodeCount"], "zone frame detail included nodes");
			AssertEqual(0, nodeStats["omittedNodeCount"], "zone frame detail omitted nodes");
			AssertEqual(false, nodeStats["truncated"], "zone frame detail truncated");

			tools.CallTool("close_session", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
			AssertTraceArtifactReloadsZone(tools, artifactId);
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertTracyPlotQueryTools(string path)
	{
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
		{
			ProfilerMcpTools tools = new ProfilerMcpTools();
			Dictionary<string, object> keptResult = tools.CallTool("load_trace_file", new Dictionary<string, object>
			{
				["path"] = path,
				["format"] = "auto",
				["keep_session"] = true
			});
			AssertEqual(false, keptResult["isError"], "plot load_trace_file isError");
			IDictionary kept = keptResult["structuredContent"] as IDictionary;
			string sessionId = Convert.ToString(kept["sessionId"]);
			string artifactId = Convert.ToString(kept["artifactId"]);
			if (string.IsNullOrWhiteSpace(artifactId))
			{
				throw new InvalidOperationException("plot load_trace_file must return artifactId.");
			}
			AssertTraceArtifactListed(tools, artifactRoot, artifactId, "file-import");
			AssertTraceArtifactNormalizedFiles(artifactRoot, artifactId, expectZone: false, expectPlot: true);
			IDictionary summary = kept["summary"] as IDictionary;
			AssertEqual(1, summary["plotCount"], "plot summary plot count");
			AssertEqual(true, summary["eventsDecoded"], "plot summary events decoded");
			IDictionary capabilities = kept["capabilities"] as IDictionary;
			AssertEqual(true, capabilities["counters"], "plot summary counters capability");

			Dictionary<string, object> countersResult = tools.CallTool("list_counters", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["top"] = 5
			});
			AssertEqual(false, countersResult["isError"], "plot list_counters isError");
			IDictionary counters = countersResult["structuredContent"] as IDictionary;
			AssertEqual(true, counters["eventsDecoded"], "plot counters events decoded");
			AssertHasItems(counters["counters"], "plot counters");
			IDictionary firstCounter = ((IList)counters["counters"])[0] as IDictionary;
			AssertEqual("FrameTime", firstCounter["name"], "plot counter name");
			AssertEqual(2, firstCounter["sampleCount"], "plot counter sample count");
			AssertMarkdownContains(countersResult, "| FrameTime | Double |  | 2 | 20 |", "plot counter markdown row");

			Dictionary<string, object> samplesResult = tools.CallTool("query_counter", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["counter_name"] = "FrameTime",
				["max_samples"] = 10
			});
			AssertEqual(false, samplesResult["isError"], "plot query_counter isError");
			IDictionary samples = samplesResult["structuredContent"] as IDictionary;
			AssertEqual("FrameTime", samples["counterName"], "plot sample counter name");
			AssertEqual(2, samples["sampleCount"], "plot sample count");
			IDictionary sampleRange = samples["range"] as IDictionary;
			AssertEqual(0, sampleRange["startFrame"], "plot sample range start frame");
			AssertEqual(1, sampleRange["endFrame"], "plot sample range end frame");
			AssertHasItems(samples["samples"], "plot samples");
			IDictionary secondSample = ((IList)samples["samples"])[1] as IDictionary;
			AssertEqual(20.0, secondSample["value"], "plot second sample value");
			AssertMarkdownContains(samplesResult, "|  | 2000000 | 20 |  |", "plot sample markdown row");

			Dictionary<string, object> missingSamplesResult = tools.CallTool("query_counter", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["counter_name"] = "MissingCounter",
				["max_samples"] = 10
			});
			AssertEqual(false, missingSamplesResult["isError"], "plot missing query_counter isError");
			IDictionary missingSamples = missingSamplesResult["structuredContent"] as IDictionary;
			AssertEqual(true, missingSamples["eventsDecoded"], "plot missing counter events decoded");
			AssertEqual(0, missingSamples["sampleCount"], "plot missing counter sample count");
			AssertDiagnosticCode(missingSamples["diagnostics"], "TracyCounterNotFound", "plot missing counter diagnostics");

			tools.CallTool("close_session", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertTracyPlotFrameRangeQueryTools(string path)
	{
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
		{
			ProfilerMcpTools tools = new ProfilerMcpTools();
			Dictionary<string, object> keptResult = tools.CallTool("load_trace_file", new Dictionary<string, object>
			{
				["path"] = path,
				["format"] = "auto",
				["keep_session"] = true
			});
			AssertEqual(false, keptResult["isError"], "plot range load_trace_file isError");
			IDictionary kept = keptResult["structuredContent"] as IDictionary;
			string sessionId = Convert.ToString(kept["sessionId"]);

			Dictionary<string, object> defaultSamplesResult = tools.CallTool("query_counter", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["counter_name"] = "FrameTime",
				["max_samples"] = 10
			});
			AssertEqual(false, defaultSamplesResult["isError"], "plot default range query_counter isError");
			IDictionary defaultSamples = defaultSamplesResult["structuredContent"] as IDictionary;
			AssertEqual(3, defaultSamples["sampleCount"], "plot default range sample count");
			AssertEqual(3, defaultSamples["availableSampleCount"], "plot default range available sample count");
			IDictionary defaultRange = defaultSamples["range"] as IDictionary;
			AssertEqual(0, defaultRange["startFrame"], "plot default range start frame");
			AssertEqual(1, defaultRange["endFrame"], "plot default range end frame");

			Dictionary<string, object> samplesResult = tools.CallTool("query_counter", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["counter_name"] = "FrameTime",
				["start_frame"] = 1,
				["end_frame"] = 1,
				["max_samples"] = 10
			});
			AssertEqual(false, samplesResult["isError"], "plot range query_counter isError");
			IDictionary samples = samplesResult["structuredContent"] as IDictionary;
			AssertEqual(1, samples["sampleCount"], "plot range sample count");
			AssertEqual(1, samples["availableSampleCount"], "plot range available sample count");
			IList sampleRows = samples["samples"] as IList;
			AssertHasItems(sampleRows, "plot range samples");
			IDictionary firstSample = sampleRows[0] as IDictionary;
			AssertEqual(20.0, firstSample["value"], "plot range sample value");

			Dictionary<string, object> openEndedSamplesResult = tools.CallTool("query_counter", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["counter_name"] = "FrameTime",
				["start_frame"] = 1,
				["max_samples"] = 10
			});
			AssertEqual(false, openEndedSamplesResult["isError"], "plot open-ended range query_counter isError");
			IDictionary openEndedSamples = openEndedSamplesResult["structuredContent"] as IDictionary;
			AssertEqual(1, openEndedSamples["sampleCount"], "plot open-ended range sample count");
			IDictionary openEndedRange = openEndedSamples["range"] as IDictionary;
			AssertEqual(1, openEndedRange["startFrame"], "plot open-ended range start frame");
			AssertEqual(1, openEndedRange["endFrame"], "plot open-ended range end frame");
			IList openEndedRows = openEndedSamples["samples"] as IList;
			AssertHasItems(openEndedRows, "plot open-ended range samples");
			IDictionary openEndedFirstSample = openEndedRows[0] as IDictionary;
			AssertEqual(20.0, openEndedFirstSample["value"], "plot open-ended range sample value");

			tools.CallTool("close_session", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertTraceLoadedSessionListed(ProfilerMcpTools tools, string sessionId, string sourceFormat, int frameCount, int threadCount)
	{
		Dictionary<string, object> result = tools.CallTool("list_sessions", new Dictionary<string, object>());
		AssertEqual(false, result["isError"], "list_sessions isError");
		IDictionary structured = result["structuredContent"] as IDictionary;
		AssertHasItems(structured["sessions"], "loaded sessions");
		foreach (object item in (IList)structured["sessions"])
		{
			IDictionary session = item as IDictionary;
			if (session != null && Convert.ToString(session["sessionId"]) == sessionId)
			{
				AssertEqual(sourceFormat, session["sourceFormat"], "loaded trace session source format");
				AssertEqual(frameCount, session["frameCount"], "loaded trace session frame count");
				AssertEqual(threadCount, session["threadCount"], "loaded trace session thread count");
				return;
			}
		}
		throw new InvalidOperationException("loaded trace session was not listed: " + sessionId);
	}

	private static void AssertLoadTraceFileDiagnostics(string path, string expectedCode)
	{
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
		{
			ProfilerMcpTools tools = new ProfilerMcpTools();
			Dictionary<string, object> keptResult = tools.CallTool("load_trace_file", new Dictionary<string, object>
			{
				["path"] = path,
				["format"] = "auto",
				["keep_session"] = true
			});
			AssertEqual(false, keptResult["isError"], "diagnostic load_trace_file isError");
			IDictionary kept = keptResult["structuredContent"] as IDictionary;
			string sessionId = Convert.ToString(kept["sessionId"]);
			string artifactId = Convert.ToString(kept["artifactId"]);
			if (string.IsNullOrWhiteSpace(artifactId))
			{
				throw new InvalidOperationException("diagnostic load_trace_file must return artifactId.");
			}
			Dictionary<string, object> diagnosticsResult = tools.CallTool("get_import_diagnostics", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
			AssertEqual(false, diagnosticsResult["isError"], "diagnostic get_import_diagnostics isError");
			IDictionary diagnostics = diagnosticsResult["structuredContent"] as IDictionary;
			AssertDiagnosticCode(diagnostics["diagnostics"], expectedCode, "load trace diagnostics");
			Dictionary<string, object> artifactDiagnosticsResult = tools.CallTool("get_import_diagnostics", new Dictionary<string, object>
			{
				["artifact_id"] = artifactId
			});
			AssertEqual(false, artifactDiagnosticsResult["isError"], "artifact diagnostic get_import_diagnostics isError");
			IDictionary artifactDiagnostics = artifactDiagnosticsResult["structuredContent"] as IDictionary;
			AssertDiagnosticCode(artifactDiagnostics["diagnostics"], expectedCode, "artifact diagnostics");
			tools.CallTool("close_session", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
			Dictionary<string, object> reloadResult = tools.CallTool("load_trace_artifact", new Dictionary<string, object>
			{
				["artifact_id"] = artifactId,
				["keep_session"] = true
			});
			AssertEqual(false, reloadResult["isError"], "diagnostic load_trace_artifact isError");
			IDictionary reloaded = reloadResult["structuredContent"] as IDictionary;
			string reloadedSessionId = Convert.ToString(reloaded["sessionId"]);
			if (string.IsNullOrWhiteSpace(reloadedSessionId))
			{
				throw new InvalidOperationException("diagnostic load_trace_artifact must return sessionId.");
			}
			Dictionary<string, object> reloadedDiagnosticsResult = tools.CallTool("get_import_diagnostics", new Dictionary<string, object>
			{
				["session_id"] = reloadedSessionId
			});
			AssertEqual(false, reloadedDiagnosticsResult["isError"], "reloaded diagnostic get_import_diagnostics isError");
			IDictionary reloadedDiagnostics = reloadedDiagnosticsResult["structuredContent"] as IDictionary;
			AssertDiagnosticCode(reloadedDiagnostics["diagnostics"], expectedCode, "reloaded artifact diagnostics");
			tools.CallTool("close_session", new Dictionary<string, object>
			{
				["session_id"] = reloadedSessionId
			});
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static void AssertTracyLiveCaptureTool()
	{
		string artifactRoot = ResetSelfTestArtifactRoot();
		try
		{
			using CancellationTokenSource canceledCapture = new CancellationTokenSource();
			canceledCapture.Cancel();
			AssertThrows(
				() => Tracy010LiveCaptureClient.Capture("127.0.0.1", 1, 1, canceledCapture.Token),
				"tracy live capture cancellation");

			using FakeTracyServer stalledWelcomeServer = new FakeTracyServer(1, stallBeforeWelcome: true);
			stalledWelcomeServer.Start();
			using CancellationTokenSource stalledWelcomeCancellation = new CancellationTokenSource();
			stalledWelcomeCancellation.CancelAfter(100);
			long stalledWelcomeStart = Environment.TickCount64;
			AssertThrows(
				() => Tracy010LiveCaptureClient.Capture("127.0.0.1", stalledWelcomeServer.Port, 1, stalledWelcomeCancellation.Token),
				"tracy live welcome read cancellation");
			long stalledWelcomeElapsed = Environment.TickCount64 - stalledWelcomeStart;
			if (stalledWelcomeElapsed > 2000)
			{
				throw new InvalidOperationException("tracy live welcome read cancellation took " + stalledWelcomeElapsed + " ms.");
			}
			stalledWelcomeServer.AssertHandshakeReceived();

			using FakeTracyServer server = new FakeTracyServer();
			server.Start();
			ProfilerMcpTools tools = new ProfilerMcpTools();
			Dictionary<string, object> result = tools.CallTool("capture_profile", new Dictionary<string, object>
			{
				["url"] = "pc://127.0.0.1:" + server.Port,
				["protocol"] = "tracy",
				["duration_seconds"] = 1,
				["top"] = 5,
				["keep_session"] = true
			});
			AssertEqual(false, result["isError"], "tracy live capture isError");
			IDictionary structured = result["structuredContent"] as IDictionary;
			AssertEqual("tracy", structured["sourceFormat"], "tracy live source format");
			AssertEqual("tracy", ((IDictionary)structured["capture"])["protocol"], "tracy live capture protocol");
			string artifactId = Convert.ToString(structured["artifactId"]);
			if (string.IsNullOrWhiteSpace(artifactId))
			{
				throw new InvalidOperationException("tracy live capture must return artifactId.");
			}
			AssertTraceArtifactListed(tools, artifactRoot, artifactId, "tracy-live-normalized-only");
			AssertTraceArtifactLiveCaptureDebugMaterial(artifactRoot, artifactId);
			string sessionId = Convert.ToString(structured["sessionId"]);
			if (string.IsNullOrWhiteSpace(sessionId))
			{
				throw new InvalidOperationException("tracy live capture keep_session=true must return sessionId.");
			}
			IDictionary summary = structured["summary"] as IDictionary;
			AssertEqual("FakeTracyProgram", summary["captureProgram"], "tracy live capture program");
			AssertEqual("FakeTracyHost", summary["hostInfo"], "tracy live host info");
			AssertEqual(31415UL, summary["processId"], "tracy live pid");

			Dictionary<string, object> summaryResult = tools.CallTool("get_session_summary", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
			AssertEqual(false, summaryResult["isError"], "tracy live summary isError");
			IDictionary loadedSummary = summaryResult["structuredContent"] as IDictionary;
			AssertEqual("tracy", loadedSummary["sourceFormat"], "tracy live loaded source format");
			Dictionary<string, object> diagnosticsResult = tools.CallTool("get_import_diagnostics", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
			AssertEqual(false, diagnosticsResult["isError"], "tracy live diagnostics isError");
			IDictionary diagnostics = diagnosticsResult["structuredContent"] as IDictionary;
			AssertEqual("tracy", diagnostics["sourceFormat"], "tracy diagnostics source format");
			AssertHasItems(diagnostics["diagnostics"], "tracy diagnostics");
			AssertTracyLiveSaveSessionFileRejected(tools, sessionId);
			tools.CallTool("close_session", new Dictionary<string, object>
			{
				["session_id"] = sessionId
			});
			server.AssertHandshakeReceived();

			int refusedPort = ReserveClosedTcpPort();
			Dictionary<string, object> connectFailedResult = tools.CallTool("capture_profile", new Dictionary<string, object>
			{
				["url"] = "pc://127.0.0.1:" + refusedPort,
				["protocol"] = "tracy",
				["duration_seconds"] = 1
			});
			AssertEqual(true, connectFailedResult["isError"], "tracy live connect failed isError");
			IDictionary connectFailed = connectFailedResult["structuredContent"] as IDictionary;
			AssertEqual("TracyConnectFailed", connectFailed["errorCode"], "tracy live connect failed error code");
			AssertEqual("0.10.0", connectFailed["lockedVersion"], "tracy live connect failed locked version");
			AssertDiagnosticCode(connectFailed["diagnostics"], "TracyConnectFailed", "tracy live connect failed diagnostics");

			using FakeTracyServer rejectedServer = new FakeTracyServer(2);
			rejectedServer.Start();
			Dictionary<string, object> rejectedResult = tools.CallTool("capture_profile", new Dictionary<string, object>
			{
				["url"] = "pc://127.0.0.1:" + rejectedServer.Port,
				["protocol"] = "tracy",
				["duration_seconds"] = 1
			});
			AssertEqual(true, rejectedResult["isError"], "tracy live protocol mismatch isError");
			IDictionary rejected = rejectedResult["structuredContent"] as IDictionary;
			AssertEqual("TracyProtocolMismatch", rejected["errorCode"], "tracy live protocol mismatch error code");
			AssertEqual("protocol:64,status:2", rejected["detectedVersion"], "tracy live protocol mismatch detected version");
			AssertEqual("0.10.0", rejected["lockedVersion"], "tracy live protocol mismatch locked version");
			AssertHasItems(rejected["supportedVersions"], "tracy live protocol mismatch supported versions");
			AssertDiagnosticCode(rejected["diagnostics"], "TracyProtocolMismatch", "tracy live protocol mismatch diagnostics");
			rejectedServer.AssertHandshakeReceived();
		}
		finally
		{
			CleanupSelfTestArtifactRoot(artifactRoot);
		}
	}

	private static int ReserveClosedTcpPort()
	{
		using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}

	private static string ResetSelfTestArtifactRoot()
	{
		string root = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-artifacts");
		if (Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
		Environment.SetEnvironmentVariable("PROFILER_STUDY_TRACE_ARTIFACT_ROOT", root);
		return root;
	}

	private static void CleanupSelfTestArtifactRoot(string root)
	{
		if (Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
		Environment.SetEnvironmentVariable("PROFILER_STUDY_TRACE_ARTIFACT_ROOT", null);
	}

	private static void AssertTraceArtifactListed(ProfilerMcpTools tools, string root, string artifactId, string sourceKind)
	{
		Dictionary<string, object> listResult = tools.CallTool("list_trace_artifacts", new Dictionary<string, object>
		{
			["format"] = "tracy",
			["limit"] = 20
		});
		AssertEqual(false, listResult["isError"], "list_trace_artifacts isError");
		IDictionary structured = listResult["structuredContent"] as IDictionary;
		AssertEqual(root, structured["root"], "trace artifact root");
		AssertHasItems(structured["artifacts"], "trace artifacts");
		bool found = false;
		foreach (object item in (IList)structured["artifacts"])
		{
			IDictionary artifact = item as IDictionary;
			if (artifact != null && Convert.ToString(artifact["artifactId"]) == artifactId)
			{
				AssertEqual("tracy", artifact["sourceFormat"], "trace artifact source format");
				AssertEqual(sourceKind, artifact["sourceKind"], "trace artifact source kind");
				AssertArtifactListSizeFields(artifact, sourceKind);
				found = true;
			}
		}
		if (!found)
		{
			throw new InvalidOperationException("trace artifact was not listed: " + artifactId);
		}
		Dictionary<string, object> futureListResult = tools.CallTool("list_trace_artifacts", new Dictionary<string, object>
		{
			["format"] = "tracy",
			["since"] = DateTime.UtcNow.AddMinutes(5).ToString("o"),
			["limit"] = 20
		});
		AssertEqual(false, futureListResult["isError"], "list_trace_artifacts future since isError");
		IDictionary futureList = futureListResult["structuredContent"] as IDictionary;
		AssertEqual(0, ((IList)futureList["artifacts"]).Count, "future since trace artifact count");
		AssertTraceArtifactManifest(root, artifactId, sourceKind);
		AssertTraceArtifactListAllContainsOnlyRegisteredArtifacts(tools);
	}

	private static void AssertTraceArtifactLiveCaptureDebugMaterial(string root, string artifactId)
	{
		string path = Path.Combine(root, artifactId, "capture.ndjson");
		if (!File.Exists(path))
		{
			throw new InvalidOperationException("tracy live capture artifact must include capture.ndjson.");
		}
		string line = File.ReadLines(path).FirstOrDefault();
		if (string.IsNullOrWhiteSpace(line))
		{
			throw new InvalidOperationException("tracy live capture.ndjson must contain capture metadata.");
		}
		using JsonDocument document = JsonDocument.Parse(line);
		AssertEqual("tracy-live-normalized-only", document.RootElement.GetProperty("sourceKind").GetString(), "tracy live capture debug source kind");
		AssertEqual("tracy", document.RootElement.GetProperty("sourceFormat").GetString(), "tracy live capture debug source format");
		AssertEqual("0.10.0", document.RootElement.GetProperty("tracyVersion").GetString(), "tracy live capture debug tracy version");
	}

	private static void AssertTraceArtifactManifest(string root, string artifactId, string sourceKind)
	{
		string manifestPath = Path.Combine(root, artifactId, "manifest.json");
		if (!File.Exists(manifestPath))
		{
			throw new InvalidOperationException("trace artifact manifest is missing: " + manifestPath);
		}
		using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
		if (manifest.RootElement.TryGetProperty("ArtifactId", out _))
		{
			throw new InvalidOperationException("trace artifact manifest must use camelCase JSON fields.");
		}
		AssertEqual(artifactId, manifest.RootElement.GetProperty("artifactId").GetString(), "trace artifact manifest artifact id");
		AssertEqual("tracy", manifest.RootElement.GetProperty("sourceFormat").GetString(), "trace artifact manifest source format");
		AssertEqual(sourceKind, manifest.RootElement.GetProperty("sourceKind").GetString(), "trace artifact manifest source kind");
		AssertEqual("ProfilerStudyCore.Tracy", manifest.RootElement.GetProperty("implementation").GetString(), "trace artifact manifest implementation");
		long sourceByteCount = manifest.RootElement.GetProperty("sourceByteCount").GetInt64();
		long normalizedByteCount = manifest.RootElement.GetProperty("normalizedByteCount").GetInt64();
		long diagnosticsByteCount = manifest.RootElement.GetProperty("diagnosticsByteCount").GetInt64();
		if (string.Equals(sourceKind, "file-import", StringComparison.OrdinalIgnoreCase) && sourceByteCount <= 0)
		{
			throw new InvalidOperationException("file-import artifact manifest must record sourceByteCount.");
		}
		if (normalizedByteCount <= 0)
		{
			throw new InvalidOperationException("trace artifact manifest must record normalizedByteCount.");
		}
		if (diagnosticsByteCount <= 0)
		{
			throw new InvalidOperationException("trace artifact manifest must record diagnosticsByteCount.");
		}
	}

	private static void AssertArtifactListSizeFields(IDictionary artifact, string sourceKind)
	{
		long sourceByteCount = Convert.ToInt64(artifact["sourceByteCount"]);
		long normalizedByteCount = Convert.ToInt64(artifact["normalizedByteCount"]);
		long diagnosticsByteCount = Convert.ToInt64(artifact["diagnosticsByteCount"]);
		if (string.Equals(sourceKind, "file-import", StringComparison.OrdinalIgnoreCase) && sourceByteCount <= 0)
		{
			throw new InvalidOperationException("list_trace_artifacts must return sourceByteCount for file imports.");
		}
		if (normalizedByteCount <= 0)
		{
			throw new InvalidOperationException("list_trace_artifacts must return normalizedByteCount.");
		}
		if (diagnosticsByteCount <= 0)
		{
			throw new InvalidOperationException("list_trace_artifacts must return diagnosticsByteCount.");
		}
	}

	private static void AssertTraceArtifactListAllContainsOnlyRegisteredArtifacts(ProfilerMcpTools tools)
	{
		Dictionary<string, object> listResult = tools.CallTool("list_trace_artifacts", new Dictionary<string, object>
		{
			["limit"] = 20
		});
		AssertEqual(false, listResult["isError"], "list_trace_artifacts all isError");
		IDictionary structured = listResult["structuredContent"] as IDictionary;
		AssertHasItems(structured["artifacts"], "all trace artifacts");
		foreach (object item in (IList)structured["artifacts"])
		{
			IDictionary artifact = item as IDictionary;
			if (artifact == null || string.IsNullOrWhiteSpace(Convert.ToString(artifact["artifactId"])))
			{
				throw new InvalidOperationException("list_trace_artifacts must not return normalized manifests or empty artifact ids.");
			}
			AssertEqual(false, string.IsNullOrWhiteSpace(Convert.ToString(artifact["sourceKind"])), "all trace artifact source kind");
			AssertEqual(false, string.IsNullOrWhiteSpace(Convert.ToString(artifact["normalizedPath"])), "all trace artifact normalized path");
		}
	}

	private static void AssertTraceArtifactNormalizedFiles(string root, string artifactId, bool expectZone, bool expectPlot)
	{
		string normalizedRoot = Path.Combine(root, artifactId, "normalized");
		string[] requiredFiles =
		{
			"manifest.json",
			"threads.ndjson",
			"frames.ndjson",
			"cpu_zones.ndjson",
			"plots.ndjson",
			"diagnostics.json"
		};
		foreach (string fileName in requiredFiles)
		{
			string fullPath = Path.Combine(normalizedRoot, fileName);
			if (!File.Exists(fullPath))
			{
				throw new InvalidOperationException("normalized artifact file is missing: " + fullPath);
			}
		}
		if (expectZone && string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(normalizedRoot, "cpu_zones.ndjson"))))
		{
			throw new InvalidOperationException("cpu_zones.ndjson must contain decoded Tracy zones.");
		}
		if (expectPlot && string.IsNullOrWhiteSpace(File.ReadAllText(Path.Combine(normalizedRoot, "plots.ndjson"))))
		{
			throw new InvalidOperationException("plots.ndjson must contain decoded Tracy plots.");
		}
	}

	private static void AssertTraceArtifactReloadsZone(ProfilerMcpTools tools, string artifactId)
	{
		Dictionary<string, object> loadResult = tools.CallTool("load_trace_artifact", new Dictionary<string, object>
		{
			["artifact_id"] = artifactId,
			["keep_session"] = true
		});
		AssertEqual(false, loadResult["isError"], "load_trace_artifact isError");
		IDictionary loaded = loadResult["structuredContent"] as IDictionary;
		AssertEqual("tracy", loaded["sourceFormat"], "artifact source format");
		AssertEqual(artifactId, loaded["artifactId"], "artifact id");
		IDictionary summary = loaded["summary"] as IDictionary;
		AssertEqual(1, summary["zoneCount"], "artifact zone summary count");
		AssertHasItems(loaded["threads"], "artifact threads");
		IDictionary thread = ((IList)loaded["threads"])[0] as IDictionary;
		AssertEqual(123UL, thread["threadId"], "artifact thread id");
		AssertEqual("RenderThread", thread["name"], "artifact thread name");
		string sessionId = Convert.ToString(loaded["sessionId"]);
		if (string.IsNullOrWhiteSpace(sessionId))
		{
			throw new InvalidOperationException("load_trace_artifact keep_session=true must return sessionId.");
		}
		Dictionary<string, object> hotspotsResult = tools.CallTool("find_scope_hotspots", new Dictionary<string, object>
		{
			["session_id"] = sessionId,
			["top"] = 5
		});
		AssertEqual(false, hotspotsResult["isError"], "artifact find_scope_hotspots isError");
		IDictionary hotspots = hotspotsResult["structuredContent"] as IDictionary;
		AssertEqual(true, hotspots["eventsDecoded"], "artifact hotspots events decoded");
		AssertHasItems(hotspots["scopeHotspots"], "artifact scope hotspots");
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

	private static void WriteOversizedTracyDump(string path)
	{
		using FileStream stream = File.Create(path);
		stream.SetLength(512L * 1024L * 1024L + 1L);
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
		WriteTracyMetadataPrefix(inner, includeZoneString: true, includePlotString: false, includeThreadName: true);
		WriteUInt64(inner, 1);                      // localThreadCompress size
		WriteUInt64(inner, 123);                    // thread id
		WriteUInt64(inner, 0);                      // externalThreadCompress size
		WriteUInt64(inner, 1);                      // sourceLocation map count
		WriteUInt64(inner, 0x4000);                 // source location pointer
		WriteSourceLocationBase(inner, nameIndex: 0, line: 77);
		WriteUInt64(inner, 1);                      // sourceLocationExpand count
		WriteUInt64(inner, 0x4000);                 // expanded source location pointer
		WriteUInt64(inner, 0);                      // sourceLocationPayload count
		WriteUInt64(inner, 1);                      // sourceLocationZones count
		WriteInt16(inner, 0);                       // expanded source location id
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

	private static void WriteTracyDumpWithPlot(string path)
	{
		using MemoryStream inner = new MemoryStream();
		WriteTracyMetadataPrefix(inner, includeZoneString: false, includePlotString: true);
		WriteUInt64(inner, 0);                      // localThreadCompress size
		WriteUInt64(inner, 0);                      // externalThreadCompress size
		WriteUInt64(inner, 0);                      // sourceLocation map count
		WriteUInt64(inner, 0);                      // sourceLocationExpand count
		WriteUInt64(inner, 0);                      // sourceLocationPayload count
		WriteUInt64(inner, 0);                      // sourceLocationZones count
		WriteUInt64(inner, 0);                      // gpuSourceLocationZones count
		WriteUInt64(inner, 0);                      // lockMap count
		WriteUInt64(inner, 0);                      // messages count
		WriteUInt64(inner, 1);                      // zoneExtra count
		inner.Write(new byte[12], 0, 12);           // ZoneExtra
		WriteUInt64(inner, 0);                      // total CPU zone count
		WriteUInt64(inner, 0);                      // zoneChildren count
		WriteUInt64(inner, 0);                      // thread count
		WriteUInt64(inner, 0);                      // total GPU zone count
		WriteUInt64(inner, 0);                      // gpuChildren count
		WriteUInt64(inner, 0);                      // gpuData count
		WriteUInt64(inner, 1);                      // plot count
		inner.WriteByte(0);                         // PlotType.User
		inner.WriteByte(0);                         // PlotValueFormatting.Number
		inner.WriteByte(0);                         // showSteps
		inner.WriteByte(1);                         // fill
		WriteUInt32(inner, 0);                      // color
		WriteUInt64(inner, 0x2000);                 // name pointer
		WriteDouble(inner, 10.0);                   // min
		WriteDouble(inner, 20.0);                   // max
		WriteDouble(inner, 30.0);                   // sum
		WriteUInt64(inner, 2);                      // sample count
		WriteInt64(inner, 1_000_000);               // sample 0 time delta
		WriteDouble(inner, 10.0);
		WriteInt64(inner, 1_000_000);               // sample 1 time delta
		WriteDouble(inner, 20.0);
		WriteTracyDump(path, inner.ToArray());
	}

	private static void WriteTracyDumpWithPlotSeparatedFrames(string path)
	{
		using MemoryStream inner = new MemoryStream();
		WriteTracyMetadataPrefix(inner, includeZoneString: false, includePlotString: true, includeThreadName: false, onDemand: false, continuousFrameSet: false, separatedFrameSet: true);
		WriteUInt64(inner, 0);                      // localThreadCompress size
		WriteUInt64(inner, 0);                      // externalThreadCompress size
		WriteUInt64(inner, 0);                      // sourceLocation map count
		WriteUInt64(inner, 0);                      // sourceLocationExpand count
		WriteUInt64(inner, 0);                      // sourceLocationPayload count
		WriteUInt64(inner, 0);                      // sourceLocationZones count
		WriteUInt64(inner, 0);                      // gpuSourceLocationZones count
		WriteUInt64(inner, 0);                      // lockMap count
		WriteUInt64(inner, 0);                      // messages count
		WriteUInt64(inner, 1);                      // zoneExtra count
		inner.Write(new byte[12], 0, 12);           // ZoneExtra
		WriteUInt64(inner, 0);                      // total CPU zone count
		WriteUInt64(inner, 0);                      // zoneChildren count
		WriteUInt64(inner, 0);                      // thread count
		WriteUInt64(inner, 0);                      // total GPU zone count
		WriteUInt64(inner, 0);                      // gpuChildren count
		WriteUInt64(inner, 0);                      // gpuData count
		WriteUInt64(inner, 1);                      // plot count
		inner.WriteByte(0);                         // PlotType.User
		inner.WriteByte(0);                         // PlotValueFormatting.Number
		inner.WriteByte(0);                         // showSteps
		inner.WriteByte(1);                         // fill
		WriteUInt32(inner, 0);                      // color
		WriteUInt64(inner, 0x2000);                 // name pointer
		WriteDouble(inner, 10.0);                   // min
		WriteDouble(inner, 30.0);                   // max
		WriteDouble(inner, 60.0);                   // sum
		WriteUInt64(inner, 3);                      // sample count
		WriteInt64(inner, 500_000);                 // sample 0 absolute time 500000
		WriteDouble(inner, 10.0);
		WriteInt64(inner, 1_500_000);               // sample 1 absolute time 2000000
		WriteDouble(inner, 20.0);
		WriteInt64(inner, 6_000_000);               // sample 2 absolute time 8000000
		WriteDouble(inner, 30.0);
		WriteTracyDump(path, inner.ToArray());
	}

	private static void WriteTracyDumpWithGpuContext(string path)
	{
		using MemoryStream inner = new MemoryStream();
		WriteTracyMetadataPrefix(inner, includeZoneString: false);
		WriteUInt64(inner, 0);                      // localThreadCompress size
		WriteUInt64(inner, 0);                      // externalThreadCompress size
		WriteUInt64(inner, 0);                      // sourceLocation map count
		WriteUInt64(inner, 0);                      // sourceLocationExpand count
		WriteUInt64(inner, 0);                      // sourceLocationPayload count
		WriteUInt64(inner, 0);                      // sourceLocationZones count
		WriteUInt64(inner, 0);                      // gpuSourceLocationZones count
		WriteUInt64(inner, 0);                      // lockMap count
		WriteUInt64(inner, 0);                      // messages count
		WriteUInt64(inner, 0);                      // zoneExtra count
		WriteUInt64(inner, 0);                      // total CPU zone count
		WriteUInt64(inner, 0);                      // zoneChildren count
		WriteUInt64(inner, 0);                      // thread count
		WriteUInt64(inner, 3);                      // total GPU zone count
		WriteUInt64(inner, 0);                      // gpuChildren count
		WriteUInt64(inner, 1);                      // gpuData count
		WriteUInt64(inner, 456);                    // GPU context thread
		inner.WriteByte(0);                         // hasCalibration
		WriteUInt64(inner, 3);                      // context zone count
		WriteSingle(inner, 1.0f);                   // period
		inner.WriteByte(0);                         // GpuContextType.OpenGL
		WriteUInt32(inner, 0);                      // name StringIdx
		WriteUInt64(inner, 0);                      // overflow
		WriteUInt64(inner, 0);                      // context threadData count
		WriteUInt64(inner, 0);                      // plot count
		WriteTracyDump(path, inner.ToArray());
	}

	private static void WriteTracyDumpWithUnsupportedEvents(string path)
	{
		using MemoryStream inner = new MemoryStream();
		WriteTracyMetadataPrefix(inner, includeZoneString: false);
		WriteUInt64(inner, 0);                      // localThreadCompress size
		WriteUInt64(inner, 0);                      // externalThreadCompress size
		WriteUInt64(inner, 0);                      // sourceLocation map count
		WriteUInt64(inner, 0);                      // sourceLocationExpand count
		WriteUInt64(inner, 0);                      // sourceLocationPayload count
		WriteUInt64(inner, 0);                      // sourceLocationZones count
		WriteUInt64(inner, 0);                      // gpuSourceLocationZones count
		WriteUInt64(inner, 1);                      // lockMap count
		WriteUInt32(inner, 1);                      // lock id
		inner.Write(new byte[3], 0, 3);             // customName Int24
		WriteInt16(inner, -1);                      // srcloc
		inner.WriteByte(0);                         // LockType
		inner.WriteByte(1);                         // valid
		WriteInt64(inner, 1_000);                   // timeAnnounce
		WriteInt64(inner, 2_000);                   // timeTerminate
		WriteUInt64(inner, 1);                      // thread list count
		WriteUInt64(inner, 123);                    // thread id
		WriteUInt64(inner, 1);                      // lock timeline event count
		inner.Write(new byte[12], 0, 12);           // lock timeline event
		WriteUInt64(inner, 1);                      // messages count
		inner.Write(new byte[32], 0, 32);           // global message
		WriteUInt64(inner, 0);                      // zoneExtra count
		WriteUInt64(inner, 0);                      // total CPU zone count
		WriteUInt64(inner, 0);                      // zoneChildren count
		WriteUInt64(inner, 0);                      // thread count
		WriteUInt64(inner, 0);                      // total GPU zone count
		WriteUInt64(inner, 0);                      // gpuChildren count
		WriteUInt64(inner, 0);                      // gpuData count
		WriteUInt64(inner, 0);                      // plot count
		WriteUInt64(inner, 1);                      // memory arena count
		WriteUInt64(inner, 1);                      // total allocation count
		WriteUInt64(inner, 0);                      // memory arena name
		WriteUInt64(inner, 1);                      // arena allocation count
		WriteUInt64(inner, 0);                      // active allocation count
		WriteUInt64(inner, 0);                      // free allocation count
		WriteUInt64(inner, 0x1000);                 // allocation pointer
		WriteUInt64(inner, 64);                     // allocation size
		inner.Write(new byte[3], 0, 3);             // allocation callstack
		inner.Write(new byte[3], 0, 3);             // free callstack
		WriteInt64(inner, 1_000);                   // allocation time offset
		WriteInt64(inner, -1);                      // free time offset
		WriteInt16(inner, 123);                     // allocation thread
		WriteInt16(inner, 0);                       // free thread
		WriteInt64(inner, 64);                      // memory high
		WriteInt64(inner, 0);                       // memory low
		WriteInt64(inner, 64);                      // memory usage
		WriteUInt64(inner, 0);                      // memory plot name
		WriteUInt64(inner, 1);                      // callstack payload count
		WriteInt16(inner, 1);                       // payload frame count
		WriteUInt64(inner, 0x1234);                 // payload frame id
		WriteUInt64(inner, 1);                      // callstack frame map count
		WriteUInt64(inner, 0x1234);                 // frame id
		inner.WriteByte(1);                         // frame data size
		WriteUInt32(inner, 0);                      // frame image name
		WriteUInt32(inner, 0);                      // frame name
		WriteUInt32(inner, 0);                      // frame file
		WriteUInt32(inner, 0);                      // frame line
		WriteUInt64(inner, 0);                      // frame symbol address
		WriteUInt64(inner, 0);                      // appInfo count
		WriteTracyDump(path, inner.ToArray());
	}

	private static void WriteTracyDumpWithOversizedLockThreadList(string path)
	{
		using MemoryStream inner = new MemoryStream();
		WriteTracyMetadataPrefix(inner, includeZoneString: false);
		WriteUInt64(inner, 0);                      // localThreadCompress size
		WriteUInt64(inner, 0);                      // externalThreadCompress size
		WriteUInt64(inner, 0);                      // sourceLocation map count
		WriteUInt64(inner, 0);                      // sourceLocationExpand count
		WriteUInt64(inner, 0);                      // sourceLocationPayload count
		WriteUInt64(inner, 0);                      // sourceLocationZones count
		WriteUInt64(inner, 0);                      // gpuSourceLocationZones count
		WriteUInt64(inner, 1);                      // lockMap count
		WriteUInt32(inner, 1);                      // lock id
		inner.Write(new byte[3], 0, 3);             // customName Int24
		WriteInt16(inner, -1);                      // srcloc
		inner.WriteByte(0);                         // LockType
		inner.WriteByte(1);                         // valid
		WriteInt64(inner, 1_000);                   // timeAnnounce
		WriteInt64(inner, 2_000);                   // timeTerminate
		WriteUInt64(inner, ulong.MaxValue);         // invalid thread list count
		WriteTracyDump(path, inner.ToArray());
	}

	private static void WriteTracyDumpWithDictionaryBackReference(string path)
	{
		byte[] firstBlock = new byte[64 * 1024];
		firstBlock[0] = (byte)'t';
		firstBlock[1] = (byte)'r';
		firstBlock[2] = (byte)'a';
		firstBlock[3] = (byte)'c';
		firstBlock[4] = (byte)'y';
		firstBlock[5] = 0;
		firstBlock[6] = 10;
		firstBlock[7] = 0;

		using FileStream stream = File.Create(path);
		byte[] outerHeader = new byte[] { (byte)'t', (byte)'l', (byte)'Z', 4 };
		stream.Write(outerHeader, 0, outerHeader.Length);
		WriteCompressedBlock(stream, EncodeLz4LiteralBlock(firstBlock));
		WriteCompressedBlock(stream, new byte[] { 0x00, 0x01, 0x00 });
	}

	private static void WriteTracyDumpWithOnDemandFrames(string path)
	{
		using MemoryStream inner = new MemoryStream();
		WriteTracyMetadataPrefix(inner, includeZoneString: false, includePlotString: false, includeThreadName: false, onDemand: true, continuousFrameSet: true);
		WriteTracyDump(path, inner.ToArray());
	}

	private static void WriteTracyMetadataPrefix(MemoryStream inner, bool includeZoneString)
	{
		WriteTracyMetadataPrefix(inner, includeZoneString, includePlotString: false, includeThreadName: false);
	}

	private static void WriteTracyMetadataPrefix(MemoryStream inner, bool includeZoneString, bool includePlotString)
	{
		WriteTracyMetadataPrefix(inner, includeZoneString, includePlotString, includeThreadName: false);
	}

	private static void WriteTracyMetadataPrefix(MemoryStream inner, bool includeZoneString, bool includePlotString, bool includeThreadName)
	{
		WriteTracyMetadataPrefix(inner, includeZoneString, includePlotString, includeThreadName, onDemand: false, continuousFrameSet: false);
	}

	private static void WriteTracyMetadataPrefix(MemoryStream inner, bool includeZoneString, bool includePlotString, bool includeThreadName, bool onDemand, bool continuousFrameSet)
	{
		WriteTracyMetadataPrefix(inner, includeZoneString, includePlotString, includeThreadName, onDemand, continuousFrameSet, separatedFrameSet: false);
	}

	private static void WriteTracyMetadataPrefix(MemoryStream inner, bool includeZoneString, bool includePlotString, bool includeThreadName, bool onDemand, bool continuousFrameSet, bool separatedFrameSet)
	{
		inner.Write(new byte[] { (byte)'t', (byte)'r', (byte)'a', (byte)'c', (byte)'y', 0, 10, 0 });
		WriteInt64(inner, 0);                       // m_delay
		WriteInt64(inner, 1_000_000_000);           // m_resolution
		WriteDouble(inner, 1.0);                    // m_timerMul
		WriteInt64(inner, 16_666_667);              // lastTime
		WriteInt64(inner, onDemand ? 1 : 0);        // frameOffset
		WriteUInt64(inner, 4242);                   // pid
		WriteInt64(inner, 0);                       // samplingPeriod
		inner.WriteByte(2);                         // CpuArchX64
		WriteUInt32(inner, 0x12345678);             // cpuId
		WriteFixedAscii(inner, "SelfTestCpu", 12);  // cpuManufacturer
		inner.WriteByte(onDemand ? (byte)1 : (byte)0); // onDemand
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
		inner.WriteByte(continuousFrameSet ? (byte)1 : (byte)0);
		if (continuousFrameSet)
		{
			WriteUInt64(inner, 4);                  // frame count
			WriteInt64(inner, 0);                  // Tracy init
			WriteInt32(inner, -1);
			WriteInt64(inner, 1_000);              // missed frames marker
			WriteInt32(inner, -1);
			WriteInt64(inner, 999_999_000);        // first user frame start
			WriteInt32(inner, -1);
			WriteInt64(inner, 16_000_000);         // second user frame start
			WriteInt32(inner, -1);
		}
		else if (separatedFrameSet)
		{
			WriteUInt64(inner, 2);                  // frame count
			WriteInt64(inner, 0);                  // frame 0 start offset
			WriteInt64(inner, 1_000_000);          // frame 0 end offset
			WriteInt32(inner, -1);                 // frame 0 image
			WriteInt64(inner, 1_000_000);          // frame 1 start offset
			WriteInt64(inner, 3_000_000);          // frame 1 end offset
			WriteInt32(inner, -1);                 // frame 1 image
		}
		else
		{
			WriteUInt64(inner, 2);                  // frame count
			WriteInt64(inner, 0);                  // frame 0 start offset
			WriteInt64(inner, 8_333_333);          // frame 0 end offset
			WriteInt32(inner, -1);                 // frame 0 image
			WriteInt64(inner, 1);                  // frame 1 start offset
			WriteInt64(inner, 9_000_000);          // frame 1 end offset
			WriteInt32(inner, -1);                 // frame 1 image
		}
		ulong stringCount = (includeZoneString ? 1UL : 0UL) + (includePlotString ? 1UL : 0UL) + (includeThreadName ? 1UL : 0UL);
		WriteUInt64(inner, stringCount);            // stringData count
		if (includeZoneString)
		{
			WriteUInt64(inner, 0x1000);
			WriteSizedString(inner, "SelfTestZone");
		}
		if (includePlotString)
		{
			WriteUInt64(inner, 0x2000);
			WriteSizedString(inner, "FrameTime");
		}
		if (includeThreadName)
		{
			WriteUInt64(inner, 0x3000);
			WriteSizedString(inner, "RenderThread");
		}
		WriteUInt64(inner, 0);                      // strings count
		WriteUInt64(inner, includeThreadName ? 1UL : 0UL); // threadNames count
		if (includeThreadName)
		{
			WriteUInt64(inner, 123);                 // thread id
			WriteUInt64(inner, 0x3000);              // name pointer
		}
		WriteUInt64(inner, 0);                      // externalNames count
	}

	private static void WriteSourceLocationBase(Stream stream, uint nameIndex, uint line)
	{
		WriteInactiveStringRef(stream);
		WriteStringRefIndex(stream, nameIndex);
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
		byte[] outerHeader = new byte[] { (byte)'t', (byte)'l', (byte)'Z', 4 };
		stream.Write(outerHeader, 0, outerHeader.Length);
		byte[] blockSize = BitConverter.GetBytes((uint)compressed.Length);
		stream.Write(blockSize, 0, blockSize.Length);
		stream.Write(compressed, 0, compressed.Length);
	}

	private static void WriteCompressedBlock(Stream stream, byte[] compressed)
	{
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

	private static void WriteSingle(Stream stream, float value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private sealed class FakeTracyServer : IDisposable
	{
		private readonly TcpListener m_Listener = new TcpListener(IPAddress.Loopback, 0);
		private readonly int m_HandshakeStatus;
		private readonly bool m_StallBeforeWelcome;
		private Thread m_Thread;
		private volatile bool m_HandshakeReceived;
		private Exception m_Exception;

		public FakeTracyServer()
			: this(1)
		{
		}

		public FakeTracyServer(int handshakeStatus)
			: this(handshakeStatus, false)
		{
		}

		public FakeTracyServer(int handshakeStatus, bool stallBeforeWelcome)
		{
			m_HandshakeStatus = handshakeStatus;
			m_StallBeforeWelcome = stallBeforeWelcome;
		}

		public int Port { get; private set; }

		public void Start()
		{
			m_Listener.Start();
			Port = ((IPEndPoint)m_Listener.LocalEndpoint).Port;
			m_Thread = new Thread(Run) { IsBackground = true };
			m_Thread.Start();
		}

		public void AssertHandshakeReceived()
		{
			if (m_Exception != null)
			{
				throw new InvalidOperationException("fake tracy server failed: " + m_Exception.Message, m_Exception);
			}
			if (!m_HandshakeReceived)
			{
				throw new InvalidOperationException("fake tracy server did not receive a handshake.");
			}
		}

		public void Dispose()
		{
			m_Listener.Stop();
			if (m_Thread != null)
			{
				m_Thread.Join(2000);
			}
		}

		private void Run()
		{
			try
			{
				using TcpClient client = m_Listener.AcceptTcpClient();
				using NetworkStream stream = client.GetStream();
				byte[] handshake = ReadExactly(stream, 12);
				string shibboleth = Encoding.ASCII.GetString(handshake, 0, 8);
				uint protocol = (uint)(handshake[8] | (handshake[9] << 8) | (handshake[10] << 16) | (handshake[11] << 24));
				if (shibboleth != "TracyPrf" || protocol != 64)
				{
					throw new InvalidOperationException("unexpected Tracy handshake.");
				}
				m_HandshakeReceived = true;
				stream.WriteByte((byte)m_HandshakeStatus);
				if (m_HandshakeStatus != 1)
				{
					return;
				}
				if (m_StallBeforeWelcome)
				{
					Thread.Sleep(5000);
					return;
				}
				WriteWelcomeMessage(stream);
				WriteLiveFrameBlock(stream);
				client.ReceiveTimeout = 2000;
				try
				{
					ReadExactly(stream, 13);
				}
				catch (IOException)
				{
				}
			}
			catch (SocketException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception ex)
			{
				m_Exception = ex;
			}
		}

		private static byte[] ReadExactly(Stream stream, int size)
		{
			byte[] bytes = new byte[size];
			int offset = 0;
			while (offset < size)
			{
				int read = stream.Read(bytes, offset, size - offset);
				if (read == 0)
				{
					throw new EndOfStreamException("fake tracy server reached end of stream.");
				}
				offset += read;
			}
			return bytes;
		}

		private static void WriteWelcomeMessage(Stream stream)
		{
			WriteDouble(stream, 1.0);
			WriteInt64(stream, 0);
			WriteInt64(stream, 5_000_000);
			WriteUInt64(stream, 0);
			WriteUInt64(stream, 1_000_000_000);
			WriteUInt64(stream, 1_700_000_000);
			WriteUInt64(stream, 1_699_999_000);
			WriteUInt64(stream, 31415);
			WriteInt64(stream, 0);
			stream.WriteByte(0);
			stream.WriteByte(2);
			WriteFixedAscii(stream, "FakeCPU", 12);
			WriteUInt32(stream, 0x01020304);
			WriteFixedAscii(stream, "FakeTracyProgram", 64);
			WriteFixedAscii(stream, "FakeTracyHost", 1024);
		}

		private static void WriteLiveFrameBlock(Stream stream)
		{
			byte[] decoded = new byte[34];
			WriteFrameMark(decoded, 0, 6_000_000);
			WriteFrameMark(decoded, 17, 22_000_000);
			byte[] compressed = new byte[36];
			compressed[0] = 0xF0;
			compressed[1] = 19;
			Buffer.BlockCopy(decoded, 0, compressed, 2, decoded.Length);
			WriteUInt32(stream, (uint)compressed.Length);
			stream.Write(compressed, 0, compressed.Length);
		}

		private static void WriteFrameMark(byte[] buffer, int offset, long time)
		{
			buffer[offset] = 66;
			byte[] timeBytes = BitConverter.GetBytes(time);
			Buffer.BlockCopy(timeBytes, 0, buffer, offset + 1, timeBytes.Length);
		}
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
		AssertStudySaveSessionFile(service, sessionId, session);
		service.CloseSession(sessionId);
	}

	private static void AssertStudySaveSessionFile(ProfilerAnalysisService service, string sessionId, Session sourceSession)
	{
		string path = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-save.profiler");
		string wrongExtensionPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-save-corrected.wrong");
		string correctedWrongExtensionPath = Path.ChangeExtension(Path.GetFullPath(wrongExtensionPath), ".profiler");
		try
		{
			Dictionary<string, object> saved = service.SaveSessionFile(sessionId, path, overwrite: false);
			AssertEqual(sessionId, saved["sessionId"], "study save session id");
			AssertEqual("study", saved["sourceFormat"], "study save source format");
			AssertEqual(Path.GetFullPath(path), saved["path"], "study save path");
			AssertEqual(true, saved["saved"], "study save saved");
			AssertEqual(false, saved["extensionCorrected"], "study save extension corrected");
			if (!File.Exists(path))
			{
				throw new InvalidOperationException("study save file was not created.");
			}

			Session loaded = new Session(new CoreSettings(), new CapturingLog());
			try
			{
				string error = string.Empty;
				if (!loaded.Read(path, ref error))
				{
					throw new InvalidOperationException("saved study session could not be read: " + error);
				}
				AssertEqual(sourceSession.FrameCount, loaded.FrameCount, "saved study frame count");
				AssertEqual(sourceSession.ThreadCount, loaded.ThreadCount, "saved study thread count");
			}
			finally
			{
				loaded.Close();
			}

			Dictionary<string, object> corrected = service.SaveSessionFile(sessionId, wrongExtensionPath, overwrite: false);
			AssertEqual(correctedWrongExtensionPath, corrected["path"], "study corrected save path");
			AssertEqual(true, corrected["extensionCorrected"], "study wrong suffix corrected");
			if (!File.Exists(correctedWrongExtensionPath))
			{
				throw new InvalidOperationException("study corrected save file was not created.");
			}
			AssertOpenFileToolWithStudy(path, sourceSession.FrameCount, sourceSession.ThreadCount);
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			if (File.Exists(correctedWrongExtensionPath))
			{
				File.Delete(correctedWrongExtensionPath);
			}
		}
	}

	private static void AssertOpenFileToolWithStudy(string path, int expectedFrameCount, int expectedThreadCount)
	{
		ProfilerMcpTools tools = new ProfilerMcpTools();
		AssertOpenFileToolSchema(tools);
		Dictionary<string, object> result = tools.CallTool("open_file", new Dictionary<string, object>
		{
			["path"] = path,
			["format"] = "auto",
			["top"] = 5
		});
		AssertEqual(false, result["isError"], "study open_file isError");
		IDictionary structured = result["structuredContent"] as IDictionary;
		AssertEqual("study", structured["sourceFormat"], "study open_file source format");
		AssertEqual("open_file", structured["openedBy"], "study open_file marker");
		AssertEqual(true, structured["isLatestSession"], "study open_file latest session");
		string sessionId = Convert.ToString(structured["sessionId"]);
		if (string.IsNullOrWhiteSpace(sessionId))
		{
			throw new InvalidOperationException("study open_file must return sessionId.");
		}
		AssertEqual(expectedFrameCount, structured["frameCount"], "study open_file frame count");
		AssertEqual(expectedThreadCount, structured["threadCount"], "study open_file thread count");
		tools.CallTool("close_session", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
	}

	private static void AssertOpenFileToolWithTracy(ProfilerMcpTools tools, string path)
	{
		AssertOpenFileToolSchema(tools);
		Dictionary<string, object> result = tools.CallTool("open_file", new Dictionary<string, object>
		{
			["path"] = path,
			["format"] = "auto",
			["top"] = 5
		});
		AssertEqual(false, result["isError"], "tracy open_file isError");
		IDictionary structured = result["structuredContent"] as IDictionary;
		AssertEqual("tracy", structured["sourceFormat"], "tracy open_file source format");
		AssertEqual("open_file", structured["openedBy"], "tracy open_file marker");
		AssertEqual(true, structured["isLatestSession"], "tracy open_file latest session");
		string sessionId = Convert.ToString(structured["sessionId"]);
		if (string.IsNullOrWhiteSpace(sessionId))
		{
			throw new InvalidOperationException("tracy open_file must return sessionId.");
		}
		tools.CallTool("close_session", new Dictionary<string, object>
		{
			["session_id"] = sessionId
		});
	}

	private static void AssertOpenFileToolSchema(ProfilerMcpTools tools)
	{
		foreach (object toolObject in tools.ListTools())
		{
			IDictionary tool = toolObject as IDictionary;
			if (tool != null && Convert.ToString(tool["name"]) == "open_file")
			{
				IDictionary inputSchema = tool["inputSchema"] as IDictionary;
				IList required = inputSchema["required"] as IList;
				if (required == null || !required.Contains("path"))
				{
					throw new InvalidOperationException("open_file must require path.");
				}
				IDictionary properties = inputSchema["properties"] as IDictionary;
				IDictionary format = properties["format"] as IDictionary;
				AssertEqual("auto", format["default"], "open_file format default");
				return;
			}
		}
		throw new InvalidOperationException("open_file tool is missing.");
	}

	private static void AssertTracySaveSessionFileTool(ProfilerMcpTools tools, string sourcePath, string sessionId)
	{
		bool found = false;
		foreach (object toolObject in tools.ListTools())
		{
			IDictionary tool = toolObject as IDictionary;
			if (tool != null && Convert.ToString(tool["name"]) == "save_session_file")
			{
				IDictionary inputSchema = tool["inputSchema"] as IDictionary;
				IList required = inputSchema["required"] as IList;
				if (required == null || !required.Contains("session_id") || !required.Contains("path"))
				{
					throw new InvalidOperationException("save_session_file must require session_id and path.");
				}
				found = true;
				break;
			}
		}
		if (!found)
		{
			throw new InvalidOperationException("save_session_file tool is missing.");
		}

		string requestedPath = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-save.profiler");
		string path = Path.ChangeExtension(Path.GetFullPath(requestedPath), ".tracy");
		try
		{
			Dictionary<string, object> result = tools.CallTool("save_session_file", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["path"] = requestedPath
			});
			AssertEqual(false, result["isError"], "tracy save_session_file isError");
			IDictionary structured = result["structuredContent"] as IDictionary;
			AssertEqual(sessionId, structured["sessionId"], "tracy save session id");
			AssertEqual("tracy", structured["sourceFormat"], "tracy save source format");
			AssertEqual("source-artifact-copy", structured["saveMode"], "tracy save mode");
			AssertEqual(path, structured["path"], "tracy save path");
			AssertEqual(Path.GetFullPath(requestedPath), structured["requestedPath"], "tracy save requested path");
			AssertEqual(true, structured["extensionCorrected"], "tracy wrong suffix corrected");
			if (!File.Exists(path))
			{
				throw new InvalidOperationException("tracy save file was not created.");
			}
			if (!File.ReadAllBytes(sourcePath).SequenceEqual(File.ReadAllBytes(path)))
			{
				throw new InvalidOperationException("tracy save must preserve the original file bytes.");
			}
			Tracy010FileReader.ReadHeader(path);
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	private static void AssertTracyLiveSaveSessionFileRejected(ProfilerMcpTools tools, string sessionId)
	{
		string path = Path.Combine(Directory.GetCurrentDirectory(), ".profiler-study-self-test-live-save.tracy");
		try
		{
			Dictionary<string, object> result = tools.CallTool("save_session_file", new Dictionary<string, object>
			{
				["session_id"] = sessionId,
				["path"] = path
			});
			AssertEqual(true, result["isError"], "tracy live save_session_file isError");
			IDictionary structured = result["structuredContent"] as IDictionary;
			string error = Convert.ToString(structured["error"]);
			if (string.IsNullOrWhiteSpace(error) || !error.Contains("no original .tracy source file"))
			{
				throw new InvalidOperationException("tracy live save_session_file must explain missing original .tracy source.");
			}
			AssertEqual(false, File.Exists(path), "tracy live save output exists");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
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

	private static void AssertDiagnosticCode(object actual, string expectedCode, string name)
	{
		IEnumerable diagnostics = actual as IEnumerable;
		if (diagnostics == null)
		{
			throw new InvalidOperationException(name + " expected diagnostics.");
		}

		foreach (object item in diagnostics)
		{
			IDictionary diagnostic = item as IDictionary;
			if (diagnostic != null && string.Equals(Convert.ToString(diagnostic["code"]), expectedCode, StringComparison.Ordinal))
			{
				return;
			}
		}

		throw new InvalidOperationException(name + " expected diagnostic code " + expectedCode + ".");
	}

	private static void AssertMarkdownContains(IDictionary toolResult, string expectedText, string name)
	{
		IList content = toolResult["content"] as IList;
		AssertHasItems(content, name + " content");
		IDictionary first = content[0] as IDictionary;
		string text = first == null ? string.Empty : Convert.ToString(first["text"]);
		if (text == null || text.IndexOf(expectedText, StringComparison.Ordinal) < 0)
		{
			throw new InvalidOperationException(name + " expected markdown to contain " + expectedText + ".");
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
