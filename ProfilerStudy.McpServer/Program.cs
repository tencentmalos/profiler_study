using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FramePro;

namespace ProfilerStudy.McpServer;

internal static class Program
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};
	private static readonly ProfilerMcpTools Tools = new ProfilerMcpTools();

	private static int Main(string[] args)
	{
		if (args != null && args.Length == 1 && args[0] == "--self-test")
		{
			return ProfilerDiagnosticsSelfTest.Run();
		}

		Console.InputEncoding = Encoding.UTF8;
		Console.OutputEncoding = Encoding.UTF8;

		string line;
		while ((line = Console.In.ReadLine()) != null)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}
			HandleMessage(line);
		}
		return 0;
	}

	private static void HandleMessage(string line)
	{
		object id = null;
		try
		{
			Dictionary<string, object> request = DeserializeObject(line);
			request.TryGetValue("id", out id);
			string method = GetString(request, "method");
			if (method == "notifications/initialized")
			{
				return;
			}
			object result = Dispatch(method, GetDictionary(request, "params"));
			WriteResponse(id, result);
		}
		catch (Exception ex)
		{
			if (id != null)
			{
				WriteError(id, -32603, ex.Message);
			}
		}
	}

	private static object Dispatch(string method, Dictionary<string, object> parameters)
	{
		switch (method)
		{
			case "initialize":
				return new Dictionary<string, object>
				{
					["protocolVersion"] = "2025-06-18",
					["capabilities"] = new Dictionary<string, object>
					{
						["tools"] = new Dictionary<string, object>()
					},
					["serverInfo"] = new Dictionary<string, object>
					{
						["name"] = "profiler-study-mcp",
						["version"] = "0.1.0"
					}
				};
			case "tools/list":
				return new Dictionary<string, object> { ["tools"] = Tools.ListTools() };
			case "tools/call":
				return Tools.CallTool(
					GetString(parameters, "name"),
					GetDictionary(parameters, "arguments"));
			default:
				throw new InvalidOperationException("Unsupported MCP method: " + method);
		}
	}

	private static void WriteResponse(object id, object result)
	{
		WriteJson(new Dictionary<string, object>
		{
			["jsonrpc"] = "2.0",
			["id"] = id,
			["result"] = result
		});
	}

	private static void WriteError(object id, int code, string message)
	{
		WriteJson(new Dictionary<string, object>
		{
			["jsonrpc"] = "2.0",
			["id"] = id,
			["error"] = new Dictionary<string, object>
			{
				["code"] = code,
				["message"] = message
			}
		});
	}

	private static void WriteJson(object value)
	{
		Console.Out.WriteLine(JsonSerializer.Serialize(value, JsonOptions));
		Console.Out.Flush();
	}

	private static Dictionary<string, object> DeserializeObject(string json)
	{
		using JsonDocument document = JsonDocument.Parse(json);
		if (ConvertJsonElement(document.RootElement) is Dictionary<string, object> dictionary)
		{
			return dictionary;
		}
		throw new InvalidOperationException("MCP request must be a JSON object.");
	}

	private static object ConvertJsonElement(JsonElement element)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				foreach (JsonProperty property in element.EnumerateObject())
				{
					dictionary[property.Name] = ConvertJsonElement(property.Value);
				}
				return dictionary;
			case JsonValueKind.Array:
				ArrayList list = new ArrayList();
				foreach (JsonElement item in element.EnumerateArray())
				{
					list.Add(ConvertJsonElement(item));
				}
				return list;
			case JsonValueKind.String:
				return element.GetString();
			case JsonValueKind.Number:
				if (element.TryGetInt32(out int intValue))
				{
					return intValue;
				}
				if (element.TryGetInt64(out long longValue))
				{
					return longValue;
				}
				return element.GetDouble();
			case JsonValueKind.True:
				return true;
			case JsonValueKind.False:
				return false;
			case JsonValueKind.Null:
			case JsonValueKind.Undefined:
				return null;
			default:
				throw new InvalidOperationException("Unsupported JSON value kind: " + element.ValueKind);
		}
	}

	private static string GetString(Dictionary<string, object> values, string key)
	{
		if (values != null && values.TryGetValue(key, out object value) && value != null)
		{
			return Convert.ToString(value);
		}
		return string.Empty;
	}

	private static Dictionary<string, object> GetDictionary(Dictionary<string, object> values, string key)
	{
		if (values != null && values.TryGetValue(key, out object value) && value is Dictionary<string, object> dictionary)
		{
			return dictionary;
		}
		return new Dictionary<string, object>();
	}
}

internal sealed class ProfilerMcpTools
{
	private readonly ProfilerAnalysisService m_AnalysisService = new ProfilerAnalysisService();

	public ArrayList ListTools()
	{
		return new ArrayList
		{
			new Dictionary<string, object>
			{
				["name"] = "capture_android_profile",
				["description"] = "Connect to the fixed Android FramePro socket, capture for a duration, then return a compact performance analysis.",
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
				["name"] = "analyze_session_file",
				["description"] = "Load an existing FramePro .profiler, .profiler_recording, or .profiler_dump file and return a compact performance analysis.",
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
				["name"] = "get_session_summary",
				["description"] = "Return summary diagnostics for a loaded profiler session.",
				["inputSchema"] = SessionIdSchema()
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
				["description"] = "Analyze a frame range with local summary, slow frames, hotspots, and pattern diagnostics.",
				["inputSchema"] = QuerySchema(includeThreshold: true, includeFrameRange: true)
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
				case "capture_android_profile":
					structured = m_AnalysisService.CaptureAndroidProfile(
						GetString(arguments, "target", "debug"),
						GetInt(arguments, "duration_seconds", 60, 1, 300),
						GetInt(arguments, "top", 10, 1, 50),
						GetBool(arguments, "keep_session", false));
					break;
				case "analyze_session_file":
					structured = m_AnalysisService.AnalyzeSessionFile(
						GetString(arguments, "path", string.Empty),
						GetInt(arguments, "top", 10, 1, 50));
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
				case "get_session_summary":
					structured = m_AnalysisService.GetSessionSummary(GetString(arguments, "session_id", string.Empty));
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
						GetInt(arguments, "max_nodes", 200, 1, 2000),
						GetInt(arguments, "max_depth", 8, 1, 64),
						GetDouble(arguments, "min_duration_ms", 0.0));
					break;
				case "analyze_time_range":
					structured = m_AnalysisService.AnalyzeTimeRange(
						GetString(arguments, "session_id", string.Empty),
						GetInt(arguments, "start_frame", 0, 0, int.MaxValue),
						GetInt(arguments, "end_frame", 0, 0, int.MaxValue),
						GetInt(arguments, "top", 10, 1, 50),
						GetDouble(arguments, "threshold_ms", 0.0));
					break;
				default:
					throw new InvalidOperationException("Unknown tool: " + name);
			}

			return ToolResult(structured, isError: false);
		}
		catch (Exception ex)
		{
			return ToolResult(new Dictionary<string, object>
			{
				["error"] = ex.Message,
				["exceptionType"] = ex.GetType().FullName,
				["stackTrace"] = ex.StackTrace ?? string.Empty
			}, isError: true);
		}
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

	private static bool GetBool(Dictionary<string, object> values, string key, bool defaultValue)
	{
		if (values != null && values.TryGetValue(key, out object raw) && raw != null)
		{
			return Convert.ToBoolean(raw);
		}
		return defaultValue;
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

	private static Dictionary<string, object> QuerySchema(bool includeThreshold, bool includeFrameRange)
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
		return new Dictionary<string, object>
		{
			["type"] = "object",
			["required"] = new ArrayList { "session_id" },
			["properties"] = properties
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
					["default"] = 200,
					["description"] = "Maximum number of hierarchical flame graph nodes returned across all threads."
				},
				["max_depth"] = new Dictionary<string, object>
				{
					["type"] = "integer",
					["minimum"] = 1,
					["maximum"] = 64,
					["default"] = 8,
					["description"] = "Maximum parent/child depth to expand per thread."
				},
				["min_duration_ms"] = new Dictionary<string, object>
				{
					["type"] = "number",
					["minimum"] = 0,
					["default"] = 0,
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
