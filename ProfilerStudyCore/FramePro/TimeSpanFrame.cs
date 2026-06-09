namespace ProfilerStudy;

internal struct TimeSpanFrame
{
	public long m_FrameEndTime;

	public long m_TotalTimeSpanDuration;

	public int m_Count;

	public TimeSpanFrame(long frame_end_time, long total_time_span_duration, int count)
	{
		m_FrameEndTime = frame_end_time;
		m_TotalTimeSpanDuration = total_time_span_duration;
		m_Count = count;
	}
}
