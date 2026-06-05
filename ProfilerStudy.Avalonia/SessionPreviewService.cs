using System;
using System.Collections.ObjectModel;
using FramePro;
using SCLCoreCLR;

namespace ProfilerStudy.Avalonia;

internal sealed class SessionPreviewService
{
	public SessionPreview LoadRandomSample()
	{
		CoreSettings settings = new CoreSettings();
		Session session = new Session(settings, new NullLog());
		session.FillWithRandomData();
		return CreatePreview(session, "Generated sample", settings.TargetFrameMS);
	}

	public SessionPreview LoadFile(string path)
	{
		CoreSettings settings = new CoreSettings();
		Session session = new Session(settings, new NullLog());
		string error = string.Empty;
		if (!session.Read(path, ref error))
		{
			throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Failed to read profiler session." : error);
		}

		return CreatePreview(session, path, settings.TargetFrameMS);
	}

	private static SessionPreview CreatePreview(Session session, string sourceName, double targetFrameMs)
	{
		ObservableCollection<FrameSample> samples = new ObservableCollection<FrameSample>();
		int frameCount = session.FrameCount;
		long frequency = session.TimerFrequency;
		double maxFrameMs = 0.0;
		double totalFrameMs = 0.0;

		for (int i = 0; i < frameCount; i++)
		{
			Frame frame = session.GetFrame(i);
			if (frame == null || !frame.Valid)
			{
				continue;
			}

			double frameMs = TicksToMs(frame.Duration, frequency);
			maxFrameMs = Math.Max(maxFrameMs, frameMs);
			totalFrameMs += frameMs;
			samples.Add(new FrameSample(frame.Index, frameMs));
		}

		double averageFrameMs = samples.Count == 0 ? 0.0 : totalFrameMs / samples.Count;
		return new SessionPreview(
			sourceName,
			samples,
			new SessionSummary
			{
				FrameCount = samples.Count,
				AverageFrameTimeMs = averageFrameMs,
				MaxFrameTimeMs = maxFrameMs,
				TargetFrameTimeMs = targetFrameMs <= 0.0 ? 33.333 : targetFrameMs,
				FirstFrameIndex = samples.Count == 0 ? 0 : samples[0].Index,
				LastFrameIndex = samples.Count == 0 ? 0 : samples[samples.Count - 1].Index
			});
	}

	private static double TicksToMs(long ticks, long frequency)
	{
		return frequency <= 0 ? 0.0 : ticks * 1000.0 / frequency;
	}
}

internal sealed class SessionPreview
{
	public SessionPreview(string sourceName, ObservableCollection<FrameSample> frameSamples, SessionSummary summary)
	{
		SourceName = sourceName;
		FrameSamples = frameSamples;
		Summary = summary;
	}

	public string SourceName { get; }

	public ObservableCollection<FrameSample> FrameSamples { get; }

	public SessionSummary Summary { get; }
}

internal sealed class SessionSummary
{
	public int FrameCount { get; set; }

	public double AverageFrameTimeMs { get; set; }

	public double MaxFrameTimeMs { get; set; }

	public double TargetFrameTimeMs { get; set; }

	public int FirstFrameIndex { get; set; }

	public int LastFrameIndex { get; set; }
}
