namespace ProfilerStudy;

internal sealed class TimeSpanPacketWithCallstack : IPacket
{
	public TimeSpanPacket m_TimeSpanPacket = new TimeSpanPacket();

	public TimeSpanCallstackPacket m_CallstackPacket = new TimeSpanCallstackPacket();

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_TimeSpanPacket.Read(reader, packed_value);
		m_CallstackPacket.Read(reader);
	}

	public int GetSize()
	{
		return m_TimeSpanPacket.GetSize() + m_CallstackPacket.GetSize();
	}
}
