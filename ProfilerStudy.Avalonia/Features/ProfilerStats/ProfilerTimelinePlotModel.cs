using ProfilerStudy.Avalonia.ProfilerStats.Timeline;
using System.Collections.Generic;

namespace ProfilerStudy.Avalonia.ProfilerStats;

internal sealed class ProfilerTimelinePlotModel
{
	public ProfilerTimelinePlotModel(DiagramMetadata metadata, IReadOnlyList<ProfilerCustomStatTimelineInfo> customStats)
	{
		Metadata = metadata;
		CustomStats = customStats;
	}

	public DiagramMetadata Metadata { get; }

	public IReadOnlyList<ProfilerCustomStatTimelineInfo> CustomStats { get; }
}

internal sealed class ProfilerCustomStatTimelineInfo
{
	public ProfilerCustomStatTimelineInfo(string plotKey, string curveKey, long statId, bool convertCyclesToMilliseconds)
	{
		PlotKey = plotKey;
		CurveKey = curveKey;
		StatId = statId;
		ConvertCyclesToMilliseconds = convertCyclesToMilliseconds;
	}

	public string PlotKey { get; }

	public string CurveKey { get; }

	public long StatId { get; }

	public bool ConvertCyclesToMilliseconds { get; }
}
