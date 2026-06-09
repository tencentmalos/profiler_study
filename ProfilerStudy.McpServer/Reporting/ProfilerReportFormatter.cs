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
		AppendFlameGraphs(builder, result);
		AppendTable(builder, "Custom Stats", result, "customStats", new[] { "name", "valueType", "totalCount", "maxCountPerFrame" });
		AppendTable(builder, "Counters", result, "counters", new[] { "name", "valueType", "unit", "totalCount", "maxValuePerFrame" });
		AppendTable(builder, "Counter Samples", result, "samples", new[] { "frameIndex", "frameEndTime", "value", "count" });
		AppendTable(builder, "Frame Counters", result, "frameCounters", new[] { "name", "unit", "value", "count", "accumulatedValue" });
		AppendTable(builder, "Top Frame Detail Spans", result, "topSpans", new[] { "name", "threadName", "depth", "durationMs", "selfMs" });
		AppendTable(builder, "Threads", result, "threads", new[] { "id", "name" });
		AppendTable(builder, "Sessions", result, "sessions", new[] { "sessionId", "source", "frameCount", "threadCount" });
		if (result.TryGetValue("nodeStats", out object nodeStatsObj) && nodeStatsObj is Dictionary<string, object> nodeStats)
		{
			builder.AppendLine();
			builder.AppendLine("## Frame Detail Node Stats");
			AppendKeyValues(builder, nodeStats);
		}
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

	private static void AppendFlameGraphs(StringBuilder builder, Dictionary<string, object> result)
	{
		if (!result.TryGetValue("threadFlameGraphs", out object value) || !(value is ArrayList threads) || threads.Count == 0)
		{
			return;
		}
		builder.AppendLine();
		builder.AppendLine("## Thread Flame Graphs");
		foreach (object threadObject in threads)
		{
			if (!(threadObject is Dictionary<string, object> thread))
			{
				continue;
			}
			builder.AppendLine();
			builder.AppendLine("### " + Convert.ToString(thread["threadName"]) + " (" + Convert.ToString(thread["threadId"]) + ")");
			if (thread.TryGetValue("roots", out object rootsObj) && rootsObj is ArrayList roots)
			{
				foreach (object rootObject in roots)
				{
					if (rootObject is Dictionary<string, object> root)
					{
						AppendFlameNode(builder, root, 0);
					}
				}
			}
		}
	}

	private static void AppendFlameNode(StringBuilder builder, Dictionary<string, object> node, int depth)
	{
		builder.Append("- ");
		for (int i = 0; i < depth; i++)
		{
			builder.Append("  ");
		}
		builder.Append(Convert.ToString(node["name"]));
		builder.Append(" - ");
		builder.Append(Convert.ToString(node["durationMs"]));
		builder.Append(" ms");
		if (node.TryGetValue("selfMs", out object selfMs))
		{
			builder.Append(" self ");
			builder.Append(Convert.ToString(selfMs));
			builder.Append(" ms");
		}
		builder.AppendLine();
		if (!node.TryGetValue("children", out object childrenObj) || !(childrenObj is ArrayList children))
		{
			return;
		}
		foreach (object childObject in children)
		{
			if (childObject is Dictionary<string, object> child)
			{
				AppendFlameNode(builder, child, depth + 1);
			}
		}
	}
}
