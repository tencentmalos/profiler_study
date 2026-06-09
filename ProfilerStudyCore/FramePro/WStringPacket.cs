namespace ProfilerStudy;

internal sealed class WStringPacket : IPacket
{
	public long m_StringId;

	public string m_String;

	public void Read(ReceiveStream reader, int packed_value)
	{
		int length = reader.ReadInt32();
		m_StringId = reader.ReadInt64();
		m_String = StringReader.ReadW(reader, length);
	}

	public int GetSize()
	{
		return 8 + m_String.Length * 2;
	}
}
