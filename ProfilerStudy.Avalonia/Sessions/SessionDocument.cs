using System;
using FramePro;

namespace ProfilerStudy.Avalonia;

internal sealed class SessionDocument
{
	public SessionDocument(string sourcePath, Session session, SessionSummary summary, FrameSample[] frameSamples)
	{
		Id = Guid.NewGuid().ToString("N");
		SourcePath = sourcePath;
		Session = session;
		Summary = summary;
		FrameSamples = frameSamples;
		Selection = new TimelineSelection();
		Viewport = TimelineViewport.CreateForFrames(summary.FrameCount);
	}

	public string Id { get; }

	public string SourcePath { get; }

	public Session Session { get; }

	public SessionSummary Summary { get; }

	public FrameSample[] FrameSamples { get; }

	public TimelineSelection Selection { get; }

	public TimelineViewport Viewport { get; }
}
