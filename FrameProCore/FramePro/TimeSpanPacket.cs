namespace FramePro;

internal sealed class TimeSpanPacket : IPacket
{
	public int m_ThreadID;

	public int m_Core;

	public long m_NameAndSourceInfo;

	public long m_StartTime;

	public long m_EndTime;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_ThreadID = reader.ReadInt32();
		m_Core = packed_value;
		m_NameAndSourceInfo = reader.ReadInt64();
		m_StartTime = reader.ReadInt64();
		m_EndTime = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 32;
	}
}
