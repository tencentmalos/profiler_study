using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ProfilerStudy;

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

		var customStatGraphs = GetCustomStatDescriptors(session)
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

	public static List<ProfilerCustomStatDescriptor> GetCustomStatDescriptors(Session session)
	{
		var customStats = session?.GetCustomStats();
		if (customStats == null || customStats.Count == 0)
		{
			return new List<ProfilerCustomStatDescriptor>();
		}

		return customStats
			.Select(item => item.Name)
			.Distinct()
			.Select(statId => CreateCustomStatDescriptor(session, statId))
			.Where(item => !string.IsNullOrWhiteSpace(item.Name))
			.OrderBy(item => item.GraphName)
			.ThenBy(item => item.Name)
			.Take(kMaxCustomStatCurves)
			.ToList();
	}

	private static ProfilerCustomStatDescriptor CreateCustomStatDescriptor(Session session, long statId)
	{
		string unit = session.GetCustomStatUnit(statId);
		bool convertCyclesToMilliseconds = string.Equals(unit, "cycles", StringComparison.OrdinalIgnoreCase);
		return new ProfilerCustomStatDescriptor(
			statId,
			session.GetString(statId),
			NormalizeGraphName(session.GetCustomStatGraph(statId)),
			convertCyclesToMilliseconds ? "ms" : unit,
			convertCyclesToMilliseconds,
			session.GetCustomStatColour(statId));
	}
}

internal sealed class ProfilerCustomStatDescriptor
{
	public ProfilerCustomStatDescriptor(long statId, string name, string graphName, string displayUnit, bool convertCyclesToMilliseconds, Color color)
	{
		StatId = statId;
		Name = name;
		GraphName = graphName;
		DisplayUnit = displayUnit;
		ConvertCyclesToMilliseconds = convertCyclesToMilliseconds;
		Color = color;
	}

	public long StatId { get; }

	public string Name { get; }

	public string GraphName { get; }

	public string DisplayUnit { get; }

	public bool ConvertCyclesToMilliseconds { get; }

	public Color Color { get; }
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
