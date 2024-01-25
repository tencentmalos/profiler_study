namespace FramePro;

internal sealed class CustomStatPacket : IPacket
{
	public int m_Count;

	public long m_Name;

	public long m_ValueInt64;

	public double m_ValueDouble;

	public CustomStatValueType m_ValueType;

	public long m_Time;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_ValueType = (CustomStatValueType)packed_value;
		m_Count = reader.ReadInt32();
		m_Name = reader.ReadInt64();
		switch (m_ValueType)
		{
		case CustomStatValueType.Int64:
			m_ValueInt64 = reader.ReadInt64();
			break;
		case CustomStatValueType.Double:
			m_ValueDouble = reader.ReadDouble();
			break;
		}
		m_Time = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 40;
	}
}
