namespace ProfilerStudy;

internal sealed class EventPacket : IPacket
{
	public uint m_Colour;

	public long m_Name;

	public long m_Time;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_Colour = reader.ReadUInt32();
		m_Name = reader.ReadInt64();
		m_Time = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 20;
	}
}
