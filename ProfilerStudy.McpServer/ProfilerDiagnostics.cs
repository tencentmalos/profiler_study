using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProfilerStudy.McpServer;

internal static class ProfilerDiagnostics
{
	public struct FrameSample
	{
		public readonly int Index;
		public readonly double DurationMs;
		public readonly int TimeSpanCount;
		public readonly int BytesSent;

		public FrameSample(int index, double durationMs, int timeSpanCount, int bytesSent)
		{
			Index = index;
			DurationMs = durationMs;
			TimeSpanCount = timeSpanCount;
			BytesSent = bytesSent;
		}
	}

	public static Dictionary<string, object> AnalyzeSlowFramePattern(IEnumerable<FrameSample> samples, double thresholdMs)
	{
		List<FrameSample> ordered = samples.OrderBy(s => s.Index).ToList();
		List<FrameSample> slow = thresholdMs > 0
			? ordered.Where(s => s.DurationMs >= thresholdMs).ToList()
			: ordered.ToList();

		List<int> deltas = new List<int>();
		for (int i = 1; i < slow.Count; i++)
		{
			deltas.Add(slow[i].Index - slow[i - 1].Index);
		}

		int dominantDelta = 0;
		int dominantDeltaCount = 0;
		if (deltas.Count != 0)
		{
			var group = deltas.GroupBy(d => d).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First();
			dominantDelta = group.Key;
			dominantDeltaCount = group.Count();
		}

		ArrayList clusters = new ArrayList();
		if (slow.Count != 0)
		{
			int start = slow[0].Index;
			int previous = slow[0].Index;
			int count = 1;
			for (int i = 1; i < slow.Count; i++)
			{
				int index = slow[i].Index;
				if (index - previous <= Math.Max(1, dominantDelta == 0 ? 1 : dominantDelta))
				{
					count++;
					previous = index;
					continue;
				}
				clusters.Add(new Dictionary<string, object> { ["startFrame"] = start, ["endFrame"] = previous, ["count"] = count });
				start = previous = index;
				count = 1;
			}
			clusters.Add(new Dictionary<string, object> { ["startFrame"] = start, ["endFrame"] = previous, ["count"] = count });
		}

		double averageDuration = slow.Count == 0 ? 0.0 : slow.Average(s => s.DurationMs);
		double averageTimeSpanCount = slow.Count == 0 ? 0.0 : slow.Average(s => s.TimeSpanCount);
		double averageBytesSent = slow.Count == 0 ? 0.0 : slow.Average(s => s.BytesSent);
		bool periodic = slow.Count >= 4 && dominantDelta > 0 && dominantDeltaCount >= Math.Max(2, deltas.Count / 2);

		return new Dictionary<string, object>
		{
			["slowFrameCount"] = slow.Count,
			["thresholdMs"] = Round(thresholdMs),
			["hasPeriodicSlowFrames"] = periodic,
			["dominantIndexDelta"] = dominantDelta,
			["dominantIndexDeltaCount"] = dominantDeltaCount,
			["averageSlowFrameMs"] = Round(averageDuration),
			["averageSlowFrameTimeSpanCount"] = Round(averageTimeSpanCount),
			["averageSlowFrameBytesSent"] = Round(averageBytesSent),
			["clusters"] = clusters,
			["indexDeltas"] = ToArrayList(deltas.Take(32))
		};
	}

	public static Dictionary<string, object> BuildDiagnostics(
		double frameDurationMs,
		IEnumerable<Dictionary<string, object>> hotspots)
	{
		List<Dictionary<string, object>> scopes = hotspots.ToList();
		Dictionary<string, object> frameScope = scopes.FirstOrDefault(IsFrameScope);
		double frameScopeMs = frameScope == null ? 0.0 : Convert.ToDouble(frameScope["totalMs"]);
		double largestNonFrameScopeMs = scopes.Where(s => !IsFrameScope(s))
			.Select(s => Convert.ToDouble(s["totalMs"]))
			.DefaultIfEmpty(0.0)
			.Max();

		double basis = frameScopeMs > 0.0 ? frameScopeMs : frameDurationMs;
		double broadUnattributedMs = Math.Max(0.0, basis - largestNonFrameScopeMs);

		ArrayList recommendations = new ArrayList();
		if (basis > 0.0 && largestNonFrameScopeMs < basis * 0.25)
		{
			recommendations.Add("Largest named child scope accounts for less than 25% of the frame; add scopes around waits, frame limiting, or outer loop gaps.");
		}
		if (scopes.Any(s => Convert.ToString(s["name"]).IndexOf("FrameLimiter", StringComparison.OrdinalIgnoreCase) >= 0))
		{
			recommendations.Add("Frame limiter instrumentation is present; compare limiter time against frame duration.");
		}

		return new Dictionary<string, object>
		{
			["frameScopeMs"] = Round(frameScopeMs),
			["largestNonFrameScopeMs"] = Round(largestNonFrameScopeMs),
			["broadUnattributedMs"] = Round(broadUnattributedMs),
			["broadUnattributedNote"] = "This subtracts only the largest non-frame scope from the frame duration, avoiding nested-scope double counting.",
			["recommendations"] = recommendations
		};
	}

	private static bool IsFrameScope(Dictionary<string, object> scope)
	{
		string name = Convert.ToString(scope["name"]);
		return name == "Frame {}" || name == "frame" || name.StartsWith("Frame ", StringComparison.Ordinal);
	}

	private static ArrayList ToArrayList(IEnumerable<int> values)
	{
		ArrayList list = new ArrayList();
		foreach (int value in values)
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
