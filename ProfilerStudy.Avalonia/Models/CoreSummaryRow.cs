using System;
using System.Collections.Generic;
using System.Linq;

namespace ProfilerStudy.Avalonia;

public sealed class CoreSummaryRow
{
	public CoreSummaryRow(int coreIndex, IEnumerable<long> contextSwitchTimes, long visibleStartTime, long visibleEndTime)
	{
		CoreIndex = coreIndex;
		ContextSwitchTimes = (contextSwitchTimes ?? Array.Empty<long>()).ToArray();
		VisibleStartTime = visibleStartTime;
		VisibleEndTime = visibleEndTime < visibleStartTime ? visibleStartTime : visibleEndTime;
	}

	public int CoreIndex { get; }

	public IReadOnlyList<long> ContextSwitchTimes { get; }

	public int ContextSwitchCount => ContextSwitchTimes.Count;

	public long VisibleStartTime { get; }

	public long VisibleEndTime { get; }

	public string CoreText => "Core " + CoreIndex;
}
