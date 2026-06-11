using System;
using System.Collections;
using System.Collections.Generic;
using ProfilerStudy.Tracy;

namespace ProfilerStudy.McpServer;

internal sealed class ProfilerMcpTools
{
	private readonly ProfilerAnalysisService m_AnalysisService = new ProfilerAnalysisService();

	public ArrayList ListTools()
	{
		return new ArrayList
		{
			new Dictionary<string, object>
			{
				["name"] = "capture_profile",
				["description"] = "Connect to a profiler target by URL, capture for a duration, then return a compact performance analysis. Use android://{forward_name} for adb forward to a device localfilesystem, localabstract, or tcp endpoint, or pc://{ip}:{port} for direct TCP.",
				["inputSchema"] = new Dictionary<string, object>
				{
					["type"] = "object",
					["required"] = new ArrayList { "url" },
					["properties"] = new Dictionary<string, object>
					{
						["url"] = new Dictionary<string, object>
						{
							["type"] = "string",
							["description"] = "Capture target URL, for example android:///data/local/tmp/framepro, android://localabstract:azahar-framepro, android://tcp:8428, or pc://127.0.0.1:8428."
						},
						["protocol"] = new Dictionary<string, object>
						{
							["type"] = "string",
							["enum"] = new ArrayList { "study", "tracy", "perfetto" },
							["default"] = "study",
							["description"] = "Capture protocol. study is the existing ProfilerStudy transport; tracy and perfetto are explicit external trace protocols."
						},
						["duration_seconds"] = new Dictionary<string, object>
						{
							["type"] = "integer",
							["minimum"] = 1,
							["maximum"] = 300,
							["default"] = 60
						},
						["top"] = new Dictionary<string, object>
						{
							["type"] = "integer",
							["minimum"] = 1,
							["maximum"] = 50,
							["default"] = 10
						},
						["keep_session"] = new Dictionary<string, object>
						{
							["type"] = "boolean",
							["default"] = false,
							["description"] = "Keep the captured session in memory and return a session_id for follow-up queries."
						}
					}
				}
			},
			new Dictionary<string, object>
			{
				["name"] = "capture_android_profile",
				["description"] = "Compatibility wrapper for fixed Android debug/release ProfilerStudy sockets. Prefer capture_profile with android:// or pc:// URLs for new use.",
				["inputSchema"] = new Dictionary<string, object>
				{
					["type"] = "object",
					["properties"] = new Dictionary<string, object>
					{
						["target"] = new Dictionary<string, object>
						{
							["type"] = "string",
							["enum"] = new ArrayList { "debug", "release" },
							["description"] = "Android package channel to capture."
						},
						["duration_seconds"] = new Dictionary<string, object>
						{
							["type"] = "integer",
							["minimum"] = 1,
							["maximum"] = 300,
							["default"] = 60
						},
						["top"] = new Dictionary<string, object>
						{
							["type"] = "integer",
							["minimum"] = 1,
							["maximum"] = 50,
							["default"] = 10
						},
						["keep_session"] = new Dictionary<string, object>
						{
							["type"] = "boolean",
							["default"] = false,
							["description"] = "Keep the captured session in memory and return a session_id for follow-up queries."
						}
					}
				}
			},
			new Dictionary<string, object>
			{
				["name"] = "get_tracy_status",
				["description"] = "Return built-in C# Tracy support status, including the locked Tracy version and optional source-reference submodule availability.",
				["inputSchema"] = new Dictionary<string, object>
				{
					["type"] = "object",
					["properties"] = new Dictionary<string, object>()
				}
			},
			new Dictionary<string, object>
			{
				["name"] = "analyze_session_file",
				["description"] = "Load an existing ProfilerStudy .profiler, .profiler_recording, or .profiler_dump file and return a compact performance analysis.",
				["inputSchema"] = new Dictionary<string, object>
				{
					["type"] = "object",
					["required"] = new ArrayList { "path" },
					["properties"] = new Dictionary<string, object>
					{
						["path"] = new Dictionary<string, object> { ["type"] = "string" },
						["top"] = new Dictionary<string, object>
						{
							["type"] = "integer",
							["minimum"] = 1,
							["maximum"] = 50,
							["default"] = 10
						}
					}
				}
			},
			new Dictionary<string, object>
			{
				["name"] = "load_trace_file",
				["description"] = "Load a trace file through the unified trace path. The initial implementation supports Tracy .tracy files locked to Tracy 0.10.0.",
				["inputSchema"] = TraceFileSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "list_trace_artifacts",
				["description"] = "List trace artifacts registered by unified trace file imports and live captures.",
				["inputSchema"] = TraceArtifactListSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "load_trace_artifact",
				["description"] = "Load a registered trace artifact from the artifact store. The initial implementation supports Tracy artifacts with normalized cache data.",
				["inputSchema"] = TraceArtifactLoadSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "load_session_file",
				["description"] = "Load a profiler file into memory and return a session_id for follow-up analysis tools.",
				["inputSchema"] = new Dictionary<string, object>
				{
					["type"] = "object",
					["required"] = new ArrayList { "path" },
					["properties"] = new Dictionary<string, object>
					{
						["path"] = new Dictionary<string, object> { ["type"] = "string" },
						["top"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 50, ["default"] = 10 }
					}
				}
			},
			new Dictionary<string, object>
			{
				["name"] = "close_session",
				["description"] = "Close a session previously returned by load_session_file or capture_android_profile keep_session=true.",
				["inputSchema"] = SessionIdSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "list_sessions",
				["description"] = "List profiler sessions currently kept in memory by this MCP server.",
				["inputSchema"] = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object>() }
			},
			new Dictionary<string, object>
			{
				["name"] = "get_import_diagnostics",
				["description"] = "Return import or live-capture diagnostics for a loaded trace session or registered trace artifact.",
				["inputSchema"] = ImportDiagnosticsSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "get_session_summary",
				["description"] = "Return summary diagnostics for a loaded profiler session.",
				["inputSchema"] = SessionIdSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "get_profiler_overhead",
				["description"] = "Return ProfilerStudy profiler overhead for a loaded session, including target-side profiler memory, bytes sent, wait-for-send stalls, and previous-frame send time.",
				["inputSchema"] = ProfilerOverheadSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "find_slow_frames",
				["description"] = "Find slow frames in a loaded session, including cadence and clustering diagnostics.",
				["inputSchema"] = QuerySchema(includeThreshold: true, includeFrameRange: false)
			},
			new Dictionary<string, object>
			{
				["name"] = "find_scope_hotspots",
				["description"] = "Find scope hotspots in a loaded session, optionally restricted to a frame range.",
				["inputSchema"] = QuerySchema(includeThreshold: false, includeFrameRange: true)
			},
			new Dictionary<string, object>
			{
				["name"] = "list_counters",
				["description"] = "List custom stat counters in a loaded session, optionally filtered by name.",
				["inputSchema"] = CounterListSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "query_counter",
				["description"] = "Return per-frame samples for one custom stat counter over a frame range.",
				["inputSchema"] = CounterQuerySchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "analyze_frame",
				["description"] = "Analyze one frame's local scope hotspots and nearby-frame context.",
				["inputSchema"] = FrameAnalysisSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "analyze_frame_detail",
				["description"] = "Extract hierarchical per-thread flame graph data for one frame for deeper single-frame analysis.",
				["inputSchema"] = FrameDetailSchema()
			},
			new Dictionary<string, object>
			{
				["name"] = "analyze_time_range",
				["description"] = "Analyze a frame range or trace time range with local summary, slow frames, hotspots, and pattern diagnostics.",
				["inputSchema"] = QuerySchema(includeThreshold: true, includeFrameRange: true, includeTimeRange: true)
			}
		};
	}

	public Dictionary<string, object> CallTool(string name, Dictionary<string, object> arguments)
	{
		try
		{
			Dictionary<string, object> structured;
			switch (name)
			{
				case "capture_profile":
					structured = m_AnalysisService.CaptureProfile(
						GetString(arguments, "url", string.Empty),
						GetString(arguments, "protocol", "study"),
						GetInt(arguments, "duration_seconds", 60, 1, 300),
						GetInt(arguments, "top", 10, 1, 50),
						GetBool(arguments, "keep_session", false));
					break;
				case "capture_android_profile":
					structured = m_AnalysisService.CaptureAndroidProfile(
						GetString(arguments, "target", "debug"),
						GetInt(arguments, "duration_seconds", 60, 1, 300),
						GetInt(arguments, "top", 10, 1, 50),
						GetBool(arguments, "keep_session", false));
					break;
				case "get_tracy_status":
					structured = m_AnalysisService.GetTracyStatus();
					break;
				case "analyze_session_file":
					structured = m_AnalysisService.AnalyzeSessionFile(
						GetString(arguments, "path", string.Empty),
						GetInt(arguments, "top", 10, 1, 50));
					break;
				case "load_trace_file":
					structured = m_AnalysisService.LoadTraceFile(
						GetString(arguments, "path", string.Empty),
						GetString(arguments, "format", "auto"),
						GetInt(arguments, "top", 10, 1, 50),
						GetBool(arguments, "keep_session", false));
					break;
				case "list_trace_artifacts":
					structured = m_AnalysisService.ListTraceArtifacts(
						GetString(arguments, "format", "all"),
						GetInt(arguments, "limit", 20, 1, 200));
					break;
				case "load_trace_artifact":
					structured = m_AnalysisService.LoadTraceArtifact(
						GetString(arguments, "artifact_id", string.Empty),
						GetInt(arguments, "top", 10, 1, 50),
						GetBool(arguments, "keep_session", false));
					break;
				case "load_session_file":
					structured = m_AnalysisService.LoadSessionFile(
						GetString(arguments, "path", string.Empty),
						GetInt(arguments, "top", 10, 1, 50));
					break;
				case "close_session":
					structured = m_AnalysisService.CloseSession(GetString(arguments, "session_id", string.Empty));
					break;
				case "list_sessions":
					structured = m_AnalysisService.ListSessions();
					break;
				case "get_import_diagnostics":
					structured = m_AnalysisService.GetImportDiagnostics(
						GetString(arguments, "session_id", string.Empty),
						GetString(arguments, "artifact_id", string.Empty));
					break;
				case "get_session_summary":
					structured = m_AnalysisService.GetSessionSummary(GetString(arguments, "session_id", string.Empty));
					break;
				case "get_profiler_overhead":
					structured = m_AnalysisService.GetProfilerOverhead(
						GetString(arguments, "session_id", string.Empty),
						GetInt(arguments, "start_frame", -1, -1, int.MaxValue),
						GetInt(arguments, "end_frame", -1, -1, int.MaxValue),
						GetInt(arguments, "top", 10, 1, 50));
					break;
				case "find_slow_frames":
					structured = m_AnalysisService.FindSlowFrames(
						GetString(arguments, "session_id", string.Empty),
						GetInt(arguments, "top", 10, 1, 50),
						GetDouble(arguments, "threshold_ms", 0.0));
					break;
				case "find_scope_hotspots":
					structured = m_AnalysisService.FindScopeHotspots(
						GetString(arguments, "session_id", string.Empty),
						GetInt(arguments, "top", 10, 1, 50),
						GetInt(arguments, "start_frame", -1, -1, int.MaxValue),
						GetInt(arguments, "end_frame", -1, -1, int.MaxValue));
					break;
				case "list_counters":
					structured = m_AnalysisService.ListCounters(
						GetString(arguments, "session_id", string.Empty),
						GetInt(arguments, "top", 20, 1, 200),
						GetString(arguments, "filter", string.Empty));
					break;
				case "query_counter":
					structured = m_AnalysisService.QueryCounterSamples(
						GetString(arguments, "session_id", string.Empty),
						GetString(arguments, "counter_name", string.Empty),
						GetInt(arguments, "start_frame", -1, -1, int.MaxValue),
						GetInt(arguments, "end_frame", -1, -1, int.MaxValue),
						GetBool(arguments, "accumulated", false),
						GetInt(arguments, "max_samples", 200, 1, 5000));
					break;
				case "analyze_frame":
					structured = m_AnalysisService.AnalyzeFrame(
						GetString(arguments, "session_id", string.Empty),
						GetInt(arguments, "frame_index", 0, 0, int.MaxValue),
						GetInt(arguments, "top", 10, 1, 50),
						GetInt(arguments, "neighbor_count", 3, 0, 20));
					break;
				case "analyze_frame_detail":
					structured = m_AnalysisService.AnalyzeFrameDetail(
						GetString(arguments, "session_id", string.Empty),
						GetInt(arguments, "frame_index", 0, 0, int.MaxValue),
						GetInt(arguments, "max_nodes", 300, 1, 2000),
						GetInt(arguments, "max_depth", 12, 1, 64),
						GetDouble(arguments, "min_duration_ms", 0.01));
					break;
				case "analyze_time_range":
					if (HasArgument(arguments, "start_time_ns") || HasArgument(arguments, "end_time_ns"))
					{
						structured = m_AnalysisService.AnalyzeTimeRange(
							GetString(arguments, "session_id", string.Empty),
							GetLong(arguments, "start_time_ns", 0L),
							GetLong(arguments, "end_time_ns", 0L),
							GetInt(arguments, "top", 10, 1, 50),
							GetDouble(arguments, "threshold_ms", 0.0));
					}
					else
					{
						structured = m_AnalysisService.AnalyzeTimeRange(
							GetString(arguments, "session_id", string.Empty),
							GetInt(arguments, "start_frame", 0, 0, int.MaxValue),
							GetInt(arguments, "end_frame", 0, 0, int.MaxValue),
							GetInt(arguments, "top", 10, 1, 50),
							GetDouble(arguments, "threshold_ms", 0.0));
					}
					break;
				default:
					throw new InvalidOperationException("Unknown tool: " + name);
			}

			return ToolResult(structured, isError: false);
		}
		catch (Exception ex)
		{
			if (ex is TracyFileFormatException tracyException)
			{
				return ToolResult(BuildTracyError(tracyException), isError: true);
			}
			return ToolResult(new Dictionary<string, object>
			{
				["error"] = ex.Message,
				["exceptionType"] = ex.GetType().FullName,
				["stackTrace"] = ex.StackTrace ?? string.Empty
			}, isError: true);
		}
	}

	private static Dictionary<string, object> BuildTracyError(TracyFileFormatException exception)
	{
		TracyStatus status = TracyVersionRegistry.GetStatus();
		ArrayList supportedVersions = ToArrayList(exception.SupportedVersions.Count > 0
			? exception.SupportedVersions
			: status.SupportedVersions);
		string lockedVersion = string.IsNullOrWhiteSpace(exception.LockedVersion)
			? status.LockedVersion
			: exception.LockedVersion;
		Dictionary<string, object> diagnostic = new Dictionary<string, object>
		{
			["severity"] = "error",
			["code"] = exception.ErrorCode,
			["message"] = exception.Message,
			["detectedVersion"] = exception.DetectedVersion,
			["supportedVersions"] = supportedVersions,
			["lockedVersion"] = lockedVersion
		};
		return new Dictionary<string, object>
		{
			["error"] = exception.Message,
			["errorCode"] = exception.ErrorCode,
			["message"] = exception.Message,
			["exceptionType"] = exception.GetType().FullName,
			["stackTrace"] = exception.StackTrace ?? string.Empty,
			["detectedVersion"] = exception.DetectedVersion,
			["supportedVersions"] = supportedVersions,
			["lockedVersion"] = lockedVersion,
			["tracyStatus"] = new Dictionary<string, object>
			{
				["lockedVersion"] = status.LockedVersion,
				["supportedVersions"] = ToArrayList(status.SupportedVersions),
				["sourceReferencePath"] = status.SourceReferencePath,
				["sourceReferenceAvailable"] = status.SourceReferenceAvailable,
				["reason"] = status.Reason
			},
			["diagnostics"] = new ArrayList { diagnostic }
		};
	}

	private static Dictionary<string, object> ToolResult(Dictionary<string, object> structured, bool isError)
	{
		return new Dictionary<string, object>
		{
			["content"] = new ArrayList
			{
				new Dictionary<string, object>
				{
					["type"] = "text",
					["text"] = ProfilerReportFormatter.ToMarkdown(structured)
				}
			},
			["structuredContent"] = structured,
			["isError"] = isError
		};
	}

	private static ArrayList ToArrayList(IEnumerable<string> values)
	{
		ArrayList list = new ArrayList();
		foreach (string value in values)
		{
			list.Add(value);
		}
		return list;
	}

	private static string GetString(Dictionary<string, object> values, string key, string defaultValue)
	{
		if (values != null && values.TryGetValue(key, out object value) && value != null)
		{
			return Convert.ToString(value);
		}
		return defaultValue;
	}

	private static int GetInt(Dictionary<string, object> values, string key, int defaultValue, int min, int max)
	{
		int value = defaultValue;
		if (values != null && values.TryGetValue(key, out object raw) && raw != null)
		{
			value = Convert.ToInt32(raw);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	private static double GetDouble(Dictionary<string, object> values, string key, double defaultValue)
	{
		if (values != null && values.TryGetValue(key, out object raw) && raw != null)
		{
			return Convert.ToDouble(raw);
		}
		return defaultValue;
	}

	private static long GetLong(Dictionary<string, object> values, string key, long defaultValue)
	{
		if (values != null && values.TryGetValue(key, out object raw) && raw != null)
		{
			return Convert.ToInt64(raw);
		}
		return defaultValue;
	}

	private static bool GetBool(Dictionary<string, object> values, string key, bool defaultValue)
	{
		if (values != null && values.TryGetValue(key, out object raw) && raw != null)
		{
			return Convert.ToBoolean(raw);
		}
		return defaultValue;
	}

	private static bool HasArgument(Dictionary<string, object> values, string key)
	{
		return values != null && values.ContainsKey(key);
	}

	private static Dictionary<string, object> SessionIdSchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "session_id" },
			["properties"] = new Dictionary<string, object>
			{
				["session_id"] = new Dictionary<string, object> { ["type"] = "string" }
			}
		};
	}

	private static Dictionary<string, object> QuerySchema(bool includeThreshold, bool includeFrameRange, bool includeTimeRange = false)
	{
		Dictionary<string, object> properties = new Dictionary<string, object>
		{
			["session_id"] = new Dictionary<string, object> { ["type"] = "string" },
			["top"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 50, ["default"] = 10 }
		};
		if (includeThreshold)
		{
			properties["threshold_ms"] = new Dictionary<string, object> { ["type"] = "number", ["minimum"] = 0, ["default"] = 0 };
		}
		if (includeFrameRange)
		{
			properties["start_frame"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0 };
			properties["end_frame"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0 };
		}
		if (includeTimeRange)
		{
			properties["start_time_ns"] = new Dictionary<string, object>
			{
				["type"] = "integer",
				["minimum"] = 0,
				["description"] = "Trace-relative start time in nanoseconds. Trace-backed sessions use this in preference to frame range when supplied."
			};
			properties["end_time_ns"] = new Dictionary<string, object>
			{
				["type"] = "integer",
				["minimum"] = 0,
				["description"] = "Trace-relative end time in nanoseconds. Trace-backed sessions use this in preference to frame range when supplied."
			};
		}
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "session_id" },
			["properties"] = properties
		};
	}

	private static Dictionary<string, object> ProfilerOverheadSchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "session_id" },
			["properties"] = new Dictionary<string, object>
			{
				["session_id"] = new Dictionary<string, object> { ["type"] = "string" },
				["start_frame"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0 },
				["end_frame"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0 },
				["top"] = new Dictionary<string, object>
				{
					["type"] = "integer",
					["minimum"] = 1,
					["maximum"] = 50,
					["default"] = 10,
					["description"] = "Maximum number of wait-heavy and bytes-heavy frames to return."
				}
			}
		};
	}

	private static Dictionary<string, object> FrameAnalysisSchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "session_id", "frame_index" },
			["properties"] = new Dictionary<string, object>
			{
				["session_id"] = new Dictionary<string, object> { ["type"] = "string" },
				["frame_index"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0 },
				["top"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 50, ["default"] = 10 },
				["neighbor_count"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 20, ["default"] = 3 }
			}
		};
	}

	private static Dictionary<string, object> TraceFileSchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "path" },
			["properties"] = new Dictionary<string, object>
			{
				["path"] = new Dictionary<string, object> { ["type"] = "string" },
				["format"] = new Dictionary<string, object>
				{
					["type"] = "string",
					["enum"] = new ArrayList { "auto", "tracy" },
					["default"] = "auto"
				},
				["top"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 50, ["default"] = 10 },
				["keep_session"] = new Dictionary<string, object>
				{
					["type"] = "boolean",
					["default"] = false,
					["description"] = "Keep the loaded trace in memory and return a session_id for follow-up queries."
				}
			}
		};
	}

	private static Dictionary<string, object> TraceArtifactLoadSchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "artifact_id" },
			["properties"] = new Dictionary<string, object>
			{
				["artifact_id"] = new Dictionary<string, object> { ["type"] = "string" },
				["top"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 50, ["default"] = 10 },
				["keep_session"] = new Dictionary<string, object>
				{
					["type"] = "boolean",
					["default"] = false,
					["description"] = "Keep the loaded artifact in memory and return a session_id for follow-up queries."
				}
			}
		};
	}

	private static Dictionary<string, object> TraceArtifactListSchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["properties"] = new Dictionary<string, object>
			{
				["format"] = new Dictionary<string, object>
				{
					["type"] = "string",
					["enum"] = new ArrayList { "all", "study", "tracy", "perfetto", "systrace" },
					["default"] = "all"
				},
				["limit"] = new Dictionary<string, object>
				{
					["type"] = "integer",
					["minimum"] = 1,
					["maximum"] = 200,
					["default"] = 20
				}
			}
		};
	}

	private static Dictionary<string, object> ImportDiagnosticsSchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["properties"] = new Dictionary<string, object>
			{
				["session_id"] = new Dictionary<string, object>
				{
					["type"] = "string",
					["description"] = "Loaded trace session id returned by load_trace_file, load_trace_artifact, or capture_profile with keep_session=true."
				},
				["artifact_id"] = new Dictionary<string, object>
				{
					["type"] = "string",
					["description"] = "Trace artifact id returned by load_trace_file, load_trace_artifact, capture_profile(protocol=tracy), or list_trace_artifacts."
				}
			}
		};
	}

	private static Dictionary<string, object> FrameDetailSchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "session_id", "frame_index" },
			["properties"] = new Dictionary<string, object>
			{
				["session_id"] = new Dictionary<string, object> { ["type"] = "string" },
				["frame_index"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0 },
				["max_nodes"] = new Dictionary<string, object>
				{
					["type"] = "integer",
					["minimum"] = 1,
					["maximum"] = 2000,
					["default"] = 300,
					["description"] = "Maximum number of hierarchical flame graph nodes returned across all threads."
				},
				["max_depth"] = new Dictionary<string, object>
				{
					["type"] = "integer",
					["minimum"] = 1,
					["maximum"] = 64,
					["default"] = 12,
					["description"] = "Maximum parent/child depth to expand per thread."
				},
				["min_duration_ms"] = new Dictionary<string, object>
				{
					["type"] = "number",
					["minimum"] = 0,
					["default"] = 0.01,
					["description"] = "Drop flame graph nodes whose clipped frame-local duration is below this threshold."
				}
			}
		};
	}

	private static Dictionary<string, object> CounterListSchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "session_id" },
			["properties"] = new Dictionary<string, object>
			{
				["session_id"] = new Dictionary<string, object> { ["type"] = "string" },
				["top"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 200, ["default"] = 20 },
				["filter"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Case-insensitive substring filter for counter names." }
			}
		};
	}

	private static Dictionary<string, object> CounterQuerySchema()
	{
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "session_id", "counter_name" },
			["properties"] = new Dictionary<string, object>
			{
				["session_id"] = new Dictionary<string, object> { ["type"] = "string" },
				["counter_name"] = new Dictionary<string, object> { ["type"] = "string" },
				["start_frame"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0 },
				["end_frame"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0 },
				["accumulated"] = new Dictionary<string, object> { ["type"] = "boolean", ["default"] = false },
				["max_samples"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 5000, ["default"] = 200 }
			}
		};
	}
}
