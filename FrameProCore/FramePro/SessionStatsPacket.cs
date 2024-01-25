namespace FramePro;

internal sealed class SessionStatsPacket : IPacket
{
	public long m_SendBufferSize;

	public long m_StringMemorySize;

	public long m_MiscMemorySize;

	public long m_RecordingFileSize;

	public void Read(ReceiveStream reader, int packed_value)
	{
		reader.ReadInt32();
		m_SendBufferSize = reader.ReadInt64();
		m_StringMemorySize = reader.ReadInt64();
		m_MiscMemorySize = reader.ReadInt64();
		m_RecordingFileSize = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 32;
	}
}
