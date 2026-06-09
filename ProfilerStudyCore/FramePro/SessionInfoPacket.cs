namespace ProfilerStudy;

internal sealed class SessionInfoPacket : IPacket
{
	public long m_Name;

	public long m_Value;

	public void Read(ReceiveStream reader, int packed_value)
	{
		reader.ReadInt32();
		m_Name = reader.ReadInt64();
		m_Value = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 16;
	}
}
