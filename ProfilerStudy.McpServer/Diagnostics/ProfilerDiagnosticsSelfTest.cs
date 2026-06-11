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
		string path = Path.Combine(Path.GetTempPath(), "profiler-study-self-test.tracy");
		string unsupportedPath = Path.Combine(Path.GetTempPath(), "profiler-study-self-test-unsupported.tracy");
		try
		{
			WriteMinimalTracyDump(path, 0, 10, 0);
			TracyFileHeader header = Tracy010FileReader.ReadHeader(path);
			AssertEqual("0.10.0", header.Version, "tracy file header version");
			AssertEqual("lz4", header.Compression, "tracy file compression");

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
		}
	}

	private static void WriteMinimalTracyDump(string path, byte major, byte minor, byte patch)
	{
		byte[] innerHeader = new byte[] { (byte)'t', (byte)'r', (byte)'a', (byte)'c', (byte)'y', major, minor, patch };
		byte[] compressed = new byte[1 + innerHeader.Length];
		compressed[0] = (byte)(innerHeader.Length << 4);
		Buffer.BlockCopy(innerHeader, 0, compressed, 1, innerHeader.Length);

		using FileStream stream = File.Create(path);
		byte[] outerHeader = Encoding.ASCII.GetBytes("tlZ4");
		stream.Write(outerHeader, 0, outerHeader.Length);
		byte[] blockSize = BitConverter.GetBytes((uint)compressed.Length);
		stream.Write(blockSize, 0, blockSize.Length);
		stream.Write(compressed, 0, compressed.Length);
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
