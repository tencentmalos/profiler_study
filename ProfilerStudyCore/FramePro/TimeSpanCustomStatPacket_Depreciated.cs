namespace FramePro;

internal sealed class TimeSpanCustomStatPacket_Depreciated : IPacket
{
	public int m_Thread;

	public CustomStatValueType m_ValueType;

	public long m_Name;

	public long m_Unit;

	public long m_ValueInt64;

	public double m_ValueDouble;

	public long m_Time;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_Thread = reader.ReadInt32();
		m_ValueType = (CustomStatValueType)reader.ReadInt32();
		reader.ReadInt32();
		m_Name = reader.ReadInt64();
		m_Unit = reader.ReadInt64();
		m_ValueInt64 = reader.ReadInt64();
		m_ValueDouble = reader.ReadDouble();
		m_Time = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 48;
	}
}
