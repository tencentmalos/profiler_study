using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ProfilerStudy;

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
