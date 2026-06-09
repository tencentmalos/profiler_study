namespace ProfilerStudy;

internal sealed class ProcessNamePacket : IPacket
{
	public int m_ProcessId;

	public long m_Name;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_ProcessId = reader.ReadInt32();
		m_Name = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 12;
	}
}
