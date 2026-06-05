using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FramePro;
using SCLCoreCLR;

namespace ProfilerStudy.McpServer;

internal sealed class ProfilerAnalysisService
{
	private sealed class LoadedSession
	{
		public string Id;
		public string Source;
		public DateTime CreatedUtc;
		public DateTime LastAccessUtc;
		public Session Session;
		public CapturingLog Log;
	}

	private readonly Dictionary<string, LoadedSession> m_Sessions = new Dictionary<string, LoadedSession>();
	private readonly object m_SessionsLock = new object();
	private int m_NextSessionId;

	public Dictionary<string, object> CaptureAndroidProfile(string target, int durationSeconds, int top, bool keepSession)
	{
		string endpoint = ResolveAndroidEndpoint(target);
		return CaptureProfile(ProfilerCaptureTarget.FromAndroidEndpoint("android:" + target, endpoint), durationSeconds, top, keepSession);
	}

	public Dictionary<string, object> CaptureProfile(string url, int durationSeconds, int top, bool keepSession)
	{
		return CaptureProfile(ProfilerCaptureTarget.Parse(url), durationSeconds, top, keepSession);
	}

	private Dictionary<string, object> CaptureProfile(ProfilerCaptureTarget target, int durationSeconds, int top, bool keepSession)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		CapturingLog log = new CapturingLog();
		Session session = new Session(new CoreSettings(), log);
		bool keepAlive = false;
		try
		{
			Stopwatch connectStopwatch = Stopwatch.StartNew();
			bool connected = target.Kind == ProfilerCaptureTargetKind.AndroidForward
				? session.ConnectToAndroid(target.Endpoint)
				: session.ConnectToTcp(target.ConnectHost, target.ConnectPort, target.Url, interactive: true, recordContextSwitches: false);
			if (!connected)
			{
				throw new InvalidOperationException(session.LastConnectionError ?? "Profiler capture connection failed.");
			}
			connectStopwatch.Stop();

			session.StartReceiving();
			DateTime end = DateTime.UtcNow.AddSeconds(durationSeconds);
			int pollCount = 0;
			while (DateTime.UtcNow < end && session.Connected)
			{
				pollCount++;
				Thread.Sleep(250);
			}

			Stopwatch processingStopwatch = Stopwatch.StartNew();
			session.Disconnect(DisconnectReason.Requested);
			session.WaitforProcessingToFinish(10000);
			processingStopwatch.Stop();
			Dictionary<string, object> analysis = AnalyzeSession(session, top);
			analysis["capture"] = new Dictionary<string, object>
			{
				["url"] = target.Url,
				["kind"] = target.Kind.ToString(),
				["endpoint"] = target.Endpoint,
				["connectHost"] = target.ConnectHost,
				["connectPort"] = target.ConnectPort,
				["durationSeconds"] = durationSeconds,
				["disconnectReason"] = session.DisconnectReason.ToString(),
				["keepSession"] = keepSession
			};
			analysis["captureTelemetry"] = new Dictionary<string, object>
			{
				["connectMs"] = Round(connectStopwatch.Elapsed.TotalMilliseconds),
				["processingWaitMs"] = Round(processingStopwatch.Elapsed.TotalMilliseconds),
				["totalToolMs"] = Round(stopwatch.Elapsed.TotalMilliseconds),
				["pollCount"] = pollCount,
				["connectedAtEndOfDuration"] = session.Connected
			};
			if (keepSession)
			{
				string sessionId = AddSession(session, target.Url, log);
				analysis["sessionId"] = sessionId;
				keepAlive = true;
			}
			analysis["logTail"] = log.GetTail(40);
			return analysis;
		}
		finally
		{
			if (!keepAlive)
			{
				session.Close();
			}
		}
	}

	public Dictionary<string, object> AnalyzeSessionFile(string path, int top)
	{
		CapturingLog log = new CapturingLog();
		Session session = LoadSessionFromFile(path, log);
		try
		{
			Dictionary<string, object> analysis = AnalyzeSession(session, top);
			analysis["sourceFile"] = Path.GetFullPath(path);
			analysis["logTail"] = log.GetTail(40);
			return analysis;
		}
		finally
		{
			session.Close();
		}
	}

	public Dictionary<string, object> LoadSessionFile(string path, int top)
	{
		CapturingLog log = new CapturingLog();
		Session session = LoadSessionFromFile(path, log);
		string sessionId = AddSession(session, Path.GetFullPath(path), log);
		Dictionary<string, object> analysis = AnalyzeSession(session, top);
		analysis["sessionId"] = sessionId;
		analysis["sourceFile"] = Path.GetFullPath(path);
		analysis["logTail"] = log.GetTail(40);
		return analysis;
	}

	public Dictionary<string, object> CloseSession(string sessionId)
	{
		LoadedSession loaded = null;
		lock (m_SessionsLock)
		{
			if (m_Sessions.TryGetValue(sessionId, out loaded))
			{
				m_Sessions.Remove(sessionId);
			}
		}
		if (loaded == null)
		{
			throw new ArgumentException("Unknown session_id: " + sessionId);
		}
		loaded.Session.Close();
		return new Dictionary<string, object>
		{
			["sessionId"] = sessionId,
			["closed"] = true
		};
	}

	public Dictionary<string, object> ListSessions()
	{
		ArrayList sessions = new ArrayList();
		lock (m_SessionsLock)
		{
			foreach (LoadedSession loaded in m_Sessions.Values.OrderBy(s => s.Id))
			{
				sessions.Add(new Dictionary<string, object>
				{
					["sessionId"] = loaded.Id,
					["source"] = loaded.Source,
					["createdUtc"] = loaded.CreatedUtc.ToString("o"),
					["lastAccessUtc"] = loaded.LastAccessUtc.ToString("o"),
					["frameCount"] = loaded.Session.FrameCount,
					["threadCount"] = loaded.Session.ThreadCount
				});
			}
		}
		return new Dictionary<string, object> { ["sessions"] = sessions };
	}

	public Dictionary<string, object> GetSessionSummary(string sessionId)
	{
		LoadedSession loaded = GetLoadedSession(sessionId);
		Dictionary<string, object> result = AnalyzeSession(loaded.Session, 10);
		result["sessionId"] = loaded.Id;
		result["source"] = loaded.Source;
		result["logTail"] = loaded.Log.GetTail(40);
		return result;
	}

	public Dictionary<string, object> FindSlowFrames(string sessionId, int top, double thresholdMs)
	{
		LoadedSession loaded = GetLoadedSession(sessionId);
		double tickToMs = TickToMs(loaded.Session);
		double resolvedThreshold = ResolveThresholdMs(loaded.Session, thresholdMs, tickToMs);
		List<ProfilerDiagnostics.FrameSample> samples = GetFrameSamples(loaded.Session, tickToMs).ToList();
		ArrayList slowFrames = GetSlowFrames(loaded.Session, top, tickToMs, resolvedThreshold);
		return new Dictionary<string, object>
		{
			["sessionId"] = loaded.Id,
			["thresholdMs"] = Round(resolvedThreshold),
			["slowFrames"] = slowFrames,
			["slowFramePattern"] = ProfilerDiagnostics.AnalyzeSlowFramePattern(samples, resolvedThreshold)
		};
	}

	public Dictionary<string, object> FindScopeHotspots(string sessionId, int top, int startFrame, int endFrame)
	{
		LoadedSession loaded = GetLoadedSession(sessionId);
		Session session = loaded.Session;
		NormalizeFrameRange(session, ref startFrame, ref endFrame);
		double tickToMs = TickToMs(session);
		return new Dictionary<string, object>
		{
			["sessionId"] = loaded.Id,
			["range"] = BuildRangeDictionary(session, startFrame, endFrame),
			["scopeHotspots"] = GetScopeHotspots(session, top, tickToMs, startFrame, endFrame)
		};
	}

	public Dictionary<string, object> ListCounters(string sessionId, int top, string filter)
	{
		LoadedSession loaded = GetLoadedSession(sessionId);
		Session session = loaded.Session;
		IEnumerable<Dictionary<string, object>> counters = GetCounterSummaries(session)
			.Where(counter => CounterMatchesFilter(counter, filter))
			.OrderByDescending(counter => Convert.ToInt64(counter["totalCount"]))
			.Take(top);
		return new Dictionary<string, object>
		{
			["sessionId"] = loaded.Id,
			["filter"] = filter ?? string.Empty,
			["counters"] = ToArrayList(counters)
		};
	}

	public Dictionary<string, object> QueryCounterSamples(string sessionId, string counterName, int startFrame, int endFrame, bool accumulated, int maxSamples)
	{
		LoadedSession loaded = GetLoadedSession(sessionId);
		Session session = loaded.Session;
		if (string.IsNullOrWhiteSpace(counterName))
		{
			throw new ArgumentException("counter_name is required.");
		}
		long counterId = session.GetCustomStatNameId(counterName);
		if (counterId < 0)
		{
			throw new ArgumentException("Unknown counter_name: " + counterName);
		}
		NormalizeFrameRange(session, ref startFrame, ref endFrame);

		List<FrameValue> values = new List<FrameValue>();
		session.GetCustomStats(startFrame, endFrame, counterId, accumulated, values);
		ArrayList samples = new ArrayList();
		int availableCount = values.Count;
		int count = Math.Min(maxSamples, availableCount);
		for (int i = 0; i < count; i++)
		{
			FrameValue value = values[i];
			samples.Add(new Dictionary<string, object>
			{
				["frameIndex"] = startFrame + i,
				["frameEndTime"] = value.m_FrameEndTime,
				["value"] = Round(value.m_Value),
				["count"] = Round(value.m_Count)
			});
		}

		Dictionary<string, object> counter = GetCounterSummaries(session)
			.FirstOrDefault(summary => Convert.ToString(summary["name"]) == counterName);
		return new Dictionary<string, object>
		{
			["sessionId"] = loaded.Id,
			["counterName"] = counterName,
			["counter"] = counter ?? new Dictionary<string, object>(),
			["range"] = BuildRangeDictionary(session, startFrame, endFrame),
			["accumulated"] = accumulated,
			["sampleCount"] = samples.Count,
			["availableSampleCount"] = availableCount,
			["truncated"] = availableCount > samples.Count,
			["samples"] = samples
		};
	}

	public Dictionary<string, object> AnalyzeFrame(string sessionId, int frameIndex, int top, int neighborCount)
	{
		LoadedSession loaded = GetLoadedSession(sessionId);
		Session session = loaded.Session;
		if (frameIndex >= session.FrameCount)
		{
			throw new ArgumentOutOfRangeException("frame_index", "frame_index is outside the session frame range.");
		}

		double tickToMs = TickToMs(session);
		Frame frame = session.GetFrame(frameIndex);
		if (frame == null)
		{
			throw new InvalidOperationException("Frame data is missing for frame_index " + frameIndex + ".");
		}
		int startNeighbor = Math.Max(0, frameIndex - neighborCount);
		int endNeighbor = Math.Min(session.FrameCount - 1, frameIndex + neighborCount);
		Dictionary<string, object> frameRange = BuildRangeDictionary(session, frameIndex, frameIndex);
		Dictionary<string, object> neighborRange = BuildRangeDictionary(session, startNeighbor, endNeighbor);
		ArrayList hotspots = GetScopeHotspots(session, top, tickToMs, frameIndex, frameIndex);
		List<Dictionary<string, object>> hotspotList = hotspots.Cast<Dictionary<string, object>>().ToList();
		return new Dictionary<string, object>
		{
			["sessionId"] = loaded.Id,
			["frame"] = FrameToDictionary(frame, tickToMs),
			["range"] = frameRange,
			["scopeHotspots"] = hotspots,
			["neighborRange"] = neighborRange,
			["neighborSlowFrames"] = GetSlowFrames(session, Math.Min(top, endNeighbor + 1 - startNeighbor), tickToMs, 0.0, startNeighbor, endNeighbor),
			["diagnostics"] = ProfilerDiagnostics.BuildDiagnostics(Round(frame.Duration * tickToMs), hotspotList)
		};
	}

	public Dictionary<string, object> AnalyzeFrameDetail(string sessionId, int frameIndex, int maxNodes, int maxDepth, double minDurationMs)
	{
		LoadedSession loaded = GetLoadedSession(sessionId);
		Session session = loaded.Session;
		if (frameIndex >= session.FrameCount)
		{
			throw new ArgumentOutOfRangeException("frame_index", "frame_index is outside the session frame range.");
		}

		double tickToMs = TickToMs(session);
		Frame frame = session.GetFrame(frameIndex);
		if (frame == null)
		{
			throw new InvalidOperationException("Frame data is missing for frame_index " + frameIndex + ".");
		}

		List<Dictionary<string, object>> flatSpans = new List<Dictionary<string, object>>();
		ArrayList threadFlameGraphs = BuildFrameFlameGraphs(
			session,
			frame,
			tickToMs,
			maxNodes,
			maxDepth,
			Math.Max(0.0, minDurationMs),
			flatSpans,
			out int includedNodeCount,
			out int omittedNodeCount,
			out int omittedByDepthCount,
			out int omittedByDurationCount,
			out bool truncated);

		return new Dictionary<string, object>
		{
			["sessionId"] = loaded.Id,
			["frame"] = FrameToDictionary(frame, tickToMs),
			["range"] = BuildRangeDictionary(session, frameIndex, frameIndex),
			["options"] = new Dictionary<string, object>
			{
				["maxNodes"] = maxNodes,
				["maxDepth"] = maxDepth,
				["minDurationMs"] = Round(Math.Max(0.0, minDurationMs))
			},
			["threadFlameGraphs"] = threadFlameGraphs,
			["topSpans"] = ToArrayList(flatSpans
				.OrderByDescending(span => Convert.ToDouble(span["durationMs"]))
				.Take(Math.Min(maxNodes, 50))),
			["frameCounters"] = GetFrameCounters(session, frameIndex, 200),
			["nodeStats"] = new Dictionary<string, object>
			{
				["includedNodeCount"] = includedNodeCount,
				["omittedNodeCount"] = omittedNodeCount,
				["omittedByDepthCount"] = omittedByDepthCount,
				["omittedByDurationCount"] = omittedByDurationCount,
				["truncated"] = truncated
			}
		};
	}

	private static ArrayList GetFrameCounters(Session session, int frameIndex, int maxCounters)
	{
		List<Dictionary<string, object>> counters = new List<Dictionary<string, object>>();
		foreach (CustomStatSessionData stat in session.GetCustomStats())
		{
			List<FrameValue> values = new List<FrameValue>();
			session.GetCustomStats(frameIndex, frameIndex, stat.Name, false, values);
			if (values.Count == 0)
			{
				continue;
			}
			FrameValue value = values[0];

			List<FrameValue> accumulatedValues = new List<FrameValue>();
			session.GetCustomStats(frameIndex, frameIndex, stat.Name, true, accumulatedValues);
			double accumulatedValue = accumulatedValues.Count == 0 ? 0.0 : accumulatedValues[0].m_Value;
			double accumulatedCount = accumulatedValues.Count == 0 ? 0.0 : accumulatedValues[0].m_Count;
			if (value.m_Value == 0.0 && value.m_Count == 0.0 && accumulatedValue == 0.0 && accumulatedCount == 0.0)
			{
				continue;
			}

			counters.Add(new Dictionary<string, object>
			{
				["name"] = session.GetString(stat.Name),
				["valueType"] = stat.ValueType.ToString(),
				["graph"] = session.GetCustomStatGraph(stat.Name),
				["unit"] = session.GetCustomStatUnit(stat.Name),
				["frameIndex"] = frameIndex,
				["frameEndTime"] = value.m_FrameEndTime,
				["value"] = Round(value.m_Value),
				["count"] = Round(value.m_Count),
				["accumulatedValue"] = Round(accumulatedValue),
				["accumulatedCount"] = Round(accumulatedCount)
			});
		}
		return ToArrayList(counters
			.OrderByDescending(counter => Convert.ToDouble(counter["count"]))
			.ThenByDescending(counter => Math.Abs(Convert.ToDouble(counter["value"])))
			.ThenBy(counter => Convert.ToString(counter["name"]))
			.Take(maxCounters));
	}

	public Dictionary<string, object> AnalyzeTimeRange(string sessionId, int startFrame, int endFrame, int top, double thresholdMs)
	{
		LoadedSession loaded = GetLoadedSession(sessionId);
		Session session = loaded.Session;
		NormalizeFrameRange(session, ref startFrame, ref endFrame);
		Dictionary<string, object> range = BuildRangeDictionary(session, startFrame, endFrame);
		double tickToMs = TickToMs(session);
		double resolvedThreshold = ResolveThresholdMs(session, thresholdMs, tickToMs);
		List<ProfilerDiagnostics.FrameSample> samples = GetFrameSamples(session, tickToMs, startFrame, endFrame).ToList();
		ArrayList hotspots = GetScopeHotspots(session, top, tickToMs, startFrame, endFrame);
		double maxFrameMs = samples.Count == 0 ? 0.0 : samples.Max(sample => sample.DurationMs);
		return new Dictionary<string, object>
		{
			["sessionId"] = loaded.Id,
			["range"] = range,
			["summary"] = GetRangeSummary(session, tickToMs, startFrame, endFrame),
			["slowFrames"] = GetSlowFrames(session, top, tickToMs, resolvedThreshold, startFrame, endFrame),
			["scopeHotspots"] = hotspots,
			["slowFramePattern"] = ProfilerDiagnostics.AnalyzeSlowFramePattern(samples, resolvedThreshold),
			["diagnostics"] = ProfilerDiagnostics.BuildDiagnostics(Round(maxFrameMs), hotspots.Cast<Dictionary<string, object>>())
		};
	}

	private static string ResolveAndroidEndpoint(string target)
	{
		switch ((target ?? string.Empty).Trim().ToLowerInvariant())
		{
			case "debug":
				return AdbSocketDiscovery.DebugFrameProEndpoint;
			case "release":
				return AdbSocketDiscovery.ReleaseFrameProEndpoint;
			default:
				throw new ArgumentException("target must be 'debug' or 'release'.");
		}
	}

	private static Dictionary<string, object> AnalyzeSession(Session session, int top)
	{
		double tickToMs = TickToMs(session);
		double thresholdMs = ResolveThresholdMs(session, 0.0, tickToMs);
		ArrayList slowFrames = GetSlowFrames(session, top, tickToMs, 0.0);
		ArrayList hotspots = GetScopeHotspots(session, top, tickToMs);
		ArrayList customStats = GetCustomStats(session, top);
		ArrayList threads = GetThreads(session, top);

		return new Dictionary<string, object>
		{
			["summary"] = GetSummary(session, tickToMs),
			["slowFrames"] = slowFrames,
			["scopeHotspots"] = hotspots,
			["customStats"] = customStats,
			["threads"] = threads,
			["slowFramePattern"] = ProfilerDiagnostics.AnalyzeSlowFramePattern(GetFrameSamples(session, tickToMs), thresholdMs),
			["diagnostics"] = ProfilerDiagnostics.BuildDiagnostics(Round(session.MaxFrameTime * tickToMs), hotspots.Cast<Dictionary<string, object>>())
		};
	}

	private static Dictionary<string, object> GetSummary(Session session, double tickToMs)
	{
		return new Dictionary<string, object>
		{
			["frameCount"] = session.FrameCount,
			["threadCount"] = session.ThreadCount,
			["timeSpanCount"] = session.TimeSpanCount,
			["timerFrequency"] = session.TimerFrequency,
			["averageFrameMs"] = Round(session.AverageFrameTime * tickToMs),
			["maxFrameMs"] = Round(session.MaxFrameTime * tickToMs),
			["maxFrameIndex"] = session.MaxFrameIndex,
			["framesInBudget"] = session.FramesInBudget,
			["firstFrameTime"] = session.FirstFrameTime,
			["lastFrameEndTime"] = session.LastFrameEndTime,
			["disconnectReason"] = session.DisconnectReason.ToString(),
			["receivedConnectPacket"] = session.ReceivedConnectPacket,
			["receivedFrameProLibVersion"] = session.ReceivedFrameProLibVersion
		};
	}

	private static Dictionary<string, object> GetRangeSummary(Session session, double tickToMs, int startFrame, int endFrame)
	{
		List<Frame> frames = GetFrames(session, startFrame, endFrame).ToList();
		double averageFrameMs = frames.Count == 0 ? 0.0 : frames.Average(frame => frame.Duration * tickToMs);
		double maxFrameMs = frames.Count == 0 ? 0.0 : frames.Max(frame => frame.Duration * tickToMs);
		int maxFrameIndex = frames.Count == 0 ? -1 : frames.OrderByDescending(frame => frame.Duration).First().Index;
		return new Dictionary<string, object>
		{
			["frameCount"] = frames.Count,
			["threadCount"] = session.ThreadCount,
			["timeSpanCount"] = session.TimeSpanCount,
			["timerFrequency"] = session.TimerFrequency,
			["averageFrameMs"] = Round(averageFrameMs),
			["maxFrameMs"] = Round(maxFrameMs),
			["maxFrameIndex"] = maxFrameIndex,
			["firstFrameTime"] = frames.Count == 0 ? 0L : frames[0].StartTime,
			["lastFrameEndTime"] = frames.Count == 0 ? 0L : frames[frames.Count - 1].EndTime
		};
	}

	private static ArrayList GetSlowFrames(Session session, int top, double tickToMs, double thresholdMs)
	{
		return GetSlowFrames(session, top, tickToMs, thresholdMs, 0, session.FrameCount - 1);
	}

	private static ArrayList GetSlowFrames(Session session, int top, double tickToMs, double thresholdMs, int startFrame, int endFrame)
	{
		List<Dictionary<string, object>> frames = new List<Dictionary<string, object>>();
		for (int i = startFrame; i <= endFrame; i++)
		{
			Frame frame = session.GetFrame(i);
			if (frame == null)
			{
				continue;
			}
			Dictionary<string, object> value = FrameToDictionary(frame, tickToMs);
			if (thresholdMs <= 0.0 || Convert.ToDouble(value["durationMs"]) >= thresholdMs)
			{
				frames.Add(value);
			}
		}
		return ToArrayList(frames.OrderByDescending(f => Convert.ToDouble(f["durationMs"])).Take(top));
	}

	private static ArrayList GetScopeHotspots(Session session, int top, double tickToMs)
	{
		return GetScopeHotspots(session, top, tickToMs, 0, session.FrameCount - 1);
	}

	private static ArrayList GetScopeHotspots(Session session, int top, double tickToMs, int startFrame, int endFrame)
	{
		List<Dictionary<string, object>> scopes = new List<Dictionary<string, object>>();
		foreach (long scopeId in session.GetTimerNames(string.Empty))
		{
			List<FrameTimeSpanStruct> frameStats = new List<FrameTimeSpanStruct>();
			session.GetTimeSpanFrameTimes(startFrame, endFrame, scopeId, frameStats);
			long totalTime = 0L;
			long totalCount = 0L;
			long maxFrameTime = 0L;
			long maxFrameCount = 0L;
			foreach (FrameTimeSpanStruct frameStat in frameStats)
			{
				totalTime += frameStat.m_Duration;
				totalCount += frameStat.m_Count;
				if (frameStat.m_Duration > maxFrameTime)
				{
					maxFrameTime = frameStat.m_Duration;
				}
				if (frameStat.m_Count > maxFrameCount)
				{
					maxFrameCount = frameStat.m_Count;
				}
			}
			if (totalTime == 0L && totalCount == 0L)
			{
				continue;
			}
			scopes.Add(new Dictionary<string, object>
			{
				["name"] = session.GetTimerName(scopeId),
				["totalMs"] = Round(totalTime * tickToMs),
				["totalCount"] = totalCount,
				["maxMsPerFrame"] = Round(maxFrameTime * tickToMs),
				["maxCountPerFrame"] = maxFrameCount
			});
		}
		return ToArrayList(scopes.OrderByDescending(s => Convert.ToDouble(s["totalMs"])).Take(top));
	}

	private static ArrayList GetCustomStats(Session session, int top)
	{
		List<Dictionary<string, object>> stats = GetCounterSummaries(session).ToList();
		return ToArrayList(stats.OrderByDescending(s => Convert.ToInt64(s["totalCount"])).Take(top));
	}

	private static IEnumerable<Dictionary<string, object>> GetCounterSummaries(Session session)
	{
		foreach (CustomStatSessionData stat in session.GetCustomStats())
		{
			yield return new Dictionary<string, object>
			{
				["name"] = session.GetString(stat.Name),
				["valueType"] = stat.ValueType.ToString(),
				["graph"] = session.GetCustomStatGraph(stat.Name),
				["unit"] = session.GetCustomStatUnit(stat.Name),
				["totalCount"] = stat.m_TotalCount,
				["totalValueInt64"] = stat.m_TotalValueInt64,
				["totalValueDouble"] = Round(stat.m_TotalValueDouble),
				["minValuePerFrame"] = Round(stat.ValueType == CustomStatValueType.Int64 ? stat.m_MinValuePerFrameInt64 : stat.m_MinValuePerFrameDouble),
				["maxValuePerFrame"] = Round(stat.ValueType == CustomStatValueType.Int64 ? stat.m_MaxValuePerFrameInt64 : stat.m_MaxValuePerFrameDouble),
				["minCountPerFrame"] = stat.m_MinCountPerFrame,
				["maxCountPerFrame"] = stat.m_MaxCountPerFrame,
				["accTotalValueInt64"] = stat.m_AccTotalValueInt64,
				["accTotalValueDouble"] = Round(stat.m_AccTotalValueDouble),
				["firstFrameSeen"] = stat.m_FirstFrameSeen
			};
		}
	}

	private static bool CounterMatchesFilter(Dictionary<string, object> counter, string filter)
	{
		if (string.IsNullOrWhiteSpace(filter))
		{
			return true;
		}
		string name = Convert.ToString(counter["name"]);
		return name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static ArrayList GetThreads(Session session, int top)
	{
		List<Dictionary<string, object>> threads = new List<Dictionary<string, object>>();
		foreach (int threadId in session.GetThreadIds().Take(top))
		{
			threads.Add(new Dictionary<string, object>
			{
				["id"] = threadId,
				["name"] = session.GetThreadName(threadId)
			});
		}
		return ToArrayList(threads);
	}

	private static Dictionary<string, object> FrameToDictionary(Frame frame, double tickToMs)
	{
		return new Dictionary<string, object>
		{
			["index"] = frame.Index,
			["durationMs"] = Round(frame.Duration * tickToMs),
			["startTime"] = frame.StartTime,
			["endTime"] = frame.EndTime,
			["timeSpanCount"] = frame.TimeSpanCount,
			["bytesSent"] = frame.BytesSent,
			["waitForSendCompleteMs"] = Round(frame.WaitForSendCompleteTime * tickToMs),
			["prevFrameSendMs"] = Round(frame.PrevFrameSendTime * tickToMs)
		};
	}

	private static ArrayList BuildFrameFlameGraphs(
		Session session,
		Frame frame,
		double tickToMs,
		int maxNodes,
		int maxDepth,
		double minDurationMs,
		List<Dictionary<string, object>> flatSpans,
		out int includedNodeCount,
		out int omittedNodeCount,
		out int omittedByDepthCount,
		out int omittedByDurationCount,
		out bool truncated)
	{
		ArrayList threads = new ArrayList();
		includedNodeCount = 0;
		omittedNodeCount = 0;
		omittedByDepthCount = 0;
		omittedByDurationCount = 0;
		truncated = false;

		List<int> threadIds = new List<int>();
		session.GetThreads(threadIds);
		foreach (int threadId in threadIds.OrderBy(id => id))
		{
			ArrayList roots = new ArrayList();
			foreach (FramePro.TimeSpan root in EnumerateTopLevelSpansForFrame(session, threadId, frame))
			{
				Dictionary<string, object> node = BuildFlameNode(
					session,
					root,
					frame,
					tickToMs,
					threadId,
					0,
					maxDepth,
					minDurationMs,
					maxNodes,
					flatSpans,
					ref includedNodeCount,
					ref omittedNodeCount,
					ref omittedByDepthCount,
					ref omittedByDurationCount,
					ref truncated);
				if (node != null)
				{
					roots.Add(node);
				}
				if (truncated)
				{
					break;
				}
			}
			if (roots.Count == 0)
			{
				continue;
			}
			threads.Add(new Dictionary<string, object>
			{
				["threadId"] = threadId,
				["threadName"] = session.GetThreadName(threadId),
				["roots"] = roots
			});
			if (truncated)
			{
				break;
			}
		}
		return threads;
	}

	private static IEnumerable<FramePro.TimeSpan> EnumerateTopLevelSpansForFrame(Session session, int threadId, Frame frame)
	{
		FramePro.TimeSpan span = session.GetTimeSpan(threadId, frame.StartTime);
		if (span == null && frame.Duration > 0)
		{
			span = session.GetTimeSpan(threadId, frame.StartTime + 1);
		}
		if (span == null)
		{
			yield break;
		}

		while (span.Parent != null && span.Parent.Parent != null)
		{
			span = span.Parent;
		}

		while (span != null && span.StartTime < frame.EndTime)
		{
			if (OverlapsFrame(span, frame))
			{
				yield return span;
			}
			span = span.Next;
		}
	}

	private static Dictionary<string, object> BuildFlameNode(
		Session session,
		FramePro.TimeSpan span,
		Frame frame,
		double tickToMs,
		int threadId,
		int depth,
		int maxDepth,
		double minDurationMs,
		int maxNodes,
		List<Dictionary<string, object>> flatSpans,
		ref int includedNodeCount,
		ref int omittedNodeCount,
		ref int omittedByDepthCount,
		ref int omittedByDurationCount,
		ref bool truncated)
	{
		if (truncated)
		{
			omittedNodeCount += CountOverlappingNodes(span, frame);
			return null;
		}
		if (!OverlapsFrame(span, frame))
		{
			return null;
		}
		if (depth >= maxDepth)
		{
			int omitted = CountOverlappingNodes(span, frame);
			omittedNodeCount += omitted;
			omittedByDepthCount += omitted;
			return null;
		}

		long clippedStart = Math.Max(span.StartTime, frame.StartTime);
		long clippedEnd = Math.Min(span.EndTime, frame.EndTime);
		double durationMs = Round((clippedEnd - clippedStart) * tickToMs);
		if (durationMs < minDurationMs)
		{
			int omitted = CountOverlappingNodes(span, frame);
			omittedNodeCount += omitted;
			omittedByDurationCount += omitted;
			return null;
		}
		if (includedNodeCount >= maxNodes)
		{
			truncated = true;
			omittedNodeCount += CountOverlappingNodes(span, frame);
			return null;
		}

		includedNodeCount++;
		ArrayList children = new ArrayList();
		for (FramePro.TimeSpan child = span.Children; child != null; child = child.Next)
		{
			if (!OverlapsFrame(child, frame))
			{
				continue;
			}
			Dictionary<string, object> childNode = BuildFlameNode(
				session,
				child,
				frame,
				tickToMs,
				threadId,
				depth + 1,
				maxDepth,
				minDurationMs,
				maxNodes,
				flatSpans,
				ref includedNodeCount,
				ref omittedNodeCount,
				ref omittedByDepthCount,
				ref omittedByDurationCount,
				ref truncated);
			if (childNode != null)
			{
				children.Add(childNode);
			}
			if (truncated)
			{
				break;
			}
		}

		TimeSpanInfo info = session.GetTimeSpanInfo(span.TimeSpanInfoId);
		string name = session.GetTimerName(info.Name);
		Dictionary<string, object> node = new Dictionary<string, object>
		{
			["name"] = name,
			["threadId"] = threadId,
			["threadName"] = session.GetThreadName(threadId),
			["depth"] = depth,
			["startTime"] = span.StartTime,
			["endTime"] = span.EndTime,
			["clippedStartTime"] = clippedStart,
			["clippedEndTime"] = clippedEnd,
			["durationMs"] = durationMs,
			["selfMs"] = Round(Math.Max(0.0, durationMs - GetDirectChildDurationMs(span, frame, tickToMs))),
			["relativeStartMs"] = Round((clippedStart - frame.StartTime) * tickToMs),
			["relativeEndMs"] = Round((clippedEnd - frame.StartTime) * tickToMs),
			["children"] = children
		};
		flatSpans.Add(new Dictionary<string, object>
		{
			["name"] = name,
			["threadId"] = threadId,
			["threadName"] = session.GetThreadName(threadId),
			["depth"] = depth,
			["durationMs"] = durationMs,
			["selfMs"] = node["selfMs"],
			["relativeStartMs"] = node["relativeStartMs"],
			["relativeEndMs"] = node["relativeEndMs"]
		});
		return node;
	}

	private static bool OverlapsFrame(FramePro.TimeSpan span, Frame frame)
	{
		return span.StartTime < frame.EndTime && span.EndTime > frame.StartTime;
	}

	private static double GetDirectChildDurationMs(FramePro.TimeSpan span, Frame frame, double tickToMs)
	{
		double childDurationMs = 0.0;
		for (FramePro.TimeSpan child = span.Children; child != null; child = child.Next)
		{
			if (!OverlapsFrame(child, frame))
			{
				continue;
			}
			long clippedStart = Math.Max(child.StartTime, frame.StartTime);
			long clippedEnd = Math.Min(child.EndTime, frame.EndTime);
			childDurationMs += (clippedEnd - clippedStart) * tickToMs;
		}
		return childDurationMs;
	}

	private static int CountOverlappingNodes(FramePro.TimeSpan span, Frame frame)
	{
		if (span == null || !OverlapsFrame(span, frame))
		{
			return 0;
		}
		int count = 1;
		for (FramePro.TimeSpan child = span.Children; child != null; child = child.Next)
		{
			count += CountOverlappingNodes(child, frame);
		}
		return count;
	}

	private static IEnumerable<ProfilerDiagnostics.FrameSample> GetFrameSamples(Session session, double tickToMs)
	{
		return GetFrameSamples(session, tickToMs, 0, session.FrameCount - 1);
	}

	private static IEnumerable<ProfilerDiagnostics.FrameSample> GetFrameSamples(Session session, double tickToMs, int startFrame, int endFrame)
	{
		for (int i = startFrame; i <= endFrame; i++)
		{
			Frame frame = session.GetFrame(i);
			if (frame == null)
			{
				continue;
			}
			yield return new ProfilerDiagnostics.FrameSample(
				frame.Index,
				Round(frame.Duration * tickToMs),
				frame.TimeSpanCount,
				frame.BytesSent);
		}
	}

	private static IEnumerable<Frame> GetFrames(Session session, int startFrame, int endFrame)
	{
		for (int i = startFrame; i <= endFrame; i++)
		{
			Frame frame = session.GetFrame(i);
			if (frame != null)
			{
				yield return frame;
			}
		}
	}

	private static double ResolveThresholdMs(Session session, double thresholdMs, double tickToMs)
	{
		if (thresholdMs > 0.0)
		{
			return thresholdMs;
		}
		double average = Round(session.AverageFrameTime * tickToMs);
		return Math.Max(33.333, average * 1.5);
	}

	private static double TickToMs(Session session)
	{
		return session.TimerFrequency > 0 ? 1000.0 / session.TimerFrequency : 0.0;
	}

	private static Session CreateRangeSession(Session source, int startFrame, int endFrame, out Dictionary<string, object> range)
	{
		NormalizeFrameRange(source, ref startFrame, ref endFrame);
		Frame start = source.GetFrame(startFrame);
		Frame end = source.GetFrame(endFrame);
		if (start == null || end == null)
		{
			throw new InvalidOperationException("Frame data is missing for the selected range.");
		}
		range = BuildRangeDictionary(source, startFrame, endFrame);
		Session session = new Session(new CoreSettings(), new CapturingLog());
		string error = string.Empty;
		ThreadJobContext context = new ThreadJobContext();
		if (!source.CopyTo(session, start.StartTime, end.EndTime, context, ref error))
		{
			session.Close();
			throw new InvalidOperationException("Failed to copy profiler range: " + error);
		}
		return session;
	}

	private static void NormalizeFrameRange(Session source, ref int startFrame, ref int endFrame)
	{
		if (source.FrameCount == 0)
		{
			throw new InvalidOperationException("Session has no frames.");
		}
		if (startFrame < 0)
		{
			startFrame = 0;
		}
		if (endFrame < 0 || endFrame >= source.FrameCount)
		{
			endFrame = source.FrameCount - 1;
		}
		if (startFrame >= source.FrameCount)
		{
			throw new ArgumentOutOfRangeException("start_frame", "start_frame is outside the session frame range.");
		}
		if (endFrame < startFrame)
		{
			throw new ArgumentException("end_frame must be greater than or equal to start_frame.");
		}
	}

	private static Dictionary<string, object> BuildRangeDictionary(Session source, int startFrame, int endFrame)
	{
		Frame start = source.GetFrame(startFrame);
		Frame end = source.GetFrame(endFrame);
		if (start == null || end == null)
		{
			throw new InvalidOperationException("Frame data is missing for the selected range.");
		}
		return new Dictionary<string, object>
		{
			["startFrame"] = startFrame,
			["endFrame"] = endFrame,
			["frameCount"] = endFrame + 1 - startFrame,
			["startTime"] = start.StartTime,
			["endTime"] = end.EndTime
		};
	}

	private static void ValidatePathAllowed(string fullPath)
	{
		List<string> allowedRoots = GetAllowedRoots().ToList();
		foreach (string root in allowedRoots)
		{
			if (IsPathUnderRoot(fullPath, root))
			{
				return;
			}
		}
		throw new UnauthorizedAccessException("Profiler file path is outside the allowed roots: " + string.Join(", ", allowedRoots));
	}

	private static IEnumerable<string> GetAllowedRoots()
	{
		yield return Path.GetFullPath(Directory.GetCurrentDirectory());

		string configuredRoots = Environment.GetEnvironmentVariable("PROFILER_STUDY_MCP_ALLOWED_ROOTS");
		if (!string.IsNullOrWhiteSpace(configuredRoots))
		{
			foreach (string root in configuredRoots.Split(Path.PathSeparator))
			{
				if (!string.IsNullOrWhiteSpace(root))
				{
					yield return Path.GetFullPath(root);
				}
			}
		}

		if (OperatingSystem.IsWindows())
		{
			yield return Path.GetFullPath(@"C:\workspace");
		}
		else if (Directory.Exists("/workspace"))
		{
			yield return Path.GetFullPath("/workspace");
		}
	}

	private static bool IsPathUnderRoot(string fullPath, string root)
	{
		string normalizedPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		return normalizedPath.Equals(normalizedRoot, comparison) ||
			normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison) ||
			normalizedPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, comparison);
	}

	private static Session LoadSessionFromFile(string path, CapturingLog log)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("path is required.");
		}
		string fullPath = Path.GetFullPath(path);
		ValidatePathAllowed(fullPath);
		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException("Profiler session file does not exist.", fullPath);
		}

		Session session = new Session(new CoreSettings(), log);
		string error = string.Empty;
		if (!session.Read(fullPath, ref error))
		{
			session.Close();
			throw new InvalidOperationException("Failed to read profiler session: " + error);
		}
		return session;
	}

	private string AddSession(Session session, string source, CapturingLog log)
	{
		lock (m_SessionsLock)
		{
			string id = "s" + (++m_NextSessionId).ToString();
			m_Sessions[id] = new LoadedSession
			{
				Id = id,
				Source = source,
				Session = session,
				Log = log,
				CreatedUtc = DateTime.UtcNow,
				LastAccessUtc = DateTime.UtcNow
			};
			return id;
		}
	}

	private LoadedSession GetLoadedSession(string sessionId)
	{
		lock (m_SessionsLock)
		{
			if (m_Sessions.TryGetValue(sessionId, out LoadedSession loaded))
			{
				loaded.LastAccessUtc = DateTime.UtcNow;
				return loaded;
			}
		}
		throw new ArgumentException("Unknown session_id: " + sessionId);
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

	private static double Round(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			return 0.0;
		}
		return Math.Round(value, 3);
	}
}

internal sealed class CapturingLog : ILog
{
	private readonly List<string> m_Lines = new List<string>();
	private readonly object m_Lock = new object();

	public void Write(string text)
	{
		Add(text);
	}

	public void DebugWrite(string text)
	{
		Add(text);
	}

	public ArrayList GetTail(int count)
	{
		ArrayList list = new ArrayList();
		lock (m_Lock)
		{
			foreach (string line in m_Lines.Skip(Math.Max(0, m_Lines.Count - count)))
			{
				list.Add(line);
			}
		}
		return list;
	}

	private void Add(string text)
	{
		lock (m_Lock)
		{
			foreach (string line in (text ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
			{
				m_Lines.Add(line);
			}
			if (m_Lines.Count > 500)
			{
				m_Lines.RemoveRange(0, m_Lines.Count - 500);
			}
		}
	}
}
