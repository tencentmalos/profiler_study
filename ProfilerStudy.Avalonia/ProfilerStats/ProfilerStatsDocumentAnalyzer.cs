using System;
using System.Linq;
using FramePro;

namespace ProfilerStudy.Avalonia.ProfilerStats;

internal static class ProfilerStatsDocumentAnalyzer
{
	private const int kMaxCustomStatCurves = 10;

	public static ProfilerStatsDocumentSummary Analyze(SessionDocument document)
	{
		int frameSeriesPointCount = document?.FrameSamples?.Length ?? 0;
		Session session = document?.Session;
		if (session == null)
		{
			return new ProfilerStatsDocumentSummary(frameSeriesPointCount, 0, 0);
		}

		var customStats = session.GetCustomStats();
		if (customStats == null || customStats.Count == 0)
		{
			return new ProfilerStatsDocumentSummary(frameSeriesPointCount, 0, 0);
		}

		var customStatGraphs = customStats
			.Select(item => item.Name)
			.Distinct()
			.Select(statId => new
			{
				Name = session.GetString(statId),
				GraphName = NormalizeGraphName(session.GetCustomStatGraph(statId)),
			})
			.Where(item => !string.IsNullOrWhiteSpace(item.Name))
			.OrderBy(item => item.GraphName)
			.ThenBy(item => item.Name)
			.Take(kMaxCustomStatCurves)
			.GroupBy(item => item.GraphName)
			.ToList();

		return new ProfilerStatsDocumentSummary(
			frameSeriesPointCount,
			customStatGraphs.Count,
			customStatGraphs.Sum(group => group.Count()));
	}

	private static string NormalizeGraphName(string graphName)
	{
		return string.IsNullOrWhiteSpace(graphName) ? "default" : graphName;
	}
}

internal sealed class ProfilerStatsDocumentSummary
{
	public ProfilerStatsDocumentSummary(int frameSeriesPointCount, int customStatPlotCount, int customStatCurveCount)
	{
		FrameSeriesPointCount = frameSeriesPointCount;
		CustomStatPlotCount = customStatPlotCount;
		CustomStatCurveCount = customStatCurveCount;
	}

	public int FrameSeriesPointCount { get; }

	public int CustomStatPlotCount { get; }

	public int CustomStatCurveCount { get; }
}
