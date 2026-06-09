using System;
using System.Collections.Generic;
using ProfilerStudy;

namespace ProfilerStudy.Avalonia;

internal sealed class SessionQueryService
{
	private readonly Session m_Session;
	private readonly double m_TargetFrameMs;

	public SessionQueryService(Session session, double targetFrameMs)
	{
		m_Session = session ?? throw new ArgumentNullException(nameof(session));
		m_TargetFrameMs = targetFrameMs <= 0.0 ? 33.333 : targetFrameMs;
	}

	public SessionSummary CreateSummary(string sourcePath, IReadOnlyList<FrameSample> samples)
	{
		double totalMs = 0.0;
		double maxMs = 0.0;
		int firstFrameIndex = 0;
		int lastFrameIndex = 0;

		if (samples != null && samples.Count != 0)
		{
			firstFrameIndex = samples[0].Index;
			lastFrameIndex = samples[samples.Count - 1].Index;
			for (int i = 0; i < samples.Count; i++)
			{
				totalMs += samples[i].DurationMs;
				maxMs = Math.Max(maxMs, samples[i].DurationMs);
			}
		}

		return new SessionSummary
		{
			FrameCount = samples?.Count ?? 0,
			AverageFrameTimeMs = samples == null || samples.Count == 0 ? 0.0 : totalMs / samples.Count,
			MaxFrameTimeMs = maxMs,
			TargetFrameTimeMs = m_TargetFrameMs,
			FirstFrameIndex = firstFrameIndex,
			LastFrameIndex = lastFrameIndex,
			ThreadCount = m_Session.GetThreadIds().Count,
			SourceName = sourcePath
		};
	}

	public FrameSample[] GetFrameSamples(int startFrame, int endFrame, int maxSamples)
	{
		int frameCount = m_Session.FrameCount;
		if (frameCount == 0)
		{
			return Array.Empty<FrameSample>();
		}

		startFrame = Math.Max(0, startFrame);
		endFrame = Math.Min(frameCount - 1, Math.Max(startFrame, endFrame));
		int count = endFrame - startFrame + 1;
		if (maxSamples <= 0 || count <= maxSamples)
		{
			return GetRawFrameSamples(startFrame, endFrame);
		}

		return GetBucketedFrameSamples(startFrame, endFrame, maxSamples);
	}

	private FrameSample[] GetRawFrameSamples(int startFrame, int endFrame)
	{
		List<FrameSample> samples = new List<FrameSample>(endFrame - startFrame + 1);
		long frequency = m_Session.TimerFrequency;
		for (int i = startFrame; i <= endFrame; i++)
		{
			Frame frame = m_Session.GetFrame(i);
			if (frame == null || !frame.Valid)
			{
				continue;
			}
			samples.Add(new FrameSample(frame.Index, TicksToMs(frame.Duration, frequency)));
		}
		return samples.ToArray();
	}

	private FrameSample[] GetBucketedFrameSamples(int startFrame, int endFrame, int maxSamples)
	{
		List<FrameSample> samples = new List<FrameSample>(maxSamples);
		long frequency = m_Session.TimerFrequency;
		double sourceCount = endFrame - startFrame + 1;
		for (int bucketIndex = 0; bucketIndex < maxSamples; bucketIndex++)
		{
			int bucketStart = startFrame + (int)Math.Floor(bucketIndex * sourceCount / maxSamples);
			int bucketEnd = startFrame + (int)Math.Floor((bucketIndex + 1) * sourceCount / maxSamples) - 1;
			bucketEnd = Math.Max(bucketStart, Math.Min(endFrame, bucketEnd));

			double maxDurationMs = 0.0;
			int maxFrameIndex = bucketStart;
			for (int frameIndex = bucketStart; frameIndex <= bucketEnd; frameIndex++)
			{
				Frame frame = m_Session.GetFrame(frameIndex);
				if (frame == null || !frame.Valid)
				{
					continue;
				}
				double frameMs = TicksToMs(frame.Duration, frequency);
				if (frameMs >= maxDurationMs)
				{
					maxDurationMs = frameMs;
					maxFrameIndex = frame.Index;
				}
			}
			samples.Add(new FrameSample(maxFrameIndex, maxDurationMs));
		}
		return samples.ToArray();
	}

	private static double TicksToMs(long ticks, long frequency)
	{
		return frequency <= 0 ? 0.0 : ticks * 1000.0 / frequency;
	}
}
