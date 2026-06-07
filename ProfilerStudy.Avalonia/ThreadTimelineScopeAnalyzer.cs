using System;
using System.Collections.Generic;
using System.Linq;

namespace ProfilerStudy.Avalonia;

internal static class ThreadTimelineScopeAnalyzer
{
	public static IReadOnlyList<ThreadTimelineScopeRow> Build(SessionDocument document, TimelineViewport viewport, int maxFrames = 90, int maxRows = 5000)
	{
		if (document?.Session == null || viewport == null || document.FrameSamples == null || document.FrameSamples.Length == 0)
		{
			return Array.Empty<ThreadTimelineScopeRow>();
		}

		int startFrame = Math.Max(0, Math.Min(viewport.StartFrame, document.Session.FrameCount - 1));
		int endFrame = Math.Max(startFrame, Math.Min(viewport.EndFrame, document.Session.FrameCount - 1));
		FrameSample[] visibleSamples = document.FrameSamples
			.Where(sample => sample.Index >= startFrame && sample.Index <= endFrame && sample.DurationMs > 0.0)
			.ToArray();
		if (visibleSamples.Length == 0)
		{
			return Array.Empty<ThreadTimelineScopeRow>();
		}

		int stride = Math.Max(1, (int)Math.Ceiling(visibleSamples.Length / (double)Math.Max(1, maxFrames)));
		List<ThreadTimelineScopeRow> rows = new List<ThreadTimelineScopeRow>();
		for (int i = 0; i < visibleSamples.Length && rows.Count < maxRows; i += stride)
		{
			FrameSample sample = visibleSamples[i];
			IReadOnlyList<ScopeFrameDetailRow> frameScopes = ScopeFrameDetailAnalyzer.Build(document, sample.Index, 160);
			foreach (ScopeFrameDetailRow scope in frameScopes)
			{
				if (rows.Count >= maxRows)
				{
					break;
				}

				if (scope.DurationMs <= 0.0)
				{
					continue;
				}

				double startRatio = Math.Clamp(scope.StartOffsetMs / sample.DurationMs, 0.0, 1.0);
				double endRatio = Math.Clamp((scope.StartOffsetMs + scope.DurationMs) / sample.DurationMs, startRatio, 1.0);
				if (endRatio <= startRatio)
				{
					endRatio = Math.Min(1.0, startRatio + 0.002);
				}

				rows.Add(new ThreadTimelineScopeRow(
					sample.Index,
					scope.ThreadName,
					scope.Depth,
					scope.Name,
					sample.Index + startRatio,
					sample.Index + endRatio));
			}
		}

		return rows;
	}
}
