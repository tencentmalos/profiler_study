using System.Collections.Generic;

namespace ProfilerStudy.Trace;

public interface ITraceQuerySession
{
	string SourceFormat { get; }

	string SourcePath { get; }

	string DisplayName { get; }

	Dictionary<string, object> GetSummary(int top);
}
