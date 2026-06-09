using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProfilerStudy;
using SCLCoreCLR;

namespace ProfilerStudy.Avalonia;

internal sealed class SessionLoader
{
	public Task<SessionDocument> LoadFileAsync(string path, CancellationToken cancellationToken)
	{
		return Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();
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
}
