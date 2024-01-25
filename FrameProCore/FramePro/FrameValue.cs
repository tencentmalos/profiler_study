namespace FramePro;

public struct FrameValue
{
	public long m_FrameEndTime;

	public double m_Value;

	public double m_Count;

	public FrameValue(long time, double value, double count)
	{
		m_FrameEndTime = time;
		m_Value = value;
		m_Count = count;
	}
}
