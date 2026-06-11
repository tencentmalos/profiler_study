using System;
using System.Collections.Generic;
using System.Linq;
using ProfilerStudy;
using ProfilerStudy.Avalonia.ProfilerStats;
using ProfilerStudy.Tracy;

namespace ProfilerStudy.Avalonia;

internal static class SelectedFrameCounterAnalyzer
{
	public static IReadOnlyList<SelectedFrameCounterRow> Build(SessionDocument document, int frameIndex)
	{
		if (document?.Session == null)
		{
			return BuildTracyCounters(document, frameIndex);
		}

		if (document.Session.TimerFrequency <= 0 || frameIndex < 0 || frameIndex >= document.Session.FrameCount)
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

	private static IReadOnlyList<SelectedFrameCounterRow> BuildTracyCounters(SessionDocument document, int frameIndex)
	{
		if (document?.TraceDocument?.QuerySession is not TracyTraceQuerySession tracyQuerySession ||
			tracyQuerySession.EventStream.Metadata == null ||
			tracyQuerySession.EventStream.Plots == null ||
			tracyQuerySession.EventStream.Plots.Count == 0 ||
			frameIndex < 0)
		{
			return Array.Empty<SelectedFrameCounterRow>();
		}

		if (!TryGetTracyFrame(tracyQuerySession, frameIndex, out TracyFrameSummary frame))
		{
			return Array.Empty<SelectedFrameCounterRow>();
		}

		List<SelectedFrameCounterRow> rows = new List<SelectedFrameCounterRow>();
		foreach (TracyPlotSummary plot in tracyQuerySession.EventStream.Plots)
		{
			List<TracyPlotSample> samples = plot.Samples
				.Where(sample => sample.Time >= frame.Start && sample.Time <= frame.End)
				.ToList();
			if (samples.Count == 0)
			{
				continue;
			}
			rows.Add(new SelectedFrameCounterRow(
				"tracy",
				plot.Name,
				samples.Average(sample => sample.Value),
				samples.Count,
				string.Empty));
		}

		return rows;
	}

	private static bool TryGetTracyFrame(TracyTraceQuerySession querySession, int frameIndex, out TracyFrameSummary frame)
	{
		foreach (TracyFrameSummary candidate in querySession.GetVisibleFrames())
		{
			if (candidate.FrameIndex == frameIndex)
			{
				frame = candidate;
				return true;
			}
		}
		frame = null;
		return false;
	}
}
