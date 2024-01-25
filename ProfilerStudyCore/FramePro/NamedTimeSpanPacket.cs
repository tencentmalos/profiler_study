namespace FramePro;

internal sealed class NamedTimeSpanPacket : IPacket
{
	public int m_ThreadID;

	public int m_Core;

	public long m_Name;

	public long m_SourceInfo;

	public long m_StartTime;

	public long m_EndTime;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_ThreadID = reader.ReadInt32();
		m_Core = packed_value;
		m_Name = reader.ReadInt64();
		m_SourceInfo = reader.ReadInt64();
		m_StartTime = reader.ReadInt64();
		m_EndTime = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 40;
	}
}
