using System.Collections;

namespace ProfilerStudy.Tracy;

public sealed class TracyLiveCaptureResult
{
	public TracyLiveCaptureResult(TracyEventStream eventStream, ArrayList diagnostics, double connectMilliseconds)
	{
		EventStream = eventStream;
		Diagnostics = diagnostics;
		ConnectMilliseconds = connectMilliseconds;
	}

	public TracyEventStream EventStream { get; }

	public ArrayList Diagnostics { get; }

	public double ConnectMilliseconds { get; }
}
