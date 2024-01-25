namespace FramePro;

public class TimeSpanCustomStat
{
	public long m_Name;

	public CustomStatValueType m_ValueType;

	public long m_ValueInt64;

	public double m_ValueDouble;

	public TimeSpanCustomStat Clone()
	{
		return new TimeSpanCustomStat
		{
			m_Name = m_Name,
			m_ValueType = m_ValueType,
			m_ValueInt64 = m_ValueInt64,
			m_ValueDouble = m_ValueDouble
		};
	}
}
