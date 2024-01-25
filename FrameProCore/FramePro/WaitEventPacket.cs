namespace FramePro;

internal sealed class WaitEventPacket : IPacket
{
	public int m_Thread;

	public int m_Core;

	public long m_EventId;

	public long m_Time;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_Thread = reader.ReadInt32();
		m_Core = reader.ReadInt32();
		reader.ReadInt32();
		m_EventId = reader.ReadInt64();
		m_Time = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 24;
	}
}
