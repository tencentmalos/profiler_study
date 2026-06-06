using System;
using System.Collections.Generic;
using FramePro;
using ProfilerStudy.Avalonia.ProfilerStats;

namespace ProfilerStudy.Avalonia;

internal static class SelectedFrameCounterAnalyzer
{
	public static IReadOnlyList<SelectedFrameCounterRow> Build(SessionDocument document, int frameIndex)
	{
		if (document?.Session == null || document.Session.TimerFrequency <= 0 || frameIndex < 0 || frameIndex >= document.Session.FrameCount)
		{
			return Array.Empty<SelectedFrameCounterRow>();
		}

		Session session = document.Session;
		List<ProfilerCustomStatDescriptor> descriptors = ProfilerStatsDocumentAnalyzer.GetCustomStatDescriptors(session);
		if (descriptors.Count == 0)
		{
			return Array.Empty<SelectedFrameCounterRow>();
		}

		List<SelectedFrameCounterRow> rows = new List<SelectedFrameCounterRow>();
		List<FrameValue> values = new List<FrameValue>(1);
		foreach (ProfilerCustomStatDescriptor descriptor in descriptors)
		{
			values.Clear();
			session.GetCustomStats(frameIndex, frameIndex, descriptor.StatId, false, values);
			if (values.Count == 0)
			{
				continue;
			}

			double value = values[0].m_Value;
			if (descriptor.ConvertCyclesToMilliseconds)
			{
				value = value * 1000.0 / session.TimerFrequency;
			}

			rows.Add(new SelectedFrameCounterRow(
				descriptor.GraphName,
				descriptor.Name,
				value,
				values[0].m_Count,
				descriptor.DisplayUnit));
		}

		return rows;
	}
}
