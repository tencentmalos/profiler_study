namespace ProfilerStudy;

internal sealed class FrameStartPacket : IPacket
{
	public int m_Legacy1;

	public int m_Legacy2;

	public long m_FrameStartTime;

	public long m_Legacy3;

	public long m_PrevFrameSendTime;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_Legacy1 = reader.ReadInt32();
		m_Legacy2 = reader.ReadInt32();
		reader.ReadInt32();
		m_FrameStartTime = reader.ReadInt64();
		m_Legacy3 = reader.ReadInt64();
		m_PrevFrameSendTime = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 32;
	}
}
