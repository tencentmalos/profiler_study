namespace FramePro;

internal sealed class NamedTimeSpanPacketWithCallstack : IPacket
{
	public NamedTimeSpanPacket m_NamedTimeSpanPacket = new NamedTimeSpanPacket();

	public TimeSpanCallstackPacket m_CallstackPacket = new TimeSpanCallstackPacket();

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_NamedTimeSpanPacket.Read(reader, packed_value);
		m_CallstackPacket.Read(reader);
	}

	public int GetSize()
	{
		return m_NamedTimeSpanPacket.GetSize() + m_CallstackPacket.GetSize();
	}
}
