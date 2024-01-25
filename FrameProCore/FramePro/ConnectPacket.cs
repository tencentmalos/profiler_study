namespace FramePro;

internal sealed class ConnectPacket : IPacket
{
	public int m_FrameProLibVersion;

	public long m_TimerFrequency;

	public int m_ProcessId;

	public Platform m_Platform;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_FrameProLibVersion = reader.ReadInt32();
		m_TimerFrequency = reader.ReadInt64();
		m_ProcessId = reader.ReadInt32();
		m_Platform = (Platform)reader.ReadInt32();
	}

	public int GetSize()
	{
		return 20;
	}
}
