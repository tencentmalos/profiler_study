using System;
using System.Collections.Generic;
using System.Linq;
using ProfilerStudy;

namespace ProfilerStudy.Avalonia;

internal static class ScopeHotspotAnalyzer
{
	public static IReadOnlyList<ScopeHotspotRow> Build(SessionDocument document, int maxRows = 200)
	{
		if (document?.Session == null || document.Session.TimerFrequency <= 0)
		{
			return Array.Empty<ScopeHotspotRow>();
		}

		List<ScopeSessionStats> stats = document.Session.GetTimeSpanStats();
		if (stats == null || stats.Count == 0)
		{
			return Array.Empty<ScopeHotspotRow>();
		}

		double ticksToMs = 1000.0 / document.Session.TimerFrequency;
		return stats
			.Select(item => new ScopeHotspotRow(
				item.m_Name,
				item.m_TotalTime * ticksToMs,
				item.m_TotalCount,
				item.m_MaxTimePerFrame * ticksToMs,
				item.m_MaxCountPerFrame))
			.OrderByDescending(item => item.TotalTimeMs)
			.ThenByDescending(item => item.MaxTimePerFrameMs)
			.Take(Math.Max(1, maxRows))
			.ToArray();
	}
}
