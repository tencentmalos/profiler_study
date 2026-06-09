namespace ProfilerStudy;

internal sealed class ContextSwitchRecordingStartedPacket : IPacket
{
	public bool m_StartedSucessfully;

	public string m_Error;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_StartedSucessfully = reader.ReadInt32() == 1;
		m_Error = StringReader.ReadInlineString(reader);
	}

	public int GetSize()
	{
		return 4 + StringReader.MaxInlineStringLength;
	}
}
