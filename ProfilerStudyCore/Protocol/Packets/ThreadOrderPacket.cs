namespace ProfilerStudy;

internal sealed class ThreadOrderPacket : IPacket
{
	public long m_ThreadNameId;

	public void Read(ReceiveStream reader, int packed_value)
	{
		reader.ReadInt32();
		m_ThreadNameId = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 8;
	}
}
