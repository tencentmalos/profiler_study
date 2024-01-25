namespace FramePro;

internal sealed class LogPacket : IPacket
{
	public long m_Time;

	public string m_String;

	public void Read(ReceiveStream reader, int packed_value)
	{
		int length = reader.ReadInt32();
		m_Time = reader.ReadInt64();
		m_String = StringReader.Read(reader, length);
	}

	public int GetSize()
	{
		return 8 + m_String.Length;
	}
}
