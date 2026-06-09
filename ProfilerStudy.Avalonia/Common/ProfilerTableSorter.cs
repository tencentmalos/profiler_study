using System;
using System.Collections.Generic;
using System.Linq;

namespace ProfilerStudy.Avalonia;

internal static class ProfilerTableSorter
{
	public static IReadOnlyList<ScopeHotspotRow> SortHotspots(IEnumerable<ScopeHotspotRow> rows, string sortKey, bool descending)
	{
		IEnumerable<ScopeHotspotRow> source = rows ?? Array.Empty<ScopeHotspotRow>();
		return ApplyDirection(source, item => sortKey switch
		{
			"Name" => item.Name,
			"Calls" => item.TotalCount,
			"Average" => item.AverageTimeMs,
			"MaxTime" => item.MaxTimePerFrameMs,
			"MaxCount" => item.MaxCountPerFrame,
			_ => item.TotalTimeMs,
		}, descending).ToArray();
	}

	public static IReadOnlyList<ScopeFrameDetailRow> SortFrameScopes(IEnumerable<ScopeFrameDetailRow> rows, string sortKey, bool descending)
	{
		IEnumerable<ScopeFrameDetailRow> source = rows ?? Array.Empty<ScopeFrameDetailRow>();
		return ApplyDirection(source, item => sortKey switch
		{
			"Thread" => item.ThreadName,
			"Scope" => item.Name,
			"Duration" => item.DurationMs,
			"Source" => item.SourceText,
			_ => item.StartOffsetMs,
		}, descending).ToArray();
	}

	public static IReadOnlyList<SelectedFrameCounterRow> SortCounters(IEnumerable<SelectedFrameCounterRow> rows, string sortKey, bool descending)
	{
		IEnumerable<SelectedFrameCounterRow> source = rows ?? Array.Empty<SelectedFrameCounterRow>();
		return ApplyDirection(source, item => sortKey switch
		{
			"Counter" => item.Name,
			"Value" => item.Value,
			"Count" => item.Count,
			"Unit" => item.Unit,
			_ => item.GraphName,
		}, descending).ToArray();
	}

	private static IEnumerable<T> ApplyDirection<T>(IEnumerable<T> rows, Func<T, object> keySelector, bool descending)
	{
		return descending ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);
	}
}
