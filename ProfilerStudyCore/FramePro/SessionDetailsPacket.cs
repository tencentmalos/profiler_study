namespace ProfilerStudy;

internal sealed class SessionDetailsPacket : IPacket
{
	public long m_Name;

	public long m_BuildId;

	public long m_Date;

	public void Read(ReceiveStream reader, int packed_value)
	{
		reader.ReadInt32();
		m_Name = reader.ReadInt64();
		m_BuildId = reader.ReadInt64();
		m_Date = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 24;
	}
}
