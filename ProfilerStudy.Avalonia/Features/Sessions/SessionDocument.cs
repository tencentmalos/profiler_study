using System;
using ProfilerStudy;
using ProfilerStudy.Trace;

namespace ProfilerStudy.Avalonia;

internal sealed class SessionDocument
{
	public SessionDocument(string sourcePath, Session session, SessionSummary summary, FrameSample[] frameSamples)
		: this(sourcePath, session, null, summary, frameSamples)
	{
	}

	public SessionDocument(string sourcePath, Session session, TraceDocument traceDocument, SessionSummary summary, FrameSample[] frameSamples)
	{
		Id = Guid.NewGuid().ToString("N");
		SourcePath = sourcePath;
		Session = session;
		TraceDocument = traceDocument;
		Summary = summary;
		FrameSamples = frameSamples;
		Selection = new TimelineSelection();
		Viewport = TimelineViewport.CreateForFrames(summary.FrameCount);
	}

	public string Id { get; }

	public string SourcePath { get; }

	public Session Session { get; }

	public TraceDocument TraceDocument { get; }

	public SessionSummary Summary { get; }

	public FrameSample[] FrameSamples { get; }

	public TimelineSelection Selection { get; }

	public TimelineViewport Viewport { get; }
}
