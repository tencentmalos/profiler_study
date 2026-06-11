using System.Collections.Generic;

namespace ProfilerStudy.Trace;

public interface ITraceQuerySession
{
	string SourceFormat { get; }

	string SourcePath { get; }

	string DisplayName { get; }

	Dictionary<string, object> GetSummary(int top);

	Dictionary<string, object> FindSlowFrames(int top, double thresholdMs);

	Dictionary<string, object> FindScopeHotspots(int top, int startFrame, int endFrame);

	Dictionary<string, object> GetProfilerOverhead(int startFrame, int endFrame, int top);

	Dictionary<string, object> ListCounters(int top, string filter);

	Dictionary<string, object> QueryCounterSamples(string counterName, int startFrame, int endFrame, bool accumulated, int maxSamples);

	Dictionary<string, object> AnalyzeFrame(int frameIndex, int top, int neighborCount);

	Dictionary<string, object> AnalyzeFrameDetail(int frameIndex, int maxNodes, int maxDepth, double minDurationMs);

	Dictionary<string, object> AnalyzeTimeRange(int startFrame, int endFrame, int top, double thresholdMs);

	Dictionary<string, object> AnalyzeTimeRange(long startTimeNs, long endTimeNs, int top, double thresholdMs);
}
