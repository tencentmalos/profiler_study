using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using FramePro;

namespace ProfilerStudy.McpServer;

internal static class Program
{
	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
	private static readonly ProfilerMcpTools Tools = new ProfilerMcpTools();

	private static int Main()
	{
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
			Dictionary<string, object> request = Json.Deserialize<Dictionary<string, object>>(line);
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
		Console.Out.WriteLine(Json.Serialize(value));
		Console.Out.Flush();
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
						GetInt(arguments, "top", 10, 1, 50));
					break;
				case "analyze_session_file":
					structured = m_AnalysisService.AnalyzeSessionFile(
						GetString(arguments, "path", string.Empty),
						GetInt(arguments, "top", 10, 1, 50));
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
				["error"] = ex.Message
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
}
