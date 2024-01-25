namespace FramePro;

internal class TimeRange
{
	public long m_StartTime;

	public double m_Scale;

	public long m_TicksPerPixel;

	public int m_ForceUpdateCounter;

	public int m_ScrollY;

	public override bool Equals(object obj)
	{
		if (obj is TimeRange timeRange && m_StartTime == timeRange.m_StartTime && m_TicksPerPixel == timeRange.m_TicksPerPixel && m_ForceUpdateCounter == timeRange.m_ForceUpdateCounter)
		{
			return m_ScrollY == timeRange.m_ScrollY;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public void Copy(TimeRange other)
	{
		CopyAllExceptScrollY(other);
		m_ScrollY = other.m_ScrollY;
	}

	public void CopyAllExceptScrollY(TimeRange other)
	{
		m_StartTime = other.m_StartTime;
		m_Scale = other.m_Scale;
		m_TicksPerPixel = other.m_TicksPerPixel;
		m_ForceUpdateCounter = other.m_ForceUpdateCounter;
	}
}
