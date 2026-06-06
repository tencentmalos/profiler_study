using System;
using System.Collections.Generic;
using FramePro;

namespace ProfilerStudy.Avalonia;

internal static class ScopeFrameDetailAnalyzer
{
	public static IReadOnlyList<ScopeFrameDetailRow> Build(SessionDocument document, int frameIndex, int maxRows = 500)
	{
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
			FramePro.TimeSpan span = session.GetTimeSpan(threadId, frameStart);
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

	private static void AppendRows(Session session, List<ScopeFrameDetailRow> rows, string threadName, FramePro.TimeSpan span, long frameStart, long frameEnd, double ticksToMs, int depth, int maxRows)
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
				FormatSource(sourceInfo)));
		}

		for (FramePro.TimeSpan child = span.Children; child != null && rows.Count < maxRows; child = child.Next)
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
