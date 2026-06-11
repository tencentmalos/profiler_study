using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProfilerStudy;
using ProfilerStudy.Tracy;
using ProfilerStudy.Trace;
using SCLCoreCLR;

namespace ProfilerStudy.Avalonia;

internal sealed class SessionLoader
{
	public Task<SessionDocument> LoadFileAsync(string path, CancellationToken cancellationToken)
	{
		return Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (IsTracyFile(path))
			{
				TraceDocument traceDocument = TracyTraceImporter.Load(path, cancellationToken);
				return CreateTraceDocument(traceDocument, path);
			}

			CoreSettings settings = new CoreSettings();
			Session session = new Session(settings, new NullLog());
			string error = string.Empty;
			if (!session.Read(path, ref error))
			{
				throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Failed to read profiler session." : error);
			}

			cancellationToken.ThrowIfCancellationRequested();
			return CreateDocument(session, path, settings.TargetFrameMS);
		}, cancellationToken);
	}

	public Task<SessionDocument> LoadSampleAsync(CancellationToken cancellationToken)
	{
		return Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();
			const int frameCount = 900;
			const double targetFrameMs = 33.333;
			FrameSample[] samples = new FrameSample[frameCount];
			double totalMs = 0.0;
			double maxMs = 0.0;
			for (int i = 0; i < frameCount; i++)
			{
				double wave = 6.0 + Math.Sin(i / 18.0) * 4.0 + Math.Sin(i / 53.0) * 2.0;
				double spike = i % 137 == 0 ? 42.0 : (i % 89 == 0 ? 24.0 : 0.0);
				double durationMs = Math.Max(8.0, 16.0 + wave + spike);
				samples[i] = new FrameSample(i, durationMs);
				totalMs += durationMs;
				maxMs = Math.Max(maxMs, durationMs);
			}

			SessionSummary summary = new SessionSummary
			{
				FrameCount = frameCount,
				AverageFrameTimeMs = totalMs / frameCount,
				MaxFrameTimeMs = maxMs,
				TargetFrameTimeMs = targetFrameMs,
				FirstFrameIndex = 0,
				LastFrameIndex = frameCount - 1,
				ThreadCount = 4,
				SourceName = "Generated sample"
			};
			return new SessionDocument("Generated sample", null, summary, samples);
		}, cancellationToken);
	}

	private static SessionDocument CreateDocument(Session session, string sourcePath, double targetFrameMs)
	{
		SessionQueryService queryService = new SessionQueryService(session, targetFrameMs);
		FrameSample[] samples = queryService.GetFrameSamples(0, Math.Max(0, session.FrameCount - 1), 0);
		SessionSummary summary = queryService.CreateSummary(sourcePath, samples);
		summary.SourceName = string.IsNullOrWhiteSpace(sourcePath) ? "Unknown" : Path.GetFileName(sourcePath);
		if (string.IsNullOrWhiteSpace(summary.SourceName))
		{
			summary.SourceName = sourcePath;
		}

		return new SessionDocument(sourcePath, session, summary, samples);
	}

	internal static SessionDocument CreateTraceDocument(TraceDocument traceDocument, string sourcePath)
	{
		FrameSample[] samples = GetTraceFrameSamples(traceDocument);
		SessionSummary summary = CreateTraceSummary(traceDocument, sourcePath, samples);
		return new SessionDocument(sourcePath, null, traceDocument, summary, samples);
	}

	private static FrameSample[] GetTraceFrameSamples(TraceDocument traceDocument)
	{
		if (traceDocument?.QuerySession is not TracyTraceQuerySession tracyQuerySession ||
			tracyQuerySession.EventStream.Metadata == null)
		{
			return Array.Empty<FrameSample>();
		}

		var samples = new System.Collections.Generic.List<FrameSample>();
		foreach (TracyFrameSummary frame in tracyQuerySession.GetVisibleFrames())
		{
			samples.Add(new FrameSample(frame.FrameIndex, frame.Duration / 1_000_000.0));
		}
		return samples.ToArray();
	}

	private static SessionSummary CreateTraceSummary(TraceDocument traceDocument, string sourcePath, FrameSample[] samples)
	{
		double totalMs = 0.0;
		double maxMs = 0.0;
		foreach (FrameSample sample in samples)
		{
			totalMs += sample.DurationMs;
			maxMs = Math.Max(maxMs, sample.DurationMs);
		}

		int threadCount = 0;
		if (traceDocument?.QuerySession is TracyTraceQuerySession tracyQuerySession)
		{
			threadCount = tracyQuerySession.EventStream.ThreadCount;
		}
		return new SessionSummary
		{
			FrameCount = samples.Length,
			AverageFrameTimeMs = samples.Length == 0 ? 0.0 : totalMs / samples.Length,
			MaxFrameTimeMs = maxMs,
			TargetFrameTimeMs = 33.333,
			FirstFrameIndex = samples.Length == 0 ? 0 : samples[0].Index,
			LastFrameIndex = samples.Length == 0 ? 0 : samples[samples.Length - 1].Index,
			ThreadCount = threadCount,
			SourceName = string.IsNullOrWhiteSpace(sourcePath) ? traceDocument?.DisplayName : Path.GetFileName(sourcePath)
		};
	}

	private static bool IsTracyFile(string path)
	{
		return string.Equals(Path.GetExtension(path), ".tracy", StringComparison.OrdinalIgnoreCase);
	}
}
