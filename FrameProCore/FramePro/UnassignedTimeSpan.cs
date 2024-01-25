using System.Collections.Generic;

namespace FramePro;

internal class UnassignedTimeSpan
{
	public long m_TimeSpanName;

	public long m_TimeSpanStartTime;

	public long m_StartTime;

	public long m_EndTime;

	public int m_ThreadId;

	public List<HiResTimer> m_HiResTimers;
}
