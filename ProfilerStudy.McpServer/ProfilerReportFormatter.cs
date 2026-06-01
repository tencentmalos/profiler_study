using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ProfilerStudy.McpServer;

internal static class ProfilerReportFormatter
{
	public static string ToMarkdown(Dictionary<string, object> result)
	{
		if (result == null)
		{
			return "No result.";
		}
		if (result.TryGetValue("error", out object error))
		{
			return "Profiler MCP error: " + error;
		}

		StringBuilder builder = new StringBuilder();
		builder.AppendLine("# Profiler Analysis");
		if (result.TryGetValue("capture", out object captureObj) && captureObj is Dictionary<string, object> capture)
		{
			builder.AppendLine();
			builder.AppendLine("## Capture");
			AppendKeyValues(builder, capture);
		}
		if (result.TryGetValue("sourceFile", out object sourceFile))
		{
			builder.AppendLine();
			builder.AppendLine("Source file: `" + sourceFile + "`");
		}
		if (result.TryGetValue("sessionId", out object sessionId))
		{
			builder.AppendLine();
			builder.AppendLine("Session id: `" + sessionId + "`");
		}
		if (result.TryGetValue("summary", out object summaryObj) && summaryObj is Dictionary<string, object> summary)
		{
			builder.AppendLine();
			builder.AppendLine("## Summary");
			AppendKeyValues(builder, summary);
		}
		if (result.TryGetValue("range", out object rangeObj) && rangeObj is Dictionary<string, object> range)
		{
			builder.AppendLine();
			builder.AppendLine("## Range");
			AppendKeyValues(builder, range);
		}
		if (result.TryGetValue("frame", out object frameObj) && frameObj is Dictionary<string, object> frame)
		{
			builder.AppendLine();
			builder.AppendLine("## Frame");
			AppendKeyValues(builder, frame);
		}
		AppendTable(builder, "Slow Frames", result, "slowFrames", new[] { "index", "durationMs", "timeSpanCount", "bytesSent" });
		AppendTable(builder, "Scope Hotspots", result, "scopeHotspots", new[] { "name", "totalMs", "totalCount", "maxMsPerFrame" });
		AppendTable(builder, "Custom Stats", result, "customStats", new[] { "name", "valueType", "totalCount", "maxCountPerFrame" });
		AppendTable(builder, "Counters", result, "counters", new[] { "name", "valueType", "unit", "totalCount", "maxValuePerFrame" });
		AppendTable(builder, "Counter Samples", result, "samples", new[] { "frameIndex", "frameEndTime", "value", "count" });
		AppendTable(builder, "Threads", result, "threads", new[] { "id", "name" });
		AppendTable(builder, "Sessions", result, "sessions", new[] { "sessionId", "source", "frameCount", "threadCount" });
		if (result.TryGetValue("slowFramePattern", out object patternObj) && patternObj is Dictionary<string, object> pattern)
		{
			builder.AppendLine();
			builder.AppendLine("## Slow Frame Pattern");
			AppendKeyValues(builder, pattern);
		}
		if (result.TryGetValue("diagnostics", out object diagnosticsObj) && diagnosticsObj is Dictionary<string, object> diagnostics)
		{
			builder.AppendLine();
			builder.AppendLine("## Diagnostics");
			AppendKeyValues(builder, diagnostics);
		}
		if (result.TryGetValue("captureTelemetry", out object telemetryObj) && telemetryObj is Dictionary<string, object> telemetry)
		{
			builder.AppendLine();
			builder.AppendLine("## Capture Telemetry");
			AppendKeyValues(builder, telemetry);
		}
		return builder.ToString().TrimEnd();
	}

	private static void AppendKeyValues(StringBuilder builder, Dictionary<string, object> values)
	{
		foreach (KeyValuePair<string, object> pair in values)
		{
			builder.AppendLine("- `" + pair.Key + "`: " + pair.Value);
		}
	}

	private static void AppendTable(StringBuilder builder, string title, Dictionary<string, object> result, string key, string[] columns)
	{
		if (!result.TryGetValue(key, out object value) || !(value is ArrayList rows) || rows.Count == 0)
		{
			return;
		}
		builder.AppendLine();
		builder.AppendLine("## " + title);
		builder.Append("| ");
		foreach (string column in columns)
		{
			builder.Append(column).Append(" | ");
		}
		builder.AppendLine();
		builder.Append("| ");
		foreach (string _ in columns)
		{
			builder.Append("--- | ");
		}
		builder.AppendLine();
		foreach (object rowObject in rows)
		{
			if (!(rowObject is Dictionary<string, object> row))
			{
				continue;
			}
			builder.Append("| ");
			foreach (string column in columns)
			{
				row.TryGetValue(column, out object cell);
				builder.Append(Convert.ToString(cell)).Append(" | ");
			}
			builder.AppendLine();
		}
	}
}
