namespace FramePro;

internal sealed class ThreadNamePacket : IPacket
{
	public int m_ThreadId;

	public long m_Name;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_ThreadId = reader.ReadInt32();
		m_Name = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 12;
	}
}
