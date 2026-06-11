using System;
using System.Collections.Generic;
using ProfilerStudy.Tracy;
using ProfilerStudy;

namespace ProfilerStudy.Avalonia;

internal static class ScopeFrameDetailAnalyzer
{
	public static IReadOnlyList<ScopeFrameDetailRow> Build(SessionDocument document, int frameIndex, int maxRows = 500)
	{
		if (document?.TraceDocument?.QuerySession is TracyTraceQuerySession tracyQuerySession)
		{
			return BuildTracyRows(tracyQuerySession, frameIndex, maxRows);
		}

		if (document?.Session == null || document.Session.TimerFrequency <= 0 || frameIndex < 0 || frameIndex >= document.Session.FrameCount)
		{
			return Array.Empty<ScopeFrameDetailRow>();
		}

		Session session = document.Session;
		session.GetFrameStartEndTime(frameIndex, out long frameStart, out long frameEnd);
		double ticksToMs = 1000.0 / session.TimerFrequency;
		List<ScopeFrameDetailRow> rows = new List<ScopeFrameDetailRow>();
		foreach (int threadId in session.GetThreadIds())
		{
			ProfilerStudy.TimeSpan span = session.GetTimeSpan(threadId, frameStart);
			if (span == null)
			{
				continue;
			}

			while (span.Parent != null && span.Parent.TimeSpanInfoId != TimeSpanInfo.InvalidInfoId)
			{
				span = span.Parent;
			}

			AppendRows(session, rows, session.GetThreadName(threadId), span, frameStart, frameEnd, ticksToMs, 0, maxRows);
			if (rows.Count >= maxRows)
			{
				break;
			}
		}

		return rows;
	}

	private static IReadOnlyList<ScopeFrameDetailRow> BuildTracyRows(TracyTraceQuerySession querySession, int frameIndex, int maxRows)
	{
		if (querySession.EventStream?.Metadata == null ||
			querySession.EventStream.CpuZones == null ||
			querySession.EventStream.CpuZones.Count == 0 ||
			frameIndex < 0 ||
			maxRows <= 0)
		{
			return Array.Empty<ScopeFrameDetailRow>();
		}

		TracyFrameSummary frame = FindFrame(querySession, frameIndex);
		if (frame == null || frame.Duration <= 0)
		{
			return Array.Empty<ScopeFrameDetailRow>();
		}

		List<ScopeFrameDetailRow> rows = new List<ScopeFrameDetailRow>();
		foreach (TracyCpuZoneSummary zone in querySession.EventStream.CpuZones)
		{
			if (rows.Count >= maxRows)
			{
				break;
			}

			long clippedStart = Math.Max(zone.Start, frame.Start);
			long clippedEnd = Math.Min(zone.End, frame.End);
			if (clippedEnd <= clippedStart)
			{
				continue;
			}

			rows.Add(new ScopeFrameDetailRow(
				"Thread " + zone.ThreadId,
				0,
				string.IsNullOrWhiteSpace(zone.Name) ? "(unnamed)" : zone.Name,
				(clippedStart - frame.Start) / 1_000_000.0,
				(clippedEnd - clippedStart) / 1_000_000.0,
				string.Empty,
				string.Empty,
				-1));
		}

		return rows;
	}

	private static TracyFrameSummary FindFrame(TracyTraceQuerySession querySession, int frameIndex)
	{
		foreach (TracyFrameSummary frame in querySession.GetVisibleFrames())
		{
			if (frame.FrameIndex == frameIndex)
			{
				return frame;
			}
		}

		return null;
	}

	private static void AppendRows(Session session, List<ScopeFrameDetailRow> rows, string threadName, ProfilerStudy.TimeSpan span, long frameStart, long frameEnd, double ticksToMs, int depth, int maxRows)
	{
		if (span == null || rows.Count >= maxRows)
		{
			return;
		}

		if (span.TimeSpanInfoId != TimeSpanInfo.InvalidInfoId && span.StartTime < frameEnd && span.EndTime > frameStart)
		{
			TimeSpanInfo info = session.GetTimeSpanInfo(span.TimeSpanInfoId);
			SourceInfoStruct sourceInfo = session.GetSourceInfo(info.SourceInfo);
			rows.Add(new ScopeFrameDetailRow(
				threadName,
				depth,
				session.GetTimerName(info.Name),
				Math.Max(0, span.StartTime - frameStart) * ticksToMs,
				span.Duration * ticksToMs,
				FormatSource(sourceInfo),
				sourceInfo.IsValid ? sourceInfo.Filename : string.Empty,
				sourceInfo.IsValid ? sourceInfo.Line : -1));
		}

		for (ProfilerStudy.TimeSpan child = span.Children; child != null && rows.Count < maxRows; child = child.Next)
		{
			if (child.EndTime <= frameStart)
			{
				continue;
			}

			if (child.StartTime >= frameEnd)
			{
				break;
			}

			AppendRows(session, rows, threadName, child, frameStart, frameEnd, ticksToMs, depth + 1, maxRows);
		}
	}

	private static string FormatSource(SourceInfoStruct sourceInfo)
	{
		if (!sourceInfo.IsValid)
		{
			return string.Empty;
		}

		string file = string.IsNullOrWhiteSpace(sourceInfo.Filename) ? sourceInfo.Function : sourceInfo.Filename;
		if (string.IsNullOrWhiteSpace(file))
		{
			return string.Empty;
		}

		return sourceInfo.Line > 0 ? file + ":" + sourceInfo.Line : file;
	}
}
