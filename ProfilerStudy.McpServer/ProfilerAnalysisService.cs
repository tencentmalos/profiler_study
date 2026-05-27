using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FramePro;
using SCLCoreCLR;

namespace ProfilerStudy.McpServer;

internal sealed class ProfilerAnalysisService
{
	public Dictionary<string, object> CaptureAndroidProfile(string target, int durationSeconds, int top)
	{
		string endpoint = ResolveAndroidEndpoint(target);
		CapturingLog log = new CapturingLog();
		Session session = new Session(new CoreSettings(), log);
		try
		{
			if (!session.ConnectToAndroid(endpoint))
			{
				throw new InvalidOperationException(session.LastConnectionError ?? "Android connection failed.");
			}

			session.StartReceiving();
			DateTime end = DateTime.UtcNow.AddSeconds(durationSeconds);
			while (DateTime.UtcNow < end && session.Connected)
			{
				Thread.Sleep(250);
			}

			session.Disconnect(DisconnectReason.Requested);
			session.WaitforProcessingToFinish(10000);
			Dictionary<string, object> analysis = AnalyzeSession(session, top);
			analysis["capture"] = new Dictionary<string, object>
			{
				["target"] = target,
				["endpoint"] = endpoint,
				["durationSeconds"] = durationSeconds,
				["disconnectReason"] = session.DisconnectReason.ToString()
			};
			analysis["logTail"] = log.GetTail(40);
			return analysis;
		}
		finally
		{
			session.Close();
		}
	}

	public Dictionary<string, object> AnalyzeSessionFile(string path, int top)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("path is required.");
		}
		string fullPath = Path.GetFullPath(path);
		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException("Profiler session file does not exist.", fullPath);
		}

		CapturingLog log = new CapturingLog();
		Session session = new Session(new CoreSettings(), log);
		try
		{
			string error = string.Empty;
			if (!session.Read(fullPath, ref error))
			{
				throw new InvalidOperationException("Failed to read profiler session: " + error);
			}
			Dictionary<string, object> analysis = AnalyzeSession(session, top);
			analysis["sourceFile"] = fullPath;
			analysis["logTail"] = log.GetTail(40);
			return analysis;
		}
		finally
		{
			session.Close();
		}
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
		double tickToMs = session.TimerFrequency > 0 ? 1000.0 / session.TimerFrequency : 0.0;
		ArrayList slowFrames = GetSlowFrames(session, top, tickToMs);
		ArrayList hotspots = GetScopeHotspots(session, top, tickToMs);
		ArrayList customStats = GetCustomStats(session, top);
		ArrayList threads = GetThreads(session, top);

		return new Dictionary<string, object>
		{
			["summary"] = new Dictionary<string, object>
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
			},
			["slowFrames"] = slowFrames,
			["scopeHotspots"] = hotspots,
			["customStats"] = customStats,
			["threads"] = threads
		};
	}

	private static ArrayList GetSlowFrames(Session session, int top, double tickToMs)
	{
		List<Dictionary<string, object>> frames = new List<Dictionary<string, object>>();
		for (int i = 0; i < session.FrameCount; i++)
		{
			Frame frame = session.GetFrame(i);
			frames.Add(new Dictionary<string, object>
			{
				["index"] = frame.Index,
				["durationMs"] = Round(frame.Duration * tickToMs),
				["startTime"] = frame.StartTime,
				["endTime"] = frame.EndTime,
				["timeSpanCount"] = frame.TimeSpanCount,
				["bytesSent"] = frame.BytesSent
			});
		}
		return ToArrayList(frames.OrderByDescending(f => Convert.ToDouble(f["durationMs"])).Take(top));
	}

	private static ArrayList GetScopeHotspots(Session session, int top, double tickToMs)
	{
		List<Dictionary<string, object>> scopes = new List<Dictionary<string, object>>();
		foreach (ScopeSessionStats stat in session.GetTimeSpanStats())
		{
			scopes.Add(new Dictionary<string, object>
			{
				["name"] = stat.m_Name,
				["totalMs"] = Round(stat.m_TotalTime * tickToMs),
				["totalCount"] = stat.m_TotalCount,
				["maxMsPerFrame"] = Round(stat.m_MaxTimePerFrame * tickToMs),
				["maxCountPerFrame"] = stat.m_MaxCountPerFrame
			});
		}
		return ToArrayList(scopes.OrderByDescending(s => Convert.ToDouble(s["totalMs"])).Take(top));
	}

	private static ArrayList GetCustomStats(Session session, int top)
	{
		List<Dictionary<string, object>> stats = new List<Dictionary<string, object>>();
		foreach (CustomStatSessionData stat in session.GetCustomStats())
		{
			stats.Add(new Dictionary<string, object>
			{
				["name"] = session.GetString(stat.Name),
				["valueType"] = stat.ValueType.ToString(),
				["totalCount"] = stat.m_TotalCount,
				["totalValueInt64"] = stat.m_TotalValueInt64,
				["totalValueDouble"] = Round(stat.m_TotalValueDouble),
				["maxCountPerFrame"] = stat.m_MaxCountPerFrame,
				["firstFrameSeen"] = stat.m_FirstFrameSeen
			});
		}
		return ToArrayList(stats.OrderByDescending(s => Convert.ToInt64(s["totalCount"])).Take(top));
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
